////////////////////////////////////////////////////////////////////////
// SSloader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var WINTAP = require("lib/wintap");
var SYS = require("lib/system");
var HOSTS = require("lib/hosts");

exports.main = function() {
    console.log("Connecting to shadowsocks...");
    var proxyport = SS.connect();
    console.log(proxyport);

    console.log("Gethering network interfaces...");
    var inets = SYS.getNetworkInterfaces();
    for (var i in inets) {
        console.log("Caption > " + inets[i].Caption);
        console.log("    > IPAddress > " + inets[i].IPAddress);
        console.log("    > MACAddress > " + inets[i].MACAddress);
    }

    console.log("Gethering hosts...");
    var hosts = HOSTS.getHosts();
    for (var i = 0; i < hosts.length; i++) {
        console.log(hosts[i].domain + " -> " + hosts[i].host);
    }

    console.log("Gethering WindowsTAP interfaces...");
    console.log(WINTAP.query("tap0901"));
    console.log("Done");
};
