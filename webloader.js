////////////////////////////////////////////////////////////////////////
// Webloader
////////////////////////////////////////////////////////////////////////
var CONFIG = require("lib/config");
var FILE = require("lib/file");
var OldBrowser = require("lib/oldbrowser");

////////////////////////////////////////////////////////////////////////
// Override global.console._echo()
////////////////////////////////////////////////////////////////////////
global.console._echo = function(msg, type) {
    var heading, icon;

    switch(type) {
        case "error":
            heading = "Error";
            icon = "error";
            break;

        case "warning":
            heading = "Warning";
            icon = "warning";
            break;

        case "info":
            heading = "Information";
            icon = "info";

        default:
            heading = "Message";
            icon = "success";
    }

    if (typeof(window.jQuery) !== "undefined") {
        window.jQuery.toast({
            heading: heading,
            text: msg,
            icon: icon
        });
    } else {
        window.alert(msg);
    }

    global.console._messages.push(msg);
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
})(document.getElementById("app"));

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
            var messages = global.console._messages;
            if (messages.length > 0) {
                // print messages
                for (var i in messages) {
                    console.log(messages[i]);
                }

                // start this app
                OldBrowser.addScript("app/index.js");

                // hide loading image
                document.getElementById("loading").style.display = "none";
                
                // show API server URL
                console.info(CONFIG.readConfig("/Config/ApiUrl").first().text);
            }
        }, function(el) {
            return window.jQuery.toast;
        });
    });

    // hook drag event
    document.body.ondragstart = function() {
        return false;
    };
    
    // hook drop event
    document.body.ondrop = function() {
        return false;
    };

    return 0;
};
