////////////////////////////////////////////////////////////////////////
// NoxPlayer API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/sys");
var ADB = require("lib/adb");

function getProcessID(id) {
    var row = id.split(':');
    //var addr = row[0];
    var port = row[1];
    var cmd = "netstat -ano | findstr :" + port + " | findstr :0";
    var result = SHELL.exec(cmd);
    return result.substring(result.lastIndexOf(' '));
};

function getList() {
    var items = [];
    var devices = ADB.create()
        .setBinPath(SYS.getEnvString("PROGRAMFILES(X86)") + "/Nox/bin/nox_adb.exe")
        .getDevices()
    ;

    for(var i = 0; i < devices.length; i++) {
        var hostname = ADB.create().getHostname(devices[i].id);
	var PID = parseInt(getProcessID(devices[i].id));
        items.push({
            hostname: hostname,
            PID: PID
        });
    }

    return items;
}

exports.getList = getList;

exports.VERSIONINFO = "NoxPlayer (noxplayer.js) version 0.2";
exports.global = global;
exports.require = global.require;
