// groq.js
// https://github.com/gnh1201/welsonjs
// Groq API documentation: https://console.groq.com/docs/api-reference
var FILE = require("lib/file");
var HTTP = require("lib/http");

function loadApiKey() {
    var s = FILE.readFile("data/groq-apikey.txt", FILE.CdoCharset.CdoUTF_8);
    return s.trim();
}

function chat(content) {
    var answers = [];

    var apikey = loadApiKey();
    console.log("Groq (GroqCloud) API KEY:", apikey);

    var response = HTTP.create("MSXML")
        .setContentType("application/json")
        .setBearerAuth(apikey)
        .setRequestBody({
            "model": "llama3-8b-8192",
            "messages": [{
                "role": "user",
                "content": content
            }]
        })
        .open("post", "https://api.groq.com/openai/v1/chat/completions")
        .send()
        .responseBody;

    if ("error" in response) {
        answers.push("Error: " + response.error.message);
    } else if (response.choices.length > 0) {
        answers.push(response.choices[0].message.content);
    }

    return answers.join(' ');
}

exports.chat = chat;

exports.VERSIONINFO = "Groq (GroqCloud) interface (groq.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
