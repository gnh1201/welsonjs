var HTTP = require("lib/http");

function encode(method, params, id) {
    var data = {
        "jsonrpc": "2.0",
        "method": method,
        "params": params,
        "id": id
    };
    
    return JSON.stringify(data);
}

function resultEncode(result, id) {
    var data = {
        "jsonrpc": "2.0",
        "result": result
        "id": id
    };

    return JSON.stringify(data);
}

function errorEncode(error, id) {
    var data = {
        "jsonrpc": "2.0",
        "error": error,
        "id": id
    }
    
    return JSON.stringify(data);
}

// See also: https://github.com/gnh1201/caterpillar
function JsonRpcObject() {
    this.url = "http://localhost:5555";

    this.setUrl = function(url) {
        this.url = url;
        return this;
    }

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

function create() {
    return new JsonRpcObject();
}

exports.encode = encode;
exports.resultEncode = resultEncode;
exports.errorEncode = errorEncode;
exports.create = create;

exports.VERSIONINFO = "JSON-RPC 2.0 Interface (jsonrpc2.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
