# WelsonJS Quick Start

[한국어 문서](quick-start.md)

This document walks you through downloading WelsonJS and running your first script.

## Requirements

- Windows XP SP3 or later (verified up to Windows 11 24H2)
- No additional software is required for basic use. Scripts run on the JScript engine of the built-in Windows Script Host (WSH).
- Some modules require external programs, network access, API keys, or administrator privileges. Check a module's implementation before using it.

## 1. Download

Choose one of the following.

**Clone with Git**

```cmd
git clone https://github.com/gnh1201/welsonjs.git
cd welsonjs
```

**One-line PowerShell install**

```powershell
irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex
```

This script downloads the `master` branch into a temporary folder (`%TEMP%`), extracts it, and runs `bootstrap.bat`. To run a specific file directly, save the script and pass arguments.

```powershell
irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 -OutFile bootstrap.ps1
.\bootstrap.ps1 -file helloworld.js
```

**WelsonJS Launcher**

Download the [WelsonJS Launcher](https://catswords.blob.core.windows.net/welsonjs/welsonjs_launcher_latest.zip) to manage instances and use the variable editor and code editor through a GUI.

## 2. (Optional) Initial setup

```cmd
bootstrap.bat
```

`bootstrap.bat` does the following:

- Imports `app\assets\reg\Default_HTA.reg` to configure the HTA file association (registry change).
- Registers the `WelsonJS.ManagedObject` component with `RegAsm.exe`. If the local `bin\x86\WelsonJS.ManagedObject.dll` is missing, it downloads the component from the internet.
- Finally runs `cscript app.js bootstrap`.

Because it changes the registry and COM registrations, it may require administrator privileges. You can skip this step if you only use pure JScript modules.

## 3. Run your first script

From the repository root, run:

```cmd
cscript app.js helloworld
```

`app.js` loads the module named by the first argument with `require()` and calls its `main(args)`. If you omit the extension, `.js` is appended. `helloworld.js` exercises the HTTP client, hashing, dialogs, and more, so some of its features work only after the managed object is registered by `bootstrap.bat`.

## 4. Create your own module

### Write `lib/sayhello.js`

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

### Write the entry point `sayhello.js`

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

### Run it

```cmd
C:\...\welsonjs> cscript app.js sayhello
calling say()
hello
ended say()
```

Extra arguments, as in `cscript app.js sayhello foo bar`, are passed to `main()` as the array `["foo", "bar"]`. If `main()` returns a number, it becomes the exit code.

> **Note**: The engine is legacy JScript (ES3-based). Some ES5 polyfills such as `core-js` are included, but ES6+ syntax such as `let`/`const`, arrow functions, and classes is not available.

## 5. GUI applications (HTA)

```cmd
mshta app.hta
```

`app.hta` loads `webloader.js`, which displays `app/index.html` with its stylesheets and then runs `app/index.js`. Edit the files in the `app/` folder to change the UI and behavior.

## 6. Writing in other languages

TypeScript (`.ts`), CoffeeScript (`.coffee`), LiveScript (`.ls`), and ReScript (`.re`, `.res`) files are converted by the built-in transpilers before running. Include the extension:

```cmd
cscript app.js helloworld.coffee
```

## 7. Configuration

Define environment values in `settings.ini`, using `settings.example.ini` as a reference. Point sensitive values such as API keys to a separate file, for example `file:data/chatgpt-apikey.txt`, and do not commit that file to the repository.

## 8. Running tests

`testloader.js` takes a test ID and a profile path and runs a single test.

```cmd
cscript app.js testloader es5_polyfills data\test-oss-korea-2023.json
```

Depending on the test, it may change Windows settings or require external programs, network access, or user input, so check its implementation in `testloader.js` first. The window stays open for 90 seconds after the run.

## Next steps

- [README](../README.md): features, release methods, and screenshots
- [`lib/`](../lib): modules for files, registry, HTTP, Office automation, LLM integrations, and more
- [`examples/`](../examples): usage examples
- [Contributing guide](../CONTRIBUTING.md)
- [MCP server wiki](https://github.com/gnh1201/welsonjs/wiki/MCP-server)
