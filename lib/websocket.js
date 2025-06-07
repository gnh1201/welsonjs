// websocket.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// references:
// https://stackoverflow.com/questions/52783655/use-curl-with-chrome-remote-debugging
// https://github.com/vi/websocat
// 
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");

var WebsocketObject = function() {
    this._interface = null;

    this.timeout = 0;

    this.setBinPath = function(path) {
        if (typeof(path) !== "undefined") {
            this._interface.setPrefix(path);
        } else {
            var arch = SYS.getArch();
            if(arch.indexOf("64") > -1) {
                this._interface.setPrefix("bin\\x64\\websocat.x86_64-pc-windows-gnu.exe");
            } else {
                this._interface.setPrefix("bin\\x86\\websocat.i686-pc-windows-gnu.exe");
            }
        }
    };

    this.setTimeout = function(timeout) {
        this.timeout = timeout;
    };

    this.send = function(uri, msg) {
        var seed = parseInt(Math.random() * 10000);
        var FN = "tmp\\stdin_" + seed + ".txt";

        try {
            FILE.writeFile(FN, msg + "\n", FILE.CdoCharset.CdoUTF_8);
            console.log(msg);
            sleep(1);

            var cmd = ["-n1", "-t"];
            if (this.timeout > 0) {
                cmd.push("--ping-timeout");
                cmd.push(this.timeout);
            }
            cmd.push(uri);
            cmd.push("<");
            cmd.push(FN);

            return this._interface.exec(cmd);
        } catch (e) {
            console.error("WebsocketObject.send() -> " + e.message);
        }

        if (FILE.fileExists(FN)) FILE.deleteFile(FN);
    };

    this.create = function() {
        this._interface = SHELL.create();
        this.setBinPath();
    };

    this.create();
};

exports.VERSIONINFO = "Websocket Interface (websocket.js) version 0.2.3";
exports.global = global;
exports.require = global.require;

exports.create = function() {
    return new WebsocketObject();
};
