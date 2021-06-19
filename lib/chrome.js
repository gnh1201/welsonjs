//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");
var HTTP = require("lib/http");

var ChromeObject = function() {
	this.workingDirectory = SYS.getEnvString("PROGRAMFILES") + "\\Google\\Chrome\\Application";
	this.binPath = SYS.getEnvString("PROGRAMFILES") + "\\Google\\:installedDir\\Application\\chrome.exe";
	//this.processID = 0;
	this.installedDir = "Chrome";
	this.profileName = "Default";
	this.userDataDir = null;
	this.proxy = {
		"protocol": "socks5",
		"host": "127.0.0.1",
		"port": 1080
	};
	this.inPrivate = false;
	this.debuggingPort = 0;

	this.setBinPath = function(path) {
		this.binPath = path;
		return this;
	};

	this.setProfile = function(profileName, installedDir) {
		this.profileName = (profileName == "Default" ? "Chrome" : profileName);
		this.installedDir = installedDir;
		this.workingDirectory = this.workingDirectory.replace(":installedDir", this.installedDir);
		this.binPath = this.binPath.replace(":installedDir", this.installedDir);
		//this.binPath = this.binPath.replace(":installedDir", "Chrome");
		return this;
	};

	this.setUserDataDir = function(dirname) {
		this.userDataDir = dirname;
		return this;
	};

	this.setInstalledDir = function(dirname) {
		this.installedDir = dirname;
		return this;
	};
	
	this.setProxy = function(obj) {
		this.proxy = obj;
		return this;
	};

	this.setProxyProtocol = function(s) {
		this.proxy.protocol = s;
		return this;
	};

	this.setProxyHost = function(s) {
		this.proxy.host = s;
		return this;
	};

	this.setProxyPort = function(n) {
		this.proxy.port = n;
		return this;
	};
	
	this.setDebuggingPort = function(n) {
		this.debuggingPort = (typeof(n) !== "number" ? this.debuggingPort : n);
	}

	this.createShoutcut = function(url) {
		if (!this.userDataDir) {
			this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
		}

		var cmd = [
			"cscript",
			"app.js",
			"shoutcut",
			"chrome",
			this.installedDir,
		];

		if (this.inPrivate) {
			cmd.push("--incognito");
		}
		
		if (this.debuggingPort > 0) {
			cmd.push("--remote-debugging-port=" + this.debuggingPort);
		}

		cmd.push("--profile-directory=\"" + this.profileName + "\"");
		cmd.push("--user-data-dir=\"" + this.userDataDir + "\"");
		cmd.push("\"" + url + "\"");

		SHELL.createDesktopIcon("Chrome Prototype (" + this.installedDir + ")", cmd.join(' '), SYS.getCurrentScriptDirectory());
	};

	this.setInPrivate = function(flag) {
		this.inPrivate = flag;
		return this;
	};

	this.open = function(url) {
		this.setProfile(this.profileName, this.installedDir);

		// if the file does not exists, Check the 32bit installation folder again
		if (!FILE.fileExists(this.binPath)) {
			this.workingDirectory = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\Chrome\\Application";
			this.binPath = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\:installedDir\\Application\\chrome.exe";
			this.setProfile(this.profileName, this.installedDir);
		}

		// find profile
		if (!FILE.fileExists(this.binPath)) {
			console.error("ChromeObject.open() -> '" + this.profileName + "' profile does not exists. You have to create it.");
			return this;
		}

		// create shoutcut to desktop
		this.createShoutcut();

		/*
		var process;
		while (this.processID == 0) {
			try {
				if (!this.userDataDir) {
					this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
				}

				var shell = SHELL.create().setWorkingDirectory(this.workingDirectory);
				var process = shell.createProcess([
					"\"" + this.binPath + "\"",
					"--profile-directory=\"" + this.profileName + "\"",
					"--proxy-server=\"socks5://127.0.0.1:" + this.proxyPort + "\"",
					"--user-data-dir=\"" + this.userDataDir + "\"",
					"\"" + url + "\""
				].join(" "));
				sleep(1500);
				this.processID = process.ProcessID;
				sleep(1500);
				shell.release();
			} catch (e) {
				console.error("ChromeObject.open() -> " + e.message);
				sleep(1500);
			}
		}
		*/

		try {
			if (!this.userDataDir) {
				this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
			}

			var shell = SHELL.create();
			shell.setWorkingDirectory(this.workingDirectory);
			
			var cmd = [];
			
			if (this.inPrivate) {
				cmd.push("--incognito");
			}

			if (this.debuggingPort > 0) {
				cmd.push("--remote-debugging-port=" + this.debuggingPort);
			}

			cmd.push("--profile-directory=\"" + this.profileName + "\"");
			cmd.push("--proxy-server=\"" + this.proxy.protocol + "://" + this.proxy.host + ":" + this.proxyPort + "\"");
			cmd.push("--user-data-dir=\"" + this.userDataDir + "\"");
			cmd.push("\"" + url + "\"");
			shell.runAs(this.binPath, cmd);

			sleep(1500);
			shell.release();
		} catch (e) {
			console.error("ChromeObject.open() -> " + e.message);
			sleep(1500);
		}

		return this;
	};
	
	this.getPageList = function() {
		if (this.debuggingPort > 0) {
			return HTTP.get("http://localhost:" + this.debuggingPort + "/json");
		} else {
			console.error("Remote debugging unavailable");
			return [];
		}
	};

	this.getPageById = function(id) {
		var pages = this.getPageList().filter(function(x) {
			return (pageList[k].id = id);
		});
		return (pages.length > 0 ? pages[0] : null);
	};
	
	this.getPagesByUrl = function(url) {
		return this.getPageList().filter(function(x) {
			return (x.url == url);
		});
	};

	this.getPagesByTitle = function(title) {
		return this.getPageList().filter(function(x) {
			return (x.title == title);
		});
	};
};

exports.create = function() {
	return new ChromeObject();
};

exports.start = function(url, proxyPort, profileName, userDataDir, installedDir) {
	return (new ChromeObject())
		.setProxyPort(proxyPort)
		.setProfile(profileName, installedDir)
		.setUserDataDir(userDataDir)
		.setInstalledDir(installedDir)
		.open(url)
	;
};

exports.startWithDebugging = function(url, proxy, profileName, userDataDir, installedDir, debuggingPort) {
	return (new ChromeObject())
		.setProxy(proxy)
		.setProfile(profileName, installedDir)
		.setUserDataDir(userDataDir)
		.setInstalledDir(installedDir)
		.setDebuggingPort(debuggingPort)
		.open(url)
	;
};
