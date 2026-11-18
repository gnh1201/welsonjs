# 변경 내역

[English version](CHANGELOG.md)

이 파일은 WelsonJS의 주요 변경 사항을 기록합니다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/)를 따릅니다.

- 각 버전은 이 저장소의 Git 태그이며, 날짜는 태그 날짜입니다.
- 내용은 GitHub 릴리스 노트, 병합된 풀 리퀘스트, 태그 사이의 커밋 이력에서 가져왔습니다. 릴리스 노트가 없는 릴리스는 커밋을 바탕으로 정리했습니다.
- 변경이 이슈와 연결되는 경우 이슈 번호를 링크했습니다. 릴리스 노트가 이슈를 명시했거나, 커밋이나 풀 리퀘스트가 이슈를 참조했거나, ("관련"으로 표시) 변경이 이슈에 적힌 내용을 구현하는 경우입니다.

태그에 관한 참고 사항:

- `0.7.34`는 0.2.7.34 버전의 태그입니다(이름이 잘못 붙음). 이 무렵 버전이 `0.2.7.x`가 아니라 `2.7.x`로 표시되는 문제도 있었으며, 0.2.7.36에서 고쳤습니다.
- 0.2.7.35, 0.2.7.58, 0.2.7.59에는 태그가 없습니다.
- `0.2.5`, `0.2.5.2`, `0.2.7.5-PRE`, `0.2.7.33`, `0.7.34`에는 GitHub 릴리스 노트가 없습니다.

## [Unreleased]

### 추가

