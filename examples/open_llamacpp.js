// Loads a model in the launcher's llama.cpp router and sends it one chat request.
// The model is named by its installer component (the folder under %APPDATA%\WelsonJS).
var LlamaCpp = require("lib/llamacpp");

function main() {
    var llama = new LlamaCpp();
    llama.setModel("hy_mt2_1_8b_1_25bit");

    try {
        llama.loadModel();
        console.info("llama.cpp router:", llama.getBaseUrl());

        var response = llama.chat([
            {"role": "user", "content": "Translate into Korean: Good morning."}
        ], {"temperature": 0, "max_tokens": 64});
        console.info(response.choices[0].message.content);

        var models = llama.listModels();
        for (var i = 0; i < models.length; i++) {
            console.info("model:", models[i].id, models[i].status && models[i].status.value);
        }
    } catch (e) {
        console.error("llama.cpp request failed:", e.message);
    }
}

exports.main = main;
