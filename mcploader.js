// mcploader.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var StdioServer = require("lib/stdio-server");
var JsonRpc2 = require("lib/jsonrpc2");

function main(args) {
	var server = StdioServer.create();

	server.addEventListener("message", function(e) {
		var message = e.target.receive();
		
		try {
			JsonRpc2.extract(message, function(method, params, id) {
				console.log("Received method: " + method);
			});
		} catch (e) {
			console.log("Ignored");
		}
		
		// send message 
		e.target.send("Hello world");
	});

	server.listen();
}

exports.main = main;
