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

var StreamBuilder = function(data) {
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

var XMLObject = function(node) {
    this.filename = null;
    this.dom = null;
    this.nodes = [];

    if (typeof(node) !== "undefined" && node instanceof this) {
        this.dom = node;
    } else {
        this.dom = createXMLObject();
    }

    this.load = function(filename) {
        this.filename = filename;

        if (FILE.fileExists(filename)) {
            this.dom.loadXML(FILE.readFile(this.filename, "utf-8"));
        } else {
            console.error("The file does not exists");
            return;
        }

        return this;
    }
    
    this.setObject = function(dom) {
        this.dom = dom;
    };

    this.getObject = function() {
        return this.dom;
    };

    this.getAttribute = function(name) {
        return this.getObject().getAttribute(name);
    };

    this.getText = function() {
        return this.getObject().text;
    };

    this.select = function(path) {
        var nodes = this.getObject().selectNodes(path);
        for (var i = 0; i < nodes.length; i++) {
            this.nodes.push(new XMLObject(node[i]));
        }
    };

    this.toArray = function() {
        return this.nodes;
    };

    this.first = function() {
        if(this.nodes.length > 0) {
            return this.nodes[0];
        }
    };

    this.last = function() {
        if(this.nodes.length > 0) {
            return this.nodes[this.nodes.length - 1];
        }
    };

    this.eq = function(i) {
        if(nodes.length > i) {
            return nodes[i];
        }
    }

    this.retrieve = function(mode, callback) {
        var nodes = [];

        if (typeof(callback) === "function") {
            var i = 0, s = 0;

            while (s == 0 && i < this.nodes.length) {
                if (callback(this.nodes[i])) {
                    if (mode === "find")
                        s++;

                    nodes.push(this.nodes[i]);
                }

                i++;
            }
        }

        switch (mode) {
            case "find":
                if (nodes.length > 0)
                    return nodes.pop();
                break;

            case "filter":
                this.nodes = nodes;
                break;

            case "forEach":
                break;
        }

        return this;
    }
    
    this.find = function(callback) {
        return this.retrieve("find", callback);
    };
    
    this.filter = function(callback) {
        return this.retrieve("filter", callback);
    };
    
    this.forEach = function(callback) {
        return this.retrieve("forEach", callback);
    };

    this.createElement = function(elementname) {
        this.setObject(this.getObject().createElement(name));
    }

    this.encode = function(value, type) {
        this.getObject().dataType = type;

        if (type.indexOf("bin.") == 0) {
            this.getObject().nodeTypedValue = (new StreamBuilder(value)).toBinary();
        } else {
            this.getObject().nodeTypedValue = value;
        }

        return this.getObject().text;
    };

    this.decode = function(value, type) {
        this.getObject().dataType = type;

        if (type.indexOf("bin.") == 0) {
            this.getObject().text = (new StreamBuilder(value)).toString();
        } else {
            this.getObject().text = value;
        }

        return this.getObject().nodeTypedValue;
    };
};

exports.create = createXMLObject;
exports.load = function(filename) {
    return (new XMLObject()).load(filename);
};
exports.createElement = function(elementname) {
    return (new XMLObject()).createElement(elementname);
};
