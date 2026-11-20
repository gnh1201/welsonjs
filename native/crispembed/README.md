# CrispEmbed build for WelsonJS

[한국어](README_KO.md)

This folder builds the Windows CrispEmbed that WelsonJS uses. [CrispEmbed](https://github.com/CrispStrobe/CrispEmbed) (MIT) runs GGUF encoder models on the CPU with ggml: text embeddings, typed decisions and other encoder tasks. The launcher runs its `crispembed-server.exe` (see [docs/crispembed-launcher-api.md](../../docs/crispembed-launcher-api.md)).

Like [native/llamacpp](../llamacpp/README.md), the build comes from a pinned source commit instead of the project's release package, so the binaries are built and signed with the rest of WelsonJS.

## Files

| File | Contents |
| --- | --- |
| `build-crispembed.ps1` | The build script. |
| `commit.txt` | The CrispEmbed commit that is built, as a full 40-character SHA. The source is fetched by this SHA rather than by a tag, because a tag can move and a commit cannot. |
| `version.txt` | The version label (`0.17.20`, CrispEmbed's `VERSION` at the commit), used in BUILDINFO.txt and the build ID only. |
| `source-archive.txt` | The fallback source archive (`url=`) and its SHA-256 (`sha256=`), used when git cannot fetch the commits. |
| `patches\*.patch` | Patches applied in name order (see [Patches](#patches)). |

Current pin: commit `ad01b21a23d884d197686aa508f78f11d1505925` of `https://github.com/CrispStrobe/CrispEmbed.git` (2026-11-18, `VERSION` 0.17.20, 343 commits after the tag `v0.17.19`). Its ggml submodule is `https://github.com/CrispStrobe/ggml.git` at the commit that CrispEmbed commit records (`3a27b6c665b191cb230be367db5c52d81dd3e2ac`); the script reads it from the tree, so it is pinned by `commit.txt` too.

## Build

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File native\crispembed\build-crispembed.ps1 [-Arch x64|x86|arm64] [-OutDir <dir>] [-WorkDir <dir>] [-LlvmDir <dir>] [-InstallLlvm] [-Force]
```

Requirements: git, and Visual Studio (or Build Tools) with the C++ workload and its CMake. The script uses Visual Studio's own CMake when it finds one, because a CMake found first on `PATH` (for example Strawberry Perl's) may choose another generator. arm64 also needs the MSVC ARM64 build tools and LLVM (clang): `-LlvmDir`, LLVM in Program Files or on `PATH`, or `-InstallLlvm` (`choco install llvm` when clang is missing).

The script:

1. fetches the pinned commit, then the ggml submodule at the commit the tree records, from the URL in `.gitmodules` (which must be the expected one),
   If git fails (`-Source auto`, the default), or with `-Source archive`, it downloads the source archive in `source-archive.txt` instead and uses it only if its SHA-256 matches,
2. applies the patches,
3. configures with MSVC: `-DGGML_NATIVE=OFF` (a fixed x86-64 baseline of SSE4.2, AVX, AVX2, FMA, F16C and BMI2, as CrispEmbed's own Windows release; with `GGML_NATIVE`, MSVC can enable AVX-512, which most consumer CPUs lack), `-DGGML_BLAS=OFF` and `-DCRISPEMBED_BUILD_SHARED=OFF` (the executables link CrispEmbed statically, so `crispembed.dll` is not built),
   `/utf-8` (the sources are UTF-8 without a BOM, which MSVC otherwise reads in the system code page; on Korean Windows that breaks the build), and the icon and version information options of `0001-windows-resources.patch`: the WelsonJS icon (`app\favicon.ico`) in the executables, and in every executable and DLL the WelsonJS version (the launcher's `AssemblyFileVersion`; `-ProductVersion` overrides it), a description that says `CrispEmbed built for WelsonJS`, `CompanyName` `Catswords Research`, and the copyright of CrispEmbed, ggml and WelsonJS,
4. builds `crispembed-server`, `crispembed-cli` (`crispembed.exe`) and `crispembed-quantize`,
5. places them in `<OutDir>\<arch>` with `ggml.dll`, `ggml-base.dll`, `ggml-cpu.dll`, the license and notice files that CrispEmbed's package ships, `BUILDINFO.txt` and `SHA256SUMS.txt`, and fails when an executable or DLL lacks the version information or is not listed for signing in `.artifact-configuration.xml`.

A build is skipped when the output was built from the same inputs (version, product version, commit, patches, the icon and the script: the "Build ID" in `BUILDINFO.txt`) and its files match `SHA256SUMS.txt`.

### Source archive

A GitHub archive of CrispEmbed does not contain the ggml submodule, so the fallback archive is made for WelsonJS and kept in the WelsonJS blob storage: the `git archive` of the CrispEmbed commit and of the ggml commit it records, in one top-level folder `CrispEmbed-<commit>` (ggml in `CrispEmbed-<commit>/ggml`):

```sh
git archive --format=zip --prefix=CrispEmbed-<commit>/ -o main.zip <commit>
git -C ggml archive --format=zip --prefix=CrispEmbed-<commit>/ggml/ -o ggml.zip HEAD
# merge the two into CrispEmbed-<commit>.zip (one entry per path), upload it to
# packages/archive/, and put its URL and SHA-256 in source-archive.txt
```

### Architectures

| `-Arch` | Compiler | CPU baseline |
| --- | --- | --- |
| `x64` (default) | MSVC, Visual Studio generator | SSE4.2, AVX, AVX2, FMA, F16C, BMI2 |
| `x86` | MSVC, Visual Studio generator (Win32) | the same without BMI2 (ggml's BMI2 code uses `_pdep_u64`, which 32-bit MSVC lacks); linked with `/LARGEADDRESSAWARE` (4 GB of address space on 64-bit Windows) |
| `arm64` | clang, cross-compiled on x64 with `cmake\arm64-windows-llvm.cmake` and Ninja in the Visual Studio `x64_arm64` environment | `-march=armv8.7-a`, as the WelsonJS llama.cpp ARM64 build; ggml's thread pool instead of OpenMP |

ggml does not support MSVC on ARM, so arm64 uses clang. CrispEmbed's `CMakeLists.txt` defines `NOMINMAX`, `WIN32_LEAN_AND_MEAN`, `_USE_MATH_DEFINES` and `_CRT_SECURE_NO_WARNINGS` for MSVC only; the toolchain file defines them for clang. The x86 build gives the same decisions as the x64 build (8 of 8 labels, probabilities within 1e-6). The arm64 build was checked for its format (ARM64) and its imports (the VC++ runtime and Windows only), not run.

## CI (AppVeyor)

The Release job runs the script for x64, arm64 and x86 after the llama.cpp build, into `artifacts\crispembed\<arch>`, and caches that folder; the cache key covers `commit.txt`, `version.txt`, the script, the patches, the arm64 toolchain file, the icon and the launcher's `AssemblyInfo.cs` (add a new patch there). The files are packaged into `artifacts.zip` and signed with the other artifacts.

## Installation

The setup component `crispembed` selects the artifacts package; `postInstall.ps1` copies `crispembed\<arch>` for the machine (on ARM64 Windows without an arm64 build, `crispembed\x64`, which runs under emulation) from it to `%APPDATA%\WelsonJS\crispembed\`, where the launcher runs `crispembed-server.exe`. The models are separate components (for example `crispembed\embeddinggemma_2_typed_decision`).

## Patches

| File | Contents |
| --- | --- |
| `0001-windows-resources.patch` | Windows icon and version information for every executable and DLL: `cmake/win-resources.cmake`, included at the end of the top-level `CMakeLists.txt`, with the cache options `CRISPEMBED_WIN_ICON`, `CRISPEMBED_WIN_DESCRIPTION`, `CRISPEMBED_WIN_VERSION`, `CRISPEMBED_WIN_COMPANY`, `CRISPEMBED_WIN_COPYRIGHT` and `CRISPEMBED_WIN_COMMIT`. The same mechanism as part 1 of llama.cpp's `0003-windows-build.patch`; with the options empty the build is unchanged. |

## Updating

1. Set `commit.txt` to the new commit (the commit itself, not a tag object) and `version.txt` to its `VERSION`.
2. If the ggml submodule URL changed, check it and update `GgmlRepositoryUrl` in the script.
3. Make the source archive for the new commit (see [Source archive](#source-archive)), upload it, and update `source-archive.txt`.
4. Check that each patch applies (`git -c core.autocrlf=false apply --check`), and add new patches to the AppVeyor cache key.
5. Build locally, also with `-Source archive`, and check that the built files are the ones listed in `.artifact-configuration.xml`, and that `crispembed-server` answers `/health` with the installed models.
6. Commit. The AppVeyor cache is invalidated by the changed files.
