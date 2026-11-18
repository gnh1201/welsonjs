# Changelog

[한국어 문서](CHANGELOG_KO.md)

All notable changes to WelsonJS are recorded in this file. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

- Each version is a Git tag of this repository. Dates are the tag dates.
- The entries come from the GitHub release notes, the merged pull requests, and the commit history between tags. Releases without notes are summarized from their commits.
- Issue numbers are linked where a change is tied to an issue: the release notes name it, a commit or pull request references it, or (marked "related to") the change implements what the issue describes.

Notes on the tags:

- `0.7.34` is the tag of version 0.2.7.34 (misnamed). Around that time the version was also displayed as `2.7.x` instead of `0.2.7.x`; 0.2.7.36 fixed it.
- There are no tags for 0.2.7.35, 0.2.7.58, and 0.2.7.59.
- `0.2.5`, `0.2.5.2`, `0.2.7.5-PRE`, `0.2.7.33`, and `0.7.34` have no GitHub release notes.

## [Unreleased]

### Added

- `lib/is.js`, a checks module written for this framework. It keeps the method names and the meaning of is.js 0.9.0, which `app.js` used to load as the global `is`, without third-party code: the type, presence, arithmetic, format, string, date, object and array checks, and the `not`, `all` and `any` interfaces. The browser and device detection group is left out, because the runtime is Windows Script Host; `is.windows()`, `is.linux()`, `is.mac()`, `is.online()` and `is.offline()` answer for the host instead, and `lib/browser.js` covers an embedded browser (related to [#434]).
  - `is.url()` and `is.email()` are parsed once from left to right over a length limited value instead of being matched by a regular expression, so no input can make them backtrack; the other format checks use anchored patterns and a length limit.
- `is_test` in `data/test-misc.json` and `testloader.js`, which checks the module and the response time of `is.url()` on hostile input: `cscript app.js testloader is_test data\test-misc.json` (related to [#434]).

### Changed

- The `microsoft_jdk` installer component installs the JDK instead of only staging its installer. `postInstall.ps1` downloads the vendor installer, runs it unattended (`/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`, plus `/ALLUSERS` when the script is elevated and `/CURRENTUSER` otherwise, and `/TASKS="FeatureEnvironment,FeatureJarFileRunWith"`), reports the exit code (including 3010, a pending restart), and leaves `JAVA_HOME` alone, because the vendor installer sets it only for the optional `FeatureJavaHome` task. The component description in `setup.iss` says that it installs.
- `data/DownloadUrls.psd1`: the Microsoft Build of OpenJDK links point at 25.0.5.0.2, the current Windows patch of JDK 25 LTS. `arm64` has a URL where it was `$null`, and `x86` is empty instead of pointing at the AArch64 installer, because Microsoft publishes no 32-bit build of JDK 25 (JEP 503 removed the x86 port).

### Security

- Removed the assembly binding redirects for Google.Protobuf and the five gRPC assemblies from `WelsonJS.Service/app.config`. Their packages left the build in 0.2.7.62 with the heartbeat gRPC client, so only the redirects remained (related to [#434]).
- Removed the `is.js` 0.9.0 dependency, which has no patched release for CVE-2020-26302 (ReDoS in `is.url()`): its two integrity sidecars and its entries in `app/assets/integrity.json` are gone, so the library is no longer downloaded or loaded. The global `is` stays, published from `lib/is.js`, so an application that called `is` keeps working (related to [#434]).
- Added `docs/third-party-dependencies.md`, which records the state of the three components reported by the Open Source Software Assessment of 2026-08-05, including why the jQuery 1.11.3 fallback that IE 6 to 8 needs cannot be dropped yet (related to [#434]).

## [0.2.7.62] - 2026-11-16

Changes since 0.2.7.62-rc1, a pre-release without an installer. When upgrading from 0.2.7.61, see [0.2.7.62-rc1](#0262-rc1---2026-11-01) as well.

Before upgrading:

- The installer replaces the launcher's settings file (`bin\x86\WelsonJS.Launcher.exe.config`). Note the values you changed there, such as the IP query API keys (`IpQueryApiKey`, `IpQueryApiKey2`) and `ResourceServerApiKey`, and set them again after upgrading. Settings removed in this release (listed below) are no longer read.
- Close the windows of the browser that an earlier launcher opened (the editor and web chat windows); the new launcher starts its own browser.

### Added

- Local AI in the WelsonJS Editor: a llama.cpp build for WelsonJS (`native/llamacpp`) that is built from source in the AppVeyor Release job, cached, signed with the other artifacts, and installed from the artifacts package to `%APPDATA%\WelsonJS\llamacpp`.
  - The source is the llama.cpp fork that Microsoft distributes with bitnet.cpp, so BitNet b1.58 (I2_S) models run (related to [#519]).
  - Patches add Tencent's AngelSlim 1.25-bit STQ1_0 type, so `tencent/Hy-MT2-1.8B-1.25Bit-GGUF` runs; type 42 files are detected by tensor size without renumbering the fork's types.
  - Builds for x64 (MSVC, every CPU variant) and ARM64 (clang cross-compile) (related to [#40]).
  - Every executable and DLL carries version information: the WelsonJS version (0.2.7.62), a description of the binary ("... (llama.cpp built for WelsonJS)"), `CompanyName` Catswords Research, and the copyright of the ggml authors (llama.cpp), Microsoft (bitnet.cpp), Tencent (AngelSlim, STQ1_0), and Catswords Research. The executables also carry the WelsonJS icon.
  - Every executable and DLL of both architectures is signed, each listed by name in `.artifact-configuration.xml` (no wildcards); the build fails when a binary lacks the version information or is not listed for signing.
  - `llama-quantize` can produce I2_S files: the fork's quantizer wrote a layout its kernels do not read, and the type was missing from the tool. STQ1_0 quantization is documented for Sherry-trained checkpoints.
  - The README explains quantizing with `llama-quantize`, with examples: select the type by name (`I2_S`, `STQ1_0`), as the file type numbers differ between builds.
- The editor's AI service toggle switches between the Local AI models (llama.cpp: Hy-MT2, the default, FrogNano and BitNet, from the launcher's `LlamaCppModels` setting) and web chat services: Copilot, ChatGPT, and more. The context menu shows Send Selection, Explain Selection, Find Problems in Selection, `Translate Selection into <language> (<code>)` for the system language and for English (en), Write Code (JavaScript, ES3 for the WSH JScript engine), and Ask... for the chosen service, and the system language is sent with prompts, to be used for the text that is not code only. Switching the service clears the prompt.
- OpenAI-compatible launcher API: `POST /v1/chat/completions` and `GET /v1/models` for the llama.cpp models and the web chat services (Copilot, ChatGPT, Claude, ...), which the launcher now drives in its browser itself; the services moved from the editor to `webchat.js`, and the editor uses the API as well. Responses are not streamed (`"stream": true` sends the finished answer as one chunk). Other local software (for example apps built on a browser engine) can use it with an API key (the launcher's `ResourceServerApiKey`, sent as `Authorization: Bearer`). See `docs/openai-compatible-api.md`.
- UberLLM provider in the language inference engine ([#543]).
- Launcher-managed LLM guard (Qwen3Guard) for prompts.
- This changelog, in English and Korean (`CHANGELOG.md`, `CHANGELOG_KO.md`), covering every release tag.

### Changed

- New installer component `llamacpp\bitnet_b1_58_2b_4t`: BitNet b1.58 2B-4T (Microsoft, 1.58-bit I2_S, `microsoft/bitnet-b1.58-2B-4T-gguf`, about 1.2 GB), which the WelsonJS llama.cpp build runs (related to [#519]).
- The default Hy-MT2 model is the 1.8B 1.25-bit GGUF (462 MB) instead of Q4_K_M (about 1.1 GB). The component is now `hy_mt2_1_8b_1_25bit` (related to [#542]).
- The installer no longer downloads the official llama.cpp package; it uses the WelsonJS build from the artifacts package. Qwen3Guard is grouped under the llama.cpp components.
- The launcher editor's AI tools use the local llama.cpp server; the launcher's Copilot menu and `CopilotUrl` setting are removed.
- llama.cpp runs as one `llama-server` in router mode (model hot swap), shared by the editor's Local AI, the LLM guard, and `lib/llamacpp.js`. Models are the folders in `%APPDATA%\WelsonJS` that hold a `.gguf` file, named after the installer component, and every request names its model. The least recently used model is unloaded beyond `LlamaCppModelsMax` (default 3).
  - FrogNano and BitNet work best in English: for them (`LlamaCppTranslatedModels`) the launcher translates the user messages into English with Hy-MT2 (`LlamaCppTranslationModel`), leaving code blocks and inline code unchanged, and tells them the user's system language, without an instruction about the reply language; the answer is not translated. The editor sends its translation actions to Hy-MT2 directly.
  - Launcher API: `/llamacpp/status`, `models`, `start`, `stop`, `load`, `unload`, and `chat`, which requires `model`; for local callers only. See `docs/llamacpp-launcher-api.md`.
  - Settings: `LlamaCppModels` (a comma-separated list of the editor's models) and `LlmGuardModel` (a model name) replace `LlamaCppModelPath` and `LlmGuardModelPath`; new `LlamaCppModelsMax` and `LlamaCppContextSize`.
  - `lib/llamacpp.js` selects models by name (`setModel`, `loadModel`, `unloadModel`, `listModels`, `chat`); the model path, port, and `openBrowser` options are removed.
- The launcher's child processes (the llama.cpp router with its model instances, terminal sessions and JSRT instances) are in a Windows job object that ends them with the launcher, also when it is killed, stopped in the debugger or crashes.
- The launcher chooses a free remote debugging port for its browser when it starts it, instead of the fixed 9222 (`ChromiumDevToolsPrefix` is removed), and the browser listens on 127.0.0.1 only. The launcher keeps the port in memory while the browser answers on it; a browser left from an earlier launcher run is not used (close its windows). The ports of the llama.cpp router and the browser are handled by one shared implementation (`LocalPorts`).
- The addresses of the third-party services that the launcher's WHOIS and IP query endpoints call come from the blob config (`blob.config.xml`, `services`) instead of the local settings (`WhoisServerUrl`, `WhoisReferrerUrl`, `WhoisClientAddress`, `IpQueryApiPrefix` and `IpQueryApiPrefix2` are removed); the IP query API keys stay in the local settings. Requests made before the blob config is loaded wait for it, and a failed load is retried on the next request. The WHOIS endpoint no longer sets the referrer on the shared HTTP client.
- The launcher's DNS query tool asks for all record types at the same time (asynchronously, at most 3 seconds per attempt, instead of one after another for up to 5 seconds each), asks again over TCP when an answer is truncated, checks that the answer matches the query, and parses names with compression correctly. It looks up IP addresses in reverse (PTR), converts internationalized names to punycode, shows the TTL and every SOA, NAPTR and multi-string TXT field, and lists the types without records on one line. `DnsServerAddress` takes several servers separated by commas (now `1.1.1.1,8.8.8.8`): when one does not answer, fails or refuses, the next one is asked, and every server is asked once before any is asked again. `?type=` selects record types.
- Vendor JavaScript files are restored on demand (`restoreAppAssets.ps1`).
- `app.hta` is opened with `welsonjs-hta.exe` in the bootstrap; YAWW updated.

### Fixed

- `WelsonJS.ManagedObject` COM bridge improvements and leak fixes ([#429]), and its build for .NET Framework 2.0.
- `welsonjs-hta.exe` bugs on x86 environments.
- Script directory decoding from `file:` URLs.
- Picture size handling in `resolvePictureSize`.
- llama.cpp: STQ1_0 files with file type 42, as PR #22836 writes them, were reported as `TL1`; they are reported as STQ1_0.
- `lib/aviation.js` did not compile (a missing parenthesis), so `require("lib/aviation")` failed; its `limit` and `offset` are now parsed in base 10 (the JScript engine reads a leading zero as octal), and `offset` is parsed from `offset` instead of `limit` (radix reported by [@avelankwon](https://github.com/avelankwon)).

### Security

- Hardened the launcher's `ResourceServer`.

## [0.2.7.62-rc1] - 2026-11-01

Pre-release. No installer is provided; build from source to try it.

### Added

- WebView support based on YAWW, with build configurations for WebView2, EdgeHTML, and MSHTML (related to [#157], [#510]).
- Installer packaging with Inno Setup in the AppVeyor build ([#486]).
- MCP (Model Context Protocol) enhancements: notification dispatch, session state, optional client setup, and verification with Claude Code, Codex CLI, and Copilot CLI ([#474]).
- JSON-RPC 2.0 method registration, dispatch, and per-request state.
- Microsoft Office helpers: PowerPoint slides and export, Word document lifecycle, and Office shapes.
- HWP automation support and component.
- Cheaper Inference ([#523]) and OrcaRouter integrations in the language inference engine.
- Object storage endpoint failover, a TLS 1.3 install option, Scrapingant proxies, and more downloadable GGUF models (for example, SmolLM2 135M).
- `SKILL.md` for MCP usage, and Korean contribution guidelines.

### Changed

- Reorganized the managed components under `native/`; updated ManagedEsent and the submodules.
- Reduced the gRPC external dependencies of `WelsonJS.Service` ([#477]).
- Added an `allowUnsafeEval` gate to the JavaScript eval tools, and restricted eval in the ES3 evaluator.
- Updated the CodeQL workflow, the architecture and contribution guides, and the README.

### Fixed

- Minor bugs and potential security issues in core modules.

## [0.2.7.61] - 2026-08-24

### Added

- MsCompress (MSCOMP/SZDD) compression and decompression.
- APIs in `WelsonJS.ManagedObject` that were previously available in `WelsonJS.Toolkit`.

### Security

- Fixed vulnerability [GHSA-9jmm-5v6v-gpq2](https://github.com/gnh1201/welsonjs/security/advisories/GHSA-9jmm-5v6v-gpq2) (KVE-2026-2271). Users of earlier versions should upgrade.

## [0.2.7.60] - 2026-08-17

### Added

- `WelsonJS.ManagedObject`, the successor to `WelsonJS.Toolkit`, published to NuGet ([#440], [#427]).
  - COM wrapper support for `ByRef` parameters ([#429]).
  - Tracking of objects created by `WelsonJS.ManagedObject` ([#449]).
- AppSignal-based error tracking in `WelsonJS.Launcher`.
- Lightweight SRI support ([#420]), with SHA-512/SHA-384 hash algorithms ([#424]) and a CAPICOM fallback.
- `Catswords.Phantomizer`, a dynamic assembly loader that loads DLLs over the network, with a flexible scheme ([#368]).
- A one-click installation script ([#404]).
- A Stdin server implemented entirely in JavaScript ([#397]).
- MCP (Model Context Protocol) support ([#233]).
- Telemetry in the launcher, TLS security protocol checks, a TLS 1.2 offline inspector, and Outlook (classic) integration.

### Changed

- Renamed the native solution from `WelsonJS.Toolkit` to `WelsonJS.Augmented`.
- Logging moved to log4net and then to a file-based `TraceLogger`.

### Fixed

- Dynamic assembly loader issues on some air-gapped systems ([#412]).
- Crashes when the `htmlfile` module is corrupted ([#399]).
- The seamless launcher and the post-installation script on Windows 8.1 ([#376], [#374]).
- Secure coding fixes from the KISA CRC checklist (related to [#411]).

## [0.2.7.57] - 2025-12-01

### Added

- Optional binary downloads in the post-install script based on usage: Python, cURL, websocat, YARA, WAMR, tessdata, GTK3 runtime, Nmap, WinDivert, Android platform tools, and more ([#259]).
- Experimental telemetry (PostHog) in the post-install script.

### Changed

- Improved the post-install script (PR [#353]); reduced package size by decoupling binaries ([#259]).

## [0.2.7.56] - 2025-11-24

### Added

- WelsonJS JCTG (JavaScript-Click-To-Go): file association, so `.js` files run with a double click in File Explorer.
- `Array.from()` for `Enumerator` objects.
- TOTP with 32-character keys, ChakraCore-based serialization, an image color picker, Chromium app mode, CORS support in `ResourceServer`, and multiple IP query providers.

### Changed

- Reduced the installer size from 58 MB to 8 MB.
- Deprecated `Enumerator.toArray` and `getFilesFromFolder` (now `getFiles`).

### Fixed

- Various reported issues and minor bugs.

## [0.2.7.55] - 2025-08-18

### Added

- `UseObject(progId, callback)`, similar to the `using` keyword.
- ESENT (Windows built-in database) support and an ESENT-based metadata store in the launcher ([#277]).
- `WelsonJS.Cryptography`: SEED, ARIA, and HIGHT with official test vectors ([#201], [#293], [#295]).
- WebSocket support for the DevTools protocol in the launcher.
- Azure AI (Azure AI Foundry), Criminal IP, and more LLM and API integrations in the editor; WHOIS and DNS results can be forwarded to LLM services.
- A cached blob, so the code editor is served while offline ([#220]).
- More Chromium-based browsers.

### Changed

- The WelsonJS Editor uses React.
- The default SERP provider is SerpApi.

### Fixed

- CDP port binding and browser test failures ([#246], [#251]).
- Compatibility of `std.js` with older `chrome.js` ([#271]), and the `--user-data-dir` flag set to null ([#274]).
- A potential license conflict with `System.IO.Compression.ZipFile` ([#254]).
- Slow cloning with GitHub Desktop, by removing the submodules ([#283]).

## [0.2.7.54] - 2025-03-23

### Added

- WelsonJS Editor, a rich code editor powered by Microsoft Monaco Editor ([#137]), with code completion from the installed software list ([#183]).
- Generative AI integrations in the editor (ChatGPT, Gemini, and more), including code generation.
- WHOIS and DNS query features, a TOTP endpoint, a tray icon, and a global settings form in the launcher.

### Changed

- Improved the launcher's start time and settings.

## [0.2.7.53] - 2024-11-24

### Added

- Proxy type `stateless-jsonrpc2` in `lib/http`.

### Changed

- Updated the search engine detection in `lib/http`.

## [0.2.7.52] - 2024-11-19

### Added

- Mathematical methods using numbers.js.

## [0.2.7.51] - 2024-11-15

### Fixed

- Log types were not displayed (a regression of 0.2.7.50).

## [0.2.7.50] - 2024-11-15

### Added

- The `numbers.js` library for advanced math operations.
- Groq LLM API support.

### Changed

- Improved type handling of the `console` object.
- Renamed some JavaScript library files in `assets/js`.

## [0.2.7.49] - 2024-11-05

### Changed

- Renamed `EncryptStringHIGHT`/`DecryptStringHIGHT` to `EncryptString`/`DecryptString`.
- Updated the NuGet package of `WelsonJS.Toolkit`.

## [0.2.7.48] - 2024-11-04

### Changed

- The screen time feature warns when the display scale is not 100%.

### Fixed

- Duplicate processing prevention of screen operations.

## [0.2.7.47] - 2024-11-04

### Added

- Anthropic (Claude) API integration.

### Fixed

- Bugs related to the settings and the logger.

## [0.2.7.46] - 2024-11-02

### Changed

- `WelsonJS.Service` logs through `Microsoft.Extensions.Logging` ([#154]).

## [0.2.7.45] - 2024-10-24

### Added

- NuGet profile for `WelsonJS.Toolkit`.

## [0.2.7.44] - 2024-10-21

### Added

- A user-defined variables editor (`.env` file based) in `WelsonJS.Launcher`.
- The `--env-file` parameter of the interactive service.

### Changed

- The documentation moved to catswords-oss.rdbl.io.
- Improved the screen time feature and SERP.

## [0.2.7.43] - 2024-10-03

### Added

- Missing parameters for detecting SERP.

### Changed

- New WelsonJS favicon.

## [0.2.7.42] - 2024-09-27

### Fixed

- The proxy URL was not applied in the default engine mode (MSXML).

## [0.2.7.41] - 2024-09-27

### Added

- SearchAPI.io SERP support.

### Changed

- Renamed `CharsetDetector` to `DetectCharset`.

### Fixed

- `.send()` did not work without a proxy ([#145]).

## [0.2.7.40] - 2024-09-26

### Fixed

- Tabs were duplicated indefinitely in the tab UI.
- Cached `stdout` and `stderr` were not cleared after shell requests.

## [0.2.7.39] - 2024-09-26

### Added

- A tab-based UI (Golden Layout).

### Fixed

- The identifier of `console.warn()` in `webloader.js`.

## [0.2.7.38] - 2024-09-25

### Added

- Proxy API services integration ([#143]).
- ARM support in `lib/http`.

## [0.2.7.37] - 2024-09-25

### Added

- Proxy API services, first part ([#143]).
- ClamAV integration in the file event monitor, and bitmap comparison with CRC32.

### Fixed

- Data was destroyed before `stdout` and `stderr` were read ([#144]).

## [0.2.7.36] - 2024-09-09

### Fixed

- The gRPC-based heartbeat service (PR [#139]).
- The file event monitor (file creation, network connection, and registry modification).
- The version was displayed as `2.7.x` instead of `0.2.7.x`.

## [0.7.34] - 2024-09-04

Version 0.2.7.34.

### Added

- OCR (tessdata) and clipboard sampling options in the screen time feature; a heartbeat gRPC definition.

### Fixed

- An `STAThreadAttribute` issue.

## [0.2.7.33] - 2024-08-30

### Added

- A parser generator (PEG.js) ([#135]).

### Changed

- Refactored the gRPC service migration to .NET ([#132]).

## [0.2.7.32] - 2024-08-27

### Fixed

- The gRPC client uses HTTP/1.1.
- The initial configuration is read from `settings.ini`.

## [0.2.7.31] - 2024-08-25

### Added

- The standalone `WelsonJS.Launcher` program.
- A YARA file rule matcher ([#131]).

### Changed

- The gRPC service was migrated to .NET ([#132]).
- The build moved to AppVeyor CI with SignPath code signing; checked-in binaries were removed ([#130]).

### Fixed

- Casting C# `string[]` to the built-in scripting engine ([#133]).
- The string formatter with JSON data ([#122]).
- The screen time feature.

## [0.2.7.30] - 2024-08-02

### Fixed

- Performance of the screen time feature in `WelsonJS.Service`. It is available in user interactive mode only; `--disable-screen-time` turns it off.

## [0.2.7.29] - 2024-07-31

### Fixed

- `.setPrefix()` of `lib/shell` ([#126]).

## [0.2.7.28] - 2024-07-30

### Changed

- Updated the assets and the installer.

## [0.2.7.27] - 2024-07-29

### Added

- The screen matching feature (related to [#124]).

## [0.2.7.26] - 2024-07-29

### Fixed

- The working directory of `WelsonJS.Service`.

## [0.2.7.25] - 2024-07-23

### Fixed

- The version information of `WelsonJS.Service`.

## [0.2.7.24] - 2024-07-23

### Changed

- The default script of `WelsonJS.Service` is `defaultService` (was `helloworld`).

## [0.2.7.23] - 2024-07-23

### Changed

- Updated the `WelsonJS.Toolkit` binaries.

## [0.2.7.22] - 2024-07-23

### Fixed

- Unwanted closing with `initializeWindow()` and `exit()`.

## [0.2.7.21] - 2024-07-23

### Changed

- Updated the `WelsonJS.Service` binaries ([#101]).

## [0.2.7.20] - 2024-07-23

### Added

- `WelsonJS.Service`, for writing Windows services in JavaScript ([#101]).

## [0.2.7.19] - 2024-07-22

### Fixed

- Remote script execution (one more fix).

## [0.2.7.18] - 2024-07-22

### Fixed

- Remote script execution.

## [0.2.7.17] - 2024-07-21

### Added

- Remote script execution in `app.js`.
- The `Certutil` HTTP request method in `lib/http`.

### Fixed

- `lib/http` with `setPrefix()` of `lib/shell`.

## [0.2.7.16] - 2024-07-20

### Fixed

- `this.evaluate()` (string formatter) with JSON data ([#122]).

## [0.2.7.15] - 2024-07-13

### Changed

- Updated the contact links.

## [0.2.7.14] - 2024-07-10

### Fixed

- A typo in `lib/wmi.js`, and the setup profile for updates.

## [0.2.7.13] - 2024-07-10

### Added

- `.setPrefix()` in `lib/shell`.
- BITS (bitsadmin) support in `lib/http` ([#119]).
- Nmap support with Caterpillar Proxy.

### Fixed

- Broken file paths.

## [0.2.7.13-rc1] - 2024-07-01

Pre-release.

### Added

- JSON-RPC 2.0 support.
- MySQL queries through Caterpillar Proxy.

### Changed

- `lib/xml` is managed by `lib/pipe-ipc`.

## [0.2.7.12] - 2024-05-28

### Added

- A `Bitmap` interface in the native component.

### Fixed

- Feedback of 2024-04-05 in `lib/http`, `lib/file`, and `lib/pipe-ipc` ([#109]).
- The User-Agent header was not changed ([#111]).
- Reading compressed responses in `lib/http`, `lib/adb.js`, and `setup.iss`.

## [0.2.7.11] - 2024-03-20

### Fixed

- Summernote issues.

## [0.2.7.10] - 2024-03-20

### Added

- `Browser.waitUntil()`.

## [0.2.7.9] - 2024-03-19

### Changed

- Updated `encryptor.js` and the README.

## [0.2.7.8] - 2024-01-27

### Changed

- Updated `bin/README.md`.

## [0.2.7.7] - 2024-01-27

### Added

- HIGHT (ISO/IEC 18033-3) encryption and decryption of script files ([#104]).

### Fixed

- cURL errors with non-escaped ampersands on the command prompt ([#103]).

## [0.2.7.6] - 2024-01-05

### Changed

- Improved shared memory access ([#96]) and a new version of `WelsonJS.Toolkit.dll`.

## [0.2.7.5] - 2023-12-29

### Added

- Transpiling ES6 generator functions ([#75]).
- An LZ77 (MsCompress) implementation.
- Shared memory access in the toolkit ([#96]).

## [0.2.7.5-PRE] - 2023-12-22

Pre-release.

### Added

- Shared memory access in the toolkit ([#96]).
- An LZ77 (MsCompress) implementation.

### Changed

- Updated `lib/msoffice` ([#93]).

## [0.2.7.4] - 2023-11-28

### Changed

- `excel.setValueByPosition(x, y, c)` is now `excel.getCellByPosition(x, y).setValue(c)` in `lib/msoffice`.

## [0.2.7.3] - 2023-11-27

### Added

- Microsoft Office (Excel) integration in `lib/msoffice` ([#93]).

## [0.2.7.2] - 2023-11-22

### Added

- A WYSIWYG HTML editor in the web loader ([#92]).
- A GUI router.

### Changed

- The test files were merged into `testloader.js`.

## [0.2.7.1] - 2023-10-26

### Fixed

- The broken GUI sample ([#86]).

## [0.2.7] - 2023-09-26

### Changed

- Improved concurrency in multiprocessing with PIPE IPC ([#84]), and the shell interface uses it.

### Fixed

- Some environment variables could not be referenced in ScriptControl mode ([#79]).

## [0.2.6.1] - 2023-09-11

### Changed

- The license changed from GPLv2 to GPLv3.

### Fixed

- Typos.

## [0.2.6] - 2023-09-07

### Added

- gRPC support ([#71]).
- CryptoJS support ([#72]).
- ReasonML/ReScript support ([#77]).

## [0.2.5.5] - 2023-08-02

### Added

- ReasonML/ReScript support ([#77]).
- CryptoJS support ([#72]).

## [0.2.5.4] - 2023-02-01

### Fixed

- `StdEventableObject`/`STD.EventableObject` were replaced with `StdEventTarget`/`STD.EventTarget` in `lib/std.js`.

## [0.2.5.3] - 2022-12-22

### Added

- TypeScript support ([#66]).

## [0.2.5.2] - 2022-12-22

### Added

- TypeScript support, first part.

## [0.2.5.1] - 2022-11-29

### Added

- CoffeeScript ([#65]) and LiveScript support.
- PIPE-based IPC (`lib/pipe-ipc`).
- Extra libraries `lib/extramath` and `lib/fakeworker`.
- `OpenThreatSettings()` in `lib/security` (opens Windows Security Center).
- Virtual keys F1 to F12 in `WelsonJS.Toolkit`.

### Changed

- Updated the contact for reporting abuse and security issues.

## [0.2.5] - 2022-11-29

### Added

- CoffeeScript support.

### Fixed

- Crashes when opening the window, and `StdEventTarget` in `lib/std.js`.

## [0.2.4p3] - 2022-08-29

### Fixed

- `HTTP.create()` was missing in `lib/chrome` and `lib/http` ([#58]).
- `AsyncFunction` in console mode ([#52]).

## [0.2.4p2] - 2022-05-31

### Added

- Debugging web proxy tools in `lib/http` (`.attachDebugger()`): Fiddler, mitmproxy, Burp Suite, and OWASP ZAP ([#43], [#51]).
- Punycode (IDNA) support.

## [0.2.4] - 2022-05-04

### Added

- mitmproxy support ([#43]) and capturing traffic with Fiddler ([#51]).
- An `AsyncFunction` counter ([#50]).

### Fixed

- "Stop running this script?" prompts ([#47]).

## [0.2.3] - 2022-04-20

### Added

- `.isFollowRedirect(boolean)` and `.setSaveTo(string)` in the HTTP/cURL mode of `lib/http`.
- `.fetchAll2()` in `lib/wmi` ([#30]).
- The `AsyncFunction` class in `lib/std`.
- `_echoCallback(f)` in the app, for logging to a file ([#33]).
- YAML support ([#35]).

### Changed

- The license changed from MS-PL to MS-RL.
- Window coordinate calculation in `lib/chrome`.

### Fixed

- `sleep()` in `lib/std` was not accurate ([#34]).
- A regular expression in `lib/shell`.

## [0.2.2] - 2022-01-19

### Added

- Integration with embedded Python in `lib/python3` ([#29]).

## [0.2.1] - 2021-09-21

### Added

- `bootstrap.bat`.
- User prompts (`alert`, `confirm`, `prompt`) in `lib/toolkit` ([#21]).
- C#/VB.NET native component support in `lib/toolkit` ([#18]).
- WebSocket support ([#17]).
- core-js ported to WSH ([#16]).
- GTK Glade GUI XML support ([#12]).
- Squel.js (WMI SQL on WSH) in `lib/system` ([#10]).

### Fixed

- Running when HTA is not associated ([#15]).
- A conflict between ES5-shim 4.5.15 and ES6-shim 0.35.x ([#14]).
- The `JSON` module in console mode ([#8]).
- Crashes of `Object.create(null)` in core-js on WSH (upstream fixes).

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
