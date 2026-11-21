# WelsonJS용 ChakraCore 빌드

[English](README.md)

이 폴더는 WelsonJS가 쓰는 Windows용 ChakraCore를 빌드합니다. [ChakraCore](https://github.com/chakra-core/ChakraCore)(MIT)는 런처의 `JsCore`와 `JsonCodec`이 쓰는 JavaScript 엔진이며, `ChakraCore.dll`로 불러옵니다.

[native/llamacpp](../llamacpp/README_KO.md), [native/crispembed](../crispembed/README_KO.md)처럼 고정한 소스 커밋을 개발자 PC에서 x64, x86, arm64로 빌드해 WelsonJS blob 스토리지에 게시하고, CI가 이를 복원해 WelsonJS의 다른 파일과 함께 서명합니다.

## 파일

| 파일 | 내용 |
| --- | --- |
| `build-chakracore.ps1` | 빌드 스크립트 |
| `commit.txt` | 빌드할 ChakraCore 커밋(40자 SHA 전체) |
| `version.txt` | llama.cpp, CrispEmbed와 같은 형식의 버전 이름표 `<버전>.<커밋>.1`. 소스의 `lib/Common/ChakraCoreVersion.h`가 적은 버전(`CHAKRA_CORE_MAJOR_VERSION`, `_MINOR_`, `_PATCH_`), `commit.txt`의 앞 일곱 글자, WelsonJS 빌드를 뜻하는 `.1`입니다(`1.13.0.97e68c9.1`). BUILDINFO.txt와 빌드 ID에만 씁니다. |
| `source-archive.txt` | git으로 커밋을 받지 못할 때 쓰는 소스 압축 파일(`url=`)과 그 SHA-256(`sha256=`) |
| `patches\*.patch` | 이름 순서로 적용하는 패치([패치](#패치) 참고) |
| `prebuilt\<arch>.sha256` | 아키텍처마다 게시한 빌드의 파일 목록과 SHA-256([CI](#ciappveyor) 참고) |

현재 고정: `https://github.com/chakra-core/ChakraCore.git`의 커밋 `97e68c9174209413f621deb2f963941550ef6f22`(2026-02-19, `master`의 최신 커밋. `ChakraCoreVersion.h`는 1.13.0이고 `CHAKRA_CORE_VERSION_RELEASE`가 0이라 릴리스가 아님). 최신 릴리스 태그는 `v1.11.24`입니다.

## 빌드

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File native\chakracore\build-chakracore.ps1 [-Arch x64|x86|arm64] [-OutDir <폴더>] [-WorkDir <폴더>] [-Source auto|git|archive] [-ProductVersion a.b.c.d] [-MSBuild <경로>] [-Force]
```

필요한 것: git, C++ 워크로드가 있는 Visual Studio(또는 Build Tools). arm64에는 "MSVC ARM64/ARM64EC 빌드 도구" 구성 요소도 필요합니다. 스크립트는 이것들이 있는 가장 새 Visual Studio의 MSBuild를 쓰며, `-MSBuild`로 지정할 수도 있습니다.

스크립트가 하는 일:

1. 고정한 커밋을 git으로 받습니다. git이 실패하면(`-Source auto`, 기본값) 또는 `-Source archive`이면 `source-archive.txt`의 소스 압축 파일을 받아 SHA-256이 맞을 때만 씁니다.
2. 패치를 적용합니다.
3. 파일 정보(아래 참고)를 담은 `bin\ChakraCore\WelsonJSVersion.h`를 씁니다.
4. `bin\ChakraCore\ChakraCore.vcxproj`와 그것이 참조하는 프로젝트를 MSBuild로 `Release`, 플랫폼 `x64`, `Win32`(x86), `ARM64`로 빌드합니다. 솔루션 폴더는 `Build\`입니다.
5. `ChakraCore.dll`, `ChakraCore.lib`, 공개 JsRT 헤더(`ChakraCore.h`, `ChakraCommon.h`, `ChakraCommonWindows.h`, `ChakraCoreWindows.h`, `ChakraDebug.h`), `LICENSE-ChakraCore.txt`, `THIRD-PARTY-NOTICES-ChakraCore.txt`, `BUILDINFO.txt`, `SHA256SUMS.txt`를 `<OutDir>\<arch>`에 둡니다. DLL에 파일 정보가 없거나 `.artifact-configuration.xml`의 서명 목록에 없으면 실패합니다.

`ChakraCore.dll`의 파일 정보:

| 항목 | 값 |
| --- | --- |
| `FileDescription` | `ChakraCore JavaScript engine (built for WelsonJS)` |
| `FileVersion`, `ProductVersion` | WelsonJS 버전(런처의 `AssemblyFileVersion`, `-ProductVersion`으로 바꿀 수 있음) |
| `CompanyName` | `Catswords Research` |
| `LegalCopyright` | `Copyright (c) Microsoft Corporation and contributors (ChakraCore), Copyright (c) 2026 Catswords OSS (WelsonJS)` |
| `ProductName` | `ChakraCore` |
| `Comments` | `ChakraCore <버전>, commit <커밋 7자리>, built for WelsonJS` |

출력이 같은 입력(버전, 제품 버전, 커밋, 패치, 스크립트: `BUILDINFO.txt`의 "Build ID")으로 빌드되었고 파일이 `SHA256SUMS.txt`와 일치하면 빌드를 건너뜁니다.

## 패치

| 파일 | 내용 |
| --- | --- |
| `0001-windows-resources.patch` | `bin\ChakraCore\ChakraCore.rc`가 `WelsonJSVersion.h`를 포함하고, 이 파일이 `WELSONJS_WIN_VERSION`을 1로 정의하면 ChakraCore의 리소스(`..\CoreCommon.ver`) 대신 그 값으로 만든 버전 리소스를 씁니다. 패치는 `WELSONJS_WIN_VERSION 0`인 기본 `WelsonJSVersion.h`도 추가하므로, 빌드 스크립트 없이 빌드하면 결과는 바뀌지 않습니다. |

## CI(AppVeyor)

CI에서는 ChakraCore를 빌드하지 않습니다. 개발자 PC에서 이 스크립트로 빌드해 WelsonJS blob 스토리지에 게시하고, Debug와 Release 작업이 이를 복원해 다른 산출물과 함께 패키징하고 서명합니다.

1. 아키텍처마다 **빌드**합니다: `-Arch x64`, `-Arch x86`, `-Arch arm64`.
2. 빌드마다 **게시**합니다: `native\publish-prebuilt.ps1 -Component chakracore -Arch <arch> -BuildDir <OutDir>\<arch>`. 폴더를 `SHA256SUMS.txt`와 `BUILDINFO.txt`로 확인하고, 각 파일을 그대로, 그리고 `<file>.gz`로 AzCopy를 써서 `packages/native/chakracore/<version>-<arch>/`에 덮어쓰지 않고 올리며, `SHA256SUMS.txt`를 `prebuilt\<arch>.sha256`으로 복사합니다.
3. 세 `prebuilt\<arch>.sha256` 파일을 변경과 함께 **커밋**합니다.

작업은 `native\restore-prebuilt.ps1 -Component chakracore -Arch <arch>`를 실행해 `artifacts\chakracore\<arch>`에 둡니다. `<file>.gz`를 받고(`.gz`를 쓸 수 없으면 원본 파일), SHA-256이 `prebuilt\<arch>.sha256`과 맞는 파일만 씁니다. 각 작업은 `version.txt`와 세 기록을 키로 `artifacts\chakracore`를 캐시하므로, 기록이 바뀔 때만 파일을 다시 받습니다. `.artifact-configuration.xml`은 아키텍처마다 `ChakraCore.dll`을 서명 목록에 둡니다.

런처와 welsonjs-hta는 아직 예전 미리 빌드한 압축 파일(`chakracore-build/<platform>_release.zip`)의 `ChakraCore.dll`을 씁니다. 이 빌드는 그와 별도로 `artifacts\chakracore\<arch>`에 복원되고 서명됩니다.

## 업데이트 방법

1. `commit.txt`를 새 커밋으로, `version.txt`를 그 커밋의 `ChakraCoreVersion.h`에 적힌 `<major>.<minor>.<patch>`, 커밋 앞 일곱 글자, `.1` 순서로 바꿉니다.
2. 그 커밋의 소스 압축 파일(`git archive --format=zip --prefix=ChakraCore-<commit>/ -o ChakraCore-<commit>.zip <commit>`)을 `packages/archive/`에 올리고 `source-archive.txt`를 바꿉니다.
3. 각 패치가 적용되는지 확인합니다(`git -c core.autocrlf=false apply --check`).
4. x64, x86, arm64로 빌드해 `ChakraCore.dll`의 파일 정보를 확인하고, 세 빌드를 게시한 뒤 새 `prebuilt\<arch>.sha256` 파일과 함께 커밋합니다. 바뀐 파일 때문에 CI 캐시가 무효화되어 CI가 새 빌드를 받습니다.
