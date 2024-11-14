// browser.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

// only less than IE 9
if (!window.addEventListener) {
    global.Element = function() {};

    (function(WindowPrototype, DocumentPrototype, ElementPrototype, registry) {
        if (!DocumentPrototype.head) {
            DocumentPrototype.head = (function() {
                return DocumentPrototype.getElementsByTagName("head")[0];
            })();
        }

        if (!DocumentPrototype.getElementsByClassName) {
            DocumentPrototype.getElementsByClassName = function(search) {
                var d = document,
                    elements, pattern, i, results = [];
                if (d.querySelectorAll) { // IE8
                    return d.querySelectorAll("." + search);
                }
                if (d.evaluate) { // IE6, IE7
                    pattern = ".//*[contains(concat(' ', @class, ' '), ' " + search + " ')]";
                    elements = d.evaluate(pattern, d, null, 0, null);
                    while ((i = elements.iterateNext())) {
                        results.push(i);
                    }
                } else {
                    elements = d.getElementsByTagName("*");
                    pattern = new RegExp("(^|\\s)" + search + "(\\s|$)");
                    for (i = 0; i < elements.length; i++) {
                        if (pattern.test(elements[i].className)) {
                            results.push(elements[i]);
                        }
                    }
                }
                return results;
            }
        }

        var enableEventListener = function(obj, registry) {
            obj.addEventListener = function(type, listener) {
                var target = this;

                if (typeof(registry) === "undefined")
                    registry = [];

                registry.unshift([target, type, listener, function(event) {
                    event.currentTarget = target;
                    event.preventDefault = function() {
                        event.returnValue = false
                    };
                    event.stopPropagation = function() {
                        event.cancelBubble = true
                    };
                    event.target = event.srcElement || target;

                    listener.call(target, event);
                }]);

                this.attachEvent("on" + type, registry[0][3]);
            };

            obj.removeEventListener = function(type, listener) {
                for (var index = 0, register; register = registry[index]; ++index) {
                    if (register[0] == this && register[1] == type && register[2] == listener) {
                        return this.detachEvent("on" + type, registry.splice(index, 1)[0][3]);
                    }
                }
            };

            obj.dispatchEvent = function(eventObject) {
                return this.fireEvent("on" + eventObject.type, eventObject);
            };
        };

        WindowPrototype.enableEventListener = function(obj) {
            if (!obj.addEventListener) {
                enableEventListener(obj);
            }
        };
        enableEventListener(WindowPrototype, registry);
        enableEventListener(DocumentPrototype, registry);
        enableEventListener(ElementPrototype, registry);

        var __createElement = DocumentPrototype.createElement;
        DocumentPrototype.createElement = function(tagName) {
            var element = __createElement(tagName);
            if (element == null) {
                return null;
            }
            for (var key in ElementPrototype)
                element[key] = ElementPrototype[key];
            return element;
        }

        var __getElementById = DocumentPrototype.getElementById;
        DocumentPrototype.getElementById = function(id) {
            var element = __getElementById(id);
            if (element == null) {
                return null;
            }
            for (var key in ElementPrototype)
                element[key] = ElementPrototype[key];
            return element;
        }
    })(window, document, global.Element.prototype, []);
}

// getIEVersion()
function getIEVersion() {
    var undef,
        v = 3,
        div = document.createElement('div'),
        all = div.getElementsByTagName('i');

    while (
        div.innerHTML = '<!--[if gt IE ' + (++v) + ']><i></i><![endif]-->',
        all[0]
    );

    return v > 4 ? v : undef;
};

// waitUntil()
function waitUntil(f, test, ttl) {
    if (typeof f !== "function") return;
    if (typeof test !== "function") return;
    var ttl = (typeof ttl === "number" ? ttl : 30000);  // TTL(Time-To-Live)
    var started_time = Date.now();

    var _test = function(el, callback, ttl) {
        var result = test(el);
        if (typeof result !== "undefined" && !!result) {
            callback(el);
        } else {
            setTimeout(function() {
                var current_time = Date.now();
                if (current_time - started_time < ttl) {
                    _test(el, callback, ttl);
                } else {
                    console.warn("Failed to load:", url);
                }
            }, 1);
        }
    };

    f(_test, ttl);
}

// addScript()
function addScript(url, callback, test, ttl) {
    if (typeof test !== "function") {
        test = function(el, callback, ttl) {};
    }
    var ttl = (typeof ttl === "number" ? ttl : 30000);  // TTL(Time-To-Live)

    var el = document.createElement("script");
    el.src = url;
    el.type = "text/javascript";
    el.charset = "utf-8";
    document.head.appendChild(el);
    
    if (typeof test === "function") {
        test(el, callback, ttl);
    }

    return el;
};

// addStylesheet()
function addStylesheet(url, callback, test, ttl) {
    if (typeof test !== "function") {
        test = function(el, callback, ttl) {};
    }
    var ttl = (typeof ttl === "number" ? ttl : 30000);  // TTL(Time-To-Live)

    var el = document.createElement("link");
    el.href = url;
    el.rel = "stylesheet";
    el.type = "text/css";
    el.media = "screen, projection";
    document.head.appendChild(el);

    if (typeof test === "function") {
        test(el, callback, ttl);
    }

    return el;
};

// setContent()
function setContent(content) {
    document.getElementById("app").innerHTML = content;
};

// start()
function start(callback) {
    var msie = getIEVersion();

    // load jQuery and cross browsing libraries
    if (msie == 8) {
        addScript("app/assets/css/jquery/ie8-0.8.1.min.js");
    }
    addScript("app/assets/js/html5shiv-printshiv-3.7.3.min.js");
    if (msie < 9) {
        addScript("app/assets/js/respond-1.4.2.wsh.js");
        addScript("app/assets/js/selectivizr-1.0.2.wsh.js");
        addScript("app/assets/js/excanvas.arv-565afad.js");

        waitUntil(function(test, ttl) {
            addScript("app/assets/js/jquery-1.11.3.min.js", callback, test, ttl);
        }, function(el) {
            return window.jQuery;
        });
    } else {
        waitUntil(function(test, ttl) {
            addScript("app/assets/js/jquery-3.5.1.min.js", callback, test, ttl);
        }, function(el) {
            return window.jQuery;
        });
    }
    addScript("app/assets/js/jquery.html5-placeholder-shim-5a87f05.js");

    // load Modernizr (2.8.3)
    addScript("app/assets/js/modernizr-2.8.3.min.js");

    // load jQuery UI (1.12.1)
    addScript("app/assets/js/jquery-ui-1.21.1.min.js");

    // load jsRender (1.0.8)
    addScript("app/assets/js/jsrender-1.0.8.min.js");
};

// reload()
function reload() {
    if (typeof window !== "undefined") {
        window.location.reload();
    }
};

// close()
function close() {
    exit(0);
};

exports.getIEVersion = getIEVersion;
exports.waitUntil = waitUntil;
exports.addScript = addScript;
exports.addStylesheet = addStylesheet;
exports.setContent = setContent;
exports.start = start;
exports.reload = reload;
exports.close = close;

exports.VERSIONINFO = "Browser Compatibility Library (browser.js) version 0.1.6";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
