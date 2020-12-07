////////////////////////////////////////////////////////////////////////
// HTTPServer API
///////////////////////////////////////////////////////////////////////
var HTTPServer = {
    _this: this, // Avoid conflicts between HTTPServer and Winsock variables

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
            _this.Listener = _this.CreateWinsockObject();
            _this.Listener.localPort = port;
            _this.Listener.bind();
            _this.Listener.listen();
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
        _this.OnRequest = OnRequest;

        return HTTPServer;
    },

    ConnectionRequest: function(requestID) {
        console.info("Connection request " + requestID);

        _this.Connections[requestID] = {
            Listener: _this.CreateWinsockObject()
        };
        _this.Connections[requestID].Listener.accept(requestID);
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

exports = HTTPServer;
