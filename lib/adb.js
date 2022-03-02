////////////////////////////////////////////////////////////////////////
// Android Debug Bridge API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

function ADBObject() {
    this.binPath = "bin\\platform-tools_r33.0.0-windows\\platform-tools\\adb.exe";

    this.setBinPath = function(binPath) {
        this.binPath = binPath;
        return this;
    };

    this.getDevices = function() {
        var devices = [];
        var result = SHELL.exec([this.binPath, "devices"]);

        splitLn(result).forEach(function(line) {
            var row = line.split(/\s+/);
            if(row.length == 2) {
                devices.push({
                    id: row[0],
                    hostname: this.getHostname(row[0]),
                    type: row[1]
                });
            }
        });

        return devices;
    };

    this.getHostname = function(id) {
        var result = this.getProperty(id, "net.hostname");
        if (typeof result === "string") {
            return result.trim();
        } else {
            return "";
        }
    };

    this.getProperty = function(id, name) {
        return this.sendShell(id, ["getprop", name]);
    };
  
    this.disableService = function(id, name) {
       return this.sendShell(id, ["svc", name, "disable"]);
    };

    this.enableService = function(id, name) {
       return this.sendShell(id, ["svc", name, "enable"]);
    };

    this.sendShell = function(id, args) {
        try {
            return SHELL.exec([this.binPath, "-s", id, "shell"].concat(args));
        } catch (e) {
            return "";
        }
    };

    // download a file from target device 
    this.pull = function(id, path) {
        return SHELL.exec([this.binPath, "-s", id, "pull", path, "data\\"]);
    };

    // upload a file to target device
    this.push = function(id, filename, path) {
        return SHELL.exec([this.binPath, "-s", id, "push", "data\\" + filename, path]);
    };
}

exports.create = function() {
    return new ADBObject();
};

exports.VERSIONINFO = "Android Debug Bridge Interface (adb.js) version 0.2";
exports.global = global;
exports.require = global.require;
