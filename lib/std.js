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
require("node_modules/es5-shim/es5-shim");
require("node_modules/es5-shim/es5-sham");

// JSON 3 was a JSON polyfill for older JavaScript platforms
require("node_modules/json3/lib/json3");

// ECMAScript 6 compatibility shims for legacy JS engines
require("node_modules/es6-shim/es6-shim");
require("node_modules/es6-shim/es6-sham");

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

global.exit = function() {
    if (typeof(WScript) !== "undefined") {
        WScript.Quit();
    } else if (typeof(window) !== "undefined") {
        window.close();
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
