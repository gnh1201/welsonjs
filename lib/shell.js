// Windows Shell Interface with WelsonJS Pipe-IPC module
// Namhyeon Go (Catswords Research) <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
var FILE = require("lib/file");
var PipeIPC = require("lib/pipe-ipc");

var ShellObject = function() {
    this._interface = null;

    this.currentDirectory = null;
    this.workingDirectory = null;
    this.isElevated = false;
    this.isFork = false;
    this.visibility = "hidden";
    this.isPreventClear = false;
    this.charset = PipeIPC.CdoUS_ASCII;

    this.stdout = null;
    this.stderr = null;
    this.prefix = null;

    this.create = function() {
        try {
            this._interface = CreateObject("WScript.Shell");
            this.currentDirectory = this._interface.CurrentDirectory;
            this.workingDirectory = this.currentDirectory;
        } catch (e) {
            console.error("ShellObject.create() ->", e.message);
        }
        return this;
    };

    this.setPrefix = function(prefix) {
        this.prefix = prefix;
        return this;
    };

    this.setCharset = function(charset) {
        this.charset = charset;
        return this;
    };
    
    this.setIsPreventClear = function(flag) {
        this.isPreventClear = flag;
        return this;
    };

    this.setWorkingDirectory = function(dirname) {
        if (typeof(dirname) === "string") {
            this.workingDirectory = dirname;
            this._interface.CurrentDirectory = this.workingDirectory;
            console.log("ShellObject.workingDirectory ->", this.workingDirectory);
        }
        return this;
    };
    
    this.setVisibility = function(visibility) {
        this.visibility = visibility;
		return this;
    };
    
    // @deprecated
    this.setVisibleWindow = function(visible) {
        if (visible == false) {
            this.setVisibility("hidden");
        } else {
            this.setVisibility("visible");
        }
        return this;
    };
    
    this.build = function(cmd) {
        var prefix = this.prefix;
        var wrap = function(s) {
            return prefix != null ? [prefix, s].join(' ') : s;
        };

        if (typeof(cmd) === "string") {
            return wrap(cmd);
        } else if (typeof(cmd) === "object") {
            return wrap(cmd.map(function(s) {
                if (s == '') {
                    return "''";
                } else if (!/[ "=]/g.test(s)) {
                    return s;
                } else {
                    return "\"" + addslashes(s) + "\"";
                }
            }).join(' '));
        } else {
            return wrap('');
        }
    };

    this.createProcess = function(cmd) {
        try {
            var c = this.build(cmd);
            console.log("ShellObject.createProcess() ->", c);
            return this._interface.Exec(c);
        } catch (e) {
            console.error("ShellObject.createProcess() ->", e.message);
        }
    };

    this.exec = function(cmd, stdOutPath, stdErrPath) {
        var stdout, stderr;

        this.stdout = PipeIPC.connect("volatile");
        this.stderr = PipeIPC.connect("volatile");

        this.stdout.flush();
        this.stderr.flush();

        if (typeof stdOutPath === "string") {
            this.stdout.startRecorder(stdOutPath, PipeIPC.ForWriting);
        }
        if (typeof stdErrPath === "string") {
            this.stderr.startRecorder(stdErrPath, PipeIPC.ForWriting);
        }

        var c = "%comspec% /c (" + this.build(cmd) + ") 1> " + this.stdout.path;
        //c += " 2>&1";
        c += " 2> " + this.stderr.path;
        this._interface.Run(c, 0, true);
        console.log("ShellObject.exec() ->", c);
        sleep(1);

        this.stdout.reload(this.charset);
        this.stderr.reload(this.charset);

        stdout = this.stdout.read();
        //stderr = this.stderr.read();

        //stdout = this.stdout.read();
        //stderr = this.stderr.read();
        //console.log("[stdout] " + stdout);
        //console.log("[stderr] " + stderr);

        if (!this.isPreventClear) {
            this.clear();
        }

        return stdout;
    };

    this.run = function(cmd, fork) {
        var fork = (typeof(fork) !== "undefined") ? fork : true;
        var c = "%comspec% /q /c (" + this.build(cmd) + ")";
        console.log("ShellObject.run() ->", c);
        this._interface.Run(c, (this.visibility === "hidden" ? 0 : 1), !fork);
    };

    this.runAs = function(FN, args) {
        var oShell = CreateObject("Shell.Application");
        var _args = null;
        console.log("ShellObject.runAs() ->", FN);
        if (typeof(args) !== "undefined") {
            _args = args.join(' ');
        }
        oShell.shellExecute(FN, _args, this.workingDirectory, "runas", (this.visibility === "hidden" ? 0 : 1));
        return oShell;
    };

    this.createShoutcut = function(shoutcutName, cmd) {
        var desktopPath = this._interface.SpecialFolders("Desktop");
        var path = desktopPath + "\\" + shoutcutName + ".lnk";

        if (!FILE.fileExists(path)) {
            var link = this._interface.CreateShortcut(path);
            //link.TargetPath = "cmd";
            //link.Arguments = "/q /c " + this.build(cmd);
            link.TargetPath = "wscript";
            link.Arguments = "bgloader.js " + this.build(cmd);
            //link.Arguments = this.build(cmd);
            link.WindowStyle = 1;
            link.WorkingDirectory = this.workingDirectory;
            //link.Hotkey = "";
            link.IconLocation = require("lib/system").getCurrentScriptDirectory() + "\\app\\favicon.ico";
            link.Save();
        }
    };

    this.getPathOfMyDocuments = function() {
        return this._interface.SpecialFolders("MyDocuments");
    };

    this.release = function() {
        console.log("ShellObject.release() ->", this.currentDirectory);
        this._interface.CurrentDirectory = this.currentDirectory;
        this._interface = null;
    };

    this.clear = function() {
        this.stdout.destroy();
        this.stderr.destroy();
    };

    this.create();
};

exports.create = function() {
    return new ShellObject();
};

exports.build = function(cmd) {
    return (new ShellObject()).build(cmd);
};

exports.exec = function(cmd, stdOutPath, stdErrPath) {
    return (new ShellObject()).setCharset(PipeIPC.CdoEUC_KR).exec(cmd, stdOutPath, stdErrPath);
};

exports.run = function(cmd, fork) {
    return (new ShellObject()).run(cmd, fork);
};

exports.show = function(cmd, fork) {
    return (new ShellObject()).setVisibility("visible").run(cmd, fork);
};

exports.runAs = function(FN, args) {
    return (new ShellObject()).runAs(FN, args);
};

exports.showAs = function(FN, args) {
    return (new ShellObject()).setVisibility("visible").runAs(FN, args);
};

exports.createProcess = function(cmd, workingDirectory) {
    if (typeof(workingDirectory) !== "undefined") {
        console.info("Working directory: " + workingDirectory);
    }
    return (new ShellObject()).setWorkingDirectory(workingDirectory).createProcess(cmd);
};

exports.createDesktopIcon = function(name, cmd, workingDirectory) {
    if (typeof(workingDirectory) !== "undefined") {
        console.info("Working directory: " + workingDirectory);
    }
    return (new ShellObject()).setWorkingDirectory(workingDirectory).createDesktopIcon(name, cmd);
};

exports.getPathOfMyDocuments = function() {
    return (new ShellObject()).getPathOfMyDocuments();
};

exports.CdoCharset = PipeIPC.CdoCharset;

exports.VERSIONINFO = "Windows Shell Interface (shell.js) version 0.3.17";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
