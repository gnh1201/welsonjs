# welsonjs
WelsonJS - Build a Windows desktop apps with JavaScript, HTML, and CSS based on WSH/HTA

## Structure
![Structure](app/assets/img/structure.png)

## Specifications
- ES5(ECMAScript 5), ES6(ECMAScript 6) compatibility with [es5-shim](https://catswords.re.kr/go/es5shim), [es6-shim](https://catswords.re.kr/go/es6shim), and [json3](https://catswords.re.kr/go/json3)
- HTML5/CSS3 compatibility with [html5shiv](https://catswords.re.kr/go/html5shiv), [jquery-html5-placeholder-shim](https://catswords.re.kr/go/placeholdershim), [respond](https://catswords.re.kr/go/respondjs), [selectivizr](https://catswords.re.kr/go/selectivizrjs), [excanvas](https://catswords.re.kr/go/excanvasjs), [html5media](https://catswords.re.kr/go/html5media), and [modernizr](https://catswords.re.kr/go/modernizrjs)
- [module.exports](https://catswords.re.kr/go/whatisrequire)(Node) styled module implementation, and managing packages with [NPM(Node Package Manager)](https://catswords.re.kr/go/npmjs)
- Ready to use on Windows machine immediately. No require additional softwares installation.

## Included libraries
- lib/std (Standard library)
- lib/system (System library)
- lib/base64 (BASE64 Encode and Decode)
- lib/db (Database interface)
- lib/file (File I/O interface)
- lib/http (HTTP interface)
- lib/json (JSON Encode and Decode)
- lib/registry (Windows Registry interface)
- lib/security (Security Policy interface)
- lib/sendmail (Sendmail interface with 3rdparty)
- lib/shell (Command Prompt interface)
- lib/timer (`setTimeout` implementation for not supported environment)
- lib/powershell (Windows Powershell interface)
- lib/service (Windows Service interface)
- lib/oldbrowser (ES5/ES6, HTML/JS/CSS compatibility)
- lib/uri (URI scheme interface)
- lib/winlibs (Windows DLL(Dynamic-link library) interface)
- lib/autohotkey ([AutoHotKey](https://catswords.re.kr/go/autohotkey) interface)
- lib/autoit3 ([AutoIt3](https://catswords.re.kr/go/autoit3) interface)
- lib/cloudflare ([Cloudflare Argo Tunnel](https://catswords.re.kr/go/argotunnel) interface)
- lib/shadowsocks ([Shadowsocks](https://catswords.re.kr/go/shadowsocks) interface)
- lib/excel (Microsoft Excel interface)
- lib/vbscript (VBScript interface)
- lib/wintap (Windows-TAP interface)
- lib/tun2socks (TUN2SOCKS interface)
- lib/hosts (Hosts file interface)
- lib/gtk (GTK-server GUI interface)

## Make your own `sayhello` example

### 1. Write a file `lib/sayhello-lib.js`
```
exports.VERSIONINFO = "sayhello library (sayhello-lib.js) version 0.1
exports.global = global;
exports.require = global.require;

exports.say = function() {
    console.log("hello");
}
```

### 2. Write a file `sayhello.js`
```
var sayhello = require("lib/sayhello-lib");
exports.main = function() {
    sayhello.say();
};
```

### 3. Execute file on the command prompt
```
C:\Users\John\Documents\GitHub\welsonjs> cscript app.js sayhello
hello
```

## Make own setup file
- compile `setup.iss` file with [Inno Setup](https://jrsoftware.org/isinfo.php)

## Screenshot
![Screenshot 1](app/assets/img/screenshot.png)

## Thanks!
![Thanks 1](app/assets/img/thanks.png)

- https://www.facebook.com/javascript4u/posts/1484014618472735
- https://python5.com/q/xtbsqjxb

## Related projects
- [gnh1201/wsh-js-gtk](https://github.com/gnh1201/wsh-js-gtk) - GTK GUI ported to Windows Scripting Host - Javascript (Microsoft JScript) (wsh-js)
- [gnh1201/wsh-json](https://github.com/gnh1201/wsh-json) - JSON stringify/parse (encode/decode) for Windows Scripting Host
- [redskyit/wsh-appjs](https://github.com/redskyit/wsh-appjs) - require-js and app framework for Windows Scripting Host JavaScript
- [JohnLaTwC's gist](https://gist.github.com/JohnLaTwC/4315bbbd89da0996f5c08c032b391799) - JavaScript RAT
- [JSMan-/JS-Framework](https://github.com/JSMan-/JS-Framework) - No description
- [iconjack/setTimeout-for-windows-script-host](https://github.com/iconjack/setTimeout-for-windows-script-host) - Replacement for the missing setTimeout and clearTimeout function in Windows Script Host
- [johnjohnsp1/WindowsScriptHostExtension](https://github.com/johnjohnsp1/WindowsScriptHostExtension) - Inject DLL Prototype using Microsoft.Windows.ACTCTX COM Object
- [kuntashov/jsunit](https://github.com/kuntashov/jsunit) - JSUnit port for Windows Scripting Host
- [nickdoth/WSHHttpServer](https://github.com/nickdoth/WSHHttpServer) - HTTP server based on Windows Script Host

## Contact me
- gnh1201@gmail.com
