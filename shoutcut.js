var SHELL = require("lib/shell");
var SYS = require("lib/system");

exports.main = function(args) {
    if (args.length < 1) {
        console.error("arguments could not be empty")
        return;
    }

	var FN = args[0];
	var target = require(FN);

	if ("onShoutcut" in target) {
		console.log("Trying execute onShoutcut:", FN);
		target.onShoutcut(args.slice(1));
	} else {
		console.error("onShoutcut not defined");
	}
};
