# Lane
UI

# Task
Audit and fix UI ownership for the rejected M01 target/selection overlay issues: huge green target marker, selected-state marker, selection affordance, and placeholder square.

# Files changed
- `Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs`
- `Assets/Tests/Editor/BattleHudGameplayBridgeConnectionTests.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- `Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`

# Contracts touched
- `SCN-08` still owns the Match HUD bridge and static `WorldCommandMarkerLayer` preview art in `Screen_MatchOverlay`.
- Live gameplay no longer exposes that fixed screen-space preview layer through `BattleHudGameplayBridge.SetWorldMarkersVisible(true)`.
- Grounded selected-state markers remain owned by the runtime ECS atlas presentation path in `MissionRuntimeAtlasQuadPresentationSystem`.
- Selection hit affordance remains owned by the gameplay/input selection path in `RTSSelectionSystem`, not by the HUD overlay prefab.

# User-visible behavior
- UI-owned large static command marker art is no longer surfaced during live selection, move, or attack orders through the gameplay bridge.
- Public selected-control capture no longer shows the UI `WorldCommandMarkerLayer` large target/attack/move preview covering the playfield.
- The remaining square/grounded selected-state marker visible under the squad is not UI prefab art; it is gameplay runtime atlas marker output and remains open for Gameplay/Art.

# Validation run
- Inspected required task/report/contract files:
  - `Design/AgentTasks/ui_current.md`
  - `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
  - `Design/AgentTasks/user_feedback_review_gate.md`
  - `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- Inspected ownership paths:
  - `Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs`
  - `Assets/Game/Scripts/UI/Components/BattleHudTacticalFeedbackController.cs`
  - `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
  - `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
  - `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - `Assets/Game/Scripts/Systems/SelectionMarkerVisibilitySystem.cs`
- Synced the two focused UI files to `/Users/farhad/Projects/WarlineCapture-CodexUnity2` for validation.
- Focused EditMode:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform EditMode -testFilter BattleHudGameplayBridgeConnectionTests -testResults /private/tmp/warlinecapture-ui-marker-bridge-results.xml -logFile /private/tmp/warlinecapture-ui-marker-bridge.log`
- Public M01 graphics validation:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicCampaignLaunch_M01GoldenPlaythroughShowsResultPopup -testResults /private/tmp/warlinecapture-ui-marker-campaign-golden-results.xml -logFile /private/tmp/warlinecapture-ui-marker-campaign-golden.log`
- Public Quick Custom graphics validation:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute -testResults /private/tmp/warlinecapture-ui-marker-quickcustom-results.xml -logFile /private/tmp/warlinecapture-ui-marker-quickcustom.log`
- Additional attempted route validation:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-ui-marker-public-m01-results.xml -logFile /private/tmp/warlinecapture-ui-marker-public-m01.log`
- Touched-file whitespace:
  `git diff --check -- Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs Assets/Tests/Editor/BattleHudGameplayBridgeConnectionTests.cs`
- Capture dimension check:
  `sips -g pixelWidth -g pixelHeight` on the refreshed selected-control and Quick Custom captures.
- Visual inspection:
  `/Users/farhad/Projects/WarlineCapture/Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`

# Validation result
- `BattleHudGameplayBridgeConnectionTests`: passed `7/7`, `0` failed. New test `GameplayBridge_DoesNotExposeStaticWorldMarkerPreviewDuringLiveOrders` passed.
- `PublicCampaignLaunch_M01GoldenPlaythroughShowsResultPopup`: passed `1/1`, refreshed:
  - `/Users/farhad/Projects/WarlineCapture/Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
  - `/Users/farhad/Projects/WarlineCapture/Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- `PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute`: passed `1/1`, refreshed:
  - `/Users/farhad/Projects/WarlineCapture/Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
  - `/Users/farhad/Projects/WarlineCapture/Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- Refreshed capture dimensions are `1280x720` and `1600x720`.
- Visual inspection confirms the UI-owned static marker layer is not covering the playfield in the selected-control capture.
- Full `Chapter01M01PlayModeValidationTests` in `-nographics` mode failed `1/8` on `GameScene_M01OpeningControlWindowPreventsLethalEnemyFireUntilFirstMove`: expected opening protection to release after explicit attack step, but it remained true. That failure is Gameplay-owned and unrelated to this UI marker bridge change. The public launch/capture tests passed separately in graphics mode.
- Main project direct Unity validation was blocked because `/Users/farhad/Projects/WarlineCapture` is already open in another Unity instance. Validation was completed in the assigned UI workspace instead.
- Touched-file `git diff --check` passed. Whole-worktree `git diff --check` still reports unrelated trailing whitespace in `Assets/Game/Scenes/Game.unity`.

# Known gaps
- The selected-state placeholder/square still visible under/behind soldiers is generated by `MissionRuntimeAtlasQuadPresentationSystem.CreateSelectionMarkers` / `UpdateSelectionMarker` using ECS runtime mesh/material marker output. Owner lane: Gameplay, with Art/Atlas for final marker asset/style.
- Selection requiring exact foot-pixel clicks is controlled by `RTSSelectionSystem` world picking and focused entity selection. Owner lane: Gameplay/QA-HCI, not UI prefab/layout.
- Final dynamic target/move/attack marker placement still needs a grounded runtime implementation or explicit PM routing. UI has disabled the harmful static preview exposure; it did not create a new gameplay-target-anchored marker system.

# Cross-lane impacts
- Gameplay: owns the remaining selected-state marker shape/size/material and selection hit target affordance.
- Art/Atlas: owns final small grounded marker art if Gameplay replaces the current runtime material square.
- QA/HCI: should verify that the huge UI-owned static command marker no longer appears, then keep UFB-2026-05-08-06 open until the runtime square and click-affordance issues are validated.
- PM: can mark UI's portion of UFB-2026-05-08-02 addressed for the static HUD overlay path, while keeping Gameplay/Art ownership open for runtime marker quality.

# Next recommended task
Gameplay should replace the runtime ECS selected marker square with a small grounded per-soldier/footprint marker and broaden the world selection hit target so selecting a soldier works from the visible body/formation footprint, then QA/HCI should rerun the rejection feedback matrix.
