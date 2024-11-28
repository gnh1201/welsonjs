// punycode.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
// ***SECURITY NOTICE***
// Due to potential security issues, the Public API URL is not provided. If you need to request access, please refer to the project's contact information.
// You can download the server-side script that implements this functionality from the link below:
// https://github.com/gnh1201/caterpillar
// 
var JsonRpc2 = require("lib/jsonrpc2");

var API_URL = "https://azure-ashlan-40.tiiny.io/";

function encode(s) {
    var rpc = JsonRpc2.create(API_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://pub-f926e14287b340cd9eff33731bb25329.r2.dev/punycode.class.php"
        ],
        "args": [
            "return Punycode::encodeHostname('" + s + "')"
        ]
    }, "");

    return result.data;
}

function decode(s) {
    var rpc = JsonRpc2.create(API_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://pub-f926e14287b340cd9eff33731bb25329.r2.dev/punycode.class.php"
        ],
        "args": [
            "return Punycode::decodeHostname('" + s + "')"
        ]
    }, "");

    return result.data;
}

exports.encode = encode;
exports.decode = decode;

exports.VERSIONINFO = "Punycode Public API client (punycode.js) version 0.2.2";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
