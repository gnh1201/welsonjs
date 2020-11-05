////////////////////////////////////////////////////////////////////////
// Config API
////////////////////////////////////////////////////////////////////////

var XML = require("lib/xml");
var configObject;

var readConfig = function(path) {
    if (typeof(configObject) === "undefined") {
        configObject = XML.loadXMLFile("config.xml");
    }

    return configObject.select(path);
};

exports.readConfig = readConfig;
