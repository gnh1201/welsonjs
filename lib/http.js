////////////////////////////////////////////////////////////////////////
// HTTP API
////////////////////////////////////////////////////////////////////////

//var WINSOCK = require("lib/winsock");

var HTTPObject = function() {
    this.interface = null;
    this.contentType = "application/octet-stream";
    this.requestBody = "";
    this.responseBody = null;
    this.method = "";
    this.headers = {};
    this.dataType = "";
    this.userAgent = "WelsonJS/0.1.4-dev (https://github.com/gnh1201/welsonjs)";

    this.create = function() {
        this.interface = CreateObject([
            "Microsoft.XMLHTTP",
            "WinHttp.WinHttpRequest.5.1",
            "Msxml3.XMLHTTP",
            "Msxml2.XMLHTTP",
            "Msxml2.XMLHTTP.7.0",
            "Msxml2.XMLHTTP.6.0",
            "Msxml2.XMLHTTP.5.O",
            "Msxml2.XMLHTTP.4.O",
            "Msxml2.XMLHTTP.3.O",
            "Msxml2.XMLHTTP.2.6",
            "Msxml2.ServerXMLHTTP",
            "Msxml2.ServerXMLHTTP.6.0",
            "Msxml2.ServerXMLHTTP.5.0",
            "Msxml2.ServerXMLHTTP.4.0",
            "Msxml2.ServerXMLHTTP.3.0"
        ], function(obj, progId) {
            switch (progId) {
                case "WinHttp.WinHttpRequest.5.1":
                    obj.setTimeouts(30000, 30000, 30000, 0);
                    break;
            }
        });
        return this;
    }

    this.isJSONRequest = function() {
        return (this.contentType === "application/json");
    };

    this.isJSONResponse = function() {
        if (this.dataType === "json") {
            return true;
        } else {
            var headers = this.getHeaders();
            for (var key in headers) {
                var _k = key.toLowerCase();
                var _v = headers[key].toLowerCase();
                if (_k === "content-type" && _v === "application/json") {
                    return true;
                }
            }
        }

        return false;
    };

    this.setMethod = function(method) {
        this.method = method;
    };

    this.setDataType = function(type) {
        this.dataType = type;
        return this;
    };

    this.setContentType = function(type) {
        this.contentType = type;
        return this;
    };

    this.setRequestBody = function(data) {
        this.requestBody = data;
        return this;
    }

    this.setResponseBody = function(data) {
        this.responseBody = data;
        return this;
    }

    this.setHeader = function(key, value) {
        this.headers[key] = value;
        return this;
    };

    this.setHeaders = function(headers) {
        var headers = (typeof(headers) !== "undefined") ? headers : {};
        for (var key in headers) {
            var value = headers[key];
            this.setHeader(key, value);
            if (key.toLowerCase() == "content-type") {
                this.contentType = value.toLowerCase();
            }
        }

        return this;
    };

    this.getHeader = function(key) {
        return this.interface.getResponseHeader(key);
    };

    this.getHeaders = function() {
        var raw = this.interface.getAllResponseHeaders();

        return raw.split(/[\r\n]+/).filter(function(s) {
            return s.trim().length > 0;
        }).map(function(s) {
            return s.trim().split(": ");
        }).reduce(function(acc, c) {
            acc[c[0].trim()] = c[1].trim();
            return acc;
        }, {});
    };

    this.setBearerAuth = function(token) {
        this.setHeader("Authorization", "Bearer " + token);
        return this;
    };

    this.setUseCache = function(flag) {
        if (flag === false) {
            this.setHeaders({
                //"Pragma": "no-cache",
                //"Cache-Control": "no-cache",
                "If-Modified-Since": "Sat, 1 Jan 2000 00:00:00 GMT"
            });
        }
        return this;
    };

    this.setUserAgent = function(agent) {
        this.userAgent = agent;
        this.setHeader("User-Agent", this.userAgent);
    };

    this.open = function(method, url, isAsync) {
        this.setMethod(method.toUpperCase());

        switch (this.method) {
            case "POST":
                this.interface.open(method, url, isAsync);
                break;

            case "GET":
                this.interface.open(method, url, isAsync);
                break;

            case "PATCH":
                break;

            default:
                console.error("Not supported HTTP method: " + method);
        }

        return this;
    };

    this.send = function(callback) {
        this.setHeader("Content-Type", this.contentType);

        for (var key in this.headers) {
            this.interface.setRequestHeader(key, this.headers[key]);
        }

        if (this.isJSONRequest() && typeof(this.requestBody) === "object") {
            this.interface.send(JSON.stringify(this.requestBody));
        } else {
            this.interface.send(this.requestBody);
        }

        if (this.isJSONResponse()) {
            this.setResponseBody(JSON.parse(this.interface.responseText));
        } else {
            this.setResponseBody(this.interface.responseText);
        }

        if (typeof(callback) === "function") {
            callback(this.responseBody);
        }

        return this;
    };

    this.post = function(url, callback) {
        return this.open("POST", url, false).send(callback);
    };

    this.get = function(url, callback) {
        return this.open("GET", url, false).send(callback);
    };

    this.patch = function(url, callback) {
        var options = {
            type: "PATCH",
            headers: this.headers,
            url: url,
            data: this.requestBody,
            contentType: this.contentType,
            success: callback
        };

        if (typeof(window) !== "undefined") {
            if (typeof(window.jQuery) !== "undefined") {
                window.jQuery.ajax(options);
            } else {
                console.error("PATCH is required jQuery library");
            }
        } else {
            console.error("PATCH dose not supported on console mode");
        }

        return this;
    };

    this.create();
    this.setUserAgent(this.userAgent);
};

exports.create = function() {
    return (new HTTPObject());
};

exports.post = function(url, data, headers) {
    return (new HTTPObject()).setHeaders(headers).setRequestBody(data).post(url).responseBody;
}

exports.get = function(url, data, headers) {
    return (new HTTPObject()).setHeaders(headers).setRequestBody(data).get(url).responseBody;
}

exports.VERSIONINFO = "HTTP Lib (http.js) version 0.2";
exports.global = global;
exports.require = global.require;
