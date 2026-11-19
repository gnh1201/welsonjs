# Patches for the modules `require()` loads

WelsonJS changes some of the third-party JavaScript files it ships. Those changes are
kept as patches in one directory, `patches/`, and the module loader applies them in
memory as it loads a module: the file in `app/assets/js/` or `lib/` stays the file the
vendor published. A vendor update is therefore a new download and a possible patch change,
not a merge of a locally edited copy.

## Layout

The path under `patches/` is the module's path relative to the application folder (the
folder that holds `app.js`), with `/` between the parts:

    patches/app/assets/js/numbers-0.7.0.js.patch       unified diff applied to the source
    patches/app/assets/js/numbers-0.7.0.js.post.js    optional script run after the module

A module at `lib/http.js` is patched from `patches/lib/http.js.patch`. Mirroring the whole
path keeps the patch of an `index.js` in one folder apart from the patch of an `index.js`
in another.

## What `require()` does

1. `require._load()` reads the file.
2. The pre-processors registered with `require.registerPreProcessor()` are called with
   `(filename, text)`; each returns the text to compile. `lib/patch.js` looks here for
   `patches/<path>.patch` and applies it to the source text. Nothing is written to disk.
3. The source is transpiled (`.coffee`, `.ls`, `.ts`, `.re`, `.res`) and executed as usual.
4. The post-processors registered with `require.registerPostProcessor()` are called with
   `(filename, exports, module, dirname)`; the value they return is what `require()`
   keeps. `lib/patch.js` looks here for `patches/<path>.post.js` and runs it with
   `(global, exports, require, module, __filename, __dirname)`.

`app.js` holds only those two lists and `require._applyProcessors()`, which calls a list
in the order its callbacks were registered. Everything about patches is in
`lib/patch.js`: the path under `patches/`, the guard, reading the files, applying them
and reporting a failure. `lib/patch.js` registers both callbacks when it is loaded, and
`app.js` requires it at bootstrap, before the application's own modules, so `patches/`
applies to every module after it. A module that has to change what `require()` loads
without shipping a file under `patches/` registers a callback the same way.

A module loaded from a URL (`http:`, `https:`, `ai:`) and a path that would leave the
application folder (`..`, an absolute path) are not patched. With `STRICT_INTEGRITY`, the
digest check covers the file as it is on disk; a patch and a post-processing script are
part of the repository and are read without that check. While a patch or a
post-processing script runs, `lib/patch.js` marks that file, so a script that calls
`require()` cannot re-enter the patch of the module it belongs to.

A patch or a post-processing script that fails is reported and the module is used without
it, so a vendor file that changed upstream does not stop the application:

    PATCH ERROR! The patch does not match the source at line 21 (patch line 41), FN=patches/app/assets/js/numbers-0.7.0.js.patch - the module is used unpatched.
    POST-PROCESS ERROR! ..., FN=patches/app/assets/js/numbers-0.7.0.js.post.js - the module is used without it.

The fix is then not in effect, which is why the patch carries the digest of the result it
should produce (see below).

## Patch format

A patch is a unified diff for one file, the form `git diff` writes:

- Anything before the file header is a description and is not read.
- The header is a `--- ` line followed by a `+++ ` line. The paths in them are only for
  `git apply`; the loader already knows which module the patch belongs to.
- Each hunk starts with `@@ -<old start>,<count> +<new start>,<count> @@`. Hunks are
  applied in the order they appear, at the line they name, and every context and removed
  line must match the source exactly. A hunk whose counts do not match its lines is
  rejected.
- A `\ No newline at end of file` line is honoured.
- A CRLF source is compared as LF and returned with CRLF; the patched text keeps the line
  ending the file had.

`git apply` can check and apply the same file, because the patch is also a normal diff:

    git apply --check patches/app/assets/js/numbers-0.7.0.js.patch
    git apply         patches/app/assets/js/numbers-0.7.0.js.patch

`.gitattributes` keeps the line endings of `patches/**/*.patch` as they are, so a checkout
with `core.autocrlf` does not change the hunk lines.

## Making a patch

Take the file as the vendor publishes it, keep a copy, edit the copy in the layout the
patch needs, and write the diff:

    mkdir -p /tmp/welsonjs-patch/app/assets/js
    git -C /tmp/welsonjs-patch init -q
    cp app/assets/js/foo-1.0.0.js /tmp/welsonjs-patch/app/assets/js/foo-1.0.0.js
    git -C /tmp/welsonjs-patch add -A
    git -C /tmp/welsonjs-patch -c user.name=welsonjs -c user.email=welsonjs@example.com commit -qm base

    # edit /tmp/welsonjs-patch/app/assets/js/foo-1.0.0.js, then
    git -C /tmp/welsonjs-patch diff > patches/app/assets/js/foo-1.0.0.js.patch

    # the unmodified file in place must take the patch
    git apply --check patches/app/assets/js/foo-1.0.0.js.patch

Write the digest of the patched file in the description of the patch. It is what the
result should be, and what a later change of the vendor file is checked against. The
digests in `app/assets/integrity.json` are Subresource Integrity values (base64), so
compute them the same way:

    openssl dgst -sha384 -binary app/assets/js/foo-1.0.0.js | openssl base64 -A | sed 's/^/sha384-/'

## numbers.js

`numbers-0.7.0` is the first module that uses this:

- `restoreAppAssets.ps1` downloads numbers.js 0.7.0 (`app/assets/js/numbers-0.7.0.js`) and
  verifies it against the digest in `app/assets/integrity.json`, which is the published
  file, unchanged.
- `patches/app/assets/js/numbers-0.7.0.js.patch` carries the WelsonJS changes: the
  `matrix.determinant` fix (numbers/numbers.js#161), `basic.powerMod`,
  `basic.modInverse`, `calculus.adaptiveSimpson`, `calculus.limit`,
  `calculus.MonteCarlo`, `generate.collatz` and `matrix.transpose`.
- `app.js` requires it as `app/assets/js/numbers-0.7.0` and publishes the patched module
  as the global `numbers`.

The patched file is 84,126 bytes, with SHA-384
`sha384-gZ3b+UKp735rDlzLa+Okb+J0nBSwjht3tIsJGUY29H5qmouPW+diziv/wG5kwcxt`, the content
that earlier versions shipped as `numbers-0.7.0.wsh.js`. `testloader.js` checks it at
runtime:

    cscript app.js testloader require_patch data\test-misc.json

The installer and the bootstrap script ship `patches/` along with `app.js`, so an
installed application applies the same patches.
