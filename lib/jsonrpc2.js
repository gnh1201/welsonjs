// jsonrpc2.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// JSON-RPC 2.0 wrapper for WelsonJS framework
// 
function JsonRpc2(url) {
    this.url = url;
    this.userAgent = "php-httpproxy/0.1.5 (Client; WelsonJS)";
    
    this.setUserAgent = function(agent) {
        this.userAgent = agent;
    };
    
    this.invoke = function(method, params, id) {
        var result;
        var response = require("lib/http").create("MSXML")
            .setContentType("application/json")
            .setDataType("json")
            .setUserAgent(this.userAgent)
            .setRequestBody(wrap(method, params, id))
            .open("POST", this.url)
            .send()
            .responseBody
        ;

        if ("error" in response) {
            console.error(response.error.message);
            return;
        }

        if ("result" in response) {
            result = response.result;
        }

        return result;
    }    
}

function wrap(method, params, id) {
    return {
        "jsonrpc": "2.0",
        "method": method,
        "params": params,
        "id": id
    };
}

function create(url) {
    return new JsonRpc2(url);
}

var DEFAULT_JSONRPC2_URL = "https://azure-ashlan-40.tiiny.io/";

exports.wrap = wrap;
exports.create = create;

exports.DEFAULT_JSONRPC2_URL = DEFAULT_JSONRPC2_URL;

exports.VERSIONINFO = "JSON-RPC 2.0 wrapper (jsonrpc2.js) version 0.1.5";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
