# APH-810 Visual Evidence Report Template

Create one JSON report per visual acceptance and validate it before handoff:

```bash
python3 Tools/CI/aph810_visual_evidence_contract.py \
  --evidence /path/to/aph810-evidence.json \
  --artifact-root / \
  --output-json /path/to/aph810-validation.json
```

`--artifact-root` makes logs and screenshots mandatory on disk. Omit it only for a schema-only review. The JSON Schema is `Tools/CI/aph810_visual_evidence.schema.json`.

## MCP Connected

```json
{
  "schemaVersion": 1,
  "taskId": "APH-810",
  "subject": "Exact scene, prefab, or visual change inspected",
  "revision": "exact Git commit or dirty working-tree identifier",
  "mcp": {
    "status": "connected",
    "probe": "Exact MCP connection/probe operation and result",
    "unavailableReason": null
  },
  "mcpEvidence": {
    "hierarchy": { "tool": "exact MCP tool", "target": "exact hierarchy target", "result": "inspection result" },
    "console": { "tool": "exact MCP tool", "target": "Console", "result": "errors/warnings reviewed" },
    "playMode": { "tool": "exact MCP tool", "target": "Play Mode state", "result": "state and behavior observed" },
    "screenshots": [
      { "tool": "exact MCP tool", "path": "/absolute/capture.png", "view": "gameplay-camera", "description": "what this proves" }
    ]
  },
  "fallback": null,
  "conclusion": "Accepted, rejected, or blocked conclusion and residual risk"
}
```

## MCP Unavailable

Do not claim MCP inspection. Record why the connection failed and the exact fallback invocation, log, pass marker, and captures used.

```json
{
  "schemaVersion": 1,
  "taskId": "APH-810",
  "subject": "Exact scene, prefab, or visual change inspected",
  "revision": "exact Git commit or dirty working-tree identifier",
  "mcp": {
    "status": "unavailable",
    "probe": "Exact failed MCP connection/probe operation",
    "unavailableReason": "Exact failure or unavailable capability"
  },
  "mcpEvidence": null,
  "fallback": {
    "runnerCommand": "Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/example.log -- -quit -executeMethod Namespace.Runner.Run",
    "logPath": "/private/tmp/example.log",
    "resultMarker": "[NamedRunner] result=Passed",
    "screenshots": [
      { "path": "/private/tmp/gameplay-camera.png", "view": "gameplay-camera", "description": "what this proves" }
    ]
  },
  "conclusion": "Accepted, rejected, or blocked conclusion and residual risk"
}
```

One report must use exactly one path. Connected reports require hierarchy, console, Play Mode, and screenshot evidence through MCP. Unavailable reports require fallback evidence and reject mixed MCP claims.
