# WelsonJS HTA

`welsonjs-hta` is a standalone YAWW launcher for local `.hta`, `.htm`, and
`.html` documents. `.hta` files always use MSHTML. For `.htm` and `.html`, the
launcher selects WebView2 when its runtime is available, then EdgeHTML when its
Windows API is available, and otherwise falls back to MSHTML. Run it without
arguments to open a file picker, or pass a document path directly.

Each viewer runs the host-side ChakraCore controller from the adjacent
`webview.js`. Pass `--chakra-console`, `--no-chakra`, or
`--chakra-script <path>` to configure it. ChakraCore is separate from the
document's page JavaScript engine.

For MSHTML, `.hta` input is copied to a temporary `.html` file beside the
source so URLMON renders it as page content. This does not reproduce the
standalone `mshta.exe` host or its HTA privilege and ActiveX behavior.

The build uses YAWW from the `native/yaww` submodule and builds a dispatcher
plus separate WebView2, EdgeHTML, and MSHTML viewers. ChakraCore's
architecture-matched prebuilt archive is used by default and its SHA-256 is
verified during configuration. WebView2 requires the WebView2 Runtime;
EdgeHTML depends on the Windows WebViewControl API. The MSHTML backend requires
the Visual Studio C++ ATL component.

```powershell
cmake -S native/welsonjs-hta -B out/build/welsonjs-hta -A x64
cmake --build out/build/welsonjs-hta --config Release --target welsonjs-hta
```

The launcher and engine-specific viewers are written to
`out/build/welsonjs-hta/bin/Release/`, alongside `webview.js` and the required
runtime DLLs. YAWW is licensed under GPL-3.0. See `native/yaww/LICENSE` for its
terms.
