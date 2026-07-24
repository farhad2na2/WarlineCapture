# Unity Execution Contract

- Keep Unity Hub open and signed in while any Unity Editor or validation runs.
- Run Unity validation **only** through `Tools/CI/invoke_unity_macos.sh`. Do not invoke the Unity executable directly.
- On macOS, `invoke_unity_macos.sh` uses **GUI licensing** and never passes `-batchmode`. Do not add `-batchmode`, do not bypass the wrapper, and do not try an alternate Unity command when validation fails.
- Normal Editors and wrapper-driven validation may run concurrently. The shared licensing client supports multiple clients.
- Never run `Tools/CI/reset_unity_macos_ipc.sh`, pass `--reset-ipc`, or use `--quit-hub` while any Unity Editor is running. Reset requires `--confirm-no-editors` and is recovery-only for a fully closed, known-stuck Unity environment that the user explicitly asked to recover.
- Do not terminate Unity, Unity Hub, Unity.Licensing.Client, Package Manager, or remove `/private/tmp/Unity-*.sock` files unless the user explicitly asks to recover a stuck Unity environment.

## macOS licensing rule (mandatory)

Unity 6000.x **batchmode** fails on this machine with `505 Unsupported protocol version '1.18.1'` against Hub's generic `LicenseClient-farhad`. That path is broken by design here.

Root cause (checked 2026-07-24): Unity Hub **3.19.5** ships licensing client **1.17.4**, while Editor **6000.5.2f1** speaks LocalIPC **1.18.1**. The Editor falls back to its versioned client (`LicenseClient-<user>-6000.5.2`); GUI licensing makes that fallback reliable. Newer Hub **3.20.0-beta.1** and Editor **6000.5.5f1** release notes do **not** claim this skew is fixed.

### Reinvestigate when Hub updates

When Unity Hub updates past **3.19.5** (or ships `UnityLicensingClient_V1` **≥ 1.18.1**):

1. Compare Hub client Info.plist / log `File version` with the Editor client.
2. If Hub speaks LocalIPC **≥ 1.18.1**, re-test `-batchmode` through `invoke_unity_macos.sh` and consider restoring batchmode as the default.
3. Until then, keep GUI licensing and do not treat the `505 / 1.18.1` log line as a blocker by itself.

Agents must:

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
