////////////////////////////////////////////////////////////////////////
// Android Debug Bridge API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

function ADBObject() {
    this.binPath = "bin\\platform-tools_r33.0.0-windows\platform-tools\\adb.exe";

    this.setBinPath = function(binPath) {
        this.binPath = binPath;
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
    };

    this.getHostname = function(id) {
       return this.getProperty(id, "net.hostname").trim();
    };

    this.getProperty = function(id, name) {
        return SHELL.exec([this.binPath, "-s", id, "shell", "getprop", name]);
    };
  
    this.disableService = function(id, name) {
       return SHELL.exec([this.binPath, "-s", id, "shell", "svc", name, "disable"]);
    };

    this.enableService = function(id, name) {
       return SHELL.exec([this.binPath, "-s", id, "shell", "svc", name, "enable"]);
    };
}

exports.VERSIONINFO = "Android Debug Bridge Interface (adb.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.create = function() {
    return new ADBObject();
};
