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

    this.createWriter = function(iomode) {
        //this.writer = CreateObject("Scripting.FileSystemObject").CreateTextFile(this.path, true, true);
        this.writer = CreateObject("Scripting.FileSystemObject").OpenTextFile(this.path, iomode, true, TristateTrue);
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

    this._write = function(message) {
        this.writer.Write(message);
    };

    this.write = function(message) {
        var isWritten = false;

        while (!isWritten) {
            try {
                this.flush();
                this.createWriter(ForAppending);
                this._write(message);
                this.writer.Close();
                isWritten = true;
            } catch (e) {
                isWritten = false;
            }
        }
    };
    
    this.flush = function() {
        var isFlushed = false;

        while (!isFlushed) {
            try {
                this.createWriter(ForWriting);
                this._write("");
                this.writer.Close();
                isFlushed = true;
            } catch (e) {
                isFlushed = false;
            }
        }
    };

    this._read = function() {
        return this.reader.ReadAll();
    };

    this.read = function() {
        var isRead = false;
        var text = "";

        while (!isRead) {
            try {
                text += this._read();
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

exports.ForReading = ForReading;
exports.ForWriting = ForWriting;
exports.ForAppending = ForAppending;

exports.VERSIONINFO = "PIPE-based IPC Module (pipe-ipc.js) version 0.1.3";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
