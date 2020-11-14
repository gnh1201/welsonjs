////////////////////////////////////////////////////////////////////////
// WMI(Windows Management Instrumentation) API
////////////////////////////////////////////////////////////////////////

var WMIClassObject = function() {
    this.interface = (new WMIQueryObject()).interface;
    this.class = null;
    this.className = "";
    this.method = null;
    this.methodName = "";
    this.inParams = {};
    this.outParams = {};

    this.setClass = function(className) {
        this.className = className;
        this.class = this.interface.Get(className);
        return this;
    };
    this.create = function() {
        var si = this.class.SpawnInstance_();
        return si;
    };
    this.setMethod = function(methodName) {
        this.methodName = methodName;
        this.method = this.class.Methods_.Item(methodName);
        return this;
    };
    this.setParameter = function(key, value) {
        this.inParams[key] = value;
        return this;
    };
    this.setParameters = function(params) {
        if (typeof(params) !== "undefined") {
            for (k in params) {
                this.setParameter(k, params[k]);
            }
        }
        return this;
    };
    this.execute = function() {
        var params = this.method.InParameters.SpawnInstance_();
        for (k in this.parameters) {
            params[k] = this.inParams[k];
        }
        this.outParams = this.class.ExecMethod_(this.methodName, params);
        return this;
    };
    this.get = function(key) {
        if (key in this.outParams) {
            return this.outParams[key];
        } else {
            return "";
        }
    };
};

var WMIQueryObject = function() {
    var wbemFlagReturnImmediately = 0x10;
    var wbemFlagForwardOnly = 0x20;

    this.computer = ".";
    this.namespace = "root\\cimv2";
    this.interface = null;
    this.cursor = {};
    this.current = {};

    this.create = function() {
        try {
            if (typeof(GetObject) === "function") {
                this.interface = GetObject("winmgmts:{impersonationLevel=impersonate}!\\\\" + this.computer + "\\" + this.namespace);
            } else {
                var objLocator = CreateObject("WbemScripting.SWbemLocator");
                this.interface = objLocator.ConnectServer(this.computer, this.namespace);
            }
        } catch (e) {
            console.error(e.message);
        }
        return this;
    };
    this.setComputer = function(computer) {
        this.computer = computer;
        return this;
    };
    this.setNamespace = function(namespace) {
        this.namespace = namespace;
        return this;
    };
    this.execQuery = function(query) {
        try {
            var result = this.interface.ExecQuery(query, "WQL", wbemFlagReturnImmediately | wbemFlagForwardOnly);
            this.cursor = new Enumerator(result);
        } catch (e) {
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
    return (new WMIQueryObject()).execQuery(query);
};

exports.setClass = function(className) {
    return (new WMIClassObject()).setClass(className);
};

exports.VERSIONINFO = "WMI interface (wmi.js) version 0.1";
exports.global = global;
exports.require = global.require;