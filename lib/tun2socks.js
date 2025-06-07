// tun2sock.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// TUN2SOCKS API
// 
var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "TUN2SOCKS Lib (tun2socks.js) version 0.1";
exports.global = global;
exports.require = global.require;
exports.binPath = "bin/tun2socks.exe";

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
