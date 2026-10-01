# WelsonJS Agent Architecture Guide

This guide describes the architecture and contributor conventions visible in this repository. Treat the source code and project files as authoritative when behavior changes.

## 1. Runtime overview

WelsonJS is a Windows application framework built around the Windows Script Host (WSH) JScript engine, with an application loader in `app.js`, startup scripts at the repository root, JavaScript modules in `lib/`, and optional .NET components in `native/WelsonJS.Augmented/`.

The runtime is not a general-purpose sandbox. Scripts can reach Windows facilities through COM, JavaScript modules, and installed managed components. Document required privileges and side effects for any such functionality.

### JavaScript runtime and modules

- `app.js` initializes the runtime, exposes the module loader, and dispatches application commands. `require()` resolves project modules, most commonly from `lib/`.
- Root scripts such as `bootstrap.js`, `testloader.js`, `webloader.js`, and `uriloader.js` provide startup and task-specific entry points.
- `lib/` contains the following feature groups. The names below are the current checked-in module names; consult each module for its exact API and runtime requirements.

  | Area | Modules and capabilities |
  | --- | --- |
  | Runtime and common utilities | `std.js` (sleep, repeat/rotation helpers, events, dialogs, resource helpers), `toolkit.js` (shared helper utilities), `config.js` (XML-backed configuration lookup), `jsunit.js` (assertions and test suite support), `fakeworker.js` (worker-style scheduling), `fsm.js` (finite-state-machine primitives), `rand.js` (random values, UUID, shuffle/sample helpers), `extramath.js` (similarity, vector and layout helpers), `strings.js` (string helpers), `base64.js`, `punycode.js`, `lz77.js`, `xml.js`, `uri.js`, `filetypes.js`. |
  | Files, data, and persistence | `file.js` (file/folder IO, environment loading, path helpers), `archive.js` (archive operations through configured external tools), `db.js` (ADO database access and BLOB helpers), `credentials.js` (credential values loaded from file or memory), `cookie.js` (cookie helpers), `hosts.js` (hosts file access), `har.js` (HTTP Archive handling), `totp.js` (time-based one-time password helper). |
  | Windows host integration | `system.js` (environment, OS/process/network/system information), `shell.js` (Windows Script Host shell and process operations), `registry.js`, `wmi.js`, `powershell.js`, `virtualinput.js` (virtual keyboard/mouse and dialogs), `security.js` (selected Windows security-setting and antivirus-product operations), `task-scheduler.js`, `task.js` (task/process utilities), `winservice.js`, `winlibs.js` (Windows library helpers), `pipe-ipc.js` and `namedsharedmemory.js` (IPC), `msmq.js` (Microsoft Message Queuing), `wintap.js` (WinTAP integration). |
  | Network and service protocols | `http.js` (HTTP client utilities), `httpserver.js`, `websocket.js`, `jsonrpc2.js`, `stdio-server.js`, `router.js`, `sendmail.js`, `ip-reputation.js`, `serp.js`, `catproxy.js` (CatProxy client), `shadowsocks.js`, `tun2socks.js`, `cloudflare.js`, `nmap.js` (Nmap interface). |
  | Browsers, UI, and desktop applications | `browser.js` (embedded browser/DOM helpers), `chrome.js` (Chrome DevTools Protocol and browser automation), `gtk.js` (GTKServer UI), `msoffice.js` (Excel, PowerPoint, Word, Outlook automation), `kakaotalk.js`, `adb.js` (Android Debug Bridge and emulator), `ldplayer.js`, `noxplayer.js`, `ovftool.js` (VMware OVF Tool), `autohotkey.js`, `autoit.js`, `sandboxie.js`. |
  | External runtimes and AI/data services | `python3.js`, `vbscript.js`, `wamr.js` (WebAssembly Micro Runtime), `aviation.js` (aviation data API), `coupang.js` (Coupang search API), `chatgpt.js`, `anthropic.js`, `grok.js`, `groq.js`, and `language-inference-engine.js` (LLM provider integrations). |
  | Source examples | `fortune.coffee` and `fortune.ls` are CoffeeScript and LiveScript examples rather than JavaScript modules. |

  These libraries are adapters and utilities, not a uniform native-agent layer: some use COM/WSH, some invoke external executables, and some call remote services. Many need local software, credentials, network access, or elevated Windows privileges. In particular, treat `security.js`, `shell.js`, `powershell.js`, `registry.js`, `virtualinput.js`, and service/process modules as host-changing APIs; inspect implementation and authorization requirements before use. Never log credentials or sensitive payloads.
