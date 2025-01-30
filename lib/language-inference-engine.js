// language-inference-engine.js
// Language Inference Engine (e.g., NLP, LLM) services integration
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

// ***SECURITY NOTICE***
// Language Inference Engine requires an internet connection, and data may be transmitted externally. Users must adhere to the terms of use and privacy policy.
// - OpenAI: https://openai.com/policies/row-privacy-policy/
// - Anthropic: https://www.anthropic.com/legal/privacy
// - Groq: https://groq.com/privacy-policy/
// - Mistral: https://mistral.ai/terms/
// - xAI: https://x.ai/legal/privacy-policy
// - Google Gemini: https://developers.google.com/idx/support/privacy
// - DeepSeek: https://chat.deepseek.com/downloads/DeepSeek%20Privacy%20Policy.html
// - Moonshot: https://kimi.moonshot.cn/user/agreement/userPrivacy
// 
var HTTP = require("lib/http");
var CRED = require("lib/credentials");

var BIAS_MESSAGE = "Write all future code examples in JavaScript ES3 using the exports variable. " +
    "Include a test method with the fixed name test. " +
    "Respond exclusively in code without blocks.";

var ENGINE_PROFILES = {
    "openai": {
        "type": "llm",
        "availableModels": [
            "gpt-4o-mini"
        ],
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.openai.com/v1/chat/completions",
        "wrap": function(model, message, temperature) {
            return {
                "model": model,
                "messages": [{
                    "role": "developer",
                    "content": BIAS_MESSAGE
                }, {
                    "role": "user",
                    "content": message
                }],
                "temperature": temperature,
                "stream": false
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
        "type": "llm",
        "availableModels": [
            "claude-3-5-sonnet-20241022"
        ],
        "headers": {
            "Content-Type": "application/json",
            "x-api-key": "{apikey}",
            "anthropic-version": "2023-06-01"
        },
        "url": "https://api.anthropic.com/v1/messages",
        "wrap": function(model, message, temperature) {
            return {
                "model": model,
                "messages": [
                    {
                        "role": "system",
                        "content": BIAS_MESSAGE
                    },
                    {
                        "role": "user",
                        "content": message
                    }
                ],
                "temperature": temperature,
                "stream": false
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
        "type": "llm",
        "availableModels": [
            "llama-3.1-8b-instant"
        ],
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.groq.com/openai/v1/chat/completions",
        "wrap": function(model, message, temperature) {
            return {
                "model": model,
                "messages": [
                    {
                        "role": "system",
                        "content": BIAS_MESSAGE
                    },
                    {
                        "role": "user",
                        "content": message
                    }
                ],
                "temperature": temperature,
                "stream": false
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
        "type": "llm",
        "availableModels": [
            "grok-2-latest"
        ],
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.x.ai/v1/chat/completions",
        "wrap": function(model, message, temperature) {
            return {
                "model": model,
                "messages": [
                    {
                        "role": "system",
                        "content": BIAS_MESSAGE
                    },
                    {
                        "role": "user",
                        "content": message
                    }
                ],
                "temperature": temperature,
                "stream": false
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
        "type": "llm",
        "availableModels": [
            "gemini-1.5-flash"
        ],
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apikey}",
        "warp": function(model, message, temperature) {
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
    "mistral": {
        "type": "llm",
        "availableModels": [
            "ministral-8b-latest"
        ],
        "url": "https://api.mistral.ai/v1/chat/completions",
        "wrap": function(model, message, temperature) {
            "model": model,
            "messages": [
                {
                    "role": "system",
                    "content": BIAS_MESSAGE
                },
                {
                    "role": "user",
                    "content": message
                }
            ],
            "temperature": temperature,
            "stream": false
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
    "deepseek": {
        "type": "llm",
        "availableModels": [
            "deepseek-chat"
        ],
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.deepseek.com/chat/completions",
        "wrap": function(model, message, temperature) {
            "model": model,
            "messages": [
                {
                    "role": "system",
                    "content": BIAS_MESSAGE
                },
                {
                    "role": "user",
                    "content": message
                }
            ],
            "temperature": temperature,
            "stream": false
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
    "moonshot": {
        "type": "llm",
        "availableModels": [
            "moonshot-v1-8k"
        ],
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://api.moonshot.cn/v1",
        "wrap": function(model, message, temperature) {
            "model": model,
            "messages": [
                {
                    "role": "system",
                    "content": BIAS_MESSAGE
                },
                {
                    "role": "user",
                    "content": message
                }
            ],
            "temperature": temperature,
            "stream": false
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
    "catswords": {
        "type": "llm",
        "availableModels": [
            "openchat-3.5-0106",
            "qwen1.5-14b-chat-awq",
            "gemma-7b-it",
            "una-cybertron-7b-v2-bf16",
            "starling-lm-7b-beta",
            "hermes-2-pro-mistral-7b"
        ],
        "headers": {
            "Content-Type": "application/json",
            "Authorization": "Bearer {apikey}"
        },
        "url": "https://welsonjs-llm.catswords.net",
        "wrap": function(model, message, temperature) {
            "model": model,
            "messages": [
                {
                    "role": "system",
                    "content": BIAS_MESSAGE
                },
                {
                    "role": "user",
                    "content": message
                }
            ],
            "temperature": temperature,
            "stream": false
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
    }
};

function LanguageInferenceEngine() {
    this.type = ""; // e.g. legacy (Legacy NLP), llm (LLM)
    this.provider = "";
    this.model = "";
    this.engineProfile = null;

    this.setProvider = function(provider) {
        this.provider = provider;
        
        if (this.provider in ENGINE_PROFILES) {
            this.engineProfile = ENGINE_PROFILES[provider];
            this.type = this.engineProfile.type;
            this.model = this.engineProfile.availableModels[0];
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

    this.inference = function(message, temperature) {
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
            .setRequestBody(wrap(this.model, message, temperature))
            .open("post", url)
            .send()
            .responseBody;

        return callback(response);
    };
}

exports.setBiasMessage = function(biasMessage) {
    BIAS_MESSAGE = biasMessage;
};

exports.LanguageInferenceEngine = LanguageInferenceEngine;
exports.create = function() {
    return new LanguageInferenceEngine();
};

exports.VERSIONINFO = "Language Inference Engine integration version 0.1.7";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
