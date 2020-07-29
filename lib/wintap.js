////////////////////////////////////////////////////////////////////////
// WindowsTAP API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");

exports.VERSIONINFO = "WindowsTAP Lib (wintap.js) version 0.1";
exports.global = global;
exports.require = global.require;

var arch = SYS.getArch();

exports.infPath = SYS.getEnvString("PROGRAMFILES") + "/TAP-Windows/driver/OemVista.inf";
exports.binPath = SYS.getEnvString("PROGRAMFILES") + "/TAP-Windows/bin/tapinstall.exe";

exports.before = function() {
    if(!FILE.fileExists(exports.binPath)) {
        console.log("WindowsTAP dose not installed. Trying to install...");
        return SHELL.exec(["bin/tap-windows-9.24.2-I601-Win7.exe"]);
    } else {
        return true;
    }
};

/**
 * @param {string} id
 */
exports.install = function(id) {
    exports.before();
    return SHELL.exec([exports.binPath, "install", exports.infPath, id]);
};

/**
 * @param {string} id
 */
exports.update = function(id) {
    exports.before();
    return SHELL.exec([exports.binPath, "update", exports.infPath, id]);
};

/**
 * @param {string} id
 */
exports.query = function(id) {
    exports.before();
    return SHELL.exec([exports.binPath, "hwids", id]);
};

/**
 * @param {string} id
 */
exports.remove = function(id) {
    exports.before();
    return SHELL.exec([exports.binPath, "remove", id]);
};
