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

The modules are designed to be accessed from WelsonJS scripts through Windows COM/ActiveX automation.

## Included Modules

### `WelsonJS.ManagedObject`

A helper module for loading and interacting with COM objects from WelsonJS.

Features include:

* ProgID-based COM object activation
* COM object creation
* ByRef argument handling
* Support for Windows COM/ActiveX automation

Example:

```javascript
var managedObject = new ActiveXObject("WelsonJS.ManagedObject");

var fso = managedObject.CreateObject("Scripting.FileSystemObject");
```

### `WelsonJS.Dialog`

Provides Windows dialog functionality for WelsonJS applications.

Supported operations include:

* `Alert`
* `Confirm`
* `FileDialog`

Example:

```javascript
var dialog = new ActiveXObject("WelsonJS.Dialog");

dialog.Alert("Hello, WelsonJS!");
```

### `WelsonJS.NamedSharedMemory`

Provides inter-process communication using Windows named shared memory.

This module can be used when multiple processes need to exchange data through a shared memory region.

### `WelsonJS.ProcessControl`

Provides Windows process and input-control functionality, including:

* Virtualized mouse input
* Virtualized keyboard input
* Window handle manipulation

This module can be used to automate or control Windows application windows from WelsonJS.

### `WelsonJS.BitmapControl`

Provides bitmap and image information utilities.

Supported functionality includes:

* Image size information
* Pixel information extraction
* Bitmap-related utilities

## Migration from `WelsonJS.Toolkit`

Users of **WelsonJS.Toolkit**, including users of WelsonJS **0.2.7.57 and earlier**, should migrate to **WelsonJS.ManagedObject**.

`WelsonJS.Toolkit` has been superseded by `WelsonJS.ManagedObject`.

The functionality previously provided by `WelsonJS.Toolkit` has been migrated to managed COM modules included in this package. New applications should use `WelsonJS.ManagedObject` instead of `WelsonJS.Toolkit`.

### Migration summary

| Legacy                           | Replacement                     |
| -------------------------------- | ------------------------------- |
| `WelsonJS.Toolkit`               | `WelsonJS.ManagedObject`        |
| Native/legacy COM modules        | Managed COM modules             |
| Legacy Toolkit-based integration | ManagedObject-based integration |

The migration is intended to provide a cleaner managed implementation while maintaining compatibility with the Windows COM-based architecture used by WelsonJS.

## Requirements

* Windows
* WelsonJS
* Windows COM / ActiveX support

Because these modules interact with Windows-specific COM and system APIs, they are intended for Windows environments.

## Usage

After installing the package, the COM modules can be created from WelsonJS using `ActiveXObject`.

For example:

```javascript
var managedObject = new ActiveXObject("WelsonJS.ManagedObject");
var dialog = new ActiveXObject("WelsonJS.Dialog");
var sharedMemory = new ActiveXObject("WelsonJS.NamedSharedMemory");
var processControl = new ActiveXObject("WelsonJS.ProcessControl");
var bitmapControl = new ActiveXObject("WelsonJS.BitmapControl");
```

The exact API exposed by each module may vary depending on the installed version.

## Compatibility

WelsonJS.ManagedObject is designed specifically for the **WelsonJS scripting environment** and its Windows ECMAScript/WSH-based execution model.

Applications migrating from `WelsonJS.Toolkit` should verify their existing COM registration and module usage after migration.

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
