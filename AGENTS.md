# Unity Execution Contract

- Keep Unity Hub open and signed in while any Unity Editor or validation runs.
- Never invoke the Unity executable directly. Use the platform-specific repository wrapper described below.
- On macOS, `invoke_unity_macos.sh` uses **GUI licensing** and never passes `-batchmode`. Do not add `-batchmode`, do not bypass the wrapper, and do not try an alternate Unity command when validation fails.
- Normal Editors and wrapper-driven validation may run concurrently. The shared licensing client supports multiple clients.
- Never run `Tools/CI/reset_unity_macos_ipc.sh`, pass `--reset-ipc`, or use `--quit-hub` while any Unity Editor is running. Reset requires `--confirm-no-editors` and is recovery-only for a fully closed, known-stuck Unity environment that the user explicitly asked to recover.
- Do not terminate Unity, Unity Hub, Unity.Licensing.Client, Package Manager, or remove `/private/tmp/Unity-*.sock` files unless the user explicitly asks to recover a stuck Unity environment.

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
