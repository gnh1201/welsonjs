/*
 * webloader.js
 */
var FILE = require('lib/file');

if (!window.Element) {
    Element = function() {};

    var __createElement = document.createElement;
    document.createElement = function(tagName) {
        var element = __createElement(tagName);
        if (element == null) {
            return null;
        }
        for (var key in Element.prototype)
            element[key] = Element.prototype[key];
        return element;
    }

    var __getElementById = document.getElementById;
    document.getElementById = function(id) {
        var element = __getElementById(id);
        if (element == null) {
            return null;
        }
        for (var key in Element.prototype)
            element[key] = Element.prototype[key];
        return element;
    }
}

if (!window.addEventListener) {
    (function(WindowPrototype, DocumentPrototype, ElementPrototype, registry) {
        DocumentPrototype.head = (function() {
            return DocumentPrototype.getElementsByTagName("head")[0];
        })();

        var inject = function(obj, registry) {
            obj.addEventListener = function(type, listener) {
                var target = this;

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

        inject(WindowPrototype, registry);
        inject(DocumentPrototype, registry);
        inject(ElementPrototype, registry);
    })(window, document, Element.prototype, []);
}

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
    setWindowsMovable: function() {
        var grip = document.getElementById('app'),
            oX, oY,
            mouseDown = function(e) {
                if (e.offsetY + e.offsetX < 0) return;
                oX = e.screenX;
                oY = e.screenY;
                window.addEventListener('mousemove', mouseMove);
                window.addEventListener('mouseup', mouseUp);
            },
            mouseMove = function(e) {
                window.moveTo(screenX + e.screenX - oX, screenY + e.screenY - oY);
                oX = e.screenX;
                oY = e.screenY;
            },
            gripMouseMove = function(e) {
                this.style.cursor = (e.offsetY + e.offsetX > -1) ? 'move' : 'default';
            },
            mouseUp = function(e) {
                window.removeEventListener('mousemove', mouseMove);
                window.removeEventListener('mouseup', mouseUp);
            };

        grip.addEventListener('mousedown', mouseDown);
        grip.addEventListener('mousemove', gripMouseMove);
    },
    getIEVersion: function() {
        return IEVersion;
    },
    addScript: function(url, callback, test, ttl) {
        var _callback = function(el, ttl) {
            setTimeout(function() {
                var result = test(el);
                if (typeof(result) !== "undefined") {
                    callback(el);
                } else {
                    if (ttl > 0) {
                        _callback(el, ttl - 50);
                    } else {
                        console.log("failed load " + url);
                    }
                }
            }, 50);
        };

        var el = document.createElement("script");
        el.src = url;
        el.type = "text/javascript";
        el.charset = "utf-8";
        document.head.appendChild(el);

        if (typeof(test) === "function") {
            // Time-To-Live: default value is 30 seconds
            ttl = (typeof(ttl) == "number" ? ttl : 30000);
            _callback(el, ttl);
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
        document.head.appendChild(el);
        if (typeof(callback) === "function") {
            el.onload = callback(el);
        }
        return el;
    },
    main: function() {
        // load contents
        var contents = FILE.readFile("app\\app.html", "utf-8");
        document.getElementById("app").innerHTML = contents;

        // load stylesheets dynamically
        this.addStylesheet("app/assets/css/jquery.toast.min.css");

        // load javascripts dynamically
        this.addScript("app/assets/js/es5-shim.min.js");
        this.addScript("app/assets/js/es5-sham.min.js");
        this.addScript("app/assets/js/json3.min.js");
        this.addScript("app/assets/js/es6-shim.min.js");
        this.addScript("app/assets/js/es6-sham.min.js");
        if (this.getIEVersion() < 9) {
            this.addScript("app/assets/js/html5shiv-printshiv.min.js");
            this.addScript("app/assets/js/jquery-1.11.3.min.js");
        } else {
            this.addScript("app/assets/js/jquery-3.5.1.min.js", function(el) {
                jQuery.support.cors = true;
            }, function(el) {
                return window.jQuery;
            });
        }
        if (this.getIEVersion() < 10) {
            this.addScript("app/assets/js/jquery.html5-placeholder-shim.js");
        }
        this.addScript("app/assets/js/jquery.form.min.js");
        this.addScript("app/assets/js/jquery.toast.min.js", function(el) {
            if (messages.length > 0) {
                for (var i in messages) {
                    console.log(messages[i]);
                }
            }
        }, function(el) {
            return window.jQuery.toast;
        });
        this.addScript("app/assets/js/index.js");

		// "prevent text drag and drop"; {
		document.body.ondragstart = function() {
		    return false;
		};
		document.body.ondrop = function() {
		    return false;
		};
		// };

        // set window movable
        this.setWindowsMovable();

        return 0;
    }
}
