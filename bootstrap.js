//////////////////////////////////////////////////////////////////////////////////
//
//    bootstrap.js
//
/////////////////////////////////////////////////////////////////////////////////

var PS = require("lib/powershell");
var REG = require("lib/registry");

return {
    main: function() {
		// unlock file
        console.log("Starting unlock files...");
        PS.execCommand("dir | Unblock-File");

		// import necessary registry
		REG.importFromFile("app/assets/reg/Allow_ADO_CORS.reg");
		//REG.importFromFile("app/assets/reg/Add_URI_Scheme.reg");

		// register URI scheme
		// todo

        console.log("done");
    }
};
