// ldplayer.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// LDPlayer API
// 
var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "LDPlayer (ldplayer.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.getList = function() {
	var data = [];
	var commands = [
		[SYS.getEnvString("SYSTEMDRIVE") + "/LDPlayer/LDPlayer4.0/ldconsole.exe", "list2"],
		[SYS.getEnvString("SYSTEMDRIVE") + "/LDPlayer/LDPlayer3.0/ldconsole.exe", "list2"],
		[SYS.getEnvString("SYSTEMDRIVE") + "/NOXGAMES/MOMO/ldconsole.exe", "list2"],
		[SYS.getEnvString("SYSTEMDRIVE") + "/XuanZhi/LDPlayer/ldconsole.exe", "list2"]
	];

	for (var i = 0; i < commands.length; i++) {
		var result = SHELL.exec(commands[i]);
		var lines = result.split(/\r?\n/);
	
		for(var k = 0; k < lines.length; k++) {
			var row = lines[k].split(',');

			if(row.length == 7) {
				data.push({
					index: row[0],
					title: row[1],
					topWindowHandle: row[2],
					binddWindowHandle: row[3],
					androidStarted: row[4],
					PID: parseInt(row[5]),
					PIDVBox: parseInt(row[6])
				});
			}
		}
	}

	return data;
};
