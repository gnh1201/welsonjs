// WAMR for WelsonJS
//   - WAMR: https://github.com/bytecodealliance/wasm-micro-runtime
//   - WelsonJS: https://github.com/gnh1201/welsonjs

var SHELL = require("lib/shell");

var WAMRObject = function() {
	this.binPath = "bin\\iwasm";

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
