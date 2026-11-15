// llm-guard.js
// Local Qwen3Guard policy gate for data sent to LLMs.
//
// Classification runs inside WelsonJS.Launcher (POST /llm-guard/inspect), which
// owns the Qwen3Guard llama-server process. Any failure is fail-closed.

var HTTP = require("lib/http");

var DEFAULTS = {
    "launcherUrl": "http://localhost:3000"
};

var anonymizer = null;

// null follows ALLOW_UNSAFE_OUTBOUND in app.js (guard on unless it is true);
// setEnabled() overrides it.
var enabledOverride = null;

function isEnabled() {
    if (enabledOverride !== null) return enabledOverride;
    return typeof ALLOW_UNSAFE_OUTBOUND === "undefined" || ALLOW_UNSAFE_OUTBOUND !== true;
}

function setEnabled(enabled) {
    if (enabled !== null && typeof enabled !== "boolean") throw new Error("setEnabled expects true, false, or null");
    enabledOverride = enabled;
}

function inspect(message, options) {
    // Disabled: nothing is classified. "unchecked" (not "safe") keeps that visible.
    if (!isEnabled()) return {"label": "unchecked", "categories": [], "raw": "", "checked": false};

    options = options || {};
    var url = String(options.launcherUrl || DEFAULTS.launcherUrl).replace(/\/+$/, "") + "/llm-guard/inspect";
    var response = HTTP.create("MSXML")
        .setContentType("application/json")
        .setRequestBody({"text": String(message)})
        .open("post", url)
        .send().responseBody;
    // Error statuses still carry a JSON body; no body means the launcher is not running.
    if (typeof response === "string" && response.length > 0) {
        try { response = JSON.parse(response); }
        catch (e) { throw new Error("The launcher LLM guard returned an invalid response"); }
    }
    if (!response || typeof response !== "object") throw new Error("The launcher LLM guard is not reachable at " + url);
    if (response.error) throw new Error(response.error);
    if (!/^(safe|controversial|unsafe)$/.test(response.label)) throw new Error("The launcher LLM guard returned an invalid label");
    return {"label": response.label, "categories": response.categories || [], "raw": response.raw || "", "checked": true};
}

// Categories that are blocked whatever the label is. An anonymizer masks data such
// as PII; it has nothing to mask in an injection, so it cannot make these safe.
var BLOCKED_CATEGORIES = ["jailbreak"];

function getBlockedCategory(decision) {
    var categories = (decision && decision.categories) || [];
    for (var i = 0; i < categories.length; i++) {
        var name = String(categories[i]).replace(/^\s+|\s+$/g, "").toLowerCase();
        for (var j = 0; j < BLOCKED_CATEGORIES.length; j++) {
            if (name === BLOCKED_CATEGORIES[j]) return String(categories[i]);
        }
    }
    return null;
}

function describe(decision) {
    return decision.label + (decision.categories.length ? ": " + decision.categories.join(", ") : "");
}

// Throws when the decision has a category that is blocked regardless of label.
function assertAllowedCategories(decision) {
    var blocked = getBlockedCategory(decision);
    if (blocked) throw new Error("Blocked by the LLM guard (" + describe(decision) + "; " + blocked + " is always blocked)");
}

function setAnonymizer(callback) {
    if (callback !== null && typeof callback !== "function") throw new Error("Anonymizer must be a function or null");
    anonymizer = callback;
}

function anonymize(message, decision) {
    // Never let an always-blocked category through by anonymizing it.
    assertAllowedCategories(decision);
    if (!anonymizer) throw new Error("Controversial content requires an anonymizer before it can leave the machine");
    var result = anonymizer(String(message), decision);
    if (typeof result !== "string") throw new Error("Anonymizer must return a string");
    return result;
}

// Policy for callers that have no local fallback (e.g. MCP tool results):
// safe text passes, controversial text passes only after anonymization, and
// unsafe text, an always-blocked category (e.g. Jailbreak) under any label, or
// any guard failure throws. Disabled: the text passes unchecked.
function enforce(message, options) {
    var decision = inspect(message, options);
    if (!decision.checked) return String(message);
    assertAllowedCategories(decision);
    if (decision.label === "unsafe") {
        throw new Error("Blocked by the LLM guard (" + describe(decision) + ")");
    }
    if (decision.label === "controversial") {
        return anonymize(message, decision);
    }
    return String(message);
}

exports.isEnabled = isEnabled;
exports.setEnabled = setEnabled;
exports.inspect = inspect;
exports.enforce = enforce;
exports.getBlockedCategory = getBlockedCategory;
exports.assertAllowedCategories = assertAllowedCategories;
exports.BLOCKED_CATEGORIES = BLOCKED_CATEGORIES;
exports.anonymize = anonymize;
exports.setAnonymizer = setAnonymizer;
exports.DEFAULTS = DEFAULTS;
exports.VERSIONINFO = "Local Qwen3Guard LLM policy gate version 0.2.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
