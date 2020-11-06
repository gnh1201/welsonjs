////////////////////////////////////////////////////////////////////////
// Shadowsocks API
////////////////////////////////////////////////////////////////////////

var CONFIG = require("lib/config");
var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "Shadowsocks Lib (shadowsocks.js) version 0.1";
exports.global = global;
exports.require = global.require;
exports.binPath = "bin\\ss-local.exe";

exports.getRandomInt = function(min, max) {
    var x = Math.random();
    return min + Math.floor((max - min) * x);
};

exports.connect = function(host) {
    var listenPort = exports.getRandomInt(49152, 65535);

    SHELL.run([
        exports.binPath,
        "-s",
        host,
        "-p",
        CONFIG.readConfig("/Config/SSPort").first().text,
        "-l",
        listenPort,
        "-k",
        CONFIG.readConfig("/Config/SSPassword").first().text,
        "-m",
        CONFIG.readConfig("/Config/SSCipher").first().text
    ], true);

    return listenPort;
};

exports.getCountByProcessName = function(processName) {
	var num = 0;
    var cmd = "tasklist | findstr " + processName;
    var result = SHELL.exec(cmd);
    var lines = result.split(/\r?\n/);
    for(var i = 0; i < lines.length; i++) {
		var row = lines[i].split(/\s+/);
		if(row[0] == processName) {
			num++;
		}
	}
	return num;
};

exports.getAvailableProcesses = function() {
	var PIDs = [];
    var cmd = "tasklist | findstr .exe";
    var result = SHELL.exec(cmd);
    var lines = result.split(/\r?\n/);
    for(var i = 0; i < lines.length; i++) {
		var row = lines[i].split(/\s+/);
        PIDs.push({
            PID: row[1],
            name: row[0]
        });
	}
	return PIDs;
};

exports.getCountOfSessions = function() {
	return exports.getCountByProcessName("ss-local.exe");
};

exports.getCountOfBridges = function() {
	return exports.getCountByProcessName("shadow.exe");
};
