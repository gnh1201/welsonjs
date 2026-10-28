# WelsonJS Editor script execution

WelsonJS Editor runs the code in the Monaco editor through the local
`welsonjs-mcp` process. The browser UI does not execute the script itself:

```mermaid
sequenceDiagram
    participant Editor as WelsonJS Editor
    participant Launcher as WelsonJS.Launcher
    participant MCP as welsonjs-mcp (stdio)
    participant Runtime as WelsonJS runtime

    Editor->>Launcher: POST /welsonjs-mcp/start
    Launcher->>MCP: cscript app.js mcploader /quiet
    Editor->>Launcher: POST /welsonjs-mcp/evaluate
    Launcher->>MCP: tools/call(evaluate_js)
    MCP->>Runtime: evaluate script
    Runtime-->>MCP: result
    MCP-->>Launcher: JSON-RPC result
    Launcher-->>Editor: result
```

The Editor ribbon provides **Run**, **Start MCP**, and **Stop MCP** actions.
`Run` starts the MCP process on demand if necessary and sends the current
editor contents to `evaluate_js` with `allowUnsafeEval: true`.

## Launcher endpoints

- `GET /welsonjs-mcp/status`
- `POST /welsonjs-mcp/start`
- `POST /welsonjs-mcp/stop`
- `POST /welsonjs-mcp/evaluate` with `{ "script": "...", "timeout": 900000 }`

The MCP process is local and is stopped when the launcher stops. Script
execution has the same host privileges and side effects as the existing
`evaluate_js` MCP tool; only use it for trusted code.
