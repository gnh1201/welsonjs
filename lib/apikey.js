// apikey.js
// https://github.com/gnh1201/welsonjs
var FILE = require("lib/file");

function loadTextFile(filename) {
    if (FILE.fileExists(filename)) {
        return FILE.readFile("data/apikey.json", FILE.CdoCharset.CdoUTF_8);
    }
    return "";
}

function loadKeyData() {
    var s = loadTextFile("data/apikey.json");
    return JSON.parse(s);
}

function getApiKey(serviceName) {
    var apikey = "";
    if (serviceName in API_KEY_DATA) {
        apikey = API_KEY_DATA[serviceName];
    }

    var prelude = "file:";
    if (apikey.indexOf(prelude) == 0) {
        var filename = apikey.substring(prelude.length);
        apikey = loadTextFile(filename);
    }

    return apikey;
}

var API_KEY_DATA = loadKeyData();

exports.getApiKey = getApiKey;

exports.VERSIONINFO = "API key library (apikey.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
