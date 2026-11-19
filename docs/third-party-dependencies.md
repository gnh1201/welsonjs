# Third-party dependencies flagged by the OSS assessment

The Open Source Software Assessment of 2026-08-05 (OPENUP) reported three components with known vulnerabilities in the snapshot of version 0.2.7.50, filed as issue #434. This page records what each component was, whether WelsonJS still contains it, and what was done about it. It is written so that a follow-up assessment can be read against it.

## Google.Protobuf 3.28.2 — CVE-2026-0994

**Current state: not in the build, not shipped.**

The package came in through the gRPC heartbeat client of `WelsonJS.Service` (`Grpc.Core` 2.46.6, `Grpc.Tools` 2.67.0, `heartbeat.proto`). That client, together with the ClamAV and Elasticsearch code, was removed in 0.2.7.62 (commit `38dc60fe`, "Replace log4net with Trace-based logging"). `packages.config` and `WelsonJS.Service.csproj` no longer reference Google.Protobuf or any Grpc package, no C# source uses them, and no build step compiles `app/assets/protos/heartbeat.proto`.

What remained was build metadata: assembly binding redirects for `Google.Protobuf` and the five `Grpc.*` assemblies in `WelsonJS.Service/app.config`, and the `Grpc.Tools` props import at the top of `WelsonJS.Service.csproj`. Both are removed in this change.

CVE-2026-0994 is a recursion depth bypass in `google.protobuf.json_format.ParseDict()` of the **Python** implementation. The GitHub advisory database and NVD list it for the Python package only; no NuGet publication of Google.Protobuf is listed as affected. Even so, nothing of the package remains, so the finding is closed by removal rather than by an upgrade.

## is.js 0.9.0 — CVE-2020-26302

**Current state: removed, and replaced by `lib/is.js`.**

`app.js` loaded the library and published it as the global `is`, but no script in the repository, the examples or the application UI called it — `is-0.9.0.min.js` was only a convenience object for applications built on the framework. CVE-2020-26302 is a ReDoS in the regular expression that validates URLs (`is.url()`), and is.js has no patched release; its last version is 0.9.0. The dependency could therefore not be upgraded, and it was removed.

The require line that loaded `app/assets/js/is-0.9.0.min.js`, the two integrity sidecars (`is-0.9.0.js.integrity`, `is-0.9.0.min.js.integrity`) and the two entries in `app/assets/integrity.json` are gone, so the library is no longer downloaded or loaded. An application that used `is` would have broken on the missing global, so the name is kept and now comes from `lib/is.js`, which `app.js` assigns to it:

- `lib/is.js` is a module of this repository: it loads no third-party library and the checks are implemented in it. It keeps the method names and the meaning of the calls of is.js 0.9.0: the type, presence, arithmetic, format, string, date, object and array checks, and the `not`, `all` and `any` interfaces.
- The browser and device detection group of is.js 0.9.0 (`is.android`, `is.chrome`, `is.edge`, `is.ie`, `is.iphone`, `is.ipad`, `is.mobile`, `is.tablet`, `is.touchDevice` and the other members of that group) is not included. It reads `navigator.userAgent`, which means nothing in Windows Script Host; `is.windows()`, `is.linux()`, `is.mac()`, `is.online()` and `is.offline()` answer for the host instead, and an embedded browser is checked with `lib/browser.js`. A call to a method that is not provided now raises a `TypeError`, as a call to a method name that never existed does.
- `is.url()` and `is.email()` do not use a regular expression. They are parsed once, from left to right, over a value of at most 2048 and 254 characters, so their cost is proportional to the length of the input and a hostile value cannot make them backtrack. The empty-label and repeated-dot shapes that made is.js 0.9.0 backtrack are answered in under 0.1 ms, against about 200 ms for a 67-byte value in the reference library, which did not finish an 82-byte value within 5 s.
- The remaining format checks (`is.creditCard`, `is.usZipCode`, `is.timeString`, `is.dateString`, `is.nanpPhone`, `is.socialSecurityNumber` and the rest) use anchored patterns without nested unbounded quantifiers, and every value passed to one is limited to 8192 characters. `is.ipv4()`, `is.ipv6()` and `is.numeric()` are parsed by hand as well.
- The differences from is.js 0.9.0 are listed at the top of `lib/is.js`: a URL host may be a loopback, private or link-local IPv4 address, a bracketed IPv6 literal, or a Unicode name; a port may be one digit and must be 1 to 65535; an email address is ASCII with one `@` and a top level domain of two characters or more; `is.finite()` does not coerce its argument; a check that receives a value of the wrong type returns false instead of throwing; `is.offline()` works, where is.js 0.9.0 threw because it was built from `is.online` before that method existed; and `is.numeric()`, `is.fn()` and `is.ssn()` were added.

