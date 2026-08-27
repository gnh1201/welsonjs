// shell.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Windows Shell Interface with WelsonJS Pipe-IPC module
// 
var FILE = require("lib/file");
var PipeIPC = require("lib/pipe-ipc");

// ---------------------------------------------------------------------------
// Secure command-line argument encoder (√Îæ‡¡°-1 fix, CWE-78/CWE-88)
//
// Array-form arguments passed to build()/run()/exec() are treated as DISCRETE
// arguments and encoded so an attacker-controlled value cannot break out of its
// argument and inject additional commands. The encoding survives BOTH layers a
// WelsonJS command passes through:
//   (1) WScript.Shell.Run/Exec environment-variable expansion of %VAR%
//   (2) cmd.exe metacharacter parsing inside `%comspec% /c (...)`
//
// Encoding steps per argument:
//   A. CommandLineToArgvW-correct quoting (backslash/double-quote rules)
//   B. caret-escape of every cmd.exe metacharacter: ( ) ! ^ " < > & |
//   C. '%' handling: rejected by default because it cannot be safely encoded
//      through the WScript.Shell.Run + cmd.exe layers. Internal callers that
//      intentionally rely on %VAR% expansion (e.g. "%PROGRAMFILES%\\...") must
//      pre-resolve the path with SYS.getEnvString() and pass a literal path.
//      Call setPercentPolicy("strip") to drop '%' instead of throwing.
//
// STRING-form commands are left untouched: passing a raw string means the
// developer is supplying a full command line intentionally, so no argument
// safety can or should be assumed there. Only the ARRAY form promises
// per-argument neutralization, and only the array form is encoded.
// ---------------------------------------------------------------------------
var _percentPolicy = "reject"; // "reject" | "strip"

function setPercentPolicy(policy) {
    if (policy === "reject" || policy === "strip") {
        _percentPolicy = policy;
    }
    return _percentPolicy;
}

function _argvNeedsQuoting(s) {
    if (s.length === 0) return true;
    for (var i = 0; i < s.length; i++) {
        var c = s.charAt(i);
        if (c === " " || c === "\t" || c === "\n" || c === "\u000B" || c === "\f" || c === "\"") {
            return true;
        }
    }
    return false;
}

function _quoteArgvW(s) {
    if (!_argvNeedsQuoting(s)) {
        return s;
    }
    var out = "\"";
    var i = 0;
    var j;
    while (true) {
        var backslashes = 0;
        while (i < s.length && s.charAt(i) === "\\") {
            i++;
            backslashes++;
        }
        if (i === s.length) {
            for (j = 0; j < backslashes * 2; j++) out += "\\";
            break;
        } else if (s.charAt(i) === "\"") {
            for (j = 0; j < backslashes * 2 + 1; j++) out += "\\";
            out += "\"";
            i++;
        } else {
            for (j = 0; j < backslashes; j++) out += "\\";
            out += s.charAt(i);
            i++;
        }
    }
    out += "\"";
    return out;
}

var _CMD_META = "()!^\"<>&|";

function _caretEscapeForCmd(s) {
    var out = "";
    for (var i = 0; i < s.length; i++) {
        var c = s.charAt(i);
        out += (_CMD_META.indexOf(c) > -1) ? ("^" + c) : c;
    }
    return out;
}

function encodeArg(arg) {
    var s = (arg === null || typeof arg === "undefined") ? "" : arg.toString();
    if (s.indexOf("%") > -1) {
        if (_percentPolicy === "strip") {
            s = s.replace(/%/g, "");
        } else {
            throw new Error("shell.encodeArg: '%' cannot be safely encoded through WScript.Shell.Run/cmd.exe. Pre-resolve via SYS.getEnvString() or call setPercentPolicy('strip').");
        }
    }
    return _caretEscapeForCmd(_quoteArgvW(s));
}

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
        } else if (typeof(cmd) === "object" && cmd != null) {
            var parts = [];
            for (var i = 0; i < cmd.length; i++) {
                parts.push(encodeArg(cmd[i]));
            }
            return wrap(parts.join(' '));
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
        var windowStyle = (this.visibility === "hidden" ? 0 : 1);
        console.log("ShellObject.run() ->", c);
        if (windowStyle == 1) {
            console.log("Will be open the visible window");
        }
        this._interface.Run(c, windowStyle, !fork);
    };

    this.runAs = function(FN, args) {
        var oShell = CreateObject("Shell.Application");
        var windowStyle = (this.visibility === "hidden" ? 0 : 1);
        var _args = null;
        console.log("ShellObject.runAs() ->", FN);
        if (typeof(args) !== "undefined") {
            _args = args.join(' ');
        }
        if (windowStyle == 1) {
            console.log("Will be open the visible window");
        }
        oShell.shellExecute(FN, _args, this.workingDirectory, "runas", windowStyle);
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

exports.encodeArg = encodeArg;

exports.setPercentPolicy = setPercentPolicy;

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

exports.VERSIONINFO = "Windows Shell Interface (shell.js) version 0.3.18";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
