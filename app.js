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
//
//    app.js
//    Namhyeon Go <abuse@catswords.net>
//    https://github.com/gnh1201/welsonjs
//    If you find an abuse case or a security issue, please feel free to contact me.
//

var exit = function(status) {
    console.error("Exit", status, "caused");

    if (typeof WScript !== "undefined") {
        WScript.Quit(status);
    } else if (typeof window !== "undefined") {
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
    _wshEcho: function(message) {
        if (typeof WScript !== "undefined") {
            WScript.Echo("[*] " + message)
        }
    },
    _echo: function(args, type) {
        var message = "";
        var params = {
            type: type,
            channel: 'default',
            message: '',
            datetime: new Date().toISOString()
        };

        if (args.length > 0) {
            if (typeof args[0] === "string") {
                // if not type is "log", then "{type}: {message}"
                if (typeof type !== "undefined") {
                    message += (type + ": " + this._join(args));
                } else {
                    message += this._join(args);
                }
                this._wshEcho(message);
                this._messages.push(message);
                params.message = message;
            } else if (typeof args[0] === "object") {
                if ('message' in args[0]) {
                    if (typeof type !== "undefined") {
                        message += (type + ": " + args[0].message);
                    } else {
                        message += args[0].message;
                    }
                }
                this._wshEcho(message);
                this._messages.push(args[0].message);
                for (var k in args[0]) {
                    params[k] = args[0][k];
                }
            }
        }

        if (params.channel != "default" && this._echoCallback != null) {
            try {
                this._echoCallback(params, type);
            } catch (e) {
                this._wshEcho("Exception on _echoCallback: " + e.message);
            }
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

if (typeof CreateObject === "undefined") {
    var CreateObject = function(progId, serverName, callback) {
        var progIds = (progId instanceof Array ? progId : [progId]);

        for (var i = 0; i < progIds.length; i++) {
            try {
                var obj = CreateObject.make(progIds[i], serverName);
                if (typeof callback === "function") {
                    callback(obj, progIds[i]);
                }
                return obj;
            } catch (e) {
                console.error(e.message);
            };
        }
    };
    CreateObject.make = function(p, s) {
        if (typeof WScript !== "undefined") {
            if ("CreateObject" in WScript) {
                return WScript.CreateObject(p, s);
            } else {
                console.warn("(Chakra) The standalone engine does not supported. Please use the built-in engine.");
                console.warn("(Chakra) hint:", "cscript //NoLogo //E:{1b7cd997-e5ff-4932-a7a6-2a9e636da385} app.js <filename> <...arguments>");
            }
        } else if (typeof ActiveXObject !== "undefined") {
            return new ActiveXObject(p);
        }
    };
}

/**
 * @FN {string} The name of the file.
 */
function __include__(FN) {
    if (FN.substr(FN.length - 3) !== '.js') FN += ".js";
    return eval(require.__load__(FN));
}

/**
 * @FN {string} The name of the file.
 */
function require(pathname) {
    var cache = require.__cache__ = require.__cache__ || {};
    var suffix = (function(pos, s) {
        return pos < 0 ? '.' : s.substr(pos);
    })(pathname.lastIndexOf('.'), pathname);
    var FN = pathname;

    if ('.js$.jse$.coffee$.ls$.ts$.re$.res$.enc$'.indexOf(suffix + '$') < 0) FN += ".js";
    if (cache[FN]) return cache[FN];

    // get file and directory name
    var __filename__ = (function(fileExists, path) {
        var filepaths = [
            FN,    // default
            path.join(pathname, "index.js"),    // default
            path.join(FN + '.enc'),    // default (encrypted)
            path.join(pathname, 'index.js.enc'),    // default (encrypted)
            path.join("Scripts", FN),    // NuGet
            path.join("Scripts", pathname, "index.js"),    // NuGet
            path.join("bower_components", FN),    // Bower
            path.join("bower_components", pathname, "index.js"),    // Bower
            path.join("node_modules", FN),    // NPM
            path.join("node_modules", pathname, "index.js"),    // NPM
        ];
        var filename = filepaths[0];

        if (!fileExists(filename)) {
            for (var i = 1; i < filepaths.length; i++) {
                if (fileExists(filepaths[i])) {
                    filename = filepaths[i];
                    break;
                }
            }
        }

        return filename;
    })(function(filename) {
        return CreateObject("Scripting.FileSystemObject").FileExists(filename);
    }, {
        join: function() {
            var result = arguments[0];
            for (var i = 1; i < arguments.length; i++) {
                result += "\\" + arguments[i];
            }
            return result;
        }
    });
    var __dirname__ = (function(dirname) { 
        var currentScriptDirectory = require.__getCurrentScriptDirectory__();
        return dirname.length > 0 ? currentScriptDirectory + "\\" + dirname : currentScriptDirectory;
    })(require.__getDirName__(__filename__));
    var T = require.__load__(__filename__);

    // check the suffix again
    suffix = (function(pos, s) {
        return pos < 0 ? '.' : s.substr(pos);
    })(__filename__.lastIndexOf('.'), __filename__);

    // transpile
    switch (suffix) {
        case '.coffee':  // CoffeeScript 2
            T = require.__msie9__("app/assets/js/coffeescript-legacy-2.7.0.min", [T], function(p, w, d, l) {
                return w.CoffeeScript.compile(p[0], {
                    "header": true,
                    "sourceMap": false,
                    "bare": true
                });
            });
            break;

        case ".ls":  // LiveScript
            T = require.__msie9__("app/assets/js/livescript-1.6.1.min", [T, "app/assets/ls/prelude.ls"], function(p, w, d, l) {
                return w.require("livescript").compile(require.__load__(p[1]) + "\n\n" + p[0], {
                    "header": true,
                    "bare": true
                });
            });
            break;

        case ".ts":  // TypeScript
            T = require.__modernie__("app/assets/js/typescript-4.9.4", [T], function(p, w, d, l) {
                return w.ts.transpile(p[0]);
            });
            break;

        case ".re":  // Rescript (aka. BuckleScript, ReasonML)
        case ".res":
            T = require.__modernie__("app/assets/js/rescript-compiler-10.1.2", [T], function(p, w, d, l) {
                var compiler = w.rescript_compiler.make();
                var result = compiler.rescript.compile(p[0]);
                return result.js_code;
            });
            break;

        case ".enc":   // HIGHT(ISO/IEC 18033-3) encrypted
            T = (function(encryptedData, ToolkitInterface) {
                try {
                    var userKey = '', t = 0, k = 6;
                    while (t < k && (userKey.length == 0 || userKey.length > 16)) {
                        if (t > 0) {
                            console.error("Invalid key length");
                        }
                        userKey = ToolkitInterface.Prompt("This file has been encrypted. Please enter the password:");
                        t++;
                    }
                    if (t == k) return '';
                    return ToolkitInterface.DecryptStringHIGHT(userKey, encryptedData);
                } catch (e) {
                    console.error("Failed to load the encrypted data:", e.message);
                    return '';
                }
            })(T, CreateObject("WelsonJS.Toolkit"));
            break;
    }

    // compile
    T = "(function(global){var module=new require.__ModulePrototype__();return(function(exports,require,module,__filename,__dirname){"
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

    // print VERSIONINFO and AUTHOR
    if (typeof cache[FN] === "object") {
        if ("VERSIONINFO" in cache[FN]) {
            if ("AUTHOR" in cache[FN]) {
                console.log(cache[FN].VERSIONINFO + " by " + cache[FN].AUTHOR);
            } else {
                console.log(cache[FN].VERSIONINFO);
            }
        }
    }

    return cache[FN];
}
require.__global__ = this;
require.__ModulePrototype__ = function() {
    this.exports = {};
};
require.__getDirName__ = function(path) {
    var pos = Math.max.apply(null, [path.lastIndexOf("\\"), path.lastIndexOf("/")]);
    return (pos > -1 ? path.substring(0, pos) : "");
};
require.__getCurrentScriptDirectory__ = function() {
    if (typeof WScript !== "undefined") {
        if ("ScriptFullName" in WScript) {
            return require.__getDirName__(WScript.ScriptFullName);
        } else {
            console.warn("Could not resolve an absolute path. Use the relative path.");
            return ".";
        }
    } else if (typeof document !== "undefined") {
        return require.__getDirName__(document.location.pathname);
    } else {
        console.warn("Could not resolve an absolute path. Use the relative path.");
        return ".";
    }
};
require.__load__ = function(FN) {
    // if empty
    if (FN == '') return '';

    // get filename
    var __filename__ = require.__getCurrentScriptDirectory__() + "\\" + FN;

    // load script file
    // use ADODB.Stream instead of Scripting.FileSystemObject, because of supporting UTF-8 (Unicode)
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
require.__msie9__ = function(FN, params, callback) {
    if (typeof FN !== "string" || FN == null) FN = '';
    else if (FN.substr(FN.length - 3) !== '.js') FN += ".js";

    var exports = null;
    try {
        var T = require.__load__("app/assets/js/core-js-3.26.1.minified.js")
            + "\n\n" + require.__load__("app/assets/js/html5shiv-printshiv-3.7.3.min.js")
            + "\n\n" + require.__load__("app/assets/js/modernizr-2.8.3.min.js")
            + "\n\n" + require.__load__(FN);
        var htmlfile = CreateObject("htmlfile");
        htmlfile.write('<meta http-equiv="X-UA-Compatible" content="IE=9">');
        htmlfile.write('<script type="text/javascript">//<!--<![CDATA[\n' + T + '\n//]]>--></script>');
        if (typeof callback === "function") {
            var loadScript = function(FN) {
                if (FN.indexOf('://') > -1) {
                    htmlfile.write('<script type="text/javascript" src="' + FN + '"></script>');
                } else {
                    htmlfile.write('<script type="text/javascript">//<!--<![CDATA[\n' + require.__load__(FN) + '\n//]]>--></script>');
                }
            };
            //console.log(htmlfile.parentWindow.navigator.userAgent);
            exports = callback(params, htmlfile.parentWindow, htmlfile.parentWindow.document, loadScript);
        }
        htmlfile.close();
    } catch (e) {
        console.error("LOAD ERROR!", e.number + ",", e.description + ",", "FN=" + FN);
    }

    return exports;
};
require.__modernie__ = function(FN, params, callback) {
    if (typeof FN !== "string" || FN == null) FN = '';
    else if (FN.substr(FN.length - 3) !== '.js') FN += ".js";

    var exports = null;
    try {
        var ua = '', T = '', htmlfile = CreateObject("htmlfile");

        htmlfile.write('<meta http-equiv="X-UA-Compatible" content="IE=edge">');
        htmlfile.write('<script type="text/javascript">//<!--<![CDATA[\n\nfunction __getUserAgent__(){return window.navigator.userAgent}\n\n//]]>--></script>');
        ua = htmlfile.parentWindow.__getUserAgent__();

        if (ua.indexOf('Trident/ ')) {
            T = require.__load__("app/assets/js/core-js-3.26.1.minified.js")
                + "\n\n" + require.__load__("app/assets/js/modernizr-2.8.3.min.js")
                + "\n\n" + require.__load__("app/assets/js/babel-standalone-7.20.6.min.js")
                + "\n\n" + require.__load__(FN);
        } else {
            T = require.__load__("app/assets/js/core-js-3.26.1.minified.js")
                + "\n\n" + require.__load__("app/assets/js/html5shiv-printshiv-3.7.3.min.js")
                + "\n\n" + require.__load__("app/assets/js/modernizr-2.8.3.min.js")
                + "\n\n" + require.__load__(FN);
        }
        htmlfile.write('<script type="text/javascript">//<!--<![CDATA[\n' + T + '\n//]]>--></script>');

        if (typeof callback === "function") {
            var loadScript = function(src) {
                if (src.indexOf('://') > -1) {
                    htmlfile.write('<script type="text/javascript" src="' + src + '"></script>');
                } else {
                    htmlfile.write('<script type="text/javascript">//<!--<![CDATA[\n' + require.__load__(src) + '\n//]]>--></script>');
                }
            };
            //console.log(htmlfile.parentWindow.navigator.userAgent);
            exports = callback(params, htmlfile.parentWindow, htmlfile.parentWindow.document, loadScript);
        }
        htmlfile.close();
    } catch (e) {
        console.error("LOAD ERROR!", e.number + ",", e.description + ",", "FN=" + FN);
    }

    return exports;
};

/////////////////////////////////////////////////////////////////////////////////
// Load script, and call app.main()
/////////////////////////////////////////////////////////////////////////////////

function initializeConsole() {
    if (typeof WScript === "undefined") {
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
                if (typeof exitStatus !== "undefined") {
                    exit(exitStatus);
                }
            } else {
                console.error("Error, missing main entry point in", name);
            }
        } else {
            console.error("Error, cannot find", name);
        }
    }
}

function initializeWindow(name, args, w, h) {
    if (typeof window === "undefined") {
        console.error("Error, window is not defined");
        exit(1);
    }
    var app = require(name);

    // "set default size of window";
    if (typeof w !== "undefined" && typeof h !== "undefined") {
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
if (typeof JSON === "undefined") {
    __include__("app/assets/js/json2");
}

// core-js (aka. babel-polyfill)
require("app/assets/js/core-js-3.26.1.minified");

// Squel.js SQL query string builder for Javascript
var squel = require("app/assets/js/squel-basic-5.13.0.hiddentao-afa1cb5.wsh");

// JavaScript YAML parser and dumper. 
var yaml = require("app/assets/js/js-yaml-4.1.0.wsh");

// is.js Micro check library
var is = require("app/assets/js/is-0.9.0.min");

// Intl (ECMA-402) polyfill
//var Intl = require("app/assets/js/Intl-1.2.5-e93b114.min");
//console.log(new Intl.NumberFormat().format(1234567890.123456));

// Dive into entrypoint 
function __main__() {
    console.log("");
    console.log(" __        __   _                     _ ____  ");
    console.log(" \\ \\      / /__| |___  ___  _ __     | / ___| ");
    console.log("  \\ \\ /\\ / / _ \\ / __|/ _ \\| '_ \\ _  | \\___ \\ ");
    console.log("   \\ V  V /  __/ \\__ \\ (_) | | | | |_| |___) |");
    console.log("    \\_/\\_/ \\___|_|___/\\___/|_| |_|\\___/|____/ ");
    console.log("");
    console.log("    WelsonJS - Build a Windows app on the Windows built-in JavaScript engine");
    console.log("    https://github.com/gnh1201/welsonjs");
    console.log("");

    if (typeof window === "undefined") {
        initializeConsole();
    } else {
        console.log("welcome");
    }
}

__main__();
