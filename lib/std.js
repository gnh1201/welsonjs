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

// MS JScript Enumerator to Array
if (!Enumerator.prototype.toArray) {
    Enumerator.prototype.toArray = function() {
        var a = [];
        for (; !this.atEnd(); this.moveNext()) {
            var x = {};
            var b = new Enumerator(this.item().Properties_);
            for (; !b.atEnd(); b.moveNext()) {
                var c = b.item();
                if (typeof c.value !== "unknown") {
                    try {
                        x[c.name] = c.value.toString();
                    } catch (e) {
                        x[c.name] = c.value;
                    }
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

function GetResource(ResourceName) {
    return arguments.callee.caller.GetResource(ResourceName);
}

// [lib/std] the time of `sleep()' function is not accuracy #34
function sleep(ms, callback) {
    var handler = null;

    var cur = Date.now();
    var end = cur + ms;

    if (typeof WScript !== "undefined") {
        /*
        while (cur < end) {
            //WScript.Sleep(1);
            cur = Date.now();
        }
        end = Date.now();
        */

        WScript.Sleep(ms);
        if (typeof callback === "function")
            callback()
        ;
    } else if (typeof window !== "undefined") {
        if (typeof callback === "function")
            handler = setTimeout(callback, ms);
        ;
    }

    return { 'ms': end, 'handler': handler };
};

function repeat(target, callback, onError) {
    switch (typeof target) {
        case "number":
        case "boolean":
            var ms = target;

            var i = 0;
            var result = null;
            var handler = null;
            var cur = Date.now();
            var end = cur + ms;

            if (typeof WScript !== "undefined") {
                while (ms === true ? true : (cur < end)) {
                    try {
                        if (typeof callback === "function") {
                            var result = callback(i);
                            if (typeof result === "number") {
                                i += result;
                            } else if (result === false) {
                                break;
                            } else if (result === true) {
                                i += 1;
                            }
                        }
                    } catch (e) {
                        if (typeof onError === "function") {
                            if (onError(e) === false) {
                                break;
                            }
                        }
                    }
                    cur = Date.now();
                }
                end = Date.now();
            } else if (typeof window !== "undefined") {
                if (typeof callback === "function")
                    handler = setInterval(callback, ms);
                ;
            }

            return { 'ms': end, 'handler': handler };

        case "object":
            var arr = target;
            if (arr.length > 0) {
                for (var i = 0; i < arr.length; i++) {
                    try {
                        if (typeof callback === "function")
                            if (callback(i, arr[i]) === false)
                                break
                            ;
                        ;
                    } catch (e) {
                        if (typeof onError === "function")
                            if (onError(e) === false)
                                break
                            ;
                        ;
                    }
                }
            }
            break;
    }
};

function rotate(target, callback, onError) {
    var arr = target;
    var i = 0;
    var stop = false;

    while (!stop) {
        try {
            if (typeof callback === "function") {
                stop = callback(i, arr[i]);
            } else {
                stop = true;
            }
        } catch (e) {
            if (typeof onError === "function")
                stop = onError(e);
            ;
        }

        i++;
        i = i % target.length;
    }
};

function range() {
    var args = arguments;
    var N = [], start, end, step;

    switch(args.length) {
        case 3:
            start = args[0];
            end = args[1];
            step = args[2];
            break;

        case 2:
            start = args[0];
            end = args[1];
            step = 1;
            break;

        case 1:
            start = 0;
            end = args[0];
            step = 1;
            break;
    }

    for (var i = start; i < end; i = i + step)
        N.push(i)
    ;

    return N;
};

function CHR(ord) {
    return String.fromCharCode(ord);
};

function splitLn(s) {
    return s.split(/\r?\n/);
};

function addslashes(s) {
    return s.toString().replace(/\\/g, '\\\\').
        replace(/\u0008/g, '\\b').
        replace(/\t/g, '\\t').
        replace(/\n/g, '\\n').
        replace(/\f/g, '\\f').
        replace(/\r/g, '\\r').
        replace(/'/g, '\\\'').
        replace(/"/g, '\\"')
    ;
};

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Emulate Server.CreateObject
/////////////////////////////////////////////////////////////////////////////////

function CreateObject(progId, serverName, callback) {
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
// Standard Event Object
/////////////////////////////////////////////////////////////////////////////////

function StdEvent(eventName) {
    this.defaultPrevented = false;
    this.timeStamp = new Date();
    this.eventName = eventName;
    this.target = null;

    this.preventDefault = function() {
        this.defaultPrevented = true;
    };
};

function StdEventableObject() {
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

global.GetResource = GetResource;
global.sleep = sleep;
global.repeat = repeat;
global.rotate = rotate;
global.range = range;
global.CHR = CHR;
global.splitLn = splitLn;
global.addslashes = addslashes;

exports.VERSIONINFO = "Standard Lib (std.js) version 0.4";
exports.global = global;
exports.require = global.require;

exports.Event = StdEvent;
exports.EventableObject = StdEventableObject;
