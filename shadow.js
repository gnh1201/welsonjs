////////////////////////////////////////////////////////////////////////
// ShadowLoader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var XML = require("lib/xml");
var LDPlayer = require("lib/ldplayer");
var NoxPlayer = require("lib/noxplayer");
var Chrome = require("lib/chrome");

var Apps = {
	LDPlayer: {},
	NoxPlayer: {},
	Chrome: {},
	ProcessName: {}
};
var AppsMutex = [];

var items = XML.load("staticip.xml").select("/StaticIP/Item").toArray();
for (var i = 0; i < items.length; i++) {
	try {
		var name = items[i].getDOM().selectSingleNode("Name").text;
		var uniqueId = items[i].getDOM().selectSingleNode("UniqueID").text;
		var ipAddress = items[i].getDOM().selectSingleNode("IPAddress").text;

		if (name in Apps) {
			Apps[name][uniqueId] = ipAddress;
		}
	} catch(e) {
		console.error(e.message);
	}
}

// App 1. LDPlayer
var check_LDPlayer = function() {
	var listenPort;
	var items = LDPlayer.getList();

	for (var i = 0; i < items.length; i++) {
		var pid = parseInt(items[i].PIDVBox);
		var title = items[i].title;
		if (pid > 0 && AppsMutex.indexOf(pid) < 0) {
			console.info("New launched LDPlayer: " + title);
			AppsMutex.push(pid);
			
			if (title in Apps.LDPlayer) {
				listenPort = SS.connect(Apps.LDPlayer[title]);
			} else {
				console.error("Not assigned static IP: " + title);
				continue;
			}

			SHELL.run([
				SYS.getCurrentScriptDirectory() + "/bin/shadow.exe",
				"-c",
				SYS.getCurrentScriptDirectory() + "/config.template.json",
				"-s",
				"socks://localhost:" + listenPort,
				"-p",
				pid
			]);

			console.info("Waiting new launched");
			sleep(3000);
		}
	}
};

// App 2. NoxPlayer
var check_NoxPlayer = function() {
	var listenPort;
	var items = NoxPlayer.getList();

	for (var i = 0; i < items.length; i++) {
		var pid = parseInt(items[i].PID);
		var hostname = items[i].hostname;

		if (pid > 0 && AppsMutex.indexOf(pid) < 0) {
			console.info("New launched NoxPlayer: " + hostname);
			AppsMutex.push(pid);

			if (hostname in Apps.NoxPlayer) {
				listenPort = SS.connect(Apps.NoxPlayer[hostname]);
			} else {
				console.error("Not assigned static IP: " + hostname);
				continue;
			}

			SHELL.run([
				SYS.getCurrentScriptDirectory() + "/bin/shadow.exe",
				"-c",
				SYS.getCurrentScriptDirectory() + "/config.template.json",
				"-s",
				"socks://localhost:" + listenPort,
				"-p",
				pid
			]);
			
			console.info("Waiting new launched");
			sleep(3000);
		}
	}
};

// App 3. Chrome
var check_Chrome = function() {
	var listenPort, pid;
	for (var uniqueId in Apps.Chrome) {
		if (AppsMutex.indexOf("chrome_" + uniqueId) < 0) {
			console.info("Starting Google Chrome: " + uniqueId);

			listenPort = SS.connect(Apps.Chrome[uniqueId]);
			Chrome.start("https://www.showmyip.com/", listenPort, uniqueId);
			AppsMutex.push("chrome_" + uniqueId);
		}
	}
};

var main = function() {
	console.info("Waiting new launched");

	while (true) {
		sleep(3000);
		check_LDPlayer();

		sleep(3000);
		check_NoxPlayer();

		sleep(3000);
		check_Chrome();
	}
};

exports.main = main;
