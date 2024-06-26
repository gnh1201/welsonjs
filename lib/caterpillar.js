// caterpillar.js
// Integration with [Caterpillar Proxy](https://github.com/gnh1201/caterpillar) project
// https://github.com/gnh1201/welsonjs 
var JSONRPC2 = require("lib/jsonrpc2");

function Caterpillar(url) {
    var rpc = JSONRPC2.create(url);
    var env = {
        "target": "http://localhost/",
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
    this._do = function() {
        var args = arguments;

        if (env.method == "relay_mysql_query") {
            var query = arguments.join(' ');
            rpc._call(env.method, {
                "hostname": env.mysql_hostname,
                "username": env.mysql_username,
                "password": env.mysql_password,
                "database": env.mysql_database,
                "port": env.mysql_port,
                "charset": env.mysql_charset,
                "query": query
            }, null);
            return;
        }

        rpc._call(env.method, {}, null);
    }
}

function create(url) {
    return new Caterpillar(url);
}

exports.create = create;

exports.VERSIONINFO = "Caterpillar Proxy Integration (caterpillar.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
