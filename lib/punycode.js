var Py3 = require("lib/python3");

function encode(s) {
    return Py3.execScript("app\\assets\\py\\idnaencode.py", [s]).trim();
}

exports.encode = encode;

exports.VERSIONINFO = "Punycode Converter (punycode.js) version 0.1";
exports.global = global;
exports.require = global.require;
