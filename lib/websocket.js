////////////////////////////////////////////////////////////////////////
// Websocket API
////////////////////////////////////////////////////////////////////////

// references:
// https://stackoverflow.com/questions/52783655/use-curl-with-chrome-remote-debugging
// https://github.com/vi/websocat

var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");

var WebsocketObject = function() {
    this.binPath = "bin\\websocat_win64";

    this.setBinPath = function(path) {
        if (typeof(path) !== "undefined") {
            this.binPath = path;
        } else {
            var arch = SYS.getArch();
            if(arch.indexOf("64") > -1) {
                this.binPath = "bin\\websocat_win64";
            } else {
                this.binPath = "bin\\x86\\websocat_win32";
            }
        }
    };

    this.send = function(uri, msg) {
        var seed = parseInt(Math.random() * 10000);
        var FN = "tmp\\stdin_" + seed + ".txt";

        try {
            FILE.writeFile(FN, msg + "\n", "utf-8");
            console.log(msg);
            sleep(1);

            return SHELL.exec([
                this.binPath,
                "-n1",
                "-t",
                uri,
                "<",
                FN
            ]);
        } catch (e) {
            console.error("WebsocketObject.send() -> " + e.message);
        }

        if (FILE.fileExists(FN)) FILE.deleteFile(FN);
    };

    this.create = function() {
        this.setBinPath();
    };

    this.create();
};

exports.VERSIONINFO = "Websocket Lib (websocket.js) version 0.2";
exports.global = global;
exports.require = global.require;

exports.create = function() {
    return new WebsocketObject();
};
