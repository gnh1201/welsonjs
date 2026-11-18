# WelsonJS Launcher OpenAI-compatible API

WelsonJS.Launcher answers OpenAI-style chat completions for its local llama.cpp models and for the web chat services that it drives in its browser, embeddings for its local embedding models, and typed decisions for its local CrispEmbed decision models. OpenAI clients and SDKs can use it with the base URL `http://localhost:3000/v1` (the launcher's `ResourceServerPrefix` plus `v1`).

## Access

The API runs local processes and the user's signed-in chat sessions, so only callers on this machine may use it (others get `403`):

- **Local scripts and command-line clients** (Python, PowerShell, curl): allowed without a key.
- **Other local software built on a browser engine** (Electron and similar apps, or tools that send browser headers): their requests look like requests from another site (`Sec-Fetch-Site: cross-site`), which the launcher rejects so that web pages cannot drive the API. Set an API key and configure it in the software:

  ```xml
  <!-- WelsonJS.Launcher.exe.config -->
  <add key="ResourceServerApiKey" value="oss@catswords.re.kr"/>
  ```

  The software then sends `Authorization: Bearer <key>` (the API key field of OpenAI clients). The default value, `oss@catswords.re.kr`, is an example key and is public: replace it with your own (16 characters or more). `ResourceServerApiKey` is the launcher's API key; for now only the OpenAI-compatible API (`/v1/...`) accepts it, and only from this machine. Generate one with PowerShell: `[Convert]::ToBase64String((1..24 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])`. Restart the launcher after changing the setting.
- **A web page on another origin** that calls the API from the browser: add its origin to `ResourceServerAllowOrigins` (comma-separated), which also answers its CORS preflight.

## Models

| Kind | Models | Answered by |
| --- | --- | --- |
| llama.cpp | The models in `LlamaCppModels` that are installed, and any other installed model folder (see [llamacpp-launcher-api.md](llamacpp-launcher-api.md)) | The launcher's llama.cpp router, with the prompt translation for `LlamaCppTranslatedModels` (FrogNano, BitNet) |
| Web chat | `copilot`, `chatgpt`, `grok`, `gemini`, `deepseek`, `perplexity`, `metaai`, `claude`, `mistral`, `qwen`, `kimi`, `zai` (from `webchat.js`) | The service's page in the launcher's browser, through the DevTools protocol |
| llama.cpp embeddings | The installed llama.cpp embedding models, such as `embeddinggemma_2` (the original google/embeddinggemma-2; `"type": "embedding"`) | The model's own `llama-server --embedding` (see [llamacpp-launcher-api.md](llamacpp-launcher-api.md#embeddings)) |
| CrispEmbed | The installed CrispEmbed models (see [crispembed-launcher-api.md](crispembed-launcher-api.md)): decision models such as `embeddinggemma_2_typed_decision` for `/v1/decisions`, embedding models for `/v1/embeddings` (`"type": "decision"` or `"embedding"` in `GET /v1/models`) | The model's `crispembed-server` |

`GET /v1/models` lists them:

```json
{"object": "list", "data": [
  {"id": "hy_mt2_1_8b_1_25bit", "object": "model", "created": 0, "owned_by": "llamacpp", "name": "hy_mt2_1_8b_1_25bit"},
  {"id": "chatgpt", "object": "model", "created": 0, "owned_by": "webchat", "name": "ChatGPT"}
]}
```

## Chat completions

```
POST /v1/chat/completions
{"model": "chatgpt", "messages": [{"role": "user", "content": "Hello"}]}
```

The response is an OpenAI `chat.completion` object; the answer is `choices[0].message.content`.

- **llama.cpp models:** the request is handled as `POST /llamacpp/chat`: the generation parameters (`temperature`, `top_p`, `top_k`, `max_tokens`, `presence_penalty`, `frequency_penalty`, `stop`, `seed`, `chat_template_kwargs`) and the optional `language` and `translate` fields apply, and the llama-server response is returned.
- **Web chat services:**
  - A single user message is sent as it is. A conversation is sent as one message, since a chat page receives one message per turn: system messages as notes at the top, then each message as `[role]` and its content.
  - The launcher uses an open page of the service, or opens one in an app window of its browser (`--app`: no tabs or address bar, 1000×800, like the editor's popup window), starting the browser with remote debugging when it is not running. The window may stay behind other windows, but do not minimize it while it answers: a minimized page stops rendering. Sign in to the service in that window when it asks; the launcher waits for the message box for 3 minutes.
  - Location access is granted in advance to the service's sites (`Browser.grantPermissions`), so the page does not stop at a permission prompt. The grant lasts while the browser runs and is not saved in the profile.
  - One request runs at a time per service. An answer can take up to 5 minutes (DeepSeek: 10 minutes).
  - Generation parameters do not apply, and `usage` is reported as 0.
- `content` can also be an array of parts; the text parts are joined.
- Responses are not streamed. With `"stream": true` the finished answer is sent as one server-sent event chunk, a chunk with the finish reason, and `data: [DONE]`.

Errors use the OpenAI error object, `{"error": {"message", "type", "param", "code"}}`: `400` for an invalid request, `404` (`model_not_found`) for an unknown or missing model, `504` when the model does not answer in time, and `503` for other failures (for example a chat page that could not be used).

## Decisions

```
POST /v1/decisions
{"model": "embeddinggemma_2_typed_decision",
 "state": {"task": "Delete old branches", "constraint_violations": 1},
 "questions": {"review": {"type": "noul", "instructions": "Does this require human review?"}}}
```

A WelsonJS extension in the style of this API (OpenAI has no decisions endpoint) for typed-decision models (`"type": "decision"` in `GET /v1/models`). `questions` maps a name to a `choice`, `score` or `noul` (yes or no) question, and `workflow` is optional. The response is `crispembed-server`'s answer, with `kind`, `label`, `value`, `confidence`, `probabilities` and `expected_level` for each question; see [crispembed-launcher-api.md](crispembed-launcher-api.md#typed-decisions).

## Embeddings

```
POST /v1/embeddings
{"model": "embeddinggemma_2", "input": ["task: search result | query: capital of France", "title: none | text: Paris is the capital of France."]}
```

The model must be an installed embedding model (`"type": "embedding"` in `GET /v1/models`): a llama.cpp embedding model such as `embeddinggemma_2`, the original google/embeddinggemma-2 in the WelsonJS llama.cpp build, or a CrispEmbed embedding model. A decision model is answered with `400` (`model_not_supported`), because its encoder is fine-tuned for its decision head. The response is the OpenAI embeddings object, with `data[i].embedding`; `input` is a string or an array of strings, and `dimensions` asks for a smaller output size (for `embeddinggemma_2`: 768, 512, 256 or 128, applied by the launcher). EmbeddingGemma 2 expects its task prompts in the text; see [llamacpp-launcher-api.md](llamacpp-launcher-api.md#embeddings).

Decision and embedding bodies are passed to the model's server as they were sent (it is started on demand). Errors use the OpenAI error object, as above.

## Examples

```python
from openai import OpenAI

client = OpenAI(base_url="http://localhost:3000/v1", api_key="unused")
reply = client.chat.completions.create(model="frognano_4b_2609", messages=[{"role": "user", "content": "Hello"}])
print(reply.choices[0].message.content)
```

```powershell
$body = @{ model = "claude"; messages = @(@{ role = "user"; content = "Hello" }) } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://localhost:3000/v1/chat/completions -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes($body))
```

## Web chat services (`webchat.js`)

`native/WelsonJS.Augmented/WelsonJS.Launcher/webchat.js` defines the services: the page URL and hosts, the message box and send button selectors, and the scripts that put the text into the message box and read the answer. The launcher evaluates it in its own JavaScript engine (ChakraCore), and the editor loads it (`/webchat.js`) for its AI service toggle; the editor's web chat requests also use `/v1/chat/completions`. The web pages change over time; update the service entry when one does.
