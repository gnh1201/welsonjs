var PS = require("lib/powershell");
//var REGISTRY = require("lib/registry");

return {
    main: function() {
        console.log("Starting unlock files...");
        PS.execCommand("dir | Unblock-File");
        //REGISTRY.import("app\\assets\\reg\\Add_URI_Scheme");
        //REGISTRY.import("app\\assets\\reg\\Allow_ADO_CORS");
        console.log("done");
    }
};
