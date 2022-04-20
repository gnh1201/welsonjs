var Chrome = require("lib/chrome");
var Toolkit = require("lib/toolkit");

function main() {
    var wbInstance = Chrome.create().setVendor("msedge").open("https://google.com");
    sleep(5000);
    //console.log(wbInstance.getHTML("body"));

    wbInstance.focus();
    wbInstance.traceMouseClick();
    Toolkit.sendClick("Google", 30, 30, 1);
}

exports.main = main;
