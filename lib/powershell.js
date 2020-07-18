var SHELL = require("lib/shell");

var scope = {
    VERSIONINFO: "Powershell (powershell.js) version 0.1",
    global: global,
    require: global.require
};

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

return scope;
