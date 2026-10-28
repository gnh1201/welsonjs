// groq.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// SECURITY NOTICE
// Groq requires an internet connection, and data may be transmitted externally. Please check the terms of use and privacy policy.
// https://groq.com/privacy-policy/
//
var FILE = require("lib/file");
var HTTP = require("lib/http");
var CRED = require("lib/credentials");

function chat(content) {
    return require("lib/language-inference-engine").create()
        .setProvider("groq").inference(content, 0).join(" ");
}

exports.chat = chat;

exports.VERSIONINFO = "Groq (GroqCloud) interface (groq.js) version 0.1.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
