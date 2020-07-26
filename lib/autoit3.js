////////////////////////////////////////////////////////////////////////
// AutoIt3 API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "AutoIt3 (autoit3.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.execScript = function(scriptName, args) {
    var cmd = [
        "%PROGRAMFILES(X86)%\\AutoIt3\\AutoIt3.exe",
        scriptName + ".au3"
    ];

    if (typeof(args) !== "undefined") {
        cmd = cmd.concat(args);
    }

    return SHELL.exec(cmd);
};
