// coupang.js
// Coupang SERP API Client
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var HTTP = require("lib/http");

function search(keyword, limit) {
    limit = (typeof limit !== "number" && limit > 0 ? 10 : limit);

    return HTTP.create()
        .setParameters({
            "keyword": keyword,
            "limit": limit
        })
        .setDataType("json")
        .open("GET", "https://cold-math-31f3.gnh1201.workers.dev/api/v1/products/search")
        .send()
        .responseBody
    ;
}

exports.search = search;

exports.VERSIONINFO = "Coupang SERP API Client (coupang.js) version 1.0";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
