# LLM data egress policy

WelsonJS LLM calls made through `lib/language-inference-engine.js` are gated by the local `lib/llm-guard.js` module.

1. Qwen3Guard-Gen-0.6B is run locally through llama.cpp on `127.0.0.1:8081`.
2. `Safe` content may be sent to the selected remote provider.
3. `Controversial` content must pass a user-supplied anonymizer. Until one is registered with `llm-guard.setAnonymizer`, it is blocked.
4. `Unsafe` content is routed only to the local `llamacpp` provider; it is never sent to a remote provider.
5. A missing guard, failed local model, malformed verdict, or unknown verdict fails closed and prevents the remote request.

The model can be installed with the post-install component `qwen3guard_06b` together with `llamacpp`. The default model path is:

`%APPDATA%\welsonjs\qwen3guard_06b\Qwen3Guard-Gen-0.6B.Q4_K_M.gguf`

The anonymizer is deliberately an injection point rather than a regex implementation. It must be implemented and reviewed for the application's data types before enabling `Controversial` traffic.
