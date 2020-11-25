//////////////////////////////////////////////////////////////////////////////////
//
//	file.js
//
//	Common routines.  Defines LIB object which contains the API, as well as
//	a global console.log function.
//
/////////////////////////////////////////////////////////////////////////////////

var LIB = require("lib/std");

/////////////////////////////////////////////////////////////////////////////////
// readFile
//	Read the conents of the pass filename and return as a string
/////////////////////////////////////////////////////////////////////////////////

var readFile = function(FN, charset) {
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
        try {
            var fsT = CreateObject("ADODB.Stream");
            var T = null;
            fsT.CharSet = charset;
            fsT.Open();
            fsT.LoadFromFile(FN);
            T = fsT.ReadText();
            fsT = null;
            return T;
        } catch (e) {
            console.error("readFile -> ", e.message);
        }
    }
};

/////////////////////////////////////////////////////////////////////////////////
// writeFile
//	Write the passed content to named disk file
/////////////////////////////////////////////////////////////////////////////////

var writeFile = function(FN, content, charset) {
    var Stream_No_UTF8_BOM = function(objStream) {
        var _objStream = CreateObject("ADODB.Stream");
        _objStream.Type = 1;
        _objStream.Mode = 3;
        _objStream.Open();
        objStream.Position = 3;
        objStream.CopyTo(_objStream);
        objStream.Flush();
        objStream.Close();
        return _objStream;
    };
    var ok;

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
    return ok;
};

var FileObject = function() {
    this.interfaces = null;
    this.interface = null;
    this.filename = null;
    this.charset = "utf-8";
    this.isExists = false;
    this.isFile = false;
    this.isDirectory = false;

    this.setInterface = function(interfaceName) {
        this.interface = this.interfaces[interfaceName];
        return this;
    };

    this.setCharset = function(charset) {
        this.charset = charset;
        return this;
    };

    this.create = function() {
        this.interfaces = {
            fso: CreateObject("Scripting.FileSystemObject"),
            ado: CreateObject("ADODB.Stream")
        };
        this.setInterface("fso");
        return this;
    };

    this.exists = function() {
        try {
            if (this.interface.FileExists(this.filename)) {
                this.isExists = true;
                this.isFile = true;
            } else if (this.interface.folderExists(this.filename)) {
                this.isExists = true;
                this.isDirectory = true;
            }
        } catch (e) {
            console.error("FileObject.exists() -> ", e.message);
        }
        return this.isExists;
    };

    this.open = function(filename) {
        this.filename = filename;
        if (!this.exists()) {
            console.warn("FileObject.open() -> The file or directory does not exists: " + this.filename);
        }
        return this;
    };

    this.getDetails = function() {
        try {
            if (this.isFile) {
                return this.interface.GetFile(this.filename);
            } else if (this.isDirectory) {
                return this.interface.GetFolder(this.filename);
            }
        } catch (e) {
            console.error("FileObject.getDetails() -> ", e.message);
        }
    };

    this.remove = function() {
        try {
            if (this.isFile) {
                return this.interface.DeleteFile(this.filename);
            } else {
                return this.interface.DeleteFolder(this.filename);
            }
        } catch (e) {
            console.error("FileObject.remove() -> ", e.message);
        }
    };

    this.moveTo = function(dst) {
        try {
            if (this.isFile) {
                return this.interface.MoveFile(this.filename, dst);
            } else {
                return this.interface.MoveFolder(this.filename, dst);
            }
        } catch (e) {
            console.error("FileObject.moveTo() -> ", e.message);
        }
    };

    this.read = function() {
        try {
            if (this.isFile) {
                return readFile(this.filename, this.charset);
            }
        } catch (e) {
            console.error("FileObject.read() -> ", e.message);
        }
    };

    this.write = function(content) {
        try {
            if (this.isFile) {
                return writeFile(this.filename, content, this.charset);
            }
        } catch (e) {
            console.error("FileObject.write() -> ", e.message);
        }
    };
    
    this.chown = function(username) {
        try {
            if (this.isFile) {
                //require("lib/shell").run(["cacls", this.filename, "/G", user + ":F"]);
                require("lib/shell").run([
                    "icacls",
                    "/C",
                    "/q",
                    this.filename,
                    "/grant",
                    username + ":(F)"
                ]);
            } else if (this.isDirectory) {
                //require("lib/shell").run(["cacls", this.filename, "/G", user + ":F"]);
                require("lib/shell").run([
                    "icacls",
                    "/C",
                    "/t",
                    "/q",
                    this.filename,
                    "/grant",
                    username + ":(F)"
                ]);
            }
        } catch (e) {
            console.error("FileObject.chown() -> ", e.message);
        }
    };

    this.mkdir = function() {
        try {
            if (this.isDirectory) {
                return this.interface.CreateFolder(this.filename);
            }
        } catch (e) {
            console.error("FileObject.mkdir() -> ", e.message);
        }
    };

    this.close = function() {
        this.interfaces.fso = null;
        this.interfaces.ado = null;
        this.interface = null;
    };

    this.create();
};

exports.getFile = function(FN) {
    return (new FileObject()).getDetails();
};

exports.fileExists = function(FN) {
    return (new FileObject()).open(FN).exists();
};

exports.folderExists = function(FN) {
    return (new FileObject()).open(FN).exists();
};

exports.moveFile = function(FROM, TO) {
    return (new FileObject()).open(FROM).moveTo(TO);
};

exports.createFolder = function(FN) {
    return (new FileObject()).open(FN).mkdir();
};

exports.deleteFile = function(FN) {
    return (new FileObject()).open(FN).remove();
};

exports.readFile = function(FN, charset) {
    return (new FileObject()).setCharset(charset).open(FN).read();
};

exports.writeFile = function(FN, content, charset) {
    return (new FileObject()).setCharset(charset).open(FN).write(content);
};

exports.VERSIONINFO = "File interface (file.js) version 0.2";
exports.global = global;
exports.require = global.require;
