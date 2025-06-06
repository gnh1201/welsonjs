// catproxy.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// Caterpillar Proxy Integration for WelsonJS framework
// 
var JSONRPC2 = require("lib/jsonrpc2");

function CatProxyClient(url) {
    var env = {
        "target": "http://localhost:5555/jsonrpc2",    // Check this: https://github.com/gnh1201/caterpillar
        "method": ""
    };

    this.set_env = function(k, v) {
        env[k] = v || null;
    };
    this.set_default_env = function(_env) {
        for (k in _env) {
            if (!(k in env)) {
                env[k] = _env[k];
            }
        }
    };
    this.set_method = function(method) {
        this.set_env("method", method);

        if (env.method == "relay_mysql_query") {
            this.set_default_env({
                "mysql_hostname": "localhost",
                "mysql_username": "root",
                "mysql_password": null,
                "mysql_database": null,
                "mysql_port": "3306",
                "mysql_charset": "utf8"
            });
            return;
        }
    }
    this.exec = function(params) {
        var args = Array.from(arguments);
        var rpc = JSONRPC2.create(env.target);
        var result;

        if (env.method == "relay_mysql_query") {
            var query = args.join(' ');
            result = rpc.invoke(env.method, {
                "hostname": env.mysql_hostname,
                "username": env.mysql_username,
                "password": env.mysql_password,
                "database": env.mysql_database,
                "port": env.mysql_port,
                "charset": env.mysql_charset,
                "query": query
            }, null);
        } else {
            result = rpc.invoke(env.method, params, null);
        }

        return result;
    }

    if (typeof url !== "undefined") {
        this.set_env(url);
    }
}

function create(url) {
    return new CatProxyClient(url);
}

exports.create = create;
exports.CatProxyClient = CatProxyClient;

exports.VERSIONINFO = "Caterpillar Proxy Integration (caterpillar.js) version 0.1.5";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
