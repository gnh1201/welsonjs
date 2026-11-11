# llama.cpp build for WelsonJS

This folder builds [llama.cpp](https://github.com/ggml-org/llama.cpp) for Windows with the patches WelsonJS needs. The output contains the same files as the official `llama-<version>-bin-win-cpu-<arch>.zip` package: `llama-server`, `llama-cli`, `llama-quantize`, and the ggml backends with every CPU variant, which are loaded at run time.

The launcher uses `llama-server` only through its OpenAI-compatible HTTP API (`/llamacpp/chat`, `lib/llamacpp.js`).

## Files

| File | Purpose |
| --- | --- |
| `commit.txt` | The llama.cpp commit that is built, as a full 40-character SHA. The source is fetched by this SHA rather than by a tag, because a tag can be moved upstream and a commit cannot. |
| `version.txt` | A label for the build, written in `git describe` form (for example `b12284-17-g956591f`). It is a label only and does not select the source. |
| `source-archive.txt` | The fallback source archive (`url=`) and its SHA-256 (`sha256=`), used when git cannot fetch the commit. |
| `patches/*.patch` | Patches applied in name order with `git apply`. They are stored with LF line endings, and `.gitattributes` keeps them byte-exact on checkout. |
| `build-llamacpp.ps1` | Fetches the source, applies the patches, builds, and writes the output. |

Current pin: commit `956591f4570aa6636895d90439892fef7c075b17`, which is master at b12284 + 17 commits. No release tag points to this commit.

## Build

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File native\llamacpp\build-llamacpp.ps1 `
    [-OutDir <dir>] [-WorkDir <dir>] [-Source auto|git|archive] [-Arch x64] [-Force]
```

Requirements: git, CMake, and Visual Studio (MSVC) with the C++ workload. A clean build took about 7 minutes on a 12-core PC.

### Source

- `-Source auto` (the default) fetches `commit.txt` with git and checks that `HEAD` is that commit. If git fails, it falls back to the archive.
- `-Source git` and `-Source archive` force one method.
- The archive is used only when all of the following are true:
  - its URL names the pinned commit,
  - its SHA-256 matches `source-archive.txt`,
  - every entry is inside `llama.cpp-<commit>/`.
- The top-level folder of the archive is stripped when it is extracted. Otherwise the deepest Web UI paths exceed the Windows `MAX_PATH` limit. Entries that would land outside the work folder are rejected.
- The archive in `source-archive.txt` was compared file by file with a git checkout of the same commit: all 4162 files are identical.
- The source is checked out with `core.autocrlf=false` so that the LF patches apply. This matters on AppVeyor, which sets `core.autocrlf=true` globally.
- An archive source has no `.git` folder, so the script passes `LLAMA_BUILD_COMMIT` and `--version` still reports the commit.

### Configuration

- `GGML_BACKEND_DL=ON` and `GGML_CPU_ALL_VARIANTS=ON`, as in the official package.
- Tests and examples are not built.
- Nothing is downloaded during the build:
  - `LLAMA_USE_PREBUILT_UI=OFF` and `LLAMA_BUILD_UI=OFF`. By default the build downloads the server Web UI from a floating Hugging Face location, and the launcher does not use the Web UI.
  - `LLAMA_CONNECT=OFF`. The llama-connect tool is a prebuilt binary.

### Output and skipping the build

The files are written to `<OutDir>\<arch>`; in CI this is `artifacts\llamacpp\x64`. No archive is created. The folder also contains:

- `BUILDINFO.txt`: the version, commit, source used (git, or the archive URL and hash), CPU backends, patches, and the build ID.
- `SHA256SUMS.txt`: one `<sha256>  <file name>` line for every file.

The build ID is a SHA-256 over these inputs:

- `version.txt`, `commit.txt`, and the architecture,
- the name and bytes of every patch,
- the bytes of the script itself, which contains the CMake options.

The script skips the build when `BUILDINFO.txt` has the same build ID and every file matches `SHA256SUMS.txt`. Otherwise, for example when an input changed or a file was modified, it builds again. `-Force` always builds.

The following are not part of the build ID:

- `source-archive.txt`, because both sources contain the same commit.
- The compiler version. A newer MSVC on the build image does not trigger a rebuild; use `-Force` for that.

## CI (AppVeyor)

llama.cpp is built in the **Release** job, so its files are packaged and signed with the other artifacts. No separate job is used, because AppVeyor jobs do not share artifacts: a separate job's files would not reach `artifacts.zip` or the SignPath signing request.

- **First step.** The build is the first `before_build` step of the Release job (the Debug job skips it). A job has 60 minutes, and building first lets the llama.cpp build finish as early as possible.
- **Output: `artifacts\llamacpp\x64`.** The `7z a artifacts.zip artifacts\*` step adds it to `artifacts.zip`, which the deployment sends to SignPath. The SignPath artifact configuration decides which of these files are signed.
- **Cache: `artifacts\llamacpp`.** It is invalidated when `commit.txt`, `version.txt`, the script, or the patch file changes.
  - AppVeyor keeps a separate cache for each job; only the Release job writes this folder.
  - AppVeyor re-uploads the cache only when the folder contents change.
  - A patch file that is added later is not in the cache dependency list. The script's build ID still covers it.
- **`APPVEYOR_SAVE_CACHE_ON_ERROR=true` (Release).** AppVeyor normally saves the cache only for successful jobs. With this setting, a llama.cpp build is kept even if a later step fails (for example, ISCC or the deployment). A partly written output is never reused: `SHA256SUMS.txt` is written last, and the script checks it.
- **Existing folder.** The cache restores `artifacts\llamacpp` before the build, so the later `mkdir artifacts` step creates the folder only if it does not exist.
- **Image.** The Release job runs on the Visual Studio 2019 image (MSVC 19.29). The build has been verified locally with MSVC 19.44 and 19.50 only.

The first build on the hosted VM is expected to take much longer than on a developer PC; the documentation lists 2-core VMs. If the Release job does not finish within 60 minutes, reduce the CPU variants or build the package elsewhere.

The installer does not use this build yet. `postInstall.ps1` still downloads the official package listed in `data\DownloadUrls.psd1`.

## Patch: STQ1_0 (PR #22836)

`patches/0001-ggml-cpu-add-STQ1_0-pr22836.patch` adds the STQ1_0 ternary quantization type (1.3125 bits per weight) with an ARM NEON `vec_dot` kernel. It comes from [ggml-org/llama.cpp#22836](https://github.com/ggml-org/llama.cpp/pull/22836), PR head `1e411d8f5a1e23525fa3265dfb4bd76265465397`. The PR is open and not merged upstream.

The PR was written against an older base, so it was rebased onto the pinned commit. The differences from the PR are:

### Type IDs

llama.cpp numbers types in two separate enums:

- **ggml tensor type** (`enum ggml_type`, `ggml.h`). It is stored for each tensor in a GGUF file, and the loader uses it to read the tensor data.
- **llama file type** (`enum llama_ftype`, `llama.h`). It is stored once in the `general.file_type` metadata, describes the file's overall quantization, and is the number shown by `llama-quantize --help`.

After the PR was written, upstream added `F8_E4M3` using the IDs the PR had chosen for STQ1_0. STQ1_0 keeps the PR's IDs, and F8_E4M3 moves to the next free IDs:

| Type | ggml tensor type | llama file type |
| --- | --- | --- |
| `STQ1_0` (same as the PR) | 43 | 42 (`LLAMA_FTYPE_MOSTLY_STQ1_0`) |
| `F8_E4M3` (upstream uses 43 / 42) | **44** | **43** |
| `GGML_TYPE_COUNT` | 45 | |

The same change is made in `gguf-py/gguf/constants.py`. Every type table in the code is indexed by enum name (designated initializers), so the swap does not change any other mapping. Only the three enum definitions use the numbers.

Compatibility:

- GGUF files quantized to STQ1_0 with the original PR load with this build.
- GGUF files that store F8_E4M3 tensors with upstream's ID 43 are **not** compatible. This build reads ID 43 as STQ1_0. The GGUF loader rejects such files: a tensor offset or row-size check fails because the two types have different sizes. They do not load silently with wrong data.
  - Upstream's converter (`conversion/base.py`) writes F8_E4M3 tensors when it keeps the weights of FP8 checkpoints, so this affects GGUF files converted from FP8 models. `llama-quantize` has no F8_E4M3 option.
- F8_E4M3 files written by this build (ID 44) cannot be loaded by upstream llama.cpp.
- When selecting a type by number in `llama-quantize`, 42 means STQ1_0 here. Use the type names instead.
- If the PR is merged upstream with different IDs, STQ1_0 files from this build will not match the official builds.

The models WelsonJS installs (Hy-MT2, FrogNano, and Qwen3Guard, all Q4_K_M) use only tensor types 0, 12, and 14 (F32, Q4_K, Q6_K). They are not affected.

### Other adjustments

- `ggml/src/ggml-cpu/arch-fallback.h`: at the pinned commit the x86 section has no `quants.c` block, so the STQ1_0 generic mapping starts one.
- The context lines in `ggml.h`, `llama.h`, and `constants.py` were adjusted for the new enum entries.

On x86, STQ1_0 runs through the generic (scalar) path. The fast kernel is ARM NEON only.

## Updating llama.cpp

1. Set `commit.txt` to the new full SHA, and `version.txt` to its `git describe` label.
2. Upload the source archive for that commit (`git archive --format=zip --prefix=llama.cpp-<sha>/ <sha>`), then update the URL and SHA-256 in `source-archive.txt`.
3. Check that each patch applies to a clean checkout with `git -c core.autocrlf=false apply --check`. Rebase the patches if needed, and check the type IDs in `ggml.h`, `llama.h`, and `gguf-py/gguf/constants.py` against new upstream types.
4. Build locally with the script. Check that `llama-quantize --help` lists STQ1_0 and that `llama-server` answers a chat request.
5. Commit. The AppVeyor cache is invalidated by the changed files.
