# LLM data egress policy

WelsonJS LLM calls made through `lib/language-inference-engine.js`, and MCP tool results returned by `mcploader.js`, are gated by the LLM guard built into WelsonJS.Launcher (`POST /llm-guard/inspect`, called through `lib/llm-guard.js`).

1. Qwen3Guard-Gen-0.6B is run locally through llama.cpp by the launcher, on a launcher-allocated localhost port.
2. `Safe` content may be sent to the selected remote provider or returned to the MCP client.
3. `Controversial` content must pass a user-supplied anonymizer. Until one is registered with `llm-guard.setAnonymizer`, it is blocked.
4. `Unsafe` content is never sent remotely: LLM requests are routed only to the local `llamacpp` provider, and MCP tool results are replaced by an error.
5. Content with a `Jailbreak` category is blocked under any label, including `Controversial`: an anonymizer has nothing to mask in a prompt injection, so it cannot make it safe. LLM requests stop (they are not routed to the local model either), and MCP tool results are replaced by an error.
6. A launcher that is not running, a missing guard model, a failed local model, or a malformed or unknown verdict fails closed and prevents the transmission.

The guard is on by default. It is switched off on the JavaScript side with `ALLOW_UNSAFE_OUTBOUND = true` in `app.js` or `require("lib/llm-guard").setEnabled(false)` at runtime; data then leaves unchecked, and `inspect()` reports `"unchecked"` instead of a verdict. Despite the general name, the switch only affects the two paths above; other outbound requests are not checked either way.

The model can be installed with the post-install component `qwen3guard_06b` together with `llamacpp`. The launcher reads the model location from the `LlmGuardModelPath` setting (`app.config`, falling back to the built-in resource default). The default path is:

`%APPDATA%\WelsonJS\qwen3guard_06b\Qwen3Guard-Gen-0.6B.Q4_K_M.gguf`

The anonymizer is deliberately an injection point rather than a regex implementation. It must be implemented and reviewed for the application's data types before enabling `Controversial` traffic.

See [llm-guard-flow.en.md](llm-guard-flow.en.md) for the full flow.
