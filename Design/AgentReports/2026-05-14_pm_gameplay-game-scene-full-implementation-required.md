# PM Correction: Gameplay Must Implement The Actual Game Scene Before Proof

Date: 2026-05-14
Lane: PM
Status: routed to Gameplay

## Problem

The previous Gameplay delivery did not satisfy the implementation requirement.

Reports reviewed:

- `Design/AgentReports/2026-05-14_gameplay_m01-imagegen-sample-implementation.md`
- `Design/AgentReports/2026-05-14_gameplay_m01-01-runtime-visual-match-proof.md`

What happened:

- Gameplay implemented only a narrow HUD behavior: Build visible but disabled in M01.
- The actual Game scene did not implement the target gameplay mockup.
- The visual proof capture was blank/invalid and explicitly reported visual-match status as blocked.
- No proof showed all soldiers, battlefield framing, HUD, minimap, or ECS animated units matching the target.

User verified in the actual Game scene that the target is not implemented.

Additional PM clarification from the M01 Designer spec: Gameplay must not skip the UI or design-spec state. A Game scene that only shows terrain/soldiers, or only shows a blank tactical screen, is not an implementation of M01-01. The visible runtime must show the M01 design specification: objective panel, Star Goals/objective row, neutral/disabled command panel, minimap with start viewport, threat/log mission start row, squad cards, assistant closed, no selected unit, no move/attack/objective/invalid markers, and Build unavailable with `MissionDoesNotAllowBuild`.

## Decision

Gameplay must keep working. A blocker-only proof report is not an acceptable delivery when the implementation has not actually been completed.

Gameplay owns implementing the actual `M01-01_TacticalStart` target in the Game scene, then proving it.

## Required Implementation

Implement in the actual Game scene path:

- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scenes/Game/GameSubScene.unity`

Target mockup:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`

Required runtime result:

- tactical battlefield visible, not a blank/gray capture
- same camera/framing direction as the target mockup
- player rifle squad present in the lower-left tactical area
- enemy patrol present in the upper-right tactical area
- all visible soldiers present according to the target/spec
- soldiers rendered through the existing ECS/runtime presentation path, not pasted mockup pixels
- soldier idle animation active for proof capture, using approved/available sprite atlas frames
- M01-01 no-selection state: no player selection rings, no selected squad status bar, no move/attack/objective/invalid world marker
- HUD baseline visible and matching the Designer spec:
  - objective panel lists `Destroy hostile patrol`
  - Star Goals row is visible
  - command panel is neutral/disabled until selection
  - allowed command set is Select, Move, Attack, Stop, Hold
  - Build is unavailable in M01 and if visible must communicate `MissionDoesNotAllowBuild`
  - minimap shows the M01 start viewport
  - threat/log panel may show mission start
  - squad cards are present/readable
  - assistant/ARIA is closed
  - no selected squad panel/status is shown
- no flattened target/mockup PNG imported or used as runtime source

## Required Proof

After implementation, Gameplay must deliver:

- `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof.md`

Proof must include:

- fresh runtime screenshot/capture from the actual Game scene
- side-by-side/contact-sheet or overlay comparison against `M01-01_TacticalStart_1920x1080.png`
- written match/mismatch assessment covering all soldiers, ECS animation proof, camera/framing, objective/Star Goals, command panel state, allowed command set, squad cards, threat feed, minimap start viewport, assistant closed, Build unavailable reason, no selected rings, no selected status, and no world markers
- validation command, workspace, log path, and result
- recommended next steps

## Blocker Policy

Gameplay should not stop with another blocker report unless there is a true external blocker after attempting the implementation and capture.

If blocked, the report must include:

- exact implementation step attempted
- exact command/workspace/log
- why Gameplay cannot proceed
- unblock owner
- smallest next executable step

## Routing

Current owner: Gameplay

Held:

- QA/HCI
- Designer
- Art/Atlas
- further sequence expansion
- M01-02 selected-state implementation
