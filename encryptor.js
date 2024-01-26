// encryptor.js
// HIGHT(ISO/IEC 18033-3) encryption and decryption tool for WelsonJS
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

var FILE = require("lib/file");
var Toolkit = require("lib/toolkit");

function main(args) {
    if (args.length < 1) {
        console.error("Usage: cscript app.js encryptor <filename>");
        return 0;
    }

    var filename = args[0];
    var userKey = '';
    while (userKey.length == 0 || userKey.length > 16) {
        userKey = Toolkit.prompt("Please enter the password for encryption:");
    }
    var data = FILE.readFile(filename, "utf-8");
    var encryptedData = Toolkit.encryptStringHIGHT(userKey, data);

    var dstfile = filename + ".enc";
    FILE.writeFile(dstfile, encryptedData, "utf-8");
    console.log("Saved to", dstfile);

    console.log("Done");
}

exports.main = main;
