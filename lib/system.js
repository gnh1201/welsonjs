////////////////////////////////////////////////////////////////////////
// System API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var WSH = CreateObject("WScript.Shell");
var WMI = GetObject("winmgmts:\\\\.\\root\\CIMV2");
var FSO = CreateObject("Scripting.FileSystemObject");

exports.VERSIONINFO = "System Module (system.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.isElevated = function() {
    try {
        WSH.RegRead("HKEY_USERS\\s-1-5-19\\");
        return true;
    } catch (e) {
        return false;
    }
};

exports.getOS =  function() {
    try {
        var colItems = WMI.ExecQuery("SELECT * FROM Win32_OperatingSystem");
        var enumItems = new Enumerator(colItems);
        var objItem = enumItems.item();
        return objItem.Caption.rtrim();
    } catch (e) {}
};

exports.getDCName = function() {
    try {
        var DC = WSH.RegRead("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History\\DCName");
        if (DC.length > 0)
            return DC;
    } catch (e) {}
};

exports.getArch = function() {
    try {
        var colItems = WMI.ExecQuery("SELECT * FROM Win32_OperatingSystem");
        var enumItems = new Enumerator(colItems);
        var objItem = enumItems.item();
        return objItem.OSArchitecture;
    } catch (e) {}
};

exports.getUUID = function() {
    try {
        var colItems = WMI.ExecQuery("SELECT * FROM Win32_ComputerSystemProduct");
        var enumItems = new Enumerator(colItems);
        var objItem = enumItems.item();
        return objItem.UUID.toLowerCase();
    } catch (e) {}
};

exports.getCurrentWorkingDirectory = function() {
    try {
        cwd = SHELL.exec("cd", "cwd.txt").rtrim();
        return cwd;
    } catch (e) {}
};

// "console only";
exports.getCurrentScriptDirectory = function() {
    var path = WScript.ScriptFullName;
    var pos = path.lastIndexOf("\\");
    return path.substring(0, pos);
};

exports.getNetworkInterfaces = function() {
    var wbemFlagReturnImmediately = 0x10;
    var wbemFlagForwardOnly = 0x20;
    var rows = [];

    try {
        var colItems = WMI.ExecQuery(
            "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True",
            "WQL",
            wbemFlagReturnImmediately | wbemFlagForwardOnly
        );

        var enumItems = new Enumerator(colItems);
        for (; !enumItems.atEnd(); enumItems.moveNext()) {
            var objItem = enumItems.item();
            try {
                rows.push({
                    Caption: objItem.Caption,
                    IPAddresses: objItem.IPAddress.toArray(),
                    MACAddress: objItem.MACAddress
                });
            } catch(e) {}
        }
    } catch(e) {}

    return rows;
};

exports.getCurrentProcesses = function() {
    var processes = [];
    var response = SHELL.exec("tasklist.exe");

    var lines = response.split(/\r?\n/);
    for(var i in lines) {
        var line = lines[i];
        var values = line.split(' ');
        processes.push(values);
    }

    return processes;
};

exports.createShortcut = function(shoutcutName, fileName) {
    var workingDirectory = exports.getCurrentWorkingDirectory();
    var desktopPath = WSH.SpecialFolders("Desktop");
    var link = WSH.CreateShortcut(desktopPath + "\\" + shoutcutName + ".lnk");
    link.IconLocation = fileName + ",1";
    link.TargetPath = workingDirectory + "\\" + fileName;
    link.WindowStyle = 3;
    link.WorkingDirectory = workingDirectory;
    link.Save();
};
