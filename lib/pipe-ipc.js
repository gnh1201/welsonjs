// pipe-ipc.js

var STD = require("lib/std");

// https://learn.microsoft.com/en-us/office/vba/language/reference/user-interface-help/opentextfile-method
var ForReading = 1;
var ForWriting = 2;
var ForAppending = 8;

var TristateUseDefault = -2;
var TristateTrue = -1;
var TristateFalse = 0;

function PipeIPC() {
    this.path = "\\.\pipe\:path";
    this.reader = null;
    this.writer = null;

    this.connect = function(path, callback) {
        this.path = this.path.replace(":path", path);
        //this.createWriter();
        this.createReader();
        if (typeof callback === "function") {
            callback(this, this.reader, this.writer);
        }
    };

    this.createWriter = function() {
        //this.writer = CreateObject("Scripting.FileSystemObject").CreateTextFile(this.path, true, true);
        this.writer = CreateObject("Scripting.FileSystemObject").OpenTextFile(this.path, ForWriting, true, TristateTrue);
    };

    this.closeWriter = function() {
        this.writer.Close();
        this.writer = null;
    };

    this.createReader = function() {
        this.reader = CreateObject("Scripting.FileSystemObject").OpenTextFile(this.path, ForReading, true, TristateTrue);
    };

    this.closeReader = function() {
        this.reader.Close();
        this.reader = null;
    };

    this.write = function(message) {
        var isWrote = false;

        while (!isWrote) {
            try {
                this.createWriter();
                this.writer.Write(message);
                sleep(1);
                this.writer.Close();
                isWrote = true;
            } catch (e) {
                isWrote = false;
            }
        }
    };

    this.read = function() {
        var isRead = false;
        var text = "";

        while (!isRead) {
            try {
                text = this.reader.ReadAll();
                isRead = true;
            } catch (e) {
                this.closeReader();
                this.createReader();
            }
        }

        return text;
    };

    this.close = function() {
        this.closeWriter();
        this.closeReader();
    };
}

exports.create = function() {
    return new PipeIPC();
};

exports.VERSIONINFO = "PIPE-based IPC Module (pipe-ipc.js) version 0.1";
exports.AUTHOR = "Nathan Catswords <catswords@protonmail.com>";
exports.global = global;
exports.require = require;
