////////////////////////////////////////////////////////////////////////
// TUN2SOCKS API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "TUN2SOCKS Lib (tun2socks.js) version 0.1";
exports.global = global;
exports.require = global.require;

var arch = SYS.getArch();

if(arch.indexOf("64")) {
    exports.binPath = "bin/tun2socks-windows-4.0-amd64.exe";
} else {
    exports.binPath = "bin/tun2socks-windows-4.0-386.exe";
}

/**
 * @param {string} name
 * @param {Object} options
 */
exports.assign = function(name, options) {
    var defaultOptions = {
        tunAddr: "10.0.0.2",
        tunGw: "10.0.0.1",
        proxyType: "socks",
        proxyServer: "127.0.0.1:1080",
        tunDns: "8.8.8.8,8.8.4.4", // Cloudflare DNS
        tunName: name
    }, _options = options, cmd = [];
    
    // fill with default options
    for (var k in defaultOptions) {
        if (!(k in _options)) {
            _options[k] = defaultOptions[k];
        }
    }

    // make command
    cmd.push("tun2socks");
    for (var k in _options) {
        cmd.push('-' + k);
        cmd.push(_options[k]);
    }

    // return
    SHELL.run(cmd);
};
