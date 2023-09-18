// pipe-ipc.js
// https://github.com/gnh1201/welsonjs

var STD = require("lib/std");

// https://learn.microsoft.com/en-us/office/vba/language/reference/user-interface-help/opentextfile-method
var ForReading = 1;
var ForWriting = 2;
var ForAppending = 8;
var TristateUseDefault = -2;
var TristateTrue = -1;
var TristateFalse = 0;

// https://learn.microsoft.com/en-us/office/client-developer/access/desktop-database-reference/streamtypeenum
var adTypeBinary = 1;
var adTypeText = 2;

// https://learn.microsoft.com/ko-kr/sql/ado/reference/ado-api/saveoptionsenum?view=sql-server-ver16
var adSaveCreateNotExist = 1;
var adSaveCreateOverWrite = 2;

// https://learn.microsoft.com/en-us/sql/ado/reference/ado-api/connectmodeenum?view=sql-server-ver16
var adModeRead = 1;
var adModeReadWrite = 3;
var adModeRecursive = 0x400000;
var adModeShareDenyNone = 16;
var adModeShareDenyRead = 4;
var adModeShareDenyWrite = 8;
var adModeShareExclusive = 12;
var adModeUnknown = 0;
var adModeWrite = 2;

// https://learn.microsoft.com/en-us/previous-versions/exchange-server/exchange-10/ms527267(v=exchg.10)
// https://learn.microsoft.com/en-us/previous-versions/exchange-server/exchange-10/ms526296(v=exchg.10)
var CdoBIG5        = "big5";
var CdoEUC_JP      = "euc-jp";
var CdoEUC_KR      = "euc-kr";
var CdoGB2312      = "gb2312";
var CdoISO_2022_JP = "iso-2022-jp";
var CdoISO_2022_KR = "iso-2022-kr";
var CdoISO_8859_1  = "iso-8859-1";
var CdoISO_8859_2  = "iso-8859-2";
var CdoISO_8859_3  = "iso-8859-3";
var CdoISO_8859_4  = "iso-8859-4";
var CdoISO_8859_5  = "iso-8859-5";
var CdoISO_8859_6  = "iso-8859-6";
var CdoISO_8859_7  = "iso-8859-7";
var CdoISO_8859_8  = "iso-8859-8";
var CdoISO_8859_9  = "iso-8859-9";
var cdoKOI8_R      = "koi8-r";
var cdoShift_JIS   = "shift-jis";
var CdoUS_ASCII    = "us-ascii";
var CdoUTF_7       = "utf-7";
var CdoUTF_8       = "utf-8";

function PipeIPC() {
    var EOL = "\r\n";

    this.path = "data\\.pipe_:pipename";
    this.reader = null;
    this.writer = null;
    this.recorder = null;
    this.savefile = null;
    this.tmpfile = null;
    this.charset = CdoUTF_8;
    this.lastReadTime = -1;
    this.lastWriteTime = -1;

    this.getCurrentTime = function() {
        return new Date().getTime();
    };

    this.setCharset = function(charset) {
        this.charset = charset;
    };

    this.connect = function(pipename, callback) {
        this.path = this.path.replace(":pipename", pipename);
        //this.openWriter();
        this.openReader();
        if (typeof callback === "function") {
            callback(this, this.reader, this.writer);
        }
        return this;
    };

    this.openWriter = function(iomode) {
        while (this.writer == null) {
            try {
                this.writer = CreateObject("Scripting.FileSystemObject").OpenTextFile(this.path, iomode, true, TristateTrue);
            } catch (e) {}
        }
    };

    this.closeWriter = function() {
        if (this.writer != null) {
            this.writer.Close();
            this.writer = null;
        }
    };

    this.openReader = function() {
        while (this.reader == null) {
            try {
                this.reader = CreateObject("Scripting.FileSystemObject").OpenTextFile(this.path, ForReading, true, TristateTrue);
            } catch (e) {}
        }
    };

    this.closeReader = function() {
        if (this.reader != null) {
            this.reader.Close();
            this.reader = null;
        }
    };

    this.startRecorder = function(filename) {
        this.savefile = filename;
        this.tmpfile = this.savefile + ".tmp";
        this.openRecorder();
    };

    this.stopRecorder = function() {
        this.savefile = null;
        this.tmpfile = null;
    };

    this.openRecorder = function() {
        while (this.recorder == null) {
            try {
                this.recorder = CreateObject("Scripting.FileSystemObject").OpenTextFile(this.tmpfile, ForAppending, true, TristateTrue);
            } catch (e) {}
        }
    };

    this.closeRecorder = function() {
        if (this.recorder != null) {
            this.recorder.Close();
            this.recorder = null;
        }
    };

    this._write = function(message) {
        this.writer.Write(message + EOL);
    };

    this.write = function(message) {
        if (this.savefile != null) {
            this.record(message);
        }

        var isWritten = false;
        while (!isWritten) {
            try {
                this.flush();
                this.openWriter(ForAppending);
                this._write(message);
                this.closeWriter();
                isWritten = true;
                this.lastWriteTime = this.getCurrentTime();
            } catch (e) {
                this.closeWriter();
                isWritten = false;
            }
        }

        return isWritten;
    };

    this._record = function(message) {
        this.recorder.Write(message);
    };

    this.record = function(message) {
        var commit = function(src, dst, charset) {
            // Open a temporary file
            var fso = CreateObject("Scripting.FileSystemObject").OpenTextFile(src, ForReading, true, TristateTrue);
            var str = fso.ReadAll();
            var StripBOM = function(adoObj) {
                var newAdoObj = CreateObject("ADODB.Stream");
                newAdoObj.Type = adTypeBinary;
                newAdoObj.Mode = adModeReadWrite;
                newAdoObj.Open();
                adoObj.Position = 3;
                adoObj.CopyTo(newAdoObj);
                adoObj.Flush();
                //adoObj.Close();
                return newAdoObj;
            };

            // Convert UTF-16 BOM to UTF-8
            var ado = CreateObject("ADODB.Stream");
            ado.Type = adTypeText;
            ado.Charset = charset;
            ado.Open();
            ado.WriteText(str);
            StripBOM(ado).SaveToFile(dst, adSaveCreateOverWrite);
            //ado.SaveToFile(savefile, adSaveCreateOverWrite);
            ado.Close();

            // Close a temporary file
            fso.Close();
        };

        var isRecorded = false;
        while (!isRecorded) {
            try {
                this.openRecorder();
                this._record(message);
                this.closeRecorder();
                commit(this.tmpfile, this.savefile, this.charset);
                isRecorded = true;
            } catch (e) {
                //console.log(e.message);
                this.closeRecorder();
                isRecorded = false;
            }
        }

        return isRecorded;
    };

    this.flush = function() {
        var isFlushed = false;

        while (!isFlushed) {
            try {
                this.openWriter(ForWriting);
                this._write('');
                this.closeWriter();
                isFlushed = true;
            } catch (e) {
                isFlushed = false;
            }
        }

        return isFlushed;
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
                this.lastReadTime = this.getCurrentTime();
            } catch (e) {
                this.closeReader();
                this.openReader();
            }
        }

        return text;
    };

    this.close = function() {
        this.closeWriter();
        this.closeReader();
        this.closeRecorder();
    };

}

exports.create = function() {
    return new PipeIPC();
};

exports.connect = function(path, callback) {
    return exports.create().connect(path, callback);
};

exports.ForReading = ForReading;
exports.ForWriting = ForWriting;
exports.ForAppending = ForAppending;

exports.VERSIONINFO = "PIPE-based IPC Module (pipe-ipc.js) version 0.1.4";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
