var SYS = require("lib/system");
var HTTP = require("lib/http");

function main(args) {
    console.log("Hello world");
    if (typeof WScript !== "undefined") {
        console.log("Process version:", SYS.getProcessVersion());
    }

    try {
        var web = HTTP.create();
        console.log(web.userAgent);
    } catch (e) {
        console.error("Something wrong");
    }
}

exports.main = main;