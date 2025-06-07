// filetypes.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var FILE = require("lib/file");

// https://support.microsoft.com/en-au/office/file-formats-that-are-supported-in-excel-0943ff2c-6014-4e8d-aaea-b83d51d46247
// https://support.microsoft.com/en-au/office/file-formats-that-are-supported-in-powerpoint-252c6fa0-a4bc-41be-ac82-b77c9773f9dc
// https://learn.microsoft.com/en-us/deployoffice/compat/office-file-format-reference#file-formats-that-are-supported-in-word
var data = JSON.parse(FILE.readFile("data/filetypes.json", FILE.CdoCharset.CdoUTF_8));

function getExtensionsByOpenWith(openwith) {
    return data.reduce(function(a, x) {
        if (x.openwith && x.openwith.indexOf(openwith) !== -1) {
            return a.concat(x.extension);
        }
        return a;
    }, []);
}

exports.getExtensionsByOpenWith = getExtensionsByOpenWith;

exports.VERSIONINFO = "FileTypes version 0.0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
