# WelsonJS용 llama.cpp 빌드

[English version](README.md)

이 폴더는 WelsonJS가 쓰는 Windows용 llama.cpp를 WelsonJS용 패치를 적용해 빌드합니다. 기본 소스는 Microsoft가 [bitnet.cpp](https://github.com/microsoft/BitNet)와 함께 `3rdparty/llama.cpp` 서브모듈로 배포하는 llama.cpp 포크이며, 이 포크는 bitnet.cpp의 양자화 기술을 지원하기 위해 만들어졌습니다. (저장소 자체는 기여자 계정인 `isHuangXin/llama.cpp`에 있습니다. [파일](#파일) 참고.) 출력물에는 공식 `llama-<version>-bin-win-cpu-<arch>.zip` 패키지와 같은 파일, 즉 `llama-server`, `llama-cli`, `llama-quantize`와 ggml 백엔드가 들어 있습니다. x64 빌드에는 모든 x86-64 CPU 변형이 들어 있고 실행 시 하나가 선택됩니다. arm64 빌드에는 CPU 백엔드가 하나입니다.

포크는 BitNet의 `I2_S` 타입과 커널을 추가합니다. 그래서 이 빌드는 일반 GGUF 파일 외에도 [microsoft/BitNet-b1.58-2B-4T-gguf](https://huggingface.co/microsoft/BitNet-b1.58-2B-4T-gguf) 같은 1.58비트 BitNet 모델을 불러옵니다. 업스트림 llama.cpp는 이런 모델을 불러오지 못합니다.

이 빌드는 포크 위에 Tencent의 [AngelSlim](https://github.com/Tencent/AngelSlim) 양자화 기술인 STQ1_0 삼진(ternary) 타입과 커널을 추가합니다([ggml-org/llama.cpp#22836](https://github.com/ggml-org/llama.cpp/pull/22836), 업스트림에 병합되지 않음. [0001-stq1_0.patch](#0001-stq1_0patch) 참고). 그래서 [tencent/Hy-MT2-1.8B-1.25Bit-GGUF](https://huggingface.co/tencent/Hy-MT2-1.8B-1.25Bit-GGUF)처럼 AngelSlim으로 만든 1.25비트 모델을 불러옵니다. 포크도 업스트림 llama.cpp도 이런 모델을 불러오지 못합니다.

런처는 `llama-server`를 `%APPDATA%\WelsonJS`에 설치된 모델로 라우터 모드에서 실행하고, HTTP API로만 사용합니다. 요청은 모델 이름을 명시합니다([docs/llamacpp-launcher-api.md](../../docs/llamacpp-launcher-api.md)).

## 파일

| 파일 | 용도 |
| --- | --- |
| `commit.txt` | 빌드하는 llama.cpp 커밋(40자 SHA 전체). 브랜치는 움직일 수 있지만 커밋은 바뀌지 않으므로, 소스는 브랜치가 아니라 이 SHA로 가져옵니다. |
| `version.txt` | 버전 이름표 `<upstream 릴리스>.<커밋>.1`(llama.cpp는 소스에 버전을 적지 않으므로 릴리스로 대신함). 포크가 upstream에서 갈라진 커밋보다 앞선 마지막 upstream llama.cpp 릴리스(`bNNNN`), 고정한 커밋(`commit.txt`)의 앞 일곱 글자, WelsonJS 빌드를 뜻하는 `.1`을 차례로 붙입니다. 이름표일 뿐(BUILDINFO.txt와 빌드 ID) 소스를 선택하지 않습니다. |
| `source-archive.txt` | git으로 커밋을 가져오지 못할 때 쓰는 대체 소스 압축 파일(`url=`)과 그 SHA-256(`sha256=`). |
| `patches/*.patch` | 이름 순서대로 `git apply`로 적용하는 패치. LF 줄바꿈으로 저장되어 있고, `.gitattributes`가 체크아웃 시 바이트 그대로 유지합니다. |
| `build-llamacpp.ps1` | 소스를 가져와 패치를 적용하고, 빌드하고, 출력물을 씁니다. |

현재 고정된 버전: `https://github.com/isHuangXin/llama.cpp.git`의 커밋 `390c307752ab78fd8189f359d6954c9ba1be74af`(브랜치 `release-bitnet-embedding-0.6b-270m`, 2026-07-15). BitNet `0b341e582afbf9e1011f24744b554c96a3477eb5`의 서브모듈 커밋입니다. 이 커밋은 upstream llama.cpp(`ggml-org/llama.cpp`)의 `665892536dfb`(2026-07-04) 다음에서 갈라졌으며(포크 자체 커밋 46개), 그보다 앞선 마지막 upstream 릴리스가 `b9871`이므로 `version.txt`는 `b9871.390c307.1`입니다.

포크는 업스트림 llama.cpp보다 오래되었으므로, 그 이후의 업스트림 변경은 들어 있지 않습니다.

## BitNet 저장소를 쓰지 않는 이유

bitnet.cpp는 포크를 BitNet 저장소의 하위 프로젝트로 빌드하며, 이때 두 가지가 추가됩니다:

- TL1/TL2 룩업 테이블 커널(`src/ggml-bitnet-lut.cpp`, `include/ggml-bitnet.h`, `include/bitnet-lut-kernels.h`). 이 커널은 한 모델의 텐서 모양에 맞춰 생성된 코드가 필요하므로(`setup_env.py`가 실행하는 `utils/codegen_tl*.py`), 이 커널을 넣은 빌드는 그 모델 하나에만 쓸 수 있습니다.
- "Clang or GCC is required"로 멈추는 CMake 검사.

`I2_S` 타입과 커널(`ggml/src/ggml-cpu/ggml-cpu-i2s.c`, AVX2, 가능하면 AVX-VNNI 또는 AVX512-VNNI)은 포크에 들어 있습니다. 이 커널은 BitNet 저장소의 헤더 하나, `gemm-config.h`(커널의 블록 크기)가 필요하며, `0002-bitnet-i2s.patch`(파트 1)가 이 헤더를 포크에 복사해 넣습니다.

나머지 BitNet 파일은 포크 코드에서 `#if defined(GGML_BITNET_ARM_TL1) || defined(GGML_BITNET_X86_TL2)` 안에서만 쓰입니다. 포크의 CMake만 이 파일들을 조건 없이 참조하는데, `0002-bitnet-i2s.patch`(파트 1)가 이를 바꿉니다. 따라서 TL1/TL2를 끄면 포크는 단독으로, 업스트림 llama.cpp처럼 MSVC로 빌드됩니다.

## 빌드

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File native\llamacpp\build-llamacpp.ps1 `
    [-Arch x64|x86|arm64] [-OutDir <dir>] [-WorkDir <dir>] [-Source auto|git|archive] [-LlvmDir <dir>] [-InstallLlvm] [-Force]
```

요구 사항:

- **모든 아키텍처 공통:** git, C++ 워크로드가 있는 Visual Studio(MSVC).
- **x64와 x86:** 설치된 Visual Studio 버전을 아는 CMake. 예를 들어 CMake 3.29에는 Visual Studio 2026용 생성기가 없으므로, Visual Studio에 포함된 CMake를 쓰세요.
- **arm64**(x64 PC에서 크로스 컴파일):
  - LLVM(`choco install llvm`). ggml은 ARM에서 MSVC를 지원하지 않습니다.
  - Visual Studio 구성 요소 "MSVC ARM64/ARM64EC 빌드 도구"(`Microsoft.VisualStudio.Component.VC.Tools.ARM64`). `lib\arm64` 폴더는 AddressSanitizer 라이브러리만 있는 상태로도 존재할 수 있으므로, 스크립트는 `VC\Tools\MSVC\<version>\lib\arm64\msvcrt.lib`가 있는지 확인합니다.
  - "Windows용 C++ CMake 도구" 구성 요소(CMake와 Ninja).

### 아키텍처

| | x64 | arm64 |
| --- | --- | --- |
| 컴파일러 | MSVC, Visual Studio 생성기 | 포크의 `cmake\arm64-windows-llvm.cmake` 툴체인을 쓰는 clang(LLVM), Ninja |
| 환경 | Visual Studio 기본 | Visual Studio `x64_arm64` 개발자 환경(ARM64 라이브러리, x64에서 실행되는 ARM64 링커). ARM64 C 런타임이 있는 MSVC 버전을 `vcvarsall.bat`에 넘깁니다 |
| CPU 대상 | 모든 x86-64 변형(`GGML_CPU_ALL_VARIANTS`), 실행 시 선택 | `-march=armv8.7-a`, 백엔드 하나 `ggml-cpu.dll`(Windows의 ARM에서는 `GGML_CPU_ALL_VARIANTS`를 지원하지 않음) |
| OpenMP | 켬(VC++ 재배포 패키지의 `VCOMP140.DLL`) | 끔: clang에서는 `libomp140.aarch64.dll`이 필요한데, Visual Studio는 이를 `debug_nonredist`로만 제공합니다. ggml은 자체 스레드 풀을 씁니다 |
| 호스트 도구(`llama-ui-embed`) | 빌드의 일부로 빌드 | 작업 폴더의 `host-cxx.cmd`(`HOST_CXX_COMPILER`)로 x64용으로 빌드, 아래 참고 |

**x86**(32비트)은 x64처럼 MSVC와 Visual Studio 생성기(`-A Win32`)로 빌드하되, CPU 백엔드는 하나(`ggml-cpu.dll`)이며 `GGML_NATIVE=OFF`의 AVX2 기준에서 BMI2를 뺍니다(`GGML_CPU_ALL_VARIANTS=OFF`, `GGML_BMI2=OFF`). ggml의 BMI2 코드가 32비트 MSVC에 없는 `_pdep_u64`를 쓰고, CPU 변형 여러 개가 BMI2를 켜기 때문입니다. 실행 파일은 `/LARGEADDRESSAWARE`로 링크해서 32비트 프로세스가 64비트 Windows에서 2GB 대신 4GB 주소 공간을 씁니다. 이것이 없으면 모델과 8192 토큰 문맥(런처의 `LlamaCppContextSize`)의 KV 캐시를 할당하지 못했습니다. x86 빌드는 8192 토큰 문맥에서 x64 빌드와 같은 그리디 답을 냅니다(Qwen3Guard 0.6B Q4_K_M, Hy-MT2 1.8B Q4_K_M, BitNet b1.58 2B I2_S). 더 큰 문맥이나 모델은 4GB에 들어가지 않을 수 있습니다.

- `-InstallLlvm`은 arm64 빌드가 필요한데 clang이 없을 때 `choco install llvm -y`를 실행합니다.
- arm64에서는 스크립트가 LLVM과 Visual Studio의 CMake, Ninja를 다른 사본(예: Strawberry Perl의 CMake 3.29)보다 앞서 `PATH`에 넣습니다.
- 크로스 컴파일할 때 `tools/ui`는 `PATH`에서 처음 찾은 `g++`나 `clang++`로 호스트(x64)용 `llama-ui-embed`를 빌드하며, `-O2 -std=c++17 -o`만 넘깁니다.
  - `x64_arm64` 환경에서는 `LIB`가 ARM64 라이브러리를 가리키므로, AppVeyor에서 `clang++`가 이를 링크하지 못했습니다(`LNK2019`, `LNK4272`). 로컬에서는 Strawberry Perl의 `g++`가 먼저 잡혀 문제가 가려졌습니다.
  - 스크립트는 `host-cxx.cmd`를 만듭니다. 이 래퍼는 `LIB`를 x64 라이브러리로 바꾸고(`LIB` 항목의 `arm64`를 `x64`로 바꿈) `clang++ --target=x86_64-pc-windows-msvc`를 호출합니다. 래퍼를 `HOST_CXX_COMPILER`로 넘기므로 모든 머신이 같은 컴파일러를 씁니다.
- 두 아키텍처 모두 `GGML_CCACHE=OFF`라서 `PATH`에 있는 `ccache`를 쓰지 않습니다.
- `-march=armv8.7-a`는 포크의 Windows ARM64 기본값으로, Snapdragon X 계열 CPU를 대상으로 합니다. 이전 ARM64 PC(예: Snapdragon 8cx Gen 1~3)에서는 실행되지 않을 수 있습니다.

### 소스

- `-Source auto`(기본값)는 git으로 `commit.txt`의 커밋을 가져오고 `HEAD`가 그 커밋인지 확인합니다. git이 실패하면 압축 파일로 대체합니다.
- `-Source git`과 `-Source archive`는 한 가지 방법을 강제합니다.
- 압축 파일은 다음 조건이 모두 맞을 때만 씁니다:
  - URL에 고정된 커밋이 들어 있음,
  - SHA-256이 `source-archive.txt`와 일치함,
  - 모든 항목이 `llama.cpp-<commit>/` 안에 있음.
- 압축을 풀 때 최상위 폴더는 제거합니다. 작업 폴더 밖으로 풀리는 항목은 거부합니다.
- `source-archive.txt`의 압축 파일은 같은 커밋의 git 체크아웃과 파일 단위로 비교했습니다.
  - 일반 파일 3059개가 모두 같습니다.
  - 심볼릭 링크 6개(`spm-headers`와 `spm-sources` 아래의 Swift 패키지 헤더)는 git이 `core.symlinks=false`일 때처럼 링크 대상을 담은 작은 텍스트 파일로 기록됩니다. 빌드는 이 파일들을 쓰지 않습니다.
  - 패치를 적용한 뒤 git 소스와 압축 파일 소스는 같습니다.
- 가장 깊은 Web UI 경로는 소스 폴더 아래 161자입니다.
  - git 체크아웃은 `core.longpaths=true`를 씁니다.
  - 스크립트는 압축 해제와 이전 작업 폴더 삭제에 `\\?\` 접두사를 쓰므로 `MAX_PATH` 제한이 적용되지 않습니다.
- LF 패치가 적용되도록 소스는 `core.autocrlf=false`로 체크아웃합니다. AppVeyor는 전역으로 `core.autocrlf=true`를 설정하므로 이 점이 중요합니다.
- `git apply`는 `-c core.autocrlf=false`로 실행합니다. 저장소 밖(압축 파일 소스)에서는 그렇지 않으면 전역 설정을 따라 패치된 파일을 CRLF로 씁니다.
- 압축 파일 소스에는 `.git` 폴더가 없으므로 스크립트가 `LLAMA_BUILD_COMMIT`를 넘기고, `--version`은 여전히 커밋을 보여 줍니다.

### 구성

- `GGML_BACKEND_DL=ON`, x64는 공식 패키지처럼 `GGML_CPU_ALL_VARIANTS=ON`. 포크는 `I2_S` 커널을 모든 CPU 변형에 컴파일해 넣습니다.
- `GGML_BITNET_ARM_TL1=OFF`와 `GGML_BITNET_X86_TL2=OFF`(기본값)를 명시합니다. TL 커널은 BitNet 저장소가 필요합니다.
- 테스트, 예제, 통합 `llama` 앱은 빌드하지 않습니다.
- `LLAMA_OPENSSL=OFF`. 도구들은 localhost HTTP만 쓰면 됩니다.
- `LLAMA_WIN_ICON`을 `app\favicon.ico`로 설정하므로 실행 파일에 WelsonJS 아이콘이 들어가고, 모든 실행 파일과 DLL에 버전 정보가 들어갑니다. 버전 정보에는 WelsonJS 버전(`0.2.7.62`, 런처의 `AssemblyFileVersion`, `-ProductVersion`으로 바꿀 수 있음), `llama.cpp built for WelsonJS`라는 설명, `CompanyName` `Catswords Research`, 모든 저작자의 저작권이 들어 있습니다. 이 옵션은 패치에서 추가됩니다. [0003-windows-build.patch 파트 1](#파트-1-windows-아이콘과-버전-정보)을 참고하세요.
- 빌드 중에는 아무것도 다운로드하지 않습니다: `LLAMA_USE_PREBUILT_UI=OFF`, `LLAMA_BUILD_UI=OFF`. 기본값으로는 빌드가 고정되지 않은 Hugging Face 위치에서 서버 Web UI를 받는데, 런처는 Web UI를 쓰지 않습니다.

### 출력과 빌드 건너뛰기

파일은 `<OutDir>\<arch>`에 기록됩니다. CI에서는 `artifacts\llamacpp\x64`, `artifacts\llamacpp\arm64`, `artifacts\llamacpp\x86`입니다. 압축 파일은 만들지 않습니다. 폴더에는 다음 파일도 있습니다:

- `LICENSE-llama.cpp.txt`(MIT).
- `BUILDINFO.txt`: 버전 이름표, 제품 버전, 커밋, 사용한 소스(git, 또는 압축 파일 URL과 해시), 컴파일러, CPU 백엔드, BitNet 커널, 패치, 빌드 ID.
- `SHA256SUMS.txt`: 파일마다 `<sha256>  <파일 이름>` 한 줄.

빌드 ID는 다음 입력에 대한 SHA-256입니다:

- `version.txt`, `commit.txt`, 아키텍처,
- 모든 패치의 이름과 내용,
- 실행 파일에 넣는 아이콘인 `app\favicon.ico`의 내용,
- CMake 옵션이 들어 있는 스크립트 자체의 내용.

`BUILDINFO.txt`의 빌드 ID가 같고 모든 파일이 `SHA256SUMS.txt`와 일치하면 스크립트는 빌드를 건너뜁니다. 그렇지 않으면(예: 입력이 바뀌었거나 파일이 수정됨) 다시 빌드합니다. `-Force`는 항상 빌드합니다.

다음은 빌드 ID에 들어가지 않습니다:

- `source-archive.txt`. 두 소스 모두 같은 커밋이기 때문입니다.
- 컴파일러 버전. 빌드 이미지의 MSVC가 새로워져도 다시 빌드하지 않습니다. 그럴 때는 `-Force`를 쓰세요.

## CI(AppVeyor)

CI에서는 빌드하지 않습니다. llama.cpp와 CrispEmbed를 x64, arm64, x86으로 빌드하면 AppVeyor 작업 제한 60분을 넘기 때문입니다. 개발자 PC에서 이 스크립트로 빌드해 WelsonJS blob 스토리지에 올리고, **Release** 작업이 이를 복원해 다른 산출물과 함께 패키징하고 서명합니다.

1. 아키텍처마다 **빌드**합니다: `-Arch x64`, `-Arch arm64`, `-Arch x86`(`PATH`의 CMake가 다른 생성기를 기본으로 고르는 PC에서는 x64와 x86에 `-Generator "Visual Studio 18 2026"`을 줍니다).
2. 빌드마다 **게시**합니다: `native\publish-prebuilt.ps1 -Component llamacpp -Arch <arch> -BuildDir <OutDir>\<arch>`.
   - 폴더가 `SHA256SUMS.txt`와 맞는지, `BUILDINFO.txt`가 현재 `version.txt`, `commit.txt`, 아키텍처를 적고 있는지 확인합니다.
   - 각 파일을 그대로, 그리고 gzip으로 압축한 `<file>.gz`로 AzCopy(먼저 `azcopy login`)를 써서 `packages/native/llamacpp/<version>-<arch>/`에 올립니다. 이미 있는 blob은 덮어쓰지 않으므로 게시한 빌드는 바뀌지 않습니다.
   - `SHA256SUMS.txt`를 `native\llamacpp\prebuilt\<arch>.sha256`으로 복사합니다.
3. 세 `prebuilt\<arch>.sha256` 파일을 변경과 함께 **커밋**합니다.

Debug와 Release 작업은 아키텍처마다 `native\restore-prebuilt.ps1 -Component llamacpp -Arch <arch>`를 실행해 `artifacts\llamacpp\<arch>`에 둡니다. `prebuilt\<arch>.sha256`에 적힌 파일을 `<version>-<arch>`에서 `<file>.gz`로 받아 압축을 풀고(`.gz`를 받지 못하거나 쓸 수 없으면 원본 파일을 받음), SHA-256이 맞을 때만 쓰므로, 무엇을 배포할지는 스토리지가 아니라 저장소가 정합니다. 두 패키지 모두 서명합니다(Debug는 test-signing 정책, Release는 release-signing 정책). 각 작업은 `version.txt`와 세 `prebuilt\<arch>.sha256` 파일을 키로 `artifacts\llamacpp`를 캐시하므로, 이 파일들이 그대로면 복원된 폴더가 기록과 맞아 아무것도 받지 않습니다.

`.artifact-configuration.xml`은 세 아키텍처의 실행 파일과 DLL을 모두 이름으로 하나씩 적고(와일드카드 없음), 빌드 스크립트는 목록에 없는 바이너리가 있으면 실패하므로 새 바이너리가 서명되지 않은 채 남을 수 없습니다. Authenticode 서명은 파일을 바꾸므로, 서명된 패키지의 `SHA256SUMS.txt`는 서명 전 파일의 해시입니다.

현재 빌드(`b9871.390c307.1`)는 Visual Studio 2026으로 만들었습니다. x64와 x86은 MSVC 19.50(도구 집합 14.50), arm64는 MSVC 14.51 라이브러리와 clang 23입니다. 이를 실행하는 PC에는 이 도구보다 오래되지 않은 Visual C++ 2015-2026 재배포 패키지가 필요합니다.

## 설치

설치 프로그램은 이 빌드를 씁니다. 더 이상 공식 llama.cpp 패키지를 다운로드하지 않습니다(`data\DownloadUrls.psd1`에 `llamacpp` 항목이 없음).

- `llamacpp` 구성 요소(그리고 이를 선택하는 모든 GGUF 모델 구성 요소)는 `artifacts`도 선택합니다. 빌드가 artifacts 패키지에 들어 있기 때문입니다.
- 빌드는 다른 바이너리와 함께 서명되도록 artifacts 패키지(`artifacts.zip`, 폴더 `llamacpp\<arch>`)에 들어 있습니다.
- `postInstall.ps1`(`Install-ArtifactsPackage`)은 패키지를 임시 폴더에 풉니다.
  - 이전 설치의 파일을 지운 뒤 `llamacpp\<arch>`를 `%APPDATA%\WelsonJS\llamacpp`로 복사합니다. 런처가 `llama-server.exe`를 실행하는 곳은 여기뿐입니다. 이 폴더는 이전처럼 사용자 `PATH`에 추가됩니다.
  - 나머지는 이전처럼 `%APPDATA%\WelsonJS\bin`으로 갑니다. llama.cpp 빌드는 `bin`에 남지 않습니다.
- ARM64 설치는 같은 artifacts 패키지(`data\DownloadUrls.psd1`)를 받고 `arm64` 빌드를 씁니다. 패키지의 x86 WelsonJS 바이너리는 x86 에뮬레이션으로 실행됩니다. 패키지에 `arm64` 빌드가 없으면 스크립트는 `x64`로 대체하며, 이는 Windows 11에서 x64 에뮬레이션으로 실행됩니다.
- 32비트 Windows에는 이전처럼 llama.cpp가 설치되지 않습니다.

## 업스트림 llama.cpp와의 호환성

포크는 일부 ggml 텐서 타입에 업스트림 llama.cpp와 다른 번호를 씁니다:

| ggml 텐서 타입 | 이 빌드(포크) | 업스트림 llama.cpp |
| --- | --- | --- |
| 36 | `I2_S`(BitNet) | 사용 안 함 |
| 37 | `I8_S`(BitNet, 실행 중에만 사용) | 사용 안 함 |
| 38 | `TL1`(BitNet) | 사용 안 함 |
| 42 | `TL2`(BitNet), 또는 Tencent 파일의 `STQ1_0`(크기로 판별, `0001-stq1_0.patch` 파트 3) | `Q2_0` |
| 43 | `STQ1_0`(`0001-stq1_0.patch`, PR의 ID) | `F8_E4M3` |

- BitNet `I2_S` GGUF 파일과 STQ1_0 GGUF 파일(타입 42를 쓰는 Tencent 파일, 타입 43을 쓰는 PR 파일)은 이 빌드에서는 불러와지고 업스트림 llama.cpp에서는 불러와지지 않습니다.
- `Q2_0`(42)이나 `F8_E4M3`(43) 텐서를 저장한 업스트림 GGUF 파일은 호환되지 **않습니다**. 이 빌드는 이 ID를 `TL2`/`STQ1_0`과 `STQ1_0`으로 읽습니다. 타입마다 크기가 다르므로, 이런 파일은 잘못된 데이터로 불러와지기보다 GGUF 로더의 크기 검사에서 실패할 것으로 예상합니다. 이 부분은 테스트하지 않았습니다.
- BitNet `TL2` 텐서는 ID를 유지하지만, 이 빌드에는 이를 실행할 TL2 커널이 없습니다.
- WelsonJS가 설치하는 모델의 아키텍처(`hunyuan-dense`, `qwen35`, `qwen3`, `bitnet-b1.58`)는 포크에 들어 있습니다:
  - Hy-MT2 1.8B 1.25비트(`tencent/Hy-MT2-1.8B-1.25Bit-GGUF`)는 STQ1_0 텐서를 타입 42로 저장하므로 이 빌드에서만 불러와집니다(`0001-stq1_0.patch` 파트 3). 나머지 텐서는 F32와 Q6_K입니다.
  - BitNet b1.58 2B-4T(`microsoft/bitnet-b1.58-2B-4T-gguf`, 구성 요소 `bitnet_b1_58_2b_4t`)는 I2_S 텐서(타입 36)를 저장하므로 이 빌드에서만 불러와집니다. [BitNet(I2_S) 모델](#bitneti2_s-모델)을 참고하세요.
  - FrogNano와 Qwen3Guard(Q4_K_M)는 텐서 타입 0, 12, 14(F32, Q4_K, Q6_K)만 씁니다.

## BitNet(I2_S) 모델

`microsoft/BitNet-b1.58-2B-4T-gguf`는 추가 옵션 없이 `llama-server`의 채팅 API로 동작합니다. 이는 `0002-bitnet-i2s.patch`의 파트 4~7 덕분이며, 이 파트들은 테스트 중에 발견한 네 가지 문제를 고칩니다:

| 패치가 없을 때의 문제 | 원인 | 파트 |
| --- | --- | --- |
| 첫 문장을 답한 뒤 토큰 한도까지 같은 말을 반복함 | 포크의 FFN이 2B-4T 모델에 SiLU를 쓰는데, 이 모델은 squared ReLU를 씀 | 5 |
| AVX2가 없는 CPU(`x64`, `sse42`, `sandybridge` 변형)에서 `?` 토큰만 생성함 | 포크의 비 AVX2 I2_S 대체 코드가 AVX2 코드와 다른 것을 계산함 | 4 |
| `/v1/chat/completions`로 대화를 지어냄 | GGUF 파일에 모델이 따르지 않는 채팅 템플릿이 들어 있음 | 6 |
| "GENERATION QUALITY WILL BE DEGRADED" 경고, 일부 텍스트가 다르게 토큰화됨 | GGUF 파일에 `tokenizer.ggml.pre`가 없음 | 7 |

I2_S 커널 자체는 정확했습니다. I2_S 가중치를 F16으로 변환한 GGUF가 I2_S 파일과 같은 토큰을 생성했습니다.

이 GGUF 파일의 `general.file_type`은 40이고, 포크에서는 `Q1_0`입니다. 파일에 I2_S 텐서가 있고 Q1_0 텐서가 없으므로 `0001-stq1_0.patch`(파트 3)는 이를 `I2_S`로 표시합니다. 파일 타입은 메타데이터일 뿐 불러오기에 영향을 주지 않습니다.

## llama-quantize로 양자화하기

`llama-quantize.exe`는 다른 도구와 함께 `%APPDATA%\WelsonJS\llamacpp`에 설치되며, 이 폴더는 사용자 `PATH`에 들어 있습니다. `llama-quantize --help`가 받아들이는 타입 목록을 보여 줍니다.

**타입은 번호가 아니라 이름으로 지정하세요.** `llama-quantize`는 둘 다 받습니다. 먼저 이름을 찾고(대소문자 구분 없음), 없으면 목록에 있는 파일 타입 번호를 찾습니다. BitNet과 STQ1_0 타입의 번호는 빌드마다 다르므로, 다른 빌드나 파일에서 가져온 번호는 다른 타입을 고를 수 있습니다:

| 번호 | 이 빌드 | 다른 곳 |
| --- | --- | --- |
| 41 | `I2_S` | Tencent 1.25비트 파일의 `general.file_type` 41은 STQ1_0 |
| 42 | 받지 않음(`TL1`, 목록에 없음) | PR #22836의 STQ1_0 |
| 44 | `STQ1_0` | STQ1_0은 PR #22836에서 42, Tencent 파일에서 41 |

예를 들어 Tencent 파일에서 가져온 `41`을 넣으면 STQ1_0이 아니라 I2_S로 양자화되고, 삼진 모델이 아니면 모델이 망가집니다. `I2_S`, `STQ1_0` 같은 이름은 그 타입이 있는 모든 빌드에서 같은 타입을 고릅니다.

예:

```powershell
# BitNet b1.58: BitNet 변환기가 만든, 삼진 가중치를 담은 F32(또는 F16) GGUF를 I2_S로
# 출력 가중치와 토큰 임베딩은 F16으로 남습니다.
llama-quantize ggml-model-f32.gguf ggml-model-i2_s.gguf I2_S

# Sherry로 학습한 체크포인트(예: AngelSlim/Hy-MT1.5-1.8B-1.25bit)를 BF16으로 변환한 파일을 STQ1_0으로
llama-quantize Hy-MT1.5-1.8B-bf16.gguf Hy-MT1.5-1.8B-STQ1_0.gguf STQ1_0

# STQ1_0 파일을 다른 타입으로(재양자화에는 --allow-requantize가 필요)
llama-quantize --allow-requantize Hy-MT2-1.8B-1.25Bit.gguf Hy-MT2-1.8B-Q4_K_M.gguf Q4_K_M
```

- I2_S와 STQ1_0은 이미 그 구조(삼진, STQ1_0은 3:4 희소까지)를 가진 가중치를 담습니다. 다른 모델을 양자화하면 모델이 망가집니다. [STQ1_0](#llama-quantize로-양자화하기-1)과 [I2_S](#파트-9-i2_s용-llama-quantize)를 참고하세요.
- I2_S 파일은 다른 타입으로 재양자화할 수 없습니다. I2_S에는 `to_float`가 없습니다(`0002-bitnet-i2s.patch` 파트 8).

## 테스트 결과

### AppVeyor 빌드

빌드 0.2.7.build2427의 Release 작업에서 받은 `artifacts.zip`의 `llamacpp\x64` 파일(MSVC 19.29, 커밋 `c68128af`, 서명 전)을 12코어 x64 PC에서 테스트했습니다:

- `SHA256SUMS.txt`가 두 아키텍처의 모든 파일과 일치합니다(x64 22개, arm64 14개).
- CPU 변형 세 가지로 `llama-cli`를 실행했습니다. 각각 그 PC의 최적 변형만, `ggml-cpu-sse42.dll`만, `ggml-cpu-x64.dll`만 남겼습니다:

  | 변형 | BitNet b1.58 2B-4T(I2_S) | Hy-MT2 1.25비트(STQ1_0), 한국어로 번역 |
  | --- | --- | --- |
  | 최적(AVX2) | `The capital of France is Paris.`(2.6초) | 오늘 날씨가 정말 좋으니 공원에서 산책을 하러 가자.(2.8초) |
  | `sse42` | 같음(4.9초) | 같음(4.8초) |
  | `x64`(일반 코드) | 같음(4.3초) | 같음(43.8초) |

  - BitNet은 `I2_S` 텐서 210개를 불러왔고, 모든 변형에서 `<|eot_id|>`(토큰 128009)로 멈췄습니다.
  - Hy-MT2 1.25비트는 타입 42 텐서 224개를 STQ1_0으로 읽었습니다. 한국어를 영어로도 정확히 번역했습니다.
- `llama-server` 채팅 API(최적 변형): BitNet 25 토큰/초, Hy-MT2 1.25비트 58 토큰/초.
- arm64 파일은 이 PC에서 실행할 수 없었습니다.

### 로컬 빌드

12코어 PC에서 MSVC 19.50(Visual Studio 2026)으로, 모든 패치를 적용해 빌드했습니다.

`llama-server`가 각 모델을 불러와 "What is the capital of France? Answer in one short sentence."에 `/v1/chat/completions`로 답했습니다(`temperature` 0, 템플릿이나 토크나이저 옵션 없음). 그 PC에서 선택된 CPU 변형은 AVX2와 AVX-VNNI를 지원합니다.

| 모델 | 답 | 생성 속도 |
| --- | --- | --- |
| Qwen3Guard-Gen 0.6B Q4_K_M | `Safety: Safe Categories: None`(분류기 출력) | 71 토큰/초 |
| Hy-MT2 1.8B Q4_K_M | `Paris is the capital of France.` | 32 토큰/초 |
| FrogNano 4B 2609 Q4_K_M | 182토큰의 추론(`reasoning_content`) 뒤 `The capital of France is Paris.` | 10 토큰/초 |
| BitNet b1.58 2B-4T(I2_S) | `The capital of France is Paris.`, 이어서 `<|eot_id|>` | 24 토큰/초 |

Hy-MT2(번역)는 모델 README의 기본 프롬프트("Translate the following text into {target_lang}. Note that you should only output the translated result without any additional explanation:")와 `temperature` 0으로 테스트했습니다. 모든 파일이 다음 세 텍스트를 번역하고 스스로 멈췄습니다:

| 텍스트 | 대상 언어 |
| --- | --- |
| The weather is really nice today, so let's go for a walk in the park. | 한국어 |
| 오늘 회의는 오후 세 시로 변경되었습니다. 늦지 않게 참석해 주세요. | 영어 |
| 今天天气真好，我们去公园散步吧。 | 영어 |

| 파일 | 예(한국어로 번역) | 생성 속도 |
| --- | --- | --- |
| `tencent/Hy-MT2-1.8B-1.25Bit-GGUF`(STQ1_0) | 오늘 날씨가 정말 좋으니 공원에서 산책을 하러 가자. | 28~30 토큰/초 |
| `tencent/Hy-MT2-1.8B-GGUF` Q4_K_M | 오늘 날씨가 정말 좋으니, 공원에서 산책하러 가요. | 27 토큰/초 |
| `tencent/Hy-MT2-1.8B-GGUF` Q6_K | 오늘 날씨가 정말 좋으니, 공원에서 산책을 하러 가보죠. | 23 토큰/초 |
| `tencent/Hy-MT2-1.8B-GGUF` Q8_0 | 오늘 날씨가 정말 좋으니, 공원에서 산책을 하러 가보죠. | 18 토큰/초 |

그 밖에 확인한 것:

- **`x64` 변형의 BitNet.** `ggml-cpu-x64.dll`만 있을 때(AVX2 없음) BitNet 모델은 AVX2 변형과 같은 토큰을 생성하고 `<|eot_id|>`로 끝났습니다.
- **BitNet 토큰화.** 다섯 개의 텍스트(축약형, 숫자, 공백, 한국어)가 `--override-kv tokenizer.ggml.pre=str:llama-bpe`를 줬을 때와 같은 토큰이 되었습니다.
- **CPU 변형 종류별 Hy-MT2 1.25비트.** 테스트한 모든 변형에서 같은 번역을 냈습니다:
  - AVX2 변형: 28~30 토큰/초,
  - `sandybridge`만: 23~24 토큰/초,
  - `sse42`만: 10 토큰/초,
  - `x64`만(일반 코드): 2 토큰/초.
- **STQ1_0 타입 ID.** STQ1_0 텐서를 42에서 43(PR의 번호)으로 바꾼 1.25비트 파일은 Tencent 파일과 같은 번역을 냈습니다. TL2 크기의 타입 42 텐서를 넣은 합성 파일은 그 텐서를 `tl2`로 유지했습니다.
- **`llama-cli`.** 다운로드한 파일을 `llama-cli -st --temp 0`(한 번만 주고받기)으로도 직접 실행했습니다. 모든 Hy-MT2 파일이 양방향으로 위 번역을 냈고, BitNet 모델은 수도 질문과 두 문장 설명 요청에 답하고 스스로 멈췄습니다.
- **arm64 빌드.** 같은 PC에서 clang 23.1.3과 MSVC 14.51(ARM64 빌드 도구)로 약 2.5분 만에 크로스 컴파일했습니다. 실행 파일과 DLL 12개 모두 ARM64(PE machine `0xAA64`)이고, 시스템 DLL, VC++/UCRT 런타임, 서로만 가져옵니다. 아이콘이 들어 있습니다. 이 PC(x64)에서는 실행할 수 없었습니다.
- **`llama-quantize`.**
  - **I2_S(`0002-bitnet-i2s.patch` 파트 9).** `microsoft/BitNet-b1.58-2B-4T-gguf`를 F16(I2_S 가중치×스케일)으로 변환한 뒤 `llama-quantize <f16> <out> I2_S`로 다시 양자화했습니다. 결과는 원본 파일과 텐서 타입이 같고(I2_S 210개, F16 토큰 임베딩, F32 121개), I2_S 텐서 210개의 2비트 코드가 모두 같으며, 스케일은 F16 반올림 오차(4.6e-4) 안이고, 나머지 텐서는 같았습니다. `41 1`(스레드 하나)로 만든 파일도 바이트 단위로 같았습니다. AVX2, `sse42`, `x64` 변형과 `llama-server`(26 토큰/초)에서 `The capital of France is Paris.`라고 답했습니다.
    - 질문 다섯 개(설명, 산술, 목록, 번역, 코드)를 greedy(`--temp 0`, `llama-cli`)로 원본 파일과 비교했습니다. 네 개는 응답이 같았습니다. 코드 질문(팩토리얼 함수)은 다르지만 역시 올바른 응답을 냈습니다. 재귀 대신 반복문을 썼습니다.
    - 원인은 F16 입력입니다. F16 값에는 F16으로 반올림된 스케일이 들어 있으므로, 새 스케일은 원본 float 스케일과 최대 4.6e-4만큼 다르고, 이 정도로도 긴 응답에서 토큰 하나의 선택이 바뀔 수 있습니다. 새 파일에 원본 스케일을 복사하자 응답이 같아졌습니다. bitnet.cpp처럼 F32 파일에서 양자화하면 이 반올림이 생기지 않습니다.
  - **STQ1_0.** Tencent의 1.25비트 파일을 F16으로 풀었다가 다시 STQ1_0으로 양자화했습니다. STQ1_0 텐서 224개가 모두 원본과 바이트 단위로 같았고(타입 42가 43으로 기록됨), 번역도 같았습니다. 손실 타입을 재양자화하면 그렇듯이 Q6_K 출력 텐서만 달랐습니다. 번역 다섯 개(영어→한국어 두 개, 한국어·중국어→영어, 영어→일본어)는 원본, 왕복 파일, Q4_K_M으로 재양자화한 파일에서 모두 같았습니다.
  - **재양자화.** STQ1_0에서 Q4_K_M으로는 되고 번역도 같습니다. I2_S에서 다른 타입으로는 `type i2_s unsupported for integer quantization: no dequantization available`로 멈춥니다.
- **기타 도구.** `llama-quantize --help`가 STQ1_0(44)과 I2_S(41)를 보여 줍니다. `llama-server.exe`, `llama-cli.exe`, `llama-quantize.exe`에 아이콘이 들어 있습니다.

## 패치

패치는 이름 순서대로 적용합니다. 각 패치는 관련 변경을 번호 붙은 파트로 묶으며, 각 패치 파일의 헤더(영어)가 모든 파트를 설명합니다.

| 파일 | 파트 |
| --- | --- |
| `0001-stq1_0.patch` | 1. STQ1_0(PR #22836), 2. STQ1_0용 x86 SIMD vec_dot, 3. 타입 42를 쓰는 STQ1_0 파일 |
| `0002-bitnet-i2s.patch` | 1. BitNet 저장소 없이 빌드, 2. ggml-base의 I2_S 기준 양자화, 3. llamafile 없는 I2_S 빠른 경로, 4. 비 AVX2 I2_S 대체 코드, 5. squared ReLU, 6. 채팅 템플릿, 7. 프리토크나이저(5~7은 BitNet b1.58 2B-4T용), 8. I2_S 타입 특성, 9. I2_S용 llama-quantize |
| `0003-windows-build.patch` | 1. 모든 실행 파일과 DLL의 Windows 아이콘과 버전 정보, 2. 지원되지 않는 MSVC 옵션, 3. MSVC 변환 경고, 4. Visual Studio 2019 STL에서의 jinja |
| `0004-embeddinggemma2.patch` | 1. `gemma-embedding2` 아키텍처, 2. EmbeddingGemma 2 텍스트 인코더(업스트림 #30054 백포트) |

## 0001-stq1_0.patch

### 파트 1: STQ1_0(PR #22836)

이 파트는 ARM NEON `vec_dot` 커널과 함께 STQ1_0 삼진(ternary) 양자화 타입(가중치당 1.3125비트)을 추가합니다. [ggml-org/llama.cpp#22836](https://github.com/ggml-org/llama.cpp/pull/22836)의 PR head `1e411d8f5a1e23525fa3265dfb4bd76265465397`에서 가져왔습니다. 이 PR은 열려 있고 업스트림에 병합되지 않았습니다. STQ1_0은 Tencent AngelSlim의 1.25비트 양자화를 llama.cpp에서 쓰는 형식이며, Hy-MT2 1.25비트 모델 카드도 필요한 커널로 이 PR을 안내합니다.

PR은 업스트림 llama.cpp 기준으로 작성되었으므로 포크로 리베이스했습니다. PR과의 차이는 다음과 같습니다:

#### 타입 ID

llama.cpp는 타입에 두 개의 별도 열거형으로 번호를 매깁니다:

- **ggml 텐서 타입**(`enum ggml_type`, `ggml.h`). GGUF 파일에 텐서마다 저장되며, 로더가 텐서 데이터를 읽을 때 씁니다.
- **llama 파일 타입**(`enum llama_ftype`, `llama.h`). `general.file_type` 메타데이터에 한 번 저장되며, 파일 전체의 양자화를 나타내고, `llama-quantize --help`에 표시되는 번호입니다.

포크의 ID는 그대로 둡니다. STQ1_0은 다음 빈 ID를 씁니다:

- 포크는 `I2_S`, `I8_S`, `TL1`, `TL2`에 ggml 타입 36~38과 42를, `I2_S`, `TL1`, `TL2`에 llama 파일 타입 41~43을 씁니다. `Q2_0`과 `F8_E4M3`은 없습니다.

| 타입 | ggml 텐서 타입 | llama 파일 타입 |
| --- | --- | --- |
| 여기의 `STQ1_0` | 43(PR과 같음) | **44**(PR은 42를 쓰는데, 포크에서는 `TL1`) |
| `GGML_TYPE_COUNT` | 44 | |

`gguf-py/gguf/constants.py`에도 같은 변경을 합니다. 코드의 모든 타입 표는 열거형 이름으로 인덱싱하므로(지정 초기화), 번호를 쓰는 것은 열거형 정의뿐입니다.

Tencent 파일은 다른 ID를 씁니다:

- `tencent/Hy-MT2-1.8B-1.25Bit-GGUF`는 STQ1_0 텐서를 ggml 타입 **42**(텐서 오프셋으로 측정한 256 가중치당 42바이트)로, `general.file_type`을 **41**로 저장합니다.
- 여기서 42는 `TL2`, 41은 `I2_S`입니다. 파트 3이 텐서 크기로 둘을 구분합니다. 파트 3이 없으면 파일을 불러오지 못합니다("failed to load model").

호환성:

- PR로 STQ1_0 양자화한 GGUF 파일(타입 43)과 Tencent 파일(타입 42) 모두 이 빌드에서 불러와집니다.
- `llama-quantize`에서 번호로 타입을 고를 때, 여기서 44는 STQ1_0입니다. 타입 이름을 쓰세요. [llama-quantize로 양자화하기](#llama-quantize로-양자화하기)를 참고하세요.
- PR이 다른 ID로 업스트림에 병합되면, 이 빌드의 STQ1_0 파일은 공식 빌드와 맞지 않게 됩니다.

#### 기타 조정

- 포크에는 `Q2_0` 항목이 없으므로 컨텍스트 줄을 조정했고, STQ1_0 줄은 `TQ2_0` 줄 뒤에 옵니다.
- `ggml/src/ggml-cpu/arch-fallback.h`: 포크의 x86 부분에는 이미 빈 `quants.c` 블록이 있고, 여기에 STQ1_0 일반 매핑이 들어갑니다.
- `gguf-py/gguf/constants.py`: 포크는 `LlamaFileType`을 두 번 정의합니다(두 번째 정의가 이김). `MOSTLY_STQ1_0`을 둘 다에 추가합니다.

PR의 빠른 커널은 ARM NEON 전용이며, 파트 2가 x86용을 추가합니다.

#### llama-quantize로 양자화하기

PR에는 `llama-quantize` 지원(`STQ1_0`, 여기서는 파일 타입 44)이 들어 있습니다. PR의 안내는 Sherry로 학습한 체크포인트(예: `AngelSlim/Hy-MT1.5-1.8B-1.25bit`)를 `convert_hf_to_gguf.py`로 BF16 GGUF로 변환한 뒤 `llama-quantize <bf16.gguf> <out.gguf> STQ1_0`을 실행하는 것입니다.

- STQ1_0은 이미 3:4 희소 삼진 구조를 따르는 가중치를 담습니다. 즉 가중치 4개 묶음(64개 청크 안에서 간격 16)마다 정확히 하나가 0입니다. 이런 체크포인트의 가중치는 손실 없이 저장됩니다.
- 다른 모델에는 맞지 않습니다. 양자화하면 묶음마다 하나를 강제로 0으로 만들기 때문에, 삼진 모델이라도 3:4 희소가 아니면 망가집니다. BitNet b1.58 2B-4T를 STQ1_0으로 양자화하면 한 단어만 반복했습니다.
- STQ1_0 파일을 다른 타입으로 재양자화하는 것은 됩니다. STQ1_0에는 `to_float`가 있기 때문입니다.

### 파트 2: STQ1_0용 x86 SIMD vec_dot

이 파트는 `ggml/src/ggml-cpu/arch/x86/quants.c`에 `ggml_vec_dot_stq1_0_q8_K`를 추가합니다.

- 이 커널이 없으면 x86은 가중치를 하나씩 디코딩하는 일반 코드를 실행합니다. 이 경우 1.25비트 Hy-MT2 모델은 약 2 토큰/초를 생성했고, 같은 모델의 Q4_K_M 파일은 27 토큰/초였습니다.
- 이 커널은 SSE4.1이나 AVX가 있는 CPU, 즉 `sse42`, `sandybridge`, 그 이상 변형에서 쓰입니다. MSVC는 `__SSE4_1__`을 정의하지 않으므로 `GGML_SSE42`도 확인합니다. 다른 CPU(`x64` 변형)는 여전히 일반 코드를 씁니다.
- 이 패치는 파트 1이 추가한 `arch-fallback.h`의 x86 줄(일반 함수를 이 이름에 매핑하던 줄)을 제거합니다.

동작 방식:

1. 64 가중치 청크마다 그룹 `g`(0~15)는 가중치 `g + p*16`(`p` = 0~3)을 담습니다. 그래서 16바이트 벡터의 레인 `g`가 그룹 `g`입니다.
2. 커널은 청크의 5비트 코드북 인덱스(4비트 코드와 부호 비트) 16개를 만듭니다. 코드북 절반마다 하나씩 `_mm_shuffle_epi8` 두 번으로 찾아보고, 부호 비트로 둘 중 하나를 고릅니다.
3. `_mm_maddubs_epi16`으로 각 2비트 필드를 활성값 16개와 곱합니다.
4. 코드는 -1/0/+1에 대해 0/1/2이므로, 결과는 `sum(q * y) - sum(y)`이며 `sum(y)`는 q8_K의 `bsums`에서 얻습니다.

연산이 정수이므로 결과는 일반 코드와 같습니다. AVX2, `sandybridge`, `sse42`, `x64` 변형에서 번역이 같았습니다.

### 파트 3: 타입 42를 쓰는 STQ1_0 파일

이 파트는 타입 ID를 바꾸지 않고, 타입 42를 쓰는 STQ1_0 파일을 빌드가 읽을 수 있게 합니다.

ggml 타입 42에는 두 가지 의미가 있습니다:

- 포크의 `GGML_TYPE_TL2`(bitnet.cpp의 룩업 테이블 타입),
- Tencent 파일(`tencent/Hy-MT2-1.8B-1.25Bit-GGUF`)의 STQ1_0.

이 빌드는 포크의 `TL2` = 42를 유지하고, PR처럼 STQ1_0에 43을 씁니다. 그래서 어느 번호 체계를 따르는 파일이든 의미가 유지됩니다.

**텐서 타입(`ggml/src/gguf.cpp`).** 두 형식은 크기가 다르고, GGUF 파일의 텐서 오프셋은 텐서 크기와 맞아야 합니다. `gguf_init_from_file_impl`은 텐서 정보를 읽은 직후 이를 검사합니다. 그 검사 전에, 패치는 타입 42 텐서가 있는 파일을 살펴봅니다:

- 오프셋이 `TL2` 크기와 맞으면 아무것도 바꾸지 않습니다.
- STQ1_0 크기와만 맞으면 타입 42 텐서를 STQ1_0(43)으로 읽습니다. 이때 로그에 `N tensors of type 42 have the size of stq1_0, not tl2; reading them as stq1_0`이 나옵니다.
- 어느 쪽과도 맞지 않으면 기존 오프셋 검사가 이전처럼 실패합니다.

**파일 타입(`src/llama-model-loader.cpp`).** `general.file_type`에도 번호 세 개에 같은 문제가 있으므로, 로더는 텐서 타입을 보고 판단합니다:

| `general.file_type` | 여기서의 의미 | 다른 쪽 사용처 | 표시 |
| --- | --- | --- | --- |
| 41 | `MOSTLY_I2_S` | Tencent의 STQ1_0 파일 | 파일에 STQ1_0 텐서가 있고 I2_S 텐서가 없으면 `MOSTLY_STQ1_0` |
| 40 | `MOSTLY_Q1_0` | `microsoft/BitNet-b1.58-2B-4T-gguf` | 파일에 I2_S 텐서가 있고 Q1_0 텐서가 없으면 `MOSTLY_I2_S` |
| 42 | `MOSTLY_TL1` | PR #22836으로 만든 STQ1_0 파일(그쪽은 `MOSTLY_STQ1_0` = 42, 여기는 44) | 파일에 STQ1_0 텐서가 있고 TL1 텐서가 없으면 `MOSTLY_STQ1_0` |

파일 타입은 메타데이터일 뿐입니다.

테스트한 것:

- Tencent 파일은 STQ1_0으로 읽히고 파일 타입 `STQ1_0 - 1.31 bpw ternary`를 보여 줍니다.
- BitNet 파일은 파일 타입 `I2_S`를 보여 줍니다(패치 전: `Q1_0`).
- PR #22836처럼 `general.file_type`이 42인 STQ1_0 파일은 파일 타입 `STQ1_0 - 1.31 bpw ternary`를 보여 줍니다(이전: `TL1`).
- 텐서 번호를 43으로 바꾼 같은 파일은 판별 없이 불러와지고 같은 번역을 냅니다.
- TL2 크기의 타입 42 텐서를 넣은 합성 파일은 그 텐서를 `tl2`로 유지합니다.

## 0002-bitnet-i2s.patch

### 파트 1: BitNet 저장소 없이 빌드

이 파트는 TL1/TL2가 꺼져 있을 때 포크가 BitNet 저장소에 의존하지 않게 합니다:

- **CMake 경로.** `ggml/src/CMakeLists.txt`는 `GGML_BITNET_ARM_TL1`이나 `GGML_BITNET_X86_TL2`가 켜져 있을 때만 `../../../../include`(BitNet의 헤더)를 `ggml-base`에 추가합니다. `ggml/src/ggml-cpu/CMakeLists.txt`도 모든 CPU 변형에서 `../../../../src/ggml-bitnet-lut.cpp`와 `../../../../include`에 대해 같은 처리를 합니다.
  - 패치가 없으면 포크의 CMake는 TL1/TL2가 꺼져 있어도 BitNet 저장소에 있는 이 경로를 참조합니다. TL1/TL2가 꺼져 있으면 그 소스 파일은 빈 코드로 컴파일되고 헤더는 포함되지 않습니다.
- **`gemm-config.h`.** I2_S 커널이 이 헤더를 포함하므로, 패치는 `ggml-cpu-i2s.c` 옆에 `ggml/src/ggml-cpu/gemm-config.h`를 추가합니다.
  - `0b341e5` 시점의 BitNet `include/gemm-config.h`(MIT, Copyright (c) Microsoft Corporation)에 끝부분 대체 정의를 더한 것입니다.
  - 이 헤더는 AVX/SSSE3(또는 NEON)가 있는 CPU에서만 `ROW_BLOCK_SIZE`와 `COL_BLOCK_SIZE`를 정의합니다. `ggml-cpu-i2s.c`의 비 AVX2 경로가 이 값을 쓰므로, `GGML_CPU_ALL_VARIANTS`의 기본 `x64` 변형과 AVX 미만의 모든 MSVC 변형(MSVC는 `__SSSE3__`을 정의하지 않음)은 컴파일되지 않았을 것입니다. 대체 정의는 x86 값을 씁니다.
  - BitNet의 `utils/tune_gemm_config.py`는 블록 크기를 조정하려고 이 파일을 다시 씁니다. 이 빌드는 공개된 값을 그대로 씁니다.

TL1이나 TL2를 켜고 BitNet 저장소 안에서 포크를 빌드하면, CMake 변경은 아무 효과가 없고 복사한 헤더가 `ggml-cpu-i2s.c`에 대해 BitNet의 헤더를 대신합니다(값은 같음).

### 파트 2: ggml-base의 I2_S 기준 양자화

이 파트는 `dequantize_row_i2_s`와 `quantize_i2_s`를 `ggml/src/ggml-cpu/quants.c`(CPU 백엔드)에서 `ggml/src/ggml-quants.c`(`ggml-base`)로 옮기고 `GGML_API`로 선언합니다.

- `ggml-base`의 일부인 `ggml.c`는 `ggml_quantize_chunk`에서 `quantize_i2_s`를 씁니다. 포크에서는 `dequantize_row_i2_s`도 I2_S `to_float`로 등록했는데, 이는 파트 8이 제거합니다.
- 포크는 이 함수들을 CPU 백엔드에 정의합니다. 정적 빌드나, 라이브러리를 불러올 때 정의되지 않은 심볼을 해결하는 Linux의 공유 빌드에서는 링크됩니다.
- Windows의 `GGML_BACKEND_DL`에서는 CPU 변형마다 별도 DLL이고, `ggml-base.dll`은 그 DLL에 링크할 수 없습니다. 패치가 없으면 MSVC 빌드가 이 두 심볼에 대해 `LNK2001`/`LNK2019`로 실패합니다.

두 함수 모두 단순한 스칼라 C이므로 그대로 옮깁니다. 이후 파트 9가 `quantize_i2_s`를 다시 씁니다. CPU 백엔드(`ops.cpp`가 `dequantize_row_i2_s`를 씀)는 다른 기준 양자화 함수처럼 이 함수들을 `ggml-base`에서 가져옵니다.

### 파트 3: llamafile 없는 I2_S 빠른 경로

이 파트는 `ggml_compute_forward_mul_mat`에 있는 포크의 I2_S GEMV/GEMM 빠른 경로가 `src1_cont` 대신 `ggml_is_contiguous(src1)`를 호출하게 합니다.

- `src1_cont`는 그 위의 `#if GGML_USE_LLAMAFILE` 블록 안에서만 선언됩니다.
- `ggml-cpu.c`는 대상이 `__ARM_FEATURE_SVE`나 `__ARM_FEATURE_MATMUL_INT8`을 지원하면 `GGML_USE_LLAMAFILE`을 해제합니다. ARM64 빌드(`-march=armv8.7-a`)는 "use of undeclared identifier 'src1_cont'"로 실패했습니다.
- x86 빌드에서는 `GGML_USE_LLAMAFILE`이 정의된 채로 남으므로 이 문제가 드러나지 않습니다.

### 파트 4: 비 AVX2 I2_S 대체 코드

이 파트는 `x64`, `sse42`, `sandybridge` CPU 변형(그리고 AVX2가 없는 모든 CPU)이 I2_S에 쓰는 코드를 고칩니다. 이 파트가 없으면 2B-4T 모델은 이 변형들에서 `?` 토큰만 생성했습니다.

- **`ggml_vec_dot_i2_i8_s`**(`ggml-cpu/quants.c`). 스칼라 대체 코드는 AVX2 코드와 세 가지가 달랐습니다:
  - 2비트 코드를 순서대로 읽었습니다. I2_S는 128개 가중치를 32바이트에 끼워 넣습니다. 바이트 `gp`는 가중치 `gp`, `gp+32`, `gp+64`, `gp+96`을 비트 7-6, 5-4, 3-2, 1-0에 담으며, `dequantize_row_i2_s`도 이렇게 읽습니다.
  - 코드를 -1/0/+1로 변환했습니다. AVX2 코드는 부호 없는 코드 0/1/2를 쓰고, 호출하는 쪽이 활성값 합계를 뺍니다.
  - 행 `r`의 주소를 `r * bx / 4`가 아니라 `r * (n/4)`로 계산했습니다.

  패치는 이 함수를 AVX2 코드와 같은 것을 계산하도록 다시 씁니다.
- **`ggml_gemm_i2_i8_s`**(`ggml-cpu/ggml-cpu-i2s.c`, 4토큰 이상의 프롬프트에 쓰임). `gemm-config.h`가 설정하는 `ACT_PARALLEL`에서, 대체 코드는 `ggml_vec_dot_i2_i8_s`가 출력 간격을 두고 활성값 열을 순회하는 것처럼 호출했습니다. 실제로 이 함수는 가중치 행을 순회하고 결과를 연속으로 씁니다. 패치는 활성값 열마다 한 번씩 호출하며, 이는 AVX2 코드의 배치 `s[col * bs + row]`와 같습니다.

패치를 적용하면 `x64` 변형이 AVX2 변형과 같은 토큰을 생성합니다.

### 파트 5: BitNet b1.58 2B-4T의 squared ReLU

이 파트는 `bitnet-b1.58` 아키텍처가 피드포워드 네트워크에서 `LLM_FFN_RELU_SQR`를 쓰게 합니다.

- 포크는 `bitnet-b1.58`(`microsoft/BitNet-b1.58-2B-4T-gguf`의 아키텍처)을 FFN이 SiLU를 쓰는 `bitnet` 그래프로 빌드합니다.
- 2B-4T 모델은 squared ReLU를 씁니다. `microsoft/bitnet-b1.58-2B-4T`의 `config.json`에서 `hidden_act`가 `relu2`입니다.
- 이전 `bitnet_b1_58` 모델(`bitnet`)은 SiLU를 유지합니다.

원인을 찾은 방법:

- SiLU에서는 모델이 첫 문장을 답한 뒤 `<|eot_id|>`를 내지 않고 토큰 한도까지 반복했습니다.
- I2_S 가중치를 F16으로 변환한 GGUF가 F16 커널로 정확히 같은 토큰을 생성했습니다. 따라서 I2_S 커널은 원인이 아니었습니다.
- squared ReLU에서는 모델이 답한 뒤 `<|eot_id|>`로 멈춥니다.

### 파트 6: BitNet b1.58 2B-4T의 채팅 템플릿

이 파트는 `microsoft/BitNet-b1.58-2B-4T-gguf`의 채팅 템플릿을 원본 모델의 템플릿으로 바꿉니다.

- GGUF 파일에는 모델이 따르지 않는 `Human: ...\n\nBITNETAssistant: `가 들어 있습니다. `/v1/chat/completions`로 요청하면 답하는 대신 대화를 지어냅니다.
- 원본 모델(`microsoft/bitnet-b1.58-2B-4T`, `tokenizer_config.json`)은 `User: ...<|eot_id|>Assistant: `를 씁니다.
- `llama_model_chat_template`은 아키텍처가 `bitnet-b1.58`이고 내장 템플릿에 `BITNETAssistant`가 들어 있으면 원본 템플릿을 돌려줍니다. 이 함수에는 이미 Mistral-Small-2503에 대한 같은 종류의 일회성 수정이 있습니다.
- `--chat-template`과 `--chat-template-file`이 여전히 우선합니다.

### 파트 7: BitNet b1.58 2B-4T의 프리토크나이저

이 파트는 파일에 `tokenizer.ggml.pre`가 없을 때 `bitnet-b1.58`에 `llama-bpe` 프리토크나이저를 줍니다.

- `microsoft/BitNet-b1.58-2B-4T-gguf`에는 `tokenizer.ggml.pre`가 없습니다. 그러면 llama.cpp는 `default` 프리토크나이저로 대체하고, 생성 품질이 떨어진다고 경고합니다.
- 원본 모델은 Llama 3 토크나이저를 씁니다. 테스트에서 `default`는 `I'm`, `they'll`, `won't`, 빈 줄을 `llama-bpe`와 다른 토큰으로 나눴습니다.
- `tokenizer.ggml.pre`를 설정한 파일이나 `--override-kv tokenizer.ggml.pre=...`는 영향을 받지 않습니다.

### 파트 8: I2_S 타입 특성

이 파트는 clang이 보고하는, `ggml/src/ggml.c`에 있는 포크의 `type_traits` 표의 두 가지 문제를 고칩니다:

- **`-Winitializer-overrides`.** 항목 `[36]`, `[37]`, `[38]`(제거된 `IQ4_NL_4_4`, `IQ4_NL_4_8`, `IQ4_NL_8_8`)을 초기화한 뒤, 같은 번호를 쓰는 포크의 `I2_S`, `I8_S`, `TL1`이 덮어썼습니다. 이 파트는 자리 표시용 항목 세 개를 제거합니다.
- **`-Wcast-function-type-mismatch`.** `I2_S`가 `(ggml_to_float_t) dequantize_row_i2_s`를 등록했지만, `dequantize_row_i2_s`는 네 번째 인자로 텐서 스케일을 받습니다.
  - 스케일은 텐서 전체의 압축된 데이터 뒤에 저장되므로, 행 포인터와 개수만 받는 `ggml_to_float_t`로는 넘길 수 없습니다. 이 형식으로 호출하면 스케일이 정의되지 않은 값이었습니다.
  - 이 파트는 `to_float`를 `NULL`로 둡니다. `get_rows`와 `mul_mat`은 `I2_S`를 직접 처리합니다.
  - `to_float`가 필요한 코드는 잘못된 값을 만드는 대신 이 타입을 거부합니다. 예를 들어 `llama-quantize`의 재양자화는 `NULL`인지 확인합니다.

### 파트 9: I2_S용 llama-quantize

이 파트는 `llama-quantize`가 I2_S 파일을 만들 수 있게 합니다. bitnet.cpp의 `setup_env.py`는 `llama-quantize <f32> <out> I2_S 1`을 실행하지만, 포크에서는 실패했습니다:

- **I2_S 옵션이 없음.** `llama-quantize`가 `I2_S`와 `41`을 거부했고("invalid ftype"), `llama-quant.cpp`는 파일 타입을 텐서 타입으로 변환하지 않았습니다. 포크에는 파일 타입 `LLAMA_FTYPE_MOSTLY_I2_S`와 `ggml_quantize_chunk`의 I2_S 분기는 있습니다.
- **배치가 다름.** 포크의 `quantize_i2_s`는 2비트 코드를 순서대로 기록했지만, 커널과 `dequantize_row_i2_s`는 128개 가중치를 32바이트에 끼워 넣은 배치(바이트 `gp`에 가중치 `gp`, `gp+32`, `gp+64`, `gp+96`)를 읽습니다. 그 결과물은 잘못된 값을 냈을 것입니다.
- **텐서당 스케일 하나.** `llama-quantize`는 스레드마다 텐서를 청크로 나누는데, 청크마다 자기 스케일을 기록했을 것입니다.
- **입력을 읽지 못함.** `ggml_validate_row_data`가 타입 36을 거부해서 `llama-quantize`가 I2_S 파일을 아예 읽지 못했습니다. 재양자화하면 `to_float`가 없는 타입에 대한 메시지(파트 8) 대신 `invalid type 36`으로 실패했습니다.

변경 사항:

- `quantize_i2_s`가 커널의 배치로 코드를 기록하고, 마지막 32바이트의 나머지를 0으로 채웁니다(원본 파일은 그중 28바이트를 초기화하지 않음).
  - 가중치는 BitNet b1.58 변환기가 쓰는 것처럼 이미 삼진, 즉 텐서마다 `s` 하나인 `{-s, 0, +s}`여야 합니다.
  - `s`는 가장 큰 절댓값입니다. 크기가 `s/2`보다 큰 가중치는 +1이나 -1이 되고, 나머지는 0이 됩니다.
- I2_S 텐서는 한 번의 호출(스레드 하나)로 양자화합니다. `ggml_quantize_chunk`는 I2_S가 0번 행에서 시작하는지 확인합니다.
- `llama-quant.cpp`가 파일 타입을 `GGML_TYPE_I2_S`로 변환합니다.
  - 출력 가중치와 토큰 임베딩은 BitNet b1.58 GGUF 파일처럼 기본으로 F16을 씁니다(삼진 가중치가 아님). BitNet b1.58 2B-4T처럼 토큰 임베딩을 출력과 공유하는 모델은 그러지 않으면 출력 텐서의 Q6_K가 됩니다. `--output-tensor-type`과 `--token-embedding-type`은 여전히 적용됩니다.
  - 행이 128의 배수가 아닌 텐서와 3D(전문가) 텐서는 Q4_0으로 대체합니다.
- `llama-quantize`가 I2_S를 보여 줍니다.
- `ggml_validate_row_data`가 I2_S(스케일을 확인함)와 다른 BitNet 타입을 받아들입니다.

## 0003-windows-build.patch

### 파트 1: Windows 아이콘과 버전 정보

llama.cpp에는 Windows 리소스 파일이 없으므로, 실행 파일과 DLL에 아이콘도 버전 정보도 없습니다(파일 속성의 "설명"이 비어 있음). 이 파트는 최상위 `CMakeLists.txt`의 끝에서 불러오는 `cmake/win-resources.cmake`를 추가하며, 캐시 옵션은 다음과 같습니다:

| 옵션 | 빌드 스크립트가 설정하는 값 | 효과 |
| --- | --- | --- |
| `LLAMA_WIN_ICON` | `app\favicon.ico` | 실행 파일에 아이콘을 애플리케이션 아이콘으로 넣습니다. |
| `LLAMA_WIN_DESCRIPTION` | `llama.cpp built for WelsonJS` | 모든 실행 파일과 DLL에 `VERSIONINFO` 리소스를 추가합니다. |
| `LLAMA_WIN_VERSION` | WelsonJS 버전 `0.2.7.62`(런처의 `AssemblyFileVersion`, 또는 `-ProductVersion`) | `FileVersion`과 `ProductVersion`(기본값 `0.0.<LLAMA_BUILD_NUMBER>.0`). |
| `LLAMA_WIN_COMPANY` | `Catswords Research` | `CompanyName`. |
| `LLAMA_WIN_COPYRIGHT` | 코드의 저작자들(아래) | `LegalCopyright`. |

`LegalCopyright` 값은 빌드에 코드가 들어간 모든 저작자를 적습니다:

```
Copyright (c) 2023-2026 The ggml authors (llama.cpp), Copyright (c) Microsoft Corporation (bitnet.cpp), Copyright (c) 2025 Tencent (AngelSlim, STQ1_0), Copyright (c) 2026 Catswords Research (WelsonJS)
```

- **The ggml authors:** llama.cpp와 ggml(MIT, 출력물의 `LICENSE-llama.cpp.txt`).
- **Microsoft Corporation:** 포크에 들어 있는 bitnet.cpp의 I2_S 타입과 커널, `gemm-config.h`(MIT).
- **Tencent:** AngelSlim 1.25비트 양자화의 llama.cpp 형식인 STQ1_0(llama.cpp PR #22836).
- **Catswords Research:** WelsonJS 패치와 빌드.

각 바이너리의 `VERSIONINFO` 리소스에는 다음 값이 들어갑니다:

| 값 | 예(`llama-server.exe`) | 예(`ggml-cpu-haswell.dll`) |
| --- | --- | --- |
| `FileDescription`("설명"으로 표시) | `llama-server - OpenAI-compatible LLM server (llama.cpp built for WelsonJS)` | `ggml-cpu-haswell - ggml CPU backend, haswell variant (llama.cpp built for WelsonJS)` |
| `FileVersion`, `ProductVersion` | `0.2.7.62` | `0.2.7.62` |
| `CompanyName` | `Catswords Research` | `Catswords Research` |
| `LegalCopyright` | 위의 값 | 위의 값 |
| `ProductName` | `llama.cpp` | `llama.cpp` |
| `InternalName`, `OriginalFilename` | `llama-server`, `llama-server.exe` | `ggml-cpu-haswell`, `ggml-cpu-haswell.dll` |
| `Comments` | `llama.cpp built for WelsonJS. llama.cpp 390c307, build 1.` | 같음 |

`FileDescription`의 역할 문구는 `ggml tensor library`, `ggml base library`, `ggml CPU backend, <변형> variant`, `llama.cpp library`, `llama.cpp common library`, `llama.cpp multimodal library`, `<도구> implementation library`, `OpenAI-compatible LLM server`, `LLM command-line interface`, `GGUF model quantizer`입니다. 역할을 모르는 대상은 `<대상> (<텍스트>)`가 됩니다.

패치가 하는 일:

- 모든 디렉터리에서 빌드의 실행 파일, 공유 라이브러리, 모듈 라이브러리를 모두 모아(ggml, ggml-base, CPU 백엔드, llama, llama-common, mtmd, 도구와 그 `-impl` 라이브러리) 각자의 리소스 스크립트(`<build>/win-resources/<target>.rc`)를 줍니다. 실행 파일에는 아이콘도 넣습니다.
- 아이콘은 스크립트 옆에 복사됩니다. 스크립트는 `#pragma code_page(65001)`로 시작하므로, ANSI 코드 페이지와 상관없이 경로에 비 ASCII 문자가 있는 빌드 폴더에서도 동작합니다.
- `FILEOS`와 `FILETYPE`은 `winver.h`의 이름 대신 숫자 값(실행 파일은 `VFT_APP`, DLL은 `VFT_DLL`)을 쓰므로, CMake가 어떤 리소스 컴파일러를 고르든 포함 경로가 필요 없습니다.
- 스크립트는 `configure_file`로 씁니다. `configure_file`은 내용이 바뀔 때만 파일을 건드리므로, CMake를 다시 실행해도 다시 링크하지 않습니다.
- `Comments`의 빌드 번호는 체크아웃의 커밋 수입니다. 얕은 git 클론은 1, 압축 파일 소스는 0입니다.

아이콘 파일은 WelsonJS 저장소에 그대로 있고, 패치는 옵션만 추가합니다. `LLAMA_WIN_ICON`과 `LLAMA_WIN_DESCRIPTION`이 비어 있으면(기본값) 패치가 없는 빌드와 같습니다. 아이콘 파일이 없거나 버전 형식이 틀리면 구성 단계가 오류로 멈춥니다.

빌드 스크립트는 출력물을 검사합니다. 모든 실행 파일과 DLL에 위의 `FileDescription`, 버전, `CompanyName`, `LegalCopyright`가 있어야 하며, 하나라도 없으면 빌드가 실패합니다.

### 파트 2: 지원되지 않는 MSVC 옵션

이 파트는 `ggml/src/ggml-cpu/CMakeLists.txt`에서 MSVC 경고 D9002("알 수 없는 옵션 무시")의 원인 두 가지를 제거합니다:

| 옵션 | 문제 | 변경 |
| --- | --- | --- |
| `/arch:SSE4.2`(`sse42` CPU 변형) | 최신 MSVC에만 있습니다. MSVC 19.29(Visual Studio 2019, AppVeyor 이미지)는 이 변형의 모든 소스 파일에서 이를 무시합니다. MSVC 19.50은 받아들입니다. | `check_c_compiler_flag`가 받아들일 때만 추가합니다. CMake는 D9002를 검사 실패로 셉니다. |
| `-fno-lto`(CPU 기능 감지 객체, `ggml-cpu-*-feats`) | GCC/clang 옵션이라 모든 MSVC가 무시합니다. | MSVC가 아닐 때만 넘깁니다. MSVC는 `/GL`을 주지 않으면 LTO를 쓰지 않습니다. |

`/arch:SSE4.2`가 없으면 `sse42` 변형은 SSE2 코드 생성으로 컴파일됩니다. 이는 안전합니다. MSVC는 어떤 `/arch`에서도 SSE4 내장 함수를 받아들이고, 이 변형은 실행 시 SSE4.2가 있는 CPU에서만 선택됩니다. AppVeyor 이미지에서 SSE4 명령어를 쓰는 자동 벡터화만 잃습니다.

### 파트 3: MSVC 변환 경고(C4244)

이 파트는 MSVC가 `llama`, `mtmd` 등의 대상에서 보고하는 많은 C4244 경고("데이터 손실 가능성")를 처리합니다. MSVC 19.50에서는 이런 줄이 31개이고, MSVC 19.29(AppVeyor)는 더 많이 보고합니다.

- **ggml처럼 끔.** 거의 모두 무해하며 업스트림 llama.cpp에서도 같습니다. 예:
  - `tools/mtmd/mtmd-audio.cpp`에서 `float` 벡터를 `0.0`이나 `0`으로 `std::fill`,
  - `src/llama-model.cpp`에서 작은 `int64_t`로 만드는 `std::pair<int64_t, uint32_t>`.

  `ggml/CMakeLists.txt`는 이미 ggml 대상에 대해 C4244, C4267, C4305 등을 끕니다. 패치는 최상위 `CMakeLists.txt`에서 `src`, `common`, `tools`를 추가하기 전에 같은 설정을 합니다. MSVC에만 적용되므로 clang(ARM64) 빌드에는 영향이 없습니다.
- **실제 잘림 하나를 고침.** `src/llama-quant.cpp`(`llama_tensor_dequantize_impl`)에서 작업자 람다가 원소 수를 `int`로 받았지만, 실제로는 `size_t`가 넘어옵니다. 람다가 호출하는 함수 `to_float`, `ggml_fp16_to_fp32_row`, `ggml_bf16_to_fp32_row`는 `int64_t`를 받습니다.
  - 원소가 2^31 - 1개를 넘는 스레드는 잘못된 개수를 받게 됩니다. `llama-quantize`가 적은 스레드로 매우 큰 텐서를 재양자화할 때 생길 수 있습니다.
  - 패치는 매개변수를 `int64_t`로 바꿉니다.

### 파트 4: Visual Studio 2019 STL에서의 jinja

이 파트는 `common/jinja/value.h`의 `value_func_t`를 바꿉니다. 이 코드는 함수 타입 `func_hptr`로 `std::function::target<func_hptr>()`을 호출했습니다.

- clang과 MSVC 14.29 STL(Visual Studio 2019) 조합에서 AppVeyor의 ARM64 빌드가 `reinterpret_cast from 'const void *' to '... (*)(const jinja::func_args &)' casts away qualifiers`로 실패했습니다.
- MSVC 14.44와 14.51 STL은 이를 컴파일하므로, Visual Studio 2022나 2026이 있는 PC에서는 드러나지 않았습니다.
- 이 호출은 일치하는 경우도 없었습니다. `std::function`은 일반 함수를 함수 포인터로 저장하므로 대상 타입은 `func_hptr *`입니다.

패치는 `target<func_hptr *>()`을 요청해 저장된 함수 포인터를 돌려주는(다른 호출 가능 객체면 `nullptr`) `function_pointer()`를 추가합니다. `unique_hash()`와 `equivalent()`가 이를 씁니다. 두 함수의 주석은 함수 포인터 비교를 설명합니다. 둘 다 지금은 쓰이지 않습니다.

## 0004-embeddinggemma2.patch

포크는 EmbeddingGemma 2([google/embeddinggemma-2](https://huggingface.co/google/embeddinggemma-2), Apache-2.0)보다 오래되었습니다. 이 모델의 GGUF 파일(예: [ggml-org/embeddinggemma-2-GGUF](https://huggingface.co/ggml-org/embeddinggemma-2-GGUF))은 아키텍처가 `gemma-embedding2`입니다. 이 패치는 업스트림 llama.cpp `4fbc76dec5`([ggml-org/llama.cpp#30054](https://github.com/ggml-org/llama.cpp/pull/30054), MIT)의 C++ 부분을 백포트해 `llama-server --embedding`이 이 모델의 텍스트 임베딩을 제공하게 합니다. 런처는 설치 구성 요소 `llamacpp\embeddinggemma_2`를 이렇게 실행합니다([docs/llamacpp-launcher-api.md](../../docs/llamacpp-launcher-api.md#embeddings) 참고).

### 파트 1: gemma-embedding2 아키텍처

`LLM_ARCH_GEMMA_EMBEDDING2`를 `LLM_ARCH_GEMMA_EMBEDDING` 옆에 등록합니다. `llama-arch`의 이름, `llama_model_mapping`의 모델 클래스, KV 캐시가 없는 모델(`create_memory`), NEOX rope 모델(`llama_model_rope_type`)입니다.

### 파트 2: EmbeddingGemma 2 텍스트 인코더

`src/models/gemma-embedding2.cpp`와 `models.h`의 `llama_model_gemma_embedding2`입니다. 양방향 Gemma 4 계열 인코더(24층, 은닉 크기 512)로, 여섯 층 중 다섯 층에 대칭 슬라이딩 윈도 어텐션을 쓰고, 층마다 헤드 크기와 KV 헤드 수가 다르며, Q/K/V RMS 정규화, 투영만으로 만드는 층별 입력, 층마다 학습된 출력 배율, 마지막 은닉 상태를 768차원 임베딩으로 바꾸는 투영을 씁니다. 풀링(평균)은 GGUF에서 읽습니다. 모델이 쓰는 나머지(`gemma4` 토크나이저, 층별 입력, 층별 헤드 크기)는 포크에 이미 있습니다(Gemma 3n, Gemma 4).

파일은 업스트림 그대로이며, 포크 API에 맞춘 두 가지만 다릅니다.

- 포크에는 `llama_model_base::load_swa_pattern`이 없습니다. 슬라이딩 윈도 패턴을 GGUF에 저장된 대로 층마다 하나씩인 플래그로 읽거나(`get_arr`), 주기(기본 6)로 읽습니다(`set_swa_pattern`). 업스트림의 `load_swa_pattern`이 하는 일과 같습니다.
- 포크의 `build_inp_embd`는 배율을 받지 않습니다. 토큰 임베딩을 그 뒤에 `sqrt(n_embd)`로 곱하며, 포크의 `gemma-embedding.cpp`처럼 토큰 입력일 때만 곱합니다.

`models/*.cpp`는 `src/CMakeLists.txt`의 `file(GLOB)`로 모이므로 새 파일에 CMake 변경이 필요 없습니다. 비전·오디오 인코더(mmproj 파일)와 Python 변환은 패치에 들어 있지 않습니다. GGUF 파일은 업스트림에서 변환합니다.

## 업데이트 방법

1. 새 BitNet 커밋이 쓰는 llama.cpp 커밋을 찾습니다: `git ls-tree <BitNet sha> 3rdparty/llama.cpp`와 BitNet의 `.gitmodules`. `commit.txt`를 그 커밋으로 설정하고, 포크 URL이 바뀌었으면 스크립트의 `RepositoryUrl`도 바꿉니다. `version.txt`를 `<릴리스>.<커밋 앞 일곱 글자>.1`로 바꿉니다. `<릴리스>`는 포크가 갈라지기 전의 마지막 upstream 릴리스입니다. 갈라진 지점은 `gh api repos/ggml-org/llama.cpp/compare/master...<포크 소유자>:llama.cpp:<sha>`의 `merge_base_commit`이고, 릴리스는 `gh api repos/ggml-org/llama.cpp/compare/bNNNN...<갈라진 지점>`이 `behind_by` 0을 돌려주는 가장 큰 `bNNNN` 태그입니다.
2. 그 커밋의 소스 압축 파일을 올리고(`git archive --format=zip --prefix=llama.cpp-<sha>/ <sha>`), `source-archive.txt`의 URL과 SHA-256을 업데이트합니다.
3. 깨끗한 체크아웃에 각 패치가 `git -c core.autocrlf=false apply --check`로 적용되는지 확인합니다. 필요하면 패치를 리베이스하고, `ggml.h`, `llama.h`, `gguf-py/gguf/constants.py`의 타입 ID를 포크의 타입과 대조합니다.
4. 포크가 여전히 BitNet 저장소의 모든 사용을 TL1/TL2 옵션 뒤에 두는지(`0002-bitnet-i2s.patch` 파트 1), 그리고 `I2_S` 소스가 모든 CPU 변형에 컴파일되는지 확인합니다.
5. 스크립트로 x64, arm64, x86을 로컬에서 빌드합니다. 다음을 확인합니다:
   - `llama-quantize --help`가 STQ1_0을 보여 줌,
   - `llama-server`가 설치되는 모델 중 하나와 `I2_S` 모델로 채팅 요청에 답함,
   - `llama-server --embedding -m embeddinggemma-2-Q8_0.gguf`가 `POST /v1/embeddings`에 입력마다 768개 값으로 답함.
6. 세 빌드를 게시하고(`native\publish-prebuilt.ps1`, [CI](#ciappveyor) 참고) 새 `prebuilt\<arch>.sha256` 파일과 함께 커밋합니다. 바뀐 파일 때문에 CI 캐시가 무효화되어 CI가 새 빌드를 받습니다.
