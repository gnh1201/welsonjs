////////////////////////////////////////////////////////////////////////
// Base64 API
////////////////////////////////////////////////////////////////////////

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
