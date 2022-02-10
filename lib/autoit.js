////////////////////////////////////////////////////////////////////////
// AutoIt (AutoIt3, AutoItX) API
////////////////////////////////////////////////////////////////////////

function AutoItObject() {
    this.interface = null;

    this.create = function() {
        try {
            this.interface = CreateObject("AutoItX3.Control");
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

        if (this.interface != null) {
            try {
                //this.interface[functionName].apply(null, args);
                eval("this.interface." + functionName + "(\"" + args.map(addslashes).join("\", \"") + "\")");
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