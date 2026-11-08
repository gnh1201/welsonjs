// managedobject.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// JavaScript interface for WelsonJS.ManagedObject: COM calls with ByRef
// (output) parameter support (#429)
//
// JScript cannot pass arguments by reference, so COM methods that return
// values through ByRef parameters (e.g. MSWinsock GetData, WMI StdRegProv
// GetStringValue) cannot be used directly. This module calls them through
// WelsonJS.ManagedObject, which invokes IDispatch with VT_BYREF arguments.
//
// Requires the WelsonJS.ManagedObject COM component to be registered.
//
// Example:
//   var ManagedObject = require("lib/managedobject");
//
//   var socket = ManagedObject.create("MSWinsock.Winsock.1");
//   var data = socket.callByRef("GetData");
//
//   var reg = ManagedObject.wrap(GetObject("winmgmts:root\\default:StdRegProv"));
//   var value = ManagedObject.ref();
//   var rc = reg.invoke("GetStringValue", 0x80000002, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ProductName", value);
//   console.log(rc + " " + value.Value);
//

function getManagedObject() {
    if (typeof CreateObject !== "undefined" && CreateObject.managed) {
        return CreateObject.managed;
    }
    throw new Error("WelsonJS.ManagedObject is not available. Please install and register the WelsonJS.ManagedObject COM component.");
}

// Returns true for refs created by ManagedObject.ref() (managed ByRefArgument objects).
function isRef(value) {
    return (value != null && typeof value === "object" && getManagedObject().IsByRef(value));
}

// Creates a ByRef holder. Read the output with `.Value` after the call.
function ref(value) {
    var managed = getManagedObject();
    return (typeof value === "undefined") ? managed.ByRef() : managed.ByRef(value);
}

// The argument array is passed as a whole and expanded on the managed side
// (JScript cannot spread arguments into a COM method call with apply).
function invokeWithArguments(wrapper, name, args) {
    return wrapper.CallWithArray(name, args);
}

function toArray(args, start) {
    var result = [];
    for (var i = start; i < args.length; i++) {
        result.push(args[i]);
    }
    return result;
}

// Wraps a COM object (JScript side of the managed ManagedObjectWrapper class).
function ManagedObjectWrapper(target) {
    if (target == null) {
        throw new Error("A COM object is required.");
    }

    this.wrapper = getManagedObject().Wrap(target);
    this.target = this.wrapper.Target;
}

// Calls a method. Pass refs created by ManagedObject.ref() for ByRef parameters;
// their `.Value` is updated after the call. Returns the method's result.
ManagedObjectWrapper.prototype.invoke = function(name) {
    return invokeWithArguments(this.wrapper, name, toArray(arguments, 1));
};

// Calls a method and returns the value of its first ByRef parameter.
// If no ref is passed, one is inserted as the first argument, so that
// `socket.callByRef("GetData")` behaves like VBScript `socket.GetData data`.
// Use invoke() when the method's own return value is also needed.
ManagedObjectWrapper.prototype.callByRef = function(name) {
    var args = toArray(arguments, 1);
    var output = null;

    for (var i = 0; i < args.length; i++) {
        if (isRef(args[i])) {
            output = args[i];
            break;
        }
    }

    if (output === null) {
        output = ref();
        args.unshift(output);
    }

    invokeWithArguments(this.wrapper, name, args);
    return output.Value;
};

ManagedObjectWrapper.prototype.get = function(name) {
    return this.wrapper.Get(name);
};

ManagedObjectWrapper.prototype.set = function(name, value) {
    this.wrapper.Set(name, value);
    return this;
};

// Releases COM objects that scripts no longer reference (see ManagedObject.Collect).
// Collection also runs automatically; call this after releasing large COM
// objects (e.g. after Excel.Application.Quit()) to release them immediately.
function collect() {
    getManagedObject().Collect();
}

// Creates a COM object by ProgID and wraps it.
function create(progId) {
    var obj = CreateObject(progId);
    if (obj == null) {
        throw new Error("Could not create the COM object \"" + progId + "\"");
    }
    return new ManagedObjectWrapper(obj);
}

// Wraps an existing COM object (from CreateObject, GetObject, or another call).
function wrap(obj) {
    return new ManagedObjectWrapper(obj);
}

exports.create = create;
exports.wrap = wrap;
exports.ref = ref;
exports.isRef = isRef;
exports.collect = collect;
exports.ManagedObjectWrapper = ManagedObjectWrapper;

exports.VERSIONINFO = "WelsonJS.ManagedObject interface (managedobject.js) version 0.2";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
