//////////////////////////////////////////////////////////////////////////////////
//
//    file.js
//
//    Common routines.  Defines LIB object which contains the API, as well as
//    a global console.log function.
//
/////////////////////////////////////////////////////////////////////////////////

var LIB = require("lib/std");
var SHELL = require("lib/shell");

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

exports.VERSIONINFO = "File Lib (file.js) version 0.2";
exports.global = global;
exports.require = global.require;

/////////////////////////////////////////////////////////////////////////////////
// fileExists
/////////////////////////////////////////////////////////////////////////////////

function fileExists(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var exists = FSO.FileExists(FN);
    FSO = null;
    return exists;
}

/////////////////////////////////////////////////////////////////////////////////
// folderExists
/////////////////////////////////////////////////////////////////////////////////

function folderExists(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var exists = FSO.FolderExists(FN);
    FSO = null;
    return exists;
}

/////////////////////////////////////////////////////////////////////////////////
// fileGet
/////////////////////////////////////////////////////////////////////////////////

function fileGet(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var file = FSO.GetFile(FN);
    FSO = null;
    return file;
}
/////////////////////////////////////////////////////////////////////////////////
// readFile
//    Read the conents of the pass filename and return as a string
/////////////////////////////////////////////////////////////////////////////////

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

/////////////////////////////////////////////////////////////////////////////////
// writeFile
//    Write the passed content to named disk file
/////////////////////////////////////////////////////////////////////////////////

function writeFile(FN, content, charset) {
    var Stream_No_UTF8_BOM = function(objStream) {
        var _objStream = CreateObject("ADODB.Stream");
        _objStream.Type = 1;
        _objStream.Mode = 3;
        _objStream.Open();
        objStream.Position = 3;
        objStream.CopyTo(_objStream);
        objStream.Flush();
        //objStream.Close();
        return _objStream;
    };
    var ok = false;

    while (!ok) {
        // [lib/file] Can not overwrite a file with ADODB.Stream SaveToFile() #32
        SHELL.exec(["del", FN]);

        // ascii:Scripting.FileSystemObject, unicode:ADODB.Stream
        if (charset) {
            console.log("WRITE TO DISK USING ADODB.Stream CHARSET " + charset);
            try {
                var fsT = CreateObject("ADODB.Stream");
                fsT.Type = 2; // save as text/string data.
                fsT.Charset = charset; // Specify charset For the source text data.
                fsT.Open();
                fsT.WriteText(content);
                if (charset == "utf-8") {
                    Stream_No_UTF8_BOM(fsT).SaveToFile(FN, 2); // save as binary to disk
                } else {
                    fsT.SaveToFile(FN, 2); // save as binary to disk
                }
                fsT.Close();
                fsT = null;
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
    }

    return ok;
}

/////////////////////////////////////////////////////////////////////////////////
// writeBinaryFile
/////////////////////////////////////////////////////////////////////////////////

function writeBinaryFile(FN, DATA) {
    var adTypeText = 1;
    var adSaveCreateOverWrite = 2;
    var BinaryStream = CreateObject("ADODB.Stream");
    BinaryStream.Type = adTypeText;
    BinaryStream.Open();
    BinaryStream.Write(DATA);
    BinaryStream.SaveToFile(FN, adSaveCreateOverWrite);
    BinaryStream.Close();
}

/////////////////////////////////////////////////////////////////////////////////
// moveFile
/////////////////////////////////////////////////////////////////////////////////

function moveFile(FROM, TO) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var result = FSO.MoveFile(FROM, TO);
    FSO = null;
    return result;
}

/////////////////////////////////////////////////////////////////////////////////
// createFolder
/////////////////////////////////////////////////////////////////////////////////

function createFolder(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var result = FSO.CreateFolder(FN);
    FSO = null;
    return result;
}

/////////////////////////////////////////////////////////////////////////////////
// deleteFolder
/////////////////////////////////////////////////////////////////////////////////

function deleteFolder(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var result = FSO.DeleteFolder(FN);
    FSO = null;
    return result;
}

/////////////////////////////////////////////////////////////////////////////////
// deleteFile
/////////////////////////////////////////////////////////////////////////////////

function deleteFile(FN) {
    var FSO = CreateObject("Scripting.FileSystemObject");
    var result = FSO.DeleteFile(FN);
    FSO = null;
    return result;
}

/////////////////////////////////////////////////////////////////////////////////
// includeFile
/////////////////////////////////////////////////////////////////////////////////

function includeFile(FN) {
    var fso = CreateObject("Scripting.FileSystemObject");
    var fileStream = fso.openTextFile(FN);
    var fileData = fileStream.readAll();
    fileStream.Close();
    eval(fileData);
}

exports.fileExists = fileExists;
exports.folderExists = folderExists;
exports.fileGet = fileGet;
exports.readFile = readFile;
exports.writeFile = writeFile;
exports.writeBinaryFile = writeBinaryFile;
exports.moveFile = moveFile;
exports.createFolder = createFolder;
exports.deleteFolder = deleteFolder;
exports.deleteFile = deleteFile;
exports.includeFile = includeFile;
