# WelsonJS.ManagedObject

Managed Windows COM modules for the [WelsonJS](https://github.com/gnh1201/welsonjs) scripting environment.

**Version:** 0.2.7.58
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
UseObject("WelsonJS.NamedSharedMemory", function(sharedMemory) {
    // Use named shared memory here.
});
```

This module can be used when multiple processes need to exchange data through a shared memory region.

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

WSH-provided `Scripting.*` objects are **not included** in `WelsonJS.ManagedObject` and remain outside the scope of this package.

## Migration from `WelsonJS.Toolkit`

Users of **WelsonJS.Toolkit**, including users of WelsonJS **0.2.7.57 and earlier**, should migrate to **WelsonJS.ManagedObject**.

`WelsonJS.Toolkit` has been superseded by `WelsonJS.ManagedObject`.

The functionality previously provided by `WelsonJS.Toolkit` has been migrated to managed COM modules included in this package. New applications should use `WelsonJS.ManagedObject` instead of `WelsonJS.Toolkit`.

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
Opensource Software Maintainer
**Catswords Research**

Contact: [gnh1201@catswords.re.kr](mailto:gnh1201@catswords.re.kr)

**#OPENTOWORK**

## Project

* GitHub: https://github.com/gnh1201/welsonjs
* Package: `WelsonJS.ManagedObject`
* Version: `0.2.7.58`

## License

WelsonJS.ManagedObject is distributed under the **GNU General Public License v3.0 only (GPL-3.0-only)**.

See the [`LICENSE`](LICENSE) file for the complete license text.
