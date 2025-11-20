// python3.js
// Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Python Interface
// 
var SYS = require("lib/system");
var SHELL = require("lib/shell");
var FILE = require("lib/file");

function PythonObject() {
    this.binPath = null;
    this.version = "3.13.2";

    this.create = function() {
        var default_python3_path = SYS.getAppDataDir() + "\\python\\python.exe";
        if (FILE.fileExists(default_python3_path)) {
            this.setBinPath(default_python3_path);
        } else {
            var arch = SYS.getArch();
            if (arch.toLowerCase().indexOf("arm") > -1) {
                this.setBinPath("bin\\arm64\\python-" + this.version + "-embed-arm64\\python.exe");
            } else if (arch.indexOf("64") > -1) {
                this.setBinPath("bin\\x64\\python-" + this.version + "-embed-amd64\\python.exe");
            } else {
                this.setBinPath("bin\\x86\\python-" + this.version + "-embed-win32\\python.exe");
            }
        }
    };
    
    this.setBinPath = function(binPath) {
        this.binPath = binPath;
    };

    this.setVersion = function(version) {
        this.version = version;
        this.create();
    };

    this.execScript = function(scriptName, args) {
        return SHELL.exec([
            this.binPath,
            scriptName
        ].concat(args));
    };

    this.runScript = function(scriptName, args) {
        return SHELL.show([
            this.binPath,
            scriptName
        ].concat(args));
    };
	
    // Initialize default Python binary path based on current version and system arch
    this.create();
}

exports.PythonObject = PythonObject;

exports.create = function() {
    return new PythonObject();
};

exports.execScript = function(scriptName, args) {
    return (new PythonObject()).execScript(scriptName, args);
};

exports.VERSIONINFO = "Python Interface (python3.js) version 0.2.2";
exports.global = global;
exports.require = global.require;
