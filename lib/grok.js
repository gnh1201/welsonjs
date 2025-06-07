// grok.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// SECURITY NOTICE
// Grok requires an internet connection, and data may be transmitted externally. Please check the terms of use and privacy policy.
// https://x.ai/legal/privacy-policy
// 
var FILE = require("lib/file");
var HTTP = require("lib/http");
var CRED = require("lib/credentials");

function chat(content) {
    var answers = [];

    var apikey = CRED.get("apikey", "grok");
    console.log("Grok (x.ai) API KEY:", apikey);

    var response = HTTP.create("MSXML")
        .setVariables({
            "GROK_API_KEY": apikey
        })
        .setHeaders({
            "Content-Type": "application/json",
            "Authorization": "Bearer {GROK_API_KEY}"
        })
        .setRequestBody({
            "messages": [{
                "role": "user",
                "content": content
            }],
            "model": "grok-2-latest"
        })
        .open("post", "https://api.x.ai/v1/chat/completions")
        .send()
        .responseBody
    ;

    if ("choices" in response && response.choices.length > 0) {
        response.choices.forEach(function(x) {
            answers.push(x.message.content);
        });
    }

    return answers.join(' ');
}

exports.chat = chat;

exports.VERSIONINFO = "Grok (x.ai) interface version 0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
