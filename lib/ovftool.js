// ovftool.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Download OVFTool (Open Virtualization Format (OVF) Tool):
// https://developer.broadcom.com/tools/open-virtualization-format-ovf-tool/latest
//
var SHELL = require("lib/shell");
var CRED = require("lib/credentials");

function OVFObject() {
    this.binPath = "bin\\x64\\VMware-ovftool-4.6.3-24031167-win.x86_64\\ovftool\\ovftool.exe";
    this.hostname = "";
    this.port = 443;
    this.resourceName = "";
    
    this.setBinPath = function(binPath) {
        this.binPath = binPath;
    };
    
    this.setHostName = function(hostname) {
        this.hostname = hostname;
    };
    
    this.setPort = function(port) {
        this.port = port;
    };
    
    this.setResourceName = function(resourceName) {
        this.resourceName = resourceName;
    };

    this.saveTo = function(filename) {
        var cred = CRED.get("password", "ovftool");
        var connectionString = "vi://" +
            encodeURIComponent(cred.username) + ":" +
            encodeURIComponent(cred.password) + "@" +
            this.hostname + (this.port == 443 ? "" : ":" + this.port) +
            this.resourceName
        ;
        var cmd = [
            this.binPath,
            connectionString,
            filename
        ];

        console.log("Use this connection string:", connectionString);
        
        // run the command synchronously
        SHELL.show(cmd, false);
    };
}

function setCredential(username, password) {
    CRED.push("password", "ovftool", {
        "username": username,
        "password": password
    });
}

function create() {
    return new OVFObject();
}

exports.setCredential = setCredential;
exports.create = create;

exports.VERSIONINFO = "Broadcom/VMware OVF Tool interface (ovftool.js) version 0.1.2";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
