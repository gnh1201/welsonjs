# Third-party dependencies flagged by the OSS assessment

The Open Source Software Assessment of 2026-08-05 (OPENUP) reported three components with known vulnerabilities in the snapshot of version 0.2.7.50, filed as issue #434. This page records what each component was, whether WelsonJS still contains it, and what was done about it. It is written so that a follow-up assessment can be read against it.

## Google.Protobuf 3.28.2 — CVE-2026-0994

**Current state: not in the build, not shipped.**

The package came in through the gRPC heartbeat client of `WelsonJS.Service` (`Grpc.Core` 2.46.6, `Grpc.Tools` 2.67.0, `heartbeat.proto`). That client, together with the ClamAV and Elasticsearch code, was removed in 0.2.7.62 (commit `38dc60fe`, "Replace log4net with Trace-based logging"). `packages.config` and `WelsonJS.Service.csproj` no longer reference Google.Protobuf or any Grpc package, no C# source uses them, and no build step compiles `app/assets/protos/heartbeat.proto`.

What remained was build metadata: assembly binding redirects for `Google.Protobuf` and the five `Grpc.*` assemblies in `WelsonJS.Service/app.config`, and the `Grpc.Tools` props import at the top of `WelsonJS.Service.csproj`. Both are removed in this change.

CVE-2026-0994 is a recursion depth bypass in `google.protobuf.json_format.ParseDict()` of the **Python** implementation. The GitHub advisory database and NVD list it for the Python package only; no NuGet publication of Google.Protobuf is listed as affected. Even so, nothing of the package remains, so the finding is closed by removal rather than by an upgrade.

## is.js 0.9.0 — CVE-2020-26302

**Current state: kept as is.js 0.9.0.2, a WelsonJS build of 0.9.0 with the vulnerable patterns rewritten.**

`app.js` publishes the library as the global `is`, for applications built on the framework. CVE-2020-26302 is a ReDoS in the regular expression of `is.url()`, and is.js has no patched release; its last version is 0.9.0. The dependency could not be upgraded, so WelsonJS ships `app/assets/js/is-0.9.0.2.js`: the `is.js` of the 0.9.0 release with three regular expressions rewritten (an earlier attempt replaced the library with a module of the repository, `lib/is.js`; it was withdrawn in favour of this build, which keeps the library's behaviour exactly).

- `is.url()`: the host name labels were `(?:[a-z\u00a1-\uffff0-9]-*)*[a-z\u00a1-\uffff0-9]+`, a quantified group inside a quantified group, which backtracks exponentially on a value such as `http://` followed by `aa.` repeated (30 repeats took about 30 s in V8). They are now `[a-z\u00a1-\uffff0-9]+(?:-+[a-z\u00a1-\uffff0-9]+)*`, which describes the same labels (they start and end with a letter or digit and may contain hyphens) with only one way to match each.
- `is.email()`: the domain part had the same flaw (its labels may contain dots, and the labels are repeated with dots between them), and `a@` followed by `a.` repeated took about 30 s at 30 repeats. It is now written as one pass that cannot match a character in two ways, for the same set of domains: a domain starts with a letter or digit, ends with a letter, and contains a dot between a letter or digit and a letter.
- `is.ipv6()` (0.9.0.2): the 0.9.0 pattern relied on lookaheads, backreferences to groups that may not have matched and an empty alternative inside a repeated group, which the JScript engine evaluates differently from V8, so it rejected valid addresses such as `2001:db8::1`. It is now the list of the address forms (eight groups; `::` with up to seven groups before and after it; six groups, or `::` with up to five, followed by an IPv4 address), with none of those constructs. It accepts and rejects the same values as the 0.9.0 pattern does in V8.
- `is.VERSION` is `0.9.0.2`; the 0.9.0 release still said `0.8.0`. (0.9.0.1 had the first two changes only.)

Nothing else is changed, so every other check and the browser and device detection group behave as in 0.9.0.

Verification: the rewritten patterns were compared with the 0.9.0 ones in V8 on 600,000 random strings and on every string of up to 7 to 10 characters over the characters that matter (about 5.5 million values, valid ones included); they gave the same answer every time. On hostile values of 50,000 repeats they answer in under 10 ms. The `is.ipv6()` pattern was compared the same way on about 11 million values (4 million random, the rest every short string over the characters that matter), with no difference, and under `cscript` it gives the V8 answer of the 0.9.0 pattern for 33 addresses, valid and invalid. Under `cscript`, `testloader.js is_test` with `data/test-misc.json` checks the global `is` and the response time on hostile values of 4 to 6 KB (7 ms and 1 ms).

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
- No build input or source file references `Google.Protobuf`, `Grpc.Core` or `Grpc.Net.Client`; `app/assets/protos/heartbeat.proto` is no longer part of any build.
- `app.js` and `testloader.js` parse, and `data/test-misc.json` parses as JSON with its `is_test` entry.
- `is_test` passes under `cscript` against is.js 0.9.0.2.
- `WelsonJS.Service/app.config` is well-formed XML, and the binding redirects that stay are unchanged.
