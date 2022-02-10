////////////////////////////////////////////////////////////////////////
// AutoIt (AutoIt3, AutoItX) API
////////////////////////////////////////////////////////////////////////

function AutoIt_CreateObject = function() {
    var obj = null;

    try {
        obj = CreateObject("AutoItX3.Control");
    } catch (e) {
        console.error("AutoIt_CreateObject() ->", e.message);
    }
    
    return obj;
}

function AutoIt_execScript = function(scriptName) {
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
}

function AutoItObject() {
    this.interface = null;

    this.create = function() {
        this.interface = AutoIt_CreateObject();
    };

    this.execScript = function(scriptName) {
        return AutoIt_execScript(scriptName);
    };
	
	this.callFunction = function(functionName, args) {
        if (this.interface != null) {
			try {
				this.interface[functionName].apply(args);
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
