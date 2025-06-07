// cloudflare.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Cloudflare API
// 
var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "Cloudflare(d) Integration (cloudflare.js) version 0.1";
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
