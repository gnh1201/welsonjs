//////////////////////////////////////////////////////////////////////////////////
//
//    bootstrap.js
//
/////////////////////////////////////////////////////////////////////////////////

var PS = require("lib/powershell");
var REG = require("lib/registry");
var SYS = require("lib/system");
var SHELL = require("lib/shell");

var appName = "welsonjs";

exports.main = function(args) {
    // unlock file
    console.log("Starting unlock files...");
    PS.execCommand("dir | Unblock-File");

    // Allow CROS to ADO
    console.log("Adjusting CROS policy to ADO...");
    REG.write(REG.HKCU, "SOFTWARE\\Policies\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Lockdown_Zones\\4", "1406", "00000000", REG.DWORD);
    REG.write(REG.HKLM, "SOFTWARE\\Policies\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Lockdown_Zones\\4", "1406", "00000000", REG.DWORD);

    // register URI scheme
    console.log("Registering URI scheme...");
    REG.write(REG.HKCR, appName, "", "URL:" + appName, REG.STRING);
    REG.write(REG.HKCR, appName, "URL Protocol", "", REG.STRING);
    REG.write(REG.HKCR, appName + "\\DefaultIcon", "", SYS.getCurrentScriptDirectory() + "\\app\\favicon.ico,0", REG.STRING);
    REG.write(REG.HKCR, appName + "\\shell\\open\\command", "", "cmd.exe /c cscript " + SYS.getCurrentScriptDirectory() + "\\app.js uriloader \"%1\"", REG.STRING);

    // open web application
    console.log("Trying open GUI...");

    // detect old process
    var processList = SYS.getProcessList();
    for (var i = 0; i < processList.length; i++) {
        var process = processList[i];
        if (process.Caption == "mshta.exe") {
            SYS.killProcess(process.ProcessID);
        }
    }

    // open web application
    if (typeof(args) !== "undefined") {
        SHELL.run(["app.hta"].concat(args));
    } else {
        SHELL.run("app.hta");
    }

    // echo welcome
    console.log("welcome");
};
