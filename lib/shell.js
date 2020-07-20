////////////////////////////////////////////////////////////////////////
// Shell API
////////////////////////////////////////////////////////////////////////

var FILE = require('lib/file');

exports.VERSIONINFO = "Shell Module (shell.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.exec = function(cmd, stdOutPath) {
    var WSH = CreateObject("WScript.Shell"),
        data;

    if (typeof(stdOutPath) == "undefined") {
        stdOutPath = "stdout.txt";
    }
    var c = "%comspec% /c (" + cmd + ") 1> " + stdOutPath;
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
    var c = "%comspec% /q /c " + cmd;
    WSH.Run(cmd, 0, !fork);
};
