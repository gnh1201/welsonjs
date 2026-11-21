# WelsonJS Launcher CrispEmbed API

WelsonJS.Launcher runs [CrispEmbed](https://github.com/CrispStrobe/CrispEmbed) (`crispembed-server.exe`, MIT) and exposes a local HTTP API for it. CrispEmbed runs GGUF encoder models on the CPU with ggml: text embeddings, and typed decisions with a model that has a decision head. Where llama.cpp generates text, CrispEmbed turns text into vectors and calibrated answers.

The default launcher prefix is `http://localhost:3000/`. The API starts and stops local processes, so only local callers may use it (other callers get `403`).

## Installation

Two setup components install it (custom installation):

| Component | Installs |
| --- | --- |
| `crispembed` | The WelsonJS build of CrispEmbed 0.17.20.ad01b2.1 (commit `ad01b21`) for Windows x86_64 (CPU, AVX2 and FMA required), from the artifacts package, into `%APPDATA%\WelsonJS\crispembed\`. It is built from a pinned commit in CI and signed with the other binaries ([native/crispembed](../native/crispembed/README.md)). There is no ARM64 or 32-bit Windows build; on ARM64 Windows the x64 build runs under emulation. |
| `crispembed\embeddinggemma_2_typed_decision` | [EmbeddingGemma 2 fine-tuned for typed decisions](https://huggingface.co/cstr/embeddinggemma-2-ft-typed-decision-GGUF), Q8_0 (308 MB, Apache-2.0), into `%APPDATA%\WelsonJS\crispembed_models\embeddinggemma_2_typed_decision\`. Selecting it also selects `crispembed`. |

The model download is pinned to a repository revision and checked against `data/DownloadHashes.txt`.

## Models

A model is a folder under `%APPDATA%\WelsonJS\crispembed_models` named after the installer component, holding one `.gguf` file:

```
%APPDATA%\WelsonJS
 ├─ crispembed\crispembed-server.exe, *.dll
 └─ crispembed_models
     └─ embeddinggemma_2_typed_decision\embeddinggemma-2-ft-typed-decision-q8_0.gguf
                                       → model "embeddinggemma_2_typed_decision"
```

The models are kept out of `%APPDATA%\WelsonJS` itself, where the llama.cpp router lists every folder with a `.gguf` file as one of its models. **Every request that uses a model names it**; model names contain only letters, digits, `_`, `.`, and `-`.

A model is one of two kinds:

| Kind | Models | Answers |
| --- | --- | --- |
| Decision | The models in `CrispEmbedDecisionModels` (`embeddinggemma_2_typed_decision`) | Typed decisions only: `POST /v1/decisions`, `POST /crispembed/decide` |
| Embedding | Every other installed model | Embeddings only: `POST /v1/embeddings`, `POST /crispembed/embed` |

A decision model's encoder is fine-tuned together with its decision head, so its vectors are not general-purpose embeddings, and the launcher does not offer them as such (an embedding request for a decision model is answered with `400`). Embeddings come from an embedding model: for EmbeddingGemma 2 that is the original [google/embeddinggemma-2](https://huggingface.co/google/embeddinggemma-2), a separate model. CrispEmbed 0.17.20.ad01b2.1 does not load it (it supports EmbeddingGemma-300m and the decision fine-tune), so it runs in the WelsonJS llama.cpp build instead: the setup component `llamacpp\embeddinggemma_2`, model `embeddinggemma_2` (see [llamacpp-launcher-api.md](llamacpp-launcher-api.md#embeddings)). Setup installs no CrispEmbed embedding model.

## Settings (`WelsonJS.Launcher.exe.config`)

| Key | Default | Meaning |
| --- | --- | --- |
| `CrispEmbedModels` | `embeddinggemma_2_typed_decision` | Models listed first by `/crispembed/models`, comma-separated |
| `CrispEmbedDecisionModels` | `embeddinggemma_2_typed_decision` | Typed-decision models, comma-separated; every other model is an embedding model |
| `CrispEmbedModelsMax` | `2` | CrispEmbed servers running at once; starting another stops the least recently used server that is not answering a request |

## Processes

`crispembed-server` serves one embedding model per process, so the launcher runs one server per model. A server starts on the first request that names its model and keeps running until the launcher exits, it is stopped, or `CrispEmbedModelsMax` makes room for another model.

Every local server of the launcher, the llama.cpp router included, is run the same way (`LocalServerProcess`): a launcher-allocated port on `127.0.0.1`, a start that waits until the server is ready and is retried once on a new port if the process exits while starting, the launcher's job object (the server ends with the launcher however it ends), and the last lines of its output kept for errors and status. `GET /localservers/status` reports all of them.

Each CrispEmbed server runs with `--offline` (no model download), `--model-root` set to the models folder, and `--image-root` set to an empty folder, so a request sent to its port cannot make it read files elsewhere. The launcher sets `CRISPEMBED_GEMMA_DECISION=1` (the typed-decision backend is opt-in) and `CRISPEMBED_GEMMA_DECISION_F32_DOT=1` (the Q8_0 model reproduces its published results with FP32 activations); neither changes other models.

## Endpoints

### Status and models

```http
GET /crispembed/status
GET /crispembed/models
```

`/crispembed/status` returns `{"server": {...}, "models": [...]}`: the executable and whether it is installed, the models folder, `modelsMax`, and `servers`, the process state of each server started so far (`running`, `pid`, `baseUrl`, `lastUsedUtc`, `activeUses`, `lastExitCode`, `recentOutput`, ...). `/crispembed/models` returns `{"models": [{"id", "kind", "configured", "installed", "running"}, ...]}`, where `kind` is `decision` or `embedding`.

### Start and stop

```http
POST /crispembed/start
{"model": "embeddinggemma_2_typed_decision"}

POST /crispembed/stop
{"model": "embeddinggemma_2_typed_decision"}
```

`start` waits until the model is loaded (up to 2 minutes) and returns `{"model", "health"}`, where `health` is the server's `/health` report (`dim`, and `typed_decision: true` for a decision model). Requests start the server on demand, so `start` is only needed to load a model in advance. `stop` with `{}` stops every CrispEmbed server.

### Embeddings

```http
POST /crispembed/embed
{"model": "<an embedding model>", "input": ["first text", "second text"]}
```

The model must be an embedding model. The body is the OpenAI embeddings request (`input` is a string or an array of strings, `dimensions` is optional) and the response is the OpenAI embeddings object (`data[i].embedding`). The same request is accepted at `POST /v1/embeddings` (see [openai-compatible-api.md](openai-compatible-api.md)).

### Typed decisions

```http
POST /crispembed/decide
{
  "model": "embeddinggemma_2_typed_decision",
  "state": {"task": "Delete old branches", "constraint_violations": 1},
  "questions": {
    "review": {"type": "noul", "instructions": "Does this require human review?"}
  }
}
```

The model must be a decision model; the same request is accepted at `POST /v1/decisions`. `questions` maps a name to a question of type `choice` (a criteria mapping or list), `score` (ordered levels or numeric-key criteria) or `noul` (yes or no); `workflow` is optional. Each answer carries `kind`, `label`, `value` (a string, integer or boolean), `confidence`, `probabilities` and `expected_level`. The model's head was trained on English schemas; it is a small decision model, not a general classifier, so check it on your own cases before relying on it. See CrispEmbed's [usage and limits](https://github.com/CrispStrobe/CrispEmbed/blob/main/docs/embeddinggemma2-typed-decision.md).

Embedding and decision request bodies are passed to `crispembed-server` exactly as they were sent (it ignores `"model"`): a decision renders the state and questions in their JSON key order, which parsing and serializing the body again could change.

Errors are `{"error": "..."}`: `400` for an invalid request, `404` when the model or `crispembed-server.exe` is not installed, `504` when the server does not answer in time, and `503` for other failures (for example a server that exited while starting, with its exit code and last output). Errors from `crispembed-server` are returned with its status code.

## JavaScript client (`lib/crispembed.js`)

```js
var CrispEmbed = require("lib/crispembed");
var crisp = new CrispEmbed().setModel("embeddinggemma_2_typed_decision");

var result = crisp.decide(
    {"task": "Delete old branches", "constraint_violations": 1},
    {"review": {"type": "noul", "instructions": "Does this require human review?"}}
);                                                          // POST /crispembed/decide
console.log(JSON.stringify(result));

// With an embedding model installed:
// var vectors = crisp.embed(["first text", "second text"], {"model": "<an embedding model>"});
// console.log(crisp.cosine(vectors[0], vectors[1]));     // POST /crispembed/embed

crisp.listModels();                                         // GET /crispembed/models
crisp.stop();                                               // POST /crispembed/stop
```

## Security and lifecycle

- Only local callers may use the API. Only the embedding and decision routes of `crispembed-server` are forwarded; its OCR, vision and other routes are not.
- JavaScript clients must go through this API; they must not launch `crispembed-server.exe` directly.
- Only the installed `%APPDATA%\WelsonJS\crispembed\crispembed-server.exe` is run, with a fixed command line. A model is selected by name, and the name is checked before it is used.
- `crispembed-server` has no authentication of its own. It binds to `127.0.0.1` on a launcher-allocated port, with the file paths it reads confined as described above.
- The servers are stopped when the launcher exits normally, and the job object ends them when it does not.
