// base64.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Base64 API
// 
//var XML = require("lib/xml");
var PS = require("lib/powershell");

function encode(sText) {
    return PS.execScript("app\\assets\\ps1\\base64encode", [sText]).trim();
    //return XML.create().createElement("base64").encode(sText, "bin.base64");
};

function decode(vCode) {
    return (function(s) {
        var h = "Encoded String: ";
        if (s.indexOf(h) == 0) {
            s = s.substring(h.length);
		}
        return s;
    })(PS.execScript("app\\assets\\ps1\\base64decode", [vCode]).trim());
    //return XML.create().createElement("base64").decode(vCode, "bin.base64");
};

exports.encode = encode;
exports.decode = decode;

exports.VERSIONINFO = "Base64 Module (base64.js) version 0.3.1";
exports.global = global;
exports.require = require;
