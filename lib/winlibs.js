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
            commandOptions.push(args.join(' '));

            return SHELL.exec(commandOptions.join(' '));
        }
    };
};
