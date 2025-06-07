// anthropic.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// SECURITY NOTICE
// Anthropic requires an internet connection, and data may be transmitted externally. Please check the terms of use and privacy policy.
// https://www.anthropic.com/legal/privacy
// 
var FILE = require("lib/file");
var HTTP = require("lib/http");
var CRED = require("lib/credentials");

function chat(content) {
    var answers = [];

    var apikey = CRED.get("apikey", "anthropic");
    console.log("Anthropic (Claude) API KEY:", apikey);

    var response = HTTP.create("MSXML")
        .setVariables({
            "ANTHROPIC_API_KEY": apikey
        })
        .setContentType("application/json")
        .setHeaders({
            "x-api-key": "{ANTHROPIC_API_KEY}",
            "anthropic-version": "2023-06-01"
        })
        .setRequestBody({
            "model": "claude-3-5-sonnet-20241022",
            "max_tokens": 1024,
            "messages": [{
                "role": "user",
                "content": content
            }]
        })
        .open("post", "https://api.anthropic.com/v1/messages")
        .send()
        .responseBody;

    if ("error" in response) {
        answers.push("Error: " + response.error.message);
    } else if ("content" in response && response.content.length > 0) {
        response.content.forEach(function(x) {
            if (x.type == "text") {
                answers.push(x.text);
            } else {
                answers.push("Not supported type: " + x.type);
            }
        });
    }

    return answers.join(' ');
}

exports.chat = chat;

exports.VERSIONINFO = "Anthropic (Claude) interface (anthropic.js) version 0.1.4";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