- The engine is legacy JScript. Keep syntax and host API usage compatible with the supported WSH environment. Do not describe ES6+ syntax as generally available just because selected `core-js` shims are present.
- COM and Windows APIs are host capabilities. Managed modules are optional dependencies and are loaded or used where the application and environment provide them.

## 2. Managed projects

The solution `native/WelsonJS.Augmented/WelsonJS.Augmented.sln` contains these projects:

| Project | Role |
| --- | --- |
| `Catswords.Phantomizer` | Assembly loading support. Consult its implementation and README for supported sources, caching, and verification behavior. |
| `WelsonJS.ManagedObject` | Shared managed types and Windows/COM integration helpers used by other projects. |
| `WelsonJS.Launcher` | Managed application launcher and related startup/UI components. |
| `WelsonJS.Service` | Windows service host and lifecycle support. |
| `WelsonJS.Esent` | ESENT database integration. |
| `WelsonJS.Cryptography` | Managed cryptographic algorithms, including implementations in this project such as ARIA and HIGHT. |
| `WelsonJS.Cryptography.Test` | Standalone cryptography validation project. |

Do not assume every capability is implemented by a managed project: many APIs live in `lib/` and interact directly with WSH, COM, external programs, or optional components. Preserve graceful handling of unavailable optional dependencies where the surrounding API already provides it.

## 3. Contributor principles

- Keep changes scoped to the responsible module or project and use explicit interfaces between runtime and managed code.
- Preserve WSH/JScript compatibility in root scripts and `lib/` unless a file explicitly targets another engine.
- Validate inputs at JavaScript-to-managed or JavaScript-to-COM boundaries, and report errors with enough context to diagnose the failing operation.
- Make OS, privilege, installed-software, network, and user-interaction requirements clear. Avoid claiming a security boundary or fallback unless the implementation provides one.
- Follow the existing project conventions and document externally visible changes near the relevant API or profile.

## 4. Test profiles and runner

Test profiles are JSON files under `data/`, including:

- `data/test-oss-korea-2023.json` — broad legacy profile.
- `data/test-misc.json` — miscellaneous runtime and integration examples.
- `data/test-msoffice.json` — Microsoft Office automation examples.

Profiles use schema version `0.2` and describe tests using metadata and a `tests` array. A profile entry documents a test; it does not by itself implement or execute the test.

`testloader.js` contains a `test_implements` map keyed by test ID. It expects a single test ID and a profile path, looks up that ID in both places, and calls the corresponding implementation. For example:

```text
cscript.exe app.js testloader es5_polyfills data\test-oss-korea-2023.json
```

The runner logs the result and waits before closing. It is an interactive/manual harness, not a profile-wide automated pass/fail runner. Test implementations can have side effects or require Windows features, installed applications, network access, or user interaction; inspect the selected implementation before running it. Keep IDs consistent between the profile and `test_implements`, and describe requirements and effects in the profile entry.

The cryptography validation project is separate from the JavaScript profile runner. Use the relevant project documentation and build configuration for that suite.

## 5. Repository map

| Path | Contents |
| --- | --- |
| `app.js` | WSH runtime, module loading, and command dispatch. |
| `lib/` | JavaScript libraries and integrations. |
| `data/` | Configuration, data files, and JSON test profiles. |
| `native/WelsonJS.Augmented/` | WelsonJS managed solution and projects. |
| `native/ManagedEsent/` | Vendored ManagedEsent source, samples, and tests. |
| `examples/` | Application and integration examples. |

When updating this guide, verify project names, entry-point behavior, and profile paths against the checked-in repository rather than carrying forward outdated architecture descriptions.
