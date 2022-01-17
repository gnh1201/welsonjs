////////////////////////////////////////////////////////////////////////
// Base64 API
////////////////////////////////////////////////////////////////////////

var XML = require("lib/xml");

exports.encode = function(sText) {
    return XML.create().createElement("base64").encode(sText, "bin.base64");
};

exports.decode = function(vCode) {
    return XML.create().createElement("base64").decode(vCode, "bin.base64");
};

exports.VERSIONINFO = "Base64 Module (base64.js) version 0.2";
exports.global = global;
exports.require = require;
