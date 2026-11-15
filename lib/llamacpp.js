// llamacpp.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// Client of the launcher's llama.cpp router (docs/llamacpp-launcher-api.md). The
// launcher runs one llama-server in router mode; every request names its model, which
// is the installer component (the folder under %APPDATA%\WelsonJS), for example
// "hy_mt2_1_8b_1_25bit". The router loads a model on demand and unloads the least
// recently used one when too many are loaded (hot swap).

var HTTP = require("lib/http");

function LlamaCpp() {
    this.model = "";
    this.launcherUrl = "http://localhost:3000/llamacpp";
    this.baseUrl = null;
    this.lastError = null;

    this.create = function() {
        return this;
    };

    this.setLauncherUrl = function(url) {
        if (!url) throw new Error("Launcher URL is required");
        url = String(url).replace(/\/$/, "");
        this.launcherUrl = /\/llamacpp$/i.test(url) ? url : url + "/llamacpp";
        this.baseUrl = null;
        return this;
    };

    // The model that requests name, for example "hy_mt2_1_8b_1_25bit".
    this.setModel = function(model) {
        this.model = String(model || "");
        return this;
    };

    this.getModel = function() {
        return this.model;
    };

    this._request = function(method, path, body) {
        var response = method === "get"
            ? HTTP.get(this.launcherUrl + path)
            : HTTP.create("MSXML")
                .setContentType("application/json")
                .setRequestBody(body || {})
                .open("post", this.launcherUrl + path)
                .send().responseBody;
        if (typeof response === "string") response = JSON.parse(response);
        if (!response || response.error) {
            // Launcher errors are {error: "..."}; llama-server errors are {error: {message}}.
            var error = response && response.error;
            this.lastError = !error ? "Launcher request failed: " + path
                : (typeof error === "string" ? error : (error.message || JSON.stringify(error)));
            throw new Error(this.lastError);
        }
        return response;
    };

    this._requireModel = function(model) {
        var name = String(model || this.model || "");
        if (!name) throw new Error("A model is required: call setModel() or pass the model name");
        return name;
    };

    // Starts the router (if needed) and returns its base URL.
    this.startServer = function() {
        var response = this._request("post", "/start", {});
        this.baseUrl = response.server && response.server.baseUrl;
        if (!this.baseUrl) throw new Error("The launcher returned no llama.cpp router address");
        return this.baseUrl;
    };

    // Stops the router and every loaded model.
    this.stopServer = function() {
        this._request("post", "/stop", {});
        this.baseUrl = null;
        return true;
    };

    // Loads a model (the given one, or setModel's) and waits until it is ready.
    this.loadModel = function(model) {
        var name = this._requireModel(model);
        var response = this._request("post", "/load", {"model": name});
        this.model = name;
        this.baseUrl = response.server && response.server.baseUrl || this.baseUrl;
        return response.model;
    };

    this.unloadModel = function(model) {
        return this._request("post", "/unload", {"model": this._requireModel(model)}).unloaded === true;
    };

    // Installed models and their state ({id, status: {value: "loaded" | "unloaded" | ...}, ...}).
    this.listModels = function() {
        return this._request("get", "/models").models || [];
    };

    // The models the launcher's editor offers as Local AI (LlamaCppModels setting), in
    // order: [{id, installed}, ...].
    this.getConfiguredModels = function() {
        return this._request("get", "/status").models || [];
    };

    // The router's base URL for direct OpenAI-compatible requests, which must
    // include "model" (for example the engine in lib/language-inference-engine).
    this.getBaseUrl = function() {
        if (!this.baseUrl) {
            var status = this._request("get", "/status");
            this.baseUrl = status.server && status.server.running ? status.server.baseUrl : null;
        }
        return this.baseUrl || this.startServer();
    };

    this.isRunning = function() {
        try {
            var status = this._request("get", "/status");
            return !!(status.server && status.server.running);
        } catch (e) {
            this.lastError = e.message;
            return false;
        }
    };

    // Chat completion through the launcher with the given messages
    // ([{role, content}, ...]). options: temperature, max_tokens, model, ...
    this.chat = function(messages, options) {
        var body = {};
        options = options || {};
        for (var key in options) {
            if (Object.prototype.hasOwnProperty.call(options, key)) body[key] = options[key];
        }
        body.model = this._requireModel(options.model);
        body.messages = messages;
        return this._request("post", "/chat", body);
    };

    this.getLastError = function() {
        return this.lastError;
    };

    this.create();
}

module.exports = LlamaCpp;
