////////////////////////////////////////////////////////////////////////
// Powershell API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "Powershell (powershell.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.execScript = function(scriptName, args) {
    var cmd = [
        "powershell.exe",
        "-NoProfile",
        "-ExecutionPolicy",
        "ByPass",
        "-nologo",
        "-file",
        scriptName + ".ps1"
    ];

    if (typeof(cmd) !== "undefined") {
        cmd = cmd.concat(args);
    }

    return SHELL.exec(cmd);
};

exports.execCommand = function(cmd) {
    return SHELL.exec([
        "powershell.exe",
        "-NoProfile",
        "-ExecutionPolicy",
        "ByPass",
        "-nologo",
        "-Command",
        "& {" + cmd + "}"
    ]);
};

exports.runAs = function(cmd) {
    return exports.execCommand("Start-Process cmd \"/q /c " + SHELL.addslashes(SHELL.makeCmdLine(cmd)) + "\" -Verb RunAs");
};
