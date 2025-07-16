// http.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// HTTP REST API client for WelsonJS framework
// 
var SYS = require("lib/system");
var FILE = require("lib/file");
var SHELL = require("lib/shell");
var RAND = require("lib/rand");
var BASE64 = require("lib/base64");
var PipeIPC = require("lib/pipe-ipc");
var JsonRpc2 = require("lib/jsonrpc2");
var SERP = require("lib/serp");

var OS_NAME = SYS.getOS();
var DEVICE_UUID = SYS.getUUID();
var PROCESS_VERSION = SYS.getProcessVersion();
var DEFAULT_USER_AGENT = "WelsonJS/0.2.7 (" + OS_NAME + "; " + PROCESS_VERSION + "; " + DEVICE_UUID + ")";

var AVAILABLE_PROXIES = [
    {
        "type": "file",
        "provider": "",
        "url": "data/available_proxies.json",
        "documentation": ""
    }
];

var HTTPObject = function(engine) {
    this._interface = null;

    this.contentType = "application/x-www-form-urlencoded";
    this.requestBody = null;
    this.responseBody = null;
    this.method = "GET";
    this.headers = {};
    this.parameters = {};
    this.dataType = null;
    this.userAgent = DEFAULT_USER_AGENT;
    this.isAsync = false;
    this.proxy = {
        "enabled": false,
        "type": "stateful",
        "provider": "",
        "protocol": "http",
        "host": "127.0.0.1",
        "port": 80,
        "credential": null, // e.g. { username: "user", password: "pass" }
        "method": null,  // for stateless only. e.g. GET, POST
        "url": null,  // for stateless only. e.g. http://localhost:8080
        "userAgent": "php-httpproxy/0.1.5 (Client; WelsonJS; abuse@catswords.re.kr)",  // for stateless only
        "rpcMethod": "relay_fetch_url"  // for stateless proxy
    };
    this.engine = (typeof(engine) !== "undefined" ? engine : "MSXML");

    this.cookie = null;
    this.storedCookie = PipeIPC.connect("volatile");
    this.states = [];
    this.variables = {
        "uuidv4": RAND.uuidv4,
        "base64json": function(v) { // e.g. {base64json VARIABLE_NAME}
            return BASE64.encode(JSON.stringify(v));
        },
        "unixnow": function(diff) { // e.g. {unixnow -300} (seconds)
            var t = parseInt(diff);
            return Math.floor(new Date().getTime() / 1000) - t;
        },
        "unixnowms": function(diff) { // e.g. {unixnowms -300000} (milliseconds)
            var t = parseInt(diff);
            return Math.floor(new Date().getTime()) - t;
        },
        "isotime": function() {
            return new Date().toISOString();
        }
    };
    this.connectTimeout = 0;
    this.maxTime = 0;
    this.isDebugging = false;
    this.credential = {
        method: "",
        username: "",
        password: ""
    };
    this.isFollowRedirect = true;
    this.saveTo = null;

    this.isLoggingCookie = false;
    this.debuggingText = '';

    this.curlVersion = "8.12.1_4";
    this.curlOptions = [];

    this.charset = FILE.CdoCharset.CdoUTF_8;
    this.isUseDetectCharset = false;
    this.isVerifySSL = true;
    this.isCompressedResponse = false;

    this.create = function() {
        if (this.engine == "MSXML") {
            this._interface = typeof XMLHttpRequest !== "undefined" ? new XMLHttpRequest() : CreateObject([
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
            this._interface = SHELL.create();

            // the location of cURL binary
            var arch = SYS.getArch();
            if (arch.toLowerCase().indexOf("arm") > -1) {
                this.setBinPath("bin\\arm64\\curl-" + this.curlVersion + "-win64a-mingw\\bin\\curl.exe");
            } else if (arch.indexOf("64") > -1) {
                this.setBinPath("bin\\x64\\curl-" + this.curlVersion + "-win64-mingw\\bin\\curl.exe");
            } else {
                this.setBinPath("bin\\x86\\curl-" + this.curlVersion + "-win32-mingw\\bin\\curl.exe");
            }

            // do not clear after calling the `exec`
            this._interface.setIsPreventClear(true);
        } else if (this.engine == "BITS") {
            this._interface = SHELL.create();
            this.setBinPath("bitsadmin.exe");   // the location of BITS binary
        } else if (this.engine == "CERT") {
            this._interface = SHELL.create();
            this.setBinPath("certutil.exe");   // the location of Certutil binary
        }

        return this;
    };

    this.setBinPath = function(binPath) {
        console.info(this.engine, "is use", binPath);
        this._interface.setPrefix(binPath);
    };
    
    this.setCurlVersion = function(curlVersion) {
        this.curlVersion = curlVersion;
        this.setEngine("CURL");
    };

    this.jqEnabled = function() {
        return (typeof(window) !== "undefined" && typeof(window.jQuery) !== "undefined");
    };

    this.jqAjax = function(url, callback, onError) {
        var options = {
            type: this.getMethod(),
            headers: this.headers,
            url: this.serializeParameters(url),
            data: null,
            contentType: this.contentType,
            success: callback,
            async: this.isAsync,
            error: onError  // f(request, status, error)
        };

        if (["POST", "PUT", "PATCH"].indexOf(this.method) > -1) {
            options['data'] = this.serialize();
        }

        this.setResponseBody(window.jQuery.ajax(options).responseText);
    };

    this.isJSONRequest = function() {
        return (this.contentType === "application/json");
    };

    this.isJSONResponse = function() {
        try {
            if (this.dataType === "json") {
                return true;
            } else if (this.engine == "MSXML") {
                var headers = this.getResponseHeaders();
                for (var key in headers) {
                    var header_key = key.toLowerCase();
                    var header_value = headers[key].toLowerCase();
                    if (header_key === "content-type" && header_value === "application/json") {
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
        this.engine = String(engine).toUpperCase();
        this.create();
        return this;
    };

    this.setProxy = function(proxy) {
        // set the proxy provider
        if ("provider" in proxy) {
            var proxyType = proxy['type'] || this.proxy['type'];
            var availableProxy = AVAILABLE_PROXIES.find(function(x) {
                return x.provider == proxy['provider'] && x.type == proxyType;
            });

            if (typeof availableProxy !== "undefined") {
                this.proxy.provider = availableProxy['provider'];
                this.proxy.protocol = availableProxy['protocol'] || this.proxy.protocol;
                this.proxy.host = availableProxy['host'] || this.proxy.host;
                this.proxy.port = availableProxy['port'] || this.proxy.port;
                this.proxy.credential = availableProxy['credential'] || this.proxy.credential;
                this.proxy.url = availableProxy['url'] || this.proxy.url;

                console.info("See the documentation:", availableProxy.documentation);
            }
        }

        // override proxy configurations
        for (var k in proxy) {
            if (k == "provider")
                continue;

            this.proxy[k] = proxy[k];
        }
        
        // When JSON-RPC 2.0 based stateless proxy
        if (this.proxy.type == "stateless-jsonrpc2") {
            this.proxy.method = "POST";
        }
        
        // check proxy configuration
        console.info("Proxy Configuration:", JSON.stringify(this.proxy));

        return this;
    };

    this.setMethod = function(method) {
        this.method = method.toUpperCase();
        return this;
    };
    
    this.getMethod = function() {
        return (this.proxy.method != null ? this.proxy.method : this.method);
    };

    this.setDataType = function(type) {
        this.dataType = type;
        return this;
    };

    this.setContentType = function(type) {
        this.contentType = type;
        this.setHeader("Content-Type", this.contentType);
        return this;
    };

    this.setCookie = function(cookie) {
        this.cookie = cookie;
        this.setHeader("Cookie", this.cookie);
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

                switch (key.toUpperCase()) {
                    case "CONTENT-TYPE":
                        console.warn("Will be forget the previous CONTENT-TYPE");
                        this.setContentType(value.toLowerCase());
                        break;
                    case "COOKIE":
                        console.warn("Will be forget the previous COOKIE");
                        this.setCookie(value);
                        break;
                    default:
                        this.setHeader(key, value);
                }
            }
        } catch (e) {
            console.error("Exception on HTTPObject.setHeaders():", e.message);
        }

        return this;
    };

    this.getResponseHeader = function(key) {
        try {
            return this._interface.getResponseHeader(key);
        } catch (e) {
            console.error("Exception on HTTPObject.getResponseHeader():", e.message);
        }
    };

    this.getResponseHeaders = function(callback) {
        try {
            var text = this._interface.getAllResponseHeaders();
            var headers = text.split(/[\r\n]+/).filter(function(s) {
                return s.trim().length > 0;
            }).map(function(s) {
                return s.trim().split(": ");
            }).reduce(function(acc, c) {
                acc[c[0].trim()] = c[1].trim();
                return acc;
            }, {});
            
            if (typeof callback !== "function") {
                return headers;
            }
            
            return callback(headers);
        } catch (e) {
            console.error("Exception on HTTPObject.getResponseHeaders():", e.message);
        }
    };
    
    this.getRequestHeader = function(key) {
        return this.headers[key];
    }
    
    this.getRequestHeaders = function(callback) {
        try {
            if (typeof callback !== "function") {
                return this.headers;
            }
            
            return callback(this.headers);
        } catch (e) {
            console.error("Exception on HTTPObject.getRequestHeaders():", e.message);
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
            console.error("Exception on HTTPObject.setParameters():", e.message);
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

    this.setIsAsynchronous = function(flag) {
        this.isAsync = flag;
        return this;
    }

    this.setUserAgent = function(agent) {
        this.userAgent = agent;
        return this;
    };
    
    this.getUserAgent = function() {
        return (this.proxy.userAgent != null ? this.proxy.userAgent : this.userAgent);
    };
    
    this.serialize = function(url) {
        var data = "";
        
        if (this.isJSONRequest() && typeof this.requestBody === "object") {
            data = JSON.stringify(this.requestBody);
        } else if (typeof this.requestBody === "object") {
            data = this.serializeURL(this.requestBody);
        } else {
            data = this.evaluate(this.requestBody);
        }
        
        if (this.proxy.type == "stateless-jsonrpc2") {
            data = JSON.stringify(
                JsonRpc2.wrap(this.proxy.rpcMethod, {
                    "method": this.method,
                    "url": url,
                    "headers": this.getRequestHeaders(function(x) {
                        return Object.keys(x).reduce(function(a, k) {
                            a.push(k + ": " + x[k]);
                            return a;
                        }, []);
                    }),
                    "data": data
                }, "")
            );
        }
        
        return data;
    };

    this.serializeURL = function(params) {
        var s = [];
        for (var k in params) {
            if (params.hasOwnProperty(k)) {
                s.push(encodeURIComponent(k) + "=" + encodeURIComponent(this.evaluate(params[k])));
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
            var params = {};
            for (var k in this.parameters) {
                if (url.indexOf(':' + k) > -1) {
                    url = url.replace(':' + k, this.evaluate(this.parameters[k]));
                } else {
                    params[k] = this.evaluate(this.parameters[k]);
                }
            }

            // Type 1
            if (Object.keys(params).length > 0) {
                if (url.indexOf('?') > -1) {
                    url += '&' + this.serializeURL(params);
                } else {
                    url += '?' + this.serializeURL(params);
                }
            }
        }

        console.log("Requested URL: " + url);

        return url;
    };

    this.getProxiedUrl = function(url) {
        if (!this.proxy.enabled) return url;

        if (this.proxy.type == "serp") {
            var serp = SERP.parseUrl(url);
            this.setVariable("engine", serp.engine);
            this.setVariable("q", encodeURIComponent(serp.keyword));
        }

        this.setVariable("url", encodeURIComponent(url));
        url = this.evaluate(this.proxy.url);

        console.log("Requested URL (Proxied):", url);

        return url;
    };

    this.open = function(method, url) {
        var url = this.serializeParameters(url);

        this.setMethod(method); // set method
        this.pushState(null, null, url); // push state
        this.setHeader("User-Agent", this.evaluate(this.getUserAgent())); // user agent

        try {
            if (this.engine == "MSXML") {
                // Get the proxied URL
                url = this.getProxiedUrl(url);

                // Open the URL
                switch (this.getMethod()) {
                    case "POST":
                        this._interface.open("POST", url, this.isAsync);
                        break;

                    case "GET":
                        this._interface.open("GET", url, this.isAsync);
                        break;

                    default:
                        console.warn(this.method, "method not supported. Retrying with cURL...");
                        this.setEngine("CURL");
                        console.log("Use the engine:", this.engine);
                }
            } else {
                console.log("Use the engine:", this.engine);
            }
        } catch (e) {
            console.error("Exception on HTTPObject.open():", e.message);
        }

        return this;
    };

    this.send = function(callback) {
        var responseText = null;
        var debuggingText = null;

        // check exists the opened states
        if (this.states.length == 0) {
            console.error("No available states");
            return;
        }
    
        // get opened URL from last states
        var state = this.states[this.states.length - 1];
        var target_url = state.url;
        var url = this.getProxiedUrl(target_url);

        // [lib/http] cURL error with non-escaped ampersand on Command Prompt #103
        var replaceAndExcludeCaretAnd = function(inputString) {
            var result = "";
            var i = 0;
        
            while (i < inputString.length) {
                // If the found position is ^&, do not modify and add it as is to the result
                if (i < inputString.length - 1 && inputString.slice(i, i + 2) === "^&") {
                    result += inputString.slice(i, i + 2);
                    i += 2;
                } else {
                    // Replace & with ^&
                    if (inputString.charAt(i) === "&") {
                        result += "^&";
                    } else {
                        result += inputString.charAt(i);
                    }
                    i++;
                }
            }

            return result;
        };

        if (this.contentType != null) {
            this.setHeader("Content-Type", this.contentType);
        }

        try {
            if (this.engine == "MSXML") {
                for (var key in this.headers) {
                    this._interface.setRequestHeader(key, this.evaluate(this.headers[key]));
                }

                switch (this.getMethod()) {
                    case "GET":
                        this._interface.send();
                        break;

                    default:
                        this._interface.send(this.serialize(target_url));
                }

                // Waiting a response
                while (this._interface.readyState < 4) sleep(100);

                // Get response text
                responseText = this._interface.responseText;
            } else if (this.engine == "CURL") {
                // Build the cURL command line context
                var cmd = [];

                if (this.isDebugging) {
                    cmd.push("-v");
                }

                if (this.isCompressedResponse) {
                    cmd.push("--compressed");
                }

                if (this.isFollowRedirect) {
                    cmd.push("-L");
                }

                cmd.push("-X");
                cmd.push(this.getMethod());

                if (Object.keys(this.headers).length > 0) {
                    for (var key in this.headers) {
                        var value = this.evaluate(this.headers[key]);
                        if (value != null) {
                            cmd.push("-H");
                            cmd.push(key + ": " + value);
                        }
                    }
                }

                if (this.cookie != null) {
                    cmd.push("-b");
                    cmd.push(this.evaluate(this.cookie));
                }

                if (this.isLoggingCookie) {
                    cmd.push("-c");
                    cmd.push(this.storedCookie.path);
                }

                cmd.push("-A");
                cmd.push(this.evaluate(this.getUserAgent()));

                // --connect-timeout
                if (this.connectTimeout > 0) {
                    cmd.push("--connect-timeout");
                    cmd.push(this.connectTimeout);
                }
                
                // --max-time
                if (this.maxTime > 0) {
                    cmd.push("--max-time");
                    cmd.push(this.maxTime);
                }

                // Add the credential parameters
                switch (this.credential.method.toUpperCase()) {
                    case "BASIC":
                        cmd.push("-u");
                        cmd.push(this.credential.username + ":" + this.credential.password);
                        break;
                }

                // Add the request body if this is not GET method
                if (this.getMethod() !== "GET") {
                    cmd.push("-d");
                    cmd.push(replaceAndExcludeCaretAnd(this.serialize(target_url)));
                }

                // Add proxy: <[protocol://][user:password@]proxyhost[:port]>
                if (this.proxy != null && this.proxy.enabled && this.proxy.type == "stateful") {
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

                // if it is download
                if (this.saveTo != null) {
                    cmd.push("-o");
                    cmd.push(this.saveTo);
                }

                // If not verify SSL
                if (!this.isVerifySSL) {
                    if (this.proxy.enabled) {
                        cmd.push("--proxy-insecure");
                    }
                    cmd.push("-k");
                    cmd.push("--ssl-no-revoke");
                }

                // if the count of this.curlOptions greater than 0
                if (this.curlOptions.length > 0) {
                    cmd = cmd.concat(this.curlOptions);
                }

                // set the URL
                cmd.push(url);

                // Get response text
                responseText = this._interface.setCharset(this.charset).exec(cmd);

                // Reload a cookie in the pipe
                if (this.isLoggingCookie) {
                    this.storedCookie.reload();
                }

                // If enabled the charset(text encoding) detector
                if (this.isUseDetectCharset) {
                    var detectedCharset = this.detectCharset(responseText);
                    console.log("Detected charset:", detectedCharset);

                    if (detectedCharset != null && this.charset != detectedCharset) {
                        var _interface = SHELL.create();
                        responseText = _interface.setCharset(detectedCharset).exec(cmd);
                        debuggingText = _interface.stderr.read();
                    }
                }

                // Get debuging text
                debuggingText = this._interface.stderr.read();
                
                // clear manually
                this._interface.clear();
            } else if (this.engine == "BITS") {
                var job_name = "welsonjs_" + PipeIPC.UUIDv4.create().substring(0, 8);
                var job_priority = "normal";
                var state = this.states[this.states.length - 1];
                var cmd = ["/transfer", job_name];
                var url = this.getProxiedUrl(state.url);
                var out = PipeIPC.connect("volatile");

                if (this.getMethod() == "GET") {
                    cmd = cmd.concat(["/download", "/priority", job_priority, url, SYS.getCurrentScriptDirectory() + "\\" + out.path]);   // build a BITS command
                    this._interface.exec(cmd);   // launch the download job
                    out.reload();    // read the downloaded data
                    responseText = out.read()    // set the downloaded data to response text

                    var err = this._interface.exec(["/geterror", job_name]);    // get error information
                    debuggingText = err.stdout.read();   // set the error information to debugging text

                    out.destroy();   // destroy the downloaded data
                }
            } else if (this.engine == "CERT") {
                var state = this.states[this.states.length - 1];
                var out = PipeIPC.connect("volatile");
                var url = this.getProxiedUrl(state.url);
                var cmd = ["-urlcache", "-split", "-f", url, out.path];
                this._interface.exec(cmd);
                out.reload();
                responseText = out.read();
                out.destroy();
            }

            if (typeof responseText === "string") {
                console.log("Received", responseText.length, "bytes");
            } else {
                console.log("No received anything");
            }

            if (this.isDebugging && typeof debuggingText === "string") {
                this.debuggingText = debuggingText;
                console.log("Created debugging text", debuggingText.length, "bytes");
            } else {
                console.log("No debugging text");
            }

            if (this.isJSONResponse()) {
                try {
                    var res = JSON.parse(responseText);
                    if (this.proxy.type == "stateless-jsonrpc2") {
                        this.setResponseBody(res.result.data);
                    } else {
                        this.setResponseBody(res);
                    }
                } catch (e) {
                    console.error("JSON parse error:", e.message);
                    this.setResponseBody({});
                }
            } else {
                this.setResponseBody(responseText);
            }

            if (typeof(callback) === "function") {
                try {
                    callback(this.responseBody);
                } catch (e) {
                    console.log("callback of HTTPObject.send() ->", e.message);
                }
            }
        } catch (e) {
            console.error("HTTPObject.send() ->", e.message);
        }

        return this;
    };

    this.get = function(url, callback, onError) {
        try {
            if (this.jqEnabled()) {
                this.setMethod("GET");
                this.jqAjax(url, callback, onError);
                return this;
            } else {
                return this.open("GET", url).send(callback);
            }
        } catch (e) {
            if (typeof onError === "function") onError(this, null, e);
        }
    };

    this.post = function(url, callback, onError) {
        try {
            if (this.jqEnabled()) {
                this.setMethod("POST");
                this.jqAjax(url, callback, onError);
                return this;
            } else {
                return this.open("POST", url).send(callback);
            }
        } catch (e) {
            if (typeof onError === "function") onError(this, null, e);
        }
    };

    this.patch = function(url, callback, onError) {
        try {
            if (this.jqEnabled()) {
                this.setMethod("PATCH");
                this.jqAjax(url, callback, onError);
                return this;
            } else {
                return this.open("PATCH", url).send(callback);
            }
        } catch (e) {
            if (typeof onError === "function") onError(this, null, e);
        }
    };

    this.put = function(url, callback, onError) {
        try {
            if (this.jqEnabled()) {
                this.setMethod("PUT");
                this.jqAjax(url, callback, onError);
                return this;
            } else {
                return this.open("PUT", url).send(callback);
            }
        } catch (e) {
            if (typeof onError === "function") onError(this, null, e);
        }
    };

    this._delete = function(url, callback, onError) {
        try {
            if (this.jqEnabled()) {
                this.setMethod("DELETE");
                this.jqAjax(url, callback, onError);
                return this;
            } else {
                return this.open("DELETE", url).send(callback);
            }
        } catch (e) {
            if (typeof onError === "function") onError(this, null, e);
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

    this.setVariable = function(k, v) {
        this.variables[k] = v;
        return this;
    };

    this.setVariables = function(variables) {
        try {
            var variables = (typeof(variables) !== "undefined") ? variables : {};
            for (var k in variables)
                this.setVariable(k, variables[k]);
        } catch (e) {
            console.error("HTTPObject.setVariables() ->", e.message);
        }
        return this;
    };

    this.evaluate = function(str) {
        if (typeof str === "undefined" || str == null) return '';

        var str = String(str);
        var L = '{', R = '}';
        var Lpos = str.indexOf(L);
        var Rpos = str.indexOf(R, Lpos + 1);
        var s0 = '',
            s1 = [],
            s2 = null,
            s3, s4;

        // fix #122
        if (str.indexOf(L) == 0) {
            try {
                JSON.parse(str);
                return str;
            } catch (e) {}
        }

        while (Lpos > -1 && Rpos > -1) {
            s0 = str.substring(Lpos + 1, Rpos);
            s2 = '';
            s1 = s0.split(' ');
            while (s1.length > 0) {
                s3 = s1.pop();

                if (s3 in this.variables) {
                    switch (typeof(this.variables[s3])) {
                        case "function":
                            s2 = this.variables[s3](s2);
                            break;

                        case "object":
                            s4 = this.variables[s3];
                            for (var k in s4) {
                                s4[k] = this.evaluate(s4[k]);
                            }
                            s2 = s4;
                            break;

                        default:
                            s2 = this.variables[s3];
                    }
                } else {
                    s2 = s3;
                }
            }
            str = str.substring(0, Lpos) + s2 + str.substring(Rpos + 1);
            Lpos = str.indexOf(L);
            Rpos = str.indexOf(R, Lpos + 1);
        }

        return str;
    };

    this.setConnectTimeout = function(seconds) {
        this.connectTimeout = seconds;
        return this;
    };

    this.setMaxTime = function(seconds) {
        this.maxTime = seconds;
        return this;
    };

    this.setIsDebugging = function(flag) {
        this.isDebugging = flag;
        return this;
    };

    this.setCredential = function(cred) {
        this.credential = cred;
        return this;
    };

    this.setIsFollowRedirect = function(flag) {
        this.isFollowRedirect = flag;
        return this;
    };

    this.setSaveTo = function(filename) {
        this.saveTo = filename;
    };

    this.parseScripts = function() {
        var scripts = [];

        if (typeof this.responseBody !== "string")
            return scripts;

        var tagName = "script";
        var a = this.responseBody.indexOf('<' + tagName + ' ');
        var b = a < 0 ? -1 : this.responseBody.indexOf('</' + tagName + '>', a);

        while (a > -1 && b > -1) {
            var outerHTML = this.responseBody.substring(a, b + tagName.length + 3);
            var innerHTML = this.responseBody.substring(this.responseBody.indexOf('>', a), b);

            scripts.push({
                'outerHTML': outerHTML,
                'innerHTML': innerHTML
            });

            a = this.responseBody.indexOf('<' + tagName + ' ', b + tagName.length + 3);
            b = a < 0 ? -1 : this.responseBody.indexOf('</' + tagName + '>', a);
        }

        return scripts;
    };

    this.setIsLoggingCookie = function(flag) {
        this.isLoggingCookie = flag;
        return this;
    };

    this.getAllCookies = function() {
        var data = {};

        var rows = splitLn(this.storedCookie.read());
        for (var i = 0; i < rows.length; i++) {
            var cols = rows[i].split("\t");
            if (cols.length == 7) {
                data[cols[5]] = cols[6];
            }
        }

        return data;
    };

    this.getFrameURLs = function() {
        if (typeof this.responseBody !== "string") {
            return [];
        }

        var urls = [];
        var response = this.responseBody;
        var pos = response.indexOf('<iframe ');

        while (pos > -1) {
            var end = response.indexOf('</iframe>', pos);

            if (end < 0) {
                pos = response.indexOf('<iframe ', pos + 8);
                continue;
            }

            var a = response.indexOf('src="', pos);
            var b = response.indexOf('"', a + 5);
            if (a > 0 && b > 0) {
                urls.push(response.substring(a + 5, b));
            }

            pos = response.indexOf('<iframe ', pos + end);
        }

        return urls;
    };

    this.attachDebugger = function(_debugger) {
        this.setProxy({
            "enabled": true,
            "provider": _debugger.toLowerCase()
        });
        this.isVerifySSL = false;

        console.warn("The existing proxy settings have been reset.");

        return this;
    };

    this.setCharset = function(charset) {
        this.charset = charset;
        return this;
    };
    
    this.setIsUseDetectCharset = function(flag) {
        this.isUseDetectCharset = flag;
        return this;
    };

    this.detectCharset = function(content) {
        var charset = null;

        try {
            var s = "charset=";
            var pos = content.toLowerCase().indexOf(s);
            if (pos > -1) {
                var end = [
                    content.indexOf('"', pos + s.length),
                    content.indexOf('\'', pos + s.length),
                    content.indexOf(';', pos + s.length)
                ].reduce(function(a, x) {
                    if (a < 0 || x > 0 && x < a) a = x;
                    return a;
                }, -1);

                if (end > -1) {
                    var detectedCharset = content.substring(pos + s.length, end);
                    if (detectedCharset.length > 0 && detectedCharset.length < 16) {
                        charset = detectedCharset.toLowerCase();
                    }
                }
            }
        } catch (e) {
            charset = null;
        }

        return charset;
    };

    this.detectSSL = function() {
        return (this.debuggingText.indexOf("certificate") > -1);
    };

    this.detectSSLCompleted = function() {
        return this.detectSSL() && (this.debuggingText.indexOf("certificate problem") < 0);
    };

    this.setIsCompressedResponse = function(isCompressedResponse) {
        this.isCompressedResponse = isCompressedResponse;
        return this;
    };

    this.create();
};

function create(engine) {
    return new HTTPObject(engine);
}

function get(url, params, headers) {
    return create().setHeaders(headers).setParameters(params).setUseCache(false).get(url).responseBody;
}

function post(url, data, headers, params) {
    return create().setHeaders(headers).setRequestBody(data).setParameters(params).post(url).responseBody;
}

function patch(url, data, headers) {
    return create().setHeaders(headers).setParameters(data).patch(url).responseBody;
}

function put(url, data, headers) {
    return create().setHeaders(headers).setParameters(data).put(url).responseBody;
}

function _delete(url, params, headers) {
    return create().setHeaders(headers).setParameters(params).setUseCache(false)._delete(url).responseBody;
}

function parseURL(url) {
    var pattern = /^(?:(https?):\/\/)?(?:([^:@]+)(?::([^:@]*))?@)?([^:]+)(?::(\d{1,5}))?$/;
    var matches = url.match(pattern);
    if (!matches) return null;

    var protocol = matches[1] || 'http';
    var username = matches[2] || '';
    var password = matches[3] || '';
    var host = matches[4] || 'localhost';
    var port = matches[5] || '';
    var credential = null;

    if (username != '' && password != '') {
        credential = {
            "username": username,
            "password": password
        }
    }

    return {
        "protocol": protocol,
        "host": host,
        "port": parseInt(port),
        "credential": credential
    };
}

// Check an available proxies
AVAILABLE_PROXIES.forEach(function(proxy) {
    if (proxy.type == "file") {
        if (FILE.fileExists(proxy.url)) {
            try {
                var fileContents = FILE.readFile(proxy.url, FILE.CdoCharset.CdoUTF_8);
                var data = JSON.parse(fileContents);
                data.forEach(function(x) {
                    AVAILABLE_PROXIES.push(x);
                });
            } catch (e) {
                console.warn(proxy.url, "is not a valid file");
            }
        } else {
            console.warn(proxy.url, "does not exists");
        }
    }
});

exports.create = create;
exports.get = get;
exports.post = post;
exports.patch = patch;
exports.put = put;
exports._delete = _delete;

exports.parseURL = parseURL;
exports.DEFAULT_USER_AGENT = DEFAULT_USER_AGENT;
exports.defaultUserAgent = DEFAULT_USER_AGENT;    // compatible

exports.VERSIONINFO = "WelsonJS framework HTTP client (http.js) version 0.7.48";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
