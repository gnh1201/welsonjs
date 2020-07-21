//////////////////////////////////////////////////////////////////////////////////
//
//    uriloader.js
//
/////////////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

return {
    parseQuery: function(queryString) {
        var query = {};
        var pairs = (queryString.substring(0, 1) === '?' ? queryString.substring(1) : queryString).split('&');
        for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i].split('=');
            query[decodeURIComponent(pair[0])] = decodeURIComponent(pair[1] || '');
        }
        return query;
    },
    main: function(args) {
        var uri = args[0];
        var pos = uri.indexOf(':///');
        if(pos < 0) {
            console.log("Not vaild URI");
        } else {
            var queryString = uri.substring(pos + 4);
            var query = this.parseQuery(queryString);
            var application = query['application'];
            var argument = query['argument'];
            var filename;

            switch(application) {
                case "app":
                    filename = "app.hta";
                    break;
                case "mscalc":
                    filename = "calc";
                    break;
                case "msie":
                    filename = "%PROGRAMFILES%\\Internet Explorer\\iexplore.exe";
                    break;
                case "msexcel":
                    filename = "%PROGRAMFILES%\\Microsoft Office\\Office15\\EXCEL.EXE";
                    break;
                case "mspowerpoint":
                    filename = "%PROGRAMFILES%\\Microsoft Office\\Office15\\POWERPNT.EXE";
                    break;
                case "msword":
                    filename = "%PROGRAMFILES%\\Microsoft Office\\Office15\\WINWORD.EXE";
                    break;
                case "msaccess":
                    filename = "%PROGRAMFILES%\\Microsoft Office\\Office15\\MSACCESS.EXE";
                    break;
                dafault:
                    console.log("Unknown application");
                    break;
            }

            SHELL.run("\"" + filename + "\"" + " " + argument);
        }
    }
}
