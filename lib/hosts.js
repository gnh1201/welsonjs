////////////////////////////////////////////////////////////////////////
// Hosts API
////////////////////////////////////////////////////////////////////////

var SYS = require("lib/system");
var FILE = require("lib/file");

exports.getHosts = function() {
    var hosts = [];
    
    var filePath = SYS.getEnvString("windir") + "\\System32\\\drivers\\etc\\hosts";
    var fileContent = FILE.readFile(filePath, "utf-8");
    
    var lines = fileContent.split(/[\r\n]+/g).filter(function(s) {
        return !(s.indexOf('#') == 0)
    }).map(function(s) {
        var pos = s.indexOf('#');
        if(pos > -1) {
            return s.substring(pos).replace(/\s\s/g, ' ');
        } else {
            return s.replace(/\s\s/g, ' ');
        }
    });

    for (var i = 0; i < lines.length; i++) {
        var col = lines[i].split(' ');
        hosts.push({
            host: col[0],
            domain: col[1]
        });
    }

    return hosts;
};
