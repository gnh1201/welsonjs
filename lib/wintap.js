////////////////////////////////////////////////////////////////////////
// WindowsTAP API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "WindowsTAP Lib (wintap.js) version 0.1";
exports.global = global;
exports.require = global.require;

var arch = SYS.getArch();
if(arch.indexOf("64") > -1) {
    exports.binPath = "bin/tap-windows-9.24.2/amd64/tapinstall.exe";
} else {
    exports.binPath = "bin/tap-windows-9.24.2/i386/tapinstall.exe";
}

exports.install = function(inf, id) {
    return SHELL.exec([exports.binPath, "install", inf, id]);
};

exports.update = function(inf, id) {
    return SHELL.exec([exports.binPath, "update", inf, id]);
};

exports.query = function(id) {
    return SHELL.exec([exports.binPath, "hwids", id]);
};

exports.remove = function(id) {
    return SHELL.exec([exports.binPath, "remove", id]);
};
