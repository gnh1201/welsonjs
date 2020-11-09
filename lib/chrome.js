//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");

var binPath = "%PROGRAMFILES%\\Google\\Chrome\\Application\\chrome.exe";

var Chrome = function() {
	this.profileName = "Default";
	this.proxyPort = 1080;

	this.setProfileName = function(s) {
		this.profileName = s;
	};

	this.setProxyPort = function(s) {
		this.proxyPort = s;
	};

	this.open = function(url) {
		SHELL.createProcess([
			binPath,
			"--profile-directory=" + this.profileName,
			"--proxy-server=socks5://127.0.0.1:" + this.proxyPort,
			url
		]);
	};
};

exports.start = function(url, proxyPort, profileName) {
	var instance = new Chrome();
	instance.setProfileName(profileName);
	instance.setProxyPort(proxyPort);
	instance.open(url);
};
