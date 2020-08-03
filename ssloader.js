////////////////////////////////////////////////////////////////////////
// SSloader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var WINTAP = require("lib/wintap");
var SYS = require("lib/system");
var HOSTS = require("lib/hosts");
var JSON = require("lib/json");

var ssConfig = {
    Server: [],
    NameServer: "https://1.1.1.1/dns-query",
    FilterString: "outbound and (ip ? ip.DstAddr != 1.1.1.1 : true)",
    TunName: "",
    TunAddr: [
        "192.168.0.11/24"
    ],
    IPCIDRRules: {
        Proxy: [
            "198.18.0.0/16",
            "8.8.8.8/32"
        ]
    },
    AppRules: {
        Proxy: []
    },
    DomainRules: {
        Proxy: [
            "**.google.com",
            "**.google.*",
            "**.google.*.*",
            "**.youtube.com",
            "*.twitter.com",
            "www.facebook.com",
            "bing.com",
            "**.amazon.*"
        ],
        Direct: [
            "**.baidu.*",
            "**.youku.*",
            "**.*"
        ],
        Blocked: [
            "ad.blocked.com"
        ]
    }
};

exports.main = function() {
    console.log("* Connecting to shadowsocks...");
    var listenPort = SS.connect();
    ssConfig.Server.push("socks://localhost:" + listenPort); 
    console.log("* Local listening port: " + listenPort);

    console.log("* Gethering network interfaces...");
    var inets = SYS.getNetworkInterfaces();
    for (var i = 0; i < inets.length; i++) {
        console.log("    > " + inets[i].Caption);
    }

    console.log("* Gethering WindowsTAP interfaces...");
    console.log(WINTAP.query("tap0901"));
    ssConfig.TunName = "TAP-Windows Adapter V9";
    
    console.log(JSON.stringify(ssConfig));
    
};
