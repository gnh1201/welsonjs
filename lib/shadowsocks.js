////////////////////////////////////////////////////////////////////////
// Shadowsocks API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "Shadowsocks Lib (shadowsocks.js) version 0.1";
exports.global = global;
exports.require = global.require;
exports.binPath = "bin/ss-local.exe";

exports.getRandomInt = function(min, max) {
    var x = Math.random();
    return min + Math.floor((max - min) * x);
};

exports.connect = function() {
    var listenPort = exports.getRandomInt(49152, 65535);

    SHELL.run([
        exports.binPath,
        "-s",
        __config.shadowsocks.host,
        "-p",
        __config.shadowsocks.port,
        "-l",
        listenPort,
        "-k",
        __config.shadowsocks.password,
        "-m",
        __config.shadowsocks.cipher
    ], true);

    return listenPort;
};
