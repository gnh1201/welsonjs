////////////////////////////////////////////////////////////////////////
// Hosts API
////////////////////////////////////////////////////////////////////////

var SYS = require("lib/system");
var FILE = require("lib/file");

exports.getHosts = function() {
    var hosts = [];
    
    var filePath = SYS.getEnvString("windir") + "\\System32\\\drivers\\etc\\hosts";
    var fileContent = FILE.readFile(filePath, "utf-8");
    
    var rows = fileContent.split(/[\r\n]+/g).filter(function(s) {
        return !(s.indexOf('#') == 0);
    }).map(function(s) {
        var pos = s.indexOf(" #");
        return (pos > -1 ? s.substring(0, pos) : s).split(/\s+/);
    });

    for (var i = 0; i < rows.length; i++) {
        hosts.push({
            host: rows[i][0],
            domain: rows[i][1]
        });
    }

    return hosts;
};
