// lib/system.js
// https://github.com/gnh1201/welsonjs
var SHELL = require("lib/shell");
var WMI =  require("lib/wmi");

function createProcess(cmd) {
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
}

function getEnvString(envName) {
    var WSH = CreateObject("WScript.Shell");
    return (function(s) {
        switch(s) {
            case "PROGRAMFILES":
                return WSH.ExpandEnvironmentStrings("%HOMEDRIVE%\\Program Files");
            case "PROGRAMFILES(X86)":
                return WSH.ExpandEnvironmentStrings("%HOMEDRIVE%\\Program Files (x86)");
            default:
                return WSH.ExpandEnvironmentStrings('%' + s + '%');
        }
    })(envName.toUpperCase());
}

function get32BitFolder() {
    var base = getEnvString("WINDIR");
    var syswow64 = base + "\\SysWOW64\\";

    if (CreateObject("Scripting.FileSystemObject").FolderExists(syswow64))
        return syswow64;

    return base + "\\System32\\";
}

function isElevated() {
    var WSH = CreateObject("WScript.Shell");
    try {
        WSH.RegRead("HKEY_USERS\\s-1-5-19\\");
        return true;
    } catch (e) {}

    return false;
}

function getOS() {
    return WMI.execQuery("SELECT Caption FROM Win32_OperatingSystem").fetch().get("Caption").trim();
}

function getDCName() {
    var WSH = CreateObject("WScript.Shell");
    try {
        var DC = WSH.RegRead("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History\\DCName");
        if (DC.length > 0)
            return DC;
    } catch (e) {}
}

function getArch() {
    return WMI.execQuery("SELECT OSArchitecture FROM Win32_OperatingSystem").fetch().get("OSArchitecture");
}

function getUUID() {
    return WMI.execQuery("SELECT UUID FROM Win32_ComputerSystemProduct").fetch().get("UUID").toLowerCase();
}

function getCurrentWorkingDirectory() {
    try {
        return SHELL.exec("cd").trim();
    } catch (e) {}
}

function getDirName(path) {
    var pos = Math.max.apply(null, [path.lastIndexOf("\\"), path.lastIndexOf("/")]);
    return (pos > -1 ? path.substring(0, pos) : "");
}

function getFileName(path) {
    var pos = Math.max.apply(null, [path.lastIndexOf("\\"), path.lastIndexOf("/")]);
    return (pos > -1 ? path.substring(pos + 1) : "");
}

function getCurrentScriptDirectory() {
    if (typeof(WScript) !== "undefined") {
        return getDirName(WScript.ScriptFullName);
    } else if (typeof(document) !== "undefined") {
        return getDirName(document.location.pathname);
    } else {
        return ".";
    }
}

function getCurrentScriptName() {
    if (typeof(WScript) !== "undefined") {
        return WScript.ScriptName;
    } else if (typeof(document) !== "undefined") {
        return getFileName(document.location.pathname);
    } else {
        return "";
    }
}

function getNetworkInterfaces() {
    return WMI.execQuery("SELECT * FROM Win32_NetworkAdapterConfiguration").fetchAll();
}

function getProcessList() {
    return WMI.execQuery("SELECT * FROM Win32_Process").fetchAll();
}

function getPIDList() {
    return processes.map(function(x) {
        return x.ProcessID;
    });
}

function isAlivePID(pid) {
    if (!pid) {
        return false;
    } else {
        return (getPIDList().indexOf(pid) > -1);
    }
}

function getProcessListByName(name) {
    return getProcessList().filter(function(x) {
        return (x.Caption === name);
    });
}

function killProcess(pid) {
    var processes = getProcessList();

    for (var i = 0; i < processes.length; i++) {
        try {
            if (processes[i].ProcessId == pid) {
                processes[i].Terminate();
                return true;
            }
        } catch (e) {
            console.error("Failed to kill process: ", e.message);
        }
    }

    return false;
}

function createShortcut(shoutcutName, fileName) {
    var WSH = CreateObject("WScript.Shell");
    var workingDirectory = getCurrentWorkingDirectory();
    var desktopPath = WSH.SpecialFolders("Desktop");
    var link = WSH.CreateShortcut(desktopPath + "\\" + shoutcutName + ".lnk");
    link.IconLocation = fileName + ",1";
    link.TargetPath = workingDirectory + "\\" + fileName;
    link.WindowStyle = 1;
    link.WorkingDirectory = workingDirectory;
    link.Save();
}

function ping(address) {
    return WMI.execQuery("SELECT ResponseTime FROM Win32_PingStatus WHERE address='" + address + "'").fetch().get("ResponseTime");
}

function getProcessVersion() {
    var getIEVersion = function() {
        var rv = -1;
        if (navigator.appName == 'Microsoft Internet Explorer') {
            var ua = navigator.userAgent;
            var re  = new RegExp("MSIE ([0-9]{1,}[\.0-9]{0,})");
            if (re.exec(ua) != null)
            rv = parseFloat( RegExp.$1 );
        }
        return rv;
    }

    if (typeof WScript !== "undefined") {
        return "Microsoft JScript" + (' ' + WScript.Version);
    } else if (typeof navigator !== "undefined") {
        return (function(rv) {
            return "MSIE" + (rv < 0 ? '' : (' ' + rv));
        })(getIEVersion());
    }
}

exports.createProcess = createProcess;
exports.getEnvString = getEnvString;
exports.get32BitFolder = get32BitFolder;
exports.isElevated = isElevated;
exports.getOS = getOS;
exports.getDCName = getDCName;
exports.getArch = getArch;
exports.getUUID = getUUID;
exports.getCurrentWorkingDirectory = getCurrentWorkingDirectory;
exports.getDirName = getDirName;
exports.getFileName = getFileName;
exports.getCurrentScriptDirectory = getCurrentScriptDirectory;
exports.getCurrentScriptName = getCurrentScriptName;
exports.getNetworkInterfaces = getNetworkInterfaces;
exports.getProcessList = getProcessList;
exports.getPIDList = getPIDList;
exports.isAlivePID = isAlivePID;
exports.getProcessListByName = getProcessListByName;
exports.killProcess = killProcess;
exports.createShortcut = createShortcut;
exports.ping = ping;
exports.getProcessVersion = getProcessVersion;

exports.VERSIONINFO = "System Module (system.js) version 0.1.5";
exports.global = global;
exports.require = global.require;
