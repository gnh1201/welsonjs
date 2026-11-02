# WelsonJS Launcher llama.cpp API

WelsonJS.Launcher exposes a local HTTP API for external applications to start and manage `llama-server.exe` processes.

The default launcher prefix is `http://localhost:3000/`.

## Start

```http
POST /llamacpp/start
Content-Type: application/json

{
  "modelPath": "C:\\Users\\user\\AppData\\Roaming\\WelsonJS\\qwen3guard_06b\\Qwen3Guard-Gen-0.6B.Q4_K_M.gguf",
  "port": 0,
  "contextSize": 4096,
  "threads": 0
}
```

`port: 0` asks the launcher to allocate an available localhost port. The response is authoritative; clients must use the returned `host` and `port` when calling llama.cpp. `executablePath` is no longer a free-form executable location and must resolve to the installed `%APPDATA%\WelsonJS\llamacpp\llama-server.exe`.

The response contains a launcher-managed `id`, process ID, port, model path, and `running`/`portListening` state.

## List and status

```http
GET /llamacpp/status
GET /llamacpp/list
```

Both endpoints return the currently managed llama.cpp processes. The launcher removes exited processes from its managed list.

## Stop

```http
POST /llamacpp/stop
Content-Type: application/json

{
  "id": "launcher-managed-process-id"
}
```

Only processes started through this launcher instance can be stopped through this API. A port already in use is rejected when starting a new process.

## LLM guard

The Qwen3Guard classifier is not started through `/llamacpp/start`. The launcher runs it on demand behind its own endpoint and does not list it here:

```http
POST /llm-guard/inspect
Content-Type: application/json

{
  "text": "data that is about to be sent to an LLM"
}
```

The response is `{"label": "safe|controversial|unsafe", "categories": [...], "raw": "..."}`; any failure is `503` with `{"error": "..."}`. `GET /llm-guard/status` reports the model path, whether it is installed, and the guard process. See [llm-guard-flow.en.md](llm-guard-flow.en.md) for the policy.

## Security and lifecycle

- The API is intended for the local launcher HTTP listener.
- JavaScript clients must request startup through this API; they must not launch `llama-server.exe` directly.
- Arbitrary command arguments are not accepted; the launcher builds a fixed llama.cpp command line.
- The launcher owns the child `llama-server.exe` process and can terminate it by managed ID. Managed processes, including the LLM guard, are stopped when the launcher exits normally.
- The API does not expose a non-loopback bind address.
