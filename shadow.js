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
var AppsPID = [];

var getAvailablePID = function() {
	var items = [];
    var cmd = "tasklist | findstr .exe";
    var result = SHELL.exec(cmd);
    var lines = result.split(/\r?\n/);
    for(var i = 0; i < lines.length; i++) {
		var row = lines[i].split(/\s+/);
        items.push(row[1]);
	}
	return items;
};

var items = XML.load("staticip.xml").select("/StaticIP/Item").toArray();
for (var i = 0; i < items.length; i++) {
	try {
		var name = items[i].getDOM().selectSingleNode("Name").text;
		var uniqueId = items[i].getDOM().selectSingleNode("UniqueID").text;
		var ipAddress = items[i].getDOM().selectSingleNode("IPAddress").text;

		if (name in Apps) {
			Apps[name][uniqueId] = ipAddress;
		}
	} catch (e) {}
}

// App 1. LDPlayer
var check_LDPlayer = function() {
    var ssPort, ssPID, shadowPID = 0;
	var items = LDPlayer.getList();

	for (var i = 0; i < items.length; i++) {
		var pid = items[i].PIDVBox;
		var title = items[i].title;
		if (pid > 0 && AppsMutex.indexOf(pid) < 0) {
			console.info("New launched LDPlayer: " + title);
			AppsMutex.push(pid);

			if (title in Apps.LDPlayer) {
                var ss = SS.connect(Apps.LDPlayer[title]);
                ssPort = ss.listenPort;
                ssPID = ss.processID;
			} else {
				console.error("Not assigned static IP: " + title);
				continue;
			}

            var process;
            while (!(shadowPID > 0)) {
                process = SHELL.createProcess([
                    SYS.getCurrentScriptDirectory() + "/bin/shadow.exe",
                    "-c",
                    SYS.getCurrentScriptDirectory() + "/config.template.json",
                    "-s",
                    "socks://localhost:" + ssPort,
                    "-p",
                    pid
                ]);
                sleep(1000);
                shadowPID = process.ProcessID;
            }

            AppsPID.push([pid, ssPID, shadowPID]);

			console.info("Waiting new launched");
			sleep(3000);
		}
	}
};

// App 2. NoxPlayer
var check_NoxPlayer = function() {
    var ssPort, ssPID, shadowPID = 0;
	var items = NoxPlayer.getList();

	for (var i = 0; i < items.length; i++) {
		var pid = items[i].PID;
		var hostname = items[i].hostname;

		if (pid > 0 && AppsMutex.indexOf(pid) < 0) {
			console.info("New launched NoxPlayer: " + hostname);
			AppsMutex.push(pid);

			if (hostname in Apps.NoxPlayer) {
                var ss = SS.connect(Apps.NoxPlayer[hostname]);
                ssPort = ss.listenPort;
                ssPID = ss.processID;
			} else {
				console.error("Not assigned static IP: " + hostname);
				continue;
			}

            var process;
            while (!(shadowPID > 0)) {
                process = SHELL.createProcess([
                    SYS.getCurrentScriptDirectory() + "/bin/shadow.exe",
                    "-c",
                    SYS.getCurrentScriptDirectory() + "/config.template.json",
                    "-s",
                    "socks://localhost:" + ssPort,
                    "-p",
                    pid
                ]);
                sleep(1000);
                shadowPID = process.ProcessID;
            }

            AppsPID.push([pid, ssPID, shadowPID]);

			console.info("Waiting new launched");
			sleep(3000);
		}
	}
};

// App 3. Chrome
var check_Chrome = function() {
	var ssPort, ssPID;
	for (var uniqueId in Apps.Chrome) {
		if (AppsMutex.indexOf("chrome_" + uniqueId) < 0) {
			console.info("Starting Google Chrome: " + uniqueId);

            var ss = SS.connect(Apps.Chrome[uniqueId]);
            ssPort = ss.listenPort;
            ssPID = ss.processID;

            console.info("Wait 10 seconds...")
            sleep(10000);

            Chrome.start("https://whatismyipaddress.com/", ssPort, uniqueId);

            AppsPID.push([ssPID]);
            AppsMutex.push("chrome_" + uniqueId);
		}
	}
};

// Check dead processes
var check_Exits = function() {
    var alivePIDList = SYS.getProcessList().reduce(function(acc, process) {
        acc.push(process.ProcessID);
    }, []);

    AppsPID.forEach(function(v1) {
        v1.forEach(function(v2) {
            if (alivePIDList.indexOf(v2) < 0) {
                console.warn("Detected dead process: " + v2);
                console.warn("Will be kill related processes.");

                v1.forEach(function(v2) {
                    SYS.killProcess(v2);
                });

                return;
            }
        });
    });
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
