// nmap.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var client = require("lib/catproxy");

var PortScanner = function(binpath, url) {
    this.url = url;
    this.binpath = binpath;

    this.scan = function(hosts) {
        var worker = client.create(url);
        worker.set_method("scan_ports_by_hosts");

        var result = worker.exec({
            "binpath": this.binpath,
            "hosts": hosts
        });
        return result;
    }
};

exports.PortScanner = PortScanner;

exports.VERSIONINFO = "NMAP interface version 0.2";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
