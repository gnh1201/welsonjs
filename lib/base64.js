////////////////////////////////////////////////////////////////////////
// Base64 API
////////////////////////////////////////////////////////////////////////

//var XML = require("lib/xml");
var PS = require("lib/powershell");

exports.encode = function(sText) {
    return PS.execScript("app\\assets\\ps1\\base64encode", [sText]).trim();
    //return XML.create().createElement("base64").encode(sText, "bin.base64");
};

exports.decode = function(vCode) {
    return PS.execScript("app\\assets\\ps1\\base64decode", [vCode]).trim();
    //return XML.create().createElement("base64").decode(vCode, "bin.base64");
};

exports.VERSIONINFO = "Base64 Module (base64.js) version 0.3";
exports.global = global;
exports.require = require;
