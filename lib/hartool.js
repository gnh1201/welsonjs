// hartool.js
// https://github.com/gnh1201/welsonjs
var PipeIPC = require("lib/pipe-ipc");

var HARObject = function() {
    this.filename = null;
    this.data = null;
    this.entryIndex = 0;

    this.setEntryIndex = function(index) {
        this.entryIndex = index;
    };

    this.load = function(filename) {
        this.filename = filename;
        this.data = JSON.parse(FILE.readFile(filename, PipeIPC.CdoUTF_8));
        return this;
    };

    this.walk = function(onEntry) {
        if (this.data = null)
            return;

        var entries = this.data.entries;
        while (this.entryIndex < entries.length) {
            var entry = this.data.entries[entryIndex];

            try {
                onEntry(this, entry, entry.request, entry.response);
            } catch (e) {
                console.error(e.message);
            }

            this.entryIndex++;
        }
    };
};

exports.HARObject = HARObject;

exports.VERSIONINFO = "HAR tool version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = require;
