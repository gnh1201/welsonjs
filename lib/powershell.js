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
    var arguments = [];

    arguments.push("powershell.exe");
    arguments.push("-NoProfile");
    arguments.push("-ExecutionPolicy");
    arguments.push("ByPass");
    arguments.push("-nologo")
    arguments.push("-file");
    arguments.push(scriptName + ".ps1");

    if(typeof(args) !== "undefined") {
        for(var i in args) {
            arguments.push(args[i]);
        }
    }

    return SHELL.exec(arguments.join(' '));
};

exports.execCommand = function(command) {
    var arguments = [];

    arguments.push("powershell.exe");
    arguments.push("-NoProfile");
    arguments.push("-ExecutionPolicy");
    arguments.push("ByPass");
    arguments.push("-nologo")
    arguments.push("-Command");
    arguments.push("\"& {");
    arguments.push(exports.addslashes(command));
    arguments.push("}\"");

    return SHELL.exec(arguments.join(' '));
};
