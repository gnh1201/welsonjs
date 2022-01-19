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
        var a = [];
        for (; !this.atEnd(); this.moveNext()) {
            var x = {};
            var b = new Enumerator(this.item().Properties_);
            for (; !b.atEnd(); b.moveNext()) {
                var c = b.item();
                if (typeof c.value !== "unknown") {
                    x[c.name] = c.value;
                } else {
                    var i = 0, d = [];
                    while (true) {
                        try {
                            d.push(c.value(i));
                            i++;
                        } catch (e) {
                            break;
                        }
                    }
                    x[c.name] = d;
                }
            }
            a.push(x);
        }
        return a;
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
};

global.CHR = function(ord) {
    return String.fromCharCode(ord);
};

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

var StdEvent = function(eventName) {
    this.bubbles = false;   // Not supported
    this.cancelable = false;   // Not supported
    this.composed = false;   // Not supported
    this.currentTarget = null;   // Not supported
    this.defaultPrevented = false;
    this.eventPhase = null;   // TODO
    this.isTrusted = true;   // Not supported
    this.timeStamp = new Date();

    this.eventName = eventName;
    this.target = null;

    // Not supported
    this.composedPath = function() {
        return null;   
    };

    this.preventDefault = function() {
        this.defaultPrevented = true;
    };

    // Not supported
    this.stopImmediatePropagation = function() {
        return null;   
    };

    // Not supported
    this.setPropagation = function() {
        return null;   
    };
};

var StdEventableObject = function() {
    this.dispatchEvent = function(event) {
        event.target = this;
        if(('on' + event.eventName) in this) this['on' + event.eventName](event);
    };

    this.addEventListener = function(eventName, fn) {
        if (typeof(fn) == "function") {
            this['on' + eventName] = fn;
        } else {
            throw new TypeError("EventListener must be a function");
        }
    };
};

exports.VERSIONINFO = "Standard Lib (std.js) version 0.3";
exports.global = global;
exports.require = global.require;

exports.Event = StdEvent;
exports.EventableObject = StdEventableObject;
