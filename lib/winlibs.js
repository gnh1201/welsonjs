
// winlibs.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// Windows Library API

var SHELL = require("lib/shell");
var FILE = require("lib/file");

var DEFAULT_CLSID = "4B72FC46-C543-4101-80DB-7777848DACDC";

/**
 * Create a side-by-side assembly manifest for a DLL.
 *
 * @param {string} libraryName
 * @param {string} clsid
 * @return {boolean}
 */
function createManifest(libraryName, clsid) {
    var manifestPath = libraryName + ".manifest";
    var classId = typeof clsid === "undefined" ?
        DEFAULT_CLSID : clsid;

    var lines = [
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>",
        "<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\">",
        "<file name=\"" + libraryName + ".dll\">",
        "<comClass clsid=\"{" + classId + "}\" threadingModel=\"Apartment\" progid=\"" + libraryName + "\" />",
        "</file>",
        "</assembly>"
    ];

    FILE.writeFile(
        manifestPath,
        lines.join("\r\n"),
        FILE.CdoCharset.CdoUTF_8
    );

    return FILE.fileExists(manifestPath);
}

/**
 * Load a Windows library.
 *
 * @param {string} libraryName
 * @return {object}
 */
function loadLibrary(libraryName) {
    var manifestPath = libraryName + ".manifest";

    if (FILE.fileExists(manifestPath)) {
        try {
            var activationContext = CreateObject("Microsoft.Windows.ActCtx");
            activationContext.Manifest = manifestPath;

            var libraryObject = activationContext.CreateObject("MessageBox");

            return {
                invoke: function(methodName, args) {
                    return libraryObject[methodName].apply(
                        libraryObject,
                        args || []
                    );
                },

                call: function(methodName, args) {
                    console.warn(
                        "loadLibrary(...).call(...) is deprecated. " +
                        "Use loadLibrary(...).invoke(...) instead."
                    );

                    return this.invoke(methodName, args);
                }
            };
        } catch (e) {
            // Fall back to rundll32 invocation.
        }
    }

    return {
        invoke: function(methodName, args) {
            var command = ["rundll32.exe"];

            if (typeof methodName === "undefined") {
                methodName = "DllMain";
            }

            command.push(libraryName + ".dll," + methodName);

            if (typeof args !== "undefined") {
                command = command.concat(args);
            }

            return SHELL.exec(command);
        },

        call: function(methodName, args) {
            console.warn(
                "loadLibrary(...).call(...) is deprecated. " +
                "Use loadLibrary(...).invoke(...) instead."
            );

            return this.invoke(methodName, args);
        }
    };
}

var SHELL32 = loadLibrary("SHELL32");

/**
 * Open a Control Panel applet.
 *
 * @param {string} name
 * @param {Array} applets
 * @param {Array} args
 * @return {*}
 */
function showControlPanel(name, applets, args) {
    var commandArgs = [];
    var extraArgs = args || [];

    commandArgs.push(name + ".cpl");

    if (applets) {
        for (var i = 0; i < applets.length; i++) {
            commandArgs.push(applets[i]);
        }
    }

    commandArgs = commandArgs.concat(extraArgs);

    return SHELL32.invoke("Control_runDLL", [
        commandArgs.join(",")
    ]);
}

/**
 * Open Network Connections.
 *
 * @return {*}
 */
function showNetworkAdapters() {
    return showControlPanel("ncpa", ["@0", 3]);
}

/**
 * Show the Shell About dialog.
 *
 * @return {*}
 */
function showEULA() {
    return SHELL32.invoke("ShellAboutW");
}

/**
 * Open a URL in the default browser.
 *
 * @param {string} url
 * @return {*}
 */
function openBrowser(url) {
    return loadLibrary("url").invoke("FileProtocolHandler", [
        url
    ]);
}

exports.createManifest = createManifest;
exports.loadLibrary = loadLibrary;
exports.SHELL32 = SHELL32;
exports.showControlPanel = showControlPanel;
exports.showNetworkAdapters = showNetworkAdapters;
exports.showEULA = showEULA;
exports.openBrowser = openBrowser;

exports.VERSIONINFO = "Windows Lib (winlibs.js) version 0.2";
exports.global = global;
exports.require = global.require;
