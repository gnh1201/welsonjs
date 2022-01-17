////////////////////////////////////////////////////////////////////////
// XML API
////////////////////////////////////////////////////////////////////////
var FILE = require("lib/file");

var StreamConvert = function(data) {
    this.data = data;

    this.toBinary = function() {
        var adTypeText = 2;
        var adTypeBinary = 1;
        var BinaryStream = CreateObject("ADODB.Stream");
        BinaryStream.Type = adTypeText;
        BinaryStream.CharSet = "utf-8";
        BinaryStream.Open();
        BinaryStream.WriteText(this.data);
        BinaryStream.Position = 0;
        BinaryStream.Type = adTypeBinary;
        BinaryStream.Position = 0;
        return BinaryStream.Read();
    };

    this.toString = function() {
        var adTypeText = 2;
        var adTypeBinary = 1;
        var BinaryStream = CreateObject("ADODB.Stream");
        BinaryStream.Type = adTypeBinary;
        BinaryStream.Open();
        BinaryStream.Write(this.data);
        BinaryStream.Position = 0;
        BinaryStream.Type = adTypeText;
        BinaryStream.CharSet = "utf-8";
        return BinaryStream.ReadText();
    };

};

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
                this.getDOM().loadXML(FILE.readFile(this.filename, "utf-8"));
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

            dom.dataType = type;
            if (type.indexOf("bin.") == 0) {
                dom.nodeTypedValue = (new StreamConvert(value)).toBinary();
            } else {
                dom.nodeTypedValue = value;
            }

            return dom.text;
        } catch (e) {
            console.error("XMLObject->encode():", e.message);
        }
    };

    this.decode = function(value, type) {
        try {
            var dom = this.getDOM();

            dom.dataType = type;
            if (type.indexOf("bin.") == 0) {
                dom.text = (new StreamConvert(value)).toString();
            } else {
                dom.text = value;
            }

            return dom.nodeTypedValue;
        } catch (e) {
            console.error("XMLObject->decode():", e.message);
        }
    };
};

exports.create = function() {
    return (new XMLObject());
};

exports.load = function(s) {
    return (new XMLObject()).load(s);
};

exports.VERSIONINFO = "XML interface (xml.js) version 0.1";
exports.global = global;
exports.require = global.require;
