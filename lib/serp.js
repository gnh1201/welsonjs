// serp.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// SERP/SEO tools integration for WelsonJS framework
// 
function parseUrl(url) {
    var getEngine = function(url) {
        var defaultEngine = "google";
        var engines = {
            "google": ["naver", "daum"],
            "google_shopping": [
                "aliexpress", "temu", "coupang", "shopping.naver",
                "ssg", "gmarket", "11st", "store.kakao",
                "lotteon", "tmon", "wemakeprice"
            ],
            "default": [
                "google", "youtube", "bing", "baidu",
                "amazon", "duckduckgo"
            ]
        };

        var match = url.match(/^(?:https?:\/\/)?(?:www\.)?([\w.-]+)\.\w+$/);
        if (!match) {
            return defaultEngine;
        }

        var domain = match[1];

        for (var key in engines) {
            if (engines.hasOwnProperty(key)) {
                var group = engines[key];
                if (group.indexOf(domain) !== -1) {
                    if (key === "default") {
                        return domain;
                    }
                    return key;
                }
            }
        }

        return defaultEngine;
    };

    var getKeyword = function(url) {
        var regex = /[?&](q|wd|query|keyword|search_query|k|SearchText|search_key)=([^&]*)/g;
        //var regex = /(?:[?&](q|wd|keyword|query|search_query|k|SearchText|search_key)=|\/pdsearch\/)([^&?]*)/g;
        var match, keywords = [];
        while ((match = regex.exec(url)) !== null) {
            keywords.push(match[2]);
        }
        return keywords.join(' ');
    };

    return {
        "engine": getEngine(url),
        "keyword": getKeyword(url)
    }
};

exports.parseUrl = parseUrl;

exports.VERSIONINFO = "SERP/SEO tools integration (serp.js) version 0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
