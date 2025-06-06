// std.js
// WelsonJS Standard Library
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
//
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

function alert(message) {
    if (typeof window !== "undefined") {
        window.alert(message);
    } else {
        try {
            CreateObject("WScript.Shell").Popup(message, 0, "WelsonJS", 0);
        } catch (e) {
            console.error(e.message);
        }
    }
}

function confirm(message) {
    var result;
    
    if (typeof window !== "undefined") {
        result = window.confirm(message);
    } else {
        try {
            result = (CreateObject("WScript.Shell").Popup(message, 0, "WelsonJS", 4) == 6);
        } catch (e) {
            console.error(e.message);
            result = false;
        }
    }
    
    return result;
}

function prompt(message, _default) {
    var result;
    
    if (typeof window !== "undefined") {
        result = window.prompt(message);
    } else {
        try {
            result = CreateObject("WelsonJS.Toolkit").Prompt(message);
        } catch (e) {
            console.error(e.message);
            if (typeof _default !== "undefined") {
                result = _default;
                console.warn("Use default value:", _default);
            }
        }
    }
    
    return result;
}

function parseEnv(s) {
    var envConfig = {};
    var lines = s.split('\n');

    for (var i = 0; i < lines.length; i++) {
        var line = lines[i].replace(/^\s+|\s+$/g, '');
        if (line && line.indexOf('=') !== -1 && line.charAt(0) !== '#') {
            var parts = line.split('=');
            var key = parts[0].replace(/^\s+|\s+$/g, '');
            var value = parts[1].replace(/^\s+|\s+$/g, '').replace(/^"(.*)"$/, '$1');
            envConfig[key] = value;
        }
    }

    return envConfig;
};

// Standard Event Object
function StdEvent(type, options) {
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

    // Apply the options
    for (var optionKey in options) {
        if (!(optionKey in this)) {
            this[optionKey] = options[optionKey];
        }
    }
};
StdEvent.NONE = 0;
StdEvent.CAPTURING_PHASE = 1; // Not used but to be compatible
StdEvent.AT_TARGET = 2;
StdEvent.BUBBLING_PHASE = 3; // Not used but to be compatible

// Standard EventTarget Object
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

function AsyncFunction(f) {
    this.f = f;
    this.__filename = AsyncFunction.__filename__;

    this.run = function() {
        var args = Array.from(arguments);

        // increase number of async functions
        AsyncFunction.__counter__++;

        // decrease number of async functions
        var dispatch = function(_args, _f) {
            if (typeof _f === "function") _f.apply(null, _args);
            AsyncFunction._counter--;
        };

        // CLI or Window?
        if (typeof WScript !== "undefined") {
            (function(_args, SHELL) {
                SHELL.show(["cscript", "app.js", this.__filename, "/async", f].concat(_args));
            })(args, require("lib/shell"));
        } else if (typeof window !== "undefined") {
            (function(_args, _f) {
                window.setTimeout(function() {
                    dispatch(_args, _f);
                }, 1);
            })(args, this.f);
        } else {
            dispatch(args, this.f);
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
AsyncFunction.__filename__ = "bootstrap.js";
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

    throw new AsyncFunction.Initialized("Initialized");
};
AsyncFunction.bind = function(exports, args) {    // compatible under 0.2.7.31
    console.warn("AsyncFunction.bind() is deprecated. Use AsyncFunction.Initialize()");
    return AsyncFunction.Initialize(exports, args); 
};
AsyncFunction.Initialized = function(message) {
    this.name = "AsyncFunction.Initialized";
    this.message = message;
};
AsyncFunction.Initialized.prototype = new Error();
AsyncFunction.Initialized.prototype.constructor = AsyncFunction.Initialized;

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

function StdAccessor(initialValue) {
    var value = initialValue;

    this.get = function() {
        return value;
    };

    this.set = function(v) {
        value = v;
    };
}
StdAccessor.isAccessor = function(obj) {
    return obj instanceof StdAccessor;
};

function StdStorage() {
    var data = new StdAccessor({});
    var length = new StdAccessor(0);

    function commit() {
        length.set(Object.keys(data.get()).length);
    }

    this.setItem = function(keyName, keyValue) {
        var d = data.get();
        d[keyName] = keyValue;
        data.set(d);
        commit();
    };

    this.getItem = function(keyName) {
        return data.get()[keyName];
    };

    this.removeItem = function(keyName) {
        var d = data.get();
        delete d[keyName];
        data.set(d);
        commit();
    };

    this.clear = function() {
        data.set({});
        commit();
    };

    this.key = function(idx) {
        var d = data.get();
        var keyName = Object.keys(d)[idx];
        return d[keyName];
    };

    this.length = function() {
        return length.get();
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
global.GeneratorFunction = GeneratorFunction;
global.parseEnv = parseEnv;

exports.Event = StdEvent;
exports.EventTarget = StdEventTarget;
exports.Accessor = StdAccessor;
exports.Storage = StdStorage;

exports.alert = alert;
exports.confirm = confirm;
exports.prompt = prompt;

exports.VERSIONINFO = "WelsonJS Standard Library (std.js) version 0.8.17";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
