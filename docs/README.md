# WelsonJS documentation

Documents in this folder, by topic. Documents with a Korean version link it at the top.

## Getting started

| Document | What it covers |
| --- | --- |
| [Quick Start](quick-start.en.md) ([한국어](quick-start.md)) | Installing WelsonJS, using the code editor and its AI tools, and running a first script. |

## Launcher APIs

WelsonJS.Launcher serves these local HTTP APIs, by default at `http://localhost:3000/`.

| Document | What it covers |
| --- | --- |
| [OpenAI-compatible API](openai-compatible-api.md) | Chat completions for the local llama.cpp models and the web chat services the launcher drives, embeddings, and typed decisions. |
| [llama.cpp API](llamacpp-launcher-api.md) | The `llama-server.exe` router: models, settings, start and stop, loading and unloading, chat, embeddings, the LLM guard model and the `lib/llamacpp.js` client. |
| [CrispEmbed API](crispembed-launcher-api.md) | The CrispEmbed servers: installation, models, embeddings, typed decisions with GGUF encoder models and the `lib/crispembed.js` client. |

## API keys

| Document | What it covers |
| --- | --- |
| [API keys from Azure Key Vault](azure-key-vault.md) | Taking API keys from Azure Key Vault instead of key files, through the launcher's Azure sign-in. |

## Editor

| Document | What it covers |
| --- | --- |
| [Analyze Tactics validation samples](analyze-tactics-validation.md) | The reference malware (Stuxnet, Flame, Duqu, Mirai, Gootloader, BPFDoor) and further samples for the MITRE ATT&CK tactics analysis, the recorded results of FrogNano and the candidate models, and what to re-run when the prompts or the model change. |

## LLM data handling

| Document | What it covers |
| --- | --- |
| [LLM data egress policy](llm-data-policy.md) | How LLM calls and MCP tool results are checked by the launcher's LLM guard before data leaves the machine. |
| [LLM data flow and Qwen3Guard policy](llm-guard-flow.en.md) ([한국어](llm-guard-flow.md)) | The data flow to an LLM and the Qwen3Guard checks along it. |

## Dependencies

| Document | What it covers |
| --- | --- |
| [Modified vendor scripts](modified-vendor-scripts.md) | The third-party scripts in `app/assets/js` that are changed to run in Windows Script Host, and how. |
| [Third-party dependencies flagged by the OSS assessment](third-party-dependencies.md) | The components with known vulnerabilities reported by the 2026 open source assessment, and what was done about each. |

## Elsewhere in the repository

- [AGENTS.md](../AGENTS.md): architecture and contributor conventions.
- `README.md` files next to the code, for example in `native/chakracore/`, `native/crispembed/`, `native/llamacpp/` and the projects under `native/WelsonJS.Augmented/`.
