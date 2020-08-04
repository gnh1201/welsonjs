////////////////////////////////////////////////////////////////////////
// SSloader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var WINTAP = require("lib/wintap");
var SYS = require("lib/system");
var FILE = require("lib/file");
var SHELL = require("lib/shell");

var ssConfig = {
    Server: "",
    NameServer: "https://1.1.1.1/dns-query",
    FilterString: "outbound and (ip ? ip.DstAddr != 1.1.1.1 : true)",
    IPRules: {
        Mode: false,
        IPCIDR: []
    },
    AppRules: {
        Mode: true,
        Programs: []
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
    // 내부 포트 결정
    console.log("* Connecting to socket proxy...");
    var listenPort = SS.connect();
    ssConfig.Server = "socks://localhost:" + listenPort;
    console.log("* Local listening port: " + listenPort);

    // 네트워크 인터페이스 정보 조회
    console.log("* Gethering network interfaces...");
    var inets = SYS.getNetworkInterfaces();
    for (var i = 0; i < inets.length; i++) {
        console.log("    > " + inets[i].Caption);
    }

    // TAP 설치 여부 조회
    console.log("* Gethering WindowsTAP interfaces...");
    console.log(WINTAP.query("tap0901"));
    //ssConfig.TunName = "TAP-Windows Adapter V9";

    // 앱 규칙 설정
    var processNames = __global.processNames;
    for (var i in processNames) {
        ssConfig.AppRules.Programs.push(processNames[i]);
    }

    // 설정 파일 저장
    var serialized_ssConfig = JSON.stringify(ssConfig, null, 4);
    if (FILE.fileExists("config.json")) {
        FILE.deleteFile("config.json");
    }
    FILE.writeFile("config.json", serialized_ssConfig, "utf-8");
    console.info("설정 파일 저장 완료!");

    // 앱 프록시 실행
    console.info("앱 프록시는 관리자 권한을 필요로 합니다. 확인을 눌러주세요.");
    SHELL.run([
        "bin/shadow.exe",
        "-c",
        "config.json"
    ]);
};

exports.ssConfig = ssConfig;
