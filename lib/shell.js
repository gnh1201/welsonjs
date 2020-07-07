////////////////////////////////////////////////////////////////////////
// Shell API
////////////////////////////////////////////////////////////////////////
var FILE = require('lib/file');

var scope = {
    VERSIONINFO: "Shell Module (shell.js) version 0.1",
    global: global,
    require: global.require
};

scope.exec = function(cmd, stdOutPath) {
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

scope.run = function(cmd, fork) {
    var WSH = CreateObject("WScript.Shell");
    var fork = (typeof(fork) !== "undefined") ? fork : true;
    var c = "%comspec% /q /c " + cmd;
    WSH.Run(cmd, 0, !fork);
};

return scope;
