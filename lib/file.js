//////////////////////////////////////////////////////////////////////////////////
//
//	file.js
//
//	Common routines.  Defines LIB object which contains the API, as well as
//	a global console.log function.
//
/////////////////////////////////////////////////////////////////////////////////

var LIB = require('lib/std');

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

exports.VERSIONINFO = "File Lib (file.js) version 0.2";
exports.global = global;
exports.require = global.require;

/////////////////////////////////////////////////////////////////////////////////
// exports.fileExists
/////////////////////////////////////////////////////////////////////////////////

exports.fileExists = function(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var exists = FSO.FileExists(FN);
    FSO = null;
    return exists;
};

/////////////////////////////////////////////////////////////////////////////////
// exports.folderExists
/////////////////////////////////////////////////////////////////////////////////

exports.folderExists = function(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var exists = FSO.FolderExists(FN);
    FSO = null;
    return exists;
};

/////////////////////////////////////////////////////////////////////////////////
// exports.fileGet
/////////////////////////////////////////////////////////////////////////////////

exports.fileGet = function(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var file = FSO.GetFile(FN);
    FSO = null;
    return file;
};

/////////////////////////////////////////////////////////////////////////////////
// exports.readFile
//	Read the conents of the pass filename and return as a string
/////////////////////////////////////////////////////////////////////////////////

exports.readFile = function(FN, charset) {
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
};

/////////////////////////////////////////////////////////////////////////////////
// exports.writeFile
//	Write the passed content to named disk file
/////////////////////////////////////////////////////////////////////////////////

exports.writeFile = function(FN, content, charset) {
    var ok;
    if (charset) {
        console.log("WRITE TO DISK USING ADODB.Stream CHARSET " + charset);
        try {
            var fsT = CreateObject("ADODB.Stream");
            fsT.Type = 2; // save as text/string data.
            fsT.Charset = charset; // Specify charset For the source text data.
            fsT.Open();
            fsT.WriteText(content);
            fsT.SaveToFile(FN, 2); // save as binary to disk
            ok = true;
        } catch (e) {
            console.log("ADODB.Stream: ERROR! " + e.number + ", " + e.description + ", FN=" + FN);
        }
    } else {
        console.log("WRITE TO DISK USING OpenTextFile CHARSET ascii");
        var FSO = CreateObject("Scripting.FileSystemObject");
        try {
            var TS = FSO.OpenTextFile(FN, 2, true, 0); // ascii
            TS.Write(content);
            TS.Close();
            TS = null;
            ok = true;
        } catch (e) {
            console.log("OpenTextFile: ERROR! " + e.number + ", " + e.description + ", FN=" + FN);
        }
        FSO = null;
    }
    return ok;
};

/////////////////////////////////////////////////////////////////////////////////
// exports.moveFile
/////////////////////////////////////////////////////////////////////////////////

exports.moveFile = function(FROM, TO) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var res = FSO.MoveFile(FROM, TO);
    FSO = null;
    return res;
};

/////////////////////////////////////////////////////////////////////////////////
// exports.createFolder
/////////////////////////////////////////////////////////////////////////////////

exports.createFolder = function(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var res = FSO.CreateFolder(FN);
    FSO = null;
    return res;
};

/////////////////////////////////////////////////////////////////////////////////
// exports.deleteFile
/////////////////////////////////////////////////////////////////////////////////

exports.deleteFile = function(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var res = FSO.DeleteFile(FN);
    FSO = null;
    return res;
};

/////////////////////////////////////////////////////////////////////////////////
