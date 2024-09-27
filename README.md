# welsonjs

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fgnh1201%2Fwelsonjs.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fgnh1201%2Fwelsonjs?ref=badge_shield)
[![AppVeyor Status](https://ci.appveyor.com/api/projects/status/github/gnh1201/welsonjs?svg=true)](https://ci.appveyor.com/project/gnh1201/welsonjs)
[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.11382384.svg)](https://doi.org/10.5281/zenodo.11382384)

WelsonJS - Build a Windows app on the Windows built-in JavaScript engine.

![(This is a cover image) Windows in 1999](app/assets/img/cover.png)

Now, You can build an Windows desktop app with JavaScript, TypeScript, CoffeeScript, ReScript, and HTML/CSS on Windows built-in ECMAScript engine.

WelsonJS = ***W***indows + ***El***ectr***on***-like + ***Javascript(JS)*** + [Your contribution](https://github.com/sponsors/gnh1201)

Dual license notice: The default license for this project is GPL 3.0. However, if the GPL 3.0 license is not compatible with Microsoft products, it is subject to the MS-RL license.

## Sponsors
- <img src="app/assets/img/github_octocat_logo.png" height="30" alt=""/> [GitHub Sponsors](https://github.com/sponsors/gnh1201)
- <img src="app/assets/img/logo_oss.gif" height="30" alt=""/> Open Software Portal, Korea National Industry Promotion Agency - Awarded Prize
- <img src="app/assets/img/signpath_logo.png" height="30" alt=""/> Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org/)

## Partnerships
- <img src="app/assets/img/scrapeops-logo.svg" height="30" alt=""/> [ScrapeOps (scrapeops.io)](https://scrapeops.io?fpr=namhyeon75) - Proxy Aggregator (Free 1,000 calls + Free 0.5GB per month, 10% Off Coupon `gnh10`)

These partnerships are based on existing cases of integration with this project. The proceeds are used to support this project.

## Structure
![The structure of the WelsonJS framework can be extended based on whether it operates in a console (command prompt) environment, a GUI (with HTML/CSS) environment, or a service environment, with the `app.js` file at its core.](app/assets/img/structure.png)

## Specifications
- Built-in transpilers: [TypeScript](https://www.typescriptlang.org/), [Rescript](https://rescript-lang.org/), [CoffeeScript 2](https://coffeescript.org/), [LiveScript](https://livescript.net/)
- Ready to use on Windows machine immediately. No require additional software installation.
- ES5(ECMAScript 5), XML, JSON, YAML compatibility
  - [github.com/zloirock/core-js](https://github.com/zloirock/core-js)
  - [github.com/douglascrockford/JSON-js](https://github.com/douglascrockford/JSON-js) (aka. JSON2.js)
  - [github.com/nodeca/js-yaml](https://github.com/nodeca/js-yaml)
- HTML5, CSS3 compatibility
  - [github.com/aFarkas/html5shiv](https://github.com/aFarkas/html5shiv)
  - [github.com/parndt/jquery-html5-placeholder-shim](https://github.com/parndt/jquery-html5-placeholder-shim)
  - [github.com/scottjehl/Respond](https://github.com/scottjehl/Respond)
  - [github.com/keithclark/selectivizr](https://github.com/keithclark/selectivizr)
  - [github.com/arv/ExplorerCanvas](https://github.com/arv/ExplorerCanvas)
  - [github.com/Modernizr/Modernizr](https://github.com/Modernizr/Modernizr)
- CSS Frameworks
  - [github.com/jslegers/cascadeframework](https://github.com/jslegers/cascadeframework)
  - [github.com/golden-layout/golden-layout](https://github.com/golden-layout/golden-layout)
- WYSIWYG HTML Editor
  - [github.com/summernote/summernote](https://github.com/summernote/summernote)
- Included libraries
  - [jQuery](https://jquery.com/)
  - [jQuery UI](https://jqueryui.com/)
  - [github.com/kamranahmedse/jquery-toast-plugin](https://github.com/kamranahmedse/jquery-toast-plugin) - Highly customizable jquery plugin to show toast messages
  - [github.com/hiddentao/squel](https://github.com/hiddentao/squel) - SQL query string builder for Javascript
  - [github.com/BorisMoore/jsrender](https://github.com/BorisMoore/jsrender) - A lightweight, powerful and highly extensible templating engine. In the browser or on Node.js, with or without jQuery.
  - [github.com/mihaifm/linq](https://github.com/mihaifm/linq) - LINQ for JavaScript
  - [github.com/pegjs/pegjs](https://github.com/pegjs/pegjs) - PEG.js: Parser generator for JavaScript
- [Includes binaries](https://github.com/gnh1201/welsonjs/blob/master/bin/README.MD)
- [module.exports](https://nodejs.org/api/modules.html#moduleexports), CommonJS, UMD compatibility
- [NPM](https://www.npmjs.com/) compatibility
- [Chrome DevTools Protocol](https://chromedevtools.github.io/devtools-protocol/) support
- [ADB(Android Debug Bridge)](https://source.android.com/docs/setup/build/adb) support
- RPC(Remote Procedure Call) protocol support
  - [gRPC](https://grpc.io/)
  - [JSON-RPC 2.0](https://www.jsonrpc.org/specification)

## Included modules
- lib/std (Standard library)
- lib/system (System interface)
- lib/base64 (BASE64 encode and decode)
- lib/file (File I/O interface)
- lib/http (HTTP/HTTPS client with [XHR(MSXML)](https://developer.mozilla.org/docs/Glossary/XMLHttpRequest), [cURL](https://curl.se/), [BITS](https://en.m.wikipedia.org/w/index.php?title=Background_Intelligent_Transfer_Service), [CERT](https://github.com/MicrosoftDocs/windowsserverdocs/blob/main/WindowsServerDocs/administration/windows-commands/certutil.md), and [Web Proxy API](https://scrapeops.io?fpr=namhyeon75))
- lib/registry (Windows Registry interface)
- lib/security (Windows Security Policy interface)
- lib/shell (Windows Shell (Command Prompt) interface)
- lib/powershell (Windows Powershell interface)
- lib/service (Windows Service interface)
- lib/browser (Modern web compatibility layer)
- lib/uri (URI scheme interface)
- lib/winlibs (Windows DLL(Dynamic-link library) interface)
- lib/autohotkey ([AutoHotkey](https://www.autohotkey.com/) interface)
- lib/autoit ([AutoIt3/AutoItX](https://www.autoitscript.com/) interface)
- lib/msoffice (Microsoft Office (e.g. Excel, PowerPoint, Word) interface)
- lib/gtk (GTK-server and GladeXML supported GUI interface)
- lib/chrome ([Chrome DevTools Protocol](https://chromedevtools.github.io/devtools-protocol/) based Chrome/Chromium web browser testing interface)
- lib/pipe-ipc (PIPE (e.g., File IO, Standard IO) based IPC(Inter-Process Communication) implementation)
- [WelsonJS.Toolkit](https://github.com/gnh1201/welsonjs/wiki/Awesome-binaries) (DLL/COM component with .NET 2.0, For all Windows based systems)
  - User prompt methods (e.g., alert, confirm) implementation
  - Useful tools to control the windows and window handle (find, attach, trigger events. e.g., Virtual Human Interface
  - Cryptography ([ISO/IEC 18033-3:2010](https://www.iso.org/standard/54531.html) aka. [HIGHT](https://seed.kisa.or.kr/kisa/algorithm/EgovHightInfo.do))
  - [Named Shared Memory](https://learn.microsoft.com/en-us/windows/win32/memory/creating-named-shared-memory) based IPC(Inter-Process Communication) implementation [#](https://qiita.com/gnh1201/items/4e70dccdb7adacf0ace5)
- [WelsonJS.Service](https://github.com/gnh1201/welsonjs/wiki/Awesome-binaries) (Windows Service Application, For recent Windows based systems)
  - Write a Windows Service Application with JavaScript
  - [File Event Monitor](https://github.com/gnh1201/welsonjs/wiki/File-Event-Monitor): Trace file creation, network connections, and registry modifications.
  - [Screen Time Feature](https://github.com/gnh1201/welsonjs/wiki/Screen-Time-Feature): Find an image position on the computer screens or windows.
- [WelsonJS.Launcher](https://github.com/gnh1201/welsonjs/wiki/Launcher) (Launcher Application, For recent Windows based systems)
  - This is a launcher app designed to easily distribute WelsonJS application packages (based on ZIP compression files).
- lib/chatgpt ([ChatGPT](https://openai.com/chatgpt) integration)
- Everything you can imagine.

## Make your own `sayhello` example

### 1. Write a file `lib/sayhello.js`
```js
// lib/sayhello.js
function say() {
    console.log("hello");
}

exports.say = say;

exports.VERSIONINFO = "SayHello Library (sayhello.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";   // e.g. YOUR EMAIL ADDRESS
exports.global = global;
exports.require = global.require;
```

### 2. Write a file `sayhello.js`
```js
// sayhello.js
var SayHello = require("lib/sayhello");

function main() {
    console.log("calling say()");
    SayHello.say();
    console.log("ended say()");
}

exports.main = main;
```

### 3. Execute file on the command prompt
```cmd
C:\Users\knh94\Documents\GitHub\welsonjs> cscript app.js sayhello
calling say()
hello
ended say()
```

## How to release my application?
The WelsonJS framework suggests the following application release methods:

- **Compress to Zip, and use the launcher**: Compress the files and directories necessary for running the project into a Zip file, and distribute it along with the [WelsonJS.Launcher](https://github.com/gnh1201/welsonjs/wiki/Launcher).
- **Build a setup file**: Use [Inno Setup](https://jrsoftware.org/isinfo.php). Information needed to create the setup file (the `setup.iss` file) is already included.
- **Copy all directories and files**: This is the simplest and most straightforward method.

## Screenshots
![(Screenshot 1) GUI environment](app/assets/img/screenshot.png)

![(Screenshot 2) Command-line environment](app/assets/img/screenshot2.png)

![(Screenshot 3) WelsonJS with Microsoft Excel](app/assets/img/screenshot3.png)

![(Screenshot 4) Write a Windows Services with JavaScript](app/assets/img/screenshot4.png)

![(Screenshot 5) Template Matching on the computer screen](app/assets/img/screenshot5.png)

![(Screenshot 6) The Launcher for WelsonJS Application Packages](app/assets/img/screenshot6.png)

## Thanks to
- Heavy-industry specialized CSP(Cloud Service Provider) in Republic of Korea - Use case establishment
- Live-commerce specialized online advertisement companies in Republic of Korea - Use case establishment
- Information security companies in Republic of Korea - Use case establishment
- <img src="app/assets/img/morioh.svg" height="30" alt=""/> morioh.com - Mentioned
- <img src="app/assets/img/CSDN_Logo.svg" height="30" alt=""/> CSDN - Mentioned
- <img src="app/assets/img/qiita-logo.png" height="30" alt=""/> Qiita - Knowledge-base about WSH environment
- <img src="app/assets/img/RedSky-logo-web.webp" height="30" alt=""/> Redsky Software - PoC(Proof of Concept) of the CommonJS on WSH environment
- Inspired by a small-sized JavaScript payload demonstrated by a cybersecurity related group.
- Inspired by the use of Named Shared Memory in an inter-language IPC implementation devised by an unidentified developer.
- <img src="app/assets/img/Fediverse_logo_proposal.svg" height="30" alt=""/> Fediverse - Mentioned
- <img src="app/assets/img/Hackernews_logo.png" height="30" alt=""/> Hacker News - Mentioned

## Related links
- [gnh1201/wsh-js-gtk](https://github.com/gnh1201/wsh-js-gtk) - GTK GUI ported to Windows Scripting Host - Javascript (Microsoft JScript) (wsh-js)
- [gnh1201/wsh-json](https://github.com/gnh1201/wsh-json) - JSON stringify/parse (encode/decode) for Windows Scripting Host
- [redskyit/wsh-appjs](https://github.com/redskyit/wsh-appjs) - require-js and app framework for Windows Scripting Host JavaScript
- [JohnLaTwC's gist](https://gist.github.com/JohnLaTwC/4315bbbd89da0996f5c08c032b391799) - JavaScript RAT
- [JSMan-/JS-Framework](https://github.com/JSMan-/JS-Framework) - No description
- [iconjack/setTimeout-for-windows-script-host](https://github.com/iconjack/setTimeout-for-windows-script-host) - Replacement for the missing setTimeout and clearTimeout function in Windows Script Host
- [johnjohnsp1/WindowsScriptHostExtension](https://github.com/johnjohnsp1/WindowsScriptHostExtension) - Inject DLL Prototype using Microsoft.Windows.ACTCTX COM Object
- [kuntashov/jsunit](https://github.com/kuntashov/jsunit) - JSUnit port for Windows Scripting Host
- [nickdoth/WSHHttpServer](https://github.com/nickdoth/WSHHttpServer) - HTTP server based on Windows Script Host
- FOSSA report [HTML](https://pub-f926e14287b340cd9eff33731bb25329.r2.dev/fossa_report.html) [CSV](https://pub-f926e14287b340cd9eff33731bb25329.r2.dev/fossa_report.csv) [TXT](https://pub-f926e14287b340cd9eff33731bb25329.r2.dev/fossa_report.txt)
- [License attributions of a stock images](https://policy.catswords.social/stock_images.html)

## Report abuse
- [GitHub Security Advisories](https://github.com/gnh1201/welsonjs/security)
- abuse@catswords.net
- ActivityPub [@catswords_oss@catswords.social](https://catswords.social/@catswords_oss)
- [Join Catswords on Microsoft Teams](https://teams.live.com/l/community/FEACHncAhq8ldnojAI)

## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fgnh1201%2Fwelsonjs.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Fgnh1201%2Fwelsonjs?ref=badge_large)