- 프레임워크 자체 구현 검사 모듈 `lib/is.js`를 추가했습니다. `app.js`가 전역 `is`로 제공하던 is.js 0.9.0의 메서드 이름과 동작을 외부 코드 없이 유지합니다. 타입·존재·산술·형식·문자열·날짜·객체·배열 검사와 `not`·`all`·`any` 인터페이스를 제공합니다. 런타임이 Windows Script Host이므로 브라우저·기기 판별 묶음은 넣지 않았고, 그 자리에 호스트 기준의 `is.windows()`·`is.linux()`·`is.mac()`·`is.online()`·`is.offline()`을 두었습니다. 임베디드 브라우저는 `lib/browser.js`로 확인합니다([#434] 관련).
  - `is.url()`과 `is.email()`은 정규식 대신 길이를 제한한 값을 왼쪽에서 오른쪽으로 한 번만 훑어 해석하므로 악의적인 입력으로 되감기(backtracking)가 일어나지 않습니다. 나머지 형식 검사는 되감기가 없는 고정 패턴과 길이 제한을 씁니다.
- `data/test-misc.json`과 `testloader.js`에 `is_test`를 추가했습니다. 모듈 동작과 악의적인 입력에 대한 `is.url()` 응답 시간을 확인합니다: `cscript app.js testloader is_test data\test-misc.json`([#434] 관련).

### 변경

- 설치 프로그램의 `microsoft_jdk` 구성 요소가 JDK를 설치하는 대신 압축을 풀어 둡니다. `postInstall.ps1`이 배포사의 `.zip`을 내려받아 `%APPDATA%\welsonjs\jdk\` 아래에 zip 안의 최상위 폴더(`jdk-25.0.5.0.2+1` 또는 Zulu 패키지 이름)를 그대로 두고 풉니다. 이어서 현재 사용자용 `JAVA_HOME`을 그 폴더로 등록하고, 그 폴더의 `bin`을 사용자 `PATH`에 같은 항목이 두 번 들어가지 않도록 추가합니다. 프로그램 파일에는 아무것도 쓰지 않고 프로그램 및 기능 항목도 생기지 않으며, JDK는 스크립트가 내려받는 다른 구성 요소들과 같은 폴더에 있어 그 폴더를 지우면 함께 사라집니다. `%APPDATA%`는 사용자별 경로이므로 설치 관리자가 관리자 권한으로 돌아도 두 값은 사용자 범위에 씁니다. 압축은 먼저 임시 폴더에 풀고 `bin\java.exe`를 확인한 뒤에야 기존 JDK를 바꾸므로, 압축이 실패하면 이전 설치가 그대로 남습니다. 임시 폴더와 압축 파일은 뒤에 정리합니다. 이 구성 요소의 이전 버전이 `%APPDATA%\welsonjs\microsoft_jdk\`에 남긴 것(그 아래에 풀어 둔 JDK, 또는 설치 파일만 두던 시절의 파일)과 그 폴더를 가리키던 `PATH` 항목도 새 JDK를 등록한 뒤에 함께 지웁니다. 무인 설치 스위치·종료 코드·다시 시작 처리를 쓰던 `.exe`·`.msi` 분기는 없앴고, Microsoft 패키지가 만들던 `.jar` 연결도 함께 없어졌습니다. `setup.iss`의 구성 요소 설명도 하는 일을 그대로 적습니다.
- `data/DownloadUrls.psd1`: `microsoft_jdk`의 세 아키텍처 주소가 모두 배포사 설치 파일이 아니라 `.zip` 패키지입니다. x64·ARM64는 JDK 25 LTS의 현재 Windows 패치인 Microsoft Build of OpenJDK 25.0.5.0.2이고, x86은 Windows i686용 Azul Zulu 17 LTS 17.0.21.1입니다. JDK 25에는 32비트 빌드가 없기 때문입니다(JEP 503으로 x86 포트 제거). Zulu 17.0.21.1은 2026년 11월 CPU에서 JDK 17 수정이 반영되는 패치 수준입니다. 주소가 비어 있던 `arm64`를 채우고, AArch64 설치 파일을 가리키던 `x86`을 바로잡았습니다. `microsoft_jdk_sha256` 항목은 이 파일에서 빠집니다. 이 구성 요소를 포함한 모든 해시가 `data/DownloadHashes.txt`에 있습니다.
- `data/DownloadUrls.psd1`: `curl`(x64·ARM64), `android_platform_tools`, `nuget` 주소를 항상 최신을 가리키는 링크 대신 버전이 박힌 파일로 바꿨습니다. `curl.se/windows/dl-8.23.0_9/curl-8.23.0_9-win64-mingw.zip`·`...-win64a-mingw.zip`은 curl 8.23.0(curl-for-win 빌드 8.23.0_9), `dl.google.com/android/repository/platform-tools_r37.0.1-win.zip`은 Android platform-tools r37.0.1(Google SDK 저장소의 안정 채널 릴리스이며, r37.0.3은 미리보기 채널에만 있습니다), `dist.nuget.org/win-x86-commandline/v8.0.0/nuget.exe`는 NuGet 8.0.0입니다. 네 주소 모두 2026-11-18에 `data/DownloadHashes.txt`를 만들 때 이전 링크가 내려주던 바이트와 같으므로 해시 목록은 55개 값을 그대로 유지하지만, 이제 해시는 배포사가 다음에 올릴 파일이 아니라 그대로 남는 파일을 가리킵니다. 아직 최신을 가리키는 주소는 5개입니다. Hugging Face `resolve/main` GGUF 모델 4개와 LDPlayer의 `LDPlayer9.exe?n=…`입니다.

### 수정

- 커스텀 설치에서 구성 요소를 하나도 고르지 않으면 모든 구성 요소를 설치하던 문제를 고쳤습니다. 이 선택에서 `WizardSelectedComponents`는 빈 문자열을 돌려주고, `postInstall.ps1`은 이를 "필터 없음"으로 읽어 전체 선택으로 처리했습니다. 이제 `setup.iss`가 `none`을 넘기고 스크립트는 빈 선택으로 읽습니다. `-Components` 없이 직접 실행하는 경우는 예전처럼 전체 구성 요소를 뜻합니다.
- 프로세서 조회가 실패할 때 `Get-NativeArchitecture`가 64비트 운영체제를 모두 x64로 답하던 대신 프로세스 환경에서 아키텍처를 읽습니다. ARM64 기기에서 x64용 빌드를 내려받지 않습니다.
- `curl` 구성 요소가 다시 압축을 풉니다. `postInstall.ps1`은 압축 파일을 `curl.zip`으로 저장했는데 압축 해제 단계는 주소에서 이름을 뽑아(`curl-latest.cgi`) 그 이름으로 파일을 찾다가 없다고 판단해 구성 요소를 건너뛰었습니다. 그래서 2026-10-05(`73d1de98`) 이후 이 구성 요소는 내려받고 해시를 확인한 뒤에도 설치되지 않았습니다. 이제 두 단계가 새 `Get-DownloadFileName` 하나로 주소에서 이름을 정하므로 저장한 이름 그대로 압축을 풀고, `curl`은 `%APPDATA%\welsonjs\curl\`에 `bin\curl.exe`와 함께 다시 설치됩니다(그 리팩터링 전과 같은 위치).

### 보안

- `postInstall.ps1`이 내려받는 모든 파일을 `data/DownloadHashes.txt`와 대조합니다. 이 새 파일은 SHA-256 값만 한 줄에 하나씩 담습니다. `Download-File`이 `Get-FileHash`(없는 PowerShell에서는 .NET 클래스)로 파일을 해싱하고, 값이 목록에 없으면 파일을 즉시 지우고 실제 해시를 로그에 남긴 뒤 오류를 발생시키므로 목록에 없는 파일은 실행되거나 압축이 풀리지 않습니다. 목록에는 `data/DownloadUrls.psd1`에 있는 55개 다운로드(모든 구성 요소·모든 아키텍처)의 해시가 들어 있고, 2026-11-18에 각 주소가 실제로 내려준 바이트에서 계산했습니다. 그래서 검사가 JDK 하나가 아니라 표 전체를 덮습니다. 목록은 실행할 때 한 번 읽어 대소문자를 구분하지 않는 집합으로 만들고, `data/DownloadUrls.psd1`에는 주소만 남으며 `microsoft_jdk_sha256` 항목은 없앴습니다. JDK 압축 파일은 압축을 풀기 직전에 다시 확인합니다. 파일이 사용자 `%TEMP%`에 있는 동안 스크립트가 관리자 권한으로 도는 경우가 있기 때문입니다. 해시 파일이 없는 실행은 그 사실을 알리고 검증 없이 내려받습니다.
- `WelsonJS.Service/app.config`에서 Google.Protobuf와 gRPC 어셈블리 5개의 바인딩 리디렉션을 제거했습니다. 해당 패키지들은 0.2.7.62에서 하트비트 gRPC 클라이언트와 함께 빌드에서 빠졌으므로 리디렉션만 남아 있었습니다([#434] 관련).
- 패치 버전이 없는 is.js 0.9.0(CVE-2020-26302, `is.url()`의 ReDoS) 의존성을 제거했습니다. 무결성 사이드카 2개와 `app/assets/integrity.json`의 항목 2개를 없애 더 이상 내려받거나 불러오지 않습니다. 전역 `is`는 `lib/is.js`가 계속 제공하므로 `is`를 쓰던 애플리케이션은 그대로 동작합니다([#434] 관련).
- 2026-08-05 오픈소스SW 검증에서 보고된 구성 요소 3개의 현황을 `docs/third-party-dependencies.md`에 정리했습니다. IE 6~8에 필요한 jQuery 1.11.3 폴백을 아직 제거할 수 없는 이유도 함께 적었습니다([#434] 관련).

## [0.2.7.62] - 2026-11-16

설치 파일 없이 배포한 사전 릴리스 0.2.7.62-rc1 이후의 변경 사항입니다. 0.2.7.61에서 업그레이드한다면 [0.2.7.62-rc1](#0262-rc1---2026-11-01)도 함께 확인하세요.

업그레이드 전에 확인할 점:

- 설치 프로그램이 런처 설정 파일(`bin\x86\WelsonJS.Launcher.exe.config`)을 새 파일로 바꿉니다. IP 조회 API 키(`IpQueryApiKey`, `IpQueryApiKey2`)나 `ResourceServerApiKey`처럼 직접 바꾼 값은 미리 적어 두었다가 업그레이드 후 다시 설정하세요. 이번 릴리스에서 제거한 설정(아래 참고)은 더 이상 읽지 않습니다.
- 이전 런처가 띄운 브라우저 창(에디터와 웹 채팅 창)을 닫으세요. 새 런처는 자기 브라우저를 새로 띄웁니다.

### 추가

- WelsonJS Editor의 Local AI: WelsonJS용 llama.cpp 빌드(`native/llamacpp`). AppVeyor Release 작업에서 소스로부터 빌드해 캐시하고, 다른 산출물과 함께 서명하며, artifacts 패키지에서 `%APPDATA%\WelsonJS\llamacpp`로 설치합니다.
  - 소스는 Microsoft가 bitnet.cpp와 함께 배포하는 llama.cpp 포크이므로 BitNet b1.58(I2_S) 모델이 실행됩니다([#519] 관련).
  - 패치로 Tencent AngelSlim의 1.25비트 STQ1_0 타입을 추가해 `tencent/Hy-MT2-1.8B-1.25Bit-GGUF`가 실행됩니다. 타입 42 파일은 포크의 타입 번호를 바꾸지 않고 텐서 크기로 판별합니다.
  - x64(MSVC, 모든 CPU 변형)와 ARM64(clang 크로스 컴파일)용으로 빌드합니다([#40] 관련).
  - 모든 실행 파일과 DLL에 버전 정보가 들어갑니다. WelsonJS 버전(0.2.7.62), 바이너리 설명("... (llama.cpp built for WelsonJS)"), `CompanyName` Catswords Research, 그리고 ggml authors(llama.cpp), Microsoft(bitnet.cpp), Tencent(AngelSlim, STQ1_0), Catswords Research의 저작권입니다. 실행 파일에는 WelsonJS 아이콘도 들어갑니다.
  - 두 아키텍처의 모든 실행 파일과 DLL을 서명합니다. `.artifact-configuration.xml`에 하나씩 이름으로 적으며(와일드카드 없음), 바이너리에 버전 정보가 없거나 서명 목록에 없으면 빌드가 실패합니다.
  - `llama-quantize`로 I2_S 파일을 만들 수 있습니다. 포크의 양자화기는 커널이 읽지 않는 배치로 기록했고, 도구의 타입 목록에도 없었습니다. STQ1_0 양자화는 Sherry로 학습한 체크포인트용이라고 문서화했습니다.
  - README에 `llama-quantize`로 양자화하는 방법과 예시를 넣었습니다. 파일 타입 번호가 빌드마다 다르므로 타입은 이름(`I2_S`, `STQ1_0`)으로 지정합니다.
- 에디터의 AI 서비스 토글로 Local AI 모델(llama.cpp: 기본값인 Hy-MT2, FrogNano, BitNet, 런처 설정 `LlamaCppModels`)과 웹 채팅 서비스(Copilot, ChatGPT 등)를 전환합니다. 컨텍스트 메뉴에는 고른 서비스의 Send Selection, Explain Selection, Find Problems in Selection(문제점 찾기), 시스템 언어와 영어(en)로 번역하는 `Translate Selection into <언어> (<코드>)`, Write Code(JavaScript, WSH JScript 엔진용 ES3), Ask...가 표시되고, 프롬프트와 함께 시스템 언어를 보냅니다(코드가 아닌 부분에만 쓰도록 지시). 서비스를 바꾸면 프롬프트를 초기화합니다.
- OpenAI 호환 런처 API: llama.cpp 모델과 웹 채팅 서비스(Copilot, ChatGPT, Claude 등)에 쓰는 `POST /v1/chat/completions`와 `GET /v1/models`. 웹 채팅은 이제 런처가 브라우저를 직접 조작하며, 서비스 정의는 에디터에서 `webchat.js`로 옮겼고 에디터도 이 API를 씁니다. 응답은 스트리밍하지 않습니다(`"stream": true`이면 완성된 답을 청크 하나로 보냄). 다른 로컬 소프트웨어(예: 브라우저 엔진 기반 앱)는 런처 API 키(`ResourceServerApiKey`, `Authorization: Bearer`로 전송)로 쓸 수 있습니다. `docs/openai-compatible-api.md`를 참고하세요.
- 언어 추론 엔진에 UberLLM 제공자 추가([#543]).
- 프롬프트를 검사하는 런처 관리 LLM 가드(Qwen3Guard).
- 모든 릴리스 태그를 다루는 영어·한국어 변경 내역(`CHANGELOG.md`, `CHANGELOG_KO.md`).

### 변경

- 새 설치 구성 요소 `llamacpp\bitnet_b1_58_2b_4t`: WelsonJS llama.cpp 빌드가 실행하는 BitNet b1.58 2B-4T(Microsoft, 1.58비트 I2_S, `microsoft/bitnet-b1.58-2B-4T-gguf`, 약 1.2GB)([#519] 관련).
- Hy-MT2 기본 모델이 Q4_K_M(약 1.1GB) 대신 1.8B 1.25비트 GGUF(462MB)입니다. 구성 요소 이름은 `hy_mt2_1_8b_1_25bit`입니다([#542] 관련).
- 설치 프로그램은 더 이상 공식 llama.cpp 패키지를 받지 않고, artifacts 패키지의 WelsonJS 빌드를 씁니다. Qwen3Guard는 llama.cpp 구성 요소 아래로 묶었습니다.
- 런처 에디터의 AI 도구는 로컬 llama.cpp 서버를 씁니다. 런처의 Copilot 메뉴와 `CopilotUrl` 설정은 제거했습니다.
- llama.cpp는 `llama-server` 하나를 라우터 모드(모델 hot swap)로 실행하며, 에디터의 Local AI, LLM 가드, `lib/llamacpp.js`가 함께 씁니다. 모델은 `%APPDATA%\WelsonJS`에서 `.gguf` 파일이 있는 폴더이고 이름은 설치 구성 요소이며, 모든 요청은 모델을 명시합니다. `LlamaCppModelsMax`(기본값 3)를 넘으면 가장 오래 쓰지 않은 모델을 내립니다.
  - FrogNano와 BitNet은 영어에서 가장 잘 동작하므로, 이 모델들(`LlamaCppTranslatedModels`)에는 런처가 사용자 메시지를 Hy-MT2(`LlamaCppTranslationModel`)로 영어로 번역하고(코드 블록과 인라인 코드는 그대로), 사용자의 시스템 언어만 알려 줍니다(답변 언어에 관한 지시는 없음). 답변은 번역하지 않습니다. 에디터의 번역 항목은 Hy-MT2로 바로 보냅니다.
  - 런처 API: `/llamacpp/status`, `models`, `start`, `stop`, `load`, `unload`, `chat`(`model` 필수). 로컬 호출자만 쓸 수 있습니다. `docs/llamacpp-launcher-api.md`를 참고하세요.
  - 설정: `LlamaCppModelPath`와 `LlmGuardModelPath` 대신 `LlamaCppModels`(에디터 모델 목록, 쉼표로 구분)와 `LlmGuardModel`(모델 이름)을 씁니다. `LlamaCppModelsMax`와 `LlamaCppContextSize`를 추가했습니다.
  - `lib/llamacpp.js`는 모델을 이름으로 고릅니다(`setModel`, `loadModel`, `unloadModel`, `listModels`, `chat`). 모델 경로, 포트, `openBrowser` 옵션은 제거했습니다.
- 런처의 자식 프로세스(llama.cpp 라우터와 모델 인스턴스, 터미널 세션, JSRT 인스턴스)를 Windows Job Object에 넣어, 런처가 강제 종료되거나 디버거에서 중지되거나 비정상 종료돼도 함께 종료되게 했습니다.
- 런처가 브라우저를 시작할 때 고정된 9222 대신 비어 있는 원격 디버깅 포트를 고르고(`ChromiumDevToolsPrefix` 설정 제거), 브라우저는 127.0.0.1에서만 받게 했습니다. 포트는 브라우저가 응답하는 동안 런처 메모리에만 유지하며, 이전 런처 실행에서 남은 브라우저는 쓰지 않습니다(그 창들을 닫아야 함). llama.cpp 라우터와 브라우저의 포트는 하나의 공통 구현(`LocalPorts`)에서 다룹니다.
- 런처의 WHOIS와 IP 조회 엔드포인트가 호출하는 외부 서비스 주소를 로컬 설정 대신 blob 설정(`blob.config.xml`의 `services`)에서 가져옵니다(`WhoisServerUrl`, `WhoisReferrerUrl`, `WhoisClientAddress`, `IpQueryApiPrefix`, `IpQueryApiPrefix2` 제거). IP 조회 API 키는 로컬 설정에 남습니다. blob 설정을 받기 전에 온 요청은 로드를 기다리고, 로드에 실패하면 다음 요청 때 다시 받습니다. WHOIS 엔드포인트가 공유 HTTP 클라이언트에 Referrer를 설정하던 문제도 고쳤습니다.
- 런처의 DNS 조회 도구가 모든 레코드 타입을 동시에 조회하고(타입마다 최대 5초씩 차례로 기다리던 것을 비동기, 시도당 최대 3초로), 잘린 응답은 TCP로 다시 묻고, 응답이 요청과 맞는지 확인하며, 압축된 이름을 올바르게 읽습니다. IP 주소는 역방향(PTR)으로 조회하고, 국제화 도메인은 punycode로 바꾸며, TTL과 SOA·NAPTR·여러 문자열로 된 TXT의 모든 필드를 보여 주고, 레코드가 없는 타입은 한 줄로 모아 보여 줍니다. `DnsServerAddress`에 쉼표로 여러 서버를 적을 수 있으며(기본값 `1.1.1.1,8.8.8.8`), 한 서버가 응답하지 않거나 실패·거부하면 다음 서버에 묻고, 모든 서버에 한 번씩 물은 뒤에 다시 시도합니다. `?type=`으로 레코드 타입을 고를 수 있습니다.
- 벤더 JavaScript 파일을 필요할 때 복원합니다(`restoreAppAssets.ps1`).
- 부트스트랩에서 `app.hta`를 `welsonjs-hta.exe`로 엽니다. YAWW를 업데이트했습니다.

### 수정

- `WelsonJS.ManagedObject` COM 브리지 개선과 누수 수정([#429]), .NET Framework 2.0용 빌드 수정.
- x86 환경에서 `welsonjs-hta.exe`의 버그.
- `file:` URL에서 스크립트 디렉터리를 디코딩하는 문제.
- `resolvePictureSize`의 그림 크기 처리.
- llama.cpp: PR #22836처럼 파일 타입이 42인 STQ1_0 파일이 `TL1`로 표시되던 문제. 이제 STQ1_0으로 표시됩니다.
- `lib/aviation.js`가 괄호 누락으로 문법 오류가 나 `require("lib/aviation")`이 실패하던 문제. `limit`과 `offset`을 10진수로 파싱하고(JScript 엔진은 0으로 시작하는 문자열을 8진수로 읽음), `offset`을 `limit`이 아닌 `offset`에서 파싱합니다(radix 제보: [@avelankwon](https://github.com/avelankwon)).

### 보안

- 런처의 `ResourceServer`를 강화했습니다.

## [0.2.7.62-rc1] - 2026-11-01

사전 릴리스입니다. 설치 프로그램이 제공되지 않으므로 소스에서 빌드해야 합니다.

### 추가

- YAWW 기반 WebView 지원과 WebView2, EdgeHTML, MSHTML용 빌드 구성([#157], [#510] 관련).
- AppVeyor 빌드에서 Inno Setup으로 설치 프로그램 패키징([#486]).
- MCP(Model Context Protocol) 개선: 알림 전달, 세션 상태, 선택적 클라이언트 설정, Claude Code·Codex CLI·Copilot CLI로 검증([#474]).
- JSON-RPC 2.0 메서드 등록, 전달, 요청별 상태.
- Microsoft Office 도우미: PowerPoint 슬라이드와 내보내기, Word 문서 수명 주기, Office 도형.
- HWP 자동화 지원과 구성 요소.
- 언어 추론 엔진에 Cheaper Inference([#523])와 OrcaRouter 통합.
- 오브젝트 스토리지 엔드포인트 장애 조치, TLS 1.3 설치 옵션, Scrapingant 프록시, 다운로드 가능한 GGUF 모델 추가(예: SmolLM2 135M).
- MCP 사용을 위한 `SKILL.md`와 한국어 기여 안내서.

### 변경

- 관리 구성 요소를 `native/` 아래로 재구성하고, ManagedEsent와 서브모듈을 업데이트했습니다.
- `WelsonJS.Service`의 gRPC 외부 의존성을 줄였습니다([#477]).
- JavaScript eval 도구에 `allowUnsafeEval` 게이트를 추가하고, ES3 평가기의 eval을 제한했습니다.
- CodeQL 워크플로, 아키텍처·기여 안내서, README를 업데이트했습니다.

### 수정

- 핵심 모듈의 사소한 버그와 잠재적 보안 문제.

## [0.2.7.61] - 2026-08-24

### 추가

- MsCompress(MSCOMP/SZDD) 압축과 압축 해제.
- `WelsonJS.Toolkit`에 있던 API를 `WelsonJS.ManagedObject`에 추가.

### 보안

- 취약점 [GHSA-9jmm-5v6v-gpq2](https://github.com/gnh1201/welsonjs/security/advisories/GHSA-9jmm-5v6v-gpq2)(KVE-2026-2271)를 수정했습니다. 이전 버전 사용자는 업그레이드해야 합니다.

## [0.2.7.60] - 2026-08-17

### 추가

- `WelsonJS.Toolkit`의 후속인 `WelsonJS.ManagedObject`, NuGet에 게시([#440], [#427]).
  - `ByRef` 매개변수를 위한 COM 래퍼 지원([#429]).
  - `WelsonJS.ManagedObject`가 만든 객체 추적([#449]).
- `WelsonJS.Launcher`에 AppSignal 기반 오류 추적.
- 경량 SRI 지원([#420])과 SHA-512/SHA-384 해시 알고리즘([#424]), CAPICOM 대체.
- 네트워크로 DLL을 불러오는 동적 어셈블리 로더 `Catswords.Phantomizer`와 유연한 스킴([#368]).
- 원클릭 설치 스크립트([#404]).
- JavaScript만으로 구현한 Stdin 서버([#397]).
- MCP(Model Context Protocol) 지원([#233]).
- 런처 원격 측정, TLS 보안 프로토콜 확인, TLS 1.2 오프라인 검사 도구, Outlook(클래식) 통합.

### 변경

- 네이티브 솔루션 이름을 `WelsonJS.Toolkit`에서 `WelsonJS.Augmented`로 바꿨습니다.
- 로깅을 log4net으로 옮긴 뒤, 파일 기반 `TraceLogger`로 바꿨습니다.

### 수정

- 일부 인터넷 차단 환경에서 동적 어셈블리 로더 문제([#412]).
- `htmlfile` 모듈이 손상된 시스템에서의 충돌([#399]).
- Windows 8.1에서 심리스 런처와 설치 후 스크립트([#376], [#374]).
- KISA CRC 체크리스트에 따른 시큐어 코딩 수정([#411] 관련).

## [0.2.7.57] - 2025-12-01

### 추가

- 설치 후 스크립트에서 용도에 따른 선택적 바이너리 다운로드: Python, cURL, websocat, YARA, WAMR, tessdata, GTK3 런타임, Nmap, WinDivert, Android 플랫폼 도구 등([#259]).
- 설치 후 스크립트의 실험적 원격 측정(PostHog).

### 변경

- 설치 후 스크립트를 개선하고(PR [#353]), 바이너리를 분리해 패키지 크기를 줄였습니다([#259]).

## [0.2.7.56] - 2025-11-24

### 추가

- WelsonJS JCTG(JavaScript-Click-To-Go): 파일 연결로 파일 탐색기에서 `.js` 파일을 두 번 클릭해 실행합니다.
- `Enumerator` 객체용 `Array.from()`.
- 32자 키 TOTP, ChakraCore 기반 직렬화, 이미지 색상 선택 도구, Chromium 앱 모드, `ResourceServer`의 CORS 지원, 여러 IP 조회 제공자.

### 변경

- 설치 프로그램 크기를 58MB에서 8MB로 줄였습니다.
- `Enumerator.toArray`와 `getFilesFromFolder`(이제 `getFiles`)를 사용 중단으로 표시했습니다.

### 수정

- 보고된 여러 문제와 사소한 버그.

## [0.2.7.55] - 2025-08-18

### 추가

- `using` 키워드와 비슷한 `UseObject(progId, callback)`.
- ESENT(Windows 내장 데이터베이스) 지원과 런처의 ESENT 기반 메타데이터 저장소([#277]).
- `WelsonJS.Cryptography`: 공식 테스트 벡터를 통과한 SEED, ARIA, HIGHT([#201], [#293], [#295]).
- 런처의 DevTools 프로토콜용 WebSocket 지원.
- 에디터에 Azure AI(Azure AI Foundry), Criminal IP 등 LLM·API 통합 추가. WHOIS·DNS 결과를 LLM 서비스로 넘길 수 있습니다.
- 오프라인에서도 코드 에디터를 제공하는 캐시 블롭([#220]).
- Chromium 기반 브라우저 추가.

### 변경

- WelsonJS Editor가 React를 씁니다.
- 기본 SERP 제공자가 SerpApi입니다.

### 수정

- CDP 포트 바인딩과 브라우저 테스트 실패([#246], [#251]).
- 이전 `chrome.js`와 `std.js`의 호환성([#271]), `--user-data-dir` 플래그가 null이 되는 문제([#274]).
- `System.IO.Compression.ZipFile`과의 잠재적 라이선스 충돌([#254]).
- 서브모듈을 제거해 GitHub Desktop으로 복제가 느린 문제([#283]).

## [0.2.7.54] - 2025-03-23

### 추가

- Microsoft Monaco Editor 기반의 리치 코드 에디터 WelsonJS Editor([#137])와 설치된 소프트웨어 목록 기반 코드 자동 완성([#183]).
- 에디터의 생성형 AI 통합(ChatGPT, Gemini 등)과 코드 생성.
- 런처의 WHOIS·DNS 조회, TOTP 엔드포인트, 트레이 아이콘, 전역 설정 창.

### 변경

- 런처의 시작 시간과 설정을 개선했습니다.

## [0.2.7.53] - 2024-11-24

### 추가

- `lib/http`의 프록시 유형 `stateless-jsonrpc2`.

### 변경

- `lib/http`의 검색 엔진 감지를 업데이트했습니다.

## [0.2.7.52] - 2024-11-19

### 추가

- numbers.js를 이용한 수학 메서드.

## [0.2.7.51] - 2024-11-15

### 수정

- 로그 유형이 표시되지 않는 문제(0.2.7.50의 회귀).

## [0.2.7.50] - 2024-11-15

### 추가

- 고급 수학 연산을 위한 `numbers.js` 라이브러리.
- Groq LLM API 지원.

### 변경

- `console` 객체의 타입 처리를 개선했습니다.
- `assets/js`의 일부 JavaScript 라이브러리 파일 이름을 바꿨습니다.

## [0.2.7.49] - 2024-11-05

### 변경

- `EncryptStringHIGHT`/`DecryptStringHIGHT`를 `EncryptString`/`DecryptString`으로 바꿨습니다.
- `WelsonJS.Toolkit` NuGet 패키지를 업데이트했습니다.

## [0.2.7.48] - 2024-11-04

### 변경

- 화면 배율이 100%가 아니면 스크린 타임 기능이 경고합니다.

### 수정

- 화면 동작 중복 처리 방지.

## [0.2.7.47] - 2024-11-04

### 추가

- Anthropic(Claude) API 통합.

### 수정

- 설정과 로거 관련 버그.

## [0.2.7.46] - 2024-11-02

### 변경

- `WelsonJS.Service`가 `Microsoft.Extensions.Logging`으로 로그를 남깁니다([#154]).

## [0.2.7.45] - 2024-10-24

### 추가

- `WelsonJS.Toolkit`의 NuGet 프로필.

## [0.2.7.44] - 2024-10-21

### 추가

- `WelsonJS.Launcher`의 사용자 정의 변수 편집기(`.env` 파일 기반).
- 대화형 서비스의 `--env-file` 매개변수.

### 변경

- 문서를 catswords-oss.rdbl.io로 옮겼습니다.
- 스크린 타임 기능과 SERP를 개선했습니다.

## [0.2.7.43] - 2024-10-03

### 추가

- SERP 감지에 빠진 매개변수.

### 변경

- 새 WelsonJS 파비콘.

## [0.2.7.42] - 2024-09-27

### 수정

- 기본 엔진 모드(MSXML)에서 프록시 URL이 적용되지 않는 문제.

## [0.2.7.41] - 2024-09-27

### 추가

- SearchAPI.io SERP 지원.

### 변경

- `CharsetDetector`를 `DetectCharset`으로 바꿨습니다.

### 수정

- 프록시 없이 `.send()`가 동작하지 않는 문제([#145]).

## [0.2.7.40] - 2024-09-26

### 수정

- 탭 UI에서 탭이 끝없이 복제되는 문제.
- 셸 요청 뒤 캐시된 `stdout`과 `stderr`가 지워지지 않는 문제.

## [0.2.7.39] - 2024-09-26

### 추가

- 탭 기반 UI(Golden Layout).

### 수정

- `webloader.js`에서 `console.warn()`의 식별자.

## [0.2.7.38] - 2024-09-25

### 추가

- 프록시 API 서비스 통합([#143]).
- `lib/http`의 ARM 지원.

## [0.2.7.37] - 2024-09-25

### 추가

- 프록시 API 서비스 첫 부분([#143]).
- 파일 이벤트 모니터의 ClamAV 통합과 CRC32 기반 비트맵 비교.

### 수정

- `stdout`과 `stderr`를 읽기 전에 데이터가 없어지는 문제([#144]).

## [0.2.7.36] - 2024-09-09

### 수정

- gRPC 기반 하트비트 서비스(PR [#139]).
- 파일 이벤트 모니터(파일 생성, 네트워크 연결, 레지스트리 수정).
- 버전이 `0.2.7.x`가 아니라 `2.7.x`로 표시되는 문제.

## [0.7.34] - 2024-09-04

0.2.7.34 버전입니다.

### 추가

- 스크린 타임 기능의 OCR(tessdata)과 클립보드 샘플링 옵션, 하트비트 gRPC 정의.

### 수정

- `STAThreadAttribute` 문제.

## [0.2.7.33] - 2024-08-30

### 추가

- 파서 생성기(PEG.js)([#135]).

### 변경

- .NET으로 옮긴 gRPC 서비스를 리팩터링했습니다([#132]).

## [0.2.7.32] - 2024-08-27

### 수정

- gRPC 클라이언트가 HTTP/1.1을 씁니다.
- 초기 설정을 `settings.ini`에서 읽습니다.

## [0.2.7.31] - 2024-08-25

### 추가

- 독립 실행형 `WelsonJS.Launcher` 프로그램.
- YARA 파일 규칙 매처([#131]).

### 변경

- gRPC 서비스를 .NET으로 옮겼습니다([#132]).
- 빌드를 AppVeyor CI와 SignPath 코드 서명으로 옮기고, 저장소에 들어 있던 바이너리를 제거했습니다([#130]).

### 수정

- C# `string[]`를 내장 스크립트 엔진으로 변환하는 문제([#133]).
- JSON 데이터에서 문자열 포매터([#122]).
- 스크린 타임 기능.

## [0.2.7.30] - 2024-08-02

### 수정

- `WelsonJS.Service`의 스크린 타임 기능 성능. 사용자 대화형 모드에서만 쓸 수 있으며, `--disable-screen-time`으로 끕니다.

## [0.2.7.29] - 2024-07-31

### 수정

- `lib/shell`의 `.setPrefix()`([#126]).

## [0.2.7.28] - 2024-07-30

### 변경

- 에셋과 설치 프로그램을 업데이트했습니다.

## [0.2.7.27] - 2024-07-29

### 추가

- 화면 매칭 기능([#124] 관련).

## [0.2.7.26] - 2024-07-29

### 수정

- `WelsonJS.Service`의 작업 디렉터리.

## [0.2.7.25] - 2024-07-23

### 수정

- `WelsonJS.Service`의 버전 정보.

## [0.2.7.24] - 2024-07-23

### 변경

- `WelsonJS.Service`의 기본 스크립트가 `defaultService`입니다(이전: `helloworld`).

## [0.2.7.23] - 2024-07-23

### 변경

- `WelsonJS.Toolkit` 바이너리를 업데이트했습니다.

## [0.2.7.22] - 2024-07-23

### 수정

- `initializeWindow()`와 `exit()`로 원치 않게 닫히는 문제.

## [0.2.7.21] - 2024-07-23

### 변경

- `WelsonJS.Service` 바이너리를 업데이트했습니다([#101]).

## [0.2.7.20] - 2024-07-23

### 추가

- JavaScript로 Windows 서비스를 작성하는 `WelsonJS.Service`([#101]).

## [0.2.7.19] - 2024-07-22

### 수정

- 원격 스크립트 실행(추가 수정).

## [0.2.7.18] - 2024-07-22

### 수정

- 원격 스크립트 실행.

## [0.2.7.17] - 2024-07-21

### 추가

- `app.js`의 원격 스크립트 실행.
- `lib/http`의 `Certutil` HTTP 요청 방식.

### 수정

- `lib/shell`의 `setPrefix()`와 함께 쓸 때 `lib/http`의 문제.

## [0.2.7.16] - 2024-07-20

### 수정

- JSON 데이터에서 `this.evaluate()`(문자열 포매터)([#122]).

## [0.2.7.15] - 2024-07-13

### 변경

- 연락처 링크를 업데이트했습니다.

## [0.2.7.14] - 2024-07-10

### 수정

- `lib/wmi.js`의 오타와 업데이트용 설치 프로필.

## [0.2.7.13] - 2024-07-10

### 추가

- `lib/shell`의 `.setPrefix()`.
- `lib/http`의 BITS(bitsadmin) 지원([#119]).
- Caterpillar Proxy를 통한 Nmap 지원.

### 수정

- 잘못된 파일 경로.

## [0.2.7.13-rc1] - 2024-07-01

사전 릴리스입니다.

### 추가

- JSON-RPC 2.0 지원.
- Caterpillar Proxy를 통한 MySQL 쿼리.

### 변경

- `lib/xml`을 `lib/pipe-ipc`가 관리합니다.

## [0.2.7.12] - 2024-05-28

### 추가

- 네이티브 구성 요소의 `Bitmap` 인터페이스.

### 수정

- `lib/http`, `lib/file`, `lib/pipe-ipc`에 대한 2024-04-05 피드백([#109]).
- User-Agent 헤더가 바뀌지 않는 문제([#111]).
- `lib/http`에서 압축된 응답 읽기, `lib/adb.js`, `setup.iss`.

## [0.2.7.11] - 2024-03-20

### 수정

- Summernote 문제.

## [0.2.7.10] - 2024-03-20

### 추가

- `Browser.waitUntil()`.

## [0.2.7.9] - 2024-03-19

### 변경

- `encryptor.js`와 README를 업데이트했습니다.

## [0.2.7.8] - 2024-01-27

### 변경

- `bin/README.md`를 업데이트했습니다.

## [0.2.7.7] - 2024-01-27

### 추가

- 스크립트 파일의 HIGHT(ISO/IEC 18033-3) 암호화와 복호화([#104]).

### 수정

- 명령 프롬프트에서 이스케이프되지 않은 앰퍼샌드로 인한 cURL 오류([#103]).

## [0.2.7.6] - 2024-01-05

### 변경

- 공유 메모리 접근을 개선하고([#96]), `WelsonJS.Toolkit.dll` 새 버전을 넣었습니다.

## [0.2.7.5] - 2023-12-29

### 추가

- ES6 제너레이터 함수 트랜스파일([#75]).
- LZ77(MsCompress) 구현.
- 툴킷의 공유 메모리 접근([#96]).

## [0.2.7.5-PRE] - 2023-12-22

사전 릴리스입니다.

### 추가

- 툴킷의 공유 메모리 접근([#96]).
- LZ77(MsCompress) 구현.

### 변경

- `lib/msoffice`를 업데이트했습니다([#93]).

## [0.2.7.4] - 2023-11-28

### 변경

- `lib/msoffice`의 `excel.setValueByPosition(x, y, c)`가 `excel.getCellByPosition(x, y).setValue(c)`로 바뀌었습니다.

## [0.2.7.3] - 2023-11-27

### 추가

- `lib/msoffice`의 Microsoft Office(Excel) 통합([#93]).

## [0.2.7.2] - 2023-11-22

### 추가

- 웹 로더의 WYSIWYG HTML 편집기([#92]).
- GUI 라우터.

### 변경

- 테스트 파일을 `testloader.js`로 합쳤습니다.

## [0.2.7.1] - 2023-10-26

### 수정

- 깨진 GUI 예제([#86]).

## [0.2.7] - 2023-09-26

### 변경

- PIPE IPC로 다중 처리의 동시성을 개선하고([#84]), 셸 인터페이스가 이를 씁니다.

### 수정

- ScriptControl 모드에서 일부 환경 변수를 참조할 수 없는 문제([#79]).

## [0.2.6.1] - 2023-09-11

### 변경

- 라이선스를 GPLv2에서 GPLv3로 바꿨습니다.

### 수정

- 오타.

## [0.2.6] - 2023-09-07

### 추가

- gRPC 지원([#71]).
- CryptoJS 지원([#72]).
- ReasonML/ReScript 지원([#77]).

## [0.2.5.5] - 2023-08-02

### 추가

- ReasonML/ReScript 지원([#77]).
- CryptoJS 지원([#72]).

## [0.2.5.4] - 2023-02-01

### 수정

- `lib/std.js`에서 `StdEventableObject`/`STD.EventableObject`를 `StdEventTarget`/`STD.EventTarget`으로 바꿨습니다.

## [0.2.5.3] - 2022-12-22

### 추가

- TypeScript 지원([#66]).

## [0.2.5.2] - 2022-12-22

### 추가

- TypeScript 지원 첫 부분.

## [0.2.5.1] - 2022-11-29

### 추가

- CoffeeScript([#65])와 LiveScript 지원.
- PIPE 기반 IPC(`lib/pipe-ipc`).
- 추가 라이브러리 `lib/extramath`와 `lib/fakeworker`.
- `lib/security`의 `OpenThreatSettings()`(Windows 보안 센터 열기).
- `WelsonJS.Toolkit`의 가상 키 F1~F12.

### 변경

- 악용 및 보안 문제 신고 연락처를 업데이트했습니다.

## [0.2.5] - 2022-11-29

### 추가

- CoffeeScript 지원.

### 수정

- 창을 열 때의 충돌, `lib/std.js`의 `StdEventTarget`.

## [0.2.4p3] - 2022-08-29

### 수정

- `lib/chrome`과 `lib/http`에 `HTTP.create()`가 없는 문제([#58]).
- 콘솔 모드에서 `AsyncFunction`([#52]).

## [0.2.4p2] - 2022-05-31

### 추가

- `lib/http`의 웹 프록시 디버깅 도구 지원(`.attachDebugger()`): Fiddler, mitmproxy, Burp Suite, OWASP ZAP([#43], [#51]).
- Punycode(IDNA) 지원.

## [0.2.4] - 2022-05-04

### 추가

- mitmproxy 지원([#43])과 Fiddler로 트래픽 캡처([#51]).
- `AsyncFunction` 카운터([#50]).

### 수정

- "이 스크립트 실행을 중지하시겠습니까?" 메시지([#47]).

## [0.2.3] - 2022-04-20

### 추가

- `lib/http`의 HTTP/cURL 모드에 `.isFollowRedirect(boolean)`와 `.setSaveTo(string)`.
- `lib/wmi`의 `.fetchAll2()`([#30]).
- `lib/std`의 `AsyncFunction` 클래스.
- 파일 로깅을 위한 앱의 `_echoCallback(f)`([#33]).
- YAML 지원([#35]).

### 변경

- 라이선스를 MS-PL에서 MS-RL로 바꿨습니다.
- `lib/chrome`의 창 좌표 계산.

### 수정

- `lib/std`의 `sleep()`이 정확하지 않은 문제([#34]).
- `lib/shell`의 정규식.

## [0.2.2] - 2022-01-19

### 추가

- `lib/python3`의 임베디드 Python 통합([#29]).

## [0.2.1] - 2021-09-21

### 추가

- `bootstrap.bat`.
- `lib/toolkit`의 사용자 프롬프트(`alert`, `confirm`, `prompt`)([#21]).
- `lib/toolkit`의 C#/VB.NET 네이티브 구성 요소 지원([#18]).
- WebSocket 지원([#17]).
- core-js를 WSH로 이식([#16]).
- GTK Glade GUI XML 지원([#12]).
- `lib/system`의 Squel.js(WSH에서 WMI SQL)([#10]).

### 수정

- HTA가 연결되어 있지 않을 때의 실행([#15]).
- ES5-shim 4.5.15와 ES6-shim 0.35.x의 충돌([#14]).
- 콘솔 모드의 `JSON` 모듈([#8]).
- WSH에서 core-js의 `Object.create(null)` 충돌(상위 프로젝트 수정).

[Unreleased]: https://github.com/gnh1201/welsonjs/compare/0.2.7.62...HEAD
[0.2.7.62]: https://github.com/gnh1201/welsonjs/compare/0.2.7.62-rc1...0.2.7.62
[0.2.7.62-rc1]: https://github.com/gnh1201/welsonjs/compare/0.2.7.61...0.2.7.62-rc1
[0.2.7.61]: https://github.com/gnh1201/welsonjs/compare/0.2.7.60...0.2.7.61
[0.2.7.60]: https://github.com/gnh1201/welsonjs/compare/0.2.7.57...0.2.7.60
[0.2.7.57]: https://github.com/gnh1201/welsonjs/compare/0.2.7.56...0.2.7.57
[0.2.7.56]: https://github.com/gnh1201/welsonjs/compare/0.2.7.55...0.2.7.56
[0.2.7.55]: https://github.com/gnh1201/welsonjs/compare/0.2.7.54...0.2.7.55
[0.2.7.54]: https://github.com/gnh1201/welsonjs/compare/0.2.7.53...0.2.7.54
[0.2.7.53]: https://github.com/gnh1201/welsonjs/compare/0.2.7.52...0.2.7.53
[0.2.7.52]: https://github.com/gnh1201/welsonjs/compare/0.2.7.51...0.2.7.52
[0.2.7.51]: https://github.com/gnh1201/welsonjs/compare/0.2.7.50...0.2.7.51
[0.2.7.50]: https://github.com/gnh1201/welsonjs/compare/0.2.7.49...0.2.7.50
[0.2.7.49]: https://github.com/gnh1201/welsonjs/compare/0.2.7.48...0.2.7.49
[0.2.7.48]: https://github.com/gnh1201/welsonjs/compare/0.2.7.47...0.2.7.48
[0.2.7.47]: https://github.com/gnh1201/welsonjs/compare/0.2.7.46...0.2.7.47
[0.2.7.46]: https://github.com/gnh1201/welsonjs/compare/0.2.7.45...0.2.7.46
[0.2.7.45]: https://github.com/gnh1201/welsonjs/compare/0.2.7.44...0.2.7.45
[0.2.7.44]: https://github.com/gnh1201/welsonjs/compare/0.2.7.43...0.2.7.44
[0.2.7.43]: https://github.com/gnh1201/welsonjs/compare/0.2.7.42...0.2.7.43
[0.2.7.42]: https://github.com/gnh1201/welsonjs/compare/0.2.7.41...0.2.7.42
[0.2.7.41]: https://github.com/gnh1201/welsonjs/compare/0.2.7.40...0.2.7.41
[0.2.7.40]: https://github.com/gnh1201/welsonjs/compare/0.2.7.39...0.2.7.40
[0.2.7.39]: https://github.com/gnh1201/welsonjs/compare/0.2.7.38...0.2.7.39
[0.2.7.38]: https://github.com/gnh1201/welsonjs/compare/0.2.7.37...0.2.7.38
[0.2.7.37]: https://github.com/gnh1201/welsonjs/compare/0.2.7.36...0.2.7.37
[0.2.7.36]: https://github.com/gnh1201/welsonjs/compare/0.7.34...0.2.7.36
[0.7.34]: https://github.com/gnh1201/welsonjs/compare/0.2.7.33...0.7.34
[0.2.7.33]: https://github.com/gnh1201/welsonjs/compare/0.2.7.32...0.2.7.33
[0.2.7.32]: https://github.com/gnh1201/welsonjs/compare/0.2.7.31...0.2.7.32
[0.2.7.31]: https://github.com/gnh1201/welsonjs/compare/0.2.7.30...0.2.7.31
[0.2.7.30]: https://github.com/gnh1201/welsonjs/compare/0.2.7.29...0.2.7.30
[0.2.7.29]: https://github.com/gnh1201/welsonjs/compare/0.2.7.28...0.2.7.29
[0.2.7.28]: https://github.com/gnh1201/welsonjs/compare/0.2.7.27...0.2.7.28
[0.2.7.27]: https://github.com/gnh1201/welsonjs/compare/0.2.7.26...0.2.7.27
[0.2.7.26]: https://github.com/gnh1201/welsonjs/compare/0.2.7.25...0.2.7.26
[0.2.7.25]: https://github.com/gnh1201/welsonjs/compare/0.2.7.24...0.2.7.25
[0.2.7.24]: https://github.com/gnh1201/welsonjs/compare/0.2.7.23...0.2.7.24
[0.2.7.23]: https://github.com/gnh1201/welsonjs/compare/0.2.7.22...0.2.7.23
[0.2.7.22]: https://github.com/gnh1201/welsonjs/compare/0.2.7.21...0.2.7.22
[0.2.7.21]: https://github.com/gnh1201/welsonjs/compare/0.2.7.20...0.2.7.21
[0.2.7.20]: https://github.com/gnh1201/welsonjs/compare/0.2.7.19...0.2.7.20
[0.2.7.19]: https://github.com/gnh1201/welsonjs/compare/0.2.7.18...0.2.7.19
[0.2.7.18]: https://github.com/gnh1201/welsonjs/compare/0.2.7.17...0.2.7.18
[0.2.7.17]: https://github.com/gnh1201/welsonjs/compare/0.2.7.16...0.2.7.17
[0.2.7.16]: https://github.com/gnh1201/welsonjs/compare/0.2.7.15...0.2.7.16
[0.2.7.15]: https://github.com/gnh1201/welsonjs/compare/0.2.7.14...0.2.7.15
[0.2.7.14]: https://github.com/gnh1201/welsonjs/compare/0.2.7.13...0.2.7.14
[0.2.7.13]: https://github.com/gnh1201/welsonjs/compare/0.2.7.13-rc1...0.2.7.13
[0.2.7.13-rc1]: https://github.com/gnh1201/welsonjs/compare/0.2.7.12...0.2.7.13-rc1
[0.2.7.12]: https://github.com/gnh1201/welsonjs/compare/0.2.7.11...0.2.7.12
[0.2.7.11]: https://github.com/gnh1201/welsonjs/compare/0.2.7.10...0.2.7.11
[0.2.7.10]: https://github.com/gnh1201/welsonjs/compare/0.2.7.9...0.2.7.10
[0.2.7.9]: https://github.com/gnh1201/welsonjs/compare/0.2.7.8...0.2.7.9
[0.2.7.8]: https://github.com/gnh1201/welsonjs/compare/0.2.7.7...0.2.7.8
[0.2.7.7]: https://github.com/gnh1201/welsonjs/compare/0.2.7.6...0.2.7.7
[0.2.7.6]: https://github.com/gnh1201/welsonjs/compare/0.2.7.5...0.2.7.6
[0.2.7.5]: https://github.com/gnh1201/welsonjs/compare/0.2.7.5-PRE...0.2.7.5
[0.2.7.5-PRE]: https://github.com/gnh1201/welsonjs/compare/0.2.7.4...0.2.7.5-PRE
[0.2.7.4]: https://github.com/gnh1201/welsonjs/compare/0.2.7.3...0.2.7.4
[0.2.7.3]: https://github.com/gnh1201/welsonjs/compare/0.2.7.2...0.2.7.3
[0.2.7.2]: https://github.com/gnh1201/welsonjs/compare/0.2.7.1...0.2.7.2
[0.2.7.1]: https://github.com/gnh1201/welsonjs/compare/0.2.7...0.2.7.1
[0.2.7]: https://github.com/gnh1201/welsonjs/compare/0.2.6.1...0.2.7
[0.2.6.1]: https://github.com/gnh1201/welsonjs/compare/0.2.6...0.2.6.1
[0.2.6]: https://github.com/gnh1201/welsonjs/compare/0.2.5.5...0.2.6
[0.2.5.5]: https://github.com/gnh1201/welsonjs/compare/0.2.5.4...0.2.5.5
[0.2.5.4]: https://github.com/gnh1201/welsonjs/compare/0.2.5.3...0.2.5.4
[0.2.5.3]: https://github.com/gnh1201/welsonjs/compare/0.2.5.2...0.2.5.3
[0.2.5.2]: https://github.com/gnh1201/welsonjs/compare/0.2.5.1...0.2.5.2
[0.2.5.1]: https://github.com/gnh1201/welsonjs/compare/0.2.5...0.2.5.1
[0.2.5]: https://github.com/gnh1201/welsonjs/compare/0.2.4p3...0.2.5
[0.2.4p3]: https://github.com/gnh1201/welsonjs/compare/0.2.4p2...0.2.4p3
[0.2.4p2]: https://github.com/gnh1201/welsonjs/compare/0.2.4...0.2.4p2
[0.2.4]: https://github.com/gnh1201/welsonjs/compare/0.2.3...0.2.4
[0.2.3]: https://github.com/gnh1201/welsonjs/compare/0.2.2...0.2.3
[0.2.2]: https://github.com/gnh1201/welsonjs/compare/0.2.1...0.2.2
[0.2.1]: https://github.com/gnh1201/welsonjs/releases/tag/0.2.1

[#8]: https://github.com/gnh1201/welsonjs/issues/8
[#10]: https://github.com/gnh1201/welsonjs/issues/10
[#12]: https://github.com/gnh1201/welsonjs/issues/12
[#14]: https://github.com/gnh1201/welsonjs/issues/14
[#15]: https://github.com/gnh1201/welsonjs/issues/15
[#16]: https://github.com/gnh1201/welsonjs/issues/16
[#17]: https://github.com/gnh1201/welsonjs/issues/17
[#18]: https://github.com/gnh1201/welsonjs/issues/18
[#21]: https://github.com/gnh1201/welsonjs/issues/21
[#29]: https://github.com/gnh1201/welsonjs/issues/29
[#30]: https://github.com/gnh1201/welsonjs/issues/30
[#33]: https://github.com/gnh1201/welsonjs/issues/33
[#34]: https://github.com/gnh1201/welsonjs/issues/34
[#35]: https://github.com/gnh1201/welsonjs/issues/35
[#40]: https://github.com/gnh1201/welsonjs/issues/40
[#43]: https://github.com/gnh1201/welsonjs/issues/43
[#47]: https://github.com/gnh1201/welsonjs/issues/47
[#50]: https://github.com/gnh1201/welsonjs/issues/50
[#51]: https://github.com/gnh1201/welsonjs/issues/51
[#52]: https://github.com/gnh1201/welsonjs/issues/52
[#58]: https://github.com/gnh1201/welsonjs/issues/58
[#65]: https://github.com/gnh1201/welsonjs/issues/65
[#66]: https://github.com/gnh1201/welsonjs/issues/66
[#71]: https://github.com/gnh1201/welsonjs/issues/71
[#72]: https://github.com/gnh1201/welsonjs/issues/72
[#75]: https://github.com/gnh1201/welsonjs/issues/75
[#77]: https://github.com/gnh1201/welsonjs/issues/77
[#79]: https://github.com/gnh1201/welsonjs/issues/79
[#84]: https://github.com/gnh1201/welsonjs/issues/84
[#86]: https://github.com/gnh1201/welsonjs/issues/86
[#92]: https://github.com/gnh1201/welsonjs/issues/92
[#93]: https://github.com/gnh1201/welsonjs/issues/93
[#96]: https://github.com/gnh1201/welsonjs/issues/96
[#101]: https://github.com/gnh1201/welsonjs/issues/101
[#103]: https://github.com/gnh1201/welsonjs/issues/103
[#104]: https://github.com/gnh1201/welsonjs/issues/104
[#109]: https://github.com/gnh1201/welsonjs/issues/109
[#111]: https://github.com/gnh1201/welsonjs/issues/111
[#119]: https://github.com/gnh1201/welsonjs/issues/119
[#122]: https://github.com/gnh1201/welsonjs/issues/122
[#124]: https://github.com/gnh1201/welsonjs/issues/124
[#126]: https://github.com/gnh1201/welsonjs/issues/126
[#130]: https://github.com/gnh1201/welsonjs/issues/130
[#131]: https://github.com/gnh1201/welsonjs/issues/131
[#132]: https://github.com/gnh1201/welsonjs/issues/132
[#133]: https://github.com/gnh1201/welsonjs/issues/133
[#135]: https://github.com/gnh1201/welsonjs/issues/135
[#137]: https://github.com/gnh1201/welsonjs/issues/137
[#139]: https://github.com/gnh1201/welsonjs/pull/139
[#143]: https://github.com/gnh1201/welsonjs/issues/143
[#144]: https://github.com/gnh1201/welsonjs/issues/144
[#145]: https://github.com/gnh1201/welsonjs/issues/145
[#154]: https://github.com/gnh1201/welsonjs/issues/154
[#157]: https://github.com/gnh1201/welsonjs/issues/157
[#183]: https://github.com/gnh1201/welsonjs/issues/183
[#201]: https://github.com/gnh1201/welsonjs/issues/201
[#220]: https://github.com/gnh1201/welsonjs/issues/220
[#233]: https://github.com/gnh1201/welsonjs/issues/233
[#246]: https://github.com/gnh1201/welsonjs/issues/246
[#251]: https://github.com/gnh1201/welsonjs/issues/251
[#254]: https://github.com/gnh1201/welsonjs/issues/254
[#259]: https://github.com/gnh1201/welsonjs/issues/259
[#271]: https://github.com/gnh1201/welsonjs/issues/271
[#274]: https://github.com/gnh1201/welsonjs/issues/274
[#277]: https://github.com/gnh1201/welsonjs/issues/277
[#283]: https://github.com/gnh1201/welsonjs/issues/283
[#293]: https://github.com/gnh1201/welsonjs/issues/293
[#295]: https://github.com/gnh1201/welsonjs/issues/295
[#353]: https://github.com/gnh1201/welsonjs/pull/353
[#368]: https://github.com/gnh1201/welsonjs/issues/368
[#374]: https://github.com/gnh1201/welsonjs/issues/374
[#376]: https://github.com/gnh1201/welsonjs/issues/376
[#397]: https://github.com/gnh1201/welsonjs/issues/397
[#399]: https://github.com/gnh1201/welsonjs/issues/399
[#404]: https://github.com/gnh1201/welsonjs/issues/404
[#411]: https://github.com/gnh1201/welsonjs/issues/411
[#412]: https://github.com/gnh1201/welsonjs/issues/412
[#420]: https://github.com/gnh1201/welsonjs/issues/420
[#424]: https://github.com/gnh1201/welsonjs/issues/424
[#427]: https://github.com/gnh1201/welsonjs/issues/427
[#429]: https://github.com/gnh1201/welsonjs/issues/429
[#434]: https://github.com/gnh1201/welsonjs/issues/434
[#440]: https://github.com/gnh1201/welsonjs/issues/440
[#449]: https://github.com/gnh1201/welsonjs/issues/449
[#474]: https://github.com/gnh1201/welsonjs/issues/474
[#477]: https://github.com/gnh1201/welsonjs/issues/477
[#486]: https://github.com/gnh1201/welsonjs/issues/486
[#510]: https://github.com/gnh1201/welsonjs/issues/510
[#519]: https://github.com/gnh1201/welsonjs/issues/519
[#523]: https://github.com/gnh1201/welsonjs/issues/523
[#542]: https://github.com/gnh1201/welsonjs/issues/542
[#543]: https://github.com/gnh1201/welsonjs/issues/543
