// chatgpt.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// SECURITY NOTICE
// ChatGPT requires an internet connection, and data may be transmitted externally. Please check the terms of use and privacy policy.
// https://openai.com/policies/row-privacy-policy/
// 
var FILE = require("lib/file");
var HTTP = require("lib/http");
var CRED = require("lib/credentials");

function chat(content) {
    var answers = [];
    
    var apikey = CRED.get("apikey", "chatgpt");
    console.log("OpenAI (ChatGPT) API KEY:", apikey);
    
    var response = HTTP.create("MSXML")
        .setVariables({
            "OPENAI_API_KEY": apikey
        })
        .setHeaders({
            "Content-Type": "application/json",
            "Authorization": "Bearer {OPENAI_API_KEY}"
        })
        .setRequestBody({
            "model": "gpt-4o",
            "messages": [
                {
                    "role": "developer",
                    "content": "Write all future code examples in JavaScript ES3 using the exports variable. Include a test method with the fixed name test. Respond exclusively in code without blocks."
                }, {
                    "role": "user",
                    "content": content
                }
            ]
        })
        .open("post", "https://api.openai.com/v1/chat/completions")
        .send()
        .responseBody
    ;
    
    if ("choices" in response && response.choices.length > 0) {
        response.choices.forEach(function(x) {
            answers.push(x.message.content);
        });
    }

    return s;
}

exports.chat = chat;

exports.VERSIONINFO = "OpenAI (ChatGPT) interface version 0.1.2";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
