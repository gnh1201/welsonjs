// wmi.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// WMI(Windows Management Instrumentation) API interface for WelsonJS framework
// 
var WMIQueryObject = function() {
    var wbemFlagReturnImmediately = 0x10;
    var wbemFlagForwardOnly = 0x20;

    this.computer = ".";
    this.namespace = "root\\cimv2";
    this._interface = null;
    this.cursor = {};
    this.current = {};

    this.create = function() {
        try {
            if (typeof(GetObject) === "function") {
                this._interface = GetObject("winmgmts:{impersonationLevel=impersonate}!\\\\" + this.computer + "\\" + this.namespace);
            } else {
                var objLocator = CreateObject("WbemScripting.SWbemLocator");
                this._interface = objLocator.ConnectServer(this.computer, this.namespace);
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
            var result = this._interface.ExecQuery(query, "WQL", wbemFlagReturnImmediately | wbemFlagForwardOnly);
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

var WMIClassObject = function() {
    this._interface = (new WMIQueryObject())._interface;
    this.classObject = null;
    this.className = "";
    this.instance = null;
    this.methodObject = null;
    this.methodName = "";
    this.inParams = {};
    this.outParams = {};

    // Instance
    this.setClass = function(className) {
        this.className = className;
        this.classObject = this._interface.Get(className);
        return this;
    };

    this.create = function() {
        this.instance = this.classObject.SpawnInstance_();
        return this;
    };

    this.getAttribute = function(key) {
        return this.instance[key];
    };

    this.setAttribute = function(key, value) {
        this.instance[key] = value;
    };

    this.getInstance = function() {
        return this.instance;
    };

    // Method
    this.setMethod = function(methodName) {
        this.methodName = methodName;
        this.methodObject = this.classObject.Methods_.Item(methodName);
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
        var params = this.methodObject.InParameters.SpawnInstance_();
        for (k in this.parameters) {
            params[k] = this.inParams[k];
        }
        this.outParams = this.classObject.ExecMethod_(this.methodName, params);
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

function create() {
    return new WMIQueryObject();
}

function execQuery(query) {
    return create().execQuery(query);
};

function setClass(className) {
    return create().setClass(className);
};

exports.create = create;
exports.execQuery = execQuery;
exports.setClass = setClass;

exports.VERSIONINFO = "WMI interface (wmi.js) version 0.1.3";
exports.global = global;
exports.require = global.require;