Verification of the module: it was loaded the way the module loader loads a file (its body inside `new Function` with a strict mode directive), and every method it shares with is.js 0.9.0 was called with 129 values of every type — 9,600 calls over 73 methods, their `not`, `all` and `any` interfaces, and the methods that take more than one argument. The only differences were the ones listed above. It was also loaded in an engine with the ES5 methods that JScript lacks removed (`Array.isArray`, `Object.getOwnPropertyNames`, `Object.keys`, `Array.prototype.forEach`, `map`, `filter`, `indexOf`, `String.prototype.trim`), and the checks that depend on them still answered. A run under `cscript` was not performed here: the environment that prepared this branch has no Windows host. `testloader.js is_test` with `data/test-misc.json` runs the same checks and the timing check on Windows.

## jQuery 1.11.3 — CVE-2020-11023, CVE-2020-11022, CVE-2015-9251, CVE-2019-11358

**Current state: kept as the IE 6–8 fallback only, with the exposure documented below.**

`lib/browser.js` chooses the library at start: jQuery 1.11.3 when `Browser.getIEVersion()` reports less than 9, jQuery 3.5.1 otherwise. `getIEVersion()` reads the document mode with conditional comments, so the fallback is used by IE 6–8 as well as by a later IE that renders a page in IE 7 or IE 8 compatibility view. Only that branch is affected. All four advisories are fixed in jQuery 3.5.0, so the other branch is not affected by them.

There is no patched release that still runs on IE 6–8. jQuery 3.x requires IE 9 or later, and the 1.x line ended at 1.12.4, which is affected by the same four advisories; the only patched 1.x builds are unofficial backports (for example `jquery-lts` `1.12.5-sec`), which WelsonJS does not want to distribute in place of the upstream file. The fallback therefore stays at 1.11.3 while the framework keeps Windows XP SP3 in its supported platforms, because MSHTML there is IE 8 or earlier. The integrity digest of each file matches the official release from `code.jquery.com` (`jquery-3.5.1.min.js` `sha384-ZvpUoO/+PpLXR1lu4jmpXWu80pZlYUAfxl5NsBMWOEPSjUn/6Z/hRTt8+pR6L4N2`, `jquery-1.11.3.min.js` `sha384-+54fLHoW8AHu3nHtUxs9fW2XKOZ2ZwKHB5olRtKSDTKJIb1Na1EceFZMS8E72mzW`), so the fallback is the unmodified upstream file and not a local variant.

Exposure in the framework as it stands:

- CVE-2015-9251 needs a cross-domain Ajax request without `dataType`. `lib/http.js` `jqAjax()` calls `jQuery.ajax()` without `dataType`, so an application that makes cross-domain requests through it under IE 6–8 inherits the issue. Applications should set an explicit `dataType` on such requests.
- CVE-2019-11358 needs `jQuery.extend(true, {}, source)` with an attacker-controlled source object. The framework does not deep-extend with remote data.
- CVE-2020-11022 and CVE-2020-11023 need HTML from an untrusted source passed to a manipulation method. The bundled UI passes golden-layout tab titles into `.html()` (`app/index.js`), so text that reaches a tab title must come from the application itself, not from a remote document.

An application that does not need IE 6–8 can ignore the fallback: on IE 9 or later the framework loads jQuery 3.5.1 and never touches 1.11.3. When Windows XP SP3 leaves the supported platforms (`README.md`), the fallback, the shims that are loaded with it (`html5shiv`, `respond`, `selectivizr`, `excanvas`, `jquery.html5-placeholder-shim`, and the `ie8` plugin for IE 8) and the `jquery-1.11.3.min.js` asset can be deleted together, which also removes the last advisory from the next assessment.

## Verification after these changes

- `app/assets/integrity.json` parses, and every sidecar file under `app/assets/js` still has an entry whose digest is listed in it (the reverse is checked by `restoreAppAssets.ps1`, which fails on a digest that has no URL).
- No build input or source file references `is-0.9.0`, `Google.Protobuf`, `Grpc.Core` or `Grpc.Net.Client`; `app/assets/protos/heartbeat.proto` is no longer part of any build.
- `app.js`, `lib/is.js` and `testloader.js` parse, and `data/test-misc.json` parses as JSON with its new `is_test` entry.
- `lib/is.js` passes the behaviour and response time checks described above. The `is_test` entry was not run under `cscript` in this environment (no Windows host).
- `WelsonJS.Service/app.config` is well-formed XML, and the binding redirects that stay are unchanged.
