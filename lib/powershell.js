var SHELL = require("lib/shell");

var scope = {
    VERSIONINFO: "Powershell (powershell.js) version 0.1",
    global: global,
    require: global.require
};

scope.addslashes = function(string) {
    return string.replace(/\\/g, '\\\\').
        replace(/\u0008/g, '\\b').
        replace(/\t/g, '\\t').
        replace(/\n/g, '\\n').
        replace(/\f/g, '\\f').
        replace(/\r/g, '\\r').
        replace(/'/g, '\\\'').
        replace(/"/g, '\\"');
}

scope.execScript = function(scriptName, args) {
    var arguments = [
        "powershell.exe",
        "-NoProfile",
        "-ExecutionPolicy",
        "ByPass",
        "-nologo",
        "-file",
        scriptName + ".ps1"
    ];

    if(typeof(args) !== "undefined") {
        for(var i in args) {
            arguments.push(args[i]);
        }
    }

    return SHELL.exec(arguments.join(' '));
};

scope.execCommand = function(command) {
    var arguments = [
        "powershell.exe",
        "-NoProfile",
        "-ExecutionPolicy",
        "ByPass",
        "-nologo"
        "-Command",
        "\"& {",
        scope.addslashes(command),
        "}\""
    ];

    return SHELL.exec(arguments.join(' '));
};

return scope;
