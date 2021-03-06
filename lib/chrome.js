//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");
var HTTP = require("lib/http");
var Websocket = require("lib/websocket");

// for remote debugging
var pageEventId = 0;

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
	this.oAutoIt = null;

	// for remote debugging
	this.debuggingPort = 0;
	this.pageId = "";
	this.ws = Websocket.create();

	this.create = function() {
		try {
			this.oAutoIt = CreateObject("AutoItX3.Control");
		} catch (e) {
			console.log("ChromeObject.create() -> " + e.message);
		}
	};

	this.setBinPath = function(path) {
		this.binPath = path;
		return this;
	};

	this.setProfile = function(profileName, installedDir) {
		this.profileName = (profileName == "Default" ? "Chrome" : profileName);
		this.setInstalledDir(installedDir);
		this.workingDirectory = this.workingDirectory.replace(":installedDir", this.installedDir);
		this.binPath = this.binPath.replace(":installedDir", this.installedDir);
		//this.binPath = this.binPath.replace(":installedDir", "Chrome");
		return this;
	};

	this.setUserDataDir = function(dirname) {
		if (dirname != null) {
			this.userDataDir = dirname;
		} else {
			this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
		}
		return this;
	};

	this.setInstalledDir = function(dirname) {
		if (dirname != null) {
			this.installedDir = dirname;
		}
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
		return this;
	}
	
	this.setPageId = function(s) {
		this.pageId = s;
	};

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
		if (this.debuggingPort == 0) {
			this.createShoutcut();
		}

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
			
			if (this.proxy != null) {
				console.log("--proxy-server=\"" + this.proxy.protocol + "://" + this.proxy.host + ":" + this.proxy.port + "\"");
				cmd.push("--proxy-server=\"" + this.proxy.protocol + "://" + this.proxy.host + ":" + this.proxy.port + "\"");
			}
			
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
			return JSON.parse(HTTP.get("http://127.0.0.1:" + this.debuggingPort + "/json"));
		} else {
			console.error("Remote debugging unavailable");
			return [];
		}
	};

	this.getPageById = function(id) {
		var pages = this.getPageList().filter(function(x) {
			return (x.id = id);
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
	
	this.sendPageRPC = function(method, params) {
		var result = null;

		try {
			if (this.pageId != "") {
				result = this.ws.send("ws://127.0.0.1:" + this.debuggingPort + "/devtools/page/" + this.pageId, JSON.stringify({
					"id": pageEventId,
					"method": method,
					"params": params
				}));
				pageEventId++;
				console.info("ChromeObject().sendPageRPC() -> Sent");
			} else {
				var pageList = this.getPageList();
				if (pageList instanceof Array && pageList.length > 0) {
					this.pageId = pageList[0].id;
					if (this.pageId != "") {
						result = this.sendPageRPC(method, params);
					} else {
						console.error("Got invaild list of pages");
					}
				}
			}
		} catch (e) {
			console.log("ChromeObject.sendPageRPC() -> " + e.message);
		}

		return result;
	};

	this.navigate = function(url) {
		return this.sendPageRPC("Page.navigate", {
			"url": url
		});
	};

	this.evaluate = function(expression) {
		try {
			return this.sendPageRPC("Runtime.evaluate", {
				"expression": expression
			});
		} catch (e) {
			console.error("ChromeObject.evaluate() -> " + e.message);
		}
	};

	this.close = function() {
		return this.sendPageRPC("Browser.close", {});
	};

	this.focus = function() {
		if (this.debuggingPort > 0) {
			try {
				// change webpage title for focusing window
				this.setTitle(this.pageId);
				sleep(1500);

				// find window by title
				var pageList = this.getPageList();
				for (var i = 0; i < pageList.length; i++) {
					if (pageList[i].id == this.pageId) {
						this.oAutoIt.WinActivate(pageList[i].title);
					}
				}
			} catch (e) {
				console.error("ChromeObject.focus() -> " + e.message);
			}
		}
	};
	
	this.blur = function() {
		return this.evaluate("window.blur()");
	};
	
	this.downMouseWheel = function(times) {
		if (this.debuggingPort > 0) {
			try {
				var pos = this.getScreenPosition();
				this.oAutoIt.MouseMove(pos.x + 100, pos.y + 100);
				this.oAutoIt.MouseWheel("down", times);
			} catch (e) {
				console.error("ChromeObject.downMouseWheel() -> " + e.message);
			}
		}
	};

	this.upMouseWheel = function(times) {
		if (this.debuggingPort > 0) {
			try {
				var pos = this.getScreenPosition();
				this.oAutoIt.MouseMove(pos.x + 100, pos.y + 100);
				this.oAutoIt.MouseWheel("up", times);
			} catch (e) {
				console.error("ChromeObject.upMouseWheel() -> " + e.message);
			}
		}
	};
	
	this.setTitle = function(title) {
		if (this.debuggingPort > 0) {
			this.evaluate('document.title = "' + title + ' " + document.title');
		}
	};

	this.getScreenPosition = function() {
		var response = this.evaluate('(function() { return [window.screenX, window.screenY].join(","); })();');
		var result = JSON.parse(response).result.result.value;
		var pos = result.split(',');
		return {
			"x": parseInt(pos[0]),
			"y": parseInt(pos[1])
		};
	};

	this.getPageHeight = function() {
		var height = 0;

		if (this.debuggingPort > 0) {
			var response = this.evaluate('(function(obj) { return Math.max(obj.scrollHeight, obj.clientHeight); })(document.querySelector("html"))');
			var result = JSON.parse(response).result.result.value;
			height = parseInt(result);
		}

		return height;
	};

	this.create();
};

exports.create = function() {
	return new ChromeObject();
};

exports.start = function(url, proxyPort, profileName, userDataDir, installedDir) {
	return (new ChromeObject())
		.setProxyPort(proxyPort)
		.setProfile(profileName, installedDir)
		.setUserDataDir(userDataDir)
		.open(url)
	;
};

exports.startWithDebugging = function(url, proxy, profileName, debuggingPort) {
	return (new ChromeObject())
		.setProxy(proxy)
		.setProfile(profileName, null)
		.setUserDataDir(null)
		.setDebuggingPort(debuggingPort)
		.open(url)
	;
};
