//////////////////////////////////////////////////////////////////////////////////
//
//    std.js
//
//    Common routines.  Defines LIB object which contains the API, as well as
//    a global DBG function.
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

if (!Enumerator.prototype.toArray) {
    Enumerator.prototype.toArray = function() {
        var items = [];
        for (; !this.atEnd(); this.moveNext()) {
            var item = this.item();
            try {
                items.push(item);
            } catch (e) {}
        }
        return items;
    };
}

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

global.CHR = function(ord) {
    return String.fromCharCode(ord);
}

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Emulate Server.CreateObject
/////////////////////////////////////////////////////////////////////////////////

exports.CreateObject = function(progId, serverName, callback) {
    var progIds = [];
    var _CreateObject = function(p, s) {
        if (typeof(WScript) !== "undefined") {
            return WScript.CreateObject(p, s);
        } else {
            return new ActiveXObject(p);
        }
    };

    if (typeof(progId) == "object") {
        progIds = progId;
    } else {
        progIds.push(progId);
    }

    for (var i = 0; i < progIds.length; i++) {
        try {
            var obj = _CreateObject(progIds[i], serverName);
            if (typeof(callback) === "function") {
                callback(obj, progIds[i]);
            }
            return obj;
        } catch (e) {
            console.error(e.message);
        };
    }
};

/////////////////////////////////////////////////////////////////////////////////

exports.VERSIONINFO = "Standard Lib (std.js) version 0.2";
exports.global = global;
exports.require = global.require;
