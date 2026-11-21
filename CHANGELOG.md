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

- Editor: **Analyze Tactics (MITRE ATT&CK)** ([#574](https://github.com/gnh1201/welsonjs/issues/574)) in the context menu of a selection asks the local model FrogNano (`frognano_4b_2609`, `POST /llamacpp/chat`) which tactics of MITRE ATT&CK Enterprise 20.1 the code performs (including Stealth TA0005 and Defense Impairment TA0112). It inserts one or two tactics with links and a one-sentence reason, or "No clear MITRE ATT&CK tactic", as a comment.
  - **How:** the prompt lists the tactics with their official summaries ("The adversary is trying to ...") and asks for JSON. The launcher's prompt translation is turned off (`"translate": false`) so that code is not translated. Selections are cut at 6,000 characters. An answer takes 5 to 10 seconds on a CPU, longer while the model loads.
  - **Accuracy:** on 32 attack snippets (8 cut from the Gootloader 2019 sample, [gootloader-payload-2019](https://github.com/gnh1201/gootloader-payload-2019)) it named the right tactic first in 20 and among its answers in 28, and never answered "none"; it answered "none" for all of 14 ordinary snippets. It tends to add Initial Access to code that downloads or runs something; telling it not to made that more frequent. The result is labeled as candidates.
  - **Tried before:** embedding similarity with EmbeddingGemma 2 (`embeddinggemma_2`) against tactic descriptions or the official technique descriptions matched words rather than behavior: it reported the decoded Gootloader stage, which has meaningless names, as having no clear tactic. The typed-decision model `embeddinggemma_2_typed_decision` could not tell the tactics apart.
- API keys from Azure Key Vault (related to [#571](https://github.com/gnh1201/welsonjs/issues/571)), in two parts.
  - **Script side:** `data/apikey.json` accepts `keyvault:<vault>/<secret>` next to `file:` and plain values. `lib/credentials.js` reads such a value through the launcher when `CRED.get()` first asks for it, keeps it in memory, and on failure returns `null` with the reason (launcher not running, sign-in required, access denied, secret not found...).
  - **Several sources:** a provider may also be an array of these forms, tried in order, for example `["keyvault:my-vault/openai-api-key", "file:data/openai_apikey.txt"]`. Missing or empty files, empty values and Key Vault secrets that cannot be read are skipped. A value from the first source is kept in memory; after a fallback, the earlier sources are tried again on the next `CRED.get()`. Single values work as before.
  - **Launcher:** **Settings > Sign in to Azure...** signs in with the user's own work or school account, in the default browser (authorization code with PKCE, loopback redirect), through the multi-tenant Entra ID app **WelsonJS Launcher** (`AzureClientId`, `5bd2e397-314a-4fc9-88db-4e2b2452fb50`). The refresh token is kept with DPAPI in `%APPDATA%\WelsonJS\azure-signin.dat`. The tenant of a vault is found from its 401 challenge, and secret values stay in memory for 15 minutes.
  - **Local endpoint:** the launcher serves `GET /credentials/keyvault/<vault>/<secret>`. It refuses browser requests and callers without the per-start token of `%APPDATA%\WelsonJS\credentials.token`, which only the current user can read.
  - No NuGet packages were added. `docs/azure-key-vault.md` describes the setup and the security model.
- `native/chakracore`: ChakraCore built for WelsonJS like llama.cpp and CrispEmbed, from the head of `chakra-core/ChakraCore` (commit `97e68c9`, version `1.13.0.97e68c9.1` from its `ChakraCoreVersion.h`), for x64, x86 and arm64 with MSBuild. `patches/0001-windows-resources.patch` gives `ChakraCore.dll` the WelsonJS version, a description that says `built for WelsonJS` and the copyright of Microsoft and Catswords OSS. The builds are published to `packages/native/ChakraCore/<version>-<arch>/`, restored by CI into `artifacts\chakracore\<arch>` and signed; the launcher and welsonjs-hta still use the earlier prebuilt `ChakraCore.dll`.
- llama.cpp is built for x86 as well (`build-llamacpp.ps1 -Arch x86`): MSVC Win32 with one CPU backend (`ggml-cpu.dll`) on the AVX2 baseline without BMI2, because ggml's BMI2 code uses a 64-bit-only intrinsic and several of the x64 CPU variants enable BMI2. The executables are linked with `/LARGEADDRESSAWARE`, so a 32-bit process gets 4 GB of address space on 64-bit Windows; without it the model and the KV cache of the launcher's 8192-token context did not fit. It is published with the other prebuilt builds and restored by CI into `artifacts\llamacpp\x86` (see the entry on prebuilt builds below), its binaries are in the signing list, and `postInstall.ps1` already installs `llamacpp\x86` on 32-bit Windows. With an 8192-token context it gives the same greedy answers as the x64 build (Qwen3Guard 0.6B, Hy-MT2 1.8B, BitNet b1.58 2B I2_S).
- CrispEmbed is built for ARM64 and x86 as well as x64 (`build-crispembed.ps1 -Arch x64|x86|arm64`). arm64 is cross-compiled with clang (ggml does not support MSVC on ARM), with `native/crispembed/cmake/arm64-windows-llvm.cmake` (`-march=armv8.7-a`, as the WelsonJS llama.cpp ARM64 build; it also defines the Windows macros that CrispEmbed sets for MSVC only) and without OpenMP. x86 is MSVC Win32 without BMI2, whose ggml code uses a 64-bit-only intrinsic, and its executables are linked with `/LARGEADDRESSAWARE`. All three are published as prebuilt builds and restored by CI into `artifacts\crispembed\<arch>`, every binary is in the signing list, and `postInstall.ps1` installs the build for the machine; ARM64 Windows falls back to the x64 build under emulation only when there is no arm64 build. The x86 build gives the same decisions as x64 (8 of 8 labels, probabilities within 1e-6); the arm64 build was checked for its format and imports, not run.
- `WelsonJS.Cryptography` has the modes of operation besides ECB, for ARIA, SEED and HIGHT. Through `Mode`: CBC, CFB (`FeedbackSize` 8 to the block size), OFB and CTS (CBC with ciphertext stealing, CS3); the classes override `Mode` because the `SymmetricAlgorithm` of .NET Core and .NET 5+ refuses OFB and CTS. Through their own methods: `CreateCtrTransform()` (CTR), `CreateGcm()` and `CreateCcm()` (authenticated encryption with associated data, for the 128-bit ciphers ARIA and SEED) and `CreateCmac()` (a `HashAlgorithm`). The modes are one implementation over a new `IBlockCipher` interface (`BlockCipherTransform`, `BlockCipherGcm`, `BlockCipherCcm`, `BlockCipherCmac`), shared by the three ciphers through `BlockCipherAlgorithm`, and the transforms work with `CryptoStream`. `WelsonJS.Cryptography.Test` now checks the library and exits with 1 on a failure: the published vectors of the ciphers (RFC 5794, RFC 4269, the KISA HIGHT reference code) and of the modes (SEED-CBC from RFC 4196, ARIA-CTR and ARIA-GCM from RFC 8269, HIGHT-CBC and HIGHT-CTR from KISA), the generic modes run with .NET's AES against NIST SP 800-38A, RFC 3962, the GCM specification and RFC 4493, the output of .NET's `Aes`, `AesGcm` and `AesCcm` on random messages, and `CryptoStream` with every cipher, mode and padding (189 checks). `native/WelsonJS.Augmented/WelsonJS.Cryptography/README.md` describes the classes.
- CrispEmbed (https://github.com/CrispStrobe/CrispEmbed, MIT), a ggml engine for GGUF encoder models, for text embeddings and typed decisions on the CPU. CrispEmbed is built like llama.cpp: `native/crispembed/build-crispembed.ps1` builds 0.17.20.ad01b21.1 (`VERSION` 0.17.20 of the pinned commit) from a pinned commit (`ad01b21`, with the ggml commit it records; from a source archive in the WelsonJS blob storage when git fails) for Windows x86_64 with MSVC and a fixed AVX2 baseline, with the WelsonJS icon and version information in every executable and DLL (`native/crispembed/patches/0001-windows-resources.patch`), CI restores the published build into `artifacts\crispembed\<arch>`, and it is packaged and signed with the other binaries; the setup component `crispembed` installs it from the artifacts package into `%APPDATA%\welsonjs\crispembed\`. `crispembed\embeddinggemma_2_typed_decision` installs EmbeddingGemma 2 fine-tuned for typed decisions (Q8_0, 308 MB, Apache-2.0) into `crispembed_models\`, outside the folder the llama.cpp router reads models from; the download is pinned and listed in `data/DownloadHashes.txt`. The launcher runs one `crispembed-server` per model on demand (at most `CrispEmbedModelsMax`, 2 by default, stopping the least recently used idle one) and exposes `crispembed/status`, `models`, `start`, `stop`, `embed` and `decide`, and `POST /v1/decisions` and `POST /v1/embeddings` in the OpenAI-compatible API; only the embedding and decision routes are forwarded. A model is a decision model (`CrispEmbedDecisionModels`, answering decisions only) or an embedding model (answering embeddings only): the decision model's encoder is fine-tuned for its head, so EmbeddingGemma 2 embeddings come from the original `google/embeddinggemma-2`, which CrispEmbed 0.17.20.ad01b21.1 does not load and which runs in the WelsonJS llama.cpp build instead. `lib/crispembed.js` is the JavaScript client, and `docs/crispembed-launcher-api.md` describes the API.
- Text embeddings with the original EmbeddingGemma 2 (google/embeddinggemma-2, Apache-2.0) in the WelsonJS llama.cpp build: `native/llamacpp/patches/0004-embeddinggemma2.patch` backports upstream llama.cpp's `gemma-embedding2` architecture (#30054) to the fork, which predates it, and the setup component `llamacpp\embeddinggemma_2` installs the ggml-org Q8_0 GGUF (310 MB, pinned, listed in `data/DownloadHashes.txt`) into `llamacpp_embedding_models\`. The launcher runs each llama.cpp embedding model in a `llama-server --embedding` of its own (not in the router, whose model instances share its options), at most `LlamaCppEmbeddingModelsMax` at a time with a context of `LlamaCppEmbeddingContextSize`, and serves it at `POST /llamacpp/embed` and `POST /v1/embeddings`, applying `dimensions` (Matryoshka truncation) itself. `lib/llamacpp.js` has `embed()`.
- `GET /localservers/status` reports every local server the launcher runs: process state, port, last use, requests in progress, last exit code and the last lines of output.
- `is_test` in `data/test-misc.json` and `testloader.js`, which checks the global `is` (is.js 0.9.0.2) and the response time of `is.url()` and `is.email()` on hostile input: `cscript app.js testloader is_test data\test-misc.json`.

### Changed

- llama.cpp and CrispEmbed are no longer built in CI. Building both for x64, arm64 and x86 takes longer than the 60-minute AppVeyor job limit, and a job stopped by the limit saves no build cache, so the Release job failed on every run. They are now built on a developer PC and published with the new `native\publish-prebuilt.ps1` to the WelsonJS blob storage, under `packages/native/<component>/<version>-<arch>/` (for example `llamacpp/b9871.390c307.1-x64/`). The script checks the build against its `SHA256SUMS.txt` and `BUILDINFO.txt`, uploads each file as it is and as `<file>.gz` with AzCopy without overwriting, and records the file list and hashes in `native\<component>\prebuilt\<arch>.sha256`. The Debug and Release jobs restore the six builds with the new `native\restore-prebuilt.ps1`, which downloads the `.gz` files (about a quarter of the size) and decompresses them, falls back to the file itself when a `.gz` cannot be downloaded or used, and uses a file only if its SHA-256 matches that record, and package and sign them as before (Debug with the test-signing policy, Release with release-signing); each job caches the restored folders, keyed on `version.txt` and the recorded file lists, so the files are downloaded only when a record changes. The published builds were made with Visual Studio 2026 (MSVC 19.50; clang 23 for arm64), and restoring all six from the storage gives files identical to the builds.
- The launcher runs its local servers through one implementation (`LocalServerProcess`), shared by the llama.cpp router and the CrispEmbed servers: a launcher-allocated port, a start that waits until the server is ready and is retried once on a new port when the process exits while starting (the port may be taken between allocation and bind), the launcher's job object, and standard output and error read continuously, so a chatty server never blocks on a full pipe and a failed start reports its exit code and last lines instead of only "exited". Servers kept in a pool are only stopped to make room when no request is using them.
- The installer's JDK component is `jdk` now, not `microsoft_jdk`, and it unpacks an OpenJDK instead of installing one. `postInstall.ps1` downloads the vendor's `.zip` and extracts it under `%APPDATA%\welsonjs\jdk\`, keeping the archive's own top folder (`jdk-25.0.5.0.2+1`, or the Zulu package name), and then writes `JAVA_HOME` for the current user and adds that folder's `bin` to the user `PATH` without adding the same entry twice. Nothing goes into Program Files, no Programs and Features entry is created, and the JDK sits with the components the script downloads and goes away with that directory. Both values are user-scope even when the setup runs elevated, because the path belongs to one user's profile. The archive is unpacked into a temporary directory first and the previous JDK is replaced only once a `bin\java.exe` is found, so an archive that fails to unpack leaves the earlier installation untouched; the staging directory and the archive are removed afterwards, along with what earlier versions of the component left in `%APPDATA%\welsonjs\microsoft_jdk\` — a JDK unpacked there, or the installer file they only staged — and any `PATH` entry that pointed into it. The `.exe` and `.msi` branches are gone with their unattended switches, exit codes and restart handling, and with them the `.jar` file association the Microsoft package used to create. The component description in `setup.iss` says what it does.
- `data/DownloadUrls.psd1`: all three `jdk` links are the `.zip` package rather than a vendor installer: x64 and ARM64 at Microsoft Build of OpenJDK 25.0.5.0.2, the current Windows patch of JDK 25 LTS, and x86 at Azul Zulu 17 LTS 17.0.21.1 for Windows i686, because Microsoft publishes no 32-bit build of JDK 25 (JEP 503 removed the x86 port). Zulu is at the patch level the JDK 17 fixes in the November 2026 CPU land in. The `arm64` key had a URL missing and `x86` pointed at the AArch64 installer; both are corrected. The file carries URLs only: every hash, this component's included, is in `data/DownloadHashes.txt`.
- `data/DownloadUrls.psd1`: `curl` (x64, ARM64), `android_platform_tools` and `nuget` point at versioned files instead of the links that serve whatever is current: `curl.se/windows/dl-8.23.0_9/curl-8.23.0_9-win64-mingw.zip` and `...-win64a-mingw.zip` (curl 8.23.0, build 8.23.0_9 of curl-for-win), `dl.google.com/android/repository/platform-tools_r37.0.1-win.zip` (Android platform-tools r37.0.1, the release in the stable channel of Google's SDK repository; r37.0.3 is only in its preview channel) and `dist.nuget.org/win-x86-commandline/v8.0.0/nuget.exe` (NuGet 8.0.0). Each serves the bytes its previous link served when `data/DownloadHashes.txt` was computed on 2026-11-18, so the hash list keeps its 55 values, but a value now belongs to a file that stays what it is instead of to whatever the vendor publishes next. Five URLs still serve whatever is current: the four Hugging Face `resolve/main` GGUF models and LDPlayer's `LDPlayer9.exe?n=…`.
- numbers.js is now `app/assets/js/numbers-0.7.0.1.js`, a WelsonJS build of its own: numbers.js 0.7.0 with the WelsonJS fixes, listed in its file header. Besides the existing `matrix.determinant` change (numbers/numbers.js#161), `basic.powerMod` is exact for moduli up to 2^53 and for exponents of 2^31 and above (which also fixes `prime.millerRabin`), `basic.modInverse` returns the inverse in [0, m), `calculus.adaptiveSimpson` splits at the midpoint with a depth limit instead of computing one step or recursing without bound, `calculus.limit` approaches the point with shrinking steps, `calculus.MonteCarlo` samples with `random.sample` and passes every coordinate, `generate.collatz` returns its result, and `matrix.transpose` turns a one-dimensional array into a column vector. `restoreAppAssets.ps1` downloads it like the other vendor files, from `ajax/libs/numbers/0.7.0.1/numbers.js` in the WelsonJS blob storage, and `app.js` loads it as the global `numbers`; `numbers-0.7.0.wsh.js` is no longer used.
- The other vendor scripts that WelsonJS changed for WSH JScript follow the same scheme: `core-js-3.49.0.1.js`, `js-yaml-4.1.0.1.js`, `linq-4.0.2.1.js`, `respond-1.4.2.1.js`, `selectivizr-1.0.2.1.js` and `squel-basic-5.13.0.1.js` replace the `.wsh.js` files of the same versions (`squel-basic-5.13.0.1.js` replaces `squel-basic-5.13.0-afa1cb5.wsh.js`; the source is still Squel.js at commit `afa1cb5`). The code is unchanged; each file now starts with a comment that names the upstream version and lists the changes, and they are kept in the WelsonJS blob storage under `ajax/libs/<library>/<version>.1/` instead of as repository snapshots. `docs/modified-vendor-scripts.md` lists, for all seven files, the upstream file they come from (with its digest) and every change, found by comparing the two.
- `app/assets/js/json2.js` is json2.js 2023-05-10 (from `ajax/libs/json2/20230510/` in the WelsonJS blob storage) instead of 2017-06-12; the two differ in the date of the header only.
- The prompts that ask a language model for code (the system message of `lib/language-inference-engine.js`, the editor's Write Code action, the `evaluate_js` description in `mcploader.js`, and `SKILL.md`) list the numbers.js functions available through the global `numbers`, with the precision limits of JavaScript numbers.

### Fixed

- HIGHT in `WelsonJS.Cryptography` is HIGHT now. The key schedule took the whitening keys from key bytes 8 to 15 and swapped the two halves of the key in the subkeys, and one entry of the F1 table was wrong (F1(0x4E) was 0x5D instead of 0x05), so the output matched no published vector; it follows the KISA reference code and passes its 75 ECB, CBC and CTR vectors. Data encrypted with the earlier `HightAlgorithm` or `HightEcbTransform` does not decrypt with this one. Also in `WelsonJS.Cryptography`: `GenerateKey()` and `GenerateIV()` changed nothing (they filled a copy of the key and IV); ARIA returned the decrypted data with its padding when the last byte was not a valid padding length, instead of refusing it; and the ECB transforms decrypted every block in `TransformBlock`, so the `TransformFinalBlock` that `CryptoStream` calls with no bytes left read past an empty buffer instead of removing the padding.
- A custom installation with no component ticked no longer installs every component. `WizardSelectedComponents` returns an empty string for that selection, which `postInstall.ps1` read as "no filter" and therefore as all components; `setup.iss` now passes `none` and the script reads it as an empty selection. A run started by hand without `-Components` still means every component.
- When the processor query fails, `Get-NativeArchitecture` reads the architecture from the process environment instead of answering x64 for every 64-bit system, so an ARM64 machine no longer downloads the x64 build of a component.
- The `curl` component is unpacked again. `postInstall.ps1` saved its archive as `curl.zip`, while the extraction phase took the name from the URL (`curl-latest.cgi`), found nothing under that name and skipped the component, so since 2026-10-05 (`73d1de98`) the archive was downloaded, verified and never installed. Both phases now name the file through one new `Get-DownloadFileName`, which reads it from the URL, so an archive is unpacked from the name it was saved under; `curl` lands in `%APPDATA%\welsonjs\curl\` with `bin\curl.exe` in it, as it did before that refactor.
- A component whose download fails, or whose file is not listed in `data/DownloadHashes.txt`, is skipped and the other components are still installed; such a failure used to end the whole download phase with `exit 1`. A curl download that delivers nothing for 5 minutes is abandoned. The hash list is kept as a case-insensitive set when it is passed between functions, instead of being unrolled into an array (where one listed hash made the check a substring test).
- When the OpenJDK build that is already unpacked is unpacked again, the old copy is renamed aside and put back if the new one cannot be moved in, instead of being deleted first, and `PATH` entries of other JDKs under `jdk\` are removed so `java` and `JAVA_HOME` name the same JDK.
- numbers.js: `basic.powerMod` is exact for moduli up to 2^53 (it lost precision above about 2^26.5, and exponents of 2^31 and above went wrong), which also fixes `prime.millerRabin`; `basic.modInverse` no longer returns a negative value; `calculus.adaptiveSimpson` splits each interval at its midpoint (it computed one Simpson step or recursed without bound); `calculus.limit` approaches the point instead of evaluating f once; `calculus.MonteCarlo` no longer calls a missing function; `generate.collatz` returns its array; `matrix.transpose` accepts a one-dimensional array.

### Security

- Every download `postInstall.ps1` makes is checked against `data/DownloadHashes.txt`, a new file that holds nothing but SHA-256 values, one per line. `Download-File` hashes the file with `Get-FileHash` (or the .NET class where that cmdlet does not exist) and, when the value is not in the list, deletes the file, logs the hash it did have and raises an error, so a file that is not listed is never run or unpacked. The list holds the hashes of all 55 downloads in `data/DownloadUrls.psd1` — every component and every architecture, computed from the bytes each URL served on 2026-11-18 — so the check covers the whole table rather than the JDK alone. It is read once per run into a case-insensitive set, and `data/DownloadUrls.psd1` carries URLs only. The JDK archive is hashed again immediately before it is unpacked, because the file sits under the user's `%TEMP%` while the script may be elevated. A run without the hash file says so and downloads unverified.
- Removed the assembly binding redirects for Google.Protobuf and the five gRPC assemblies from `WelsonJS.Service/app.config`. Their packages left the build in 0.2.7.62 with the heartbeat gRPC client, so only the redirects remained (related to [#434]).
- is.js 0.9.0, which has no patched release for CVE-2020-26302 (ReDoS in `is.url()`), is replaced by is.js 0.9.0.2 (`app/assets/js/is-0.9.0.2.js`, from `ajax/libs/is_js/0.9.0.2/` in the WelsonJS blob storage): the `is.js` of the release with the host name pattern of `is.url()` rewritten so that it cannot backtrack exponentially, the domain pattern of `is.email()`, which had the same flaw, rewritten the same way, and the `is.ipv6()` pattern written as the list of the address forms, because JScript evaluated its lookaheads and backreferences differently and rejected valid addresses such as `2001:db8::1`. All three accept and reject exactly the values they did before (compared in V8 on about 17 million strings), and `app.js` publishes the library as the global `is` as it did before is.js was removed; the module that replaced it for a while, `lib/is.js`, is gone. `docs/third-party-dependencies.md` describes the patterns.
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
