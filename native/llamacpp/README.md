# llama.cpp build for WelsonJS

[한국어 문서](README_KO.md)

This folder builds the Windows llama.cpp that WelsonJS uses. The source is the llama.cpp fork that [bitnet.cpp](https://github.com/microsoft/BitNet) includes as its `3rdparty/llama.cpp` submodule, with WelsonJS's patches applied. The output contains the same files as the official `llama-<version>-bin-win-cpu-<arch>.zip` package: `llama-server`, `llama-cli`, `llama-quantize`, and the ggml backends. The x64 build has every x86-64 CPU variant, chosen at run time; the arm64 build has one CPU backend.

The fork adds BitNet's `I2_S` type and kernels, so besides the usual GGUF files the build loads 1.58-bit BitNet models such as [microsoft/BitNet-b1.58-2B-4T-gguf](https://huggingface.co/microsoft/BitNet-b1.58-2B-4T-gguf), which upstream llama.cpp cannot load.

On top of the fork, this build adds the STQ1_0 ternary type and kernels from Tencent's [AngelSlim](https://github.com/Tencent/AngelSlim) quantization ([ggml-org/llama.cpp#22836](https://github.com/ggml-org/llama.cpp/pull/22836), not merged upstream; see [0001-stq1_0.patch](#0001-stq1_0patch)). It loads 1.25-bit models produced by AngelSlim, such as [tencent/Hy-MT2-1.8B-1.25Bit-GGUF](https://huggingface.co/tencent/Hy-MT2-1.8B-1.25Bit-GGUF), which neither the fork nor upstream llama.cpp can load.

The launcher uses `llama-server` only through its OpenAI-compatible HTTP API (`/llamacpp/chat`, `lib/llamacpp.js`).

## Files

| File | Purpose |
| --- | --- |
| `commit.txt` | The llama.cpp commit that is built, as a full 40-character SHA. The source is fetched by this SHA rather than by a branch, because a branch can move and a commit cannot. |
| `version.txt` | A label for the build. It is a label only and does not select the source. |
| `source-archive.txt` | The fallback source archive (`url=`) and its SHA-256 (`sha256=`), used when git cannot fetch the commit. |
| `patches/*.patch` | Patches applied in name order with `git apply`. They are stored with LF line endings, and `.gitattributes` keeps them byte-exact on checkout. |
| `build-llamacpp.ps1` | Fetches the source, applies the patches, builds, and writes the output. |

Current pin: commit `390c307752ab78fd8189f359d6954c9ba1be74af` of `https://github.com/isHuangXin/llama.cpp.git` (branch `release-bitnet-embedding-0.6b-270m`, 2026-07-15). It is the submodule commit of BitNet `0b341e582afbf9e1011f24744b554c96a3477eb5`.

The fork is older than upstream llama.cpp, so upstream changes made after it are not included.

## Why the BitNet repository is not used

bitnet.cpp builds the fork as a subproject of the BitNet repository, which adds two things:

- The TL1/TL2 lookup-table kernels (`src/ggml-bitnet-lut.cpp`, `include/ggml-bitnet.h`, `include/bitnet-lut-kernels.h`). These need code generated for one model's shapes (`utils/codegen_tl*.py`, run by `setup_env.py`), so a build with them serves only that model.
- A CMake check that stops with "Clang or GCC is required".

The `I2_S` type and kernels (`ggml/src/ggml-cpu/ggml-cpu-i2s.c`, AVX2, and AVX-VNNI or AVX512-VNNI when available) are part of the fork. They need one header from the BitNet repository, `gemm-config.h` (the kernels' block sizes), which `0002-bitnet-i2s.patch` (part 1) copies into the fork.

The other BitNet files are used only inside `#if defined(GGML_BITNET_ARM_TL1) || defined(GGML_BITNET_X86_TL2)` in the fork's code. Only the fork's CMake refers to them unconditionally, and `0002-bitnet-i2s.patch` (part 1) changes that. So with TL1/TL2 off, the fork builds on its own, with MSVC like upstream llama.cpp.

## Build

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File native\llamacpp\build-llamacpp.ps1 `
    [-Arch x64|arm64] [-OutDir <dir>] [-WorkDir <dir>] [-Source auto|git|archive] [-LlvmDir <dir>] [-InstallLlvm] [-Force]
```

Requirements:

- **Both architectures:** git and Visual Studio (MSVC) with the C++ workload.
- **x64:** CMake that knows the installed Visual Studio version. For example, CMake 3.29 has no generator for Visual Studio 2026; use the CMake that comes with Visual Studio.
- **arm64**, cross-compiled on an x64 PC:
  - LLVM (`choco install llvm`), because ggml does not support MSVC on ARM.
  - The Visual Studio component "MSVC ARM64/ARM64EC build tools" (`Microsoft.VisualStudio.Component.VC.Tools.ARM64`). The script checks for `VC\Tools\MSVC\<version>\lib\arm64\msvcrt.lib`, because the `lib\arm64` folder can exist with only the AddressSanitizer libraries.
  - The "C++ CMake tools for Windows" component (CMake and Ninja).

### Architectures

| | x64 | arm64 |
| --- | --- | --- |
| Compiler | MSVC, Visual Studio generator | clang (LLVM) with the fork's `cmake\arm64-windows-llvm.cmake` toolchain, Ninja |
| Environment | Visual Studio default | Visual Studio `x64_arm64` developer environment (ARM64 libraries, x64-hosted ARM64 linker); the MSVC version with the ARM64 C runtime is passed to `vcvarsall.bat` |
| CPU target | every x86-64 variant (`GGML_CPU_ALL_VARIANTS`), chosen at run time | `-march=armv8.7-a`, one backend `ggml-cpu.dll` (`GGML_CPU_ALL_VARIANTS` is not supported for ARM on Windows) |
| OpenMP | on (`VCOMP140.DLL` from the VC++ redistributable) | off: with clang it needs `libomp140.aarch64.dll`, which Visual Studio ships only as `debug_nonredist`; ggml uses its own thread pool |
| Host tool (`llama-ui-embed`) | built as part of the build | built for x64 with `host-cxx.cmd` in the work folder (`HOST_CXX_COMPILER`), see below |

- `-InstallLlvm` runs `choco install llvm -y` when an arm64 build is needed and clang is missing.
- For arm64, the script puts LLVM and Visual Studio's CMake and Ninja first on `PATH`, ahead of other copies (for example, Strawberry Perl's CMake 3.29).
- When cross-compiling, `tools/ui` builds `llama-ui-embed` for the host (x64) with the first `g++` or `clang++` on `PATH`, passing only `-O2 -std=c++17 -o`.
  - In the `x64_arm64` environment, `LIB` points to the ARM64 libraries, so `clang++` failed to link it on AppVeyor (`LNK2019`, `LNK4272`). Locally, Strawberry Perl's `g++` was found first and hid the problem.
  - The script writes `host-cxx.cmd`, which sets `LIB` to the x64 libraries (the `LIB` entries with `arm64` replaced by `x64`) and calls `clang++ --target=x86_64-pc-windows-msvc`. It passes the wrapper as `HOST_CXX_COMPILER`, so every machine uses the same compiler.
- `GGML_CCACHE=OFF` for both, so that a `ccache` on `PATH` is not used.
- `-march=armv8.7-a` is the fork's default for Windows ARM64; it targets Snapdragon X class CPUs. Older ARM64 PCs (for example, Snapdragon 8cx Gen 1-3) may not run the build.

### Source

- `-Source auto` (the default) fetches `commit.txt` with git and checks that `HEAD` is that commit. If git fails, it falls back to the archive.
- `-Source git` and `-Source archive` force one method.
- The archive is used only when all of the following are true:
  - its URL names the pinned commit,
  - its SHA-256 matches `source-archive.txt`,
  - every entry is inside `llama.cpp-<commit>/`.
- The top-level folder of the archive is stripped when it is extracted. Entries that would land outside the work folder are rejected.
- The archive in `source-archive.txt` was compared file by file with a git checkout of the same commit.
  - All 3059 regular files are identical.
  - The 6 symbolic links (Swift package headers under `spm-headers` and `spm-sources`) are written as small text files holding the link target, as git does with `core.symlinks=false`. The build does not use them.
  - After the patches, the sources from git and from the archive are identical.
- The deepest Web UI paths are 161 characters below the source folder.
  - Git checkouts use `core.longpaths=true`.
  - The script extracts the archive and deletes old work folders with the `\\?\` prefix, so the `MAX_PATH` limit does not apply.
- The source is checked out with `core.autocrlf=false` so that the LF patches apply. This matters on AppVeyor, which sets `core.autocrlf=true` globally.
- `git apply` runs with `-c core.autocrlf=false`. Outside a repository (archive source) it otherwise follows the global setting and writes the patched files with CRLF.
- An archive source has no `.git` folder, so the script passes `LLAMA_BUILD_COMMIT` and `--version` still reports the commit.

### Configuration

- `GGML_BACKEND_DL=ON`, and for x64 `GGML_CPU_ALL_VARIANTS=ON`, as in the official package. The fork compiles the `I2_S` kernels into every CPU variant.
- `GGML_BITNET_ARM_TL1=OFF` and `GGML_BITNET_X86_TL2=OFF` (the defaults), stated explicitly: the TL kernels need the BitNet repository.
- Tests, examples, and the unified `llama` app are not built.
- `LLAMA_OPENSSL=OFF`. The tools only need HTTP on localhost.
- `LLAMA_WIN_ICON` is set to `app\favicon.ico` and `LLAMA_WIN_DESCRIPTION` to `llama.cpp built for WelsonJS`, so the executables show the WelsonJS icon and version information. The options come from a patch; see [0003-windows-build.patch, part 1](#part-1-windows-icon-and-version-information).
- Nothing is downloaded during the build: `LLAMA_USE_PREBUILT_UI=OFF` and `LLAMA_BUILD_UI=OFF`. By default the build downloads the server Web UI from a floating Hugging Face location, and the launcher does not use the Web UI.

### Output and skipping the build

The files are written to `<OutDir>\<arch>`; in CI this is `artifacts\llamacpp\x64` and `artifacts\llamacpp\arm64`. No archive is created. The folder also contains:

- `LICENSE-llama.cpp.txt` (MIT).
- `BUILDINFO.txt`: the version, commit, source used (git, or the archive URL and hash), compiler, CPU backends, BitNet kernels, patches, and the build ID.
- `SHA256SUMS.txt`: one `<sha256>  <file name>` line for every file.

The build ID is a SHA-256 over these inputs:

- `version.txt`, `commit.txt`, and the architecture,
- the name and bytes of every patch,
- the bytes of `app\favicon.ico`, the icon embedded in the executables,
- the bytes of the script itself, which contains the CMake options.

The script skips the build when `BUILDINFO.txt` has the same build ID and every file matches `SHA256SUMS.txt`. Otherwise, for example when an input changed or a file was modified, it builds again. `-Force` always builds.

The following are not part of the build ID:

- `source-archive.txt`, because both sources contain the same commit.
- The compiler version. A newer MSVC on the build image does not trigger a rebuild; use `-Force` for that.

## CI (AppVeyor)

llama.cpp is built in the **Release** job, so its files are packaged and signed with the other artifacts. No separate job is used, because AppVeyor jobs do not share artifacts: a separate job's files would not reach `artifacts.zip` or the SignPath signing request.

- **First step.** The build is the first `before_build` step of the Release job. The Debug job skips it, so only the Release job's `artifacts.zip` contains `llamacpp`. A job has 60 minutes, and building first lets the llama.cpp build finish as early as possible.
- **stderr of native tools.** AppVeyor's `ps:` steps capture stderr, and with `$ErrorActionPreference = "Stop"` each stderr line of a native command becomes a terminating error. CMake, git, Ninja, and `vcvarsall.bat` write warnings and progress there. For example, CMake warns about an unused `CMAKE_BUILD_TYPE`, and `vcvarsall.bat` prints `'vswhere.exe' is not recognized`. So the script prints stderr like stdout (`Invoke-Checked`, `Get-NativeOutput`) and decides by exit codes only. `CMAKE_BUILD_TYPE` is passed only to single-configuration generators (Ninja), not to the Visual Studio generator.
- **Both architectures.** The step runs the script for `x64`, then `arm64`, each with `-InstallLlvm`. LLVM is installed only when the arm64 build is needed and clang is missing. The arm64 build also needs the MSVC ARM64 build tools on the image; without them, the script stops with a message that names the component.
- **Output: `artifacts\llamacpp\x64` and `artifacts\llamacpp\arm64`.** The `7z a artifacts.zip artifacts\*` step adds it to `artifacts.zip`, which the deployment sends to SignPath. The SignPath artifact configuration (`.artifact-configuration.xml`) decides which of these files are signed.
  - It signs the three executables and every DLL of both architectures. The x64 CPU backends are matched with `ggml-cpu-*.dll`, because the set of CPU variants depends on the compiler. The arm64 build has one backend, `ggml-cpu.dll`.
  - Authenticode signing changes the files. In the signed package, `SHA256SUMS.txt` still lists the hashes of the unsigned build output.
- **Cache: `artifacts\llamacpp`.** It is invalidated when `commit.txt`, `version.txt`, the script, a patch file, or `app\favicon.ico` changes.
  - AppVeyor keeps a separate cache for each job; only the Release job writes this folder.
  - AppVeyor re-uploads the cache only when the folder contents change.
  - A patch file that is added later is not in the cache dependency list. The script's build ID still covers it.
- **`APPVEYOR_SAVE_CACHE_ON_ERROR=true` (Release).** AppVeyor normally saves the cache only for successful jobs. With this setting, a llama.cpp build is kept even if a later step fails (for example, ISCC or the deployment). A partly written output is never reused: `SHA256SUMS.txt` is written last, and the script checks it.
- **Existing folder.** The cache restores `artifacts\llamacpp` before the build, so the later `mkdir artifacts` step creates the folder only if it does not exist.
- **Image.** The Release job runs on the Visual Studio 2019 image (MSVC 19.29).

A build without the cache takes much longer on the hosted VM than on a developer PC. In the first successful run (build 0.2.7.build2427), x64 took 20.2 minutes and arm64 9.1 minutes, and the whole Release job 36 minutes of its 60. With a valid cache both builds are skipped. If the Release job comes close to 60 minutes, reduce the x64 CPU variants or build the package elsewhere.

## Installation

The installer uses this build; it no longer downloads the official llama.cpp package (`data\DownloadUrls.psd1` has no `llamacpp` entry).

- The `llamacpp` component (and every GGUF model component, which selects it) also selects `artifacts`, because the build is part of the artifacts package.
- The build is inside the artifacts package (`artifacts.zip`, folder `llamacpp\<arch>`) so that it is signed with the other binaries.
- `postInstall.ps1` (`Install-ArtifactsPackage`) extracts the package to a staging folder.
  - It copies `llamacpp\<arch>` to `%APPDATA%\WelsonJS\llamacpp`, after removing the files of a previous installation. That is the only place the launcher runs `llama-server.exe` from. The folder is added to the user's `PATH` as before.
  - Everything else goes to `%APPDATA%\WelsonJS\bin`, as before. The llama.cpp build is not left in `bin`.
- ARM64 installations download the same artifacts package (`data\DownloadUrls.psd1`) and get the `arm64` build. The x86 WelsonJS binaries in the package run under x86 emulation. If a package has no `arm64` build, the script falls back to `x64`, which runs under x64 emulation on Windows 11.
- 32-bit Windows gets no llama.cpp, as before.

## Compatibility with upstream llama.cpp

The fork numbers some ggml tensor types differently from upstream llama.cpp:

| ggml tensor type | This build (fork) | Upstream llama.cpp |
| --- | --- | --- |
| 36 | `I2_S` (BitNet) | unused |
| 37 | `I8_S` (BitNet, run time only) | unused |
| 38 | `TL1` (BitNet) | unused |
| 42 | `TL2` (BitNet), or `STQ1_0` in Tencent's files (detected by size, `0001-stq1_0.patch` part 3) | `Q2_0` |
| 43 | `STQ1_0` (`0001-stq1_0.patch`, the PR's ID) | `F8_E4M3` |

- BitNet `I2_S` GGUF files and STQ1_0 GGUF files (Tencent's, with type 42, and the PR's, with type 43) load with this build and not with upstream llama.cpp.
- Upstream GGUF files that store `Q2_0` (42) or `F8_E4M3` (43) tensors are **not** compatible: this build reads those IDs as `TL2`/`STQ1_0` and `STQ1_0`. The types have different sizes, so such files are expected to fail the GGUF loader's size checks rather than load with wrong data; this was not tested.
- BitNet `TL2` tensors keep their ID, but this build has no TL2 kernels to run them.
- The models WelsonJS installs (Hy-MT2, FrogNano, and Qwen3Guard, all Q4_K_M) use only tensor types 0, 12, and 14 (F32, Q4_K, Q6_K), and their architectures (`hunyuan-dense`, `qwen35`, `qwen3`) are in the fork.

## BitNet (I2_S) models

`microsoft/BitNet-b1.58-2B-4T-gguf` works through `llama-server`'s chat API with no extra options. That depends on parts 4-7 of `0002-bitnet-i2s.patch`, which fix four problems found while testing it:

| Problem without the patch | Cause | Part |
| --- | --- | --- |
| Answers the first sentence, then repeats itself until the token limit | The fork's FFN uses SiLU for the 2B-4T model, which uses squared ReLU | 5 |
| Only `?` tokens on CPUs without AVX2 (the `x64`, `sse42`, and `sandybridge` variants) | The fork's non-AVX2 I2_S fallbacks compute something else than its AVX2 code | 4 |
| Invents a conversation through `/v1/chat/completions` | The GGUF file embeds a chat template the model does not follow | 6 |
| Warning "GENERATION QUALITY WILL BE DEGRADED"; some text tokenized differently | The GGUF file has no `tokenizer.ggml.pre` | 7 |

The I2_S kernels themselves were correct. A GGUF with the I2_S weights converted to F16 generated the same tokens as the I2_S file.

`general.file_type` of the GGUF file is 40, which is `Q1_0` in the fork. `0001-stq1_0.patch` (part 3) reports it as `I2_S`, because the file has I2_S tensors and no Q1_0 tensors. The file type is metadata only and does not affect loading.

## Test results

### AppVeyor build

The `llamacpp\x64` files of `artifacts.zip` from the Release job of build 0.2.7.build2427 (MSVC 19.29; commit `c68128af`, before signing) were tested on a 12-core x64 PC:

- `SHA256SUMS.txt` matches every file of both architectures (x64 22 files, arm64 14).
- `llama-cli` with each of three CPU variants. Only the best variant for the PC was kept, or only `ggml-cpu-sse42.dll`, or only `ggml-cpu-x64.dll`:

  | Variant | BitNet b1.58 2B-4T (I2_S) | Hy-MT2 1.25-bit (STQ1_0), into Korean |
  | --- | --- | --- |
  | best (AVX2) | `The capital of France is Paris.` (2.6 s) | 오늘 날씨가 정말 좋으니 공원에서 산책을 하러 가자. (2.8 s) |
  | `sse42` | same (4.9 s) | same (4.8 s) |
  | `x64` (generic code) | same (4.3 s) | same (43.8 s) |

  - BitNet loaded 210 `I2_S` tensors and stopped with `<|eot_id|>` (token 128009) on every variant.
  - Hy-MT2 1.25-bit read its 224 type-42 tensors as STQ1_0. It also translated Korean into English correctly.
- `llama-server` chat API (best variant): BitNet 25 tokens/s, Hy-MT2 1.25-bit 58 tokens/s.
- The arm64 files could not be run on this PC.

### Local build

Built with MSVC 19.50 (Visual Studio 2026) on a 12-core PC, with all patches.

`llama-server` loaded each model and answered "What is the capital of France? Answer in one short sentence." through `/v1/chat/completions` (`temperature` 0, no template or tokenizer options). The CPU variant chosen for that PC has AVX2 and AVX-VNNI.

| Model | Answer | Generation speed |
| --- | --- | --- |
| Qwen3Guard-Gen 0.6B Q4_K_M | `Safety: Safe Categories: None` (its classifier output) | 71 tokens/s |
| Hy-MT2 1.8B Q4_K_M | `Paris is the capital of France.` | 32 tokens/s |
| FrogNano 4B 2609 Q4_K_M | `The capital of France is Paris.`, after 182 tokens of reasoning (`reasoning_content`) | 10 tokens/s |
| BitNet b1.58 2B-4T (I2_S) | `The capital of France is Paris.`, then `<|eot_id|>` | 24 tokens/s |

Hy-MT2 (translation) with the README's default prompt ("Translate the following text into {target_lang}. Note that you should only output the translated result without any additional explanation:"), `temperature` 0. Every file translated these three texts and stopped by itself:

| Text | Into |
| --- | --- |
| The weather is really nice today, so let's go for a walk in the park. | Korean |
| 오늘 회의는 오후 세 시로 변경되었습니다. 늦지 않게 참석해 주세요. | English |
| 今天天气真好，我们去公园散步吧。 | English |

| File | Example (into Korean) | Generation speed |
| --- | --- | --- |
| `tencent/Hy-MT2-1.8B-1.25Bit-GGUF` (STQ1_0) | 오늘 날씨가 정말 좋으니 공원에서 산책을 하러 가자. | 28-30 tokens/s |
| `tencent/Hy-MT2-1.8B-GGUF` Q4_K_M | 오늘 날씨가 정말 좋으니, 공원에서 산책하러 가요. | 27 tokens/s |
| `tencent/Hy-MT2-1.8B-GGUF` Q6_K | 오늘 날씨가 정말 좋으니, 공원에서 산책을 하러 가보죠. | 23 tokens/s |
| `tencent/Hy-MT2-1.8B-GGUF` Q8_0 | 오늘 날씨가 정말 좋으니, 공원에서 산책을 하러 가보죠. | 18 tokens/s |

Also checked:

- **BitNet on the `x64` variant.** With only `ggml-cpu-x64.dll` (no AVX2), the BitNet model generated the same tokens as on the AVX2 variant, ending with `<|eot_id|>`.
- **BitNet tokenization.** Five texts (contractions, numbers, whitespace, Korean) gave the same tokens as with `--override-kv tokenizer.ggml.pre=str:llama-bpe`.
- **Hy-MT2 1.25-bit on each kind of CPU variant.** It gave the same translations on every variant tested:
  - AVX2 variant: 28-30 tokens/s,
  - `sandybridge` alone: 23-24 tokens/s,
  - `sse42` alone: 10 tokens/s,
  - `x64` alone (generic code): 2 tokens/s.
- **STQ1_0 type IDs.** The 1.25-bit file with its STQ1_0 tensors renumbered from 42 to 43 (the PR's numbering) gave the same translations as Tencent's file. A synthetic file with a TL2-sized type-42 tensor kept the tensor as `tl2`.
- **`llama-cli`.** The downloaded files were also run directly with `llama-cli -st --temp 0` (single turn): every Hy-MT2 file gave the translations above in both directions, and the BitNet model answered the capital question and a two-sentence explanation, stopping by itself.
- **arm64 build.** Cross-compiled on the same PC with clang 23.1.3 and MSVC 14.51 (ARM64 build tools) in about 2.5 minutes. All 12 executables and DLLs are ARM64 (PE machine `0xAA64`), and they import only system DLLs, the VC++/UCRT runtime, and each other. They contain the icon. They could not be run here (x64 PC).
- **Other tools.** `llama-quantize --help` lists STQ1_0 (44). `llama-server.exe`, `llama-cli.exe`, and `llama-quantize.exe` contain the icon.

## Patches

The patches are applied in name order. Each one groups related changes as numbered parts; the header of each patch file describes every part.

| File | Parts |
| --- | --- |
| `0001-stq1_0.patch` | 1. STQ1_0 (PR #22836), 2. x86 SIMD vec_dot for STQ1_0, 3. STQ1_0 files with type 42 |
| `0002-bitnet-i2s.patch` | 1. build without the BitNet repository, 2. I2_S reference quantization in ggml-base, 3. I2_S fast path without llamafile, 4. non-AVX2 I2_S fallbacks, 5. squared ReLU, 6. chat template, 7. pre-tokenizer (5-7 for BitNet b1.58 2B-4T), 8. I2_S type traits |
| `0003-windows-build.patch` | 1. Windows icon and version information, 2. unsupported MSVC options, 3. MSVC conversion warnings, 4. jinja with the Visual Studio 2019 STL |

## 0001-stq1_0.patch

### Part 1: STQ1_0 (PR #22836)

This part adds the STQ1_0 ternary quantization type (1.3125 bits per weight) with an ARM NEON `vec_dot` kernel. It comes from [ggml-org/llama.cpp#22836](https://github.com/ggml-org/llama.cpp/pull/22836), PR head `1e411d8f5a1e23525fa3265dfb4bd76265465397`. The PR is open and not merged upstream. STQ1_0 is the llama.cpp format of Tencent's AngelSlim 1.25-bit quantization; the Hy-MT2 1.25-bit model card names this PR as the kernel it needs.

The PR was written against upstream llama.cpp, so it was rebased onto the fork. The differences from the PR are:

#### Type IDs

llama.cpp numbers types in two separate enums:

- **ggml tensor type** (`enum ggml_type`, `ggml.h`). It is stored for each tensor in a GGUF file, and the loader uses it to read the tensor data.
- **llama file type** (`enum llama_ftype`, `llama.h`). It is stored once in the `general.file_type` metadata, describes the file's overall quantization, and is the number shown by `llama-quantize --help`.

The fork's IDs are kept. STQ1_0 takes the next free IDs:

- The fork uses ggml types 36-38 and 42 for `I2_S`, `I8_S`, `TL1`, and `TL2`, and llama file types 41-43 for `I2_S`, `TL1`, and `TL2`. It has neither `Q2_0` nor `F8_E4M3`.

| Type | ggml tensor type | llama file type |
| --- | --- | --- |
| `STQ1_0` here | 43 (same as the PR) | **44** (the PR uses 42, which is `TL1` in the fork) |
| `GGML_TYPE_COUNT` | 44 | |

The same change is made in `gguf-py/gguf/constants.py`. Every type table in the code is indexed by enum name (designated initializers), so only the enum definitions use the numbers.

Tencent's files use other IDs:

- `tencent/Hy-MT2-1.8B-1.25Bit-GGUF` stores its STQ1_0 tensors as ggml type **42** (42 bytes per 256 weights, measured from the tensor offsets), and `general.file_type` **41**.
- Here 42 is `TL2` and 41 is `I2_S`. Part 3 tells them apart by the tensor sizes. Without it, the file fails to load ("failed to load model").

Compatibility:

- GGUF files quantized to STQ1_0 with the PR (type 43) and Tencent's files (type 42) both load with this build.
- When selecting a type by number in `llama-quantize`, 44 means STQ1_0 here. Use the type names instead.
- If the PR is merged upstream with different IDs, STQ1_0 files from this build will not match the official builds.

#### Other adjustments

- The fork has no `Q2_0` entries, so the context lines were adjusted and the STQ1_0 lines follow the `TQ2_0` lines.
- `ggml/src/ggml-cpu/arch-fallback.h`: the fork's x86 section already has an empty `quants.c` block, which receives the STQ1_0 generic mapping.
- `gguf-py/gguf/constants.py`: the fork defines `LlamaFileType` twice (the second definition wins). `MOSTLY_STQ1_0` is added to both.

The PR's fast kernel is ARM NEON only; part 2 adds one for x86.

### Part 2: x86 SIMD vec_dot for STQ1_0

This part adds `ggml_vec_dot_stq1_0_q8_K` to `ggml/src/ggml-cpu/arch/x86/quants.c`.

- Without it, x86 runs the generic code, which decodes one weight at a time. The 1.25-bit Hy-MT2 model generated about 2 tokens/s with it, against 27 tokens/s for the Q4_K_M file of the same model.
- The kernel is used on CPUs with SSE4.1 or AVX: the `sse42`, `sandybridge`, and newer variants. `GGML_SSE42` is also tested, because MSVC does not define `__SSE4_1__`. Other CPUs (the `x64` variant) still use the generic code.
- The patch removes the x86 line of `arch-fallback.h` that part 1 adds, which mapped the generic function to this name.

How it works:

1. In each 64-weight chunk, group `g` (0-15) holds weights `g + p*16` (`p` = 0-3). So lane `g` of a 16-byte vector is group `g`.
2. The kernel builds the 16 five-bit codebook indices of a chunk (4-bit code and sign bit). It looks them up with two `_mm_shuffle_epi8`, one per codebook half, and picks between them by the sign bit.
3. It multiplies each 2-bit field with 16 activations using `_mm_maddubs_epi16`.
4. The codes are 0/1/2 for -1/0/+1, so the result is `sum(q * y) - sum(y)`, with `sum(y)` from the q8_K `bsums`.

The arithmetic is integer, so the result equals the generic code's. The translations were the same on the AVX2, `sandybridge`, `sse42`, and `x64` variants.

### Part 3: STQ1_0 files with type 42

This part lets the build read STQ1_0 files that use type 42, without changing any type ID.

ggml type 42 has two meanings:

- `GGML_TYPE_TL2` in the fork (bitnet.cpp's lookup-table type),
- STQ1_0 in Tencent's files (`tencent/Hy-MT2-1.8B-1.25Bit-GGUF`).

This build keeps the fork's `TL2` = 42 and numbers STQ1_0 43, as the PR does. So files that follow either numbering keep their meaning.

**Tensor type (`ggml/src/gguf.cpp`).** The two formats have different sizes, and a GGUF file's tensor offsets must match the tensor sizes; `gguf_init_from_file_impl` checks that right after reading the tensor infos. Before that check, the patch looks at files that have type-42 tensors:

- If the offsets match the sizes as `TL2`, nothing changes.
- If they match only as STQ1_0, the type-42 tensors are read as STQ1_0 (43). The log then shows `N tensors of type 42 have the size of stq1_0, not tl2; reading them as stq1_0`.
- If they match neither, the existing offset check fails as before.

**File type (`src/llama-model-loader.cpp`).** `general.file_type` has the same problem with two numbers, so the loader goes by the tensor types:

| `general.file_type` | Meaning here | Also used by | Reported as |
| --- | --- | --- | --- |
| 41 | `MOSTLY_I2_S` | Tencent's STQ1_0 files | `MOSTLY_STQ1_0`, when the file has STQ1_0 tensors and no I2_S tensors |
| 40 | `MOSTLY_Q1_0` | `microsoft/BitNet-b1.58-2B-4T-gguf` | `MOSTLY_I2_S`, when the file has I2_S tensors and no Q1_0 tensors |

The file type is metadata only.

Tested:

- Tencent's file is read as STQ1_0 and shows the file type `STQ1_0 - 1.31 bpw ternary`.
- The BitNet file shows the file type `I2_S` (before the patch: `Q1_0`).
- The same file with the tensors renumbered to 43 loads without the detection and gives the same translations.
- A synthetic file with a TL2-sized type-42 tensor keeps the tensor as `tl2`.

## 0002-bitnet-i2s.patch

### Part 1: build without the BitNet repository

This part removes the fork's dependency on the BitNet repository when TL1/TL2 are off:

- **CMake paths.** `ggml/src/CMakeLists.txt` adds `../../../../include` (BitNet's headers) to `ggml-base` only when `GGML_BITNET_ARM_TL1` or `GGML_BITNET_X86_TL2` is on. `ggml/src/ggml-cpu/CMakeLists.txt` does the same for `../../../../src/ggml-bitnet-lut.cpp` and `../../../../include` in every CPU variant.
  - Without the patch, the fork's CMake refers to these paths, which are in the BitNet repository, even when TL1/TL2 are off. With TL1/TL2 off, the source file compiles to nothing and the headers are not included.
- **`gemm-config.h`.** The I2_S kernels include it, so the patch adds `ggml/src/ggml-cpu/gemm-config.h`, next to `ggml-cpu-i2s.c`.
  - It is BitNet's `include/gemm-config.h` at `0b341e5` (MIT, Copyright (c) Microsoft Corporation), with a fallback added at the end.
  - The header sets `ROW_BLOCK_SIZE` and `COL_BLOCK_SIZE` only for CPUs with AVX/SSSE3 (or NEON). The non-AVX2 path of `ggml-cpu-i2s.c` uses them, so the baseline `x64` variant of `GGML_CPU_ALL_VARIANTS`, and every MSVC variant below AVX (MSVC does not define `__SSSE3__`), would not compile. The fallback uses the x86 values.
  - BitNet's `utils/tune_gemm_config.py` rewrites this file to tune the block sizes; this build uses the values as published.

When the fork is built inside the BitNet repository with TL1 or TL2 on, the CMake change does nothing, and the copied header takes the place of BitNet's for `ggml-cpu-i2s.c` (same values).

### Part 2: I2_S reference quantization in ggml-base

This part moves `dequantize_row_i2_s` and `quantize_i2_s` from `ggml/src/ggml-cpu/quants.c` (the CPU backend) to `ggml/src/ggml-quants.c` (`ggml-base`), and declares them `GGML_API`.

- `ggml.c`, which is part of `ggml-base`, uses `quantize_i2_s` in `ggml_quantize_chunk`. In the fork it also registered `dequantize_row_i2_s` as the I2_S `to_float`, which part 8 removes.
- The fork defines them in the CPU backend. That links in a static build, and in shared builds on Linux, where undefined symbols are resolved when the library is loaded.
- With `GGML_BACKEND_DL` on Windows, each CPU variant is a separate DLL, and `ggml-base.dll` cannot link against it. Without the patch, the MSVC build fails with `LNK2001`/`LNK2019` for these two symbols.

Both functions are plain scalar C, so the code is unchanged. The CPU backend (`ops.cpp` uses `dequantize_row_i2_s`) imports them from `ggml-base`, like the other reference quantization functions.

### Part 3: I2_S fast path without llamafile

This part makes the fork's I2_S GEMV/GEMM fast path in `ggml_compute_forward_mul_mat` call `ggml_is_contiguous(src1)` instead of using `src1_cont`.

- `src1_cont` is declared only inside the `#if GGML_USE_LLAMAFILE` block above it.
- `ggml-cpu.c` undefines `GGML_USE_LLAMAFILE` when the target has `__ARM_FEATURE_SVE` or `__ARM_FEATURE_MATMUL_INT8`. The ARM64 build (`-march=armv8.7-a`) failed with "use of undeclared identifier 'src1_cont'".
- x86 builds do not see it because `GGML_USE_LLAMAFILE` stays defined there.

### Part 4: non-AVX2 I2_S fallbacks

This part fixes the code that the `x64`, `sse42`, and `sandybridge` CPU variants (and any CPU without AVX2) use for I2_S. Without it, the 2B-4T model generated only `?` tokens on those variants.

- **`ggml_vec_dot_i2_i8_s`** (`ggml-cpu/quants.c`). The scalar fallback differed from the AVX2 code in three ways:
  - It read the 2-bit codes sequentially. I2_S interleaves 128 weights in 32 bytes: byte `gp` holds weights `gp`, `gp+32`, `gp+64`, and `gp+96` in bits 7-6, 5-4, 3-2, and 1-0, as `dequantize_row_i2_s` reads them.
  - It mapped the codes to -1/0/+1. The AVX2 code uses the unsigned codes 0/1/2, and the callers subtract the activation sum.
  - It addressed row `r` at `r * (n/4)` instead of `r * bx / 4`.
  
  The patch rewrites it to compute what the AVX2 code computes.
- **`ggml_gemm_i2_i8_s`** (`ggml-cpu/ggml-cpu-i2s.c`, used for prompts of four or more tokens). With `ACT_PARALLEL`, which `gemm-config.h` sets, the fallback called `ggml_vec_dot_i2_i8_s` as if that function iterated over activation columns with an output stride. It iterates over weight rows and writes them consecutively. The patch calls it once per activation column, which gives the AVX2 code's layout `s[col * bs + row]`.

With the patch, the `x64` variant generates the same tokens as the AVX2 variants.

### Part 5: squared ReLU for BitNet b1.58 2B-4T

This part makes the `bitnet-b1.58` architecture use `LLM_FFN_RELU_SQR` in its feed-forward network.

- The fork builds `bitnet-b1.58` (the architecture of `microsoft/BitNet-b1.58-2B-4T-gguf`) with the `bitnet` graph, whose FFN uses SiLU.
- The 2B-4T model uses squared ReLU: `hidden_act` is `relu2` in `config.json` of `microsoft/bitnet-b1.58-2B-4T`.
- The older `bitnet_b1_58` models (`bitnet`) keep SiLU.

How the cause was found:

- With SiLU, the model answered the first sentence and then never emitted `<|eot_id|>`, repeating itself until the token limit.
- A GGUF with the I2_S weights converted to F16 generated exactly the same tokens through the F16 kernels. So the I2_S kernels were not the cause.
- With squared ReLU, the model answers and stops with `<|eot_id|>`.

### Part 6: chat template for BitNet b1.58 2B-4T

This part replaces the chat template of `microsoft/BitNet-b1.58-2B-4T-gguf` with that of the original model.

- The GGUF file embeds `Human: ...\n\nBITNETAssistant: `, which the model does not follow. Through `/v1/chat/completions` it invents a conversation instead of answering.
- The original model (`microsoft/bitnet-b1.58-2B-4T`, `tokenizer_config.json`) uses `User: ...<|eot_id|>Assistant: `.
- `llama_model_chat_template` returns the original template when the architecture is `bitnet-b1.58` and the embedded template contains `BITNETAssistant`. The function already has a one-off fix of this kind for Mistral-Small-2503.
- `--chat-template` and `--chat-template-file` still take precedence.

### Part 7: pre-tokenizer for BitNet b1.58 2B-4T

This part gives `bitnet-b1.58` the `llama-bpe` pre-tokenizer when the file has no `tokenizer.ggml.pre`.

- `microsoft/BitNet-b1.58-2B-4T-gguf` has no `tokenizer.ggml.pre`. llama.cpp then falls back to the `default` pre-tokenizer and warns that generation quality will be degraded.
- The original model uses the Llama 3 tokenizer. In a test, `default` split `I'm`, `they'll`, `won't`, and blank lines into different tokens than `llama-bpe`.
- A file that sets `tokenizer.ggml.pre`, or `--override-kv tokenizer.ggml.pre=...`, is not affected.

### Part 8: I2_S type traits

This part fixes two problems in the fork's `type_traits` table in `ggml/src/ggml.c`, which clang reports:

- **`-Winitializer-overrides`.** The entries `[36]`, `[37]`, and `[38]` (the removed `IQ4_NL_4_4`, `IQ4_NL_4_8`, and `IQ4_NL_8_8`) were initialized, then overridden by the fork's `I2_S`, `I8_S`, and `TL1`, which use the same numbers. The part removes the three placeholders.
- **`-Wcast-function-type-mismatch`.** `I2_S` registered `(ggml_to_float_t) dequantize_row_i2_s`, but `dequantize_row_i2_s` takes a fourth argument, the tensor scale.
  - The scale is stored after the packed data of the whole tensor, so it cannot be passed through `ggml_to_float_t`, which takes only a row pointer and a count. Called through that type, the scale was undefined.
  - The part sets `to_float` to `NULL`. `get_rows` and `mul_mat` handle `I2_S` themselves.
  - Code that needs `to_float` refuses the type instead of producing wrong values. For example, `llama-quantize` requantization checks for `NULL`.

## 0003-windows-build.patch

### Part 1: Windows icon and version information

This part adds two cache options to `tools/CMakeLists.txt`. llama.cpp has no Windows resource files, so its executables have no icon and no version information (the "Description" in the file properties is empty).

| Option | Set by the build script to | Effect |
| --- | --- | --- |
| `LLAMA_WIN_ICON` | `app\favicon.ico` | The icon is embedded as the application icon. |
| `LLAMA_WIN_DESCRIPTION` | `llama.cpp built for WelsonJS` | A `VERSIONINFO` resource is added. |

The `VERSIONINFO` resource of each tool has these values:

| Value | Example (`llama-server`) |
| --- | --- |
| `FileDescription` (shown as "Description") | `llama-server (llama.cpp built for WelsonJS)` |
| `ProductName` | `llama.cpp` |
| `FileVersion`, `ProductVersion` | `0.0.<LLAMA_BUILD_NUMBER>.0 (<LLAMA_BUILD_COMMIT>)` |
| `InternalName`, `OriginalFilename` | `llama-server`, `llama-server.exe` |
| `Comments` | `llama.cpp built for WelsonJS` |

How the patch does it:

- Every executable target under `tools/` gets its own resource script (`<build>/tools/win-resources/<target>.rc`). That includes `llama-server`, `llama-cli`, and `llama-quantize`. The DLLs get none.
- The icon is copied next to the scripts. The scripts start with `#pragma code_page(65001)`, so a build folder with non-ASCII characters in its path works regardless of the ANSI code page.
- `FILEOS` and `FILETYPE` use numeric values instead of the `winver.h` names, so no include path is needed, whichever resource compiler CMake picks (the x64 build uses the Windows SDK's rc.exe).
- The scripts are written through `configure_file`, which touches a file only when its content changes, so a new CMake run does not relink the tools.
- The build number is the commit count of the checkout: 1 for the shallow git clone, 0 for the archive source.

The icon file stays in the WelsonJS repository; the patch only adds the options. With both options empty (the default), the build is the same as without the patch. A missing icon file stops the configuration with an error.

### Part 2: unsupported MSVC options

This part removes two sources of MSVC warning D9002 ("ignoring unknown option") in `ggml/src/ggml-cpu/CMakeLists.txt`:

| Option | Problem | Change |
| --- | --- | --- |
| `/arch:SSE4.2` (the `sse42` CPU variant) | Exists only in newer MSVC. MSVC 19.29 (Visual Studio 2019, the AppVeyor image) ignores it for every source file of the variant. MSVC 19.50 accepts it. | Added only when `check_c_compiler_flag` accepts it; CMake counts D9002 as a failed check. |
| `-fno-lto` (the CPU feature detection objects, `ggml-cpu-*-feats`) | A GCC/clang option, ignored by every MSVC. | Passed only when not MSVC. MSVC does not use LTO unless `/GL` is given. |

Without `/arch:SSE4.2`, the `sse42` variant is compiled for SSE2 code generation. That is safe: MSVC accepts SSE4 intrinsics with any `/arch`, and the variant is chosen at run time only on CPUs with SSE4.2. It only loses auto-vectorization with SSE4 instructions on the AppVeyor image.

### Part 3: MSVC conversion warnings (C4244)

This part handles the many C4244 warnings ("possible loss of data") that MSVC reports in the `llama`, `mtmd`, and other targets. With MSVC 19.50 there are 31 such lines; MSVC 19.29 (AppVeyor) reports more.

- **Disabled like in ggml.** Almost all of them are harmless and the same in upstream llama.cpp. Examples:
  - `std::fill` of a `float` vector with `0.0` or `0` in `tools/mtmd/mtmd-audio.cpp`,
  - `std::pair<int64_t, uint32_t>` built from a small `int64_t` in `src/llama-model.cpp`.

  `ggml/CMakeLists.txt` already disables C4244, C4267, and C4305 (among others) for the ggml targets. The patch does the same in the top-level `CMakeLists.txt`, before `src`, `common`, and `tools` are added. It applies to MSVC only, so the clang (ARM64) build is not affected.
- **One real truncation fixed.** In `src/llama-quant.cpp` (`llama_tensor_dequantize_impl`), the worker lambda took the element count as `int`, but it is passed a `size_t`. The functions it calls take `int64_t`: `to_float`, `ggml_fp16_to_fp32_row`, and `ggml_bf16_to_fp32_row`.
  - A thread with more than 2^31 - 1 elements would get a wrong count. That can happen when `llama-quantize` requantizes a very large tensor with few threads.
  - The patch makes the parameter `int64_t`.

### Part 4: jinja with the Visual Studio 2019 STL

This part changes `value_func_t` in `common/jinja/value.h`, which called `std::function::target<func_hptr>()` with the function type `func_hptr`.

- With clang and the MSVC 14.29 STL (Visual Studio 2019), the ARM64 build on AppVeyor failed with `reinterpret_cast from 'const void *' to '... (*)(const jinja::func_args &)' casts away qualifiers`.
- The MSVC 14.44 and 14.51 STLs compile it, so it did not show on a PC with Visual Studio 2022 or 2026.
- The call also never matched: `std::function` stores a plain function as a function pointer, so the target type is `func_hptr *`.

The patch adds `function_pointer()`, which asks for `target<func_hptr *>()` and returns the stored function pointer (`nullptr` for another callable). `unique_hash()` and `equivalent()` use it; their comments describe comparing function pointers. Both are unused for now.

## Updating

1. Find the llama.cpp commit that the new BitNet commit uses: `git ls-tree <BitNet sha> 3rdparty/llama.cpp` and BitNet's `.gitmodules`. Set `commit.txt` to it, and `RepositoryUrl` in the script if the fork URL changed. Update `version.txt`.
2. Upload the source archive for that commit (`git archive --format=zip --prefix=llama.cpp-<sha>/ <sha>`), then update the URL and SHA-256 in `source-archive.txt`.
3. Check that each patch applies to a clean checkout with `git -c core.autocrlf=false apply --check`. Rebase the patches if needed, and check the type IDs in `ggml.h`, `llama.h`, and `gguf-py/gguf/constants.py` against the fork's types.
4. Check that the fork still keeps every use of the BitNet repository behind the TL1/TL2 options (`0002-bitnet-i2s.patch`, part 1), and that the `I2_S` sources are compiled into every CPU variant.
5. Build locally with the script, for x64 and arm64. Check that:
   - `llama-quantize --help` lists STQ1_0,
   - `llama-server` answers a chat request with one of the installed models and with an `I2_S` model.
6. Commit. The AppVeyor cache is invalidated by the changed files.
