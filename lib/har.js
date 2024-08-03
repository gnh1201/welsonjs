// har.js
// HAR(HTTP Archive) manipulate and replay tools
// https://github.com/gnh1201/welsonjs
var PipeIPC = require("lib/pipe-ipc");
var FILE = require("lib/file");

var HARObject = function() {
    this.filename = null;
    this.data = {};
    this.entryIndex = 0;
    this.onEntry = null;
    this.filters = [];

    this.setEntryIndex = function(index) {
        this.entryIndex = index;
    };

    this.load = function(filename) {
        this.filename = filename;
        this.data = JSON.parse(FILE.readFile(this.filename, PipeIPC.CdoUTF_8));

        return this;
    };
    
    this.test = function(entry, test) {
        if (typeof test !== "function")
            return true;

        try {
            return test(this, entry, entry.request, entry.response, this.entryIndex);
        } catch (e) {
            console.warn(e.message);
        }

        return true;
    };

    this.play = function(callback, test) {
        var entries = this.data.log.entries;

        while (this.entryIndex < entries.length) {
            var entry = entries[this.entryIndex];

            if (this.test(entry, test)) {
                if (typeof callback !== "function") {
                    console.log(
                        '[' + entry.startedDateTime + ']',
                        '"' + [entry.request.method, entry.request.url, entry.request.httpVersion].join(' ') + '"',
                        entry.request.url,
                        entry.request.httpVersion,
                        entry.response.status,
                        entry.response.content.size
                    );
                } else {
                    try {
                        callback(this, entry, entry.request, entry.response, this.entryIndex);
                    } catch (e) {
                        console.error(e.message);
                    }
                }
            }

            this.entryIndex++;
        }
        
        return this;
    };

    this.rewind = function() {
        this.entryIndex = 0;

        return this;
    };
};

exports.HARObject = HARObject;

exports.VERSIONINFO = "HAR(HTTP Archive) manipulate and replay tools version 0.1.5";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
