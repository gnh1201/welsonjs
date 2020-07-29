////////////////////////////////////////////////////////////////////////
// Windows Library API
////////////////////////////////////////////////////////////////////////

exports.VERSIONINFO = "Windows Lib (winlibs.js) version 0.1";
exports.global = global;
exports.require = global.require;

var SHELL = require("lib/shell");
var FILE = require("lib/file");

/**
 * @param {string} LIB
 * @return {string} clsid
 */
exports.createManifest = function(FN, clsid) {
    var manifestPath = FN + ".manifest";

    if (typeof(clsid) !== "undefined") {
        clsid = "4B72FC46-C543-4101-80DB-7777848DACDC";
    }

    var lines = [
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>",
        "<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\">",
        "<file name=\"" + FN + ".dll\">",
        "<comClass clsid=\"{" + clsid + "}\" threadingModel=\"Apartment\" progid=\"" + FN + "\" />",
        "</file>",
        "</assembly>"
    ];

    // write a manifest file
    FILE.writeFile(manifestPath, lines.join("\r\n"), "utf-8");

    return FILE.fileExists(manifestPath);
};

/**
 * @param {string} LIB
 * @return {function}
 */
exports.loadLibrary = function(LIB) {
    var dllManifest = LIB + ".manifest";

    if (FILE.fileExists(dllManifest)) {
        var actCtx = CreateObject("Microsoft.Windows.ActCtx");
        actCtx.Manifest = dllManifest;
        try {
            var DX = actCtx.CreateObject("MessageBox");
            return {
                call: function(FN, args) {
                    return DX[FN].call(this, args);
                }
            }
        } catch(e) {
            // return null;
        }
    } else {
        return {
            call: function(FN, args) {
                var cmd = [
                    "rundll32.exe"
                ];
                if (typeof(FN) === "undefined") {
                    FN = "DllMain";
                }
                cmd.push(LIB + ".dll," + FN);
                if (typeof(args) !== "undefined") {
                    cmd = cmd.concat(args);
                }
                return SHELL.exec(cmd);
            }
        };
    }
};

/**
 */
exports.SHELL32 = exports.loadLibrary("SHELL32");

/**
 * @param {string} name
 * @param {Object} applets
 * @param {Array} applets
 * @return {string}
 */
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

/**
 */
exports.showNetworkAdapters = function() {
    return exports.openControlPanel("ncpa", ["@0", 3]);
};

/**
 */
exports.showWindowsCopyright = function() {
    return exports.SHELL32.call("ShellAboutW");
};
