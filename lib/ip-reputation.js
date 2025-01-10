// ip-reputation.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
//
// ***SECURITY NOTICE***
// IP Reputation Checker requires an internet connection, and data may be transmitted externally. Users must adhere to the terms of use and privacy policy.
// - AbuseIPDB website: https://www.abuseipdb.com/
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
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
