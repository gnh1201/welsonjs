# WelsonJS용 CrispEmbed 빌드

[English](README.md)

이 폴더는 WelsonJS가 쓰는 Windows용 CrispEmbed를 빌드합니다. [CrispEmbed](https://github.com/CrispStrobe/CrispEmbed)(MIT)는 GGUF 인코더 모델을 ggml로 CPU에서 실행합니다. 텍스트 임베딩, 타입 지정 판단 같은 인코더 작업을 합니다. 런처는 그 `crispembed-server.exe`를 실행합니다([docs/crispembed-launcher-api.md](../../docs/crispembed-launcher-api.md) 참고).

[native/llamacpp](../llamacpp/README_KO.md)처럼, 프로젝트의 릴리스 패키지 대신 고정한 소스 커밋에서 빌드하므로 바이너리가 WelsonJS의 다른 파일과 함께 빌드되고 서명됩니다.

## 파일

| 파일 | 내용 |
| --- | --- |
| `build-crispembed.ps1` | 빌드 스크립트 |
| `commit.txt` | 빌드할 CrispEmbed 커밋(40자 SHA 전체). 태그는 옮겨질 수 있지만 커밋은 그렇지 않으므로 태그 대신 이 SHA로 소스를 받습니다. |
| `version.txt` | llama.cpp와 같은 형식의 버전 이름표 `<버전>.<커밋>.1`. 고정한 커밋이 `VERSION` 파일에 적은 버전(이 파일이 없는 커밋이면 그 이전의 마지막 릴리스에서 태그의 `v`를 뺀 것), `commit.txt`의 앞 여섯 글자, WelsonJS 빌드를 뜻하는 `.1`입니다(`0.17.20.ad01b2.1`). BUILDINFO.txt와 빌드 ID에만 씁니다. |
| `source-archive.txt` | git으로 커밋을 받지 못할 때 쓰는 소스 압축 파일(`url=`)과 그 SHA-256(`sha256=`) |
| `patches\*.patch` | 이름 순서로 적용하는 패치([패치](#패치) 참고) |

현재 고정: `https://github.com/CrispStrobe/CrispEmbed.git`의 커밋 `ad01b21a23d884d197686aa508f78f11d1505925`(2026-11-18, `VERSION` 0.17.20, 릴리스 `v0.17.19` 이후 343개 커밋). ggml 서브모듈은 `https://github.com/CrispStrobe/ggml.git`이며, 그 CrispEmbed 커밋이 기록한 커밋(`3a27b6c665b191cb230be367db5c52d81dd3e2ac`)을 씁니다. 스크립트가 트리에서 읽으므로 이것도 `commit.txt`로 고정됩니다.

## 빌드

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File native\crispembed\build-crispembed.ps1 [-Arch x64|x86|arm64] [-OutDir <폴더>] [-WorkDir <폴더>] [-LlvmDir <폴더>] [-InstallLlvm] [-Force]
```

필요한 것: git, 그리고 C++ 워크로드와 CMake가 있는 Visual Studio(또는 Build Tools). `PATH`에서 먼저 찾은 CMake(예: Strawberry Perl의 것)는 다른 생성기를 고를 수 있으므로, 스크립트는 Visual Studio에 들어 있는 CMake를 찾으면 그것을 씁니다. arm64에는 MSVC ARM64 빌드 도구와 LLVM(clang)도 필요합니다: `-LlvmDir`, Program Files나 `PATH`의 LLVM, 또는 `-InstallLlvm`(clang이 없으면 `choco install llvm`).

스크립트가 하는 일:

1. 고정한 커밋을 받고, 트리가 기록한 커밋의 ggml 서브모듈을 `.gitmodules`의 URL(예상한 URL이어야 함)에서 받습니다.
   git이 실패하면(`-Source auto`, 기본값) 또는 `-Source archive`이면 `source-archive.txt`의 소스 압축 파일을 대신 받고, SHA-256이 일치할 때만 씁니다.
2. 패치를 적용합니다.
3. MSVC로 구성합니다. `-DGGML_NATIVE=OFF`(CrispEmbed의 Windows 릴리스처럼 SSE4.2, AVX, AVX2, FMA, F16C, BMI2로 고정한 x86-64 기준. `GGML_NATIVE`를 켜면 MSVC가 대부분의 일반 PC CPU에 없는 AVX-512를 켤 수 있음), `-DGGML_BLAS=OFF`, `-DCRISPEMBED_BUILD_SHARED=OFF`(실행 파일이 CrispEmbed를 정적으로 링크하므로 `crispembed.dll`은 빌드하지 않음).
   `/utf-8`(소스가 BOM 없는 UTF-8이라 이 옵션이 없으면 MSVC가 시스템 코드 페이지로 읽으며, 한국어 Windows에서는 빌드가 깨짐)과 `0001-windows-resources.patch`의 아이콘·파일 정보 옵션도 넘깁니다. 실행 파일에는 WelsonJS 아이콘(`app\favicon.ico`)이, 모든 실행 파일과 DLL에는 WelsonJS 버전(런처의 `AssemblyFileVersion`, `-ProductVersion`으로 바꿀 수 있음), `CrispEmbed built for WelsonJS`라는 설명, `CompanyName` `Catswords Research`, CrispEmbed·ggml·WelsonJS의 저작권이 들어갑니다.
4. `crispembed-server`, `crispembed-cli`(`crispembed.exe`), `crispembed-quantize`를 빌드합니다.
5. `ggml.dll`, `ggml-base.dll`, `ggml-cpu.dll`, CrispEmbed 패키지에 들어 있는 라이선스와 고지 파일, `BUILDINFO.txt`, `SHA256SUMS.txt`와 함께 `<OutDir>\<arch>`에 둡니다. 실행 파일이나 DLL에 파일 정보가 없거나 `.artifact-configuration.xml`의 서명 목록에 없으면 실패합니다.

출력이 같은 입력(버전, 제품 버전, 커밋, 패치, 아이콘, 스크립트: `BUILDINFO.txt`의 "Build ID")으로 빌드되었고 파일이 `SHA256SUMS.txt`와 일치하면 빌드를 건너뜁니다.

### 소스 압축 파일

CrispEmbed의 GitHub 압축 파일에는 ggml 서브모듈이 들어 있지 않으므로, 대체용 압축 파일은 WelsonJS용으로 만들어 WelsonJS blob 스토리지에 둡니다. CrispEmbed 커밋과 그 커밋이 기록한 ggml 커밋의 `git archive`를 최상위 폴더 하나(`CrispEmbed-<커밋>`, ggml은 `CrispEmbed-<커밋>/ggml`)에 담습니다.

```sh
git archive --format=zip --prefix=CrispEmbed-<커밋>/ -o main.zip <커밋>
git -C ggml archive --format=zip --prefix=CrispEmbed-<커밋>/ggml/ -o ggml.zip HEAD
# 두 파일을 경로마다 항목 하나씩 CrispEmbed-<커밋>.zip으로 합치고, packages/archive/에 올린 뒤
# 그 URL과 SHA-256을 source-archive.txt에 적습니다.
```

### 아키텍처

| `-Arch` | 컴파일러 | CPU 기준 |
| --- | --- | --- |
| `x64`(기본값) | MSVC, Visual Studio 생성기 | SSE4.2, AVX, AVX2, FMA, F16C, BMI2 |
| `x86` | MSVC, Visual Studio 생성기(Win32) | 위와 같되 BMI2 제외(ggml의 BMI2 코드가 32비트 MSVC에 없는 `_pdep_u64`를 씀). `/LARGEADDRESSAWARE`로 링크(64비트 Windows에서 4GB 주소 공간) |
| `arm64` | clang. x64에서 `cmake\arm64-windows-llvm.cmake`와 Ninja로 Visual Studio `x64_arm64` 환경에서 교차 컴파일 | WelsonJS llama.cpp ARM64 빌드와 같은 `-march=armv8.7-a`. OpenMP 대신 ggml의 스레드 풀 |

ggml은 ARM용 MSVC를 지원하지 않으므로 arm64는 clang을 씁니다. CrispEmbed의 `CMakeLists.txt`는 `NOMINMAX`, `WIN32_LEAN_AND_MEAN`, `_USE_MATH_DEFINES`, `_CRT_SECURE_NO_WARNINGS`를 MSVC에서만 정의하므로, 툴체인 파일이 clang용으로 정의합니다. x86 빌드는 x64 빌드와 같은 판단을 냅니다(라벨 8개 모두 같고 확률 차이 1e-6 이내). arm64 빌드는 형식(ARM64)과 가져오는 DLL(VC++ 런타임과 Windows만)을 확인했고, 실행해 보지는 않았습니다.

## CI(AppVeyor)

Release 작업이 llama.cpp 빌드 다음에 x64, arm64, x86마다 스크립트를 실행해 `artifacts\crispembed\<아키텍처>`에 두고, 그 폴더를 캐시합니다. 캐시 키는 `commit.txt`, `version.txt`, 스크립트, 패치, arm64 툴체인 파일, 아이콘, 런처의 `AssemblyInfo.cs`입니다(새 패치를 추가하면 그 파일도 넣으세요). 파일은 `artifacts.zip`에 들어가 다른 아티팩트와 함께 서명됩니다.

## 설치

설치 구성 요소 `crispembed`를 고르면 artifacts 패키지가 함께 선택되고, `postInstall.ps1`이 그 안에서 컴퓨터에 맞는 `crispembed\<아키텍처>`(arm64 빌드가 없는 ARM64 Windows에서는 에뮬레이션으로 실행되는 `crispembed\x64`)를 런처가 `crispembed-server.exe`를 실행하는 `%APPDATA%\WelsonJS\crispembed\`로 복사합니다. 모델은 별도 구성 요소입니다(예: `crispembed\embeddinggemma_2_typed_decision`).

## 패치

| 파일 | 내용 |
| --- | --- |
| `0001-windows-resources.patch` | 모든 실행 파일과 DLL의 Windows 아이콘과 버전 정보. 최상위 `CMakeLists.txt` 끝에서 포함하는 `cmake/win-resources.cmake`와 캐시 옵션 `CRISPEMBED_WIN_ICON`, `CRISPEMBED_WIN_DESCRIPTION`, `CRISPEMBED_WIN_VERSION`, `CRISPEMBED_WIN_COMPANY`, `CRISPEMBED_WIN_COPYRIGHT`, `CRISPEMBED_WIN_COMMIT`. llama.cpp `0003-windows-build.patch` 파트 1과 같은 방식이며, 옵션이 비어 있으면 빌드가 달라지지 않습니다. |

## 업데이트 방법

1. `commit.txt`를 새 커밋(태그 객체가 아닌 커밋 자체)으로, `version.txt`를 `<그 커밋의 VERSION>.<커밋 앞 여섯 글자>.1`로 바꿉니다. 커밋에 `VERSION` 파일이 없으면 `gh api repos/CrispStrobe/CrispEmbed/compare/<태그>...<커밋>`이 `behind_by` 0을 돌려주는 가장 큰 릴리스 태그에서 `v`를 뺀 것을 씁니다.
2. ggml 서브모듈 URL이 바뀌었으면 확인하고 스크립트의 `GgmlRepositoryUrl`을 바꿉니다.
3. 새 커밋의 소스 압축 파일을 만들어([소스 압축 파일](#소스-압축-파일) 참고) 올리고 `source-archive.txt`를 바꿉니다.
4. 각 패치가 적용되는지 확인하고(`git -c core.autocrlf=false apply --check`), 새 패치는 AppVeyor 캐시 키에 넣습니다.
5. 로컬에서 `-Source archive`로도 빌드하고, 빌드된 파일이 `.artifact-configuration.xml`에 나열된 파일과 같은지, `crispembed-server`가 설치되는 모델로 `/health`에 답하는지 확인합니다.
6. 커밋합니다. 바뀐 파일 때문에 AppVeyor 캐시가 무효화됩니다.
