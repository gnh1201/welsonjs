var SHELL = require("lib/shell");

function PythonObject() {
    this.version = "3.10.2-embed";
    this.platform = "amd64";
    
    this.setVersion = function(version) {
        this.version = version;
    };
    
    this.setPlatform = function(platform) {
        this.platfrom = platform;
    };

    this.execScript = function(scriptName, args) {
        return SHELL.exec([
            "bin\\python-" + this.version + "\\" + this.platform + "\\python",
            scriptName
        ].concat(args));
    };
}

exports.PythonObject = PythonObject;

exports.create = function() {
    return new PythonObject();
};

exports.execScript = function(scriptName, args) {
    return (new PythonObject()).execScript(scriptName, args);
};

exports.VERSIONINFO = "Python Interface (python3.js) version 0.1";
exports.global = global;
exports.require = global.require;
