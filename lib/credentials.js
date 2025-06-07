// credentials.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var FILE = require("lib/file");

var CREDENTIALS_DATA = [];

function getTextFromFile(filename) {
    if (FILE.fileExists(filename)) {
        return FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8);
    }

    return "";
}

function readFromFile(type, filename) {
    var data = JSON.parse(getTextFromFile(filename));

    for (var provider in data) {
        var prelude = "file:";
        var value = (function(s) {
            if (s.indexOf(prelude) == 0) {
                var filename = s.substring(prelude.length);
                return getTextFromFile(filename);
            } else {
                return s;
            }
        })(data[provider]);
        
        push(type, provider, value);
    }
}

function push(type, provider, value) {
    CREDENTIALS_DATA.push({
        "type": type,
        "provider": provider,
        "value": value
    });
}

function get(type, provider, index) {
    var index = index || 0;
    var matches =  CREDENTIALS_DATA.reduce(function(a, x) {
        if (x.type == type && x.provider == provider) {
            a.push(x.value);
        }

        return a;
    }, []);

    if (matches.length - 1 < index) {
        return null;
    }
    
    return matches[index];
}

readFromFile("apikey", "data/apikey.json");

exports.readFromFile = readFromFile;
exports.push = push;
exports.get = get;

exports.VERSIONINFO = "Credential store (credentials.js) version 0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
