var SYS = require("lib/system");
var LlamaCpp = require("lib/llamacpp");

function main() {
    var llama = new LlamaCpp();

    llama.setModelPath(
        SYS.getAppDataDir() + "\\lfm2_5_350m\\LFM2.5-350M-Q8_0.gguf"
    );

    try {
        llama.loadModel();
        llama.openBrowser();

        console.info("llama.cpp server:", llama.getBaseUrl());
    } catch (e) {
        console.error("Failed to launch llama.cpp:", e.message);
    }
}

exports.main = main;