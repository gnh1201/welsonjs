var nsm = CreateObject("WelsonJS.NamedSharedMemory");

function NamedSharedMemory(pipename) {
    this.pipename = pipename;

    this.writeText = function(text) {
        return nsm.WriteTextToSharedMemory(this.pipename, text);
    };

    this.readText = function() {
        return nsm.ReadTextFromSharedMemory(this.pipename);
    };
    
    this.clear = function() {
        return nsm.ClearSharedMemory(this.pipename);
    };
    
    this.close = function() {
        return nsm.CloseSharedMemory(this.pipename);
    };

    // Description of the last failure (e.g. text too long, Win32 error), or "".
    this.getLastError = function() {
        return nsm.LastError;
    };
}

exports.NamedSharedMemory = NamedSharedMemory;

exports.VERSIONINFO = "Named Shared Memory version 0.2";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
