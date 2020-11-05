////////////////////////////////////////////////////////////////////////
// Base64 API
////////////////////////////////////////////////////////////////////////

var XML = require("lib/xml");

exports.VERSIONINFO = "Base64 Module (base64.js) version 0.1";
exports.global = global;
exports.require = require;

var getStream_StringToBinary = function(dText) {
    var adTypeText = 2;
    var adTypeBinary = 1;
    var BinaryStream = CreateObject("ADODB.Stream");
    BinaryStream.Type = adTypeText;
    BinaryStream.CharSet = "utf-8";
    BinaryStream.Open();
    BinaryStream.WriteText(dText);
    BinaryStream.Position = 0;
    BinaryStream.Type = adTypeBinary;
    BinaryStream.Position = 0;
    return BinaryStream.Read();
};

var getStream_BinaryToString = function(dBinary) {
    var adTypeText = 2;
    var adTypeBinary = 1;
    var BinaryStream = CreateObject("ADODB.Stream");
    BinaryStream.Type = adTypeBinary;
    BinaryStream.Open();
    BinaryStream.Write(dBinary);
    BinaryStream.Position = 0;
    BinaryStream.Type = adTypeText;
    BinaryStream.CharSet = "utf-8";
    return BinaryStream.ReadText();
};

exports.encode = function(sText) {
    var oXML = XML.createXMLObject();
    var oNode = oXML.createElement("base64");
    oNode.dataType = "bin.base64";
    oNode.nodeTypedValue = getStream_StringToBinary(sText);
    return oNode.text;
};

exports.decode = function(vCode) {
    var oXML = XML.createXMLObject();
    var oNode = oXML.createElement("base64");
    oNode.dataType = "bin.base64";
    oNode.text = vCode;
    return getStream_BinaryToString(oNode.nodeTypedValue);
};
