// mcploader.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
var StdioServer = require("lib/stdio-server");
var JsonRpc2 = require("lib/jsonrpc2");

function main(args) {
    var server = StdioServer.create();
    
    // notify
    var _notify = function(message) {
        console.log(message);
    };
    
    server.addEventListener("message", function(e) {
        var message = e.target.receive();
        
        // clear all console messsages
        console.clear();
        
        // response
        e.target.send(JsonRpc2.dispatch(message, _notify));
    });
    
    server.listen();
}

// initialize
JsonRpc2.register("initialize", function (rpc, params, id, callback) {
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
JsonRpc2.register("notifications/initialized", function (rpc, params, id, callback) {
    return false;
});

// tools/list
JsonRpc2.register("tools/list", function (rpc, params, id, callback) {
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
                    + "Set allowUnsafeEval to true to allow execution of the provided script. "
                    + "The specified execution timeout is optional and may be considered when appropriate. "
                    + "Whenever possible, perform operations visibly through the user interface rather than in the background. "
                    + "Unless explicitly instructed otherwise, do not save results or intermediate data to files. "
                    + "Each script execution is independent, so state from previous executions may not be shared. "
                    + "Use `_setState(key, value)` and `_getState(key)` when variables or instances need to be reused across script executions. "
                    + "Use `_hasState(key)`, `_deleteState(key)`, `_listStateKeys()`, and `_clearState()` to manage that state. "
                    + "Make every effort to restore or reacquire any necessary objects or processes. "
                    + "Always use Unicode escape sequences for non-ASCII text. "
                    + "Use require(\"lib/shell\") for shell access. "
                    + "Use require(\"lib/msoffice\") to access Microsoft Excel. "
                    + "Use `_notify(message)` instead of `console.log` when logging is needed. "
                    + "If the outcome cannot be determined programmatically, ask the user for confirmation.",
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
                        },
                        "timeout": {
                            "type": "integer",
                            "description": "Maximum execution time in milliseconds.",
                            "default": 900000,
                            "minimum": 180000,
                            "maximum": 900000
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
                        },
                        "timeout": {
                            "type": "integer",
                            "description": "Maximum execution time in milliseconds.",
                            "default": 900000,
                            "minimum": 180000,
                            "maximum": 900000
                        }
                    },
                    "required": ["script"]
                }
            },
            {
                "name": "manage_state",
                "title": "Manage evaluation state",
                "description": "Inspect or modify values shared between evaluate_js calls in this MCP session.",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "action": {
                            "type": "string",
                            "enum": ["get", "set", "delete", "list", "clear"]
                        },
                        "key": {
                            "type": "string",
                            "description": "State key; required for get, set, and delete."
                        },
                        "value": {
                            "description": "JSON-compatible value; required for set."
                        }
                    },
                    "required": ["action"]
                }
            }
        ],
        "isError": false
    };
});

// tools/call
JsonRpc2.register("tools/call", function (rpc, params, id, callback) {
    var _notify = typeof callback === "function" ? callback : console.log;
    var _getState = typeof rpc.getState === "function" ? function(key) { return rpc.getState(key); } : null; // `_getState(key);`
    var _setState = typeof rpc.setState === "function" ? function(key, value) { rpc.setState(key, value); } : null; // `_setState(key, state);`
    var _hasState = typeof rpc.hasState === "function" ? function(key) { return rpc.hasState(key); } : null;
    var _deleteState = typeof rpc.deleteState === "function" ? function(key) { return rpc.deleteState(key); } : null;
    var _listStateKeys = typeof rpc.listStateKeys === "function" ? function() { return rpc.listStateKeys(); } : null;
    var _clearState = typeof rpc.clearState === "function" ? function() { return rpc.clearState(); } : null;
    
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
                        var text = "";
                        
                        try {
                            if (!ALLOW_UNSAFE_EVAL && !allowUnsafeEval) {
                                throw new Error("Unsafe eval is not allowed. Please set ALLOW_UNSAFE_EVAL to true if you want to allow it.");
                            }
                            var evaluate = new Function("_notify", "_getState", "_setState", "_hasState", "_deleteState", "_listStateKeys", "_clearState", script);
                            text += String(evaluate(_notify, _getState, _setState, _hasState, _deleteState, _listStateKeys, _clearState));
                            if (console._messages.length > 0) {
                                text += "\r\n\r\n" + console._messages.join("\r\n");
                            }
                        } catch (e) {
                            isError = true;
                            text += "Error: " + e.message;
                        }
                        
                        return text;
                    })(params.arguments.script)
                }
            ],
            "isError": isError
        }
    }

    if (function_calling_name == "manage_state") {
        var argumentsObject = params && params.arguments ? params.arguments : {};
        var action = argumentsObject.action;
        var key = argumentsObject.key;
        var result;

        try {
            if (action == "get") {
                if (!_hasState(key)) throw new Error("State key does not exist");
                result = {"key": String(key), "value": _getState(key)};
            } else if (action == "set") {
                if (typeof key !== "string" || !("value" in argumentsObject)) throw new Error("set requires key and value");
                _setState(key, argumentsObject.value);
                result = {"key": key, "value": argumentsObject.value};
            } else if (action == "delete") {
                result = {"key": String(key), "deleted": _deleteState(key)};
            } else if (action == "list") {
                result = {"keys": _listStateKeys()};
            } else if (action == "clear") {
                result = {"deleted": _clearState()};
            } else {
                throw new Error("Unknown state action: " + action);
            }
            return {"content": [{"type": "text", "text": JSON.stringify(result)}], "isError": false};
        } catch (e) {
            return {"content": [{"type": "text", "text": "Error: " + e.message}], "isError": true};
        }
    }
});

exports.main = main;
