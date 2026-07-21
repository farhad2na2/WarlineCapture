# Unity Execution Contract

- Keep Unity Hub open and signed in while any Unity Editor or batch validation runs.
- Run Unity batch validation through `Tools/CI/invoke_unity_macos.sh`; do not invoke the Unity executable directly.
- Normal Editors and batch validations may run concurrently. The shared licensing client supports multiple clients.
- Never run `Tools/CI/reset_unity_macos_ipc.sh`, pass `--reset-ipc`, or use `--quit-hub` while any Unity Editor is running. Reset requires `--confirm-no-editors` and is recovery-only for a fully closed, known-stuck Unity environment.
- Do not terminate Unity, Unity Hub, Unity.Licensing.Client, Package Manager, or remove `/private/tmp/Unity-*.sock` files unless the user explicitly asks to recover a stuck Unity environment.
