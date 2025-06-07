// certchecker.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var FILE = require("lib/file");
var HTTP = require("lib/http");
var RAND = require("lib/rand");

function main() {
    var domains = splitLn(FILE.readFile("data\\example_domains.txt", FILE.CdoCharset.CdoUTF_8));
    var urls = [];

    domains.forEach(function(x) {
        var handler = HTTP.create("CURL")
            .setIsDebugging(true)
            .setUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36 Edg/105.0.1343.27")
            .setMaxTime(2)
            .setConnectTimeout(2)
            .open("GET", "https://" + x)
            .send();

        if (handler.detectSSLCompleted()) {
            urls.push("https://" + x);
            console.log("https://" + x);
        } else {
            urls.push("http://" + x);
            console.log("http://" + x);
        }

        sleep(RAND.getInt(1000, 2000));
    });

    FILE.writeFile("data\\example_urls.txt", urls.join("\r\n"), FILE.CdoCharset.CdoUTF_8);

    console.log("Done");
}

exports.main = main;
