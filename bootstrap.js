// bootstrap.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var STD = require("lib/std");
var PS = require("lib/powershell");
var REG = require("lib/registry");
var SYS = require("lib/system");
var SHELL = require("lib/shell");
var WMI = require("lib/wmi");

var appName = "welsonjs";

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

// Serializes the running-app check and launch across bootstrap processes.
// Returns null while another bootstrap holds the lock. The owner keeps the file
// open, so only a file left behind by a crashed owner can be deleted.
function acquireStartupLock() {
    var fso = CreateObject("Scripting.FileSystemObject");
    var path = fso.BuildPath(fso.GetSpecialFolder(2).Path, appName + "-bootstrap.lock");
    var stream;
    try {
        if (fso.FileExists(path)) {
            fso.DeleteFile(path, true);
        }
        stream = fso.CreateTextFile(path, false);
    } catch (e) {
        var code = e.number & 0xFFFF;
        if (code == 58 || code == 70) { // file already exists, permission denied
            return null;
        }
        console.warn("Could not create the startup lock:", e.message);
        return {
            release: function() {}
        };
    }
    return {
        release: function() {
            stream.Close();
            fso.DeleteFile(path, true);
        }
    };
}

// SHELL.run returns before the forked process appears in the process list.
function waitForProcess(name) {
    for (var i = 0; i < 20 && SYS.getProcessListByName(name).length == 0; i++) {
        sleep(250);
    }
}

function openApplication(args) {
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
                return false;
            }
        } catch (e) {
            console.warn(e.message);
        }
    }

    // Opening HTML application
    // app.hta needs the mshta host: the bundled welsonjs-hta viewers render it as
    // a plain page without WELSONJS_WINDOW or HTA privileges.
    if (isLegacyWindows()) {
        console.log("Registering HTA file association...");
        REG.execFile("app\\assets\\reg\\Default_HTA.reg");
    }
    var command = ["mshta.exe", "app.hta"];
    if (typeof args !== "undefined") {
        SHELL.run(command.concat(args));
    } else {
        SHELL.run(command);
    }
    waitForProcess("mshta.exe");
    return true;
}

function main(args) {
    //console.trace();
    
    // unlock file
    console.log("Starting unlock files...");
    PS.execCommand("dir | Unblock-File");

    // open web application
    console.log("Trying open GUI...");

    var startupLock = acquireStartupLock();
    if (startupLock === null) {
        console.warn("Another bootstrap is already starting the application.");
        return 0;
    }
    try {
        if (!openApplication(args)) {
            return 0;
        }
    } finally {
        startupLock.release();
    }

    // print welcome
    console.log("welcome");
};

//exports.main = __export__(main, "main");
exports.main = main;
