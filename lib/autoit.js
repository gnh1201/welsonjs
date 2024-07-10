// autoit.js
// AutoIt (AutoIt3, AutoItX) API interface for WelsonJS framework
// Namhyeon Go (Catswords Research) <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
function AutoItObject() {
    this._interface = null;

    this.create = function() {
        try {
            this._interface = CreateObject("AutoItX3.Control");
        } catch (e) {
            console.error("AutoIt_CreateObject() ->", e.message);
        }
    };

    this.execScript = function(scriptName) {
        var result = null;

        var cmd = [
            "%PROGRAMFILES(X86)%\\AutoIt3\\AutoIt3.exe",
            scriptName + ".au3"
        ];

        if (typeof args !== "undefined") {
            cmd = cmd.concat(args);
        }
        
        try {
            result = SHELL.exec(cmd);
        } catch (e) {
            console.error("AutoIt_execScript() ->", e.message);
        }

        return result;
    };
    
    this.callFunction = function(functionName, args) {
        console.log("Calling AutoItX function...", functionName);

        if (this._interface != null) {
            try {
                //this._interface[functionName].apply(null, args);
                eval("this._interface." + functionName + "(\"" + args.map(addslashes).join("\", \"") + "\")");
                sleep(300);
            } catch (e) {
                console.error("AutoItObject.callFunction() ->", e.message);
            }
        } else {
            console.warn("AutoItX is disabled");
        }
    };

    this.create();
}

exports.create = function() {
    return new AutoItObject();
};

exports.VERSIONINFO = "AutoIt API interface version 0.1.3";
exports.global = global;
exports.require = global.require;
