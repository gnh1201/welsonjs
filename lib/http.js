////////////////////////////////////////////////////////////////////////
// HTTP API
////////////////////////////////////////////////////////////////////////
var FILE = require("lib/file");
var SHELL = require("lib/shell");
var RAND = require("lib/rand");

var HTTPObject = function(engine) {
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
    this.proxy = {
        "enabled": false,
        "protocol": "http",
        "host": "127.0.0.1",
        "port": 80,
        "credential": null    // { username: "user", password: "pass" }
    };
    this.engine = (typeof(engine) !== "undefined" ? engine : "MSXML");

    this.cookie = null;
    this.states = [];

    this.create = function() {
        if (this.engine == "MSXML") {
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
        } else if (this.engine == "CURL") {
            this.interface = SHELL.create();
        }
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
            } else if (this.engine == "MSXML") {
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

    this.setEngine = function(engine) {
        if (typeof(engine) == "string") {
            this.engine = engine.toUpperCase();
        } else {
            this.engine = engine;
        }
        return this;
    };
    
    this.setProxy = function(proxy) {
        for (var k in proxy) {
            if (k in this.proxy) {
                this.proxy[k] = proxy[k];
            }
        }
        return this;
    };

    this.setMethod = function(method) {
        this.method = method;
        return this;
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
    
    this.setCookie = function(cookie) {
        this.cookie = cookie;
    };

    this.setHeaders = function(headers) {
        try {
            var headers = (typeof(headers) !== "undefined") ? headers : {};
            for (var key in headers) {
                var value = headers[key];

                switch (key.toUpperCase()) {
                    case "CONTENT-TYPE":
                        this.setContentType(value.toLowerCase());
                        break;
                    case "COOKIE":
                        this.setCookie(value);
                        break;
                    default:
                        this.setHeader(key, value);
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
        return this;
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

    this.serializeURL = function(parameters) {
        var s = [];
        for (var k in parameters) {
            if (parameters.hasOwnProperty(k)) {
                s.push(encodeURIComponent(k) + "=" + encodeURIComponent(parameters[k]));
            }
        }
        return s.join("&");
    };

    // Type 1: http://domain?a=1&b=2&c=3
    // Type 2: http://domain/:a/:b/:c
    this.serializeParameters = function(url) {
        console.log(Object.keys(this.parameters).join(","));

        // Bind parameters
        if (Object.keys(this.parameters).length > 0) {
            // Type 2
            var parameters = {};
            for (var k in this.parameters) {
                if (url.indexOf(':' + k) > -1) {
                    url = url.replace(':' + k, this.parameters[k]);
                } else if(this.parameters[k] == "{uuidv4}") {
                    parameters[k] = RAND.uuidv4();   // Generate UUID v4
                } else {
                    parameters[k] = this.parameters[k];
                }
            }

            // Type 1
            if (Object.keys(parameters).length > 0) {
                if (url.indexOf('?') > -1) {
                    url += '&' + this.serializeURL(parameters);
                } else {
                    url += '?' + this.serializeURL(parameters);
                }
            }
        }

        console.log("Requested URL: " + url);

        return url;
    };

    this.open = function(method, url) {
        var url = this.serializeParameters(url);

        this.setMethod(method.toUpperCase());   // set method
        this.setHeader("User-Agent", (this.userAgent != null ? this.userAgent : ''));   // set user agent
        this.pushState(null, null, url);   // push state

        try {
            if (this.engine == "MSXML") {
                switch (this.method) {
                    case "POST":
                        this.interface.open(method, url, this.isAsync);
                        break;

                    case "GET":
                        this.interface.open(method, url, this.isAsync);
                        break;

                    default:
                        console.error("Not supported method in MSXML: " + method);
                }
            } else {
                console.log("Opened engine:", this.engine);
            }
        } catch (e) {
            console.error("HTTPObject.open() ->", e.message);
        }

        return this;
    };

    this.send = function(callback) {
        var responseText = null;

        if (this.contentType != null) {
            this.setHeader("Content-Type", this.contentType);
        }

        try {
            if (this.engine == "MSXML") {
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
                while (this.interface.readyState < 4) sleep(100);

                // Get response text
                responseText = this.interface.responseText;
            } else if (this.engine == "CURL") {
                if (this.states.length > 0) {
                    // Make CURL context
                    var state = this.states[this.states.length - 1];
                    var cmd = ["bin\\curl", "-X", this.method];
                    var url = state.url;

                    if (Object.keys(this.headers).length > 0) {
                        for (var key in this.headers) {
                            var value = this.headers[key];
                            if (value != null) {
                                cmd.push("-H");
                                cmd.push(key + ": " + value);
                            }
                        }
                    }
                    
                    if (this.cookie != null) {
                        cmd.push("-b");
                        cmd.push(this.cookie);
                    }

                    cmd.push("-A");
                    cmd.push((this.userAgent != null ? this.userAgent : ''));

                    // Add the request body if this is not GET method
                    if (this.method !== "GET") {
                        cmd.push("-d");
                        cmd.push(this.requestBody);
                    }
                    
                    // Add proxy: <[protocol://][user:password@]proxyhost[:port]>
                    if (this.proxy != null && this.proxy.enabled) {
                        cmd.push("-x");
                        if (this.proxy.credential != null) {
                            cmd.push([
                                this.proxy.protocol,
                                "://",
                                this.proxy.credential.username,
                                ":",
                                this.proxy.credential.password,
                                "@",
                                this.proxy.host,
                                ":",
                                this.proxy.port
                            ].join(""));
                        } else {
                            cmd.push([
                                this.proxy.protocol,
                                "://",
                                this.proxy.host,
                                ":",
                                this.proxy.port
                            ].join(""));
                        }
                    }

                    cmd.push(state.url);

                    // Get response text
                    responseText = this.interface.exec(cmd);
                }
            }

            console.log("ResponseText: " + responseText);

            if (this.isJSONResponse()) {
                if (typeof(WScript) !== "undefined") {
                    JSON = {};
                    FILE.includeFile("app/assets/js/json2.js");
                }
                this.setResponseBody(JSON.parse(responseText));
            } else {
                this.setResponseBody(responseText);
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
    
    this.pushState = function(state, title, url) {
        this.states.push({
            "state": state,
            "title": title,
            "url": url
        });
    };

    this.popState = function() {
        return this.states.pop();
    };

    this.create();
};

exports.create = function(engine) {
    return (new HTTPObject(engine));
};

exports.post = function(url, data, headers, params) {
    return (new HTTPObject()).setHeaders(headers).setRequestBody(data).setParameters(params).post(url).responseBody;
};

exports.get = function(url, data, headers) {
    return (new HTTPObject()).setHeaders(headers).setParameters(data).setUseCache(false).get(url).responseBody;
};

exports.VERSIONINFO = "HTTP Lib (http.js) version 0.3";
exports.global = global;
exports.require = global.require;
