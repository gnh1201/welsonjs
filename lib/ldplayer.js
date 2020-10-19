////////////////////////////////////////////////////////////////////////
// LDPlayer API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.VERSIONINFO = "LDPlayer (ldplayer.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.getList = function() {
    var data = [];
    var cmd = [
        SYS.getEnvString("SYSTEMDRIVE") + "/LDPlayer/LDPlayer3.0/ldconsole.exe",
        "list2"
    ];
    var result = SHELL.exec(cmd);
    var lines = result.split(/\r?\n/);
   
    for(var i = 0; i < lines.length; i++) {
        var row = lines[i].split(',');
      
        if(row.length == 7) {
            data.push({
                index: row[0],
                title: row[1],
                topWindowHandle: row[2],
                binddWindowHandle: row[3],
                androidStarted: row[4],
                PID: row[5],
                PIDVBox: row[6]
            });
        }
    }
    
    return data;
};
