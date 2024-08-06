// std.js
// Common routines. Defines STD object which contains the APIs.
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

// Polyfills
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

// The provided code snippet has been corrected by ChatGPT.
// https://chat.openai.com/share/eaab056c-d265-4ee3-b355-9f29176a9caa
// Related issues: #75 #42 #30
if (typeof Enumerator !== "undefined") {
    Enumerator.prototype.toArray = function() {
        var result = [];
        while (!this.atEnd()) {
            var currentItem = this.item();
            var currentItemProperties = currentItem.Properties_;
            var itemObject = {};

            var propertiesEnumerator = new Enumerator(currentItemProperties);
            while (!propertiesEnumerator.atEnd()) {
                var property = propertiesEnumerator.item();
                if (typeof property.value !== "unknown") {  // The type "Unknown" is Array
                    itemObject[property.name] = property.value;
                } else {
                    var arrayValues = [];
                    var index = 0;
                    while (true) {
                        try {
                            arrayValues.push(property.value(index));
                            index++;
                        } catch (e) {
                            break;
                        }
                    }
                    itemObject[property.name] = arrayValues;
                }
                propertiesEnumerator.moveNext();
            }
            result.push(itemObject);
            this.moveNext();
        }
        return result;
    };
}

// Global APIs
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

        if (typeof callback === "function") {
            callback();
        }
    } else if (typeof window !== "undefined") {
        if (typeof callback === "function") {
            handler = setTimeout(callback, ms);
        }
    }

    return {
        'ms': end,
        'handler': handler
    };
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

            return {
                'ms': end, 'handler': handler
            };

        case "object":
            var arr = target;
            if (arr.length > 0) {
                for (var i = 0; i < arr.length; i++) {
                    try {
                        if (typeof callback === "function")
                            if (callback(i, arr[i]) === false)
                                break;;
                    } catch (e) {
                        if (typeof onError === "function")
                            if (onError(e, i, arr[i]) === false)
                                break;;
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
                stop = onError(e, i, arr[i]);;
        }

        i++;
        i = i % target.length;
    }
};

function range() {
    var args = arguments;
    var N = [],
        start, end, step;

    switch (args.length) {
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
        N.push(i);

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
    replace(/"/g, '\\"');
};

// Private APIs / Utility functions

// Emulate Server.CreateObject
function CreateObject(progId, serverName, callback) {
    var progIds = [];
    var _CreateObject = function(p, s) {
        if (typeof WScript !== "undefined") {
            return WScript.CreateObject(p, s);
        } else if (typeof ActiveXObject !== "undefined") {
            return new ActiveXObject(p);
        }
        return null;
    };

    if (typeof progId == "object") {
        progIds = progId;
    } else {
        progIds.push(progId);
    }

    for (var i = 0; i < progIds.length; i++) {
        try {
            var obj = _CreateObject(progIds[i], serverName);
            if (typeof callback === "function") {
                callback(obj, progIds[i]);
            }
            return obj;
        } catch (e) {
            console.error(e.message);
        };
    }
}

function alert(message) {
    if (typeof window !== "undefined") {
        window.alert(message);
    } else {
        CreateObject("WScript.Shell").Popup(message);
    }
}

function confirm(message) {
    if (typeof window !== "undefined") {
        return window.confirm(message);
    } else {
        CreateObject("WScript.Shell").Popup(message);
    }
}

// Standard Event Object
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
    this.bubbles = false; // Not used but to be compatible
    this.composed = false; // Not used but to be compatible

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
StdEvent.CAPTURING_PHASE = 1; // Not used but to be compatible
StdEvent.AT_TARGET = 2;
StdEvent.BUBBLING_PHASE = 3; // Not used but to be compatible

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

/*
var a = new AsyncFunction(function() {
    console.log("calling A");
});

var _async_b = function(function() {
    console.log("calling B");
});

function main(args) {
    AsyncFunction.bind(this, args);
    console.log("welcome");
}

exports.a = a;
exports._async_b = _async_b;
*/

function AsyncFunction(f, __filename) {
    this.f = f;
    this.__filename = __filename;

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
            require("lib/shell").show(["cscript", "app.js", this.__filename, "/async", f].concat(args));
        } else {
            sleep(1, _f);
        }
    };

    this.runSynchronously = function() {
        return this.f.apply(null, arguments);
    };

    if (typeof this.__filename === "string") {
        this.__filename = __filename;
    } else if (typeof WScript !== "undefined") {
        this.__filename = (function(path) {
            var pos = Math.max.apply(null, [path.lastIndexOf("\\"), path.lastIndexOf("/")]);
            return (pos > -1 ? path.substring(pos + 1) : "");
        })(WScript.ScriptFullName);
    }

    AsyncFunction.__counter__++;
};
AsyncFunction.__counter__ = 0;
AsyncFunction.Initialize = function(exports, args) {
    if (args.length < 2)
        return;

    if (args[0] != "/async")
        return;

    var target = args[1];
    if (target in exports && exports[target] instanceof AsyncFunction) {
        try {
            exports[target].f(args.slice(2));
        } catch (e) {
            console.error("Exception on", target, e.message);
        }
    }

    throw new AsyncFunction.Resolved("Resolved");
};
AsyncFunction.bind = function(exports, args) {    // compatible under 0.2.7.31
    console.warn("AsyncFunction.bind() is deprecated. Use AsyncFunction.Initialize()");
    return AsyncFunction.Initialize(exports, args); 
};
AsyncFunction.Resolved = function(message) {
    this.name = "AsyncFunction.Resolved";
    this.message = message;
};
AsyncFunction.Resolved.prototype = new Error();
AsyncFunction.Resolved.prototype.constructor = AsyncFunction.Resolved;

