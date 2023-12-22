////////////////////////////////////////////////////////////////////////
// Webloader
////////////////////////////////////////////////////////////////////////
var CONFIG = require("lib/config");
var FILE = require("lib/file");
var Browser = require("lib/browser");

////////////////////////////////////////////////////////////////////////
// Override global.console._echo()
////////////////////////////////////////////////////////////////////////
global.console._echo = function(args, type) {
    var heading, icon, message = this._join(args);
    var params = {
        type: type,
        channel: 'default',
        message: '',
        datetime: new Date().toISOString()
    };

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
            break;

        default:
            heading = "Success";
            icon = "success";
    }

    try {
        if (typeof(window.jQuery.toast) !== "undefined") {
            window.jQuery.toast({
                heading: heading,
                text: message,
                icon: icon
            });
        } else {
            window.alert(message);
        }
    } catch (e) {
        window.alert(e.message);
    }

    this._messages.push(message);

    if (params.channel != "default" && this._echoCallback != null) {
        try {
            this._echoCallback(params, type);
        } catch (e) {
            window.jQuery.toast({
                heading: "Error",
                text: e.message,
                icon: "error"
            });
        }
    }
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
exports.IEVersion = Browser.getIEVersion();

////////////////////////////////////////////////////////////////////////
// exports.main()
////////////////////////////////////////////////////////////////////////
exports.main = function(args) {
    // make will display contents
    Browser.setContent(FILE.readFile("app\\index.html", FILE.CdoCharset.CdoUTF_8));

    // add stylesheets
    Browser.addStylesheet("app/assets/css/jquery-ui-1.21.1.min.css");
    Browser.addStylesheet("app/assets/css/jquery.toast-1.3.2.min.css");
    Browser.addStylesheet("app/assets/css/cascade/production/build-full.min.css");
    Browser.addStylesheet("app/assets/css/style.css");

    // start
    Browser.start(function(el) {
        jQuery.support.cors = true;

        Browser.addScript("app/assets/js/jquery.toast-1.3.2.min.js", function(el) {
            var messages = global.console._messages;
            if (messages.length > 0) {
                // print messages
                for (var i in messages) {
                    console.log(messages[i]);
                }

                // start this app
                Browser.addScript("app/assets/js/jquery.form-4.3.0.min.js");
                Browser.addScript("app/index.js");

                // hide loading image
                document.getElementById("loading").style.display = "none";
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
