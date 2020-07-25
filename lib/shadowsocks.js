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
    exports.binPath = "bin/shadowsocks-lib-mingw-x86_64/ss-local";
} else {
    exports.binPath = "bin/shadowsocks-lib-mingw-x86/ss-local";
}

exports.connect = function(host, remotePort, localPort, password, algorithm) {
    var commandOptions = [];
    commandOptions.push(binPath);
    commandOptions.push("-s");
    commandOptions.push(host);
    commandOptions.push("-p");
    commandOptions.push(remotePort);
    commandOptions.push("-l");
    commandOptions.push(localPorts);
    commandOptions.push("-k");
    commandOptions.push(password);
    commandOptions.push("-m");
    commandOptions.push(algoritm);
    SHELL.run(commandOptions.join(' '), true);
};
