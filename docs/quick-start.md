# WelsonJS 빠른 시작

[English version](quick-start.en.md)

이 문서는 WelsonJS 0.2.7.62 기준이다. WelsonJS를 설치하고, AI 도구가 있는 코드 편집기를 쓰고, 첫 번째 스크립트를 실행하기까지의 과정을 설명한다. 소스 코드로 작업하려는 개발자는 [개발자용 안내](#개발자용-안내)로 바로 가면 된다.

## 요구 사항

- **Windows**와 설치에 필요한 관리자 권한.
- 설치하는 동안 **인터넷 연결**. 설치 프로그램이 WelsonJS 바이너리와 선택한 구성 요소를 내려받는다.
- 코드 편집기를 위한 **Microsoft Edge 또는 Google Chrome**. 편집기는 브라우저에서 열린다.
- WelsonJS Launcher를 위한 **.NET Framework 4.7.2 이상**. Windows 10(1803) 이후에는 기본으로 들어 있다.
- **Local AI를 쓰려면:** 64비트 Windows(x64 또는 ARM64)와, 설치할 모델에 맞는 디스크 공간과 메모리. 모델은 [구성 요소 고르기](#구성-요소-고르기)에 정리했다.

## 1. 설치

1. [Releases 페이지](https://github.com/gnh1201/welsonjs/releases/latest)에서 `welsonjs_0.2.7.62_setup.exe`를 내려받는다. SignPath Foundation이 서명한 파일이다.
2. 실행하고 변경 허용을 묻는 창에서 허용한다(관리자 권한이 필요하다).
3. **Customer Information**(선택): 비워 두어도 된다. 내용을 입력하고 동의란에 표시했을 때만 Catswords Research로 전송된다.
4. 구성 요소를 고르고(아래 참고) 설치가 끝날 때까지 기다린다.

파일 복사가 끝나면 PowerShell 창에서 설치 후 스크립트가 실행된다. 이 스크립트가 WelsonJS 바이너리와 선택한 구성 요소를 내려받는데, AI 모델은 시간이 걸릴 수 있다. **이 창을 닫지 않는다.** 스크립트가 끝나면 WelsonJS Launcher가 실행된다.

WelsonJS는 `C:\Program Files (x86)\WelsonJS`에 설치된다. llama.cpp와 AI 모델을 비롯해 내려받은 구성 요소는 `%APPDATA%\WelsonJS`에 들어간다.

### 구성 요소 고르기

기본 선택인 **Full**에는 다음이 들어 있다.

| 구성 요소 | 하는 일 |
| --- | --- |
| Associate .js files to run with WelsonJS | `.js` 파일을 더블클릭하면 Windows Script Host 대신 WelsonJS로 실행한다. Windows 기본 동작을 유지하려면 해제한다. |
| Register the welsonjs:// URI scheme | `welsonjs://` 링크로 WelsonJS를 실행할 수 있게 한다. 어떤 웹 페이지에서나 이 링크를 열 수 있으므로, 필요하지 않으면 해제한다. |
| WelsonJS Launcher and Windows Service | 런처와 코드 편집기, WelsonJS Windows 서비스. |
| Configure WelsonJS MCP server for supported AI CLIs | 설치된 AI 명령줄 도구(예: Claude Code)에 WelsonJS를 MCP 서버로 등록한다. |
| Python, cURL, websocat | 일부 WelsonJS 모듈이 쓰는 도구. |
| **Local AI 모델**(Llama.cpp 아래) | 편집기의 AI 도구가 내 PC에서 실행하는 모델. llama.cpp도 함께 설치된다. |

Local AI 모델은 모두 합쳐 약 4.9 GB다.

| 모델 | 다운로드 크기 | 용도 |
| --- | --- | --- |
| Hy-MT2 1.8B 1.25-bit (Tencent) | 462 MB | 번역, 기본 Local AI 모델 |
| FrogNano 4B 2609 | 2.8 GB | 코딩 질문과 코드 작성 |
| BitNet b1.58 2B-4T (Microsoft) | 1.2 GB | 1.58비트 모델로 하는 일반 질문 |
| Qwen3Guard-Gen-0.6B (Qwen) | 484 MB | 프롬프트의 위험한 내용 검사(LLM 가드) |

어떤 로컬 모델을 골라야 할지 고민이라면, 하려는 일에 맞는 모델을 활성화한다.

- **LLM 안전 기능을 테스트하려면:** Qwen3Guard-Gen-0.6B (Qwen).
- **SWE(소프트웨어 엔지니어링), 예를 들어 코딩 질문과 코드 작성이 필요하면:** FrogNano 4B 2609.
- **다국어 번역이 필요하면:** Hy-MT2 1.8B 1.25-bit (Tencent).

용량을 줄이려면 **Custom**을 고르고 필요한 것만 표시한다. 예를 들어 번역만 한다면 Hy-MT2 하나로 충분하다. 나중에 설치 프로그램을 다시 실행해 구성 요소를 추가할 수 있다.

## 2. 런처와 편집기 실행

- 시작 메뉴에서 **Start WelsonJS Launcher**를 실행한다. 관리자 권한으로 실행된다.
- 코드 편집기가 Edge(또는 Chrome)의 앱 창으로 `http://localhost:3000/`에 열린다. 다시 열려면 런처의 **Start the editor...**를 누르거나, 런처의 트레이 아이콘 메뉴에서 **Open the code editor...**를 고른다.
- 런처 창을 닫으면 트레이로 들어간다. 종료하려면 트레이 아이콘 메뉴에서 **Exit**를 고른다.

런처는 자기 전용 브라우저 프로필을 쓰므로, 여기서 웹 서비스에 로그인해도 평소 쓰는 브라우저에는 영향이 없다. 이전 버전에서 업그레이드했다면 이전 런처가 띄운 브라우저 창을 먼저 닫는다.

## 3. 스크립트 작성과 실행

편집기에는 예제 스크립트가 들어 있다. WelsonJS 스크립트는 `main` 함수를 내보낸다.

```js
// hello.js
function main() {
    console.log("Hello World");
}

exports.main = main;
```

- **Run File**을 누르면 스크립트가 실행되고, 결과는 터미널 창에 나온다.
- **Open File**과 **Save File**로 스크립트를 PC에서 불러오고 저장한다.
- **Terminal**은 명령 프롬프트를 연다.

스크립트는 Windows Script Host의 JScript 엔진(ES3 기반)에서 실행된다. `var`와 `function`을 쓰고, `let`, `const`, 화살표 함수, 클래스 같은 ES6+ 문법은 쓸 수 없다. `lib` 폴더의 모듈은 `var HTTP = require("lib/http");`처럼 `require`로 불러온다.

## 4. AI 도구 사용

리본의 **Inference (AI)** 그룹에는 지금 쓰는 AI 서비스 이름이 적힌 버튼이 있다. 누를 때마다 다음 서비스로 바뀐다.

- **Local AI**(Hy-MT2, FrogNano, BitNet): 내 PC에서 실행되며, 인터넷으로 아무것도 보내지 않는다.
- **웹 채팅 서비스**(Copilot, ChatGPT, Claude, Gemini, Grok, DeepSeek, Perplexity, Meta AI, Mistral, Qwen, Kimi, Z.ai): 런처가 자기 브라우저에서 대신 물어본다.

편집기에서 텍스트를 선택하고 마우스 오른쪽 버튼을 누르면 다음 메뉴를 쓸 수 있다.

| 메뉴 | 하는 일 |
| --- | --- |
| Send Selection | 선택한 텍스트를 보낸다. |
| Explain Selection | 선택한 코드나 텍스트를 설명한다. |
| Find Problems in Selection | 선택한 코드에서 버그를 찾는다. |
| Translate Selection into ... | 시스템 언어나 영어로 번역한다. Local AI는 번역을 항상 Hy-MT2로 한다. |
| Write Code | 설명한 내용대로 WelsonJS에서 실행되는 JavaScript 코드를 작성한다. |
| Ask... | 직접 입력한 질문을 한다. |

답은 편집기에 주석으로 들어간다. 서비스를 바꾸면 쌓인 대화가 지워진다.

**Analyze Tactics (MITRE ATT&CK)**는 어떤 서비스를 고르든 똑같이 동작한다. 로컬 모델 FrogNano(설치 구성 요소 `llamacpp\frognano_4b_2609`)가 선택한 코드가 하는 [MITRE ATT&CK](https://attack.mitre.org/) 전술 한두 개를 링크와 한 문장 근거와 함께 알려 주거나, 두드러지는 전술이 없다고 알린다. 내 PC에서 실행되며 5~10초 걸린다(처음에는 모델을 불러오느라 더 걸린다). 결과는 후보로 본다. 시험에서 공격 코드 조각 32개 중 20개는 정답 전술을 첫 번째로, 28개는 답 안에 넣었고, 평범한 코드 14개는 모두 평범하다고 했다. 무언가를 내려받거나 실행하는 코드에 Initial Access를 덧붙이는 경향이 있다.

### Local AI

- 첫 요청에서 llama.cpp를 시작하고 모델을 불러오므로 시간이 조금 걸린다. 그다음 요청은 더 빠르다.
- 설치하지 않은 모델은 요청이 실패한다. 설치 프로그램을 다시 실행해 그 모델을 고른다.
- FrogNano와 BitNet은 영어에서 가장 잘 답하므로, 런처가 먼저 Hy-MT2로 메시지를 영어로 번역한다. 이 두 모델을 쓸 때는 Hy-MT2도 함께 설치한다.

### 웹 채팅 서비스

- 서비스를 처음 쓰면 그 서비스 페이지가 런처 창으로 열린다. 서비스가 요구하면 **그 창에서 로그인**한다. 런처는 메시지 입력란이 나타날 때까지 최대 3분 기다린다.
- 그 창은 다른 창 뒤에 있어도 되지만, 답하는 동안 최소화하지 않는다.
- 보내는 내용에는 각 서비스의 이용 약관과 개인정보 처리방침이 적용된다.

## 5. 네트워크 도구

리본의 **Network Tools** 그룹에서 호스트 정보를 조회한다.

- **Whois**: 도메인 등록 정보.
- **DNS**: 호스트 이름의 DNS 레코드. IP 주소를 넣으면 역방향 이름을 조회한다.
- **IP**: Criminal IP와 AbuseIPDB의 IP 주소 위협 정보. 직접 발급한 API 키가 필요하다(아래 참고).

## 6. 설정 바꾸기

런처 설정은 `C:\Program Files (x86)\WelsonJS\bin\x86\WelsonJS.Launcher.exe.config`에 있다. 관리자 권한으로 파일을 고친 뒤 런처를 다시 시작한다.

| 설정 | 용도 |
| --- | --- |
| `IpQueryApiKey` | IP 도구에 쓰는 Criminal IP API 키 |
| `IpQueryApiKey2` | IP 도구에 쓰는 AbuseIPDB API 키 |
| `DnsServerAddress` | DNS 도구가 쓰는 DNS 서버(쉼표로 구분, 기본값 `1.1.1.1,8.8.8.8`) |
| `ResourceServerApiKey` | 런처의 OpenAI 호환 API를 쓰는 다른 프로그램을 위한 API 키 |
| `AzureClientId`, `AzureTenant` | Azure Key Vault에서 API 키를 받기 위한 로그인(**Settings > Sign in to Azure...**, [azure-key-vault.md](azure-key-vault.md) 참고) |

업그레이드하면 설치 프로그램이 이 파일을 새 파일로 바꾼다. 바꾼 값은 적어 두었다가 업그레이드 후 다시 설정한다.

## 7. 다른 프로그램에서 AI 쓰기(선택)

런처에는 OpenAI 호환 API가 있어서, OpenAI를 지원하는 프로그램이 Local AI 모델과 웹 채팅 서비스를 쓸 수 있다.

- Base URL: `http://localhost:3000/v1`
- 모델: `hy_mt2_1_8b_1_25bit`, `frognano_4b_2609`, `bitnet_b1_58_2b_4t`, 그리고 `copilot`, `chatgpt` 같은 웹 채팅 이름

런처가 실행 중이면 이 PC의 프로그램이 쓸 수 있다. 브라우저 엔진으로 만든 앱처럼 API 키가 필요한 프로그램도 있다. 이때는 `ResourceServerApiKey`를 공개된 예시 값에서 16자 이상의 내 키로 바꾼다. 자세한 내용은 [openai-compatible-api.md](openai-compatible-api.md)를 참고한다.

## 8. 문제 해결

- **편집기가 열리지 않는다.** 런처에서 **Start the editor...**를 누른다. Edge나 Chrome이 설치되어 있는지 확인한다.
- **웹 채팅 창은 열리는데 메시지가 전달되지 않는다.** 편집기를 포함해 런처가 띄운 브라우저 창을 모두 닫고, 런처에서 편집기를 다시 연다.
- **Local AI가 답하지 않는다.** 모델이 설치되었는지 확인한다. `%APPDATA%\WelsonJS\hy_mt2_1_8b_1_25bit`처럼 모델 이름의 폴더에 `.gguf` 파일이 있어야 한다. `%APPDATA%\WelsonJS\llamacpp\llama-server.exe`가 있는지도 확인한다.
- **로그:** `C:\Program Files (x86)\WelsonJS\bin\x86\logs\welsonjs.log`.
- **도움 받기:** [GitHub Issues](https://github.com/gnh1201/welsonjs/issues), 또는 편집기의 Discord, Kakao+ 버튼.

---

## 개발자용 안내

설치 프로그램 없이 소스 코드로도 WelsonJS를 쓸 수 있다. 스크립트는 Windows에 내장된 Windows Script Host의 JScript 엔진에서 실행되므로, 기본 실행에는 별도 소프트웨어가 필요하지 않다. WelsonJS는 Windows XP SP3 이상에서 동작한다(Windows 11 24H2까지 확인).

### 소스 받기

```cmd
git clone https://github.com/gnh1201/welsonjs.git
cd welsonjs
```

PowerShell 한 줄로 내려받아 실행할 수도 있다. `master` 브랜치를 `%TEMP%`에 내려받아 `bootstrap.bat`을 실행한다.

```powershell
irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex
```

### (선택) 초기 설정

```cmd
bootstrap.bat
```

`bootstrap.bat`은 두 가지 일을 한다.

- `WelsonJS.ManagedObject` COM 구성 요소를 등록한다. `bin\x86\WelsonJS.ManagedObject.dll`이 없으면 내려받는다.
- `cscript app.js bootstrap`을 실행한다. Windows 8.1 미만에서는 이 단계에서 HTA 파일 연결도 설정한다.

레지스트리와 COM 등록을 바꾸므로 관리자 권한이 필요할 수 있다. 순수 JScript 모듈만 쓴다면 건너뛰어도 된다.

### 스크립트 실행

```cmd
cscript app.js helloworld
```

`app.js`는 첫 번째 인자로 받은 이름의 모듈을 불러와(확장자를 생략하면 `.js`가 붙는다) `main(args)`를 호출한다. 추가 인자는 배열로 전달되고, `main`이 숫자를 반환하면 그 값이 종료 코드가 된다.

모듈과 진입점은 다음과 같이 만든다.

```js
// lib/sayhello.js
function say() {
    console.log("hello");
}

exports.say = say;
```

```js
// sayhello.js
var SayHello = require("lib/sayhello");

function main(args) {
    SayHello.say();
}

exports.main = main;
```

```cmd
cscript app.js sayhello
```

### 더 알아보기

- **GUI(HTA):** `mshta app.hta`는 `app/index.html`을 표시하고 `app/index.js`를 실행한다. `app/` 폴더의 파일을 수정한다.
- **다른 언어:** TypeScript(`.ts`), CoffeeScript(`.coffee`), LiveScript(`.ls`), ReScript(`.re`, `.res`)는 실행 전에 변환된다. `cscript app.js helloworld.coffee`처럼 확장자를 함께 쓴다.
- **설정:** `settings.example.ini`를 참고해 `settings.ini`에 값을 정의한다. 비밀 값은 `file:data/chatgpt-apikey.txt`처럼 별도 파일을 가리키게 하고, 그 파일은 커밋하지 않는다.
- **테스트:** `cscript app.js testloader es5_polyfills data\test-oss-korea-2023.json`은 테스트 하나를 실행한다. Windows 설정을 바꾸거나 다른 프로그램, 사용자 입력이 필요한 테스트도 있으니 `testloader.js`에서 구현을 먼저 확인한다.
- **스크립트에서 Local AI 쓰기:** `lib/llamacpp.js`가 런처의 llama.cpp 라우터를 쓴다. [llamacpp-launcher-api.md](llamacpp-launcher-api.md)를 참고한다.

## 다음 단계

- [README](../README.md): 기능 목록, 배포 방법, 스크린샷
- [CHANGELOG](../CHANGELOG_KO.md): 릴리스별 변경 내역
- [`lib/`](../lib): 파일, 레지스트리, HTTP, Office 자동화, LLM 서비스 등 모듈
- [`examples/`](../examples): 활용 예제
- [기여 안내](../CONTRIBUTING_KO.md)
- [MCP 서버 위키](https://github.com/gnh1201/welsonjs/wiki/MCP-server)
