// crispembed.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// Client of the launcher's CrispEmbed servers (docs/crispembed-launcher-api.md). The
// launcher runs one crispembed-server per model, started on first use; every request
// names its model, which is the installer component (the folder under
// %APPDATA%\WelsonJS\crispembed_models), for example "embeddinggemma_2_typed_decision".
//
// Requirements: the launcher (WelsonJS.Launcher) running on this machine, and the setup
// components crispembed and the model (for example crispembed\embeddinggemma_2_typed_decision).

var HTTP = require("lib/http");

function CrispEmbed() {
    this.model = "";
    this.launcherUrl = "http://localhost:3000/crispembed";
    this.lastError = null;

    this.create = function() {
        return this;
    };

    this.setLauncherUrl = function(url) {
        if (!url) throw new Error("Launcher URL is required");
        url = String(url).replace(/\/$/, "");
        this.launcherUrl = /\/crispembed$/i.test(url) ? url : url + "/crispembed";
        return this;
    };

    // The model that requests name, for example "embeddinggemma_2_typed_decision".
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
            // Launcher and crispembed-server errors are {error: "..."}; OpenAI-style ones are {error: {message}}.
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

    // Starts the model's server (if needed), waits until it is ready and returns its
    // health report ({status, dim, typed_decision, ...}).
    this.start = function(model) {
        var name = this._requireModel(model);
        var response = this._request("post", "/start", {"model": name});
        this.model = name;
        return response.health;
    };

    // Stops the model's server; with no model and none set, stops every CrispEmbed server.
    this.stop = function(model) {
        var name = String(model || this.model || "");
        var response = this._request("post", "/stop", name ? {"model": name} : {});
        return response.stopped === true;
    };

    // Configured and installed models: [{id, kind: "decision" | "embedding", configured, installed, running}, ...].
    this.listModels = function() {
        return this._request("get", "/models").models || [];
    };

    this.isRunning = function(model) {
        var name = String(model || this.model || "");
        try {
            var models = this.listModels();
            for (var i = 0; i < models.length; i++) {
                if (models[i].id === name) return models[i].running === true;
            }
            return false;
        } catch (e) {
            this.lastError = e.message;
            return false;
        }
    };

    // Embeddings of one text or an array of texts, as arrays of numbers in input order,
    // from a CrispEmbed embedding model (a decision model such as
    // embeddinggemma_2_typed_decision is refused: its encoder is fine-tuned for its
    // decision head). The original google/embeddinggemma-2 runs in llama.cpp instead:
    // lib/llamacpp.js embed() with the model "embeddinggemma_2".
    // options: model, dimensions (a smaller output size, when the model supports it).
    this.embed = function(input, options) {
        options = options || {};
        var body = {"model": this._requireModel(options.model), "input": input};
        if (typeof options.dimensions === "number") body.dimensions = options.dimensions;
        var response = this._request("post", "/embed", body);
        var vectors = [];
        var data = response.data || [];
        for (var i = 0; i < data.length; i++) {
            vectors[typeof data[i].index === "number" ? data[i].index : i] = data[i].embedding;
        }
        return vectors;
    };

    // Typed decisions with a model that has a typed-decision head (for example
    // embeddinggemma_2_typed_decision): state is an object describing the situation,
    // questions maps a name to {type: "choice" | "score" | "noul", instructions, ...}.
    // Returns crispembed-server's JSON response, whose answers carry kind, label,
    // value, confidence, probabilities and expected_level. options: model, workflow.
    this.decide = function(state, questions, options) {
        options = options || {};
        var body = {"model": this._requireModel(options.model), "state": state, "questions": questions};
        if (options.workflow) body.workflow = String(options.workflow);
        return this._request("post", "/decide", body);
    };

    // Cosine similarity of two vectors of the same length.
    this.cosine = function(a, b) {
        if (!a || !b || a.length !== b.length || a.length === 0) throw new Error("Vectors of the same length are required");
        var dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.length; i++) {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return (na === 0 || nb === 0) ? 0 : dot / (Math.sqrt(na) * Math.sqrt(nb));
    };

    this.getLastError = function() {
        return this.lastError;
    };

    this.create();
}

module.exports = CrispEmbed;
