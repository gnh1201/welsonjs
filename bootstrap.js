// bootstrap.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var STD = require("lib/std");
var FILE = require("lib/file");
var PS = require("lib/powershell");
var REG = require("lib/registry");
var SYS = require("lib/system");
var SHELL = require("lib/shell");
var WMI = require("lib/wmi");

var appName = "welsonjs";
var isDisabledRegisterURIScheme = false;

// Windows versions before 8.1 (NT 6.3) are legacy.
function isLegacyWindows() {
    try {
        var version = String(WMI.execQuery("SELECT Version FROM Win32_OperatingSystem").fetch().get("Version")).split(".");
        var major = parseInt(version[0], 10);
        var minor = parseInt(version[1], 10);
        if (isNaN(major) || isNaN(minor)) {
            throw new Error("Unexpected version: " + version.join("."));
        }
        return major < 6 || (major == 6 && minor < 3);
    } catch (e) {
        console.warn("Could not detect the Windows version:", e.message);
        return true;
    }
}

function main(args) {
    //console.trace();
    
    // unlock file
    console.log("Starting unlock files...");
    PS.execCommand("dir | Unblock-File");

    // Register URI scheme
    if (!isDisabledRegisterURIScheme) {
        console.log("Registering URI scheme...");
        REG.write(REG.HKCR, appName, "", "URL:" + appName, REG.STRING);
        REG.write(REG.HKCR, appName, "URL Protocol", "", REG.STRING);
        REG.write(REG.HKCR, appName + "\\DefaultIcon", "", SYS.getCurrentScriptDirectory() + "\\app\\favicon.ico,0", REG.STRING);
        REG.write(REG.HKCR, appName + "\\shell\\open\\command", "", "cmd.exe /c cscript " + SYS.getCurrentScriptDirectory() + "\\app.js uriloader \"%1\"", REG.STRING);
    } else {
        console.log("Skipped register URI scheme");
    }

    // open web application
    console.log("Trying open GUI...");

    // detect old process
    var processList = SYS.getProcessList();
    for (var i = 0; i < processList.length; i++) {
        try {
            var process = processList[i];
            if ([
                "mshta.exe",
                "welsonjs-hta-mshtml.exe",
                "welsonjs-hta-webview2.exe",
                "welsonjs-hta-edgehtml.exe"
            ].indexOf(process.Caption) > -1) {
                //console.warn("Will be kill process ID:", process.ProcessID);
                //SYS.killProcess(process.ProcessID);
                //sleep(1000);
                STD.alert("Please close the running application (PID: " + process.processID + ")");
                return 0;
            }
        } catch (e) {
            console.warn(e.message);
        }
    }

    // Opening HTML application
    // Use mshta on legacy Windows and on source checkouts without the launcher.
    var legacyWindows = isLegacyWindows();
    if (legacyWindows) {
        console.log("Registering HTA file association...");
        REG.execFile("app\\assets\\reg\\Default_HTA.reg");
    }
    var yawwExecutablesPath = "bin\\x86\\welsonjs-hta.exe";
    var command = (!legacyWindows && FILE.fileExists(yawwExecutablesPath)) ?
        [yawwExecutablesPath, "app.hta"] : ["mshta.exe", "app.hta"];
    if (typeof args !== "undefined") {
        SHELL.run(command.concat(args));
    } else {
        SHELL.run(command);
    }

    // print welcome
    console.log("welcome");
};

//exports.main = __export__(main, "main");
exports.main = main;
