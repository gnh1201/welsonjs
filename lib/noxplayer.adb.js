////////////////////////////////////////////////////////////////////////
// LDPlayer API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "NoxPlayer (noxplayer.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.binPath = SYS.getEnvString("PROGRAMFILES(X86)") + "/Nox/bin/nox_adb.exe";

exports.getDevices = function() {
    var data = [];
    var cmd = [
        exports.binPath,
        "devices"
    ]
    var result = SHELL.exec(cmd);
    var lines = result.split(/\r?\n/);
    for(var i = 0; i < lines.length; i++) {
        var row = lines[i].split(/\s+/);
        
        if(row.length == 2) {
            data.push({
                host: row[0],
                type: row[1]
            });
        }
    }

    return data;
};

exports.getHostname = function(host) {
	return SHELL.exec([
		exports.binPath,
		"-s",
		host,
		"shell",
		"getprop",
		"net.hostname"
	]).trim();
};

exports.getPID = function(host) {
	var row = host.split(':');
	var addr = row[0];
	var port = row[1];
	var cmd = "netstat -ano | findstr :" + port + " | findstr :0";
	var result = SHELL.exec(cmd);
	return result.substring(result.lastIndexOf(' '));
};

exports.getList = function() {
	var data = [];
    var devices = exports.getDevices();

    for(var i = 0; i < devices.length; i++) {
		var hostname = exports.getHostname(devices[i].host);
		var pid = exports.getPID(devices[i].host);

		data.push({
			hostname: hostname,
			PID: parseInt(pid)
		});
	}

    return data;
};
