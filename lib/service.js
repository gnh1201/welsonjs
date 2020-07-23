var SHELL = require("lib/shell");

exports.VERSIONINFO = "Service Module (service.js) version 0.1";
exports.global = global;
exports.require = global.require;

// https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2012-r2-and-2012/dd228922(v=ws.11)
exports.queryService = function(name, options) {
    var commandOptons = [],
        _options = {
            type: [
                "service",      // type= {driver | service | all}
                "own"           // type= {own | share | interact | kernel | filesys | rec | adapt}
            ],
            state: "active",    // state= {active | inactive | all}
            bufsize: "1024",    // bufsize= <BufferSize>
            ri: "0"             // ri= <ResumeIndex>
            group: ""           // group= <GroupName>
        };

    for(var k in _options) {
        if(k in options) {
            _options[k] = options[k];
        }

        if(_options[k] !== false) {
            if(_options[k].constructor == Array) {
                for(var i in options[k]) {
                    commandOptions.push(k + "=");
                    commandOptions.push("\"" + _options[k][i] + "\"");
                }
            } else {
                commandOptions.push(k + '=');
                commandOptions.push("\"" + _options[k] + "\"");
            }
        }
    }

    return SHELL.exec(commandOptions.join(' '));
};

// https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/sc-create
exports.createService = function(name, options) {
    var commandOptions = [],
        _options = {
            type: "share",      // type= {own | share | kernel | filesys | rec | interact type= {own | share}}
            start: "demand",    // start= {boot | system | auto | demand | disabled | delayed-auto }	
            error: "normal",    // error= {normal | severe | critical | ignore}	
            binPath: false,     // binpath= <BinaryPathName>
            group: false,       // group= <LoadOrderGroup>
            tag: false,         // tag= {yes | no}
            depend: false,      // depend= <dependencies>
            obj: false,         // obj= {<AccountName> | <ObjectName>}
            DisplayName: false, // displayname= <DisplayName>
            password: false     // password= <Password>
        };

    commandOptions.push("sc");
    commandOptions.push("create");
    commandOptions.push(name);

    for(var k in _options) {
        if(k in options) {
            _options[k] = options[k];
        }

        if(_options[k] !== false) {
            if(_options[k].constructor == Array) {
                for(var i in options[k]) {
                    commandOptions.push(k + "=");
                    commandOptions.push("\"" + _options[k][i] + "\"");
                }
            } else {
                commandOptions.push(k + '=');
                commandOptions.push("\"" + _options[k] + "\"");
            }
        }
    }

    return SHELL.exec(commandOptions.join(' '));
};

exports.startService = function(name, args) {
    var commandOptions = [];

    commandOptions.push("sc");
    commandOptions.push("start");
    commandOptions.push(name);

    if(typeof(args) !== "undefined") {
        for(var i in args) {
            commandOptions.push(args[i]);
        }
    }
    
    return SHELL.exec(commandOptions.join(' '));
};

exports.stopService = function(name, args) {
    var commandOptions = [];

    commandOptions.push("sc");
    commandOptions.push("stop");
    commandOptions.push(name);
    
    return SHELL.exec(commandOptions.join(' '));
};

exports.deleteService = function(name) {
    var commandOptions = [];

    commandOptions.push("sc");
    commandOptions.push("delete");
    commandOptions.push(name);

    return SHELL.exec(commandOptions.join(' '));
};
