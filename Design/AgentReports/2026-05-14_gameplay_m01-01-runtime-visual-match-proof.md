# Lane
Gameplay

# Task
Provide supplemental runtime visual-match proof for the implemented `M01-01_TacticalStart` slice before PM/user approval or QA/HCI routing.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Design/AgentReports/Captures/M01-01_RuntimeCapture_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeCapture_vs_Target_Comparison.png`
- `Design/AgentReports/2026-05-14_gameplay_m01-01-runtime-visual-match-proof.md`

# Contracts touched
- Added an editor-only capture entry point: `WarlineCaptureM01RuntimeVisualMatchProofCapture.Capture`.
- No gameplay runtime contract was expanded beyond the prior M01-01 HUD slice.
- No M01-02 selected-state behavior was implemented.
- No flattened target PNG was imported as runtime source.

# User-visible behavior
- No new player-facing behavior was intentionally added in this heartbeat.
- The proof capture shows that the current runtime visual output is not ready for PM/user visual approval.

# Validation run
- Read `Design/AgentTasks/gameplay_heartbeat.md`.
- Read `Design/AgentTasks/gameplay_current.md`.
- Read PM correction report: `Design/AgentReports/2026-05-14_pm_gameplay-runtime-visual-proof-required.md`.
- Target mockup:
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- Runtime capture output:
  - `Design/AgentReports/Captures/M01-01_RuntimeCapture_1920x1080.png`
- Comparison output:
  - `Design/AgentReports/Captures/M01-01_RuntimeCapture_vs_Target_Comparison.png`
- Main workspace capture was not attempted because Unity already has `/Users/farhad/Projects/WarlineCapture` open.
- Copied the scoped Gameplay source/test/capture utility files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Failed command, first attempt:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: Unity 6000.4.0f1 `-batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.Capture -logFile /private/tmp/warlinecapture-m01-01-runtime-visual-match-capture.log`
  - Result: failed because `SceneManager.CreateScene()` is not valid in editor batchmode; Unity requires `EditorSceneManager.NewScene()`.
- Fix applied:
  - Changed the editor-only capture utility to use `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)`.
- Successful capture command:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: Unity 6000.4.0f1 `-batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.Capture -logFile /private/tmp/warlinecapture-m01-01-runtime-visual-match-capture.log`
  - Log path: `/private/tmp/warlinecapture-m01-01-runtime-visual-match-capture.log`
  - Log marker: `WARLINECAPTURE_M01_RUNTIME_VISUAL_MATCH_CAPTURED path=Design/AgentReports/Captures/M01-01_RuntimeCapture_1920x1080.png`
- Comparison command:
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png --capture Design/AgentReports/Captures/M01-01_RuntimeCapture_1920x1080.png --out Design/AgentReports/Captures/M01-01_RuntimeCapture_vs_Target_Comparison.png --label "M01-01 runtime visual proof"`

# Validation result
- Unity capture command exited successfully after the editor-scene fix.
- Runtime capture dimensions are 1920x1080.
- Comparison artifact generated successfully.
- Comparison MSE: `15051.04`.
- Visual-match status: blocked / needs fixes before PM/user approval.

# Match / mismatch assessment
- Build disabled/secondary treatment: not visually proven. The capture output is effectively a blank gray runtime frame, so the disabled Build affordance cannot be verified visually.
- Command buttons neutral/inactive: not visually proven from the runtime capture.
- Objective and Star Goals: not visually proven from the runtime capture.
- Squad cards: not visually proven from the runtime capture.
- Threat feed: not visually proven from the runtime capture.
- Minimap: not visually proven from the runtime capture.
- Camera/framing: mismatch. The capture does not show the approved tactical battlefield framing.
- No selected rings: inconclusive. The capture does not show selected rings, but it also does not show the expected runtime battlefield/HUD state.
- No selected status: inconclusive. The capture does not show selected status, but the frame is not a valid visual proof of the intended runtime state.

# Known gaps
- The current editor-only capture utility successfully writes a PNG but does not render a valid combined battlefield/HUD visual in batchmode; the result is a blank gray capture.
- The implemented M01-01 runtime slice remains too narrow to claim visual match against the approved full HUD mockup.
- The capture path does not yet prove native objective panel, Star Goals, squad tray, threat feed, minimap, command bar, or Build disabled presentation.
- QA/HCI, PM/user approval, M01-02, and further gameplay slices remain held.

# Cross-lane impacts
- PM/user should not approve this M01-01 runtime slice as visually matched yet.
- QA/HCI should remain blocked until Gameplay can provide a valid runtime screenshot and comparison.
- Designer and Art/Atlas do not need to take action on this blocker unless PM decides the runtime visual target must be narrowed or re-authored.

# Next recommended task
Gameplay should replace the proof capture path with a reliable runtime capture flow that renders the actual M01 battlefield plus `Screen_MatchOverlay` in batchmode, then re-run the comparison and update this report with a valid screenshot before PM/user approval.
