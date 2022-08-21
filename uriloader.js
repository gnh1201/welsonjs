//////////////////////////////////////////////////////////////////////////////////
//
//    uriloader.js
//
/////////////////////////////////////////////////////////////////////////////////
var SYS = require("lib/system");
var SHELL = require("lib/shell");
var URI = require("lib/uri");
var WINLIBS = require("lib/winlibs");

function main(args) {
    var uri = args[0];
    var pos = uri.indexOf(":///");
    if (pos < 0) {
        console.log("Not vaild URI scheme");
    } else {
        var commands = [],
            queryString = uri.substring(pos + 4),
            query = URI.parseQueryString(queryString);

        if (!query.application)
            query.application = "";

        switch (query.application) {
            case "app":
                commands.push([
                    "start",
                    "/d",
                    SYS.getCurrentScriptDirectory(),
                    "app.hta",
                    uri
                ]);
                break;

            case "mscalc":
                commands.push([
                    "calc.exe"
                ]);
                break;

            case "msie":
                WINLIBS.loadLibrary("url").call("FileProtocolHandler", [
                    "https://github.com/gnh1201/welsonjs"
                ]);
                break;

            case "msexcel":
                commands.push([
                    "%PROGRAMFILES%\\Microsoft Office\\Office15\\EXCEL.EXE"
                ]);
                break;

            case "mspowerpoint":
                commands.push([
                    "%PROGRAMFILES%\\Microsoft Office\\Office15\\POWERPNT.EXE"
                ]);
                break;

            case "msword":
                commands.push([
                    "%PROGRAMFILES%\\Microsoft Office\\Office15\\WINWORD.EXE"
                ]);
                break;

            case "msaccess":
                commands.push([
                    "%PROGRAMFILES%\\Microsoft Office\\Office15\\MSACCESS.EXE"
                ]);
                break;

            case "ldmultiplayer":
                commands.push([
                    "%SYSTEMDRIVE%\\LDPlayer\LDPlayer3.0\\dnmultiplayer.exe"
                ]);
                break;

            case "noxmultiplayer":
                commands.push([
                    "%PROGRAMFILES(X86)%\\Nox\\bin\\MultiPlayerManager.exe"
                ]);
                break;

            case "codingschool":
                commands.push([
                    "%PROGRAMFILES(X86)\\CodingSchool3\\CodingSchool3.exe"
                ]);
                break;

            case "arduino":
                commands.push([
                    "%PROGRAMFILES(X86)%\\Arduino\\arduino.exe"
                ]);
                break;

            case "opentyping":
                commands.push([
                    SYS.getCurrentScriptDirectory() + "\\bin\\OpenTyping\\OpenTyping.exe"
                ]);
                break;

            case "hnctt80":
                commands.push([
                    "%PROGRAMFILES(X86)%\\HNC\\HncTT80\\HncTT.exe"
                ]);
                break;

            dafault:
                console.log("Unknown application");
        }

        if (commands.length > 0) {
            SHELL.run(commands.pop());
        }
    }

    return 0;
}

exports.main = main;
