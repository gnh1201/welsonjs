# ChakraCore build for WelsonJS

[한국어](README_KO.md)

This folder builds the Windows ChakraCore that WelsonJS uses. [ChakraCore](https://github.com/chakra-core/ChakraCore) (MIT) is the JavaScript engine behind the launcher's `JsCore` and `JsonCodec`, loaded from `ChakraCore.dll`.

Like [native/llamacpp](../llamacpp/README.md) and [native/crispembed](../crispembed/README.md), it is built from a pinned source commit on a developer PC, for x64, x86 and arm64, and published to the WelsonJS blob storage, from which CI restores it so that it is signed with the rest of WelsonJS.

## Files

| File | Contents |
| --- | --- |
| `build-chakracore.ps1` | The build script. |
| `commit.txt` | The ChakraCore commit that is built, as a full 40-character SHA. |
| `version.txt` | The version label, `<version>.<commit>.1` as for llama.cpp and CrispEmbed: the version the source records in `lib/Common/ChakraCoreVersion.h` (`CHAKRA_CORE_MAJOR_VERSION`, `_MINOR_`, `_PATCH_`), the first seven characters of `commit.txt`, and `.1` for the WelsonJS build (`1.13.0.97e68c9.1`). Used in BUILDINFO.txt and the build ID only. |
| `source-archive.txt` | The fallback source archive (`url=`) and its SHA-256 (`sha256=`), used when git cannot fetch the commit. |
| `patches\*.patch` | Patches applied in name order (see [Patches](#patches)). |
| `prebuilt\<arch>.sha256` | The file list and SHA-256 of the published build for each architecture (see [CI](#ci-appveyor)). |

Current pin: commit `97e68c9174209413f621deb2f963941550ef6f22` of `https://github.com/chakra-core/ChakraCore.git` (2026-02-19, the head of `master`; `ChakraCoreVersion.h` says 1.13.0, with `CHAKRA_CORE_VERSION_RELEASE` 0: not a release). The latest release tag is `v1.11.24`.

## Build

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File native\chakracore\build-chakracore.ps1 [-Arch x64|x86|arm64] [-OutDir <dir>] [-WorkDir <dir>] [-Source auto|git|archive] [-ProductVersion a.b.c.d] [-MSBuild <path>] [-Force]
```

Requirements: git and Visual Studio (or Build Tools) with the C++ workload; for arm64 also the "MSVC ARM64/ARM64EC build tools" component. The script uses the MSBuild of the newest Visual Studio that has them, or `-MSBuild`.

The script:

1. fetches the pinned commit with git; if git fails (`-Source auto`, the default), or with `-Source archive`, it downloads the source archive in `source-archive.txt` and uses it only if its SHA-256 matches,
2. applies the patches,
3. writes `bin\ChakraCore\WelsonJSVersion.h` with the version information (see below),
4. builds `bin\ChakraCore\ChakraCore.vcxproj` and the projects it references with MSBuild, `Release`, for the platform `x64`, `Win32` (x86) or `ARM64`, with `Build\` as the solution folder,
5. places `ChakraCore.dll`, `ChakraCore.lib`, the public JsRT headers (`ChakraCore.h`, `ChakraCommon.h`, `ChakraCommonWindows.h`, `ChakraCoreWindows.h`, `ChakraDebug.h`), `LICENSE-ChakraCore.txt`, `THIRD-PARTY-NOTICES-ChakraCore.txt`, `BUILDINFO.txt` and `SHA256SUMS.txt` in `<OutDir>\<arch>`, and fails when the DLL lacks the version information or is not listed for signing in `.artifact-configuration.xml`.

`ChakraCore.dll` carries:

| Field | Value |
| --- | --- |
| `FileDescription` | `ChakraCore JavaScript engine (built for WelsonJS)` |
| `FileVersion`, `ProductVersion` | the WelsonJS version (the launcher's `AssemblyFileVersion`; `-ProductVersion` overrides it) |
| `CompanyName` | `Catswords Research` |
| `LegalCopyright` | `Copyright (c) Microsoft Corporation and contributors (ChakraCore), Copyright (c) 2026 Catswords OSS (WelsonJS)` |
| `ProductName` | `ChakraCore` |
| `Comments` | `ChakraCore <version>, commit <7 characters>, built for WelsonJS` |

A build is skipped when the output was built from the same inputs (version, product version, commit, patches and the script: the "Build ID" in `BUILDINFO.txt`) and its files match `SHA256SUMS.txt`.

## Patches

| File | Contents |
| --- | --- |
| `0001-windows-resources.patch` | `bin\ChakraCore\ChakraCore.rc` includes `WelsonJSVersion.h` and, when it defines `WELSONJS_WIN_VERSION` as 1, uses a version resource made from its values instead of ChakraCore's (`..\CoreCommon.ver`). The patch adds a default `WelsonJSVersion.h` with `WELSONJS_WIN_VERSION 0`, so without the build script the build is unchanged. |

## CI (AppVeyor)

ChakraCore is not built in CI. It is built on a developer PC with this script, published to the WelsonJS blob storage, and restored by the Debug and Release jobs, which package and sign it with the other artifacts.

1. **Build** each architecture: `-Arch x64`, `-Arch x86` and `-Arch arm64`.
2. **Publish** each build: `native\publish-prebuilt.ps1 -Component chakracore -Arch <arch> -BuildDir <OutDir>\<arch>`. It checks the folder against its `SHA256SUMS.txt` and `BUILDINFO.txt`, uploads each file as it is and as `<file>.gz` with AzCopy to `packages/native/ChakraCore/<version>-<arch>/` (the folder keeps the casing of `packages/native/ChakraCore`, where Catswords.Phantomizer loads native modules from) without overwriting, and copies `SHA256SUMS.txt` to `prebuilt\<arch>.sha256`.
3. **Commit** the three `prebuilt\<arch>.sha256` files with the change.

The jobs run `native\restore-prebuilt.ps1 -Component chakracore -Arch <arch>` into `artifacts\chakracore\<arch>`. It downloads `<file>.gz` (or the file itself when the `.gz` cannot be used) and uses a file only if its SHA-256 matches `prebuilt\<arch>.sha256`. Each job caches `artifacts\chakracore`, keyed on `version.txt` and the three records, so the files are downloaded again only when a record changes. `.artifact-configuration.xml` lists `ChakraCore.dll` of each architecture for signing.

The launcher loads the signed release of this build for the architecture of its process through Catswords.Phantomizer: `packages/native/ChakraCore/<version>-signed/<arch>/ChakraCore.dll`, checked against the release's `SHA256SUMS.txt` (recorded in `prebuilt\signed.sha256`, published by `native\publish-signed.ps1`) and its Authenticode signature, as set by `ChakraCoreVersion` in the launcher's settings. welsonjs-hta still takes its `ChakraCore.dll` from the earlier prebuilt archives (`chakracore-build/<platform>_release.zip`).

## Updating

1. Set `commit.txt` to the new commit, and `version.txt` to `<major>.<minor>.<patch>` of its `ChakraCoreVersion.h`, then the first seven characters of the commit, then `.1`.
2. Upload a source archive of the commit (`git archive --format=zip --prefix=ChakraCore-<commit>/ -o ChakraCore-<commit>.zip <commit>`) to `packages/archive/`, and update `source-archive.txt`.
3. Check that each patch applies (`git -c core.autocrlf=false apply --check`).
4. Build for x64, x86 and arm64, check the version information of `ChakraCore.dll`, publish the three builds and commit with the new `prebuilt\<arch>.sha256` files. The changed files invalidate the CI cache, so CI downloads the new build.
