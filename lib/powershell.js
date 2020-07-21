////////////////////////////////////////////////////////////////////////
// Powershell API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "Powershell (powershell.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.addslashes = function(string) {
    return string.replace(/\\/g, '\\\\').
        replace(/\u0008/g, '\\b').
        replace(/\t/g, '\\t').
        replace(/\n/g, '\\n').
        replace(/\f/g, '\\f').
        replace(/\r/g, '\\r').
        replace(/'/g, '\\\'').
        replace(/"/g, '\\"');
};

exports.execScript = function(scriptName, args) {
    var commandOptions = [];

    commandOptions.push("powershell.exe");
    commandOptions.push("-NoProfile");
    commandOptions.push("-ExecutionPolicy");
    commandOptions.push("ByPass");
    commandOptions.push("-nologo")
    commandOptions.push("-file");
    commandOptions.push(scriptName + ".ps1");

    if(typeof(args) !== "undefined") {
        for(var i in args) {
            commandOptions.push(args[i]);
        }
    }

    return SHELL.exec(commandOptions.join(' '));
};

exports.execCommand = function(command) {
    var commandOptions = [];

    commandOptions.push("powershell.exe");
    commandOptions.push("-NoProfile");
    commandOptions.push("-ExecutionPolicy");
    commandOptions.push("ByPass");
    commandOptions.push("-nologo")
    commandOptions.push("-Command");
    commandOptions.push("\"& {");
    commandOptions.push(exports.addslashes(command));
    commandOptions.push("}\"");

    return SHELL.exec(commandOptions.join(' '));
};
