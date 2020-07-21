//////////////////////////////////////////////////////////////////////////////////
//
//    bootstrap.js
//
/////////////////////////////////////////////////////////////////////////////////

var PS = require("lib/powershell");
var REG = require("lib/registry");
var SYS = require("lib/system");
var SHELL = require("lib/shell");

return {
    main: function() {
        // unlock file
        console.log("Starting unlock files...");
        PS.execCommand("dir | Unblock-File");

        // import necessary registry
        REG.importFromFile("app/assets/reg/Allow_ADO_CORS.reg");
        //REG.importFromFile("app/assets/reg/Add_URI_Scheme.reg");

        // register URI scheme
        REG.write(REG.HKCR, "welsonjs", "", "URL:welsonjs", REG.STRING);
        REG.write(REG.HKCR, "welsonjs", "URL Protocol", "", REG.STRING);
        REG.write(REG.HKCR, "welsonjs\\shell\\open\\command", "", "cscript " + SYS.getCurrentScriptDirectory() + "\\app.js uriloader \"%1\"", REG.STRING);

        // open HTA file
        SHELL.run("app.hta");

        console.log("done");
    }
};
