////////////////////////////////////////////////////////////////////////
// Cloudflare API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "Cloudflare Lib (cloudflare.js) version 0.1";
exports.global = global;
exports.require = global.require;

var arch = SYS.getArch();
if(arch.indexOf("64") > -1) {
    exports.binPath = "bin/cloudflared-stable-windows-amd64/cloudflared";
} else {
    exports.binPath = "bin/cloudflared-stable-windows-386/cloudflared";
}

// TODO: https://developers.cloudflare.com/access/rdp/rdp-guide/

exports.installService = function() {
    return SHELL.exec([
        exports.binPath,
        "service",
        "install"
    ]);
};
