//////////////////////////////////////////////////////////////////////////////////
//
//    file.js
//
//    Common routines.  Defines LIB object which contains the API, as well as
//    a global console.log function.

//    with the PIPE based IPC (lib/pipe-ipc.js)
//
/////////////////////////////////////////////////////////////////////////////////

var LIB = require("lib/std");
var PipeIPC = require("lib/pipe-ipc");

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

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
    var pipe = PipeIPC.create();
    pipe.setCharset(charset);
    return pipe.readTextFromFile(FN);
}

/////////////////////////////////////////////////////////////////////////////////
// writeFile
//    Write the passed content to named disk file
/////////////////////////////////////////////////////////////////////////////////

function writeFile(FN, content, charset) {
    var pipe = PipeIPC.connect(PipeIPC.createUUIDv4().substring(0, 8));
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
    eval(readFile(FN));
}

function appendFile(FN, content, charset) {
    var pipe = PipeIPC.connect("write");
    pipe.setCharset(charset);
    pipe.startRecorder(FN, PipeIPC.ForAppending);
    pipe.write(content);
    pipe.destroy();
}

function rotateFile(FN, content, numOfLines, charset) {
    var pipe = PipeIPC.connect("write");
    pipe.setCharset(charset);
    pipe.setMaxSentences(numOfLines);
    pipe.startRecorder(FN, PipeIPC.ForAppending);
    pipe.write(content);
    pipe.destroy();
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

exports.VERSIONINFO = "File Library (file.js) version 0.2.3";
exports.global = global;
exports.require = global.require;
