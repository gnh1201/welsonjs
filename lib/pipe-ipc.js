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
var CdoCharset = {};
CdoCharset.CdoBIG5        = "big5";
CdoCharset.CdoEUC_JP      = "euc-jp";
CdoCharset.CdoEUC_KR      = "euc-kr";
CdoCharset.CdoGB2312      = "gb2312";
CdoCharset.CdoISO_2022_JP = "iso-2022-jp";
CdoCharset.CdoISO_2022_KR = "iso-2022-kr";
CdoCharset.CdoISO_8859_1  = "iso-8859-1";
CdoCharset.CdoISO_8859_2  = "iso-8859-2";
CdoCharset.CdoISO_8859_3  = "iso-8859-3";
CdoCharset.CdoISO_8859_4  = "iso-8859-4";
CdoCharset.CdoISO_8859_5  = "iso-8859-5";
CdoCharset.CdoISO_8859_6  = "iso-8859-6";
CdoCharset.CdoISO_8859_7  = "iso-8859-7";
CdoCharset.CdoISO_8859_8  = "iso-8859-8";
CdoCharset.CdoISO_8859_9  = "iso-8859-9";
CdoCharset.cdoKOI8_R      = "koi8-r";
CdoCharset.cdoShift_JIS   = "shift-jis";
CdoCharset.CdoUS_ASCII    = "us-ascii";
CdoCharset.CdoUTF_7       = "utf-7";
CdoCharset.CdoUTF_8       = "utf-8";

var randomize = Math.random;

function UUIDv4() {}
UUIDv4.create = function() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = randomize() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
};

function CRC32() {
    this.value = 0;

    this.fromString = function(s) {
        var crc = 0 ^ (-1);
        for (var i = 0; i < s.length; i++ ) {
            crc = (crc >>> 8) ^ CRC32.table[(crc ^ s.charCodeAt(i)) & 0xFF];
        }
        this.value = ((crc ^ (-1)) >>> 0);
    };

    this.toString = function() {
        return this.value.toString(16).padStart(8, '0');
    };
}
CRC32.table = [];
(function() {
    for (var n = 0; n < 256; n++) {
        c = n;
        for(var k =0; k < 8; k++){
            c = ((c&1) ? (0xEDB88320 ^ (c >>> 1)) : (c >>> 1));
        }
        CRC32.table[n] = c;
    }
})();

function makeProbabilityBit(p) {
    return !( p > 0.0 ? ( (randomize() / p) > 1.0 ) : true) ? 1 : 0;
}

function createFSO() {
    return CreateObject("Scripting.FileSystemObject");
}

function createADO() {
    return CreateObject("ADODB.Stream");
}

function openTextFile(filename, iomode) {
    return createFSO().OpenTextFile(filename, iomode, true, TristateTrue);
}

function checkFileExists(filename) {
    return createFSO().FileExists(filename);;
}

function deleteFile(filename) {
    if (checkFileExists(filename)) createFSO().DeleteFile(filename);
}

function getFileSize(filename) {
    try {
        return createFSO().GetFile(filename).Size;
    } catch (e) {
        return -1; 
    }
}

function AdoConvertToStream(ado, position) {
    position = (position !== "undefined" ? position : 0);

    var newAdo = createADO();
    newAdo.Type = adTypeBinary;
    newAdo.Mode = adModeReadWrite;
    newAdo.Open();
    ado.Position = position;
    ado.CopyTo(newAdo);
    ado.Flush();
    ado.Close();
    return newAdo;
}

