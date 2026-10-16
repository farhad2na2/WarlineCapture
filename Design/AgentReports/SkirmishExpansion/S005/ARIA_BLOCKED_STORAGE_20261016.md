# ARIA blocked-session storage repair   2026-10-16

When ARIA stopped as Blocked, the HUD published an inactive observation and storage reset the session to Manual. The repair retains the stop reason and retry counters within the same match while clearing touch, observation and planner authority. Explicit Stop, route/sequence changes and active rewinds still reset the session.

Validation: native Unity 6000.5.8f1, Android target, checked GUI-licensing focused wrapper with 900-second timeout. Actual caller and wrapper exits were both 0. Required log marker: `[AriaUiOwnedStorageTests] result=Passed controls=10`. The ten controls include retained diagnostics without input authority, later ownership reset and active rewind reset.

Evidence: `Logs/SkirmishProduction/Offline/S005AriaStopRepairDraft/native-FocusedControls-20261016-063501-c1f63c691516485a9483d4231b547742`.

Unity log SHA-256: `960c7191227cc931d23af14ee370f1780ad9856557aad07a52a48a509b190045`.

- `Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishObservationStorage.cs`: `3054e800cb31055ebea9128277c0e223c2a15e3f40a1007b427aaf3a8a14c183`
- `Assets/Tests/Editor/SkirmishExpansion/AriaUiOwnedStorageTests.cs`: `8f120c117ac331faf667e82e0bc0e94c25c1afe2e79d0336d9908bae12cf02e3`

This is an engineering regression repair. It does not resolve the underlying S005 FindBase failure or establish normal-input mission completion, camera acceptance, device performance or player readiness. The failed FA seed155926 attempt remains preserved; no new completed mission is credited.
