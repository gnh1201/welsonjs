# WelsonJS.ManagedObject

Managed Windows COM modules for the [WelsonJS](https://github.com/gnh1201/welsonjs) scripting environment.

**License:** GPL-3.0-only
**Author:** Catswords Research

## Overview

**WelsonJS.ManagedObject** provides managed COM modules for the WelsonJS scripting environment.

The package extends WelsonJS with Windows-specific capabilities such as:

* COM object activation
* Windows dialogs
* Inter-process communication (IPC)
* Mouse and keyboard input simulation
* Window handle manipulation
* Bitmap and image information extraction
* Microsoft Compress (MSCOMP/SZDD) compression and decompression

The managed object functionality is automatically initialized by the WelsonJS `app.js` runtime and is available globally. Users do not need to explicitly create a `WelsonJS.ManagedObject` instance.

## Included Modules

### `WelsonJS.Dialog`

Provides Windows dialog functionality for WelsonJS applications.

Supported operations include:

* `Alert`
* `Confirm`
* `FileDialog`

Example:

```javascript
UseObject("WelsonJS.Dialog", function(dialog) {
    dialog.Alert("boom");
});
```

### `WelsonJS.NamedSharedMemory`

Provides inter-process communication using Windows named shared memory.

Example:

```javascript
var NSM = require("lib/namedsharedmemory");
var mem = new NSM.NamedSharedMemory("MySharedMemory");

if (!mem.writeText("Hello")) {
    console.error(mem.getLastError());
}
console.log(mem.readText());
mem.close();
```

This module can be used when multiple processes need to exchange data through a shared memory region.

* The region is 1024 bytes. Text is stored as UTF-8 with a NUL terminator, so up to 1023 bytes of text fit; longer text is rejected (`false`) instead of being truncated.
* ASCII text is byte-compatible with earlier versions. Data that is not valid UTF-8 is decoded with the system ANSI code page, for compatibility with older writers.
* Methods return `false` (or `""` for reads) on failure; `LastError` describes the cause.
* A mapping stays open in the process until `CloseSharedMemory` is called; the OS frees it after every process has closed it.
* Reads and writes are not synchronized between processes.
* Names prefixed with `Global\` require `SeCreateGlobalPrivilege` (typically administrator or service accounts).

### `WelsonJS.ProcessControl`

Provides Windows process and input-control functionality, including:

* Virtualized mouse input
* Virtualized keyboard input
* Window handle manipulation

Example:

```javascript
UseObject("WelsonJS.ProcessControl", function(processControl) {
    // Control Windows input and windows here.
});
```

### `WelsonJS.BitmapControl`

Provides bitmap and image information utilities.

Supported functionality includes:

* Image size information
* Pixel information extraction
* Bitmap-related utilities

Example:

```javascript
UseObject("WelsonJS.BitmapControl", function(bitmapControl) {
    // Inspect bitmap and image information here.
});
```

### `WelsonJS.MsCompress`

Provides Microsoft Compress (MSCOMP/SZDD) compression and decompression functionality for WelsonJS applications.

This module can be used to work with legacy Microsoft compressed files and SZDD-compressed data.

Example:

```javascript
UseObject("WelsonJS.MsCompress", function(msCompress) {
    // Compress or decompress data here.
});
```

## Usage

`WelsonJS.ManagedObject` is automatically initialized by the WelsonJS `app.js` runtime.

Therefore, applications **do not need to create or load `WelsonJS.ManagedObject` directly**.

Instead, use the global `UseObject` and `CreateObject` functions provided by WelsonJS.

### UseObject

Use `UseObject` to load a WelsonJS module and receive the module instance through a callback.

```javascript
UseObject("WelsonJS.Dialog", function(dialog) {
    dialog.Alert("Hello, WelsonJS!");
});
```

The callback receives the requested module as its argument.

### CreateObject

Use `CreateObject` to create a supported Windows COM object from its ProgID.

```javascript
var object = CreateObject("Some.Component");
```

For example:

```javascript
var object = CreateObject("Some.Component");

if (object) {
    // Use the COM object here.
}
```

`CreateObject` supports ProgID-based COM activation and ByRef argument handling through the managed COM implementation.

### ByRef arguments (`Wrap` / `ByRef`)

JScript cannot pass arguments by reference. To call a COM method with `ByRef`/`out` parameters (for example `MSWinsock.Winsock` `GetData`, or WMI `StdRegProv` `GetStringValue`), use `lib/managedobject.js`:

```javascript
var ManagedObject = require("lib/managedobject");

// The first ByRef parameter is returned (a ref is inserted if none is passed)
var socket = ManagedObject.create("MSWinsock.Winsock.1");
var data = socket.callByRef("GetData");

// Explicit refs, for any COM object however it was obtained
var reg = ManagedObject.wrap(GetObject("winmgmts:root\\default:StdRegProv"));
var value = ManagedObject.ref();
var rc = reg.invoke("GetStringValue", 0x80000002, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ProductName", value);
console.log(value.Value);
```

The same is available directly on the managed object:

```javascript
var managed = CreateObject.managed;
var target = managed.Wrap(someComObject);
var output = managed.ByRef();        // or managed.ByRef(initialValue)

target.Call("MethodWithOutParam", "input", output);
console.log(output.Value);           // value written by the callee
```

* `Wrap(obj)` accepts any COM object, however it was obtained: `WScript.CreateObject` (ADO, Scripting, WScript), `GetObject`, or the return value of another call (WMI services, Office collections, Shell folders). Managed (.NET) objects are accepted only if the same `ManagedObject` instance created them.
* `Wrap` adds no tracking reference of its own; see [Releasing COM objects](#releasing-com-objects) for when wrapped COM objects are released. `wrapper.Target` returns the original object.
* There is no limit on the number of arguments: `lib/managedobject.js` passes them as one array through `CallWithArray`.
* `Call(name, args...)`, `CallWithArray(name, array)`, `Get(name)`, and `Set(name, value)` call members through late binding. ByRef values are copied back for both COM and managed targets.
* Failures are reported as COM errors whose message names the ProgID or member; the original HRESULT is preserved.

### Releasing COM objects

COM objects that pass through `WelsonJS.ManagedObject` (created by `CreateObject`, returned by `Call`/`Get`, or passed as arguments) are held by .NET runtime callable wrappers until the .NET garbage collector runs. Because a script host allocates little managed memory, the collector could otherwise stay idle while released COM objects keep their memory and handles.

* A collection runs automatically after every `CollectThreshold` COM objects (default `100`, process-wide; `0` disables it).
* Call `Collect()` (or `require("lib/managedobject").collect()`) after releasing large COM objects, such as after `Excel.Application.Quit()`, to release them immediately.

```javascript
excel.Quit();
excel = null;
CreateObject.managed.Collect();
```

### Choosing Between `UseObject` and `CreateObject`

Use `UseObject` when loading one of the WelsonJS modules provided by this package:

```javascript
UseObject("WelsonJS.Dialog", function(dialog) {
    dialog.Alert("Hello!");
});
```

Use `CreateObject` when creating a COM object by its ProgID:

```javascript
var object = CreateObject("Some.Component");
```

The managed COM bridge is already available globally through the WelsonJS runtime, so there is no need to instantiate `WelsonJS.ManagedObject` explicitly.

## Scope

WelsonJS.ManagedObject provides managed implementations for Windows COM modules used by WelsonJS.

The following modules are included:

| Module                       | Description                         |
| ---------------------------- | ----------------------------------- |
| `WelsonJS.Dialog`            | Windows dialog operations           |
| `WelsonJS.NamedSharedMemory` | Named shared memory IPC             |
| `WelsonJS.ProcessControl`    | Mouse, keyboard, and window control |
| `WelsonJS.BitmapControl`     | Bitmap and image information        |
| `WelsonJS.MsCompress`         | Microsoft Compress / SZDD support   |

WSH-provided `Scripting.*`, `ADODB.*`, and `WScript.*` objects are **not included** in `WelsonJS.ManagedObject` and remain outside the scope of this package.

### Migration from `WelsonJS.Toolkit`

Users of **WelsonJS.Toolkit**, including users of WelsonJS **0.2.7.57 and earlier**, should migrate to **WelsonJS.ManagedObject**.

`WelsonJS.Toolkit` has been superseded by `WelsonJS.ManagedObject`.

The functionality previously provided by `WelsonJS.Toolkit` has been migrated to managed COM modules included in this package. New applications should use `WelsonJS.ManagedObject` instead of `WelsonJS.Toolkit`.

If migration is difficult due to compatibility requirements, you can use the `WelsonJS.Legacy.Cipher` and `WelsonJS.Legacy.Toolkit` ProgIDs, which provide compatibility with the existing APIs.

### Migration summary

| Legacy                           | Replacement                  |
| -------------------------------- | ---------------------------- |
| `WelsonJS.Toolkit`               | `WelsonJS.ManagedObject`     |
| Native/legacy COM modules        | Managed COM modules          |
| Legacy Toolkit-based integration | `UseObject` / `CreateObject` |

The migration is intended to provide a cleaner managed implementation while maintaining compatibility with the Windows COM-based architecture used by WelsonJS.

## Requirements

* Windows
* WelsonJS
* Windows COM / ActiveX support

Because these modules interact with Windows-specific COM and system APIs, they are intended for Windows environments.

## Compatibility

WelsonJS.ManagedObject is designed specifically for the **WelsonJS scripting environment** and its Windows ECMAScript/WSH-based execution model.

Applications migrating from `WelsonJS.Toolkit` should verify their existing module usage and COM registration after migration.

## Developer

**Namhyeon Go**
Open-source Software Maintainer
**Catswords Research**

Contact: [gnh1201@catswords.re.kr](mailto:gnh1201@catswords.re.kr)

**#OPENTOWORK**

## Project

* GitHub: https://github.com/gnh1201/welsonjs
* Package: `WelsonJS.ManagedObject`

## License

WelsonJS.ManagedObject is distributed under the **GNU General Public License v3.0 only (GPL-3.0-only)**.

See the [`LICENSE`](LICENSE) file for the complete license text.
