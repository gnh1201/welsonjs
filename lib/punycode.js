// punycode.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// SECURITY NOTICE
// Due to potential security issues, the Public API URL is not provided. If you need to request access, please refer to the project's contact information.
// You can download the server-side script that implements this functionality from the link below:
// https://github.com/gnh1201/caterpillar
// 
var JsonRpc2 = require("lib/jsonrpc2");

function encode(s) {
    var rpc = JsonRpc2.create(JsonRpc2.DEFAULT_JSONRPC2_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://scriptas.catswords.net/punycode.class.php"
        ],
        "args": [
            "return Punycode::encodeHostname('" + s + "')"
        ]
    }, "");

    return result.data;
}

function decode(s) {
    var rpc = JsonRpc2.create(JsonRpc2.DEFAULT_JSONRPC2_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://scriptas.catswords.net/punycode.class.php"
        ],
        "args": [
            "return Punycode::decodeHostname('" + s + "')"
        ]
    }, "");

    return result.data;
}

exports.encode = encode;
exports.decode = decode;

exports.VERSIONINFO = "Punycode Conversion Client (punycode.js) version 0.2.4";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
