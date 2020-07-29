////////////////////////////////////////////////////////////////////////
// Shadowsocks API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "Shadowsocks Lib (shadowsocks.js) version 0.1";
exports.global = global;
exports.require = global.require;

var arch = SYS.getArch();
if(arch.indexOf("64") > -1) {
    exports.binPath = "bin/shadowsocks-lib-mingw-x86_64/ss-local.exe";
} else {
    exports.binPath = "bin/shadowsocks-lib-mingw-x86/ss-local.exe";
}

exports.getRandomInt = function(min, max) {
    var x = Math.random();
    return min + Math.floor((max - min) * x);
};

exports.connect = function() {
    return;
    
    var port = exports.getRandomInt(49152, 65535);

    SHELL.run([
        exports.binPath,
        "-s",
        __config.shadowsocks.host,
        "-p",
        __config.shadowsocks.port,
        "-l",
        port,
        "-k",
        __config.shadowsocks.password,
        "-m",
        __config.shadowsocks.cipher
    ], true);

    return port; 
};
