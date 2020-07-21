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
            var commandOptions = [],
                queryString = uri.substring(pos + 4),
                query = this.parseQuery(queryString);

            if(!query.application) {
                query.application = "";
            }

            switch(query.application) {
                case "app":
                    commandOptions.push("app.hta");
                    break;
                case "mscalc":
                    commandOptions.push("calc");
                    break;
                case "msie":
                    commandOptions.push("\"%PROGRAMFILES%\\Internet Explorer\\iexplore.exe\"");
                    commandOptions.push("https://github.com/gnh1201/welsonjs");
                    break;
                case "msexcel":
                    commandOptions.push("\"%PROGRAMFILES%\\Microsoft Office\\Office15\\EXCEL.EXE\"");
                    break;
                case "mspowerpoint":
                    commandOptions.push("\"%PROGRAMFILES%\\Microsoft Office\\Office15\\POWERPNT.EXE\"");
                    break;
                case "msword":
                    commandOptions.push("\"%PROGRAMFILES%\\Microsoft Office\\Office15\\WINWORD.EXE\"");
                    break;
                case "msaccess":
                    commandOptions.push("\"%PROGRAMFILES%\\Microsoft Office\\Office15\\MSACCESS.EXE\"");
                    break;
                dafault:
                    console.log("Unknown application");
                    break;
            }

            if(typeof(query.argument) !== "undefined") {
                commandOptions.push(query.argument);
            }

            SHELL.run(commandOptions.join(' '));
        }
    }
}
