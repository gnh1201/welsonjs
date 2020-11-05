////////////////////////////////////////////////////////////////////////
// Shell API
////////////////////////////////////////////////////////////////////////

var FILE = require("lib/file");

exports.VERSIONINFO = "Shell Lib (shell.js) version 0.1";
exports.global = global;
exports.require = global.require;

var addslashes = function(s) {
    return s.toString().replace(/\\/g, '\\\\').
        replace(/\u0008/g, '\\b').
        replace(/\t/g, '\\t').
        replace(/\n/g, '\\n').
        replace(/\f/g, '\\f').
        replace(/\r/g, '\\r').
        replace(/'/g, '\\\'').
        replace(/"/g, '\\"');
};

var makeCommand = function(cmd) {
    if (typeof(cmd) === "string") {
        return cmd;
    } else if (typeof(cmd) === "object") {
        return cmd.map(function(s) {
            var regex = /[ "]/g;
            if (!regex.test(s)) {
                return s;
            } else {
                return "\"" + addslashes(s) + "\"";
            }
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
    var c = "%comspec% /c (" + makeCommand(cmd) + ") 1> " + stdOutPath;
    c += " 2>&1";
    WSH.Run(c, 0, true);
    console.info("exec() -> " + c);
    data = FILE.readFile(stdOutPath, "utf-8");

    if (FILE.fileExists(stdOutPath)) {
        FILE.deleteFile(stdOutPath);
    }

    return data;
}

exports.run = function(cmd, fork) {
    var WSH = CreateObject("WScript.Shell");
    var fork = (typeof(fork) !== "undefined") ? fork : true;
    var c = "%comspec% /q /c (" + makeCommand(cmd) + ")";
    console.info("run() -> " + c);
    WSH.Run(c, 0, !fork);
};

exports.runWindow = function(cmd, fork) {
    var WSH = CreateObject("WScript.Shell");
    var fork = (typeof(fork) !== "undefined") ? fork : true;
    var c = "%comspec% /q /c (" + makeCommand(cmd) + ")";
    console.info("run() -> " + c);
    WSH.Run(c, 1, !fork);
}

exports.createProcess = function(cmd) {
    var WSH = CreateObject("WScript.Shell");
    return WSH.Exec(makeCommand(cmd));
};

exports.elevatedRun = function(FN, args) {
	console.info("elevatedRun() -> " + FN + " " + args.join(' '));
    var oShell = CreateObject("Shell.Application");
    oShell.shellExecute(FN, args, null, "runas", 0);
    return oShell;
};
