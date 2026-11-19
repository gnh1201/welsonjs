# Modified vendor scripts

Most files under `app/assets/js` are third-party scripts exactly as their authors published them. Eight are WelsonJS builds of a published version, changed so that they run in the Windows Script Host JScript engine (ES3) or work with local files, or to fix bugs. They are named after the version they come from with `.1` appended (`numbers-0.7.0.1.js` comes from numbers.js 0.7.0), each starts with a comment that names the version and lists the changes, and they are kept in the WelsonJS blob storage under `ajax/libs/<library>/<version>.1/` like a CDN release. `restoreAppAssets.ps1` downloads them as it downloads every other vendor file, from the URL that `app/assets/integrity.json` lists for the digest in the file's `.integrity` sidecar.

Up to 0.2.7.62, the same files carried `.wsh` in their names (`core-js-3.49.0.wsh.js`) and were stored as snapshots of the repository. The `.1` files are those files with the header comment added; the code is unchanged.

| File | Comes from | Loaded by |
| --- | --- | --- |
| `core-js-3.49.0.1.js` | core-js 3.49.0, `index.js` of the npm package `core-js-bundle` | `app.js` |
| `is-0.9.0.1.js` | is.js 0.9.0, `is.js` of the release (npm package `is_js`, tag `v0.9.0`) | `app.js` (`is`) |
| `js-yaml-4.1.0.1.js` | js-yaml 4.1.0, `dist/js-yaml.js` of the npm package | `app.js` (`yaml`) |
| `linq-4.0.2.1.js` | linq.js 4.0.2, `linq.js` of the npm package `linq` | `app.js` (`Enumerable`) |
| `numbers-0.7.0.1.js` | numbers.js 0.7.0, the browserified `src/numbers.js` | `app.js` (`numbers`) |
| `respond-1.4.2.1.js` | Respond.js 1.4.2, `dest/respond.src.js` at the tag `1.4.2` | `lib/browser.js` (IE 8 and earlier) |
| `selectivizr-1.0.2.1.js` | selectivizr 1.0.2 (`v1.0.2b`), `selectivizr.js` at the tag `1.0.2` | `lib/browser.js` (IE 8 and earlier) |
| `squel-basic-5.13.0.1.js` | Squel.js 5.13.0 as of commit `afa1cb5` ("fix build", 2020-07-28, after the 5.13.0 release), `dist/squel-basic.js` | `app.js` (`squel`) |

## Upstream files

The changes below were found by comparing each file with these upstream files, ignoring line endings and the UTF-8 byte order mark:

| Upstream file | Bytes | SHA-384 |
| --- | --- | --- |
| `https://registry.npmjs.org/core-js-bundle/-/core-js-bundle-3.49.0.tgz` → `package/index.js` | 887,730 | `sha384-ja+wc46KctKrEra0BQe9bGANaK9o5D9mxX+jqWi2r06sX0iJkHqrpZ06zkIOzvLo` |
| `https://registry.npmjs.org/is_js/-/is_js-0.9.0.tgz` → `package/is.js` (the same file as `is.js` at the tag `v0.9.0`) | 32,813 | `sha384-r1rqAfJdNwo/sq+UOvPjgD9EOEuy1IpGN/LEUr4cF6gJeB6S/zqXyYXt1UF0CzKl` |
| `https://registry.npmjs.org/js-yaml/-/js-yaml-4.1.0.tgz` → `package/dist/js-yaml.js` | 114,359 | `sha384-wnpIlY73zQKRQF/TqAFQ+A41ZkUYqXitl4hV5aLCxwVJAL+gC8wxg3p6+hRkYxkq` |
| `https://registry.npmjs.org/linq/-/linq-4.0.2.tgz` → `package/linq.js` | 99,889 | `sha384-V7R4ik0ja96Aele/LkyQ6/3NgUgp6+iLDBlDljdWyY+mxw1WPghhppszor+x63iL` |
| `https://catswords.blob.core.windows.net/welsonjs/ajax/libs/numbers/0.7.0/numbers.js` | 78,930 | `sha384-SkZBybQ2Co+dcQQotKqcRyETNrPQiyioytIgfvmTRCay2prTjmbpTzVtTIsYY1R3` |
| `https://raw.githubusercontent.com/scottjehl/Respond/1.4.2/dest/respond.src.js` | 8,536 | `sha384-HwX0n9BiVutdIP0m3jKYarATBrBQMARe4JqN2bNN+eqbEirLWcklhaZgkovSBfc+` |
| `https://raw.githubusercontent.com/keithclark/selectivizr/1.0.2/selectivizr.js` | 19,041 | `sha384-KkFKLb40ypWxr/mNh/wx1XJCBdkGxksV9eVUtjiCCBZH4uGEBfPUftKF04ZornYz` |
| `https://raw.githubusercontent.com/hiddentao/squel/afa1cb5/dist/squel-basic.js` | 96,201 | `sha384-TGO48mVugymTS1uW/t9JIZym//JA2wjNJh/YzL/dFKYSiNjmdeYyZgnPccam1RmH` |

The digests are of the files as downloaded. Squel.js at `afa1cb5` is not the `dist/squel-basic.js` of the npm release 5.13.0, which differs from line 2979 on. The file is named after the version (`5.13.0.1`) and the commit is recorded here and in its header; up to 0.2.7.62 it was `squel-basic-5.13.0-afa1cb5.wsh.js`.

