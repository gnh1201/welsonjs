//////////////////////////////////////////////////////////////////////////////////
//
//    bootstrap.js
//
/////////////////////////////////////////////////////////////////////////////////

var PS = require("lib/powershell");
var REG = require("lib/registry");
var SYS = require("lib/system");
var SHELL = require("lib/shell");

// set application name
var APPLICATION_NAME = "welsonjs";

return {
    main: function() {
        // unlock file
        console.log("Starting unlock files...");
        PS.execCommand("dir | Unblock-File");

        // Allow CROS to ADO
        console.log("Adjusting CROS policy to ADO...");
        REG.write(REG.HKCU, "SOFTWARE\\Policies\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Lockdown_Zones\\4", "1406", "00000000", REG.DWORD);
        REG.write(REG.HKLM, "SOFTWARE\\Policies\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Lockdown_Zones\\4", "1406", "00000000", REG.DWORD);

        // register URI scheme
        console.log("Registering URI scheme...");
        REG.write(REG.HKCR, APPLICATION_NAME, "", "URL:" + APPLICATION_NAME, REG.STRING);
        REG.write(REG.HKCR, APPLICATION_NAME, "URL Protocol", "", REG.STRING);
        REG.write(REG.HKCR, APPLICATION_NAME + "\\shell\\open\\command", "", "cscript " + SYS.getCurrentScriptDirectory() + "\\app.js uriloader \"%1\"", REG.STRING);

        // open HTA file
        console.log("Trying open GUI...");
        SHELL.run("app.hta");

        console.log("done");
    }
};
