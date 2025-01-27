// language-inference-engine.js
// Language Inference Engine (e.g., NLP, LLM) services integration
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

// ***SECURITY NOTICE***
// Language Inference Engine requires an internet connection, and data may be transmitted externally. Users must adhere to the terms of use and privacy policy.
// - OpenAI: https://openai.com/policies/row-privacy-policy/
// - Anthropic: https://www.anthropic.com/legal/privacy
// - Groq: https://groq.com/privacy-policy/
// - xAI: https://x.ai/legal/privacy-policy
// - Google Gemini: https://developers.google.com/idx/support/privacy
// - DeepSeek: https://chat.deepseek.com/downloads/DeepSeek%20Privacy%20Policy.html
// 
var HTTP = require("lib/http");
var CRED = require("lib/credentials");

var biasMessage = "Write all future code examples in JavaScript ES3 using the exports variable. " +
    "Include a test method with the fixed name test. " +
    "Respond exclusively in code without blocks.";

var engineProfiles = {
    "openai": {
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.openai.com/v1/chat/completions",
        "wrap": function(model, message) {
            return {
                "model": model,
                "messages": [{
                    "role": "developer",
                    "content": biasMessage
                }, {
                    "role": "user",
                    "content": message
                }]
            };
        },
        "callback": function(response) {
            if ("error" in response) {
                return ["Error: " + response.error.message];
            } else {
                return response.choices.reduce(function(a, x) {
                    a.push(x.message.content);
                    
                    return a;
                }, []);
            }
        }
    },
    "anthropic": {
        "headers": {
            "Content-Type": "application/json",
            "x-api-key": "{apikey}",
            "anthropic-version": "2023-06-01"
        },
        "url": "https://api.anthropic.com/v1/messages",
        "wrap": function(model, message) {
            return {
                "model": model,
                "max_tokens": 1024,
                "messages": [
                    {
                        "role": "system",
                        "content": biasMessage
                    },
                    {
                        "role": "user",
                        "content": message
                    }
                ]
            };
        },
        "callback": function(response) {
            if ("error" in response) {
                return ["Error: " + response.error.message];
            } else {
                return response.content.reduce(function(a, x) {
                    if (x.type == "text") {
                        a.push(x.text);
                    } else {
                        a.push("Not supported type: " + x.type);
                    }
                    
                    return a;
                }, []);
            }
        }
    },
    "groq": {
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.groq.com/openai/v1/chat/completions",
        "wrap": function(model, message) {
            return {
                "model": model,
                "messages": [
                    {
                        "role": "system",
                        "content": biasMessage
                    },
                    {
                        "role": "user",
                        "content": message
                    }
                ]
            };
        },
        "callback": function(response) {
            if ("error" in response) {
                return ["Error: " + response.error.message];
            } else {
                return response.choices.reduce(function(a, x) {
                    a.push(x.message.content);
                    
                    return a;
                }, []);
            }
        }
    },
    "xai": {
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.x.ai/v1/chat/completions",
        "wrap": function(model, message) {
            return {
                "messages": [
                    {
                        "role": "system",
                        "content": biasMessage
                    },
                    {
                        "role": "user",
                        "content": message
                    }
                ],
                "model": model
            }
        },
        "callback": function(response) {
            return response.choices.reduce(function(a, x) {
                a.push(x.message.content);
                
                return a;
            }, []);
        }
    },
    "google": {
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apikey}",
        "warp": function(model, message) {
            return {
                "contents": [
                    {
                        "parts": [
                            {
                                "text": message
                            }
                        ]
                    }
                ]
            }
        },
        "callback": function(response) {
            if ("error" in response) {
                return ["Error: " + response.error.message];
            } else {
                return response.candidates.reduce(function(a, x) {
                    x.content.parts.forEach(function(part) {
                        if ("text" in part) {
                            a.push(part.text);
                        } else {
                            a.push("Not supported type");
                        }
                    });
                    
                    return a;
                }, []);
            }
        }
    },
    "deepseek": {
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.deepseek.com/chat/completions",
        "wrap": function(model, message) {
            "model": model,
            "messages": [
                {
                    "role": "system",
                    "content": biasMessage
                },
                {
                    "role": "user",
                    "content": message
                }
            ],
            "stream": false
        }
    },
    "callback": function(response) {
        if ("error" in response) {
            return ["Error: " + response.error.message];
        } else {
            return response.choices.reduce(function(a, x) {
                a.push(x.message.content);
                
                return a;
            }, []);
        }
    }
};

function LanguageInferenceEngine() {
    this.type = "llm"; // e.g. legacy (Legacy NLP), llm (LLM)
    this.provider = "";
    this.engineProfile = null;

    this.setProvider = function(provider) {
        this.provider = provider;

        if (provider in engineProfiles) {
            this.engineProfile = engineProfiles[provider];
        }

        return this;
    };

    this.setModel = function(model) {
        this.model = model;

        return this;
    };

    this.setEngineProfileURL = function(url) {
        if (this.engineProfile == null)
            return this;

        this.engineProfile.url = url;

        return this;
    }

    this.inference = function(message) {
        if (this.engineProfile == null)
            return this;

        var apikey = CRED.get("apikey", this.provider); // Get API key
        var headers = this.engineProfile.headers;
        var wrap = this.engineProfile.wrap;
        var url = this.engineProfile.url;
        var callback = this.engineProfile.callback;
        
        var response = HTTP.create("MSXML")
            .setVariables({
                "apikey": apikey
            })
            .setHeaders(headers)
            .setRequestBody(wrap(message))
            .open("post", url)
            .send()
            .responseBody;

        return callback(response);
    };
}

exports.LanguageInferenceEngine = LanguageInferenceEngine;
exports.create = function() {
    return new LanguageInferenceEngine();
};

exports.VERSIONINFO = "Language Inference Engine (NLP/LLM) integration version 0.1.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