## Changes

### core-js 3.49.0.1

`NullProtoObjectViaActiveX` builds an object without a prototype from the `Object` constructor of an `htmlfile` ActiveX document. Windows Script Host has no such document, so the call is in a `try` block, and on failure the constructor comes from another script engine instance:

1. `MSScriptControl.ScriptControl`, the 32-bit Script Control that Microsoft ships;
2. `ScriptControl`, a 64-bit Script Control that third parties provide;
3. a function that returns `{}`, when neither exists, so that the deletion of the `enumBugKeys` that follows does not fail.

Nothing else is changed.

### is.js 0.9.0.1

- `is.url()`: the host name labels are matched with a pattern that has one way to match each label, instead of a quantified group inside a quantified group that backtracked exponentially (CVE-2020-26302). See `docs/third-party-dependencies.md`.
- `is.email()`: the domain part is rewritten the same way; the 0.9.0 pattern backtracked exponentially on `a@` followed by `a.` repeated.
- `is.VERSION` is `0.9.0.1` (the release said `0.8.0`).

Both patterns accept and reject exactly the values the 0.9.0 patterns did; they were compared on about 6 million strings. WelsonJS used to load the release's `is.min.js`, the minified form of the same source; 0.9.0.1 is the readable `is.js`. The `is.js` on the default branch of the repository is newer than the release (2020, with `is.thenable` and `is.operaMini`) and is not used.

### js-yaml 4.1.0.1

- `null` and `default` are quoted where they are property names (`'null': _null` in the type list, `exports['default'] = jsYaml`). JScript does not accept reserved words as unquoted property names.
- The error snippet marks a tab with `->` instead of `→`, so the text stays ASCII.
- The blank first line is dropped, and the file has no newline at the end.

### linq.js 4.0.2.1

- `let` and `const` are `var` (ES3 has no block-scoped declarations). In every place the variable is used only inside the block that declared it, so the meaning is unchanged.
- `export default Enumerable;` is `exports._default = Enumerable;`, so the file is a module of `require()`; `app.js` takes `Enumerable` from `._default`.

### numbers.js 0.7.0.1

Bug fixes, listed in the file header:

- `matrix.determinant`: cofactor expansion along the first row (numbers/numbers.js#161).
- `basic.powerMod`: exact for every modulus up to 2^53, and halves the exponent without the 32-bit `>>` operator (exponents of 2^31 and above gave wrong results); `prime.millerRabin` depends on it.
- `basic.modInverse`: returns the inverse in [0, m), not a negative number.
- `calculus.adaptiveSimpson`: splits each interval at its midpoint and passes the recursive arguments in order, with a depth limit, and stops on a NaN error estimate (it computed one Simpson step, or recursed without bound).
- `calculus.limit`: approaches the point with shrinking steps, reports divergence, and uses `"middle"` by default (it evaluated `f` once at `point ± 1e-15`).
- `calculus.MonteCarlo`: samples with `random.sample` (`statistic.randomSample` does not exist) and passes every coordinate to the function.
- `generate.collatz`: the result array is optional and returned.
- `matrix.transpose`: turns a one-dimensional array into a column vector.

### Respond.js 1.4.2.1

- `var FILE = require('lib/file');` at the top, and `ajax(url, callback)` reads a style sheet whose URL is not `http:` or `https:` with `FILE.readFile(url, "utf-8")` instead of `XMLHttpRequest`, so the style sheets of an application loaded from local files are processed.
- The comma-separated `var` declarations are split over several lines. This is formatting only.

The local branch assigns the text to `responseText` without declaring it, so it creates a global variable of that name.

### selectivizr 1.0.2.1

- `var FILE = require('lib/file');` after the opening comment, and `loadStyleSheet(url)` reads a URL that is not `http:` or `https:` with `FILE.readFile(url, "utf-8")` instead of a synchronous `XMLHttpRequest`.
- Some statements are reformatted (`} else {` on one line, object literals over several lines). This is formatting only.

### Squel.js 5.13.0.1

Reserved words are not used as property names, which JScript rejects:

- In the 14 `for...of` helpers that Babel generated, `_iterator.return` is `_iterator.RETURN`. An iterator does not have a `RETURN` method, so the early-exit call of an iterator is never made; for the arrays Squel iterates over this makes no difference.
- `squel.case` and `squel.delete` are `squel._case` and `squel._delete`. `squel.remove`, the alias of `delete`, still works, so a script uses `squel.remove()` or `squel._delete()` for a `DELETE` query and `squel._case()` for a `CASE` expression.
- `inst.function(...)` in `str()` and `rstr()` is `inst['function'](...)`.

## Updating one of these files

1. Take the new upstream file and compare it with the upstream file of the current build (the table above); apply the changes of this page to it again.
2. Name it `<library>-<new version>.1.js`, update the header comment, and upload it to a new folder `ajax/libs/<library>/<new version>.1/` (the current files are listed in `app/assets/integrity.json`).
3. Replace the `.integrity` sidecar and the entry in `app/assets/integrity.json` (the digest is over the text with LF line endings, as `restoreAppAssets.ps1` computes it), update the `require()` or `addScript()` call, and update this page.

A further WelsonJS change to the same upstream version is `.2`, and so on.
