# WelsonJS HTA

This is a standalone YAWW application named `welsonjs-hta`, inspired by the
`open-file` and `chakra-control` examples. Its own `welsonjs_hta.cpp` presents a file picker for
`.hta`, `.htm`, and `.html` documents, opens the selected local document in a
YAWW MSHTML window, and starts the host-side ChakraCore controller. A document
path can also be passed on the command line.

For MSHTML, `.hta` input is copied to a temporary `.html` file beside the
source so it is rendered as page content. This does not reproduce the
standalone `mshta.exe` host or its HTA privilege and ActiveX behavior. The
ChakraCore controller is separate from the page's JavaScript engine.

The build uses the YAWW library from the `native/yaww` submodule. It does
not add or build YAWW's example targets. ChakraCore is fetched and built during
CMake configuration/build. The MSHTML backend requires the Visual Studio C++
ATL component.

```powershell
cmake -S native/welsonjs-hta -B out/build/welsonjs-hta -A Win32
cmake --build out/build/welsonjs-hta --config Release --target welsonjs-hta
```

The executable and its adjacent `webview.js` and ChakraCore runtime are
written to `out/build/welsonjs-hta/bin/Release/`. Run it without arguments to
open the file picker. You can also pass a document path, `--chakra-console`,
`--no-chakra`, or `--chakra-script <path>`.

YAWW is licensed under GPL-3.0. See `native/yaww/LICENSE` for its terms.


