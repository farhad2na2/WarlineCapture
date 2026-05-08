Status: blocked
Topic:
Manual M01 launch paths enter legacy 3D gameplay path

Source:
User manual test feedback.

Finding:
The user started M01 / First Contact through both recommended manual paths and saw the old 3D prototype instead of the current M01 2D/isometric sprite-presenter direction.

Confirmed user paths:
- Quick Custom / Launch.
- Main Menu -> Saga Map / campaign map -> First Contact path.

Code trace:
- `Assets/Game/Scripts/UI/Screens/QuickCustomScreenController.cs` calls `WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter(this)` from `LaunchMission()`.
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureGameLaunchUtility.cs` re-enables the legacy `UI_Canvas`, calls `MenuView.RequestGameStart()` or `GameBootstrap.BeginGameplay()`, and disables the `WarlineCaptureRouter`.
- `Assets/Game/Prefabs/UI/Screens/Screen_SagaMap.prefab` contains M01 mission node metadata for `saga.ch01.m01.first_contact` and routes campaign flow through the router.
- `Assets/Game/Prefabs/UI/Screens/Screen_MissionBriefing.prefab` has a route button to `LoadoutSquadPrep` plus `WarlineCaptureMissionSessionButton` using the active mission. The user confirms this campaign/manual path also ends in the legacy 3D experience.

Why it matters:
The current automated route/capture evidence proves parts of the route-driven UI and M01 state surfaces, but the player-facing manual launch paths still route into the legacy gameplay experience. That means manual HCI/balance feedback from public M01 entry points is not validating the intended M01 production slice. It also explains why the user saw a legacy 3D prototype despite the accepted M01 sprite-presenter and sprite-renderer evidence.

Affected blockers:
- Gate 4 manual player-route validation remains blocked.
- Real touch/camera ergonomics remain unverified for the intended M01 2D/isometric route.
- The old 3D route must not be treated as accepted M01 production gameplay.

Recommended fix:
Route this to Gameplay/UI as a shared launch-path blocker:
- Decide whether Quick Custom should continue to launch legacy gameplay for sandbox testing or should launch the current M01 production route when `WarlineCaptureMissionSession.ActiveMissionId == saga.ch01.m01.first_contact`.
- Decide whether Saga Map / Mission Briefing / Loadout should launch legacy gameplay or the current M01 production route for `saga.ch01.m01.first_contact`.
- Provide a user-accessible M01 production launch path that shows the current sprite-presenter/sprite-renderer visual direction and mounted HUD/assistant route from both the direct test path and the campaign path, or clearly label legacy paths as sandbox.
- If the current 2D/isometric slice is only available through editor captures or validation builders, document that clearly and do not ask the user to manually validate current public launch paths as M01 production gameplay.

Affected lanes:
- Gameplay
- UI
- QA/HCI
- PM

Needs user decision:
Maybe. PM recommendation is to keep legacy Quick Custom only if explicitly labeled as sandbox/legacy, and create or wire a clear "M01 First Contact Production Test" launch path for manual validation.

Next task update needed:
Yes. Gameplay/UI tasks should include this manual-launch blocker before further manual HCI or balance validation is requested. QA/HCI should not ask for manual balance/HCI feedback until a non-legacy M01 production launch path is available.
