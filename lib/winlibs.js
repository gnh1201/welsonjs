////////////////////////////////////////////////////////////////////////
// Windows Library API
////////////////////////////////////////////////////////////////////////

exports.VERSIONINFO = "Windows Lib (winlibs.js) version 0.1";
exports.global = global;
exports.require = global.require;

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

exports.SHELL32 = (function() {
    return exports.loadLibrary("SHELL32");
})();

exports.showControlPanel = function(name, applets, args) {
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
    return exports.SHELL32.call("Control_runDLL", _args);
};

exports.showNetworkAdapters = function() {
    return exports.openControlPanel("ncpa", ["@0", 3]);
};

exports.showWindowsCopyright = function() {
    return exports.SHELL32.call("ShellAboutW");
};
