////////////////////////////////////////////////////////////////////////
// Shadowsocks API
////////////////////////////////////////////////////////////////////////

var CONFIG = require("lib/config");
var SHELL = require("lib/shell");

var binPath = "bin\\ss-local.exe";

var getRandomInt = function(min, max) {
    var x = Math.random();
    return min + Math.floor((max - min) * x);
};

var ShadowsocksObject = function() {
    this.binPath
    this.processID = 0;
    this.listenPort = 0;

    this.connect = function(host) {
        this.listenPort = getRandomInt(49152, 65535);

        var process = SHELL.createProcess([
            binPath,
            "-s",
            host,
            "-p",
            CONFIG.readConfig("/SSPort").first().getText(),
            "-l",
            this.listenPort,
            "-k",
            CONFIG.readConfig("/SSPassword").first().getText(),
            "-m",
            CONFIG.readConfig("/SSCipher").first().getText()
        ]);

        sleep(1000);

        try {
            this.processID = process.ProcessID;

            if (this.processID > 0) {
                return this;
            } else {
                console.info("Retrying connect to shadowsocks...");
                return this.connect();
            }
        } catch(e) {
            console.info("Retrying connect to shadowsocks...");
            return this.connect();
        }
    };
};

exports.connect = function(host) {
    return (new ShadowsocksObject()).connect(host);
};

exports.VERSIONINFO = "Shadowsocks interface (shadowsocks.js) version 0.2";
exports.global = global;
exports.require = global.require;
