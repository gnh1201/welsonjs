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

function createUUIDv4() {
    var randomize = Math.random;
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = randomize() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function createFSO() {
    return CreateObject("Scripting.FileSystemObject");
}

function createADO() {
    return CreateObject("ADODB.Stream");
}

function PipeIPC() {
    this.path = "data\\.pipe_:pipename";
    this.delimiter = "\r\n";
    this.reader = null;
    this.writer = null;
    this.recorder = null;
    this.savefile = null;
    this.tmpfile = null;
    this.charset = CdoUTF_8;
    this.lastReadTime = -1;
    this.lastWriteTime = -1;
    this.maxSentences = 0;

    this.getCurrentTime = function() {
        return new Date().getTime();
    };
    

    this.setCharset = function(charset) {
        this.charset = charset;
    };
    
    this.setDelimiter = function(delimiter) {
        this.delimiter = delimiter;
    };

    this.setMaxSentences = function(maxSentences) {
        this.maxSentences = maxSentences;
    };

    this.connect = function(pipename, callback) {
        if (pipename == "volatile") {
            pipename = createUUIDv4().substring(0, 8);
        }
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
                this.writer = createFSO().OpenTextFile(this.path, iomode, true, TristateTrue);
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
                this.reader = createFSO().OpenTextFile(this.path, ForReading, true, TristateTrue);
            } catch (e) {}
        }
    };

    this.closeReader = function() {
        if (this.reader != null) {
            this.reader.Close();
            this.reader = null;
        }
    };

    this.startRecorder = function(filename, iomode) {
        this.savefile = filename;
        this.tmpfile = this.savefile + ".tmp";

        // read a text from save file
        var isExistsTmpFile = createFSO().FileExists(this.tmpfile);
        if (!isExistsTmpFile) {
            var handler = createFSO().OpenTextFile(this.tmpfile, ForWriting, true, TristateTrue);
            handler.Write(this.readTextFromFile(this.savefile));
            handler.Close();
        }

        // with iomode (default: ForAppending)
        var iomode = (iomode !== "undefined" ? ForAppending : iomode);
        this.openRecorder(iomode);
    };

    this.stopRecorder = function() {
        this.savefile = null;
        this.tmpfile = null;
    };

    this.openRecorder = function(iomode) {
        while (this.recorder == null) {
            try {
                this.recorder = createFSO().OpenTextFile(this.tmpfile, iomode, true, TristateTrue);
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
        this.writer.Write(message + this.delimiter);
    };

    this.write = function(message, iomode) {
        // with iomode (default: ForAppending)
        var iomode = (iomode !== "undefined" ? ForAppending : iomode);

        if (this.savefile != null) {
            this.record(message);
        }

        var isWritten = false;
        while (!isWritten) {
            try {
                this.flush();
                this.openWriter(iomode);
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
        var isRecorded = false;
        while (!isRecorded) {
            try {
                this.openRecorder();
                this._record(message);
                this.closeRecorder();
                this.commit(this.savefile);
                isRecorded = true;
            } catch (e) {
                //console.log(e.message);
                this.closeRecorder();
                isRecorded = false;
            }
        }

        return isRecorded;
    };

    this.commit = function(dst) {
        var src = this.tmpfile;
        var charset = this.charset;
        var isCommited = false;

        // define functions
        var StripAdoBOM = function(adoObj) {
            var newAdoObj = createADO();
            newAdoObj.Type = adTypeBinary;
            newAdoObj.Mode = adModeReadWrite;
            newAdoObj.Open();
            adoObj.Position = 3;
            adoObj.CopyTo(newAdoObj);
            adoObj.Flush();
            adoObj.Close();
            return newAdoObj;
        };

        while (!isCommited) {
            try {
                // Open a temporary file
                var fso = createFSO().OpenTextFile(src, ForReading, true, TristateTrue);
                var text = fso.ReadAll();
                var sentences = text.split(this.delimiter);
                var newSentences = [], str = '';

                // if enabled "maxSentences" feature
                if (this.maxSentences > 0) {
                    while (sentences.length > 0 && newSentences.length < this.maxSentences) {
                        newSentences.push(sentences.pop());
                    }
                    str = newSentences.reverse().join(this.delimiter);
                } else {
                    str = text;
                }

                // Convert UTF-16 BOM to UTF-8
                var ado = createADO();
                ado.Type = adTypeText;
                ado.Charset = charset;
                ado.Open();
                ado.WriteText(str);
                ado = StripAdoBOM(ado);
                ado.SaveToFile(dst, adSaveCreateOverWrite);
                ado.Close();

                // Write a new temporary file
                if (this.maxSentences > 0) {
                    var handler = createFSO().OpenTextFile(src, ForWriting, true, TristateTrue);
                    handler.Write(str);
                    handler.Close();
                }

                // Close the temporary file
                fso.Close(); 

                // Set a result
                isCommited = true;
            } catch (e) {
                isCommited = false;
            }
        }
        
        return isCommited;
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

    this.destroy = function() {
        this.close();
        createFSO().DeleteFile(this.path);
        
    };

    this.readTextFromFile = function(filename, charset) {
        var text = '';

        var isFileExists = createFSO().FileExists(filename);
        if (isFileExists) {
            var isRead = false;
            while (!isRead) {
                try {
                    var ado = createADO();
                    ado.CharSet = charset;
                    ado.Open();
                    ado.LoadFromFile(filename);
                    text += ado.ReadText();
                    ado.Close();
                    isRead = true;
                } catch (e) {
                    isRead = false;
                }
            }
        } else {
            console.warn("File", filename, "not exists");
        }

        return text;
    };
    

    this.loadFromFile = function(filename, charset) {
        this.write(this.readTextFromFile(filename, charset), ForWriting);
    };
}

exports.create = function() {
    return new PipeIPC();
};

exports.connect = function(path, callback) {
    var pipe = exports.create();
    return pipe.connect(path, callback);
};

exports.createUUIDv4 = createUUIDv4;

exports.ForReading = ForReading;
exports.ForWriting = ForWriting;
exports.ForAppending = ForAppending;
exports.CdoUTF_8 = CdoUTF_8;
exports.CdoUS_ASCII = CdoUS_ASCII;
exports.CdoEUC_KR = CdoEUC_KR;
exports.CdoEUC_JP = CdoEUC_JP;
exports.adTypeBinary = adTypeBinary;
exports.adTypeText = adTypeText;
exports.adSaveCreateNotExist = adSaveCreateNotExist;
exports.adSaveCreateOverWrite = adSaveCreateOverWrite;
exports.adModeReadWrite = adModeReadWrite;

exports.VERSIONINFO = "PIPE-based IPC Module (pipe-ipc.js) version 0.1.9";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
