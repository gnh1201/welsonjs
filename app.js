//////////////////////////////////////////////////////////////////////////////////
//
//    app.js
//
//    Bootstrap code for running a javascript app in windows.  Run as:
//
//    cscript.js app.js <appname> <app arguments> ...
//
/////////////////////////////////////////////////////////////////////////////////
//"use strict";

/////////////////////////////////////////////////////////////////////////////////
// Bootstrap code, basic module loading functionality
/////////////////////////////////////////////////////////////////////////////////

//
//    The module loaded is run inside a function, with one argument, global which
//    points to the global context.  So global.FN is the same as FN (as long as a
//    version of FN does not exist in local scope).
//
//    The module should return its interface at the end of the script.  The basic
//    pattern for a module is:-
//
//    var module = { ... };
//    return module;
//
//    Or:-
//
//    return function() {
//    }
//
//    The appname argument causes <appname>.js to be loaded. The interface returned
//    must define main = function(args) {}, which is called once the module is
//    loaded.

var exit = function(status) {
    console.error("Exit caused by status " + status);

    if (typeof(WScript) !== "undefined") {
        WScript.quit(status);
    } else if (typeof(window) !== "undefined") {
        window.close();
    }
};

var console = {
    _timers: {},
    _counters: {},
    _messages: [],
    _join: function(args, sep) {
        args = args || [];
        sep = sep || ' ';
        var res = '';
        for (var i = args.length - 1; i > -1; i--) {
            res = (i ? sep : '') + args[i] + res;
        }
        return res;
    },
    _echoCallback: null,
    _echo: function(args, type) {
        msg = (typeof(type) !== "undefined" ? type + ": " : "") + this._join(args);
        if (typeof(WScript) !== "undefined") {
            WScript.echo("  * " + (type == "log" ? msg : "[" + type + "] " + msg));
        }
        this._messages.push(msg);

        // after calling echo
        if (['error', 'info', 'warn'].indexOf(type) > -1 && typeof this._echoCallback === "function") {
            this._echoCallback(this);
        }
    },
    assert: function(assertion) {
        if (arguments.length > 1 && assertion === arguments[0]) {
            if(!assertion) {
                this.error("Assertion failed: " + this._join(arguments.slice(1)));
            }
        }
    },
    clear: function() {
        this._messages = [];
    },
    log: function() {
        this._echo(arguments);
    },
    error: function() {
        this._echo(arguments, "error");
    },
    info: function() {
        this._echo(arguments, "info");
    },
    warn: function() {
        this._echo(arguments, "warn");
    },
    debug: function() {
        this._echo(arguments, "debug");
    },
    time: function(label) {
        label = label || "default";
        if (!(label in this._timers)) {
            this._timers[label] = new Date();
        }
    },
    timeLog: function(label, end) {
        label = label || "default";
        if (label in this._timers) {
            console.debug(label + ":", ((new Date()).getTime() - this._timers[label].getTime()) + "ms", (end ? " - timer ended" : "")); 
        }
    },
    timeEnd: function(label) {
        label = label || "default";
        if (label in this._timers) {
            this.timeLog();
            delete this._timers[label];
        }
    },
    count: function(label) {
        label = label || "default";
        if (!(label in this._counters)) {
            this._counters[label] = 1;
        }
    },
    countReset: function(label) {
        label = label || "default";
        if (label in this._counters) {
            this.timeLog();
            delete this._counters[label];
        }
    }
};

