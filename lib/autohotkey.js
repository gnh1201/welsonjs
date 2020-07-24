////////////////////////////////////////////////////////////////////////
// AutoHotKey API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "AutoHotKey (autohotkey.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.execScript = function(scriptName, args) {
    var commandOptions = [];

    commandOptions.push("\"%PROGRAMFILES%\\AutoHotkey\\AutoHotkey.exe\"");
    commandOptions.push(scriptName + ".ahk");

    if(typeof(args) !== "undefined") {
        for(var i in args) {
            commandOptions.push(args[i]);
        }
    }

    return SHELL.exec(commandOptions.join(' '));
};
