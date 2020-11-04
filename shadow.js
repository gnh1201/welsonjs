////////////////////////////////////////////////////////////////////////
// ShadowLoader
////////////////////////////////////////////////////////////////////////

var SS = require("lib/shadowsocks");
var SYS = require("lib/system");
var FILE = require("lib/file");
var SHELL = require("lib/shell");
var LDPlayer = require("lib/ldplayer");
var NoxPlayer = require("lib/noxplayer");
var JSON = require("lib/json");

var PIDList = [];

var NumSessions = 0;
var _NumSessions = 0;
var NumBridges = 0;
var _NumBridges = 0;

exports.main = function() {
    console.info("Waiting new launched");
    sleep(10000);

    while (true) {
        sleep(10000);

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
                if (!(title in __config.StaticIP.LDPlayer)) {
                    console.error("Not assigned static IP: " + title);
                    continue;
                } else {
                    listenPort = SS.connect(__config.StaticIP.LDPlayer[title]);
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
                sleep(10000);
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
                if (!(hostname in __config.StaticIP.NoxPlayer)) {
                    console.error("Not assigned static IP: " + hostname);
                    continue;
                } else {
                    listenPort = SS.connect(__config.StaticIP.NoxPlayer[hostname]);
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
                sleep(10000);
            }
        }
    }
};
