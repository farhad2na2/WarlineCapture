# Visual Target Current Task

Date: 2026-05-08
Status: active
Priority: P0 create M01 gameplay visual target package before final selected-readability approval

## Assignment

Create a gameplay-only visual target package for M01 selected-readability so Art/Atlas, Gameplay, QA/HCI, and PM stop relying on repeated user feedback to define the visual bar.

This lane owns target references and paintover/mockup direction only. It does not implement runtime code or final art.

Read first:

- `Design/AgentReports/2026-05-08_pm_gameplay-visual-target-lane-routing.md`
- `Design/AgentTasks/visual-target_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`

## Required Behavior

- Put all gameplay visual targets under `Design/VisualTargets/Gameplay/M01_SelectedReadability/`.
- Do not put gameplay target mockups under `Design/VisualLock/` or `Design/VisualLockLayered/`; those are UI/HUD target systems.
- Use existing UI targets only as alignment references, especially:
  - `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
  - `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- Clearly separate:
  - UI/HUD visual target: screen-space panels, buttons, HUD shell, command rail, cards.
  - Gameplay visual target: in-world unit scale, soldier pose, selection marker, move/attack marker, enemy readability, building/road scale, animation frame expectations.
- Produce a target package with:
  - full M01 gameplay target or paintover,
  - soldier/building/road scale board,
  - selected-state marker target,
  - move/attack target marker target,
  - enemy readability target,
  - idle/run pose contact sheet guidance,
  - bad-example sheet naming rejected cases: huge green marker, yellow square, squashed soldier, crouch-run, red sitting artifact, foot-only selection.
- Include short QA acceptance checks for comparing runtime captures to the target.
- Include short user review instructions in the completion report:
  - what files to open,
  - what to compare,
  - what answer PM needs.
- User approval is required. Do not mark the visual target package accepted for downstream Gameplay/Art/QA work until PM/user explicitly approves it.

## Waiting On

Waiting on lane:
none

Owner of next action:
Visual Target

Can my lane still continue fallback work? yes, only this target package.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_visual-target_m01-selected-readability-package.md`

Use the standard WarlineCapture handoff format and include all target file paths, plus a short "User Review Steps" section.
