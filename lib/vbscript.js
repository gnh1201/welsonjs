////////////////////////////////////////////////////////////////////////
// VBScript API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "VBScript (vbscript.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.execScript = function(scriptName, args) {
    var commandOptions = [];

    commandOptions.push("cscript");
    commandOptions.push(scriptName + ".vbs");

    if(typeof(args) !== "undefined") {
        for(var i in args) {
            commandOptions.push(args[i]);
        }
    }

    return SHELL.exec(commandOptions.join(' '));
};

exports.execCommand = function(command) {
    // MSScriptControl.ScriptControl
    var ret, sc = new ActiveXObject("ScriptControl");
    sc.language = "VBScript";
    sc.addCode(command);
    sc.allowUI = true;
    //sc.eval(command);
    ret = sc.run(command);
    sc = null;
    return ret;
};
