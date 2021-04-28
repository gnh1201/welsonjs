//////////////////////////////////////////////////////////////////////////////////
//
//    bootstrap.js
//
/////////////////////////////////////////////////////////////////////////////////

var PS = require("lib/powershell");
var REG = require("lib/registry");
var SYS = require("lib/system");
var SHELL = require("lib/shell");
//var UPDATER = require("lib/updater");

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

    // check updates
    //console.log("Checking updates...");
    //UPDATER.checkUpdates();

    // open web application
    console.log("Trying open GUI...");

    // detect old process
    var processList = SYS.getProcessList();
    for (var i = 0; i < processList.length; i++) {
        try {
            var process = processList[i];
            if (process.Caption == "mshta.exe") {
                console.warn("Will be kill process ID: " + process.ProcessID);
                SYS.killProcess(process.ProcessID);
                sleep(1000);
            }
        } catch (e) {
            console.warn(e.message);
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
