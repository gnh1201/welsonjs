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
        
        // response
        e.target.send(JsonRpc2.dispatch(message));
    });
    
    server.listen();
}

// initialize
JsonRpc2.register("initialize", function (params, id) {
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
            "version": "1.1.0"
        },
        "isError": false
    };
});

// notifications/initialized
JsonRpc2.register("notifications/initialized", function (params, id) {
    return false;
});

// tools/list
JsonRpc2.register("tools/list", function (params, id) {
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
            },
			{
				"name": "evaluate_js",
				"title": "Evaluate JavaScript",
				"description": "Evaluate JavaScript for Windows system control and automation. "
					+ "Provides flexible system-level capabilities through WelsonJS. "
					+ "ES3 or ES5-compatible syntax is recommended for maximum compatibility. "
					+ "Set allowUnsafeEval to true when execution of the provided script is required. "
					+ "For shell access, use require(\"lib/shell\") first.",
				"inputSchema": {
					"type": "object",
					"properties": {
						"script": {
							"type": "string",
							"description": "JavaScript code to execute."
						},
						"allowUnsafeEval": {
							"type": "boolean",
							"description": "Set to true to allow execution of the provided JavaScript code. Required unless ALLOW_UNSAFE_EVAL is enabled.",
							"default": false
						}
					},
					"required": ["script"]
				}
			},
            {
                "name": "evaluate_js_es3",
                "title": "Evaluate JavaScript ES3",
                "description": "Deprecated. Please use evaluate_js instead.",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "script": {
                            "type": "string",
                            "description": "JavaScript code"
                        },
                        "allowUnsafeEval": {
                            "type": "boolean",
                            "description": "Set to true to allow execution of the provided JavaScript code. Required unless ALLOW_UNSAFE_EVAL is enabled.",
                            "default": false
                        }
                    },
                    "required": ["script"]
                }
            }
        ],
        "isError": false
    };
});

// tools/call
JsonRpc2.register("tools/call", function (params, id) {
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
    
    if (function_calling_name == "evaluate_js" || function_calling_name == "evaluate_js_es3") {
        var isError = false;
        var allowUnsafeEval = params.arguments
            && ("allowUnsafeEval" in params.arguments)
            ? params.arguments.allowUnsafeEval
            : false;

        return {
            "content": [
                {
                    "type": "text",
                    "text": (function(script) {
                        try {
                            if (!ALLOW_UNSAFE_EVAL && !allowUnsafeEval) {
                                throw new Error("Unsafe eval is not allowed. Please set ALLOW_UNSAFE_EVAL to true if you want to allow it.");
                            }
                            var evaluate = new Function(script);
                            return String(evaluate());
                        } catch (e) {
                            isError = true;
                            return "Error: " + e.message;
                        }
                    })(params.arguments.script)
                }
            ],
            "isError": isError
        }
    }
});

exports.main = main;
