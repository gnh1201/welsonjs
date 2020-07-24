////////////////////////////////////////////////////////////////////////
// AutoIt3 API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "AutoIt3 (autoit3.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.execScript = function(scriptName, args) {
    var commandOptions = [];

    commandOptions.push("\"%PROGRAMFILES(X86)%\\AutoIt3\\AutoIt3.exe\"");
    commandOptions.push(scriptName + ".au3");

    if(typeof(args) !== "undefined") {
        for(var i in args) {
            commandOptions.push(args[i]);
        }
    }

    return SHELL.exec(commandOptions.join(' '));
};
