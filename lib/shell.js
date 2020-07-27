////////////////////////////////////////////////////////////////////////
// Shell API
////////////////////////////////////////////////////////////////////////

var FILE = require("lib/file");

exports.VERSIONINFO = "Shell Lib (shell.js) version 0.1";
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

exports.makeCmdLine = function(cmd) {
    if (typeof(cmd) === "string") {
        return cmd;
    } else if (typeof(cmd) === "object") {
        return cmd.map(function(s) {
            return "\"" + exports.addslashes(s) + "\"";
        }).join(' ');
    } else {
        return "";
    }
};

exports.exec = function(cmd, stdOutPath) {
    var WSH = CreateObject("WScript.Shell"), data;
    if (typeof(stdOutPath) === "undefined") {
        stdOutPath = "stdout.txt";
    }
    var c = "%comspec% /c (" + exports.makeCmdLine(cmd) + ") 1> " + stdOutPath;
    c += " 2>&1";
    WSH.Run(c, 0, true);
    data = FILE.readFile(stdOutPath, "utf-8");

    if (FILE.fileExists(stdOutPath)) {
        FILE.deleteFile(stdOutPath);
    }

    return data;
}

exports.run = function(cmd, fork) {
    var WSH = CreateObject("WScript.Shell");
    var fork = (typeof(fork) !== "undefined") ? fork : true;
    var c = "%comspec% /q /c " + exports.makeCmdLine(cmd);
    WSH.Run(c, 0, !fork);
};
