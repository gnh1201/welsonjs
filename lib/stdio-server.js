// stdio-server.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var STD = require("lib/std");

function StdioServer() {
    // Set event-attachable object
    STD.EventTarget.apply(this, arguments);
    
    this.messages = [];
    
    this.receive = function () {
        return this.messages.shift();
    };
    
    this.send = function (message) {
        if (!message) return;
        
        if (typeof message === "object") {
            try {
                var _serialized = JSON.stringify(message);
                message = _serialized;
            } catch (e) { /* ignore */ }
        }
        
        WScript.StdOut.WriteLine(message);
        WScript.StdErr.WriteLine(message);
    };
    
    this.listen = function () {
        while (!WScript.StdIn.AtEndOfStream) {
            this.messages.push(WScript.StdIn.ReadLine());
            this.dispatchEvent(new STD.Event("message"));
        }
    };
}
StdioServer.prototype = new STD.EventTarget();
StdioServer.prototype.constructor = StdioServer;

/*

// For example
var server = new StdioServer();

server.addEventListener("message", function(e) {
    // receive message
    var message = e.target.receive();
    
    console.log(message);
    
    // send message 
    e.target.send("Hello world");
});

server.listen();

*/

exports.create = function () {
    return new StdioServer();
};

exports.VERSIONINFO = "Stdio Server (stdio-server.js) version 0.1";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
