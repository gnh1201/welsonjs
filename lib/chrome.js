//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");

var ChromeObject = function() {
	this.workingDirectory = SYS.getEnvString("PROGRAMFILES") + "\\Google\\Chrome\\Application";
	this.binPath = SYS.getEnvString("PROGRAMFILES") + "\\Google\\:installedDir\\Application\\chrome.exe";
	this.processID = 0;
	this.installedDir = "Chrome";
	this.profileName = "Default";
	this.userDataDir = null;
	this.proxyPort = 1080;
	this.processList = [];

	this.setBinPath = function(path) {
		this.binPath = path;
		return this;
	};

	this.setProfile = function(profileName, installedDir) {
		this.profileName = (profileName == "Default" ? "Chrome" : profileName);
		this.installedDir = installedDir;
		//this.workingDirectory = this.workingDirectory.replace(":installedDir", this.installedDir);
		//this.binPath = this.binPath.replace(":installedDir", this.installedDir);
		this.binPath = this.binPath.replace(":installedDir", "Chrome");
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

	this.setProxyPort = function(s) {
		this.proxyPort = s;
		return this;
	};

	this.getProcessList = function() {
		return this.processList;
	};

	this.createShoutcut = function(url) {
		if (!this.userDataDir) {
			this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
		}

		SHELL.createDesktopIcon("Chrome Prototype (" + this.installedDir + ")", [
			"cscript",
			"app.js",
			"shoutcut",
			"chrome",
			this.installedDir,
			"--profile-directory=\"" + this.profileName + "\"",
			"--user-data-dir=\"" + this.userDataDir + "\"",
			url
		].join(' '), SYS.getCurrentScriptDirectory());
	};

	this.open = function(url) {
		this.setProfile(this.profileName, this.installedDir);

		// 파일이 없는 경우, 32비트 설치 폴더에 위치하는지 한번 더 확인
		if (!FILE.fileExists(this.binPath)) {
			this.workingDirectory = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\Chrome\\Application";
			this.binPath = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\:installedDir\\Application\\chrome.exe";
			this.setProfile(this.profileName, this.installedDir);
		}

		// 파일 찾기
		if (!FILE.fileExists(this.binPath)) {
			console.error("ChromeObject.open() -> '" + this.profileName + "' 존재하지 않는 프로파일입니다. 생성 후 사용해주세요.");
			return this;
		}

		// 바로가기 생성
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
				sleep(1000);
				this.processID = process.ProcessID;
				sleep(1000);
				shell.release();
			} catch (e) {
				console.error("ChromeObject.open() -> " + e.message);
				sleep(1000);
			}
		}
		*/

		try {
			if (!this.userDataDir) {
				this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
			}

			var shell = SHELL.create();
			shell.setWorkingDirectory(this.workingDirectory);
			shell.runAs(this.binPath, [
				"--profile-directory=\"" + this.profileName + "\"",
				"--proxy-server=\"socks5://127.0.0.1:" + this.proxyPort + "\"",
				"--user-data-dir=\"" + this.userDataDir + "\"",
				"\"" + url + "\""
			]);
			sleep(3000);
			shell.release();
		} catch (e) {
			console.error("ChromeObject.open() -> " + e.message);
			sleep(1000);
		}

		return this;
	};
};

exports.create = function() {
	return  new ChromeObject();
};

exports.getProcessIDs = function() {
	return (new ChromeObject()).getProcessIDs();
};

exports.start = function(url, proxyPort, profileName, userDataDir, installedDir) {
	return (new ChromeObject())
		.setProxyPort(proxyPort)
		.setProfile(profileName, installedDir)
		.setUserDataDir(userDataDir)
		.setInstalledDir(installedDir)
		.open(url)
		.processID
	;
};
