////////////////////////////////////////////////////////////////////////
// XML API
////////////////////////////////////////////////////////////////////////

var FILE = require("lib/file");

var createXMLObject = function() {
    return CreateObject([
        "Msxml2.DOMDocument.6.0", 
        "Msxml2.DOMDocument.5.0", 
        "Msxml2.DOMDocument.4.0", 
        "Msxml2.DOMDocument.3.0", 
        "MSXML2.DOMDocument", 
        "MSXML.DOMDocument"
    ]);
};

var loadXMLFile = function(filename) {
    var doc;

    try {
        doc = createXMLObject();

        if (FILE.fileExists(filename)) {
            doc.loadXML(FILE.readFile(filename, "utf-8"));
        } else {
            console.error("The file does not exists");
            return;
        }
    } catch(e) {
        console.error(e.message);
    }

    return {
        select: function(path) {
            var nodes = doc.selectNodes(path);

            return {
                getObject: function() {
                    return doc;
                },
                all: function() {
                    return nodes;
                },
                first: function() {
                    if(nodes.length > 0) {
                        return nodes[0];
                    }
                },
                last: function() {
                    if(nodes.length > 0) {
                        return nodes[nodes.length - 1];
                    }
                },
                eq: function(i) {
                    if(nodes.length > i) {
                        return nodes[i];
                    }
                }
            }
        }
    };
};

exports.createXMLObject = createXMLObject;
exports.loadXMLFile = loadXMLFile;
