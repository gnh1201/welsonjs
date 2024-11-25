// chatgpt.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
//
// ***SECURITY NOTICE***
// ChatGPT requires an internet connection, and data may be transmitted externally. Users must adhere to the terms of use and privacy policy.
// - Privacy Policy: https://openai.com/policies/row-privacy-policy/
//
var FILE = require("lib/file");
var HTTP = require("lib/http");

function loadApiKey() {
    var s = FILE.readFile("data/chatgpt-apikey.txt", FILE.CdoCharset.CdoUTF_8);
    return s.trim();
}

function chat(content) {
    var s = '';

    var apikey = loadApiKey();
    console.log("ChatGPT API KEY:", apikey);
    
    var response = HTTP.create("MSXML")
        .setVariables({
            "OPENAI_API_KEY": apikey
        })
        .setHeaders({
            "Content-Type": "application/json",
            "Authorization": "Bearer {OPENAI_API_KEY}"
        })
        .setRequestBody({
            "model": "gpt-3.5-turbo",
            "messages": [
              {
                "role": "user",
                "content": content
              }
            ]
        })
        .open("post", "https://api.openai.com/v1/chat/completions")
        .send()
        .responseBody
    ;
    
    if (response.choices.length > 0) {
        s += response.choices[0].message.content;
    }

    return s;
}

exports.chat = chat;

exports.VERSIONINFO = "ChatGPT interface (chatgpt.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
