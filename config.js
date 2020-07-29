exports.config = {
    appName: "welsonjs",
    shadowsocks: {
        host: "158.247.196.146",
        port: 8388,
        password: "korea82",
        cipher: "chacha20-ietf-poly1305"
    },
    webapp: {
        baseURL: "http://158.247.196.146/"
    },
    shadow: {
        "IPRules": {
            "Mode": true,
            "IPCIDR": [
                "44.44.0.0/16",
                "1.1.1.1",
                "1.0.0.1",
                "8.8.8.8",
                "8.8.4.4",
                "2001:4860:4860::8888",
                "2001:4860:4860::8844",
                "2001:4860:4860::/64",
                "91.108.4.0/22",
                "91.108.8.0/22",
                "91.108.12.0/22",
                "91.108.20.0/22",
                "91.108.36.0/23",
                "91.108.38.0/23",
                "91.108.56.0/22",
                "149.154.160.0/20",
                "149.154.164.0/22",
                "149.154.172.0/22"
            ]
        },
        "AppRules": {
            "Mode": true,
            "Programs":[
                "git.exe",
                "chrome.exe",
                "msedge.exe",
                "iexplore.exe",
                "firefox.exe",
                "opera.exe"
            ]
        },
        "DomainRules": {
            "Proxy": [
                "**.google.com",
                "**.google.*",
                "**.google.*.*",
                "**.youtube.com",
                "*.twitter.com",
                "www.facebook.com",
                "bing.com",
                "**.amazon.*"
            ],
            "Direct": [
                "**.baidu.*",
                "**.youku.*",
                "**.*"
            ],
            "Blocked": [
                "ad.blocked.com"
            ]
        }
    }
};
