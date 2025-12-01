// adb.js
// Namhyeon Go <gnh1201@catswords.re.kr> and the Catswords OSS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// Android Debug Bridge API
// 
var SHELL = require("lib/shell");
var SYS = require("lib/system");

// A common Android devices
function ADBObject() {
    this._interface = SHELL.create();

    this.setBinPath = function(binPath) {
        this.binPath = binPath;
        this._interface.setPrefix(this.binPath);
        return this;
    };

    this.getDevices = function() {
        var devices = [];
        var result = this._interface.exec(["devices"]);

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
        return this._interface.exec(["-s", id, "pull", path, "data\\"]);
    };

    // upload a file to target device
    this.push = function(id, filename, path) {
        return this._interface.exec(["-s", id, "push", "data\\" + filename, path]);
    };
    
    // install APK file
    this.install = function(id, filename) {
        return this._interface.exec(["-s", id, "install", "data\\" + filename]);
    };
    
    // Uninstall the App
    this.uninstall = function(id, appname) {
        return this._interface.exec(["-s", id, "uninstall", appname]);
    };

    // reboot device
    this.reboot = function(id) {
        return this._interface.exec(["-s", id, "reboot"]);
    };

    // set the binary path
    this.binPath = SYS.getAppDataDir() + "\\android_platform_tools\\adb.exe";
    this._interface.setPrefix(this.binPath);
}

// An Android Emulator
function EmulatorObject(binPath) {
    this.ADBI = (new ADBObject()).setBinPath(binPath);

    this.getProcessID = function(id) {
        var row = id.split(':');
        //var addr = row[0];
        var port = row[1];
        var cmd = "netstat -ano | findstr :" + port + " | findstr :0";
        var result = SHELL.exec(cmd);
        return result.substring(result.lastIndexOf(' '));
    };
    
    this.getList = function() {
        var items = [];
        var devices = this.ADBI.getDevices();

        for(var i = 0; i < devices.length; i++) {
            var hostname = devices[i].hostname;
            var PID = parseInt(this.getProcessID(devices[i].id));
            items.push({
                hostname: hostname,
                PID: PID
            });
        }

        return items;
    };
}

exports.create = function() {
    return new ADBObject();
};

exports.createEmulator = function(binPath) {
    return new EmulatorObject(binPath);
};

exports.VERSIONINFO = "Android Debug Bridge Interface (adb.js) version 0.2.3";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
