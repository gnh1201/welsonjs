////////////////////////////////////////////////////////////////////////
// Windows Library API
////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");

exports.loadLibrary = function(LIB) {
    return {
        call: function(FN, args) {
            var commandOptions = [];

            if (typeof(FN) === "undefined") {
                FN = "DllMain";
            }

            commandOptions.push("rundll32.exe");
            commandOptions.push(LIB + ".dll," + FN);
            commandOptions.push("\"" + args.join("\" \"") + "\"");

            return SHELL.exec(commandOptions.join(' '));
        }
    };
};

exports.openControlPanel = function(name, applets, args) {
    var shell32 = exports.loadLibrary("shell32");
    var _applets = [];
    var _args = [];
    
    // write a applets section
    _applets.push(name + ".cpl");
    if (typeof(applets) !== "undefined") {
        for (var i in applets) {
            _applets.push(applets[i]);
        }
    }

    // write a args section
    _args.push(_applets.join(','));

    // run command
    return shell32.call("Control_runDLL", _args);
};

exports.openTimezonePanel = function() {
    return exports.openControlPanel("desk", ["@0", 1]);
};

exports.openNetworkPanel = function() {
    return exports.openControlPanel("ncpa");
};
