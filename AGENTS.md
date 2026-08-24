# Unity Execution Contract

- Keep Unity Hub open and signed in while any Unity Editor or validation runs.
- Do not invoke the Unity executable directly. Use the platform-specific repository wrapper described below.
- On macOS, `invoke_unity_macos.sh` uses **GUI licensing** and never passes `-batchmode`. Do not add `-batchmode`, do not bypass the wrapper, and do not try an alternate Unity command when validation fails.
- Normal Editors and wrapper-driven validation may run concurrently. The shared licensing client supports multiple clients.
- Never run `Tools/CI/reset_unity_macos_ipc.sh`, pass `--reset-ipc`, or use `--quit-hub` while any Unity Editor is running. Reset requires `--confirm-no-editors` and is recovery-only for a fully closed, known-stuck Unity environment that the user explicitly asked to recover.
- Do not terminate Unity, Unity Hub, Unity.Licensing.Client, Package Manager, or remove `/private/tmp/Unity-*.sock` files unless the user explicitly asks to recover a stuck Unity environment.

## Unity CLI / Pipeline agent path (updated 2026-08-24)

- Prefer Unity CLI + Unity Pipeline for agent integration. The AI Assistant package's in-Editor MCP server is deprecated; do not depend on it for Codex/GamePlay agent work.
- The Unity CLI still has an MCP mode. When an agent needs MCP, use the CLI stdio server (`unity mcp --project-path <project>`) rather than the deprecated in-Editor MCP bridge.
- Keep `com.unity.pipeline` installed in this project for CLI-connected Editor commands. Verify with `unity pipeline list`, `unity status --project-path <project>`, `unity list --project-path <project>`, and `unity command --project-path <project>`.
- Do not use Unity CLI `build`, `run`, or `test` as a replacement for the macOS validation/build wrappers until the macOS licensing rule below is re-tested. Those commands may spawn batchmode Unity, which is still known-broken on this machine.

## Windows validation rule (approved 2026-07-25)

Windows validation is permitted through the checked-in PowerShell wrappers:

- Use `Tools/CI/ResolveUnityEditor.ps1` to resolve the Editor version recorded in `ProjectSettings/ProjectVersion.txt`.
- Use `Tools/CI/InvokeUnityExecuteMethodValidation.ps1` for focused `executeMethod` validation.
- Use `Tools/CI/InvokeUnity.ps1` for Unity Test Framework runs or other validation modes not covered by the focused wrapper.
- Launch the PowerShell wrappers with `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ...` when the machine's execution policy blocks checked-in scripts.
- `-batchmode` is permitted on Windows only through `InvokeUnity.ps1`; this does not relax the macOS prohibition.
- Give every run an explicit log path and timeout. Treat a timeout, missing pass marker, nonzero exit, or project lock as a failed validation.
- Before starting a run, confirm that no active Unity process owns this project. Do not terminate an existing Editor to make room for validation.
- A wrapper timeout may terminate only the Unity process tree that wrapper started. Never terminate unrelated Unity or Unity Hub processes.

### Windows licensing recovery ladder (authorized 2026-08-05)

- Do not pause or declare the Windows validation lane blocked solely because Unity licensing or licensing IPC fails.
- Start every Windows recovery attempt with the checked wrapper's GUI-licensing mode (`-GuiLicensing`) while Unity Hub remains open and signed in.
- If GUI licensing still fails, use only the checked-in project recovery workaround applicable to the observed failure, after verifying that no Unity Editor owns this project.
- If the checked recovery workaround still fails, the user has explicitly authorized restarting only the verified stale `Unity.Licensing.Client` process. Re-verify its executable path, start time, command line, and that no Unity Editor is active before restarting it; do not restart Unity Hub, Package Manager, or unrelated processes.
- After each recovery step, rerun the same checked wrapper with an explicit log and timeout. Never bypass the wrapper or weaken its fail-closed pass-marker requirements.
- If the verified licensing-client restart still fails, search current official Unity documentation, release notes, and support material for another narrowly scoped workaround, record the sources and exact recovery action, and continue until the checked validation can run. Do not wait for another approval merely to perform this authorized licensing recovery ladder.
- When the Hub client is healthy for Hub but its generic pipe still refuses the Editor after that verified restart, keep Hub open and rerun the same checked Windows wrapper without `-GuiLicensing`. This uses the wrapper-owned Windows command-line/batchmode path and lets the Editor launch its version-matched licensing client; it is not permission to invoke Unity directly or to bypass the required pass marker.
- Do not manually launch `Unity.Licensing.Client` from an elevated recovery shell. Stop only the verified stale client after confirming no Editor, then let the already-running Hub or checked wrapper recreate the client at the caller's normal integrity level.
- A full system drive is an infrastructure failure, not a passive blocker. After confirming no Editor owns the project, audit exact sizes and remove only explicitly verified rebuildable Unity caches (for example `Library/Bee`) or obsolete build outputs; preserve source, current packages, validation evidence, and unrelated worktree changes. Rerun the same checked wrapper with its original marker and timeout after space recovery.
- These permissions are recovery-scoped only. They do not authorize terminating an active Editor, changing Unity/Jenkins paths, altering project assets, or killing unrelated processes.

Example:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" `
  -ProjectPath (Get-Location).Path `
  -ExecuteMethod Game.Tests.Editor.MyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\warline-task.log" `
  -RequiredPassMarker "[MyTests] result=Passed"
```

## macOS licensing rule (mandatory)

Unity 6000.x **batchmode** fails on this machine with `505 Unsupported protocol version '1.18.1'` against Hub's generic `LicenseClient-farhad`. That path is broken by design here.

Root cause (checked 2026-07-24): Unity Hub **3.19.5** ships licensing client **1.17.4**, while Editor **6000.5.2f1** speaks LocalIPC **1.18.1**. The Editor falls back to its versioned client (`LicenseClient-<user>-6000.5.2`); GUI licensing makes that fallback reliable. Newer Hub **3.20.0-beta.1** and Editor **6000.5.5f1** release notes do **not** claim this skew is fixed.

### Reinvestigate when Hub updates

When Unity Hub updates past **3.19.5** (or ships `UnityLicensingClient_V1` **≥ 1.18.1**):

1. Compare Hub client Info.plist / log `File version` with the Editor client.
2. If Hub speaks LocalIPC **≥ 1.18.1**, re-test `-batchmode` through `invoke_unity_macos.sh` and consider restoring batchmode as the default.
3. Until then, keep GUI licensing and do not treat the `505 / 1.18.1` log line as a blocker by itself.

On macOS, agents must:

1. Use `Tools/CI/invoke_unity_macos.sh` for every Unity `executeMethod`, test run, prefab build, and capture.
2. Never pass `-batchmode` or invoke `/Applications/Unity/Hub/Editor/.../Unity` directly.
3. Never rerun Unity through reset scripts, IPC cleanup, escalated batchmode, or a "different route" when licensing is mentioned.
4. Never report "Unity licensing blocked the lane" unless Hub is closed and the wrapper exits with code `65`.
5. If the wrapper times out or the executeMethod fails after Unity starts, treat it as a normal validation failure and fix the test/code — not a licensing incident.

Example:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/warline-task.log -- \
  -quit -executeMethod Game.Tests.Editor.MyTests.RunFocusedValidation
```

Forbidden examples:

```bash
# Never do these on macOS for this project
Tools/CI/invoke_unity_macos.sh -- ... -batchmode ...
/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode ...
Tools/CI/reset_unity_macos_ipc.sh --confirm-no-editors   # unless user explicitly asked
```
