//////////////////////////////////////////////////////////////////////////////////
//
//    file.js
//    with the PIPE based IPC (lib/pipe-ipc.js)
//
/////////////////////////////////////////////////////////////////////////////////

var LIB = require("lib/std");
var PipeIPC = require("lib/pipe-ipc");

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
    if (typeof charset === "undefined") {
        console.warn("CHARSET has not been passed. Force to UTF-8.");
        charset = PipeIPC.CdoUTF_8;
    }

    var text =  ''; 
    var pipe = PipeIPC.connect("volatile");
    pipe.setCharset(charset);
    pipe.loadFromFile(FN, charset);
    text += pipe.read();
    pipe.destroy();

    return text;
}

/////////////////////////////////////////////////////////////////////////////////
// writeFile
//    Write the passed content to named disk file
/////////////////////////////////////////////////////////////////////////////////

function writeFile(FN, content, charset) {
    if (typeof content === "undefined") {
        console.warn("CONTENT has not been passed. Force to empty string.");
        content = '';
    }
    if (typeof charset === "undefined") {
        console.warn("CHARSET has not been passed. Force to UTF-8.");
        charset = PipeIPC.CdoUTF_8;
    }

    var pipe = PipeIPC.connect("volatile");
    pipe.setCharset(charset);
    pipe.startRecorder(FN, PipeIPC.ForWriting);
    pipe.write(content);
    pipe.destroy();
    return true;
}

/////////////////////////////////////////////////////////////////////////////////
// writeBinaryFile
/////////////////////////////////////////////////////////////////////////////////

function writeBinaryFile(FN, DATA) {
    var BinaryStream = CreateObject("ADODB.Stream");
    BinaryStream.Type = PipeIPC.adTypeBinary;
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
    try {
        eval(readFile(FN));
    } catch (e) {
        console.error(e.message, "in", FN);
    }
}

/////////////////////////////////////////////////////////////////////////////////
// appendFile
/////////////////////////////////////////////////////////////////////////////////

function appendFile(FN, content, charset) {
    var result = false;
    var pipe = PipeIPC.connect(PipeIPC.CRC32(FN));
    pipe.setCharset(charset);
    pipe.startRecorder(FN, PipeIPC.ForAppending);
    result = pipe.write(content);
    pipe.close();

    return result;
}

/////////////////////////////////////////////////////////////////////////////////
// prependFile
/////////////////////////////////////////////////////////////////////////////////

function prependFile(FN, content, charset) {
    var pipe = PipeIPC.connect("volatile");
    pipe.setCharset(charset);
    pipe.startRecorder(FN, PipeIPC.ForWriting);
    pipe.write(content);
    pipe.destroy();
    return true;
}

/////////////////////////////////////////////////////////////////////////////////
// rotateFile
/////////////////////////////////////////////////////////////////////////////////

function rotateFile(FN, content, numOfLines, charset) {
    var result = false;
    var pipe = PipeIPC.connect(PipeIPC.CRC32(FN));
    pipe.setCharset(charset);
    pipe.setMaxSentences(numOfLines);
    pipe.startRecorder(FN, PipeIPC.ForAppending);
    result = pipe.write(content);
    pipe.close();

    return result;
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
exports.appendFile = appendFile;
exports.rotateFile = rotateFile;

exports.CdoCharset = PipeIPC.CdoCharset;

exports.VERSIONINFO = "File IO Library (file.js) version 0.2.12";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
