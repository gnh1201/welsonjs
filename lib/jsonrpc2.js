// jsonrpc2.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
var HTTP = require("lib/http");

function jsonrpc2(url) {
    this.url = url;
    this.call = function(method, params, id) {
        var result;
        var response = HTTP.create("MSXML")
            .setContentType("application/json")
            .setDataType("json")
            .setRequestBody(encode(method, params, id))
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

function encode(method, params, id) {
    return JSON.stringify({
        "jsonrpc": "2.0",
        "method": method,
        "params": params,
        "id": id
    });
}

function create(url) {
    return new jsonrpc2(url);
}

exports.encode = encode;
exports.create = create;

exports.VERSIONINFO = "JSON-RPC 2.0 Interface (jsonrpc2.js) version 0.1.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
