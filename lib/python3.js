var SHELL = require("lib/shell");

function PythonObject(platform) {
    this.version = "3.10.2-embed";
    this.platform = platform;

    this.setVersion = function(version) {
        this.version = version;
    };

    this.execScript = function(scriptName, args) {
        return SHELL.exec([
            "bin\\python-" + this.version + "\\" + this.platform + "\\python",
            scriptName
        ].concat(args));
    };

    this.runScript = function(scriptName, args) {
        return SHELL.show([
            "bin\\python-" + this.version + "\\" + this.platform + "\\python",
            scriptName
        ].concat(args));
    };
}

exports.PythonObject = PythonObject;

exports.create = function(platform) {
    var platform = (typeof platform !== "undefined" ? platform : "amd64");
    return new PythonObject(platform);
};

exports.execScript = function(scriptName, args) {
    return (new PythonObject()).execScript(scriptName, args);
};

exports.VERSIONINFO = "Python Interface (python3.js) version 0.2";
exports.global = global;
exports.require = global.require;
