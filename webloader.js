////////////////////////////////////////////////////////////////////////
// Webloader
////////////////////////////////////////////////////////////////////////
var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");

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
// this window makes movable
////////////////////////////////////////////////////////////////////////
(function(grip) {
    var oX, oY,
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
})(document.getElementsByTagName("body")[0]);

////////////////////////////////////////////////////////////////////////
// exports.IEVersion
////////////////////////////////////////////////////////////////////////
exports.IEVersion = OldBrowser.getIEVersion();

////////////////////////////////////////////////////////////////////////
// exports.main()
////////////////////////////////////////////////////////////////////////
exports.main = function(args) {
    // make will display contents
    OldBrowser.setContent(FILE.readFile("app\\index.html", "utf-8"));
    OldBrowser.addStylesheet("app/assets/css/jquery-ui-1.21.1.min.css");
    OldBrowser.addStylesheet("app/assets/css/jquery.toast-1.3.2.min.css");
    OldBrowser.addStylesheet("app/assets/css/cascade/production/build-full.min.css");
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

                // hide loading image
                document.getElementById("loading").style.display = "none";
            }
        }, function(el) {
            return window.jQuery.toast;
        });
    });

    // hook drag and drop
    document.body.ondragstart = function() {
        return false;
    };
    document.body.ondrop = function() {
        return false;
    };

    return 0;
};
