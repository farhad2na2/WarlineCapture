# QA/HCI Current Task

Date: 2026-05-08
Status: waiting
Priority: accepted focused Gate 4 rerun; waiting for PM/user temporary-art decision

## Assignment

Wait for PM/user temporary-art decision.

QA/HCI delivered the focused Gate 4 rerun:

- `Design/AgentReports/2026-05-08_qa-hci_gate4-final-rerun.md`

PM accepted it for route stability and temporary-art review in:

- `Design/AgentReports/2026-05-08_pm_qa-hci-gate4-final-rerun-review.md`

## Waiting On

UI report:

- `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`
- `Design/AgentReports/2026-05-08_pm_ui-m01-infantry-only-hud-scope-review.md`

Gameplay report:

- `Design/AgentReports/2026-05-08_gameplay_m01-unit-readability-selection-art.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-manual-opening-control-fix.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-manual-opening-control-review.md`

Art/Atlas report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`

PM/user decision:

- approve or reject temporary Gate 4 infantry art

## Required Fix Evidence Before Rerun

UI must prove:

- M01 HUD no longer shows APC, Tank, air support, Build, vehicle production, transport, or base/build affordances as usable M01 options.
- Selected rifle squad HUD state remains readable.
- Public route shell still reaches M01 and result flow remains intact.

Gameplay must prove before QA/HCI rerun:

- The public M01 route gives the player a safe first-control window after Deploy.
- The player can wait briefly, select the rifle squad, and issue the first move before hostile fire kills or critically damages the squad.
- Player rifle squad reads as four distinct soldiers under one command identity at public gameplay camera scale.
- Selected state is visually clear in public first-control captures.
- Projectile/impact scale remains tactical.
- ECS atlas presentation remains the public visible path.
- M01 remains infantry-only with one player rifle squad type, one enemy patrol type, and no player vehicles/build/transport/base mechanics.

Art/Atlas has proved for the current temporary-art pass:

- `FinalAtlasArtReady = 0` is not resolved, but PM/user art approval is not the next request until Gameplay proves the manual opening-control route is reviewable.
- Player rifle squad, enemy patrol, selected-state treatment, destroyed/death state, and projectile/impact VFX are covered at public camera-scale review quality.
- The asset package does not require visible legacy `Model`, SpriteRenderer review proxy, old per-Model animation output, or separate `Destroyed` child runtime dependency.

## Next QA/HCI Work After Fixes

No new QA/HCI rerun is required while waiting. If PM/user approves temporary art, PM may route final Gate 4 packaging or a milestone acceptance decision. If PM/user rejects temporary art, wait for Art/Atlas and Gameplay follow-up before rerunning.

The completed rerun covered the public route, first-control readability, four-soldier squad readability, selected state, infantry-only HUD scope, invalid command recovery, atlas-backed visible units, projectile scale, and whether the route is suitable for short PM/user temporary-art review.

## Completion Report

If PM/user or PM assigns a new QA/HCI follow-up, write:

`Design/AgentReports/2026-05-08_qa-hci_<specific-followup>.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
