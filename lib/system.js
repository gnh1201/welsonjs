////////////////////////////////////////////////////////////////////////
// System API
////////////////////////////////////////////////////////////////////////
var SHELL = require("lib/shell");
var WSH = CreateObject("WScript.Shell");
var WMI =  require("lib/wmi");

exports.VERSIONINFO = "System Module (system.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.createProcess = function(cmd) {
    var SW_HIDE = 0;
    //wmi.Get("Win32_Process").Create(cmd, null, si, pid);
    return WMI.setClass("Win32_Process").setMethod("Create").setParameters({
        "CommandLine": cmd,
        "CurrentDirectory": null,
        "ProcessStartupInformation": WMI.setClass("Win32_ProcessStartup").create()
            .setAttribute("ShowWindow", SW_HIDE)
            .setAttribute("CreateFlags", 16777216)
            .setAttribute("X", 1)
            .setAttribute("Y", 1)
            .setAttribute("xSize", 1)
            .setAttribute("ySize", 1)
            .getInstance()
    }).execute().get("ProcessID");
};

exports.getEnvString = function(envName) {
    return WSH.ExpandEnvironmentStrings('%' + envName + '%');
};

exports.get32BitFolder = function() {
    var base = exports.getEnvString("WINDIR");
    var syswow64 = base + "\\SysWOW64\\";

    if (JPTUDBSTOW.FS.FolderExists(syswow64))
        return syswow64;

    return base + "\\System32\\";
}

exports.isElevated = function() {
    try {
        WSH.RegRead("HKEY_USERS\\s-1-5-19\\");
        return true;
    } catch (e) {
        return false;
    }
};

exports.getOS = function() {
    return WMI.execQuery("SELECT * FROM Win32_OperatingSystem").fetch().get("Caption").rtrim();
};

exports.getDCName = function() {
    try {
        var DC = WSH.RegRead("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History\\DCName");
        if (DC.length > 0)
            return DC;
    } catch (e) {}
};

exports.getArch = function() {
    return WMI.execQuery("SELECT * FROM Win32_OperatingSystem").fetch().get("OSArchitecture");
};

exports.getUUID = function() {
    return WMI.execQuery("SELECT * FROM Win32_ComputerSystemProduct").fetch().get("UUID").toLowerCase();
};

exports.getCurrentWorkingDirectory = function() {
    try {
        cwd = SHELL.exec("cd", "cwd.txt").rtrim();
        return cwd;
    } catch (e) {}
};

exports.getDirName = function(path) {
    var delimiter = "\\";
    var pos = path.lastIndexOf(delimiter);
    return (pos > -1 ? path.substring(0, pos) : "");
};

exports.getFileName = function(path) {
    var delimiter = "\\";
    var pos = path.lastIndexOf(delimiter);
    return (pos > -1 ? path.substring(pos + delimiter.length) : "");
};

exports.getCurrentScriptDirectory = function() {
    if (typeof(WScript) !== "undefined") {
        return exports.getDirName(WScript.ScriptFullName);
    } else if (typeof(document) !== "undefined") {
        return exports.getDirName(document.location.pathname);
    } else {
        return ".";
    }
};

exports.getCurrentScriptName = function() {
    if (typeof(WScript) !== "undefined") {
        return WScript.ScriptName;
    } else if (typeof(document) !== "undefined") {
        return exports.getFileName(document.location.pathname);
    } else {
        return "";
    }
};

exports.getNetworkInterfaces = function() {
    return WMI.execQuery("SELECT * FROM Win32_NetworkAdapterConfiguration").fetchAll();
};

exports.getProcessList = function() {
    return WMI.execQuery("Select * From Win32_Process").fetchAll();
};

exports.getPIDList = function() {
    var result = [];
    var processes = exports.getProcessList();
    for (var i = 0; i < processes.length; i++) {
        result.push(processes[i].ProcessID);
    }
    return result;
};

exports.getProcessListByName = function(name) {
	return exports.getProcessList().filter(function(s) {
		return (s.Caption === name);
	});
};

exports.killProcess = function(pid) {
    var processes = exports.getProcessList();

    for (var i = 0; i < processes.length; i++) {
        try {
            if (processes[i].ProcessId == pid) {
                processes[i].Terminate();
                return true;
            }
        } catch (e) {
            console.error("Lib -> System -> killProcess() -> ", e.message);
        }
    }

    return false;
};

exports.createShortcut = function(shoutcutName, fileName) {
    var workingDirectory = exports.getCurrentWorkingDirectory();
    var desktopPath = WSH.SpecialFolders("Desktop");
    var link = WSH.CreateShortcut(desktopPath + "\\" + shoutcutName + ".lnk");
    link.IconLocation = fileName + ",1";
    link.TargetPath = workingDirectory + "\\" + fileName;
    link.WindowStyle = 1;
    link.WorkingDirectory = workingDirectory;
    link.Save();
};

exports.ping = function(address) {
    return WMI.execQuery("Select * From Win32_PingStatus where address='" + address + "'").fetch().get("ResponseTime");
};
