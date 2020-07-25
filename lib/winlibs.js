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

exports.openControlPanel = function(name, args) {
    var shell32 = exports.loadLibrary("shell32");
    var _args = [];

    _args.push(name + ".cpl");

    if (typeof(args) !== "undefined") {
        for (var i in args) {
            _args.push(args[i]);
        }
    }

    return shell32.call("Control_runDLL", _args);
};

exports.openNetworkControlPanel = function() {
    return exports.openControlPanel("ncpa");
};
