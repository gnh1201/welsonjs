---
name: welsonjs-mcp
description: Work with WelsonJS projects and the WelsonJS MCP server, especially when planning or writing Windows automation scripts for its evaluate_js tool.
metadata:
  short-description: Use WelsonJS through its MCP interface
---

# WelsonJS MCP and project work

Use this skill when a task involves this repository's `mcploader.js`, its MCP tools, or writing JavaScript to run in a WelsonJS environment. Treat the checked-in implementation as authoritative; this framework runs on Windows Script Host and exposes host capabilities, so code may interact with the user's desktop, files, processes, network, COM objects, and installed applications.

## Understand the runtime first

- Read `AGENTS.md` and inspect the relevant implementation under `lib/` before relying on an API. `README.md` gives the broad project map; `mcploader.js` documents the MCP surface.
- WelsonJS uses legacy WSH JScript. Prefer ES3/ES5-compatible syntax in root scripts and `lib/` unless the target file explicitly uses another engine. Do not assume Node.js APIs or modern JavaScript syntax are available.
- Project modules are commonly loaded with `require("lib/<module>")`. Confirm the module's exports and host requirements before using it. Some features need Windows, COM, optional software, credentials, network access, or elevated privileges.
- Each `evaluate_js` execution is independent; local variables and COM objects do not survive between calls. When a value or instance needs to be reused, save it with `_setState(key, value)` in one execution and retrieve it with `_getState(key)` in a later execution. These helpers are `null` when the JSON-RPC runtime does not provide state support, so check availability before calling them. Reacquire objects that cannot be preserved as state.

## Use the MCP tools accurately

The server is a stdio JSON-RPC server exposed by `mcploader.js`. It advertises protocol version `2025-11-25`, server name `WelsonJS MCP` (version `1.1.0`), and the `io.modelcontextprotocol/ui` extension for `text/html;profile=mcp-app`. Its registered tools currently include:

- `add_both_numbers`: adds numeric arguments `a` and `b`.
- `evaluate_js`: executes the `script` argument through `new Function` and returns its string result and captured console messages. Set `allowUnsafeEval: true` for the call unless the host has enabled the global `ALLOW_UNSAFE_EVAL` setting. The checked-in `app.js` defaults that setting to `false`. The function receives `_notify`, `_getState`, and `_setState`; state helpers are `null` when the JSON-RPC runtime does not provide state support.
- `evaluate_js_es3`: deprecated alias; use `evaluate_js`.

Both evaluation tools declare a `timeout` argument with a 180,000–900,000 ms schema range and a 900,000 ms default. The current `mcploader.js` implementation does not read or enforce that argument, so do not rely on it to interrupt a long-running script.

Do not imply that unsafe evaluation has been enabled by the skill or by the MCP tool description. If execution is rejected, explain that the host's unsafe-eval setting or explicit call argument is required; do not try to change host configuration to bypass it.

When writing an `evaluate_js` script:

- Keep the script focused on the requested task and make host-visible effects clear. Prefer visible UI actions when practical.
- Do not write files or persist intermediate data unless the user asked for that outcome.
- Use `_notify(message)` for progress or diagnostic output. Avoid `console.log`; the loader supplies `_notify` to the script and separately returns collected console messages.
- Use ASCII source text. Encode non-ASCII string content with JavaScript Unicode escapes such as `"\\uC548\\uB155"`, as required by the MCP tool guidance in `mcploader.js`.
- Use `require("lib/shell")` for supported shell operations and `require("lib/msoffice")` for Office automation; inspect those modules before selecting methods.
- The schema's `timeout` field is not currently enforced by `mcploader.js`; keep scripts bounded and do not claim that a timeout will stop execution.
- If the result cannot be established from returned output, describe the uncertainty and ask the user to confirm the visible outcome.

## Handle Windows side effects carefully

Before composing or invoking code that changes the host, identify what it will affect and keep the operation within the user's request. This matters especially for shell/process execution, PowerShell, registry/security settings, virtual input, services, scheduled tasks, and file operations. State requirements such as installed Office, network access, or administrator rights when relevant. Never expose credentials or sensitive payloads in output.

Do not invoke a host-changing script just to explore the API. For uncertain APIs, inspect the implementation or provide a proposed script for review. When execution is authorized, report what ran and the observable result; if an action is irreversible or outside the user's stated scope, stop and obtain authorization first.

## Change this repository

Keep edits scoped to the responsible module or project. Preserve existing WSH/JScript compatibility, validate JavaScript-to-COM and JavaScript-to-managed boundaries, and document externally visible API behavior near its implementation. For test profiles, keep IDs aligned with `testloader.js` and inspect the selected implementation before running it because tests may have host or network side effects.

Useful entry points: `app.js` (runtime and unsafe-eval default), `mcploader.js` (MCP methods and their tool schemas), `lib/jsonrpc2.js` (request dispatch), `lib/stdio-server.js` (stdio transport), and `AGENTS.md` (repository conventions).
