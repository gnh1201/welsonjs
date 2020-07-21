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
        //REG.write(hKey, path, key, value, REG.STRING, computer);

		// open HTA file
		SHELL.run("app.hta");

        console.log("done");
    }
};
