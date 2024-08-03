// hartool.js
// https://github.com/gnh1201/welsonjs
var PipeIPC = require("lib/pipe-ipc");
var FILE = require("lib/file");

var HARObject = function() {
    this.filename = null;
    this.data = null;
    this.entryIndex = 0;
    this.onEntry = null;

    this.setEntryIndex = function(index) {
        this.entryIndex = index;
    };
    
    this.setOnEntry = function(callback) {
        this.onEntry = callback;
    };

    this.load = function(filename) {
        this.filename = filename;
        this.data = JSON.parse(FILE.readFile(filename, PipeIPC.CdoUTF_8));
        return this;
    };

    this.play = function(onEntry) {
        if (this.data = null)
            return;

        var entries = this.data.entries;
        while (this.entryIndex < entries.length) {
            var entry = this.data.entries[entryIndex];

            if (typeof this.onEntry !== "function") {
                console.log(entry.request.httpVersion, entry.request.method, entry.response.status, entry.request.url);
            } else {
                try {
                    this.onEntry(this, entry, entry.request, entry.response);
                } catch (e) {
                    console.error(e.message);
                }
            }

            this.entryIndex++;
        }
    };

    this.rewind = function() {
        this.entryIndex = 0;
    };
};

exports.HARObject = HARObject;

exports.VERSIONINFO = "HAR manipulate and replay tools version 0.1.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
