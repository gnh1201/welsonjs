// totp.js
// TOTP library for WelsonJS framework
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
// ***SECURITY NOTICE***
// The TOTP library requires an internet connection, and data may be transmitted externally. Users must adhere to the terms of use and privacy policy.
// - Privacy Policy: https://policy.catswords.social/site_terms.html
// - Terms of Service: https://policy.catswords.social/site_extended_description.html
// 
var JsonRpc2 = require("lib/jsonrpc2");

var API_URL = "https://public-api.catswords.net";

function getPubKey() {
    var rpc = JsonRpc2.create(API_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://raw.githubusercontent.com/dimamedia/PHP-Simple-TOTP-and-PubKey/refs/heads/master/class.tfa.php"
        ],
        "args": [
            "$tfa = new tfa(); return $tfa->getPubKey()"
        ]
    }, "");
    
    return result.data;
}

function getOtp(pubkey) {
    var rpc = JsonRpc2.create(API_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://raw.githubusercontent.com/dimamedia/PHP-Simple-TOTP-and-PubKey/refs/heads/master/class.tfa.php"
        ],
        "args": [
            "$tfa = new tfa(); return $tfa->getOtp('" + pubkey + "')"
        ]
    }, "");

    return result.data;
}

exports.getPubKey = getPubKey;
exports.getOtp = getOtp;

exports.VERSIONINFO = "TOTP library (totp.js) version 0.1.3";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
