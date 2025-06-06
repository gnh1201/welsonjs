// virtualinput.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// VirtualInput API
// 
var VirtualInputObject = function() {
	this.oShell = null;
	this.oAutoIt = null;

	this.create = function() {
		try {
			this.oShell = CreateObject("WScript.Shell");
			this.oAutoIt = CreateObject("AutoItX3.Control");
		} catch (e) {
			console.error("VirtualInputObject.create() -> " + e.message);
		}
	};
	
	this.moveMouse = function(x, y) {
		this.oAutoIt.MouseMove(x, y);
	};

	this.sendKeys = function(s) {
		this.oShell.SendKeys(s);
	};

	this.create();
};

exports.VERSIONINFO = "VirtualInput Lib (virtualinput.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.create = function() {
	return new VirtualInputObject();
};

exports.moveMouse = function(x, y) {
	return (new VirtualInputObject()).moveMouse(x, y);
};
