// shoutcut.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var SHELL = require("lib/shell");
var SYS = require("lib/system");

function main(args) {
    if (args.length < 1) {
        console.error("arguments could not be empty")
        return;
    }

    var FN = args[0];
    var target = require(FN);

    if ("onShoutcut" in target) {
        console.log("Trying execute onShoutcut:", FN);
        while (true) {
            try {
                target.onShoutcut(args.slice(1));
            } catch (e) {
                console.error("onShoutcut ->", e.message);
                target.onShoutcut(args.slice(1));
            }
        }
    } else {
        console.error("onShoutcut not defined");
    }
};

exports.main = main;
