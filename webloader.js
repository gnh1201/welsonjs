/*
 * webloader.js
 * Go Namhyeon (gnh1201@gmail.com)
 * 2020-07-07
 * https://github.com/gnh1201/welsonjs
 *
 */
var FILE = require('lib/file');

// "less than IE 9";
window.enableEventListener = function(obj) {};
if (!window.addEventListener) {
    Element = function() {};

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
    })(window, document, Element.prototype, []);
}

// "get IE version";
var IEVersion = (function() {
    var undef,
        v = 3,
        div = document.createElement('div'),
        all = div.getElementsByTagName('i');

    while (
        div.innerHTML = '<!--[if gt IE ' + (++v) + ']><i></i><![endif]-->',
        all[0]
    );

    return v > 4 ? v : undef;
})();

return {
    enableMovableWindow: function() {
        var grip = document.getElementById("app"),
            oX, oY,
            mouseDown = function(e) {
                if (e.offsetY + e.offsetX < 0) return;
                oX = e.screenX;
                oY = e.screenY;
                window.addEventListener("mousemove", mouseMove);
                window.addEventListener("mouseup", mouseUp);
            },
            mouseMove = function(e) {
                window.moveTo(screenX + e.screenX - oX, screenY + e.screenY - oY);
                oX = e.screenX;
                oY = e.screenY;
            },
            gripMouseMove = function(e) {
                this.style.cursor = (e.offsetY + e.offsetX > -1) ? "move" : "default";
            },
            mouseUp = function(e) {
                window.removeEventListener("mousemove", mouseMove);
                window.removeEventListener("mouseup", mouseUp);
            };

        enableEventListener(grip);
        grip.addEventListener("mousedown", mouseDown);
        grip.addEventListener("mousemove", gripMouseMove);
    },
    getIEVersion: function() {
        return IEVersion;
    },
    addScript: function(url, callback, test, ttl) {
        var _callback = function(el, ttl) {
            var result = test(el);
            if (typeof(result) !== "undefined") {
                callback(el);
            } else {
                setTimeout(function() {
                    if (ttl > 0) {
                        _callback(el, ttl - 1);
                    } else {
                        console.log("failed load " + url);
                    }
                }, 1);
            }
        };

        var el = document.createElement("script");
        el.src = url;
        el.type = "text/javascript";
        el.charset = "utf-8";
        document.head.appendChild(el);

        if (typeof(test) === "function" && typeof(callback) === "function") {
            // "Time-To-Live: default value is 30 seconds";
            ttl = (typeof(ttl) == "number" ? ttl : 30000);
            el.onload = _callback(el, ttl);
        } else if (typeof(callback) === "function") {
            el.onload = callback(el);
        }

        return el;
    },
    addStylesheet: function(url, callback) {
        var el = document.createElement("link");
        el.href = url;
        el.rel = "stylesheet";
        el.type = "text/css";
        el.media = "screen, projection";
        document.head.appendChild(el);
        if (typeof(callback) === "function") {
            el.onload = callback(el);
        }
        return el;
    },
    main: function() {
        // "set variable 'self'";
        var self = this;

        // "load contents";
        var contents = FILE.readFile("app\\index.html", "utf-8");
        document.getElementById("app").innerHTML = contents;

        // "load stylesheets dynamically";
        self.addStylesheet("app/assets/css/jquery-ui-1.21.1.min.css");
        self.addStylesheet("app/assets/css/jquery.toast-1.3.2.min.css");
        self.addStylesheet("app/assets/css/style.css");

        // "when loaded jquery (strictly)";
        var jqLoaded = function(el) {
            jQuery.support.cors = true;

            self.addScript("app/assets/js/jquery.toast-1.3.2.min.js", function(el) {
                if (messages.length > 0) {
                    for (var i in messages) {
                        console.log(messages[i]);
                    }
                }
            }, function(el) {
                return window.jQuery.toast;
            });
        };

        // "load javascripts dynamically";
        self.addScript("app/assets/js/es5-shim-4.5.14.min.js");
        self.addScript("app/assets/js/es5-sham-4.5.14.min.js");
        self.addScript("app/assets/js/json3-3.3.2.min.js");
        self.addScript("app/assets/js/es6-shim-0.35.5.min.js");
        self.addScript("app/assets/js/es6-sham-0.35.5.min.js");
        self.addScript("app/assets/js/html5shiv-printshiv-3.7.3.min.js");
        if (self.getIEVersion() < 9) {
			self.addScript("app/assets/js/welsonjs-respond-1.4.2-modified.js");
			self.addScript("app/assets/js/welsonjs-selectivizr-1.0.2-modified.js");
            self.addScript("app/assets/js/jquery-1.11.3.min.js", jqLoaded, function(el) {
                return window.jQuery;
            });
        } else {
            self.addScript("app/assets/js/jquery-3.5.1.min.js", jqLoaded, function(el) {
                return window.jQuery;
            });
        }
        self.addScript("app/assets/js/modernizr-2.8.3.min.js");

        // "load jQuery UI (1.12.1)";
        self.addScript("app/assets/js/jquery-ui-1.21.1.min.js");

        // "load jQuery plugins";
        if (self.getIEVersion() < 10) {
            self.addScript("app/assets/js/PIE-1.0.0.js");
            self.addScript("app/assets/js/jquery.html5-placeholder-shim.js");
        }
        self.addScript("app/assets/js/jquery.form.min.js");

        // "prevent text drag and drop"; {
        document.body.ondragstart = function() {
            return false;
        };
        document.body.ondrop = function() {
            return false;
        };
        // };

        // "set movable window";
        self.enableMovableWindow();

        // "go to entrypoint";
        self.addScript("app/assets/js/index.js");

        return 0;
    }
}
