// coupang.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
// ***SECURITY NOTICE***
// Due to potential security issues, the Public API URL is not provided. If you need to request access, please refer to the project's contact information.
// You can download the server-side script that implements this functionality from the link below:
// https://github.com/gnh1201/caterpillar
// 
var JsonRpc2 = require("lib/jsonrpc2");

function search(s) {
    var rpc = JsonRpc2.create(JsonRpc2.DEFAULT_JSONRPC2_URL);
    var result = rpc.invoke("relay_invoke_method", {
        "callback": "load_script",
        "requires": [
            "https://scriptas.catswords.net/coupang.class.php"
        ],
        "args": [
            "$search = new CoupangProductSearch(); return $search->searchProducts('" + s + "')"
        ]
    }, "");

    return result.data;
}

exports.search = search;

exports.VERSIONINFO = "Coupang Product Search Client (coupang.js) version 0.1.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
