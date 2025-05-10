var LIE = require("lib/language-inference-engine")

function main(args) {
  
    var provider = "gemini"
    var text = "hanoi tower example"

    var res = LIE.create()
        .setProvider(provider)
        // If no model is specified, the first possible model is used.
        .setModel("gemini-2.0-flash")
        .inference(text, 0)
        .join(' ')
    console.log(res)
}

exports.main = main;
