////////////////////////////////////////////////////////////////////////
// Cloudflare API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "Cloudflare Lib (cloudflare.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.binPath = "bin/cloudflared";

// TODO: https://developers.cloudflare.com/access/rdp/rdp-guide/

exports.installService = function() {
    var commandOptions = [];
    commandOptions.push(binPath);
    commandOptions.push("service");
    commandOptions.push("install");

    return SHELL.exec(commandOptions.join(' '));
};
