// bgloader.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// √Îæ‡¡°-1 fix (CWE-78/CWE-88): the previous build() only wrapped tokens that
// contained a space or a double quote and only escaped whitespace/quotes/
// backslashes via addslashes(). cmd.exe metacharacters ( ) ! ^ " < > & | were
// left intact, so an argument such as "127.0.0.1&calc.exe" broke out of its
// intended position and executed an extra command. build() now encodes each
// array element as a discrete argument that survives WScript.Shell.Run
// environment expansion and cmd.exe parsing.
// Patched By. Ephinence A. Kwon
if (!Array.prototype.map) {
    Array.prototype.map = function(fn) {
        var rv = [];
        for (var i = 0; i < this.length; i++)
            rv.push(fn(this[i]));
        return rv;
    };
}

var _percentPolicy = "reject"; // "reject" | "strip"

function setPercentPolicy(policy) {
    if (policy === "reject" || policy === "strip") {
        _percentPolicy = policy;
    }
    return _percentPolicy;
}

function _argvNeedsQuoting(s) {
    if (s.length === 0) return true;
    for (var i = 0; i < s.length; i++) {
        var c = s.charAt(i);
        if (c === " " || c === "\t" || c === "\n" || c === "\u000B" || c === "\f" || c === "\"") {
            return true;
        }
    }
    return false;
}

function _quoteArgvW(s) {
    if (!_argvNeedsQuoting(s)) {
        return s;
    }
    var out = "\"";
    var i = 0;
    var j;
    while (true) {
        var backslashes = 0;
        while (i < s.length && s.charAt(i) === "\\") {
            i++;
            backslashes++;
        }
        if (i === s.length) {
            for (j = 0; j < backslashes * 2; j++) out += "\\";
            break;
        } else if (s.charAt(i) === "\"") {
            for (j = 0; j < backslashes * 2 + 1; j++) out += "\\";
            out += "\"";
            i++;
        } else {
            for (j = 0; j < backslashes; j++) out += "\\";
            out += s.charAt(i);
            i++;
        }
    }
    out += "\"";
    return out;
}

var _CMD_META = "()!^\"<>&|";

function _caretEscapeForCmd(s) {
    var out = "";
    for (var i = 0; i < s.length; i++) {
        var c = s.charAt(i);
        out += (_CMD_META.indexOf(c) > -1) ? ("^" + c) : c;
    }
    return out;
}

function encodeArg(arg) {
    var s = (arg === null || typeof arg === "undefined") ? "" : arg.toString();
    if (s.indexOf("%") > -1) {
        if (_percentPolicy === "strip") {
            s = s.replace(/%/g, "");
        } else {
            throw new Error("bgloader.encodeArg: '%' cannot be safely encoded through WScript.Shell.Run/cmd.exe. Pre-resolve the value or call setPercentPolicy('strip').");
        }
    }
    return _caretEscapeForCmd(_quoteArgvW(s));
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
    } else if (typeof(cmd) === "object" && cmd != null) {
        var parts = [];
        for (var i = 0; i < cmd.length; i++) {
            parts.push(encodeArg(cmd[i]));
        }
        return parts.join(' ');
    } else {
        return "";
    }
}

main();
