// scriptcontrol.js
// The Simplified entrypoint for win32com (based on MSScriptControl.ScriptControl)

var workingDirectory = "";

if (typeof CreateObject === "undefined") {
    var CreateObject = function(progId, serverName, callback) {
        var progIds = (progId instanceof Array ? progId : [progId]);

        for (var i = 0; i < progIds.length; i++) {
            try {
                var obj = CreateObject.make(progIds[i], serverName);
                if (typeof callback === "function") {
                    callback(obj, progIds[i]);
                }
                return obj;
            } catch (e) {
                console.error(e.message);
            };
        }
    };
    CreateObject.make = function(p, s) {
        if (typeof WScript !== "undefined") {
            return WScript.CreateObject(p, s);
        } else if (typeof ActiveXObject !== "undefined") {
            return new ActiveXObject(p);
        }
    };
}

function readFile(FN, charset) {
    if(typeof(charset) === "undefined") {
        var FSO = CreateObject("Scripting.FileSystemObject");
        var T = null;
        try {
            var TS = FSO.OpenTextFile(FN, 1);
            if (TS.AtEndOfStream) return "";
            T = TS.ReadAll();
            TS.Close();
            TS = null;
        } catch (e) {
            console.log("ERROR! " + e.number + ", " + e.description + ", FN=" + FN);
        }
        FSO = null;
        return T;
    } else {
        var fsT = CreateObject("ADODB.Stream");
        fsT.CharSet = charset;
        fsT.Open();
        fsT.LoadFromFile(FN);
        T = fsT.ReadText();
        fsT = null;
        return T;
    }
}

function setWorkingDirectory(directory) {
    workingDirectory = directory;
}

function run(targetName) {
    var w = CreateObject("WScript.Shell")
        , stdOutPath = "tmp\\stdout.txt"
        , stdErrPath = "tmp\\stderr.txt"
    ;
    if (workingDirectory != "") {
        w.CurrentDirectory = workingDirectory;
    }
    w.Run("%comspec% /c (cscript //NoLogo app.js " + targetName + ") 1> " + stdOutPath + " 2> " + stdErrPath, 0, true);
    return readFile(stdOutPath, "utf-8");
}
