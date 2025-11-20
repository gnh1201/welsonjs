// wamr.js
// Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// WAMR(WebAssembly Micro Runtime) integration
// https://github.com/bytecodealliance/wasm-micro-runtime
// 
var SYS = require("lib/system");
var SHELL = require("lib/shell");

var WAMRObject = function() {
	this.binPath = SYS.getAppDataDir() + "\\wamr\\iwasm.exe";

    this.verbose = 0;
    this.stackSize = 0;
    this.heapSize = 0;
    this.repl = false;
    this.env = {};
    this.dirs = [];

    this.exec = function(f1, f2) {
        var cmd = [];

        cmd.push(binPath);

        if (this.verbose > 0) {
            cmd.push("-v=" + this.verbose);
        }
        
        if (this.stackSize > 0) {
            cmd.push("--stack-size=" + this.stackSize);
        }
        
        if (this.heapSize > 0) {
            cmd.push("--heap-size=" + this.heapsize);
        }
        
        if (Object.keys(env).length > 0) {
            for (var k in env) {
                cmd.push("--env=\"" + k + "=" + env[k] + "\"");
            }
        }
        
        if (dirs.length > 0) {
            for (var i = 0; i < dirs.length; i++) {
                cmd.push("--dir=" + dirs[i]);
            }
        }
        
        if (typeof f2 !== "undefined") {
            cmd.push("--function-name=" + f2);
        }
        
        cmd.push(f1);

        return SHELL.exec(cmd);
    };
};

exports.create = function() {
    return new WAMRObject();
};

exports.VERSIONINFO = "WAMR(WebAssembly Micro Runtime) integration (wamr.js) version 0.1.1";
exports.global = global;
exports.require = global.require;
