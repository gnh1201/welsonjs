////////////////////////////////////////////////////////////////////////
// Webloader
////////////////////////////////////////////////////////////////////////
var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");
var URILoader = require("uriloader");

////////////////////////////////////////////////////////////////////////
// Override global.console.__echo()
////////////////////////////////////////////////////////////////////////
global.console.__echo = function(msg) {
    if (typeof(window.jQuery) !== "undefined") {
        window.jQuery.toast({
            heading: "Information",
            text: msg,
            icon: "info"
        });
    } else {
        alert(msg);
    }

    global.console.__messages.push(msg);
};

////////////////////////////////////////////////////////////////////////
// Override global.exit()
////////////////////////////////////////////////////////////////////////
global.exit = function() {
    if (typeof(window) !== "undefined") {
        window.close();
    }
};

////////////////////////////////////////////////////////////////////////
// exports.IEVersion
////////////////////////////////////////////////////////////////////////
exports.IEVersion = OldBrowser.getIEVersion();

////////////////////////////////////////////////////////////////////////
// exports.enableMovableWindow()
////////////////////////////////////////////////////////////////////////
exports.enableMovableWindow = function() {
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

    grip.addEventListener("mousedown", mouseDown);
    grip.addEventListener("mousemove", gripMouseMove);
};

////////////////////////////////////////////////////////////////////////
// exports.main()
////////////////////////////////////////////////////////////////////////
exports.main = function(args) {
    // make will display contents
    OldBrowser.setContent(FILE.readFile("app\\index.html", "utf-8"));
    OldBrowser.addStylesheet("app/assets/css/jquery-ui-1.21.1.min.css");
    OldBrowser.addStylesheet("app/assets/css/jquery.toast-1.3.2.min.css");
    OldBrowser.addStylesheet("app/assets/css/style.css");
    OldBrowser.start(function(el) {
        jQuery.support.cors = true;

        OldBrowser.addScript("app/assets/js/jquery.toast-1.3.2.min.js", function(el) {
            var messages = global.console.__messages;
            if (messages.length > 0) {
                for (var i in messages) {
                    console.log(messages[i]);
                }

                // start this app
                OldBrowser.addScript("app/index.js");
            }
        }, function(el) {
            return window.jQuery.toast;
        });

        OldBrowser.addScript("app/assets/js/jquery.form-4.3.0.min.js");
    });

    // hook drag and drop
    document.body.ondragstart = function() {
        return false;
    };
    document.body.ondrop = function() {
        return false;
    };

    // set movable window
    exports.enableMovableWindow();

    // assign click event
    var elems = document.getElementsByTagName("a");
    for(var i in elems) {
        var uri = elems[i].href || "";
        var pos = uri.indexOf("://");
        if(uri.substring(0, pos) == __config.appName) {
            elems[i].onclick = function(e) {
                var uri = this.href || "";
                URILoader.main([uri]);
                e.preventDefault();
            };
        }
    }

    return 0;
};
