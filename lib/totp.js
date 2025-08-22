// totp.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// To use this feature, you’ll need software that can generate Time-based OTPs (HMACSHA1 algorithm, supporting 16- or 32-character keys).
// If the WelsonJS Launcher is running, you can access this feature right away.
// 
var HTTP = require("lib/http");

var TFA_API_BASE_URL = "http://localhost:3000/tfa";

function getPubKey() {
    var response = HTTP.create()
        .open("GET", TFA_API_BASE_URL + "/pubkey")
        .send()
        .responseBody
    ;
    return response.trim();
}

function getOtp(pubkey) {
    var response = HTTP.create()
        .setRequestBody({
            "secret": pubkey
        })
        .open("POST", TFA_API_BASE_URL + "/otp")
        .send()
        .responseBody
    ;
    return response.trim();
}

exports.getPubKey = getPubKey;
exports.getOtp = getOtp;

exports.VERSIONINFO = "Time-based OTP client (totp.js) version 1.0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;

/*
// Example:
var TOTP = require("lib/totp");
console.log(TOTP.getPubKey()); // get public key. e.g. ih6vdfuh75ugdcmruaexhfh3miiwdqhx
console.log(TOTP.getOtp('ih6vdfuh75ugdcmruaexhfh3miiwdqhx')); // get OTP code. (32 characters key) e.g. 774372
console.log(TOTP.getOtp('6Y4R 3AQN 4TTV CEQT')); // get OTP code. (16 characters key) e.g. 317884
*/
