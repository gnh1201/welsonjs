// powershell.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Powershell Interface API
// 
var SHELL = require("lib/shell");

var PowershellObject = function() {
    var _interface = SHELL.create();

    this.execType = "ps1";
    this.dataType = -1;
    this.target = null;
    
    this.setExecType = function(execType) {
        this.execType = execType;
        return this;
    };

    this.load = function(script) {
        this.target = script;
        this.dataType = 0;
        return this;
    };

    this.loadCommand = function(command) {
        this.target = command;
        this.dataType = 1;
        return this;
    };

    this.loadFile = function(filename) {
        this.target = filename;
        this.dataType = 2;
        return this;
    };

    this.loadUrl = function(url) {
        this.target = url;
        this.dataType = 3;
        return this;
    };

    // For example:
    //   file:C:\\a\\b\\c
    //   http://
    //   https://
    //   data:text/plain;base64,SGVsbG8sIFdvcmxkIQ==
    this.loadURI = function(uri) {
        var pos = uri.indexOf(':');
        var scheme = (pos < 0 ? '' : url.substring(0, pos));
        var target = (pos < 0 ? uri : url.substring(pos + 1));

        switch (scheme) {
            case 'http':
            case 'https':
                this.loadUrl(target);
                break;

            case 'file':
                this.loadFile(target);
                break;

            case 'data':
                this.load(target);
                break;

            default:
                console.error("Invalid scheme");
        }
        return this;
    };
    
    this.build = function(args) {
        if (this.execType != "ps1") {
            console.warn("The execType is not set 'ps1'. Will be forward it to the default shell.");
            return SHELL.build(this.target);
        }

        var cmd = [
            "-NoProfile",
            "-ExecutionPolicy",
            "ByPass",
            "-nologo"
        ];    // default arguments

        switch (this.dataType) {
            case 3:    // dataType: URL(3)\
                // todo
                break;
        
            case 2:   // dataType: file(2)
                cmd.push("-file");
                cmd.push(this.target + ".ps1");
                break;
            
            case 1:   // dataType: command(1)
                cmd.push("-Command");
                if (typeof this.target === "string") {
                    cmd.push("& {" + this.target + "}");
                } else {
                    cmd.push("& {" + SHELL.build(this.target) + "}");
                }
                break;

            case 0:   // dataType: script(0)
                // todo
                break;
  
            default:
                break;
        }

        if (typeof(cmd) !== "undefined") {
            cmd = cmd.concat(args);
        }
 
        return cmd;
    };

    this.exec = function(args) {
        return _interface.exec(this.build(args));
    };

    this.runAs = function(args) {
        return this.exec("Start-Process cmd \"/q /c " + SHELL.addslashes(this.build(args)) + "\" -Verb RunAs");
    };

    // set the location of PowerShell runtime
    _interface.setPrefix("powershell.exe");
}

function create() {
	return new PowershellObject();
}

function execScript(scriptName, args) {
    return create().loadFile(scriptName).exec(args);
}

function execCommand(cmd) {
    return create().loadCommand(cmd).exec();
}

function runAs(cmd) {
    return create().setExecType("cmd").runAs();
}

exports.execScript = execScript;
exports.execCommand = execCommand;
exports.runAs = runAs;

exports.VERSIONINFO = "Powershell Interface (powershell.js) version 0.1.4";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
