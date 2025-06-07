// ip-reputation.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// SECURITY NOTICE
// AbuseIPDB requires an internet connection, and data may be transmitted externally. Please check the terms of use and privacy policy.
// https://www.abuseipdb.com/
//
var HTTP = require("lib/http");
var APIKEY = require("lib/apikey");

function check(ip_address) {
    var apikey = APIKEY.getApiKey("abuseipdb");
    
    var response = HTTP.create()
        .setHeaders({
            "Key": apikey,
            "Accept": "application/json"
        })
        .setParameters({
            "ipAddress": ip_address
        });
    
    return response.responseBody;
}

exports.check = check;

exports.VERSIONINFO = "IP Reputation Checker (ip-reputation.js) version 0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
