////////////////////////////////////////////////////////////////////////
// WMI(Windows Management Instrumentation) API
////////////////////////////////////////////////////////////////////////

var WMIObject = function() {
    var wbemFlagReturnImmediately = 0x10;
    var wbemFlagForwardOnly = 0x20;

    this.interface = null;
    this.cursor = {};
    this.current = {};

    this.create = function() {
        this.interface = GetObject("winmgmts:{impersonationLevel=impersonate}!\\\\.\\root\\cimv2");
        return this;
    };
    this.execQuery = function(query) {
        try {
            var result = this.interface.ExecQuery(query, "WQL", wbemFlagReturnImmediately | wbemFlagForwardOnly);
            this.cursor = new Enumerator(result);
        } catch(e) {
            console.error(e.message);
        }
        return this;
    };
    this.fetch = function() {
        if (!this.cursor.atEnd()) {
            this.current = this.cursor.item();
            this.cursor.moveNext();
        }
        return this;
    };
    this.fetchAll = function() {
        return this.cursor.toArray();
    };
    this.get = function(key) {
        if (key in this.current) {
            return this.current[key];
        } else {
            return "";
        }
    };

    this.create();
};

exports.execQuery = function(query) {
    return (new WMIObject()).execQuery(query);
};

exports.VERSIONINFO = "WMI interface (wmi.js) version 0.1";
exports.global = global;
exports.require = global.require;