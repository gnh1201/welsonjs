// sendboxie.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var SHELL = require("lib/shell");

var SandboxieObject = function() {
    this.binPath = "%PROGRAMFILES%\\Sandboxie-Plus\\Start.exe";
    this.processID = 0;
    this.sandboxName = "";
    
    this.setSandboxName = function(name) {
        this.sandboxName = name;
        return this;
    };

    this.start = function(cmd) {
        var process;
        while (this.processID == 0) {
            try {
                process = SHELL.createProcess([
                    this.binPath,
                    "/box:" + this.sandboxName,
                    SHELL.build(cmd)
                ].join(' '));
                this.processID = process.ProcessID;
            } catch (e) {
                console.error(e.message);
            }
        }
        return this;
    };
};

exports.start = function(sandboxName, cmd) {
    return (new SandboxieObject()).setSandboxName(sandboxName).start(cmd).processID;
};

exports.VERSIONINFO = "Sandboxie interface (sandboxie.js) version 0.1";
exports.global = global;
exports.require = global.require;
