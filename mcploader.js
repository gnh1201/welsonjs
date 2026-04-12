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

        e.target.send(
            JsonRpc2.extract(message, function (method, params, id) {
                if (method == "initialize") {
                    return {
                        "protocolVersion": "2025-11-25",
                        "capabilities": {
                            "extensions": {
                                "io.modelcontextprotocol/ui": {
                                    "mimeTypes": ["text/html;profile=mcp-app"]
                                }
                            }
                        },
                        "serverInfo": {
                            "name": "WelsonJS MCP",
                            "version": "1.0.0"
                        }
                    };
                }
                
                if (method === "notifications/initialized") {
                    // DO NOT return anything
                    return false;
                }
                
                if (method == "tools/list") {
                    return {
                        "tools": [
                            {
                                "name": "add_both_numbers",
                                "title": "add both_numbers (add A and B)",
                                "description": "add two numbers (add A and B)",
                                "inputSchema": {
                                    "type": "object",
                                    "properties": {
                                        "a": {
                                            "type": "number"
                                        },
                                        "b": {
                                            "type": "number"
                                        }
                                    },
                                    "required": ["a", "b"]
                                }
                            }
                        ]
                    };
                }

                if (method == "tools/call") {
                    var function_calling_name = params.name;

                    if (function_calling_name == "add_both_numbers") {
                        return {
                            "content": [
                                {
                                    "type": "text",
                                    "text": "Result is " + (parseFloat(params.arguments.a) + parseFloat(params.arguments.b))
                                }
                            ],
                            "isError": false
                        };
                    }
                }

                return {
                    "isError": true
                };
            })
        );
    });

    server.listen();
}

exports.main = main;
