# WelsonJS Quick Start

[한국어 문서](quick-start.md)

This guide is for WelsonJS 0.2.7.62. It walks you through installing WelsonJS, using the code editor with its AI tools, and running your first script. Developers who want to work from the source code can skip to [For developers](#for-developers).

## Requirements

- **Windows** with administrator rights for the installation.
- **An internet connection** during the installation. The installer downloads the WelsonJS binaries and the components you choose.
- **Microsoft Edge or Google Chrome** for the code editor. The editor opens in your browser.
- **.NET Framework 4.7.2 or later** for the WelsonJS Launcher. Windows 10 (version 1803) and later include it.
- **For Local AI:** 64-bit Windows (x64 or ARM64) and enough disk space and memory for the models you install. The models are listed under [Choose components](#choose-components).

## 1. Install

1. Download `welsonjs_0.2.7.62_setup.exe` from the [Releases page](https://github.com/gnh1201/welsonjs/releases/latest). It is signed by SignPath Foundation.
2. Run it and allow it to make changes (it needs administrator rights).
3. **Customer Information** (optional): you can leave the fields empty. Your details are sent to Catswords Research only if you fill them in and tick the consent box.
4. Choose the components (see below), then let the installation finish.

When the files are copied, a PowerShell window runs the post-installation script. It downloads the WelsonJS binaries and the components you chose, which can take a while for the AI models. **Do not close this window.** When it finishes, the WelsonJS Launcher starts.

WelsonJS is installed in `C:\Program Files (x86)\WelsonJS`. The downloaded components, including llama.cpp and the AI models, go to `%APPDATA%\WelsonJS`.

### Choose components

The default **Full** selection includes:

| Component | What it does |
| --- | --- |
| Associate .js files to run with WelsonJS | Double-clicking a `.js` file runs it with WelsonJS instead of Windows Script Host. Clear it if you want to keep the Windows default. |
| Register the welsonjs:// URI scheme | Lets `welsonjs://` links start WelsonJS. Any web page can open such a link, so clear it if you do not need it. |
| WelsonJS Launcher and Windows Service | The launcher and code editor, and the WelsonJS Windows service. |
| Configure WelsonJS MCP server for supported AI CLIs | Registers WelsonJS as an MCP server for AI command-line tools that are installed, such as Claude Code. |
| Python, cURL, websocat | Tools that some WelsonJS modules use. |
| **Local AI models** (under Llama.cpp) | Models that the editor's AI tools run on your PC. llama.cpp is installed with them. |

The Local AI models are about 4.9 GB in total:

| Model | Download | Use |
| --- | --- | --- |
| Hy-MT2 1.8B 1.25-bit (Tencent) | 462 MB | Translation, and the default Local AI model |
| FrogNano 4B 2609 | 2.8 GB | Coding questions and code writing |
| BitNet b1.58 2B-4T (Microsoft) | 1.2 GB | General questions with a 1.58-bit model |
| Qwen3Guard-Gen-0.6B (Qwen) | 484 MB | Checks prompts for unsafe content (LLM guard) |

Not sure which local models to choose? Enable the ones that match what you need:

- **Testing the LLM safety features:** Qwen3Guard-Gen-0.6B (Qwen).
- **Software engineering (SWE), such as coding questions and writing code:** FrogNano 4B 2609.
- **Translation between languages:** Hy-MT2 1.8B 1.25-bit (Tencent).

To install less, choose **Custom** and tick only what you need. For example, Hy-MT2 alone is enough for translation. You can run the installer again later to add components.

## 2. Start the launcher and the editor

- Start **Start WelsonJS Launcher** from the Start menu. It runs as administrator.
- The code editor opens in an app window of Edge (or Chrome) at `http://localhost:3000/`. To open it again, click **Start the editor...** in the launcher, or use the launcher's tray icon (**Open the code editor...**).
- Closing the launcher window keeps it in the tray. To quit it, use **Exit** in the tray icon menu.

The launcher uses its own browser profile, so signing in to web services there does not affect your usual browser. If you upgraded from an earlier version, close the browser windows that the earlier launcher opened first.

## 3. Write and run a script

The editor starts with an example script. A WelsonJS script exports a `main` function:

```js
// hello.js
function main() {
    console.log("Hello World");
}

exports.main = main;
```

- Click **Run File** to run the script. Its output appears in a terminal window.
- **Open File** and **Save File** load and save scripts on your PC.
- **Terminal** opens a command prompt.

Scripts run on the JScript engine of Windows Script Host (based on ES3). Use `var` and `function`. ES6+ syntax such as `let`, `const`, arrow functions and classes is not available. Modules in the `lib` folder are loaded with `require`, for example `var HTTP = require("lib/http");`.

## 4. Use the AI tools

The **Inference (AI)** group in the ribbon has a button that shows the current AI service. Click it to switch to the next service:

- **Local AI** (Hy-MT2, FrogNano, BitNet): runs on your PC. Nothing is sent to the internet.
- **Web chat services** (Copilot, ChatGPT, Claude, Gemini, Grok, DeepSeek, Perplexity, Meta AI, Mistral, Qwen, Kimi, Z.ai): the launcher asks the service in its browser for you.

Select text in the editor and right-click it to use the following menu items:

| Menu | What it does |
| --- | --- |
| Send Selection | Sends the selected text. |
| Explain Selection | Explains the selected code or text. |
| Find Problems in Selection | Looks for bugs in the selected code. |
| Translate Selection into ... | Translates into your system language or English. Local AI always translates with Hy-MT2. |
| Write Code | Writes code from your description, in JavaScript that runs on WelsonJS. |
| Ask... | Asks a question of your own. |

The answer is inserted into the editor as a comment. Switching the service clears the accumulated conversation.

### Local AI

- The first request starts llama.cpp and loads the model, which can take a little while. Later requests are faster.
- If a model is not installed, its requests fail. Run the installer again and choose the model.
- FrogNano and BitNet answer best in English, so the launcher translates your message into English with Hy-MT2 first. Install Hy-MT2 together with them.

### Web chat services

- The first time you use a service, its page opens in a launcher window. **Sign in there** if the service asks you to. The launcher waits up to 3 minutes for the message box.
- The window may stay behind other windows, but do not minimize it while it answers.
- Each service's terms of use and privacy policy apply to what you send.

## 5. Network tools

The **Network Tools** group looks up information about a host:

- **Whois**: domain registration information.
- **DNS**: the DNS records of a hostname. If you enter an IP address, its reverse name is looked up.
- **IP**: threat intelligence for an IP address from Criminal IP and AbuseIPDB. This needs your own API keys (see below).

## 6. Change settings

The launcher's settings are in `C:\Program Files (x86)\WelsonJS\bin\x86\WelsonJS.Launcher.exe.config`. Edit the file as administrator, then restart the launcher.

| Setting | Use |
| --- | --- |
| `IpQueryApiKey` | Criminal IP API key, for the IP tool |
| `IpQueryApiKey2` | AbuseIPDB API key, for the IP tool |
| `DnsServerAddress` | DNS servers for the DNS tool, separated by commas (default `1.1.1.1,8.8.8.8`) |
| `ResourceServerApiKey` | API key for other software that uses the launcher's OpenAI-compatible API |
| `AzureClientId`, `AzureTenant` | Sign-in for API keys from Azure Key Vault (**Settings > Sign in to Azure...**; see [azure-key-vault.md](azure-key-vault.md)) |

The installer replaces this file when you upgrade. Note your changes, and set them again after upgrading.

## 7. Use the AI from other programs (optional)

The launcher has an OpenAI-compatible API, so programs that support OpenAI can use the Local AI models and the web chat services:

- Base URL: `http://localhost:3000/v1`
- Models: `hy_mt2_1_8b_1_25bit`, `frognano_4b_2609`, `bitnet_b1_58_2b_4t`, and the web chat names such as `copilot` and `chatgpt`

Programs on this PC can use it while the launcher runs. Some programs, such as apps built on a browser engine, need the API key: change `ResourceServerApiKey` from the public example value to your own key of 16 characters or more. See [openai-compatible-api.md](openai-compatible-api.md) for details.

## 8. Troubleshooting

- **The editor does not open.** Click **Start the editor...** in the launcher. Check that Edge or Chrome is installed.
- **A web chat window opens, but no message is sent.** Close all browser windows that the launcher opened, including the editor, then start the editor again from the launcher.
- **Local AI does not answer.** Check that the model was installed: there is a folder named after it, such as `%APPDATA%\WelsonJS\hy_mt2_1_8b_1_25bit`, with a `.gguf` file. Also check that `%APPDATA%\WelsonJS\llamacpp\llama-server.exe` exists.
- **Logs:** `C:\Program Files (x86)\WelsonJS\bin\x86\logs\welsonjs.log`.
- **Help:** [GitHub Issues](https://github.com/gnh1201/welsonjs/issues), or the Discord and Kakao+ buttons in the editor.

---

## For developers

You can use WelsonJS from the source code without the installer. Basic use needs no additional software, because scripts run on the JScript engine of Windows Script Host, which Windows includes. WelsonJS runs on Windows XP SP3 or later (verified up to Windows 11 24H2).

### Get the source

```cmd
git clone https://github.com/gnh1201/welsonjs.git
cd welsonjs
```

You can also download and start it with one PowerShell line. This downloads the `master` branch into `%TEMP%` and runs `bootstrap.bat`:

```powershell
irm https://catswords.blob.core.windows.net/welsonjs/bootstrap.ps1 | iex
```

### (Optional) Initial setup

```cmd
bootstrap.bat
```

`bootstrap.bat` does two things:

- It registers the `WelsonJS.ManagedObject` COM component, downloading it if `bin\x86\WelsonJS.ManagedObject.dll` is missing.
- It runs `cscript app.js bootstrap`. On Windows earlier than 8.1, this step also configures the HTA file association.

It changes the registry and COM registrations, so it may need administrator rights. Skip it if you use only pure JScript modules.

### Run a script

```cmd
cscript app.js helloworld
```

`app.js` loads the module named by the first argument (`.js` is added when the extension is omitted) and calls its `main(args)`. Extra arguments are passed as an array, and a number returned by `main` becomes the exit code.

A module and an entry point look like this:

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

### More

- **GUI (HTA):** `mshta app.hta` shows `app/index.html` and runs `app/index.js`. Edit the files in `app/`.
- **Other languages:** TypeScript (`.ts`), CoffeeScript (`.coffee`), LiveScript (`.ls`) and ReScript (`.re`, `.res`) are converted before running. Include the extension, as in `cscript app.js helloworld.coffee`.
- **Settings:** define values in `settings.ini`, based on `settings.example.ini`. Point secrets to a file, such as `file:data/chatgpt-apikey.txt`, and do not commit it.
- **Tests:** `cscript app.js testloader es5_polyfills data\test-oss-korea-2023.json` runs one test. Some tests change Windows settings or need other programs or user input, so check the test in `testloader.js` first.
- **Local AI from scripts:** `lib/llamacpp.js` uses the launcher's llama.cpp router. See [llamacpp-launcher-api.md](llamacpp-launcher-api.md).

## Next steps

- [README](../README.md): features, release methods, and screenshots
- [CHANGELOG](../CHANGELOG.md): what changed in each release
- [`lib/`](../lib): modules for files, the registry, HTTP, Office automation, LLM services, and more
- [`examples/`](../examples): usage examples
- [Contributing guide](../CONTRIBUTING.md)
- [MCP server wiki](https://github.com/gnh1201/welsonjs/wiki/MCP-server)
