function main(args) {
    console.log("Hello world");
    if (typeof WScript !== "undefined") {
        console.log("Runtime version:", WScript.Version);
    }
}

exports.main = main;