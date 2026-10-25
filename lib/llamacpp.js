// llamacpp.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// LLama.cpp integration for local model serving via llama-server.
// This library starts and manages a local llama.cpp server process and
// communicates with it through the OpenAI-compatible HTTP interface.
//
var SYS = require("lib/system");
var SHELL = require("lib/shell");
var FILE = require("lib/file");
var HTTP = require("lib/http");

function safeJSONParse(input) {
    if (typeof input === "string") {
        try {
            return JSON.parse(input);
        } catch (e) {
            return null;
        }
    }
    return input;
}

function LLamaCPPObject() {
    this.binPath = null;
    this.modelPath = null;
    this.modelName = "local-model";
    this.host = "127.0.0.1";
    this.port = 8080;
    this.workingDirectory = null;
    this.contextSize = 2048;
    this.nThreads = 0;
    this.nGpuLayers = 0;
    this.batchSize = 512;
    this.maxTokens = 256;
    this.temperature = 0.7;
    this.verbose = false;
    this.extraArgs = [];
    this.pid = null;

    this.create = function() {
        var candidates = [
            SYS.getAppDataDir() + "\\llama.cpp\\llama-server.exe",
            SYS.getAppDataDir() + "\\llama.cpp\\bin\\llama-server.exe",
            "bin\\x64\\llama-server.exe",
            "bin\\x86\\llama-server.exe",
            "bin\\arm64\\llama-server.exe",
            ".\\llama-server.exe",
            ".\\bin\\x64\\llama-server.exe",
            ".\\bin\\x86\\llama-server.exe",
            ".\\bin\\arm64\\llama-server.exe"
        ];

        for (var i = 0; i < candidates.length; i++) {
            if (FILE.fileExists(candidates[i])) {
                this.binPath = candidates[i];
                break;
            }
        }

        if (!this.modelPath) {
            var modelCandidates = [
                SYS.getAppDataDir() + "\\llama.cpp\\models\\*.gguf",
                SYS.getAppDataDir() + "\\llama.cpp\\*.gguf",
                ".\\models\\*.gguf",
                ".\\*.gguf"
            ];

            for (var j = 0; j < modelCandidates.length; j++) {
                var match = modelCandidates[j];
                if (FILE.fileExists(match)) {
                    this.modelPath = match;
                    break;
                }
            }
        }

        return this;
    };

    this.setBinPath = function(binPath) {
        this.binPath = binPath;
        return this;
    };

    this.setModelPath = function(modelPath) {
        this.modelPath = modelPath;
        return this;
    };

    this.setModelName = function(modelName) {
        this.modelName = modelName;
        return this;
    };

    this.setHost = function(host) {
        this.host = host;
        return this;
    };

    this.setPort = function(port) {
        this.port = port;
        return this;
    };

    this.setWorkingDirectory = function(workingDirectory) {
        this.workingDirectory = workingDirectory;
        return this;
    };

    this.setContextSize = function(contextSize) {
        this.contextSize = contextSize;
        return this;
    };

    this.setThreads = function(nThreads) {
        this.nThreads = nThreads;
        return this;
    };

    this.setGpuLayers = function(nGpuLayers) {
        this.nGpuLayers = nGpuLayers;
        return this;
    };

    this.setBatchSize = function(batchSize) {
        this.batchSize = batchSize;
        return this;
    };

    this.setMaxTokens = function(maxTokens) {
        this.maxTokens = maxTokens;
        return this;
    };

    this.setTemperature = function(temperature) {
        this.temperature = temperature;
        return this;
    };

    this.setVerbose = function(verbose) {
        this.verbose = verbose;
        return this;
    };

    this.addExtraArg = function(arg) {
        this.extraArgs.push(arg);
        return this;
    };

    this.getServerURL = function() {
        return "http://" + this.host + ":" + this.port;
    };

    this.getCommand = function() {
        var cmd = [];

        if (!this.binPath) {
            this.create();
        }

        if (!this.binPath) {
            throw new Error("llama-server.exe not found. Install llama.cpp or set binPath.");
        }

        cmd.push(this.binPath);

        if (this.modelPath) {
            cmd.push("-m");
            cmd.push(this.modelPath);
        }

        cmd.push("--host");
        cmd.push(this.host);
        cmd.push("--port");
        cmd.push(String(this.port));

        if (this.contextSize > 0) {
            cmd.push("-c");
            cmd.push(String(this.contextSize));
        }

        if (this.nThreads > 0) {
            cmd.push("-t");
            cmd.push(String(this.nThreads));
        }

        if (this.nGpuLayers > 0) {
            cmd.push("-ngl");
            cmd.push(String(this.nGpuLayers));
        }

        if (this.batchSize > 0) {
            cmd.push("-b");
            cmd.push(String(this.batchSize));
        }

        if (this.verbose) {
            cmd.push("--verbose");
        }

        if (this.extraArgs.length > 0) {
            for (var i = 0; i < this.extraArgs.length; i++) {
                cmd.push(this.extraArgs[i]);
            }
        }

        return cmd;
    };

    this.start = function() {
        if (!this.binPath) {
            this.create();
        }

        if (!this.binPath || !FILE.fileExists(this.binPath)) {
            throw new Error("llama-server executable could not be found at: " + this.binPath);
        }

        if (this.modelPath && !FILE.fileExists(this.modelPath)) {
            throw new Error("Model file could not be found at: " + this.modelPath);
        }

        var cmd = this.getCommand();
        var commandLine = SHELL.build(cmd);
        this.pid = SYS.createProcess(commandLine);

        return this.pid;
    };

    this.stop = function() {
        if (this.pid && SYS.isAlivePID(this.pid)) {
            return SYS.killProcess(this.pid);
        }
        return false;
    };

    this.isRunning = function() {
        if (!this.pid) {
            return false;
        }
        return SYS.isAlivePID(this.pid);
    };

    this.waitForReady = function(timeoutMs, intervalMs) {
        timeoutMs = (typeof timeoutMs === "undefined") ? 30000 : timeoutMs;
        intervalMs = (typeof intervalMs === "undefined") ? 500 : intervalMs;

        var endTime = (new Date()).getTime() + timeoutMs;

        while ((new Date()).getTime() < endTime) {
            try {
                var response = HTTP.create("MSXML")
                    .open("get", this.getServerURL() + "/health")
                    .send()
                    .responseBody;

                if (response) {
                    return true;
                }
            } catch (e) {
                // retry until timeout
            }
            sleep(Math.ceil(intervalMs / 1000));
        }

        return false;
    };

    this.chat = function(prompt, options) {
        if (typeof options === "undefined") {
            options = {};
        }

        var requestBody = {
            "model": (typeof options.model !== "undefined") ? options.model : this.modelName,
            "messages": [{
                "role": "user",
                "content": prompt
            }],
            "temperature": (typeof options.temperature !== "undefined") ? options.temperature : this.temperature,
            "max_tokens": (typeof options.max_tokens !== "undefined") ? options.max_tokens : this.maxTokens,
            "stream": false
        };

        if (typeof options.stop !== "undefined") {
            requestBody.stop = options.stop;
        }

        var response = HTTP.create("MSXML")
            .setContentType("application/json")
            .setRequestBody(requestBody)
            .open("post", this.getServerURL() + "/v1/chat/completions")
            .send()
            .responseBody;

        response = safeJSONParse(response);

        if (response && response.error) {
            return "Error: " + response.error.message;
        }

        if (response && response.choices && response.choices.length > 0) {
            var text = [];
            for (var i = 0; i < response.choices.length; i++) {
                if (response.choices[i].message && response.choices[i].message.content) {
                    text.push(response.choices[i].message.content);
                }
            }
            return text.join("\n");
        }

        return "";
    };

    this.generate = function(prompt, options) {
        return this.chat(prompt, options);
    };

    this.create();
}

exports.LLamaCPPObject = LLamaCPPObject;
exports.create = function() {
    return new LLamaCPPObject();
};

exports.start = function() {
    return (new LLamaCPPObject()).start();
};

exports.stop = function(pid) {
    return SYS.killProcess(pid);
};

exports.VERSIONINFO = "LLama.cpp integration (llamacpp.js) version 0.1.0";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
