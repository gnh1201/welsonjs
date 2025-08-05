// file.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// FILE IO Library with the PIPE based IPC (lib/pipe-ipc.js)
// 
var STD = require("lib/std");
var PipeIPC = require("lib/pipe-ipc");

function fileExists(path) {
    return UseObject("Scripting.FileSystemObject", function(fso) {
        return fso.FileExists(path);
    });
}

function folderExists(path) {
    return UseObject("Scripting.FileSystemObject", function(fso) {
        return fso.FolderExists(path);
    });
}

function fileGet(path) {
    return UseObject("Scripting.FileSystemObject", function(fso) {
        return fso.GetFile(path);
    });
}

function readFile(path, charset) {
    if (typeof charset === "undefined") {
        console.warn("CHARSET not passed. Defaulting to UTF-8.");
        charset = PipeIPC.CdoUTF_8;
    }

    var pipe = PipeIPC.connect("volatile");
    pipe.setCharset(charset);
    pipe.loadFromFile(path, charset);
    var text = pipe.read();
    pipe.destroy();

    return text;
}

function writeFile(path, content, charset) {
    if (typeof content === "undefined") {
        console.warn("CONTENT not passed. Defaulting to empty string.");
        content = '';
    }
    if (typeof charset === "undefined") {
        console.warn("CHARSET not passed. Defaulting to UTF-8.");
        charset = PipeIPC.CdoUTF_8;
    }

    var pipe = PipeIPC.connect("volatile");
    pipe.setCharset(charset);
    pipe.startRecorder(path, PipeIPC.ForWriting);
    pipe.write(content);
    pipe.destroy();

    return true;
}

function writeBinaryFile(path, data) {
    var binaryStream = CreateObject("ADODB.Stream");
    binaryStream.Type = PipeIPC.adTypeBinary;
    binaryStream.Open();
    binaryStream.Write(data);
    binaryStream.SaveToFile(path, adSaveCreateOverWrite);
    binaryStream.Close();
}

function moveFile(fromPath, toPath) {
    return UseObject("Scripting.FileSystemObject", function(fso) {
        return fso.MoveFile(fromPath, toPath);
    });
}

function createFolder(path) {
    return UseObject("Scripting.FileSystemObject", function(fso) {
        return fso.CreateFolder(path);
    });
}

function deleteFolder(path) {
    return UseObject("Scripting.FileSystemObject", function(fso) {
        return fso.DeleteFolder(path);
    });
}

function deleteFile(path) {
    return UseObject("Scripting.FileSystemObject", function(fso) {
        return fso.DeleteFile(path);
    });
}

function includeFile(path) {
    try {
        eval(readFile(path));
    } catch (e) {
        console.error(e.message, "in", path);
    }
}

function getFilesFromFolder(path) {
    if (!folderExists(path)) return [];

    var folder = UseObject("Scripting.FileSystemObject", function(fso) {
        return fso.GetFolder(path);
    });

    return Array.from(folder.Files);
}

function appendFile(path, content, charset) {
    var pipe = PipeIPC.connect(PipeIPC.CRC32(path));
    pipe.setCharset(charset);
    pipe.startRecorder(path, PipeIPC.ForAppending);
    var result = pipe.write(content);
    pipe.close();
    return result;
}

function prependFile(path, content, charset) {
    var pipe = PipeIPC.connect("volatile");
    pipe.setCharset(charset);
    pipe.startRecorder(path, PipeIPC.ForWriting);
    pipe.write(content);
    pipe.destroy();
    return true;
}

function rotateFile(path, content, numOfLines, charset) {
    var pipe = PipeIPC.connect(PipeIPC.CRC32(path));
    pipe.setCharset(charset);
    pipe.setMaxSentences(numOfLines);
    pipe.startRecorder(path, PipeIPC.ForAppending);
    var result = pipe.write(content);
    pipe.close();
    return result;
}

function loadEnvFromFile(path, callback) {
    try {
        var envString = readFile(path, PipeIPC.CdoCharset.CdoUTF_8);
        var envConfig = parseEnv(envString);
        console.log('Environment variables loaded from ' + path);

        if (typeof callback === "function") {
            try {
                callback(envConfig);
            } catch (e) {
                console.error('Callback error:', e.message);
            }
        }
    } catch (e) {
        console.error('Error reading environment file:', path, e.message);
    }
}

function loadEnvFromArgs(args, callback) {
    var envFileArg = args.find(function(x) {
        return x.startsWith('--env-file=');
    });

    if (envFileArg) {
        var path = envFileArg.split('=')[1];
        loadEnvFromFile(path, callback);
    } else {
        console.warn('No --env-file argument provided.');
    }
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
exports.getFilesFromFolder = getFilesFromFolder;
exports.appendFile = appendFile;
exports.rotateFile = rotateFile;
exports.loadEnvFromFile = loadEnvFromFile;
exports.loadEnvFromArgs = loadEnvFromArgs;

exports.CdoCharset = PipeIPC.CdoCharset;

exports.VERSIONINFO = "File IO Library (file.js) version 0.2.14";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
