////////////////////////////////////////////////////////////////////////
// SSloader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var WINTAP = require("lib/wintap");
var SYS = require("lib/system");

exports.main = function() {
    console.log("Connecting to shadowsocks...");
    var proxyport = SS.connect();
    console.log(proxyport);
    
    console.log("Gethering informations of network interfaces");
    var inets = SYS.getNetworkInterfaces();
    for(var i in inets) {
        console.log(inets[i].Caption);
        console.log("    > " + inets[i].IPAddresses.join(","));
        console.log("    > " + inets[i].MACAddress);
    }

    console.log("Installing new WindowsTAP...");
    console.log(WINTAP.query("tap0901"));
    console.log("Done");
};
