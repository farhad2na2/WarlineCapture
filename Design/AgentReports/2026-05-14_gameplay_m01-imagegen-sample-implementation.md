# Lane
Gameplay

# Task
Approve the latest imagegen-only M01 sample for implementation readiness and, if approved, implement only the first runtime slice: `M01-01_TacticalStart`.

# Files changed
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
- `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`
- `Design/AgentReports/2026-05-14_gameplay_m01-imagegen-sample-implementation.md`

# Contracts touched
- Approved the latest imagegen sample for the first M01-01 implementation slice only.
- `M01InfantryOnlyHudScopeController` now treats `BuildButton` as visible but non-interactable during M01 instead of hiding it with the rest of the non-infantry suppressed roots.
- Existing M01 build-denial reason remains `MissionDoesNotAllowBuild` through `TacticalCommandFeedbackText`.
- M01 suppressed-root validation now allows the visible disabled Build secondary command while still requiring the other suppressed roots to be hidden.
- M01-02 selected-state markers/status remain reference-only and were not implemented.

# User-visible behavior
- In M01 First Contact, the Match HUD keeps Build visible as a disabled secondary command.
- APC, Tank, Air, Special, Build Drawer, and Command Wheel affordances remain suppressed by the M01 infantry-only HUD scope.
- Non-M01 match HUD behavior restores Build as active/interactable.
- No flattened mockup PNGs were imported or used as runtime source.

# Validation run
- Read `Design/AgentTasks/gameplay_heartbeat.md`.
- Read `Design/AgentTasks/gameplay_current.md`.
- Read the current Designer, PM, Art/Atlas, Gameplay audit, LayerPack, and frame metadata handoffs named by the task.
- Parsed JSON with `python3 -m json.tool`:
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
- Checked sample image dimensions with `sips`:
  - `M01-01_TacticalStart_1920x1080.png`: 1920x1080
  - `M01-02_SquadSelected_1920x1080.png`: 1920x1080
  - `M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`: 1920x1080
- Attempted focused Unity EditMode validation in `/Users/farhad/Projects/WarlineCapture`; blocked because another Unity instance has the project open.
- Copied the two changed source/test files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1` per standing user instruction for locked Unity workspaces.
- Ran focused Unity batchmode EditMode validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - Command: Unity 6000.4.0f1 `-batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-m01-matchoverlay-results.xml -logFile /private/tmp/warlinecapture-m01-matchoverlay.log`

# Validation result
- LayerPack JSON parsed successfully.
- Sample image dimensions match the required 1920x1080 reference resolution.
- Main workspace Unity validation is blocked by an already-open Unity editor on `/Users/farhad/Projects/WarlineCapture`.
- CodexUnity1 Unity batchmode exited with code 0 after compiling/importing the focused copied changes.
- Unity did not emit `/private/tmp/warlinecapture-m01-matchoverlay-results.xml` and the log did not include a per-test assertion summary, so per-test pass counts are not claimable from this run.

# Known gaps
- M01-02 selected state, selected markers, and selected world status were not implemented.
- M01-03 through M01-11 remain untouched.
- Disabled Build currently communicates through disabled button state and the existing `MissionDoesNotAllowBuild` reason contract; no new hover/click tooltip was added for a disabled button.
- Runtime camera/framing, minimap viewport, and enemy affiliation readability were not expanded beyond the existing supported systems in this first HUD-focused slice.
- QA/HCI validation has not been routed; current task says to hold QA/HCI until runtime implementation exists and PM routes validation.

# Cross-lane impacts
- PM can review this as the Gameplay implementation handoff for the M01-01 first runtime slice.
- QA/HCI should validate only after PM routes them, and should scope validation to M01-01 tactical start/no selection.
- Art/Atlas remains owner for final runtime slices/assets beyond the approved first-slice direction.
- Designer remains owner for visual approval of later selected-state slices before Gameplay implements M01-02.

# Next recommended task
PM should review the M01-01 runtime slice, decide whether the disabled Build affordance needs an explicit visible disabled-reason treatment, then route QA/HCI for focused M01-01 validation.
