// llm-guard.js
// Local Qwen3Guard policy gate for outbound LLM requests.

var HTTP = require("lib/http");
var SYS = require("lib/system");

var DEFAULTS = {
    "url": "http://127.0.0.1:8081/v1/chat/completions",
    "model": "qwen3guard",
    "modelPath": SYS.getAppDataDir() + "\\qwen3guard_06b\\Qwen3Guard-Gen-0.6B.Q4_K_M.gguf"
};

var anonymizer = null;
var guardServer = null;

function ensureServer(options) {
    options = options || {};
    var url = options.url || DEFAULTS.url;
    try {
        var healthUrl = url.replace(/\/v1\/chat\/completions$/, "/health");
        var health = HTTP.create("MSXML").open("get", healthUrl).send();
        if (health._interface.status === 200) return;
    } catch (ignored) {}
    if (options.autoStart === false) return;
    if (!guardServer) {
        var LlamaCpp = require("lib/llamacpp");
        guardServer = new LlamaCpp();
        guardServer.setPort(parseInt(options.port || 8081, 10));
        guardServer.loadModel(options.modelPath || DEFAULTS.modelPath);
    }
    for (var i = 0; i < 20; i++) {
        if (guardServer.isRunning()) return;
        sleep(1);
    }
    throw new Error("Qwen3Guard local server did not become ready");
}

function parseDecision(text) {
    var value = String(text || "");
    var match = value.match(/Safety\s*:\s*(Safe|Unsafe|Controversial)/i);
    if (!match) throw new Error("Qwen3Guard returned no valid Safety label");
    var categories = [];
    var categoryMatch = value.match(/Categories?\s*:\s*([^\r\n]+)/i);
    if (categoryMatch) {
        categories = categoryMatch[1].split(",").map(function(x) {
            return x.replace(/^\s+|\s+$/g, "");
        });
    }
    return {"label": match[1].toLowerCase(), "categories": categories, "raw": value};
}

function inspect(message, options) {
    options = options || {};
    ensureServer(options);
    var response = HTTP.create("MSXML")
        .setContentType("application/json")
        .setRequestBody({
            "model": options.model || DEFAULTS.model,
            "messages": [{"role": "user", "content": String(message)}],
            "temperature": 0, "max_tokens": 64, "stream": false
        })
        .open("post", options.url || DEFAULTS.url)
        .send().responseBody;
    if (typeof response === "string") response = JSON.parse(response);
    if (response && response.error) throw new Error(response.error.message || "local guard error");
    if (!response || !response.choices || !response.choices.length) throw new Error("Qwen3Guard returned an empty response");
    return parseDecision(response.choices[0].message.content);
}

function setAnonymizer(callback) {
    if (callback !== null && typeof callback !== "function") throw new Error("Anonymizer must be a function or null");
    anonymizer = callback;
}

function anonymize(message, decision) {
    if (!anonymizer) throw new Error("Controversial content requires an anonymizer before it can leave the machine");
    var result = anonymizer(String(message), decision);
    if (typeof result !== "string") throw new Error("Anonymizer must return a string");
    return result;
}

exports.inspect = inspect;
exports.anonymize = anonymize;
exports.setAnonymizer = setAnonymizer;
exports.DEFAULTS = DEFAULTS;
exports.VERSIONINFO = "Local Qwen3Guard LLM policy gate version 0.1.0";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
