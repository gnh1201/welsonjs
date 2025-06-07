// noxplayer.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// NoxPlayer API
// 
var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "NoxPlayer (noxplayer.js) version 0.2";
exports.global = global;
exports.require = global.require;

exports.getList = function() {
	var data = [];
	var commands = [
		[SYS.getEnvString("PROGRAMFILES(X86)") + "/Nox/bin/NoxConsole", "list"]
	];

	for (var i = 0; i < commands.length; i++) {
		var result = SHELL.exec(commands[i]);
		var lines = result.split(/\r?\n/);
	
		for(var k = 0; k < lines.length; k++) {
			var row = lines[k].split(',');

			if(row.length == 7) {
				data.push({
					index: row[0],
					name: row[1],
					title: row[2],
					handle1: row[3],
					handle2: row[4],
					handle3: row[5],
					PID: parseInt(row[6]),
					handle4: -1
				});
			} else if(row.length == 8) {
				data.push({
					index: row[0],
					name: row[1],
					title: row[2],
					handle1: row[3],
					handle2: row[4],
					handle3: row[5],
					PID: parseInt(row[6]),
					handle4: row[7]
				});
			}
		}
	}

	return data;
};
