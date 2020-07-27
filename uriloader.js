//////////////////////////////////////////////////////////////////////////////////
//
//    uriloader.js
//
/////////////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

return {
    main: function(args) {
        var uri = args[0];
        var pos = uri.indexOf(":///");
        if(pos < 0) {
            console.log("Not vaild URI scheme");
        } else {
            var cmd = [],
                queryString = uri.substring(pos + 4),
                query = this.parseQueryString(queryString);

            if(!query.application) {
                query.application = "";
            }

            switch(query.application) {
                case "app":
                    cmd.push(["app.hta"].concat(args));
                    break;
                case "mscalc":
                    cmd.push("calc.exe");
                    break;
                case "msie":
                    cmd.push("%PROGRAMFILES%\\Internet Explorer\\iexplore.exe");
                    cmd.push("https://github.com/gnh1201/welsonjs");
                    break;
                case "msexcel":
                    cmd.push("%PROGRAMFILES%\\Microsoft Office\\Office15\\EXCEL.EXE");
                    break;
                case "mspowerpoint":
                    cmd.push("%PROGRAMFILES%\\Microsoft Office\\Office15\\POWERPNT.EXE");
                    break;
                case "msword":
                    cmd.push("%PROGRAMFILES%\\Microsoft Office\\Office15\\WINWORD.EXE");
                    break;
                case "msaccess":
                    cmd.push("%PROGRAMFILES%\\Microsoft Office\\Office15\\MSACCESS.EXE");
                    break;
                dafault:
                    console.log("Unknown application");
                    break;
            }

            if(typeof(query.args) !== "undefined") {
                cmd.push(query.args);
            }

            SHELL.run(cmd);

            return 0;
        }
    },
    parseQueryString: function(queryString) {
        var query = {};
        var pairs = (queryString.substring(0, 1) === '?' ? queryString.substring(1) : queryString).split('&');
        for (var i in pairs) {
            var pair = pairs[i].split('=');
            var _k = decodeURIComponent(pair[0]);
            var _v = decodeURIComponent(pair[1] || '');
            var path = _k.split('[').map(function(s) {
                return (s.indexOf(']') < 0 ? s : s.substring(0, s.length -1));
            }).join('/');

            if(path in query) {
                if (Array.isArray(query[path])) {
                    query[path].push(_v);
                } else {
                    query[path] = [query[path], _v];
                }
            } else {
                query[path] = _v;
            }
        }
        return query;
    };
}
