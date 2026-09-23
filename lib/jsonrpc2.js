// jsonrpc2.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// JSON-RPC 2.0 wrapper for WelsonJS framework
// 
function JsonRpc2(url) {
    this.url = url || null;
    this.userAgent = "php-httpproxy/0.1.5 (Client; WelsonJS)";
    this.methods = [];
    
    this.setUserAgent = function(agent) {
        this.userAgent = agent;
    };
    
    this.register = function(method, callback) {
        if (!method || typeof method !== "string") {
            throw new Error("Invalid method");
        }
        
        if (typeof callback !== "function") {
            throw new Error("Invalid callback");
        }
        
        this.methods.push({
            method: method,
            callback: callback
        });
    };
    
    this.invoke = function(method, params, id) {
        var result;
        
        if (!this.url) {
            throw new Error("Invalid URL");
        }
        
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
    };
    
    this.dispatch = function(message) {
        var methods = this.methods;
        
        return extract(message, function(method, params, id) {
            var i = 0;
            var found = false;
            var result;
            
            while (!found && i < methods.length) {
                if (methods[i].method === method && typeof methods[i].callback === "function") {
                    found = true;
                    result = methods[i].callback(params, id);
                }
                i++;
            }
            
            if (!found) {
                throw new Error("Method not found: " + method);
            }

            return result;
        });
    };
}

function extract(message, callback) {
    var data;
    
    if (typeof callback !== "function") {
        throw new Error("Invalid callback");
    }
    
    try {
        data = JSON.parse(message);
    } catch (e) {
        throw new Error("Invalid JSON: " + e.message);
    }
    
    if (!data || typeof data !== "object") {
        throw new Error("Invalid request object");
    }
    
    if (data.jsonrpc !== "2.0") {
        throw new Error("Invalid JSON-RPC version");
    }
    
    if (!data.method || typeof data.method !== "string") {
        throw new Error("Missing or invalid method");
    }
    
    var params = data.params !== undefined ? data.params : null;
    var id = data.id !== undefined ? data.id : null;
    
    try {
        var result = callback(data.method, params, id);
        
        // Return the result as a JSON-RPC 2.0 response unless it is false
        if (result !== false) {
            return {
                jsonrpc: "2.0",
                result: result === undefined ? null : result,
                id: id
            };
        }
        
        return result;
    } catch (e) {
        return {
            jsonrpc: "2.0",
            error: {
                code: -32603,
                message: e && e.message ? e.message : "Internal error"
            },
            id: id
        };
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

function register(method, callback) {
    DEFAULT_JSONRPC2.register(method, callback);
}

function dispatch(message) {
    return DEFAULT_JSONRPC2.dispatch(message);
}

var DEFAULT_JSONRPC2_URL = "http://localhost:5555";
var DEFAULT_JSONRPC2 = new JsonRpc2();

exports.extract = extract;
exports.wrap = wrap;
exports.create = create;
exports.register = register;
exports.dispatch = dispatch;

exports.DEFAULT_JSONRPC2_URL = DEFAULT_JSONRPC2_URL;

exports.VERSIONINFO = "JSON-RPC 2.0 wrapper (jsonrpc2.js) version 0.1.8";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