function PipeIPC() {
    this.path = "data\\.pipe_:pipename";
    this.delimiter = "\r\n";
    this.reader = null;
    this.writer = null;
    this.recorder = null;
    this.savefile = null;
    this.tmpfile = null;
    this.charset = CdoCharset.CdoUTF_8;
    this.lastReadTime = -1;
    this.lastWriteTime = -1;
    this.maxSentences = 0;
    this.recorder_iomode = ForAppending;

    this.getCurrentTime = function() {
        return new Date().getTime();
    };

    this.setCharset = function(charset) {
        charset = charset.toLowerCase();

        if (Object.values(CdoCharset).indexOf(charset) < 0) {
            console.warn(charset.toUpperCase() + " may not be an encoding supported by the system.");
        }
        this.charset = charset;
    };
    
    this.setDelimiter = function(delimiter) {
        this.delimiter = delimiter;
    };

    this.setMaxSentences = function(maxSentences) {
        this.maxSentences = maxSentences;
    };

    this.waitForRetry = function() {
        var t = makeProbabilityBit(0.5);
        if (t > 0) sleep(t);
    };

    this.connect = function(pipename, callback) {
        if (pipename == "volatile") {
            pipename = UUIDv4.create().substring(0, 8);
        }
        this.path = this.path.replace(":pipename", pipename);
        //this.openWriter();
        //this.openReader();
        //console.info("Opened pipe:", pipename);
        if (typeof callback === "function") {
            callback(this, this.reader, this.writer);
        }
        return this;
    };

    this.openWriter = function(iomode) {
        while (this.writer == null) {
            try {
                this.writer = openTextFile(this.path, iomode);
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
                this.reader = openTextFile(this.path, ForReading);
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
        iomode = (iomode !== "undefined" ? ForAppending : iomode);  // default: ForAppending

        this.savefile = filename;
        this.tmpfile = this.savefile + ".tmp";
        this.recorder_iomode = iomode;

        // read a text from save file
        if (this.recorder_iomode == ForAppending) {
            var isExistsSaveFile = checkFileExists(this.savefile);
            var isExistsTmpFile = checkFileExists(this.tmpfile);
            while (isExistsSaveFile && !isExistsTmpFile) {
                try {
                    var fso = openTextFile(this.tmpfile, ForWriting);
                    fso.Write(this.readTextFromFile(this.savefile));
                    fso.Close();
                    isExistsTmpFile = checkFileExists(this.tmpfile);
                } catch (e) {
                    isExistsTmpFile = false;
                    this.waitForRetry();
                }
            }
        }

        // open a recorder
        this.openRecorder(iomode);
    };

    this.stopRecorder = function() {
        this.savefile = null;
        this.tmpfile = null;
    };

    this.openRecorder = function(iomode) {
        while (this.recorder == null) {
            try {
                this.recorder = openTextFile(this.tmpfile, iomode);
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
        this.writer.Write(message);
    };

    this.write = function(message, iomode) {
        iomode = (iomode !== "undefined" ? ForAppending : iomode);  // default: ForAppending

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
                //console.log(e.message);
                this.closeWriter();
                isWritten = false;
                this.waitForRetry();
            }
        }
        
        // record the last message
        if (isWritten && this.savefile != null) {
            this.record(message);
        }

        return isWritten;
    };

    this._record = function(message) {
        if (this.recorder_iomode == ForAppending) {
            this.recorder.Write(message);
        } else if (this.recorder_iomode == ForWriting) {
            this.recorder.Write(message + this.delimiter + this.readTextFromFile(this.savefile));
        }
    };

    this.record = function(message) {
        var isRecorded = false;
        while (!isRecorded) {
            try {
                this.openRecorder(this.recorder_iomode);
                this._record(message);
                this.closeRecorder();
                this.commit(this.savefile);
                isRecorded = true;
                this.closeRecorder();
            } catch (e) {
                //console.log(e.message);
                this.closeRecorder();
                isRecorded = false;
                this.waitForRetry();
            }
        }

        return isRecorded;
    };

    this.commit = function(dst) {
        var src = this.tmpfile;
        var charset = this.charset;
        var isCommited = false;

        while (!isCommited) {
            try {
                // Open a temporary file
                var fso = openTextFile(src, ForReading);
                var text = this._read(fso);
                fso.Close();  // close the file immediately
                var sentences = text.split(this.delimiter);
                var newSentences = [], str = '';

                // if enabled "maxSentences" feature
                if (text.length > 0 && this.maxSentences > 0) {
                    while (sentences.length > 0 && newSentences.length < this.maxSentences) {
                        newSentences.push(sentences.pop());
                    }
                    str = newSentences.reverse().join(this.delimiter);

                    // Write a new temporary file
                    var done = false;
                    while (!done) {
                        try {
                            var _fso = openTextFile(src, ForWriting);
                            _fso.Write(str);
                            _fso.Close();
                            done = true;
                        } catch (e) {
                            done = false;
                            this.waitForRetry();
                        }
                    }
                } else {
                    str = text;
                }
                
                // Convert UTF-16 BOM to a character set
                var ado = createADO();
                ado.Type = adTypeText;
                ado.Charset = charset;
                ado.Open();
                ado.WriteText(str);
                ado = AdoConvertToStream(ado, 3);
                ado.SaveToFile(dst, adSaveCreateOverWrite);
                ado.Close();

                // Set a result
                isCommited = true;
            } catch (e) {
                //console.log(e.message);
                isCommited = false;
                this.waitForRetry();
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
                this.closeWriter();
                isFlushed = false;
                this.waitForRetry();
            }
        }

        return isFlushed;
    };

    // Fixed bug: broken GUI sample #86, Reported by @eogloblin, in 2023-10-26
    // Reference #1: https://gist.github.com/antic183/619f42b559b78028d1fe9e7ae8a1352d
    this._read = function(fh) {
        try {
            return (function(s) {
                return (s.length > 1 && s.charCodeAt(0) === 0xFEFF) ? s.substring(1) : s;
            })(fh.ReadAll());
        } catch (e) {
            //console.log(e.message);
            return '';
        }
    };

    this.read = function() {
        var isRead = false;
        var text = '';

        while (!isRead) {
            try {
                this.openReader();
                text += this._read(this.reader);
                isRead = true;
                this.lastReadTime = this.getCurrentTime();
                this.closeReader();
            } catch (e) {
                this.closeReader();
                this.waitForRetry();
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
        deleteFile(this.path);
    };

    this.readTextFromFile = function(filename, charset) {
        charset = (typeof charset !== "undefined" ? charset : this.charset);

        var text = '';

        var isFileExists = checkFileExists(filename);
        if (isFileExists) {
            var isRead = false;
            while (!isRead) {
                try {
                    var ado = createADO();
                    ado.Charset = charset;
                    ado.Open();
                    ado.LoadFromFile(filename);
                    text += ado.ReadText();
                    ado.Close();
                    isRead = true;
                } catch (e) {
                    //console.log(e.message);
                    isRead = false;
                }
            }
        } else {
            console.warn("File", filename, "not exists");
        }

        return text;
    };

    this.loadFromFile = function(filename, charset) {
        charset = (typeof charset !== "undefined" ? charset : this.charset);

        this.write(this.readTextFromFile(filename, charset), ForWriting);
    };

    this.reload = function(charset) {
        charset = (typeof charset !== "undefined" ? charset : this.charset);

        this.loadFromFile(this.path, charset);
    };
}

exports.create = function() {
    return new PipeIPC();
};

exports.connect = function(path, callback) {
    var pipe = exports.create();
    return pipe.connect(path, callback);
};

exports.UUIDv4 = UUIDv4;
exports.CRC32 = CRC32;

exports.ForReading = ForReading;
exports.ForWriting = ForWriting;
exports.ForAppending = ForAppending;
exports.CdoCharset = CdoCharset;
exports.CdoUTF_8 = CdoCharset.CdoUTF_8;
exports.CdoUS_ASCII = CdoCharset.CdoUS_ASCII;
exports.CdoEUC_KR = CdoCharset.CdoEUC_KR;
exports.CdoEUC_JP = CdoCharset.CdoEUC_JP;
exports.adTypeBinary = adTypeBinary;
exports.adTypeText = adTypeText;
exports.adSaveCreateNotExist = adSaveCreateNotExist;
exports.adSaveCreateOverWrite = adSaveCreateOverWrite;
exports.adModeReadWrite = adModeReadWrite;

exports.VERSIONINFO = "PIPE-based IPC Module (pipe-ipc.js) version 0.1.22";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