if (typeof(CreateObject) === "undefined") {
    var CreateObject = function(progId, serverName, callback) {
        var progIds = (progId instanceof Array ? progId : [progId]);
        var _CreateObject = function(p, s) {
            if (typeof(WScript) !== "undefined") {
                return WScript.CreateObject(p, s);
            } else {
                return new ActiveXObject(p);
            }
        };

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
}

/**
 * @FN {string} The name of the file.
 */
function include(FN) {
    if (FN.substr(FN.length - 3) !== '.js') FN += ".js";
    return eval(require.__load__(FN));
}

/**
 * @FN {string} The name of the file.
 */
function require(FN) {
    var cache = require.__cache__ = require.__cache__ || {};

    if (FN.substr(FN.length - 3) !== '.js') FN += ".js";
    if (cache[FN]) return cache[FN];
    
    // get file and directory name
    var __filename__ = require.__getCurrentScriptDirectory__() + "\\" + FN;
    var __dirname__ = require.__getDirName__(__filename__);
    var T = require.__load__(FN);

    // build
    T = "(function(global){var module=new require.__ModuleObject__();return(function(exports,require,module,__filename,__dirname){"
        + '"use strict";'
        + T
        + "\n\nreturn module.exports})(module.exports,global.require,module,__filename__,__dirname__)})(require.__global__);\n\n////@ sourceURL="
        + FN
    ;

    // execute
    try {
        cache[FN] = eval(T);
    } catch (e) {
        console.error("PARSE ERROR!", e.number + ",", e.description + ",", "FN=" + FN);
    }

    // print VERSIONINFO
    if (typeof(cache[FN]) === "object") {
        if ("VERSIONINFO" in cache[FN]) console.log(cache[FN].VERSIONINFO);
    }

    return cache[FN];
}
require.__global__ = this;
require.__ModuleObject__ = function() {
    this.exports = {};
};
require.__getDirName__ = function(path) {
    var delimiter = "\\";
    var pos = path.lastIndexOf(delimiter);
    return (pos > -1 ? path.substring(0, pos) : "");
};
require.__getCurrentScriptDirectory__ = function() {
    if (typeof(WScript) !== "undefined") {
        return require.__getDirName__(WScript.ScriptFullName);
    } else if (typeof(document) !== "undefined") {
        return require.__getDirName__(document.location.pathname);
    } else {
        return ".";
    }
};
require.__load__ = function(FN) {
    // get filename
    var __filename__ = require.__getCurrentScriptDirectory__() + "\\" + FN;

    // load script file
    // use ADODB.Stream instead of Scripting.FileSystemObject, because of UTF-8 (unicode)
    var objStream = CreateObject("ADODB.Stream");
    var T = null;
    try {
        objStream.charSet = "utf-8";
        objStream.open();
        objStream.loadFromFile(__filename__);
        T = objStream.readText();
        objStream.close();
    } catch (e) {
        console.error("LOAD ERROR!", e.number + ",", e.description + ",", "FN=" + FN);
        return;
    }

    return T;
};

/////////////////////////////////////////////////////////////////////////////////
// Load script, and call app.main()
/////////////////////////////////////////////////////////////////////////////////

function initializeConsole() {
    if (typeof(WScript) === "undefined") {
        console.error("Error, WScript is not defined");
        exit(1);
    }

    var argl = WScript.arguments.length;
    if (argl > 0) {
        var args = [];
        for (var i = 0; i < argl; i++) {
            args.push(WScript.arguments(i));
        }
        var name = args.shift();
        var app = require(name);
        if (app) {
            if (app.main) {
                var exitStatus = app.main.call(this, args);
                if (typeof(exitStatus) !== "undefined") {
                    exit(exitStatus);
                }
            } else {
                console.error("Error, missing main entry point in", name + ".js");
            }
        } else {
            console.error("Error, cannot find", name + ".js");
        }
    }
}

function initializeWindow(name, args, w, h) {
    if (typeof(window) === "undefined") {
        console.error("Error, window is not defined");
        exit(1);
    }
    var app = require(name);

    // "set default size of window";
    if (typeof(w) !== "undefined" && typeof(h) !== "undefined") {
        window.resizeTo(w, h);
    }

    // "load app";
    if (app) {
        if (app.main) {
            var exitStatus = app.main.call(app, args);
            if (exitStatus > 0) {
                exit(exitStatus);
            }
        } else {
            console.error("Error, missing main entry point in", name + ".js");
            exit(1);
        }
    } else {
        console.error("Error, cannot find", name + ".js");
        exit(1);
    }
}

// JSON 2
include("app/assets/js/json2");

// JSON 3 was a JSON polyfill for older JavaScript platforms
//var JSON = require("app/assets/js/json3-3.3.2.min");

// core-js (formerly babel-polyfill)
require("app/assets/js/corejs-build-20210810");

// es5-shims
//require("app/assets/js/es5-shim-4.5.15.min");
//require("app/assets/js/es5-sham-4.5.15.min");

// Squel.js SQL query string builder for Javascript
var squel = require("app/assets/js/squel-basic-5.13.0.hiddentao-afa1cb5.wsh");

// Dive into entrypoint 
function __main__() {
    if (typeof(window) === "undefined") {
        initializeConsole();
    } else {
        console.log("welcome");
    }
}

__main__();
