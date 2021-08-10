////////////////////////////////////////////////////////////////////////
// HTTP API
////////////////////////////////////////////////////////////////////////
var FILE = require("lib/file");

var HTTPObject = function() {
    this.interface = null;
    this.contentType = "application/x-www-form-urlencoded";
    this.requestBody = "";
    this.responseBody = null;
    this.method = "GET";
    this.headers = {};
    this.parameters = {};
    this.dataType = null;
    this.userAgent = "WelsonJS/0.1.4-dev (https://github.com/gnh1201/welsonjs)";
    this.isAsync = false;

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
        ]);
        return this;
    };

    this.jqEnabled = function() {
        return (typeof(window) !== "undefined" && typeof(window.jQuery) !== "undefined");
    };

    this.jqAjax = function(options) {
        return (this.jqEnabled() ? window.jQuery.ajax(options) : null);
    };

    this.isJSONRequest = function() {
        return (this.contentType === "application/json");
    };

    this.isJSONResponse = function() {
        try {
            if (this.dataType === "json") {
                return true;
            } else {
                var headers = this.getHeaders();
                for (var key in headers) {
                    var _k = key.toLowerCase();
                    var _v = headers[key].toLowerCase();
                    if (_k === "content-type" && _v === "application/json") {
                        this.dataType = "json";
                        return true;
                    }
                }
            }
        } catch (e) {
            console.error("HTTPObject.isJSONResponse() -> ", e.message);
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
        try {
            var headers = (typeof(headers) !== "undefined") ? headers : {};
            for (var key in headers) {
                var value = headers[key];
                this.setHeader(key, value);
                if (key.toLowerCase() == "content-type") {
                    this.contentType = value.toLowerCase();
                }
            }
        } catch (e) {
            console.error("HTTPObject.setHeaders() -> ", e.message);
        }

        return this;
    };

    this.getHeader = function(key) {
        try {
            return this.interface.getResponseHeader(key);
        } catch (e) {
            console.error("HTTPObject.getHeader() -> ", e.message);
        }
    };

    this.getHeaders = function() {
        try {
            var raw = this.interface.getAllResponseHeaders();

            return raw.split(/[\r\n]+/).filter(function(s) {
                return s.trim().length > 0;
            }).map(function(s) {
                return s.trim().split(": ");
            }).reduce(function(acc, c) {
                acc[c[0].trim()] = c[1].trim();
                return acc;
            }, {});
        } catch (e) {
            console.error("HTTPObject.getHeaders() -> ", e.message);
        }
    };

    this.setParameter = function(key, value) {
        this.parameters[key] = value;
        return this;
    };

    this.getParameter = function(key) {
        return this.parameters[key];
    };

    this.setParameters = function(params) {
        try {
            var params = (typeof(params) !== "undefined") ? params : {};
            for (var key in params) {
                var value = params[key];
                this.setParameter(key, value);
            }
        } catch (e) {
            console.error("HTTPObject.setParameters() -> ", e.message);
        }
        return this;
    };

    this.getParameters = function() {
        return this.parameters;
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

    this.setUseAsync = function(flag) {
        this.isAsync = flag;
        return this;
    }

    this.setUserAgent = function(agent) {
        this.userAgent = agent;
        this.setHeader("User-Agent", this.userAgent);
    };

    this.serialize = function() {
        if (this.isJSONRequest() && typeof(this.requestBody) === "object") {
            return JSON.stringify(this.requestBody);
        } else if (typeof(requestBody) === "object") {
            return this.serializeURL(this.requestBody);
        } else {
            return this.requestBody;
        }
    };

    this.serializeURL = function(obj) {
        var str = [];
        for (var p in obj)
        if (obj.hasOwnProperty(p)) {
            str.push(encodeURIComponent(p) + "=" + encodeURIComponent(obj[p]));
        }
        return str.join("&");
    };

    this.serializeParameters = function(url) {
        if (Object.keys(this.parameters).length > 0) {
            if (url.indexOf('?') > -1) {
                return url + '&' + this.serializeURL(this.parameters);
            } else {
                return url + '?' + this.serializeURL(this.parameters);
            }
        } else {
            return url;
        }
    };

    this.open = function(method, url) {
        this.setMethod(method.toUpperCase());

        try {
            switch (this.method) {
                case "POST":
                    this.interface.open(method, this.serializeParameters(url), this.isAsync);
                    break;

                case "GET":
                    this.interface.open(method, this.serializeParameters(url), this.isAsync);
                    break;

                case "PATCH":
                    break;

                default:
                    console.error("Not supported HTTP method: " + method);
            }
        } catch (e) {
            console.error("HTTPObject.open() -> ", e.message);
        }

        return this;
    };

    this.send = function(callback) {
        this.setHeader("Content-Type", this.contentType);

        try {
            for (var key in this.headers) {
                this.interface.setRequestHeader(key, this.headers[key]);
            }
            
            switch (this.method) {
                case "GET":
                    this.interface.send();
                    break;

                default:
                    this.interface.send(this.serialize(this.requestBody));
            }

            // Waiting a response
            while (this.interface.readyState < 4) {
                sleep(100);
            }

            if (this.isJSONResponse()) {
                if (typeof(WScript) !== "undefined") {
                    JSON = {};
                    FILE.includeFile("app/assets/js/json2.js");
                }
                this.setResponseBody(JSON.parse(this.interface.responseText));
            } else {
                this.setResponseBody(this.interface.responseText);
            }

            if (typeof(callback) === "function") {
                try {
                    callback(this.responseBody);
                } catch (e) {
                    console.log("callback of HTTPObject.send() -> ", e.message);
                }
            }
        } catch (e) {
            console.error("HTTPObject.send() -> ", e.message);
        }

        return this;
    };

    this.post = function(url, callback) {
        try {
            return this.open("POST", url).send(callback);
        } catch (e) {
            console.error("HTTPObject.post() -> ", e.message);
        }
    };

    this.get = function(url, callback) {
        try {
			if (this.jqEnabled()) {
				return this.jqAjax({
                    type: "GET",
                    headers: this.headers,
                    url: this.serializeParameters(url),
                    data: this.requestBody,
                    contentType: this.contentType,
                    success: callback,
                    async: this.isAsync,
					error: function(request, status, error) {
						console.error("code: ", request.status, ", message: " + request.responseText + ", error: " + error);
					}
				});
			} else {
				return this.open("GET", url).send(callback);
			}
        } catch (e) {
            console.error("HTTPObject.get() -> ", e.message);
        }
    };

    this.patch = function(url, callback) {
        try {
            if (this.jqEnabled()) {
                return this.jqAjax({
                    type: "PATCH",
                    headers: this.headers,
                    url: this.serializeParameters(url),
                    data: this.requestBody,
                    contentType: this.contentType,
                    success: callback,
                    async: this.isAsync,
                    error: function(request, status, error) {
						console.error("code: ", request.status, ", message: " + request.responseText + ", error: " + error);
					}
                });
            } else {
                throw Error("PATCH does not supported on GUI mode");
            }
        } catch (e) {
            console.error("HTTPObject.patch() -> ", e.message);
        }
    };

    this.create();
    this.setUserAgent(this.userAgent);
};

exports.create = function() {
    return (new HTTPObject());
};

exports.post = function(url, data, headers, params) {
    return (new HTTPObject()).setHeaders(headers).setRequestBody(data).setParameters(params).post(url).responseBody;
};

exports.get = function(url, data, headers) {
    return (new HTTPObject()).setHeaders(headers).setParameters(data).setUseCache(false).get(url).responseBody;
};

exports.VERSIONINFO = "HTTP Lib (http.js) version 0.2";
exports.global = global;
exports.require = global.require;
