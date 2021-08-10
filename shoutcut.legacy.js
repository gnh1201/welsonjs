var SS = require("lib/shadowsocks");
var XML = require("lib/xml");
var Chrome = require("lib/chrome");
var SHELL = require("lib/shell");
var SYS = require("lib/system");

var Apps = {
	LDPlayer: {},
	NoxPlayer: {},
	Chrome: {},
	ProcessName: {}
};
var AppsMutex = [];
var AppsPID = [];

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

var do_Chrome = function(args) {
    var _uniqueId = args[1];
	var _args = {};
	var _url = "https://google.com";
    for (var i = 0; i < args.length; i++) {
        var pos = args[i].indexOf('=');
        if (pos > -1) {
            if (args[i].indexOf("--") == 0) {
                _args[args[i].substring(2, pos)] = args[i].substring(pos + 1);
            } else {
                _args[args[i].substring(0, pos)] = args[i].substring(pos + 1);
            }
        } else if (args[i] != "chrome") {
			_url = args[i];
		}
	}

	var ssPort, ssPID;
	for (var uniqueId in Apps.Chrome) {
		if (_uniqueId == uniqueId && AppsMutex.indexOf("chrome_" + uniqueId) < 0) {
			console.info("Starting Google Chrome: " + uniqueId);

            var ss = SS.connect(Apps.Chrome[uniqueId]);
            ssPort = ss.listenPort;
            ssPID = ss.processID;

            console.info("Wait 3 seconds...")
            sleep(3000);

            Chrome.start(_url, ssPort, _args['profile-directory'], _args['user-data-dir'], _uniqueId);

            AppsPID.push([ssPID]);
            AppsMutex.push("chrome_" + uniqueId);
		}
	}
};

exports.main = function(args) {
    if (args.length < 1) {
        console.error("arguments could not empty.")
        return;
    }

    while (true) {
        sleep(1000);
        switch (args[0]) {
            case "chrome":
                return do_Chrome(args);
        }
    }
};
