////////////////////////////////////////////////////////////////////////
// Base64 API
////////////////////////////////////////////////////////////////////////

var scope = {
    VERSIONINFO: "Base64 Module (base64.js) version 0.1",
    global: global,
    require: global.require
};

scope.createMSXMLObject = function() {
	var progIDs = [
		"Msxml2.DOMDocument.6.0", 
		"Msxml2.DOMDocument.5.0", 
		"Msxml2.DOMDocument.4.0", 
		"Msxml2.DOMDocument.3.0", 
		"MSXML2.DOMDocument", 
		"MSXML.DOMDocument"
	];
	for (var i = 0; i < progIDs.length; i++) {
		try { 
			return new ActiveXObject(progIDs[i]); 
		} catch(e) {};
	}
	return null;
};

scope.getStream_StringToBinary = function(dText) {
	var adTypeText = 2;
	var adTypeBinary = 1;
	var BinaryStream = new ActiveXObject("ADODB.Stream");
	BinaryStream.Type = adTypeText;
	BinaryStream.CharSet = "utf-8";
	BinaryStream.Open();
	BinaryStream.WriteText(dText);
	BinaryStream.Position = 0;
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Position = 0;
	return BinaryStream.Read();
};

scope.getStream_BinaryToString = function(dBinary) {
	var adTypeText = 2;
	var adTypeBinary = 1;
	var BinaryStream = new ActiveXObject("ADODB.Stream");
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(dBinary);
	BinaryStream.Position = 0;
	BinaryStream.Type = adTypeText;
	BinaryStream.CharSet = "utf-8";
	return BinaryStream.ReadText();
};

scope.encode = function(sText) {
	var oXML = scope.createMSXMLObject();
	var oNode = oXML.createElement("base64");
	oNode.dataType = "bin.base64";
	oNode.nodeTypedValue = scope.getStream_StringToBinary(sText);
	return oNode.text;
};

scope.decode = function(vCode) {
	var oXML = scope.createMSXMLObject();
	var oNode = oXML.createElement("base64");
	oNode.dataType = "bin.base64";
	oNode.text = vCode;
	return scope.getStream_BinaryToString(oNode.nodeTypedValue);
};

return scope;
