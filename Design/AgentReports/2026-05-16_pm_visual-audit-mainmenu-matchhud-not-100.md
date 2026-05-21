# PM Visual Audit: Main Menu And Match HUD Not 100 Percent

Date: 2026-05-16
Owner: PM
Status: visual acceptance rejected
Priority: P0

## Question

Do the current Main Menu and Match HUD match the target-lock mockups 100%?

## Decision

No.

## Main Menu

Current implementation evidence:

- `Design/AgentReports/Captures/SCN-02_MainMenu_ApprovedTargetImplementation_1672x941.png`

Target:

- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`

Result:
Rejected for visual acceptance.

Reasons:

- Implementation is still an older shell layout.
- Target has a Warline Capture masthead and full-width canonical resource strip; implementation uses a different commander/header layout.
- Target has Commander Profile with large portrait/fallback panel; implementation uses small sidebar route blocks.
- Target has three large illustrated mode cards; implementation uses wide horizontal route rows.
- Target has visible Persistent Operation pressure/risk content; implementation shows a simpler designed-unavailable row.
- Target has a large Deploy Command CTA; implementation does not match it.
- Overall composition, spacing, visual density, and polish are not target-lock matched.

## Mission Result

Current implementation evidence:

- `Design/AgentReports/Captures/POP-05_MissionResult_ApprovedTargetImplementation_1672x941.png`

Target:

- `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png`

Result:
Rejected for visual acceptance.

Reasons:

- Implementation has some correct semantic fields, but the visual treatment is far from the approved target.
- Target has a large premium Victory hero, mission image/identity block, strong metal/glass frame, cinematic background, rich reward card grid, polished city consequence row, and high-quality Replay/Continue button treatment.
- Implementation is a simplified centered panel with flat/layout-debug looking chrome, different hierarchy, different stats/objectives structure, missing target background depth, and lower quality icon/card/button presentation.
- It is not a target-lock visual match.

## Match HUD

Current implementation evidence:

- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v6_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png`

Targets:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`

Result:
Accepted only for the already-scoped UI fixes, not as a 100% target-lock match.

Accepted from UI v6:

- green chroma artifacts removed
- M01 command order fixed as `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`
- `SPECIAL` not used for M01
- M01 no-selection state remains correct

Still not 100%:

- battlefield/background and camera composition differ from M01 target
- objective panel density and exact placement differ
- bottom squad cards differ in size, proportions, and polish
- command rail placement/scale differs from M01 target
- minimap size/content/placement differs
- runtime capture still includes Gameplay-owned soldier/readability differences
- SCN-08 generic target and M01-specific target have command/state differences that must be reconciled explicitly, not treated as already matched

## Routing

Current owner:
UI for POP-05 Mission Result and SCN-02 Main Menu target-match fix.

Required UI report:

- `Design/AgentReports/2026-05-16_ui_pop05-scn02-target-match-fix.md`

Held:
QA/HCI, Gameplay continuation, Art/Atlas, Support/FTUE, Designer, and non-routed packages.
