////////////////////////////////////////////////////////////////////////
// ShadowLoader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var LDPlayer = require("lib/ldplayer");
var NoxPlayer = require("lib/noxplayer");
var XML = require("lib/xml");

var PIDList = [];

var NumSessions = 0;
var _NumSessions = 0;
var NumBridges = 0;
var _NumBridges = 0;

var StaticIP = {
    LDPlayer: {},
    NoxPlayer: {}
};

var items = XML.loadXMLFile("staticip.xml").select("/StaticIP/Item").all();
for (var i = 0; i < items.length; i++) {
    try {
        var Name = items[i].selectSingleNode("Name").text;
        var UniqueID = items[i].selectSingleNode("UniqueID").text;
        var IPAddress = items[i].selectSingleNode("IPAddress").text;
        StaticIP[Name][UniqueID] = IPAddress;
    } catch(e) {
        console.error(e.message);
    }
}

exports.main = function() {
    console.info("Waiting new launched");
    sleep(3000);

    while (true) {
        sleep(3000);

        ////////////////////////////////////////////////////////////////
        // LDPlayer
        ////////////////////////////////////////////////////////////////

        var LDPList = LDPlayer.getList();
        for (var i = 0; i < LDPList.length; i++) {
            var pid = parseInt(LDPList[i].PIDVBox);
            var title = LDPList[i].title;
            if (pid > 0 && PIDList.indexOf(pid) == -1) {
                console.info("New launched LDPlayer: " + title);

                PIDList.push(pid);

                var listenPort;
                if (!(title in StaticIP.LDPlayer)) {
                    console.error("Not assigned static IP: " + title);
                    continue;
                } else {
                    listenPort = SS.connect(StaticIP.LDPlayer[title]);
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

                NumSessions = SS.getCountOfSessions();
                NumBridges = SS.getCountOfBridges();

                if (!(NumSessions > _NumSessions && NumBridges > _NumBridges)) {
                    console.error("Retrying...");
                    PIDList.pop();
                }

                _NumSessions = NumSessions;
                _NumBridges = NumBridges;

                console.info("Waiting new launched");
                sleep(3000);
            }
        }

        ////////////////////////////////////////////////////////////////
        // NoxPlayer
        ////////////////////////////////////////////////////////////////

        var NoxPList = NoxPlayer.getList();
        for (var i = 0; i < NoxPList.length; i++) {
            var pid = parseInt(NoxPList[i].PID);
            var hostname = NoxPList[i].hostname;
            if (pid > 0 && PIDList.indexOf(pid) == -1) {
                console.info("New launched NoxPlayer: " + hostname);

                PIDList.push(pid);

                var listenPort;
                if (!(hostname in StaticIP.NoxPlayer)) {
                    console.error("Not assigned static IP: " + hostname);
                    continue;
                } else {
                    listenPort = SS.connect(StaticIP.NoxPlayer[hostname]);
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

                NumSessions = SS.getCountOfSessions();
                NumBridges = SS.getCountOfBridges();

                if (!(NumSessions > _NumSessions && NumBridges > _NumBridges)) {
                    console.error("Retrying...");
                    PIDList.pop();
                }

                _NumSessions = NumSessions;
                _NumBridges = NumBridges;

                console.info("Waiting new launched");
                sleep(3000);
            }
        }
    }
};
