////////////////////////////////////////////////////////////////////////
// SSloader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var WINTAP = require("lib/wintap");
var SYS = require("lib/system");
var FILE = require("lib/file");
var SHELL = require("lib/shell");

var ssConfig = {
    "Server": [],
    "NameServer": "https://1.1.1.1/dns-query",
    "FilterString": "outbound and (ip ? ip.DstAddr != 1.2.3.4 and ip.DstAddr != 1.1.1.1 : true)",
    "IPCIDRRules": {
        "Proxy": [
            "198.18.0.0/16",
            "8.8.8.8/32"
        ]
    },
    "AppRules": {
        "Proxy":[]
    },
    "DomainRules": {
        "Proxy": [],
        "Direct": [],
        "Blocked": []
    }
};

exports.main = function() {
    // 내부 포트 결정
    console.log("* Connecting to socket proxy...");
    var listenPort = SS.connect();
    ssConfig.Server.push("socks://localhost:" + listenPort);
    console.log("* Local listening port: " + listenPort);

    // 앱 규칙 설정
    var processNames = __global.processNames;
    for (var i in processNames) {
        ssConfig.AppRules.Proxy.push(processNames[i]);
    }

    // 설정 파일 저장
    var serialized_ssConfig = JSON.stringify(ssConfig, null, 4);
    if (FILE.fileExists("config.json")) {
        FILE.deleteFile("config.json");
    }
    FILE.writeFile("config.json", serialized_ssConfig, "utf-8");
    console.info("설정 파일 저장 완료!");

    // 앱 프록시 실행
    console.info("앱 프록시는 관리자 권한을 필요로 합니다. 권한 요청 시 확인을 눌러주세요.");
    SHELL.elevatedRun("assignProxy.bat");
};

exports.ssConfig = ssConfig;
