/*
 * webloader.js
 */

var FILE = require('lib/file');

// https://stackoverflow.com/questions/597268/element-prototype-in-ie7
if (!window.Element) {
    Element = function() {};

    var __createElement = document.createElement;
    document.createElement = function(tagName) {
        var element = __createElement(tagName);
        for (var key in Element.prototype) {
            element[key] = Element.prototype[key];
		}
        return element;
    }

    var __getElementById = document.getElementById;
    document.getElementById = function(id) {
        var element = __getElementById(id);
        for (var key in Element.prototype) {
            element[key] = Element.prototype[key];
		}
        return element;
    }
}

// https://gist.github.com/jonathantneal/3748027
if (!window.addEventListener) {
	(function(WindowPrototype, DocumentPrototype, ElementPrototype, registry) {
		DocumentPrototype.head = (function() {
			return DocumentPrototype.getElementsByTagName("head")[0];
		})();

		var addEventListener = function(type, listener) {
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
        WindowPrototype.addEventListener = addEventListener;
        DocumentPrototype.addEventListener = addEventListener;
        ElementPrototype.ElementPrototype = addEventListener;

        var removeEventListener = function(type, listener) {
            for (var index = 0, register; register = registry[index]; ++index) {
                if (register[0] == this && register[1] == type && register[2] == listener) {
                    return this.detachEvent("on" + type, registry.splice(index, 1)[0][3]);
                }
            }
        };
        WindowPrototype.removeEventListener = removeEventListener;
        DocumentPrototype.removeEventListener = removeEventListener;
        ElementPrototype.removeEventListener = removeEventListener;

        var dispatchEvent = function(eventObject) {
            return this.fireEvent("on" + eventObject.type, eventObject);
        };
        WindowPrototype.dispatchEvent = dispatchEvent;
        DocumentPrototype.dispatchEvent = dispatchEvent;
        ElementPrototype.dispatchEvent = dispatchEvent;
    })(window, document, Element.prototype, []);
}

// https://stackoverflow.com/questions/10964966/detect-ie-version-prior-to-v9-in-javascript
// https://stackoverflow.com/a/18249612
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
    setWindowDraggable: function() {
        // https://stackoverflow.com/questions/21493777/how-to-make-a-tab-to-move-the-window-of-an-hta-application-in-vbscript
        // https://stackoverflow.com/a/21497175
        var grip = document.getElementById('app'),
            oX, oY,
            mouseDown = function (e) {
                if (e.offsetY + e.offsetX < 0) return;
                oX = e.screenX;
                oY = e.screenY;
                window.addEventListener('mousemove', mouseMove);
                window.addEventListener('mouseup', mouseUp);
            },
            mouseMove = function (e) {
                window.moveTo(screenX + e.screenX - oX, screenY + e.screenY - oY);
                oX = e.screenX;
                oY = e.screenY;
            },
            gripMouseMove = function (e) {
                this.style.cursor = (e.offsetY + e.offsetX > -1) ? 'move' : 'default';
            },
            mouseUp = function (e) {
                window.removeEventListener('mousemove', mouseMove);
                window.removeEventListener('mouseup', mouseUp);
            };
        grip.addEventListener('mousedown', mouseDown);
        grip.addEventListener('mousemove', gripMouseMove);
    },
    getIEVersion: function() {
        return IEVersion;
    },
    addScript: function(url, callback, test) {
		var _callback = function(el) {
			setTimeout(function() {
				var result = test(el);
				if(typeof(result) !== "undefined") {
					callback(el);
				} else {
					_callback(el);
				}
			}, 50);
		};

		var el = document.createElement("script");
        el.src = url;
        el.type = "text/javascript";
        el.charset = "utf-8";
        document.head.appendChild(el);

		if(typeof(test) === "function") {
			_callback();
		} else if(typeof(callback) === "function") {
            el.onload = callback(el);
        }

        return el;
    },
    addStylesheet: function(url, callback, test) {
		var _callback = function(el) {
			setTimeout(function() {
				var result = test(el);
				if(typeof(result) !== "undefined") {
					callback(el);
				} else {
					_callback(el);
				}
			}, 50);
		};
		
        var el = document.createElement("link");
        el.href = url;
        el.rel = "stylesheet";
        el.type = "text/css";
        document.head.appendChild(el);

		if(typeof(test) === "function") {
			_callback();
		} else if(typeof(callback) === "function") {
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
        if(this.getIEVersion() < 9) {
            this.addScript("app/assets/js/html5shiv-printshiv.min.js");
            this.addScript("app/assets/js/jquery-1.11.3.min.js");
        } else {
            this.addScript("app/assets/js/jquery-3.5.1.min.js", function(el) {
				jQuery.support.cors = true;
            }, function(el) {
				return window.jQuery;
			});
        }
        if(this.getIEVersion() < 10) {
            this.addScript("app/assets/js/jquery.html5-placeholder-shim.js");
        }
        this.addScript("app/assets/js/jquery.form.min.js");
        this.addScript("app/assets/js/jquery.toast.min.js", function(el) {
			if(messages.length > 0) {
				for(var i in messages) {
					console.log(messages[i]);
				}
			}
		}, function(el) {
			return window.jQuery.toast;
		});
        this.addScript("app/assets/js/index.js");

        // set window draggable
        this.setWindowDraggable();

        return 0;
    }
}
