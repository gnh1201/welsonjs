var SHELL = require("lib/shell");

var scope = {
    VERSIONINFO: "Powershell (powershell.js) version 0.1",
    global: global,
    require: global.require
};

scope.execScript = function(scriptName) {
    return SHELL.exec("powershell.exe -NoProfile -ExecutionPolicy Bypass -nologo -file " + scriptName + ".ps1");
};

return scope;
