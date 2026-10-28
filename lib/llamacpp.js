
// llamacpp.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs

var FILE = require("lib/file");
var HTTP = require("lib/http");
var WINLIBS = require("lib/winlibs");

function LlamaCpp() {
    this.modelPath = "";
    this.modelAlias = "";
    this.host = "127.0.0.1";
    this.port = 0;
    this.launcherUrl = "http://localhost:3000/llamacpp";
    this.processId = null;
    this.ctxSize = 4096;
    this.threads = 0;
    this.lastError = null;

    this.create = function() {
        return this;
    };

    this.setLauncherUrl = function(url) {
        if (!url) throw new Error("Launcher URL is required");
        url = String(url).replace(/\/$/, "");
        this.launcherUrl = /\/llamacpp$/i.test(url) ? url : url + "/llamacpp";
        return this;
    };

    this.getExecutableDirectory = function(path) {
        var pos = Math.max(
            path.lastIndexOf("\\"),
            path.lastIndexOf("/")
        );

        return pos >= 0 ? path.substring(0, pos) : ".";
    };

    this.setModelPath = function(path) {
        this.modelPath = path;
        this.modelAlias = path;
        return this;
    };

    this.setModel = function(model) {
        this.modelAlias = String(model || "");
        return this;
    };

    this.setHost = function(host) {
        this.host = host;
        return this;
    };

    this.setPort = function(port) {
        port = parseInt(port, 10);

        if (isNaN(port) || port < 0 || port > 65535) {
            throw new Error("Invalid port");
        }

        this.port = port;
        return this;
    };

    this.setContextSize = function(size) {
        size = parseInt(size, 10);

        if (isNaN(size) || size < 1) {
            throw new Error("Invalid context size");
        }

        this.ctxSize = size;
        return this;
    };

    this.setThreads = function(threads) {
        threads = parseInt(threads, 10);

        if (isNaN(threads) || threads < 0) {
            throw new Error("Invalid thread count");
        }

        this.threads = threads;
        return this;
    };

    this.getBaseUrl = function() {
        if (!this.processId) this.discoverProcess();
        var host = this.host;

        if (host === "0.0.0.0" || host === "::") {
            host = "127.0.0.1";
        }

        if (host.indexOf(":") >= 0 && host.charAt(0) !== "[") {
            host = "[" + host + "]";
        }

        return "http://" + host + ":" + this.port;
    };

    this.discoverProcess = function() {
        var response = HTTP.get(this.launcherUrl + "/list");
        if (typeof response === "string") response = JSON.parse(response);
        var processes = response && response.processes || [];
        for (var i = 0; i < processes.length; i++) {
            var item = processes[i];
            var identity = String(item.modelPath || "").toLowerCase();
            var alias = String(this.modelAlias || "").toLowerCase();
            if (!alias || identity.indexOf(alias) >= 0 || identity.indexOf(alias.replace(/_/g, "-")) >= 0) {
                this.processId = item.id;
                this.host = item.host || this.host;
                this.port = parseInt(item.port, 10);
                return item;
            }
        }
        throw new Error("No launcher-managed llama.cpp process is available");
    };

    // Start the server asynchronously.
    this.startServer = function() {
        if (!this.modelPath) throw new Error("Model path is required");
        if (!FILE.fileExists(this.modelPath)) throw new Error("Model file not found: " + this.modelPath);
        this.lastError = null;
        var response = HTTP.create("MSXML")
            .setContentType("application/json")
            .setRequestBody({
                "modelPath": this.modelPath,
                "host": this.host,
                "port": this.port,
                "contextSize": this.ctxSize,
                "threads": this.threads
            })
            .open("post", this.launcherUrl + "/start")
            .send().responseBody;
        if (typeof response === "string") response = JSON.parse(response);
        if (!response || response.error) throw new Error(response && response.error ? response.error : "Launcher failed to start llama.cpp");
        this.processId = response.id;
        this.host = response.host || this.host;
        this.port = parseInt(response.port, 10);
        if (!this.processId || isNaN(this.port)) throw new Error("Launcher returned an invalid llama.cpp process");
        return true;
    };

    // Set a model and launch the server.
    this.loadModel = function(path) {
        if (path) {
            this.setModelPath(path);
        }

        if (!this.modelPath) {
            throw new Error("Model path is required");
        }

        return this.startServer();
    };

    // Check whether the HTTP API is responding.
    this.isRunning = function() {
        try {
            var response = HTTP.get(this.launcherUrl + "/list");
            if (typeof response === "string") response = JSON.parse(response);
            var processes = response && response.processes || [];
            for (var i = 0; i < processes.length; i++) {
                if (this.processId && processes[i].id === this.processId) {
                    this.host = processes[i].host;
                    this.port = parseInt(processes[i].port, 10);
                    return processes[i].running === true;
                }
            }
            return false;
        } catch (e) {
            this.lastError = e.message;
            return false;
        }
    };

    this.stopServer = function() {
        if (!this.processId) return false;
        var response = HTTP.create("MSXML")
            .setContentType("application/json")
            .setRequestBody({"id": this.processId})
            .open("post", this.launcherUrl + "/stop")
            .send().responseBody;
        if (typeof response === "string") response = JSON.parse(response);
        if (response && response.error) throw new Error(response.error);
        this.processId = null;
        return true;
    };

    this.openBrowser = function() {
        WINLIBS.openBrowser(this.getBaseUrl());
        return true;
    };

    this.getLastError = function() {
        return this.lastError;
    };
	
	this.create();
}

module.exports = LlamaCpp;
