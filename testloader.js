////////////////////////////////////////////////////////////////////////
// Testloader
////////////////////////////////////////////////////////////////////////
var JsUnit = require("lib/jsunit").JsUnit;

exports.main = function(args) {
    JsUnit.Runner.run("test");
};
