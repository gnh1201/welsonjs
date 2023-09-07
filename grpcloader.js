// grpcloader.js
var Py3 = require("lib/python3");

function main(args) {
    var py = Py3.create("win32");
    py.runScript("app/assets/py/apploader.py");
}

exports.main = main;
