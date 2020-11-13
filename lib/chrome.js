//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");

var binPath = "%PROGRAMFILES%\\Google\\Chrome\\Application\\chrome.exe";

var Chrome = function() {
	this.PID = 0;
	this.profileName = "Default";
	this.proxyPort = 1080;

	this.setProfileName = function(s) {
		this.profileName = s;
	};

	this.setProxyPort = function(s) {
		this.proxyPort = s;
	};
	
	this.getPID = function() {
		return this.PID;
	};

	this.open = function(url) {
		var process = SHELL.createProcess([
			binPath,
			"--profile-directory=" + this.profileName,
			"--proxy-server=socks5://127.0.0.1:" + this.proxyPort,
			url
		]);

		sleep(1000);

        try {
            this.PID = process.ProcessID;

            if (this.PID > 0) {
                return this;
            } else {
                console.info("Retrying call to open Chrome...");
                return this.connect();
            }
        } catch(e) {
            console.info("Retrying call to open Chrome...");
            return this.connect();
        }
	};
};

exports.start = function(url, proxyPort, profileName) {
	var instance = new Chrome();
	instance.setProfileName(profileName);
	instance.setProxyPort(proxyPort);
	instance.open(url);
	return instance.getPID();
};
