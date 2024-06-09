////////////////////////////////////////////////////////////////////////
// XML interface
////////////////////////////////////////////////////////////////////////
var FILE = require("lib/file");
var PipeIPC = require("lib/pipe-ipc");

var XMLObject = function(dom) {
    this.filename = null;
    this.dom = null;
    this.nodes = [];

    if (typeof(dom) !== "undefined") {
        this.dom = dom;
    } else {
        this.dom = CreateObject([
            "Msxml2.DOMDocument.6.0", 
            "Msxml2.DOMDocument.5.0", 
            "Msxml2.DOMDocument.4.0", 
            "Msxml2.DOMDocument.3.0", 
            "Msxml2.DOMDocument", 
            "MSXML.DOMDocument"
        ]);
    }

    this.getDOM = function() {
        return this.dom;
    };

    this.load = function(filename) {
        this.filename = filename;
        console.info("XMLObject.load() -> Opening XML file: " + filename)

        try {
            if (FILE.fileExists(filename)) {
                this.getDOM().loadXML(FILE.readFile(this.filename, FILE.CdoCharset.CdoUTF_8));
            } else {
                console.error("XMLObject.load() -> The file does not exists or access denied");
            }
        } catch (e) {
            console.error("XMLObject.load() -> " + e.message);
        }

        return this;
    }

    this.getAttribute = function(s) {
        return this.getDOM().getAttribute(s);
    };

    this.getText = function() {
        return this.getDOM().text;
    };
    
    this.select = function(path) {
        var nodes = this.getDOM().selectNodes(path);
        for (var i = 0; i < nodes.length; i++) {
            this.nodes.push(new XMLObject(nodes[i]));
        }
        return this;
    };

    this.toArray = function() {
        return this.nodes;
    };

    this.first = function() {
        if (this.nodes.length > 0) {
            return this.nodes[0];
        }
    };
    
    this.last = function() {
        if (this.nodes.length > 0) {
            return this.nodes[this.nodes.length - 1];
        }
    };
    
    this.eq = function() {
        if (this.nodes.length > i) {
            return this.nodes[i];
        }
    };

    this.createElement = function(name) {
        return new XMLObject(this.getDOM().createElement(name));
    }

    this.encode = function(value, type) {
        try {
            var dom = this.getDOM();
            var node = dom.createElement("XMLNode");

            node.dataType = type;
            node.nodeTypedValue = (new PipeIPC.Converter()).from(value).getBinaryFromText();

            return node.text;
        } catch (e) {
            console.error("XMLObject->encode():", e.message);
        }
    };

    this.decode = function(value, type) {
        try {
            var dom = this.getDOM();
            var node = dom.createElement("XMLNode");

            node.dataType = type;
            node.text = value;

            return (new PipeIPC.Converter()).from(node.nodeTypedValue).getTextFromBinary();
        } catch (e) {
            console.error("XMLObject->decode():", e.message);
        }
    };
};

function create() {
    return new XMLObject();
}

function load(s) {
    return create().load(s);
}

function encode(value, type) {
    return create().encode(value, type);
}

function decode(value, type) {
    return create().decode(value, type);
}

exports.create = create;
exports.load = load;
exports.encode = encode;
exports.decode = decode;

exports.VERSIONINFO = "XML interface (xml.js) version 0.2.4";
exports.global = global;
exports.require = global.require;
