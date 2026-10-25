var SYS = require("lib/system");
var LlamaCpp = require("lib/llamacpp");

function main() {
    var llama = new LlamaCpp();

    llama.setModelPath(
        SYS.getAppDataDir() + "\\hy_mt2_1_8b\\Hy-MT2-1.8B-Q4_K_M.gguf"
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
