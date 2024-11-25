// punycode.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
//
// ***SECURITY NOTICE***
// The Punycode (IDN) library requires an internet connection. Data may be transmitted externally. Users must also comply with the terms of use.
// - Privacy Policy: https://policy.catswords.social/site_terms.html
// - Terms of Service: https://policy.catswords.social/site_extended_description.html
// 
var JsonRpc2 = require("lib/jsonrpc2");

var API_URL = "https://public-api.catswords.net";

function encode(s) {
    var rpc = JsonRpc2.create(API_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "idn_to_ascii",
        "args": [s]
    }, "");

    return result.data;
}

function decode(s) {
    var rpc = JsonRpc2.create(API_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "idn_to_utf8",
        "args": [s]
    }, "");

    return result.data;
}

exports.encode = encode;
exports.decode = decode;

exports.VERSIONINFO = "Punycode Public API client (punycode.js) version 0.2";
exports.global = global;
exports.require = global.require;
