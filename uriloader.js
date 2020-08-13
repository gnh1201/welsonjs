//////////////////////////////////////////////////////////////////////////////////
//
//    uriloader.js
//
/////////////////////////////////////////////////////////////////////////////////

var SYS = require("lib/system");
var SHELL = require("lib/shell");
var URI = require("lib/uri");
var WINLIBS = require("lib/winlibs");

exports.main = function(args) {
    var uri = args[0];
    var pos = uri.indexOf(":///");
    if(pos < 0) {
        console.log("Not vaild URI scheme");
    } else {
        var cmd = [],
            queryString = uri.substring(pos + 4),
            query = URI.parseQueryString(queryString);

        if(!query.application) {
            query.application = "";
        }

        switch(query.application) {
            case "app":
                cmd.push("start");
                cmd.push("/d");
                cmd.push(SYS.getCurrentScriptDirectory());
                cmd.push("app.hta");
                cmd.push(uri); // passing URI to application
                break;
            case "mscalc":
                cmd.push("calc.exe");
                break;
            case "msie":
                //cmd.push("%PROGRAMFILES%\\Internet Explorer\\iexplore.exe");
                //cmd.push("https://github.com/gnh1201/welsonjs");
                WINLIBS.loadLibrary("url").call("FileProtocolHandler", [
                    "https://github.com/gnh1201/welsonjs"
                ]);
                break;
            case "msexcel":
                cmd.push("%PROGRAMFILES%\\Microsoft Office\\Office15\\EXCEL.EXE");
                break;
            case "mspowerpoint":
                cmd.push("%PROGRAMFILES%\\Microsoft Office\\Office15\\POWERPNT.EXE");
                break;
            case "msword":
                cmd.push("%PROGRAMFILES%\\Microsoft Office\\Office15\\WINWORD.EXE");
                break;
            case "msaccess":
                cmd.push("%PROGRAMFILES%\\Microsoft Office\\Office15\\MSACCESS.EXE");
                break;
            dafault:
                console.log("Unknown application");
        }

        if(typeof(query.args) !== "undefined") {
            cmd.push(query.args);
        }

        SHELL.run(cmd);
    }

    return 0;
};
