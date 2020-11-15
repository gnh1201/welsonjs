//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var SB = require("lib/sandboxie");

var ChromeObject = function() {
	this.binPath = "%PROGRAMFILES%\\Google\\Chrome\\Application\\chrome.exe";
	this.processID = 0;
	this.profileName = "Default";
	this.proxyPort = 1080;
	this.processList = [];

	this.setBinPath = function(path) {
		this.binPath = path;
		return this;
	};

	this.setProfileName = function(s) {
		this.profileName = s;
		return this;
	};

	this.setProxyPort = function(s) {
		this.proxyPort = s;
		return this;
	};

	this.getProcessList = function() {
		return this.processList;
	};

	this.open = function(url) {
		var process;
		while (this.processID == 0) {
			try {
				process = SB.start(this.profileName, [
					this.binPath,
					"--profile-directory=" + this.profileName,
					"--proxy-server=socks5://127.0.0.1:" + this.proxyPort,
					url
				]);
				this.processID = process.ProcessID;
			} catch (e) {
				console.error(e.message);
			}
		}

		return this;
	};
};

exports.getProcessIDs = function() {
	return (new ChromeObject()).getProcessIDs();
};

exports.start = function(url, proxyPort, profileName) {
	return (new ChromeObject()).setProxyPort(proxyPort).setProfileName(profileName).open(url).processID;
};