// [app] Transpiling ES6 generator functions #75
function GeneratorFunction(f) {
    var _lastState = 0;
    var _state = 0;
    var _yield = function(value) {
        _state++;
        if (_state > _lastState) {
            throw new GeneratorFunction.Yield(value);
        }
    };

    this.next = function() {
        var go = true;
        var value = undefined;

        _state = 0;

        while (go) {
            try {
                f(_yield);
            } catch (e) {
                if (e instanceof GeneratorFunction.Yield) {
                    value = e.message;
                    go = false;
                    _lastState = _state;
                } else {
                    console.error(e.message);
                }
            }
        }

        return {
            "value": value,
            "done": false
        }
    };
}
GeneratorFunction.Yield = function(message) {
    this.name = "GeneratorFunction.Yield";
    this.message = message;
};
GeneratorFunction.Yield.prototype = new Error();
GeneratorFunction.Yield.prototype.constructor = GeneratorFunction.Yield;

/*
var a = new GeneratorFunction(function(_yield) {
    _yield("a");
    _yield("b");
    _yield("c");
});
console.log(a.next().value);
console.log(a.next().value);
console.log(a.next().value);
*/

function StdStorage() {
    var data = {};
    var commit = function() {
        this.length = Object.keys(data).length;
    };

    this.length = 0;
    this.key = function(idx) {
        var keyName = Object.keys(data)[idx];
        return data[keyName];
    };
    this.setItem = function(keyName, keyValue) {
        data[keyName] = keyValue;
        commit();
    };
    this.getItem = function(keyName) {
        return data[keyName];
    };
    this.removeItem = function(keyName) {
        delete data[keyName];
        commit();
    };
    this.clear = function() {
        data = {};
        commit();
    };
}

global.GetResource = GetResource;
global.sleep = sleep;
global.repeat = repeat;
global.rotate = rotate;
global.range = range;
global.CHR = CHR;
global.splitLn = splitLn;
global.addslashes = addslashes;
global.AsyncFunction = AsyncFunction;

exports.Event = StdEvent;
exports.EventTarget = StdEventTarget;
exports.Storage = StdStorage;

exports.alert = alert;
exports.confirm = confirm;

exports.VERSIONINFO = "WelsonJS Standard Library (std.js) version 0.8.7";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
