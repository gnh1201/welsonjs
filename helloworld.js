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

    // test htmlfile
    var htmlfile = CreateObject("htmlfile");
    console.log(htmlfile.parentWindow.navigator.userAgent);

    // test SHA256 hash
    var hash = __hash_dotnetfx_managed__("Hello world", "sha256");
    console.log("Hash:", hash);
    
    sleep(100000);

    //Toolkit.create();
}

exports.main = main;
