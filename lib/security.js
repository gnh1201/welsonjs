////////////////////////////////////////////////////////////////////////
// Security API
////////////////////////////////////////////////////////////////////////
var scope = {
    VERSIONINFO: "Security Module (security.js) version 0.1",
    global: global,
	require: global.require
};
var registry = require("registry");

scope.DISABLED = 0x00000001;
scope.ENABLED = 0x00000000;

// check 'run as administrator'
scope.isElevated = function() {
    try {
        WS.RegRead("HKEY_USERS\\s-1-5-19\\");
        return true;
    } catch (e) {
        return false;
    }
}

// turn on/off Windows Defender
scope.setAntiSpyware = function(buffer) {
    var path = "SOFTWARE\\Policies\\Microsoft\\Windows Defender";
    var key = "DisableAntiSpyware";
    registry.write(registry.HKLM, path, key, buffer, registry.DWORD);
}

// trun on/off Registry Editor (regedit)
scope.setRegedit = function(buffer) {
    var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    var key = "DisableRegistryTools";
    registry.write(registry.HKLM, path, key, buffer, registry.DWORD);
}

// turn on/off Task Manager (taskmgr)
scope.setTaskmgr = function(buffer) {
    var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    var key = "DisableTaskMgr";
    registry.write(registry.HKLM, path, key, buffer, registry.DWORD);
}

return scope;
