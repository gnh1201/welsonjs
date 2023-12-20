var SYS = require("lib/system");
var HTTP = require("lib/http");
//var Toolkit = require("lib/toolkit");

function main(args) {
    console.log("Hello world");
    if (typeof WScript !== "undefined") {
        console.log("Process version:", SYS.getProcessVersion());
    }

    try {
        var web = HTTP.create();
        console.log(web.userAgent);
    } catch (e) {
        console.error("lib/http: Something wrong");
    }

    //Toolkit.create();
}

exports.main = main;