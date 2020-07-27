//////////////////////////////////////////////////////////////////////////////////
//
//    bootstrap.js
//
/////////////////////////////////////////////////////////////////////////////////

var PS = require("lib/powershell");
var REG = require("lib/registry");
var SYS = require("lib/system");
var SHELL = require("lib/shell");

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
    REG.write(REG.HKCR, __config.appName, "", "URL:" + __config.appName, REG.STRING);
    REG.write(REG.HKCR, __config.appName, "URL Protocol", "", REG.STRING);
    REG.write(REG.HKCR, __config.appName + "\\DefaultIcon", "", SYS.getCurrentScriptDirectory() + "\\app\\favicon.ico,0", REG.STRING);
    REG.write(REG.HKCR, __config.appName + "\\shell\\open\\command", "", "cscript " + SYS.getCurrentScriptDirectory() + "\\app.js uriloader \"%1\"", REG.STRING);

    // open HTA file
    console.log("Trying open GUI...");
    if (typeof(args) !== "undefined") {
        SHELL.run(["app.hta"].concat(args));
    } else {
        SHELL.run("app.hta");
    }

    // echo welcome
    console.log("welcome");
};