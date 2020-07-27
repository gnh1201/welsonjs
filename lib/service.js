var SHELL = require("lib/shell");

exports.VERSIONINFO = "Service Lib (service.js) version 0.1";
exports.global = global;
exports.require = global.require;

// https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2012-r2-and-2012/dd228922(v=ws.11)
exports.queryService = function(name, options) {
    var cmd = [
            "sc",
            "query",
            name
        ],
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
            if(Array.isArray(_options[k])) {
                for(var i in options[k]) {
                    cmd.push(k + '=');
                    cmd.push(_options[k][i]);
                }
            } else {
                cmd.push(k + '=');
                cmd.push(_options[k]);
            }
        }
    }

    return SHELL.exec(cmd);
};

// https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/sc-create
exports.createService = function(name, options) {
    var cmd = [
            "sc",
            "create",
            name
        ],
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

    for(var k in _options) {
        if(k in options) {
            _options[k] = options[k];
        }

        if(_options[k] !== false) {
            if(Array.isArray(_options[k])) {
                for(var i in options[k]) {
                    cmd.push(k + '=');
                    cmd.push(_options[k][i]);
                }
            } else {
                cmd.push(k + '=');
                cmd.push(_options[k]);
            }
        }
    }

    return SHELL.exec(cmd);
};

exports.startService = function(name, args) {
    var cmd = [
        "sc",
        "start",
        name
    ];

    if(typeof(args) !== "undefined") {
        cmd = cmd.concat(args);
    }

    return SHELL.exec(cmd);
};

exports.stopService = function(name) {
    return SHELL.exec([
        "sc",
        "stop",
        name
    ]);
};

exports.deleteService = function(name) {
    return SHELL.exec([
        "sc",
        "delete",
        name
    ]);
};
