////////////////////////////////////////////////////////////////////////
// Powershell Interface API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

/*

// new powershell interface

function PowershellInterface() {
    this.type = -1;
    this.target = null;

    this.load = function(script) {
        this.target = script;
        this.type = 0;
    };

    this.loadFile = function(filename) {
        this.target = filename;
        this.type = 1;
    };

    this.loadRemoteUrl = function(url) {
        this.target = url;
        this.type = 2;
    };

    // For example:
    //   file:C:\\a\\b\\c
    //   http://
    //   https://
    //   data:text/plain;base64,SGVsbG8sIFdvcmxkIQ==
    this.loadUri = function(uri) {
        var pos = uri.indexOf(':');
        var scheme = (pos < 0 ? '' : url.substring(0, pos));
        var target = (pos < 0 ? uri : url.substring(pos + 1));

        switch (scheme) {
            case 'http':
            case 'https':
                this.loadRemoteUrl(target);
                break;
            
            case 'file':
                this.load(target);
                break;
        }
    };

    this.exec = function() {
        switch (this.type) {
            case 2:
                // todo
                break;
            
            case 1:
                // todo
                break;
            
            case 0:
                // todo
                break;
                
            default:
                break;
        }
    };
}

*/


function execScript(scriptName, args) {
    var cmd = [
        "powershell.exe",
        "-NoProfile",
        "-ExecutionPolicy",
        "ByPass",
        "-nologo",
        "-file",
        scriptName + ".ps1"
    ];

    if (typeof(cmd) !== "undefined") {
        cmd = cmd.concat(args);
    }

    return SHELL.exec(cmd);
};

function execCommand(cmd) {
    return SHELL.exec([
        "powershell.exe",
        "-NoProfile",
        "-ExecutionPolicy",
        "ByPass",
        "-nologo",
        "-Command",
        "& {" + cmd + "}"
    ]);
};

function runAs(cmd) {
    return execCommand("Start-Process cmd \"/q /c " + SHELL.addslashes(SHELL.makeCmdLine(cmd)) + "\" -Verb RunAs");
};

exports.execScript = execScript;
exports.execCommand = execCommand;
exports.runAs = runAs;

exports.VERSIONINFO = "Powershell (powershell.js) version 0.1.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
