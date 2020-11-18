////////////////////////////////////////////////////////////////////////
// Shell API
////////////////////////////////////////////////////////////////////////
var FILE = require("lib/file");

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

var ShellObject = function() {
    this.interface = null;
    this.currentDirectory = null;
    this.workingDirectory = null;
    this.isElevated = false;
    this.isFork = false;

    this.create = function() {
        try {
            this.interface = CreateObject("WScript.Shell");
            this.currentDirectory = this.interface.CurrentDirectory;
        } catch (e) {
            console.error("ShellObject.create() -> " + e.message);
        }
        return this;
    };

    this.setWorkingDirectory = function(dirname) {
        if (typeof(dirname) === "string") {
            this.workingDirectory = dirname;
            this.interface.CurrentDirectory = this.workingDirectory;
            console.info("ShellObject.workingDirectory -> " + this.workingDirectory);
        }
        return this;
    };

    this.build = function(cmd) {
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

    this.createProcess = function(cmd) {
        try {
            var c = this.build(cmd);
            console.info("ShellObject.createProcess() -> " + c);
            return this.interface.Exec(c);
        } catch (e) {
            console.error("ShellObject.createProcess() -> " + e.message);
        }
    };

    this.exec = function(cmd, stdOutPath) {
        var data;

        if (typeof(stdOutPath) === "undefined") {
            stdOutPath = "stdout.txt";
        }
        var c = "%comspec% /c (" + this.build(cmd) + ") 1> " + stdOutPath;
        c += " 2>&1";
        this.interface.Run(c, 0, true);
        console.info("ShellObject.exec() -> " + c);
        data = FILE.readFile(stdOutPath, "utf-8");
    
        if (FILE.fileExists(stdOutPath)) {
            FILE.deleteFile(stdOutPath);
        }

        return data;
    };

    this.run = function(cmd, fork) {
        var fork = (typeof(fork) !== "undefined") ? fork : true;
        var c = "%comspec% /q /c (" + this.build(cmd) + ")";
        console.info("ShellObject.run() -> " + c);
        this.interface.Run(c, 0, !fork);
    };

    this.runWindow = function(cmd, fork) {
        var fork = (typeof(fork) !== "undefined") ? fork : true;
        var c = "%comspec% /q /c (" + this.build(cmd) + ")";
        console.info("ShellObject.runWindow() -> " + c);
        this.interface.Run(c, 1, !fork);
    };

    this.runAs = function(FN, args) {
        var c = FN + " " + args.join(' ');
        var oShell = CreateObject("Shell.Application");
        console.info("ShellObject.runAs() -> " + c);
        oShell.shellExecute(FN, args, null, "runas", 0);
        return oShell;
    };

    this.release = function() {
        console.info("ShellObject.release() -> " + this.currentDirectory);
        this.interface.CurrentDirectory = this.currentDirectory;
        this.interface = null;
    };

    this.create();
};

exports.create = function() {
    return new ShellObject();
};

exports.build = function(cmd) {
    return (new ShellObject()).build(cmd);
};

exports.exec = function(cmd, stdOutPath) {
    return (new ShellObject()).exec(cmd, stdOutPath);
};

exports.run = function(cmd, fork) {
    return (new ShellObject()).run(cmd, fork);
};

exports.runWindow = function(cmd, fork) {
    return (new ShellObject()).runWindow(cmd, fork);
}

exports.createProcess = function(cmd, workingDirectory) {
    if (typeof(workingDirectory) !== "undefined") {
        console.info("Working directory: " + workingDirectory);
    }
    return (new ShellObject()).setWorkingDirectory(workingDirectory).createProcess(cmd);
};

exports.runAs = function(FN, args) {
    return (new ShellObject()).runAs(FN, args);
};

exports.VERSIONINFO = "Shell Lib (shell.js) version 0.1";
exports.global = global;
exports.require = global.require;
