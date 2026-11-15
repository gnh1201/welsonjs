# WelsonJS Launcher llama.cpp API

WelsonJS.Launcher runs one `llama-server.exe` in **router mode** and exposes a local HTTP API for it. The router loads models on demand and unloads the least recently used model when too many are loaded (model hot swap). The editor's Local AI, the LLM guard, and `lib/llamacpp.js` share this one router.

The default launcher prefix is `http://localhost:3000/`. The API starts and stops local processes, so only local callers may use it (other callers get `403`).

## Models

The router reads models from `%APPDATA%\WelsonJS` (`--models-dir`). Each folder there that contains a `.gguf` file is a model named after the folder, which is the installer component:

```
%APPDATA%\WelsonJS
 ├─ hy_mt2_1_8b_1_25bit\Hy-MT2-1.8B-1.25Bit.gguf    → model "hy_mt2_1_8b_1_25bit"
 ├─ frognano_4b_2609\frognano-4b-2609-q4_k_m.gguf   → model "frognano_4b_2609"
 ├─ bitnet_b1_58_2b_4t\ggml-model-i2_s.gguf         → model "bitnet_b1_58_2b_4t"
 ├─ qwen3guard_06b\Qwen3Guard-Gen-0.6B.Q4_K_M.gguf  → model "qwen3guard_06b" (the LLM guard)
 └─ bin\, llamacpp\, ...                            → no .gguf, not a model
```

- The router looks one level deep: a `.gguf` file in a model folder's subfolder is not found.
- A model folder should hold one model file (plus `mmproj*` files or the shards of one model); with several, the router uses one of them.
- The router reads the folder when it starts. Restart the launcher after installing a model.

**Every request that uses a model names it** (`"model": "hy_mt2_1_8b_1_25bit"`). Model names contain only letters, digits, `_`, `.`, and `-`.

## Settings (`WelsonJS.Launcher.exe.config`)

| Key | Default | Meaning |
| --- | --- | --- |
| `LlamaCppModels` | `hy_mt2_1_8b_1_25bit,frognano_4b_2609,bitnet_b1_58_2b_4t` | The models the editor offers as Local AI, comma-separated, in the order of its ribbon toggle; reported as `models` by `/llamacpp/status` |
| `LlmGuardModel` | `qwen3guard_06b` | The LLM guard's model |
| `LlamaCppModelsMax` | `2` | Models loaded at once (`--models-max`); beyond it the least recently used model is unloaded. `0` means no limit |
| `LlamaCppContextSize` | `8192` | Context size of every model instance (`--ctx-size`) |

With the default of 2, the model in use and the LLM guard stay loaded together; switching between Hy-MT2, FrogNano and BitNet in the editor swaps them.

## Endpoints

### Status

```http
GET /llamacpp/status
```

```json
{
  "server": {"running": true, "pid": 1234, "host": "127.0.0.1", "port": 50123, "baseUrl": "http://127.0.0.1:50123",
             "modelsDirectory": "C:\\Users\\user\\AppData\\Roaming\\WelsonJS", "modelsMax": 2, "contextSize": 8192, ...},
  "models": [{"id": "hy_mt2_1_8b_1_25bit", "installed": true}, {"id": "frognano_4b_2609", "installed": true},
             {"id": "bitnet_b1_58_2b_4t", "installed": true}]
}
```

`models` lists `LlamaCppModels` with whether each model is installed. Does not start the router.

### Models

```http
GET /llamacpp/models
```

Starts the router if needed and returns its model list (`GET /models` of llama-server) without the LLM guard's model, which is not a chat model:

```json
{
  "models": [{"id": "hy_mt2_1_8b_1_25bit", "path": "...", "status": {"value": "loaded"}, ...}]
}
```

`status.value` is `unloaded`, `loading`, `loaded`, or `sleeping`; a model that failed to load is `unloaded` with `"failed": true` and its `exit_code`.

### Start and stop

```http
POST /llamacpp/start
POST /llamacpp/stop
Content-Type: application/json

{}
```

`start` starts the router and returns `{"server": {...}}`; requests start it on demand too. `stop` stops the router and every model instance.

### Load and unload

```http
POST /llamacpp/load
POST /llamacpp/unload
Content-Type: application/json

{"model": "hy_mt2_1_8b_1_25bit"}
```

`load` loads the model and waits until it can serve (up to 3 minutes); it answers `{"model": {...}, "server": {...}}`. `unload` answers `{"model": "...", "unloaded": true|false}`. A model that is not installed answers `404`.

### Chat

```http
POST /llamacpp/chat
Content-Type: application/json

{
  "model": "hy_mt2_1_8b_1_25bit",
  "messages": [{"role": "user", "content": "Translate into Korean: Good morning."}],
  "temperature": 0
}
```

An OpenAI-compatible, non-streaming chat completion. `model` is required. The launcher loads the model if needed and returns the llama-server response (status code and body) unchanged. The passed-through parameters are `temperature`, `top_p`, `top_k`, `max_tokens`, `presence_penalty`, `frequency_penalty`, `stop`, `seed`, and `chat_template_kwargs` (for example `{"enable_thinking": false}` for reasoning models).

### Direct requests to the router

Clients may also send OpenAI-compatible requests to the router itself, at the `baseUrl` from `/llamacpp/status` (or `/llamacpp/start`), for example `POST {baseUrl}/v1/chat/completions`. The router requires `"model"` in POST bodies and `?model=` in GET requests, and loads the model on demand. `lib/language-inference-engine.js` does this for the `llamacpp` provider.

## LLM guard

The Qwen3Guard classifier runs in the same router as the model `LlmGuardModel`, behind its own endpoint:

```http
POST /llm-guard/inspect
Content-Type: application/json

{
  "text": "data that is about to be sent to an LLM"
}
```

The response is `{"label": "safe|controversial|unsafe", "categories": [...], "raw": "..."}`; any failure is `503` with `{"error": "..."}`. `GET /llm-guard/status` reports the guard model, its file, whether it is installed, and the router state. See [llm-guard-flow.en.md](llm-guard-flow.en.md) for the policy.

## JavaScript client (`lib/llamacpp.js`)

```js
var LlamaCpp = require("lib/llamacpp");
var llama = new LlamaCpp().setModel("hy_mt2_1_8b_1_25bit");
llama.loadModel();                       // POST /llamacpp/load
var reply = llama.chat([{"role": "user", "content": "Hello"}], {"temperature": 0});
console.log(reply.choices[0].message.content);
llama.listModels();                      // GET /llamacpp/models
llama.getConfiguredModels();             // LlamaCppModels, from GET /llamacpp/status
llama.getBaseUrl();                      // the router, for direct requests with "model"
```

See [examples/open_llamacpp.js](../examples/open_llamacpp.js).

## Security and lifecycle

- Only local callers may use the API.
- JavaScript clients must go through this API; they must not launch `llama-server.exe` directly.
- Only the installed `%APPDATA%\WelsonJS\llamacpp\llama-server.exe` is run, with a fixed command line; arbitrary arguments and model paths are not accepted. A model is selected by name, and the name is checked before it is used.
- The router binds to `127.0.0.1` on a launcher-allocated port.
- The router and its model instances are stopped when the launcher exits normally.
