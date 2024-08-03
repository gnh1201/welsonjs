// har.js
// HAR(HTTP Archive) manipulate and replay tools
// https://github.com/gnh1201/welsonjs
var PipeIPC = require("lib/pipe-ipc");
var FILE = require("lib/file");
var HTTP = require("lib/http");

var HARObject = function() {
    this.filename = null;
    this.data = {};
    this.entryIndex = 0;
    this.onEntry = null;
    this.filters = [];
    this.isSimulated = true;

    this.setEntryIndex = function(index) {
        this.entryIndex = index;
    };

    this.setIsSimulated = function(isSimulated) {
        this.isSimulated = isSimulated;
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
    
    this.send = function(entry, done) {
        if (typeof entry === "undefined" || entry == null)
            return;

        var callback = null;
        if (!this.isSimulated) {
            callback = function() {
                console.log("callback");
            };
        }

        try {
            done(this, entry, entry.request, entry.response, callback);
        } catch (e) {
            console.error(e.message);
        }
    };

    this.play = function(done, edit, test) {
        var entries = this.data.log.entries;
        
        if (!this.isSimulated) {
            console.warn("Take care! This is not a simulated mode.");
        }

        while (this.entryIndex < entries.length) {
            var entry = entries[this.entryIndex];

            if (this.test(entry, test)) {
                if (typeof edit !== "function") {
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
                        entry = edit(this, entry, entry.request, entry.response, this.entryIndex);
                    } catch (e) {
                        console.error(e.message);
                    }
                }
            }

            this.send(entry, done);

            this.entryIndex++;
        }
        
        return this;
    };

    this.rewind = function() {
        this.setEntryIndex(0);

        return this;
    };
};

exports.HARObject = HARObject;

exports.VERSIONINFO = "HAR(HTTP Archive) manipulate and replay tools version 0.1.8";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
