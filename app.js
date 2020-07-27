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
    if (typeof(WScript) !== "undefined") {
        WScript.quit(status);
    }
    console.warn("Exit caused by: " + status);
};

var console = {
    __messages: [],
    __echo: function(msg) {
        if (typeof(WScript) !== "undefined") {
            WScript.echo(msg);
        }
        this.__messages.push(msg);
    },
    log: function(msg) {
        this.__echo(msg);
    },
    error: function(msg) {
        var msg = "[ERROR] " + msg;
        this.__echo(msg);
    },
    info: function(msg) {
        var msg = "[INFO] " + msg;
        this.__echo(msg);
    },
    warn: function(msg) {
        var msg = "[WARN] " + msg;
        this.__echo(msg);
    },
    debug: function(msg) {
        var msg = "[DEBUG] " + msg;
        this.__echo(msg);
    }
};

if (typeof(CreateObject) !== "function") {
    var CreateObject = function(className) {
        return new ActiveXObject(className);
    };
}

if (typeof(GetObject) !== "function") {
    var GetObject = function(pathName, className) {
        var paths = pathName.split("\\");
        if (paths[0].indexOf("winmgmts:") > -1) {
            var objLocator = CreateObject("WbemScripting.SWbemLocator");
            var strComputer = paths[2];
            var strNamespace = paths.slice(3).join("\\");
            return objLocator.ConnectServer(strComputer, strNamespace);
        } else {
            console.log("Not supported: " + pathName);
        }
    };
}

function require(FN) {
    var cache = require.__cache = require.__cache || {};
    if (FN.substr(FN.length - 3) !== '.js') FN += ".js";
    if (cache[FN]) return cache[FN];

    // get directory name
    var getDirName = function(path) {
        var pos = path.lastIndexOf("\\");
        return path.substring(0, pos);
    };

    // get current script directory
    var getCurrentScriptDirectory = function() {
        if (typeof(WScript) !== "undefined") {
            var path = WScript.ScriptFullName;
            return getDirName(path);
        } else {
            return ".";
        }
    };

    // get file and directory name
    var __filename = getCurrentScriptDirectory() + "\\" + FN;
    var __dirname = getDirName(__filename);

    // load script file
    var FSO = CreateObject("Scripting.FileSystemObject");
    var T = null;
    try {
        TS = FSO.OpenTextFile(__filename, 1);
        if (TS.AtEndOfStream) return "";
        T = TS.ReadAll();
        TS.Close();
        TS = null;
    } catch (e) {
        console.error("LOAD ERROR! " + e.number + ", " + e.description + ", FN=" + FN, 1);
        return;
    }

    // make global function
    FSO = null;
    T = "(function(global){var module={exports:{}};return(function(exports,require,module,__filename,__dirname){" + '"use strict";' + T + "\nreturn exports})(module.exports,global.require,module,__filename,__dirname)})(this);\n\n////@ sourceURL=" + FN;
    try {
        cache[FN] = eval(T);
    } catch (e) {
        console.error("PARSE ERROR! " + e.number + ", " + e.description + ", FN=" + FN, 1);
    }

    // check type of callback return
    if (typeof(cache[FN]) === "object") {
        if ("VERSIONINFO" in cache[FN]) console.log(cache[FN].VERSIONINFO);
    }

    return cache[FN];
}

/////////////////////////////////////////////////////////////////////////////////
// get configuration variables
/////////////////////////////////////////////////////////////////////////////////

var __config = require("config").config;

/////////////////////////////////////////////////////////////////////////////////
// Load script, and call app.main()
/////////////////////////////////////////////////////////////////////////////////

function init_console() {
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
                var exitstatus = app.main.call(this, args);
                if (typeof(exitstatus) !== "undefined") {
                    exit(exitstatus);
                }
            } else {
                console.error("Error, missing main entry point in " + name + ".js", 1);
            }
        } else {
            console.error("Error, cannot find " + name + ".js", 1);
        }
    }
}

function init_window(name, args, w, h) {
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
            var exitstatus = app.main.call(app, args);
            if (exitstatus > 0) {
                console.error("error");
                exit(exitstatus);
            }
        } else {
            console.error("Error, missing main entry point in " + name + ".js");
            exit(1);
        }
    } else {
        console.error("Error, cannot find " + name + ".js");
        exit(1);
    }
}

function main() {
    if (typeof(window) === "undefined") {
        init_console();
    } else {
        console.log("welcome");
    }
}

main();
