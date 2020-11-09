//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");

var binPath = "%PROGRAMFILES%\\Google\\Chrome\\Application\\chrome.exe";

var Chrome = function() {
	this.processID = 0;
	this.profileName = "Default";
	this.proxyPort = 1080;

	this.setProfileName = function(s) {
		this.profileName = s;
	};

	this.setProxyPort = function(s) {
		this.proxyPort = s;
	};
	
	this.getProcessID = function() {
		return this.processID;
	};

	this.open = function(url) {
		var process = SHELL.createProcess([
			binPath,
			"--profile-directory=" + this.profileName,
			"--proxy-server=socks5://127.0.0.1:" + this.proxyPort,
			url
		]);
		sleep(1000);
		this.processID = process.ProcessID;
	};
};

exports.start = function(url, proxyPort, profileName) {
	var instance = new Chrome();
	instance.setProfileName(profileName);
	instance.setProxyPort(proxyPort);
	instance.open(url);
	return instance.getProcessID();
};
