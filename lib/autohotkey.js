////////////////////////////////////////////////////////////////////////
// AutoHotKey API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "AutoHotKey (autohotkey.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.execScript = function(scriptName, args) {
    var cmd = [
        "%PROGRAMFILES%\\AutoHotkey\\AutoHotkey.exe",
        scriptName + ".ahk"
    ];

    if (typeof(args) !== "undefined") {
        cmd = cmd.concat(args);
    }

    return SHELL.exec(cmd);
};
