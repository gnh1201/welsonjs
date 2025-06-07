// bgloader.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
if (!Array.prototype.map) {
    Array.prototype.map = function(fn) {
        var rv = [];
        for (var i = 0; i < this.length; i++)
            rv.push(fn(this[i]));
        return rv;
    };
}

function main() {
    var args = [];
    var argl = WScript.arguments.length;
    for (var i = 0; i < argl; i++) {
        args.push(WScript.arguments(i));
    }

    var objShell = WScript.CreateObject("WScript.Shell");
    objShell.Run(build(args), 0, true);
}

function build(cmd) {
    if (typeof(cmd) === "string") {
        return cmd;
    } else if (typeof(cmd) === "object") {
        return cmd.map(function(s) {
            var regex = /[ "]/g;
            if (!regex.test(s)) {
                return s;
            } else {
                return "\"" + addslashes(s) + "\"";
            }
        }).join(' ');
    } else {
        return "";
    }
}

function addslashes(s) {
    return s.toString().replace(/\\/g, '\\\\').
    replace(/\u0008/g, '\\b').
    replace(/\t/g, '\\t').
    replace(/\n/g, '\\n').
    replace(/\f/g, '\\f').
    replace(/\r/g, '\\r').
    replace(/'/g, '\\\'').
    replace(/"/g, '\\"');
};

main();