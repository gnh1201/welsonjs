////////////////////////////////////////////////////////////////////////
// Security API
////////////////////////////////////////////////////////////////////////
var scope = {
    VERSIONINFO: "Security Module (security.js) version 0.1",
    global: global,
	require: global.require
};
var REG = require("lib/registry");
var WSH = CreateObject("WScript.Shell");

scope.DISABLED = 0x00000001;
scope.ENABLED = 0x00000000;

// check 'run as administrator'
scope.isElevated = function() {
    try {
        WSH.RegRead("HKEY_USERS\\s-1-5-19\\");
        return true;
    } catch (e) {
        return false;
    }
};

// turn on/off Windows Defender
scope.setAntiSpyware = function(buffer) {
    var path = "SOFTWARE\\Policies\\Microsoft\\Windows Defender";
    var key = "DisableAntiSpyware";
    REG.write(registry.HKLM, path, key, buffer, registry.DWORD);
};

// trun on/off Registry Editor (regedit)
scope.setRegedit = function(buffer) {
    var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    var key = "DisableRegistryTools";
    REG.write(registry.HKLM, path, key, buffer, registry.DWORD);
};

// turn on/off Task Manager (taskmgr)
scope.setTaskmgr = function(buffer) {
    var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    var key = "DisableTaskMgr";
    REG.write(registry.HKLM, path, key, buffer, registry.DWORD);
};

// detect antivirus from security center
scope.detectAntivirus = function() {
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

return scope;
