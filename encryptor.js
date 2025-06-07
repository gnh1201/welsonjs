// encryptor.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// HIGHT(ISO/IEC 18033-3) encryption and decryption tool for WelsonJS framework
// 
var FILE = require("lib/file");
var Toolkit = require("lib/toolkit");

function main(args) {
    if (args.length < 1) {
        console.error("Usage: cscript app.js encryptor <filename>");
        return 0;
    }

    var filename = args[0];
    var dstfile = filename + ".enc";
    if (FILE.fileExists(dstfile)) {
        console.error(dstfile, "already exists. Please delete it.");
        return 0;
    }

    var userKey = '';
    while (userKey.length == 0 || userKey.length > 16) {
        userKey = Toolkit.prompt("Please enter the password for encryption:");
    }

    var data = FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8);
    var encryptedData = Toolkit.encryptString(userKey, data);

    var dstfile = filename + ".enc";
    FILE.writeFile(dstfile, encryptedData, FILE.CdoCharset.CdoUTF_8);
    console.log("Saved to", dstfile);

    console.log("Done");
}

exports.main = main;
