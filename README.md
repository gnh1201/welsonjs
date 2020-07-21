# welsonjs
WelsonJS - Build a Windows desktop apps with JavaScript, HTML, and CSS based on WSH/HTA

## Structure
![Structure of WelsonJS](app/assets/img/structure.png)

## Specifications
- ES5(ECMAScript 5), ES6(ECMAScript 6) compatibility with [es5-shim](https://github.com/es-shims/es5-shim) and [es6-shim](https://github.com/es-shims/es5-shim)
- HTML5 compatibility with [html5shiv](https://github.com/aFarkas/html5shiv) and [jquery-html5-placeholder-shim](https://github.com/jcampbell1/jquery-html5-placeholder-shim)
- [module.exports](https://nodejs.org/en/knowledge/getting-started/what-is-require/)(NodeJS) styled module implementation

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
- lib/sendmail (example of sendmail)
- lib/shell (Command Prompt interface)
- lib/timer (`setTimeout` implementation for not supported environment)
- lib/powershell (Windows Powershell interface)

## Related projects
- [gnh1201/wsh-js-gtk](https://github.com/gnh1201/wsh-js-gtk) - GTK GUI ported to Windows Scripting Host - Javascript (Microsoft JScript) (wsh-js)
- [gnh1201/wsh-json](https://github.com/gnh1201/wsh-json) - JSON stringify/parse (encode/decode) for Windows Scripting Host
- [redskyit/wsh-appjs](https://github.com/redskyit/wsh-appjs) - require-js and app framework for Windows Scripting Host JavaScript
- [JohnLaTwC's gist](https://gist.github.com/JohnLaTwC/4315bbbd89da0996f5c08c032b391799) - JavaScript RAT
- [JSMan-/JS-Framework](https://github.com/JSman-/JS-Framework) - No description
- [iconjack/setTimeout-for-windows-script-host](https://github.com/iconjack/setTimeout-for-windows-script-host) - Replacement for the missing setTimeout and clearTimeout function in Windows Script Host

## Contact me
- gnh1201@gmail.com
