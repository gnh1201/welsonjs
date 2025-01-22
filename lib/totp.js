// totp.js
// TOTP library for WelsonJS framework
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
// ***SECURITY NOTICE***
// Due to potential security issues, the Public API URL is not provided. If you need to request access, please refer to the project's contact information.
// You can download the server-side script that implements this functionality from the link below:
// https://github.com/gnh1201/caterpillar
// 
var JsonRpc2 = require("lib/jsonrpc2");

function getPubKey() {
    var rpc = JsonRpc2.create(JsonRpc2.DEFAULT_JSONRPC2_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://scriptas.catswords.net/class.tfa.php"
        ],
        "args": [
            "$tfa = new tfa(); return $tfa->getPubKey()"
        ]
    }, "");
    
    return result.data;
}

function getOtp(pubkey) {
    var rpc = JsonRpc2.create(JsonRpc2.DEFAULT_JSONRPC2_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://scriptas.catswords.net/class.tfa.php"
        ],
        "args": [
            "$tfa = new tfa(); return $tfa->getOtp('" + pubkey + "')"
        ]
    }, "");

    return result.data;
}

exports.getPubKey = getPubKey;
exports.getOtp = getOtp;

exports.VERSIONINFO = "TOTP Client (totp.js) version 0.1.8";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;

/*
// Example:
var TOTP = require("lib/totp");
console.log(TOTP.getPubKey()); // get public key. e.g. 6Y4R 3AQN 4TTV CEQT
console.log(TOTP.getOtp('6Y4R 3AQN 4TTV CEQT')); // get OTP code. e.g. 317884
*/
