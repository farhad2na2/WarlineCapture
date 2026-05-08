# Lane
Gameplay

# Task
M01 selected first-control soldier readability and selection-marker fix.

# Files changed
- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Tests/Editor/Chapter01M01SpriteRendererTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`

# Contracts touched
- M01 public unit visuals still use ECS-owned atlas quad presentation.
- Public player/enemy unit visuals still expose no `SpriteRenderer` components.
- Public player/enemy unit visuals still expose no `MissionRuntimeSpriteRendererRuntime` component.
- M01 infantry metric scale stays at the rejected-art fix target near `0.20`.
- M01 move/run proof stays in the ECS atlas quad presenter.
- M01 golden path and infantry-only scope stay intact.
- Unit state sprite resolution now maps M01 player/enemy `.idle`, `.move`, `.attack`, and `.damaged` state IDs to individual `Unit_Chr_Soldier_Male_02_*_SE` sheet sprites instead of falling back to `infantry_squad.png`.

# User-visible behavior
- The selected first-control player squad now reads as four separate soldier quads instead of four duplicated mini-squad blobs.
- The four soldier visuals are offset from the ECS gameplay entity into a readable public formation for both 16:9 and 20:9 captures.
- Selection feedback is now four small warm/amber foot markers, one under/near each soldier, instead of blue/green UI-like effects or a large overlay.
- The enemy patrol also resolves through the individual soldier sheet and keeps the temporary enemy tint.

# Validation run
- Synced focused files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- PlayMode:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-gameplay-m01-soldier-readability-selection-results.xml -logFile /private/tmp/warlinecapture-gameplay-m01-soldier-readability-selection.log`
- EditMode:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter Chapter01M01AtlasQuadPresentationTests -testResults /private/tmp/warlinecapture-gameplay-m01-soldier-readability-editor-results.xml -logFile /private/tmp/warlinecapture-gameplay-m01-soldier-readability-editor.log`
- Fresh selected first-control capture proof:
  - `/Users/farhad/Projects/WarlineCapture/Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
  - `/Users/farhad/Projects/WarlineCapture/Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`

# Validation result
- PlayMode passed: `8/8`, `0` failed.
- EditMode passed: `4/4`, `0` failed.
- Fresh 16:9 and 20:9 selected first-control captures were regenerated and visually inspected by Gameplay after the final run.

# Known gaps
- `Unit_Chr_Soldier_Male_02` remains temporary key-pose art, not final multi-frame production animation.
- Final enemy-specific individual infantry art is still not present; enemy readability currently uses the same individual soldier sheet with a temporary tint.
- UI/unit-card icon polish remains outside this Gameplay fix if PM wants the squad card art changed separately.
- Final destroyed VFX sprite is still recorded as unavailable by the existing focused test.

# Cross-lane impacts
- Art/Atlas: Gameplay accepted the individual soldier sheet handoff and wired M01 runtime state resolution to it; Art/Atlas still owns final frame quality, enemy variant, and final multi-frame loops.
- QA/HCI: Ready for rerun/review using the listed captures plus the PlayMode and EditMode result XML files.
- UI: No UI source was modified; card/icon visual follow-up should be routed to UI/Art if PM requires it.

# Next recommended task
QA/HCI should review the refreshed selected first-control 16:9 and 20:9 captures for PM/user readiness, then PM should decide whether UI card/icon polish is needed as a separate lane task.
