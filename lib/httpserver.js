// httpserver.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// HTTPServer API
// 
var HTTPServer = {
    StatusCodes: {
        100: "Continue",
        200: "OK",
        206: "Partial Content",
        301: "Moved Permanently",
        302: "Found",
        304: "Not Modified",
        400: "Bad Request",
        401: "Unauthorized",
        403: "Forbidden",
        404: "Not Found",
        500: "Internal Server Error",
        503: "Service Unavailable"
    },

    Listener: null,

    Connections: {},

    CreateWinsockObject: function() {
        return CreateObject([
            "MSWinsock.Winsock.1",
            "MSWinsock.Winsock"
        ], "listener_");
    },

    Bind: function(port) {
        try {
            HTTPServer.Listener = HTTPServer.CreateWinsockObject();
            HTTPServer.Listener.localPort = port;
            HTTPServer.Listener.bind();
            HTTPServer.Listener.listen();
            console.info("Listening port: " + port);
        } catch (e) {
            console.error("port ", port, " could not bind: ", e.message);
        }
    },

    OnRequest: function(request, response) {
        console.log("HTTPServer.OnRequest() dose not implemented");
    },

    CreateServer: function(OnRequest) {
        if (typeof OnRequest !== "function") {
            throw new TypeError("OnRequest() must be a function.");
        }
        HTTPServer.OnRequest = OnRequest;

        return HTTPServer;
    },

    ConnectionRequest: function(requestID) {
        console.info("Connection request " + requestID);

        HTTPServer.Connections[requestID] = {
            Listener: HTTPServer.CreateWinsockObject()
        };
        HTTPServer.Connections[requestID].Listener.accept(requestID);
    },

    DataArrival: function(length) {
        // TODO: DataArrival
    },

    SendComplete: function() {
        // TODO: SendComplete
    }
};

global.listener_ConnectionRequest = HTTPServer.ConnectionRequest;
global.listener_DataArrival = HTTPServer.DataArrival;
global.listener_SendComplete = HTTPServer.SendComplete;

exports.HTTPServer = HTTPServer;
exports.CreateServer = HTTPServer.CreateServer;
exports.Bind = HTTPServer.Bind;

exports.VERSIONINFO = "HTTPServer API (httpserver.js) version 0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
