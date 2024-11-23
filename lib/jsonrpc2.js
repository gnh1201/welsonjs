// jsonrpc2.js
// JSON-RPC 2.0 wrapper for WelsonJS framework
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
var HTTP = require("lib/http");

function JsonRpc2(url) {
    this.url = url;
    this.userAgent = "php-httpproxy/0.1.5 (Client; WelsonJS; abuse@catswords.net)";

    this.setUserAgent = function(agent) {
        this.userAgent = agent;
    };
	
    this.invoke = function(method, params, id) {
        var result;
        var response = HTTP.create("MSXML")
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

exports.wrap = wrap;
exports.create = create;

exports.VERSIONINFO = "JSON-RPC 2.0 wrapper (jsonrpc2.js) version 0.1.4";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
