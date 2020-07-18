var PS = require("lib/powershell");

return {
    main: function() {
        console.log("Starting unlock files...");
        PS.execCommand("dir | Unblock-File");
        console.log("done");
    }
};
