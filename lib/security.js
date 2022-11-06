////////////////////////////////////////////////////////////////////////
// Security Policy API
////////////////////////////////////////////////////////////////////////

var SYS = require("lib/system");
var FILE = require("lib/file");
var REG = require("lib/registry");

var DISABLED = 0x00000001;
var ENABLED = 0x00000000;

function __BOOL_TO_DWORD__(x) {
    return x ? DISABLED : ENABLED;
}

// Check 'Run as administrator'
function isElevated = function() {
    try {
        CreateObject("WScript.Shell").RegRead("HKEY_USERS\\s-1-5-19\\");
        return true;
    } catch (e) {
        return false;
    }
}

// Turn on/off Windows Defender
function setDisableAntiSpyware(x) {
    var path = "SOFTWARE\\Policies\\Microsoft\\Windows Defender";
    var key = "DisableAntiSpyware";
    REG.write(REG.HKLM, path, key, __BOOL_TO_DWORD__(x), REG.DWORD);
};

// Trun on/off Registry Editor (regedit)
function setDisableRegistryTools(x) {
    var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    var key = "DisableRegistryTools";
    REG.write(REG.HKLM, path, key, __BOOL_TO_DWORD__(x), REG.DWORD);
}

// Turn on/off Task Manager (taskmgr)
function setDisableTaskMgr(x) {
    var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
    var key = "DisableTaskMgr";
    REG.write(REG.HKLM, path, key, __BOOL_TO_DWORD__(x), REG.DWORD);
}

// Get antivirus products list from the security center
function getAntiVirusProducts() {
    var displayNames = [];

    var objWMIService = GetObject("winmgmts:\\.\root\SecurityCenter2");
    var colItems = objWMIService.ExecQuery("SELECT * FROM AntiVirusProduct");
    var enumItems = new Enumerator(colItems);
    for (; !enumItems.atEnd(); enumItems.moveNext()) {
        var objItem = enumItems.item();
        displayNames.push(objItem.displayName);
    }

    return displayNames;
}

// Open the threat setting window on Windows Defender
function OpenThreatSettings() {
    var FN_MSASCui = SYS.getEnvString("%ProgramFiles%") + "\\Windows Defender\\MSASCui.exe";
    if (!FILE.fileExists(FN_MSASCui)) {
        SHELL.runAs(FN_MSASCui);   // old Windows Defender
    } else {
        SHELL.runAs("windowsdefender://Threatsettings");
    }
}

exports.DISABLED = DISABLED;
exports.ENABLED = ENABLED;
exports.setDisableAntiSpyware = setDisableAntiSpyware;
exports.setDisableRegistryTools = setDisableRegistryTools;
exports.setDisableTaskMgr = setDisableTaskMgr;
exports.getAntiVirusProducts = getAntiVirusProducts;
exports.OpenThreatSettings = OpenThreatSettings;

exports.VERSIONINFO = "Security Policy Module (security.js) version 0.2";
exports.AUTHOR = "catswords@protonmail.com";
exports.global = global;
exports.require = global.require;
