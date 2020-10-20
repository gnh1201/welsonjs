////////////////////////////////////////////////////////////////////////
// HTTPServer API
///////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

var ResponseCodes = {
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
};

var listener, http = {};

var listen = function(port) {
    try {
        listener = CreateObject("MSWinsock.Winsock.1", "listener_");
        listener.localPort = port;
        listener.bind();
        listener.listen();
        console.info("Listening port: " + port);
    } catch(e) {
        console.error("port " + port " could not bind: " + e.message);
    }
};

var arrival = function() {
    // todo
};

// todo
