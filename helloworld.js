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

    console.error("Muted:", console._muted);

    var htmlfile = CreateObject("htmlfile");
    console.log(htmlfile.parentWindow.navigator.userAgent);
    
    sleep(100000);

    //Toolkit.create();
}

exports.main = main;
