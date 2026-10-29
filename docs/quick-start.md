# WelsonJS 빠른 시작

[English version](quick-start.en.md)

이 문서는 WelsonJS를 내려받아 첫 번째 스크립트를 실행하기까지의 과정을 설명한다.

## 요구 사항

- Windows XP SP3 이상 (Windows 11 24H2까지 확인)
- 기본 실행에는 별도 소프트웨어 설치가 필요하지 않다. 스크립트는 Windows에 내장된 Windows Script Host(WSH)의 JScript 엔진으로 실행된다.
- 일부 모듈은 외부 프로그램, 네트워크, API 키 또는 관리자 권한이 필요하다. 사용하기 전에 해당 모듈의 구현을 확인한다.

## 1. 내려받기

다음 중 하나를 선택한다.

**Git으로 복제**

```cmd
git clone https://github.com/gnh1201/welsonjs.git
cd welsonjs
```

**PowerShell 한 줄 설치**

```powershell
irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex
```

이 스크립트는 `master` 브랜치를 임시 폴더(`%TEMP%`)에 내려받아 압축을 풀고 `bootstrap.bat`을 실행한다. 특정 파일을 바로 실행하려면 스크립트를 저장한 뒤 인자를 넘긴다.

```powershell
irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 -OutFile bootstrap.ps1
.\bootstrap.ps1 -file helloworld.js
```

**WelsonJS Launcher**

[WelsonJS Launcher](https://catswords.blob.core.windows.net/welsonjs/welsonjs_launcher_latest.zip)를 내려받으면 인스턴스 관리, 변수 편집기, 코드 편집기를 GUI로 사용할 수 있다.

## 2. (선택) 초기 설정

```cmd
bootstrap.bat
```

`bootstrap.bat`은 다음 작업을 수행한다.

- `app\assets\reg\Default_HTA.reg`를 가져와 HTA 파일 연결을 설정한다 (레지스트리 변경).
- `WelsonJS.ManagedObject` 구성 요소를 `RegAsm.exe`로 등록한다. 로컬 `bin\x86\WelsonJS.ManagedObject.dll`이 없으면 인터넷에서 내려받는다.
- 마지막으로 `cscript app.js bootstrap`을 실행한다.

레지스트리와 COM 등록을 변경하므로 관리자 권한이 필요할 수 있다. 순수 JScript 모듈만 사용한다면 이 단계를 건너뛰어도 된다.

## 3. 첫 번째 스크립트 실행

저장소 루트에서 다음을 실행한다.

```cmd
cscript app.js helloworld
```

`app.js`는 첫 번째 인자로 받은 이름의 모듈을 `require()`로 불러온 뒤 `main(args)`를 호출한다. 확장자를 생략하면 `.js`가 붙는다. `helloworld.js`는 HTTP 클라이언트, 해시, 대화 상자 등을 시험하므로 일부 기능은 `bootstrap.bat`으로 관리 개체를 등록한 뒤에 동작한다.

## 4. 나만의 모듈 만들기

### `lib/sayhello.js` 작성

```js
// lib/sayhello.js
function say() {
    console.log("hello");
}

exports.say = say;

exports.VERSIONINFO = "SayHello (sayhello.js) version 0.1";
exports.AUTHOR = "you@example.com";
exports.global = global;
exports.require = global.require;
```

### 진입점 `sayhello.js` 작성

```js
// sayhello.js
var SayHello = require("lib/sayhello");

function main(args) {
    console.log("calling say()");
    SayHello.say();
    console.log("ended say()");
}

exports.main = main;
```

### 실행

```cmd
C:\...\welsonjs> cscript app.js sayhello
calling say()
hello
ended say()
```

`cscript app.js sayhello foo bar`처럼 인자를 추가하면 `main()`에 `["foo", "bar"]` 배열로 전달된다. `main()`이 숫자를 반환하면 그 값이 종료 코드가 된다.

> **참고**: 엔진은 레거시 JScript(ES3 기반)다. `core-js` 등 일부 ES5 폴리필이 포함되어 있지만, `let`/`const`, 화살표 함수, 클래스 같은 ES6+ 문법은 사용할 수 없다.

## 5. GUI 애플리케이션 (HTA)

```cmd
mshta app.hta
```

`app.hta`는 `webloader.js`를 불러오고, `webloader.js`는 `app/index.html`과 스타일시트를 표시한 뒤 `app/index.js`를 실행한다. 화면과 동작을 바꾸려면 `app/` 폴더의 파일을 수정한다.

## 6. 다른 언어로 작성하기

TypeScript(`.ts`), CoffeeScript(`.coffee`), LiveScript(`.ls`), ReScript(`.re`, `.res`) 파일은 내장 트랜스파일러로 변환되어 실행된다. 확장자를 함께 지정한다.

```cmd
cscript app.js helloworld.coffee
```

## 7. 설정

`settings.example.ini`를 참고해 `settings.ini`에 환경 변수를 정의한다. API 키 같은 민감한 값은 `file:data/chatgpt-apikey.txt`처럼 별도 파일을 가리키게 하고, 해당 파일을 저장소에 커밋하지 않는다.

## 8. 테스트 실행

`testloader.js`는 테스트 ID와 프로필 경로를 받아 하나의 테스트를 실행한다.

```cmd
cscript app.js testloader es5_polyfills data\test-oss-korea-2023.json
```

테스트에 따라 Windows 설정 변경, 외부 프로그램, 네트워크, 사용자 입력이 필요할 수 있으므로 `testloader.js`의 구현을 먼저 확인한다. 실행 후 90초 동안 창이 유지된다.

## 다음 단계

- [README](../README.md): 기능 목록, 배포 방법, 스크린샷
- [`lib/`](../lib): 파일, 레지스트리, HTTP, Office 자동화, LLM 연동 등 모듈
- [`examples/`](../examples): 활용 예제
- [기여 안내](../CONTRIBUTING_KO.md)
- [MCP 서버 위키](https://github.com/gnh1201/welsonjs/wiki/MCP-server)
