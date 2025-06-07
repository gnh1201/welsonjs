// file.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// FILE IO Library with the PIPE based IPC (lib/pipe-ipc.js)
// 
var STD = require("lib/std");
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

// Function to load and parse the .env file
var loadEnvFromFile = function(envFilePath, callback) {
    try {
        // Read the file content using PipeIPC.CdoCharset.CdoUTF_8 encoding
        var envString = readFile(envFilePath, PipeIPC.CdoCharset.CdoUTF_8);
        
        // Parse the environment variables
        var envConfig = parseEnv(envString);

        console.log('Environment variables loaded from ' + envFilePath);

        // Call the callback function if provided
        if (typeof callback === "function") {
            try {
                callback(envConfig);
            } catch (e) {
                console.error('Callback error:', e.message);
            }
        }
    } catch (e) {
        console.error('Error reading environment file:', envFilePath, e.message);
    }
};

// Function to find --env-file argument in the args array and load the environment file
var loadEnvFromArgs = function(args, callback) {
    var envFileArg = args.find(function(x) {
        return x.startsWith('--env-file=');
    });

    if (envFileArg) {
        var envFilePath = envFileArg.split('=')[1];
        loadEnvFromFile(envFilePath, callback);
    } else {
        console.warn('No --env-file argument provided.');
    }
};

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
exports.loadEnvFromFile = loadEnvFromFile;
exports.loadEnvFromArgs = loadEnvFromArgs;

exports.CdoCharset = PipeIPC.CdoCharset;

exports.VERSIONINFO = "File IO Library (file.js) version 0.2.13";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
