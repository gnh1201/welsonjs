var SS = require("lib/shadowsocks");
var XML = require("lib/xml");
var Chrome = require("lib/chrome");

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

var do_Chrome = function(_uniqueId) {
	var ssPort, ssPID;
	for (var uniqueId in Apps.Chrome) {
		if (_uniqueId == uniqueId && AppsMutex.indexOf("chrome_" + uniqueId) < 0) {
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

exports.main = function(args) {
    if (args.length < 1) {
        console.error("arguments could not empty.")
        return;
    }

    while (true) {
        sleep(1000);
        switch (args[0]) {
            case "chrome":
                return do_Chrome(args[1]);
        }
    }
};
