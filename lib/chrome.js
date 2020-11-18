//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");

var ChromeObject = function() {
	this.workingDirectory = SYS.getEnvString("PROGRAMFILES") + "\\Google\\:profileName\\Application";
	this.binPath = this.workingDirectory + "\\chrome.exe";
	this.processID = 0;
	this.profileName = "Default";
	this.proxyPort = 1080;
	this.processList = [];

	this.setBinPath = function(path) {
		this.binPath = path;
		return this;
	};

	this.setProfileName = function(profileName) {
		this.profileName = (profileName == "Default" ? "Chrome" : profileName);
		this.workingDirectory = this.workingDirectory.replace(":profileName", this.profileName);
		this.binPath = this.binPath.replace(":profileName", this.profileName);
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
		this.setProfileName(this.profileName);

		// 파일이 없는 경우, 32비트 설치 폴더에 위치하는지 한번 더 확인
		if (!FILE.fileExists(this.binPath)) {
			this.workingDirectory = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\:profileName\\Application";
			this.binPath = this.workingDirectory + "\\chrome.exe";
			this.setProfileName(this.profileName);
		}

		// 파일 찾기
		if (!FILE.fileExists(this.binPath)) {
			console.error("ChromeObject.open() -> '" + this.profileName + "' 존재하지 않는 프로파일입니다. 생성 후 사용해주세요.");
			return this;
		}

		// 바로가기 생성
		SHELL.createDesktopIcon("Chrome (" + this.profileName + ")", [
			"cscript",
			"app.js",
			"shoutcut",
			"chrome",
			this.profileName
		], SYS.getCurrentScriptDirectory());

		var process;
		while (this.processID == 0) {
			try {
				/*
				process = SB.start(this.profileName, [
					this.binPath,
					"--profile-directory=" + this.profileName,
					"--proxy-server=socks5://127.0.0.1:" + this.proxyPort,
					url
				]);
				*/
				/*
				process = SHELL.createProcess([
					this.binPath,
					"--profile-directory=" + this.profileName,
					"--proxy-server=socks5://127.0.0.1:" + this.proxyPort,
					url
				], this.workingDirectory);
				*/
				var shell = SHELL.create().setWorkingDirectory(this.workingDirectory);
				var process = shell.createProcess([
					"\"" + this.binPath + "\"",
					"--profile-directory=\"" + this.profileName + "\"",
					"--proxy-server=\"socks5://127.0.0.1:" + this.proxyPort + "\"",
					"--user-data-dir=\"" + SYS.getCurrentScriptDirectory() + "\\UserData_Chrome_" + this.profileName + "\"",
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

		return this;
	};
};

exports.getProcessIDs = function() {
	return (new ChromeObject()).getProcessIDs();
};

exports.start = function(url, proxyPort, profileName) {
	return (new ChromeObject()).setProxyPort(proxyPort).setProfileName(profileName).open(url).processID;
};
