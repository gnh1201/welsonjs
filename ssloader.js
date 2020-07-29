////////////////////////////////////////////////////////////////////////
// Socksloader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var WINTAP = require("lib/wintap");

exports.main = function() {
    console.log("Connecting to shadowsocks...");
    var proxyport = SS.connect();
    console.log(proxyport);

    console.log("Installing new WindowsTAP...");
    console.log(WINTAP.query("tap0901"));
    console.log("Done");
};
