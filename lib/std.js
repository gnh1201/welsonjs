//////////////////////////////////////////////////////////////////////////////////
//
//    std.js
//
//    Common routines.  Defines LIB object which contains the API, as well as
//    a global DBG function.
//
//    References:
//  * https://github.com/redskyit/wsh-appjs
//  * https://github.com/JSman-/JS-Framework
//
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Polyfills
/////////////////////////////////////////////////////////////////////////////////

if (!Function.prototype.GetResource) {
    Function.prototype.GetResource = function(ResourceName) {
        if (!this.Resources) {
            var UnNamedResourceIndex = 0,
                _this = this;
            this.Resources = {};

            function f(match, resType, Content) {
                _this.Resources[(resType == "[[") ? UnNamedResourceIndex++ : resType.slice(1, -1)] = Content;
            }
            this.toString().replace(/\/\*(\[(?:[^\[]+)?\[)((?:[\r\n]|.)*?)\]\]\*\//gi, f);
        }

        return this.Resources[ResourceName];
    }
}

// ECMAScript 5 compatibility shims for legacy (and modern) JavaScript engines
require("app/assets/js/es5-shim-4.5.14.min");
require("app/assets/js/es5-sham-4.5.14.min");

// JSON 3 was a JSON polyfill for older JavaScript platforms
require("app/assets/js/json3-3.3.2.min");

// ECMAScript 6 compatibility shims for legacy JS engines
require("app/assets/js/es6-shim-0.35.5.min");
require("app/assets/js/es6-sham-0.35.5.min");

/////////////////////////////////////////////////////////////////////////////////
// Global APIs
/////////////////////////////////////////////////////////////////////////////////

global.GetResource = function(ResourceName) {
    return arguments.callee.caller.GetResource(ResourceName);
}

global.sleep = function(ms, callback) {
    if (typeof(WScript) !== "undefined") {
        WScript.Sleep(ms);
        if (typeof(callback) === "function") {
            callback();
        }
    } else {
        if (typeof(callback) === "function") {
            setTimeout(callback, ms);
        }
    }
}

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

exports.VERSIONINFO = "Standard Lib (std.js) version 0.2";
exports.global = global;
exports.require = global.require;

/////////////////////////////////////////////////////////////////////////////////
// Emulate Server.CreateObject
/////////////////////////////////////////////////////////////////////////////////

exports.CreateObject = function(n) {
    return new ActiveXObject(n);
};

/////////////////////////////////////////////////////////////////////////////////
