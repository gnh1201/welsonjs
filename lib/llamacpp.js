
// llamacpp.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs

var SYS = require("lib/system");
var FILE = require("lib/file");
var SHELL = require("lib/shell");
var HTTP = require("lib/http");
var WINLIBS = require("lib/winlibs");

function LlamaCpp() {
    this._interface = SHELL.create();

    this.modelPath = "";
    this.host = "127.0.0.1";
    this.port = 8080;
    this.ctxSize = 4096;
    this.threads = 0;
    this.lastError = null;

    this.create = function() {
        var executablePath =
            SYS.getAppDataDir() + "\\llamacpp\\llama-server.exe";

        if (!FILE.fileExists(executablePath)) {
            throw new Error(
                "llama-server.exe not found: " + executablePath
            );
        }

        this._interface.setPrefix(executablePath);
        this._interface.setWorkingDirectory(
            this.getExecutableDirectory(executablePath)
        );

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
        return this;
    };

    this.setHost = function(host) {
        this.host = host;
        return this;
    };

    this.setPort = function(port) {
        port = parseInt(port, 10);

        if (isNaN(port) || port < 1 || port > 65535) {
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
        var host = this.host;

        if (host === "0.0.0.0" || host === "::") {
            host = "127.0.0.1";
        }

        if (host.indexOf(":") >= 0 && host.charAt(0) !== "[") {
            host = "[" + host + "]";
        }

        return "http://" + host + ":" + this.port;
    };

    this.buildArgs = function() {
        var args = [
            "--host", this.host,
            "--port", String(this.port),
            "--ctx-size", String(this.ctxSize)
        ];

        if (this.threads > 0) {
            args.push("--threads", String(this.threads));
        }

        if (this.modelPath) {
            if (!FILE.fileExists(this.modelPath)) {
                throw new Error(
                    "Model file not found: " + this.modelPath
                );
            }

            args.push("-m", this.modelPath);
        }

        return args;
    };

    // Start the server asynchronously.
    this.startServer = function() {
        this.lastError = null;
        this._interface.run(this.buildArgs(), true);

        console.info("llama-server launch requested");

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
            var request = HTTP.create();
            request.open("GET", this.getBaseUrl() + "/health");
            request.send();

            return request._interface.status === 200;
        } catch (e) {
            this.lastError = e.message;
            return false;
        }
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
