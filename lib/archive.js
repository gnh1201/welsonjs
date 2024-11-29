// archive.js
// File archiver library for WelsonJS framework
// Namhyeon Go (Catswords Research) <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
var SHELL = require("lib/shell");
var FILE = require("lib/file");

function ArchiveObject(engine) {
    this.workingDirectory = "";
    this.patterns = [];
    this.readFormat = "";
    this.writeFormat = "";
    this.engine = null;
    
    this.setReadFormat = function(readFormat) {
        this.readFormat = readFormat;
        
        return this;
    };
    
    this.setWriteFormat = function(writeFormat) {
        this.writeFormat = writeFormat;
        
        return this;
    };
    
    this.setEngine = function(engine) {
        this.engine = engine;
        
        if (this.engine == "cabinet") {
            this.setReadFormat("cab");
            this.setWriteFormat("cab");
        }
        
        // TODO: zip, 7z, rar
        
        return this;
    };
    
    this.extractTo = function(filename, targetDirectory) {
        var result;
        
        if (this.engine == "cabinet") {
            // compress cab to target directory
            result = SHELL.exec([
                "expand",
                filename,
                targetDirectory
            ]);
        }
        
        return result;
    };
    
    this.compressTo = function(filename) {
        var result;
        
        // https://superuser.com/questions/658021/whats-the-best-way-to-create-a-cab-file-of-multiple-files-quickly
        if (this.engine == "cabinet") {
            var directives = this.buildDirectives();
            
            // create the directives file (temporary file)
            var tmp = PipeIPC.connect("volatile");
            tmp.write(directives);
            var tmp_path = out.path;
            tmp.close();
            
            // compress to cab
            SHELL.exec([
                "makecab",
                "/d",
                "CabinetName1=" + filename,
                "/d",
                "DiskDirectoryTemplate=" + this.workingDirectory,
                "/f",
                tmp_path
            ]);
            
            // destroy the temporary file
            tmp.destroy();
        }
        
        return result;
    };
    
    this.buildDirectives = function() {
        var directives = [];
        
        if (this.engine == "cabinet") {
            this.patterns.forEach(function(x) {
                if (FILE.fileExists(x)) directives.push(x);
            });
        }
        
        return directives.join("\r\n");
    };
    
    this.addFile = function(file) {
        this.patterns.push(file);
        
        return this;
    };
    
    // set default engines
    this.create = function() {
        if (typeof engine === "undefined") {
            this.setEngine("cabinet");
        };
        
        return this;
    };
    
    this.create();
}

exports.create = function(engine) {
    return new ArchiveObject(engine);
};

exports.VERSIONINFO = "File archiver library (archive.js) version 0.1-dev";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
