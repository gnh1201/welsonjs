// Example: all LLM requests are locally classified before outbound inference.
var LIE = require("lib/language-inference-engine");
var GUARD = require("lib/llm-guard");

// Install a real PII anonymizer here before enabling controversial requests.
// The default is intentionally unset: controversial content is blocked.
// GUARD.setAnonymizer(function(text, decision) { return myAnonymize(text); });

function main() {
    var engine = LIE.create().setProvider("openai");
    return engine.inference("Summarize this text", 0).join(" ");
}

exports.main = main;
exports.guard = GUARD;
