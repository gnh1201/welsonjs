// autohotkey.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// AutoHotKey API
// 
var SHELL = require("lib/shell");

exports.VERSIONINFO = "AutoHotKey (autohotkey.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.execScript = function(scriptName, args) {
    var cmd = [
        "%PROGRAMFILES%\\AutoHotkey\\AutoHotkey.exe",
        scriptName + ".ahk"
    ];

    if (typeof(args) !== "undefined") {
        cmd = cmd.concat(args);
    }

    return SHELL.exec(cmd);
};
