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

if (!Enumerator.prototype.toArray2) {
    Enumerator.prototype.toArray2 = function() {
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
        while (cur < end) {
            WScript.Sleep(1);
            cur = Date.now();
        }
        end = Date.now();
        
        //WScript.Sleep(ms);
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

function repeat(target, callback, onError, onNextInterval, onNext) {
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
                            if (onError(e, i) === false) {
                                break;
                            }
                        }
                    }

                    // if use onNextInterval method
                    if (typeof onNextInterval === "function") {
                        var nextInterval = onNextInterval();
                        if (typeof nextInterval === "number") {
                            var nextEnd = cur + nextInterval;
                            while (cur < nextEnd) {
                                WScript.Sleep(1);
                                cur = Date.now();
                            }
                        }
                    }

                    // if use onNext method
                    if (typeof onNext === "function") {
                        try {
                            onNext();
                        } catch (e) {}
                    }

                    // set the last time
                    cur = Date.now();
                }
                end = Date.now();
            } else if (typeof window !== "undefined") {
                if (typeof callback === "function") {
                    handler = setInterval(callback, ms);
                }
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
                            if (onError(e, i, arr[i]) === false)
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
                stop = onError(e, i, arr[i]);
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

function alert(msg) {
    CreateObject("WScript.Shell").Popup(msg);
}

/////////////////////////////////////////////////////////////////////////////////
// Standard Event Object
/////////////////////////////////////////////////////////////////////////////////

// https://developer.mozilla.org/ko/docs/Web/API/Event

function StdEvent(type) {
    this.defaultPrevented = false;
    this.timeStamp = new Date();
    this.type = type;
    this.isTrusted = true;
    this.cancelable = true;
    this.target = null;
    this.currentTarget = null;
    this.eventPhase = StdEvent.NONE;
    this.bubbles = false;   // Not used but to be compatible
    this.composed = false;   // Not used but to be compatible

    this.preventDefault = function() {
        this.defaultPrevented = true;
    };

    // Not used but to be compatible
    this.initEvent = function(type, bubbles, cancelable) {
        this.type = type;
        this.bubbles = bubbles;
        this.cancelable = cancelable;
    };

    // Not used but to be compatible
    this.stopImmediatePropagation = function() {};

    // Not used but to be compatible
    this.stopPropagation = function() {};
};
StdEvent.NONE = 0;
StdEvent.CAPTURING_PHASE = 1;    // Not used but to be compatible
StdEvent.AT_TARGET = 2;
StdEvent.BUBBLING_PHASE = 3;    // Not used but to be compatible

function StdEventTarget() {
    this.__events__ = [];

    this.dispatchEvent = function(event, __exception__) {
        event.target = this;
        event.isTrusted = false;
        event.eventPhase = StdEvent.AT_TARGET;
        event.currentTarget = event.target;
        for (var i = 0; i < this.__events__.length; i++) {
            var e = this.__events__[i];
            if (e.type == event.type && typeof(e.listener) === "function") {
                try {
                    e.listener(event, __exception__);
                } catch (ex) {
                    this.dispatchEvent(new StdEvent("error"), ex);
                }
            }
        }
    };

    this.addEventListener = function(type, listener) {
        if (typeof listener === "function") {
            this.__events__.push({
                "type": type,
                "listener": listener,
                "counter": StdEventTarget.__counter__
            });
            StdEventTarget.__counter__++;
        } else {
            throw new TypeError("EventListener must be a function");
        }
    };

    this.removeEventListener = function(type, listener) {
        if (typeof listener === "function") {
            for (var i = 0; i < this.__events__.length; i++) {
                var e = this.__events__[i];
                if (e.type == type && typeof(e.listener) === "function" && e.listener.toString() == listener.toString()) {
                    delete this.__events__[i];
                }
            }
        } else {
            throw new TypeError("EventListener must be a function");
        }
    };
};
StdEventTarget.__counter__ = 0;

function AsyncFunction(f, _filename) {
    this.f = f;
    this._filename = _filename;

    this.run = function() {
        var args = Array.from(arguments);

        // increase number of async functions
        AsyncFunction.counter++;
        
        // decrease number of async functions
        var _this = this;
        var _f = function() {
            if (typeof _this.f === "function") _this.f();
            AsyncFunction.counter--;
        };

        // CLI or Window?
        if (typeof WScript !== "undefined") {
            require("lib/shell").show(["cscript", "app.js", this._filename, f].concat(args));
        } else {
            sleep(1, _f);
        }
    };

    this.runSynchronously = function() {
        return this.f.apply(null, arguments);
    };
};
AsyncFunction.counter = 0;
AsyncFunction.bind = function(exports, args) {
    var result = false;

    if (args.length > 0) {
        var f = '_async_' + args[0];

        if (f in exports && typeof exports[f] === "function") {
            try {
                exports[f](args);
                result = true;
            } catch(e) {
                console.error("AsyncFunction.bind exception", e.message);
                result = false;
            }
        }
    }

    return result;
};

global.GetResource = GetResource;
global.sleep = sleep;
global.repeat = repeat;
global.rotate = rotate;
global.range = range;
global.CHR = CHR;
global.splitLn = splitLn;
global.addslashes = addslashes;
global.AsyncFunction = AsyncFunction;

exports.VERSIONINFO = "Standard Library (std.js) version 0.8";
exports.global = global;
exports.require = global.require;

exports.Event = StdEvent;
exports.EventTarget = StdEventTarget;

exports.alert = alert;
