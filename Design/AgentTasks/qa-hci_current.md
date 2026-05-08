# QA/HCI Current Task

Date: 2026-05-08
Status: waiting
Priority: blocked on PM/user art decision before Gate 4 rerun

## Assignment

Wait for the concrete fixes identified by the focused Gate 4 rerun before producing final Gate 4 acceptance.

The previous Gameplay opening-control and ECS atlas architecture handoff is accepted for automated proof. The current blockers are public HCI/readability blockers, not the old route/protection blocker.

## Waiting On

UI report:

- `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`
- `Design/AgentReports/2026-05-08_pm_ui-m01-infantry-only-hud-scope-review.md`

Gameplay report:

- `Design/AgentReports/2026-05-08_gameplay_m01-unit-readability-selection-art.md`

Art/Atlas report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`

PM/user decision:

- approve or reject the temporary M01 infantry art package identified in `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`

## Required Fix Evidence Before Rerun

UI must prove:

- M01 HUD no longer shows APC, Tank, air support, Build, vehicle production, transport, or base/build affordances as usable M01 options.
- Selected rifle squad HUD state remains readable.
- Public route shell still reaches M01 and result flow remains intact.

Gameplay has proved for the current temporary-art pass:

- Player rifle squad reads as four distinct soldiers under one command identity at public gameplay camera scale.
- Selected state is visually clear in public first-control captures.
- Projectile/impact scale remains tactical.
- ECS atlas presentation remains the public visible path.
- M01 remains infantry-only with one player rifle squad type, one enemy patrol type, and no player vehicles/build/transport/base mechanics.

Art/Atlas has proved for the current temporary-art pass:

- `FinalAtlasArtReady = 0` is not resolved; PM/user must approve temporary Gate 4 art or reject it and route Art/Atlas follow-up.
- Player rifle squad, enemy patrol, selected-state treatment, destroyed/death state, and projectile/impact VFX are covered at public camera-scale review quality.
- The asset package does not require visible legacy `Model`, SpriteRenderer review proxy, old per-Model animation output, or separate `Destroyed` child runtime dependency.

## Next QA/HCI Work After Fixes

After PM/user makes the art decision, rerun focused Gate 4 HCI from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`:

- public route: Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy -> select rifle squad -> move to tutorial cover -> attack hostile patrol -> enemy destroyed/neutralized -> objective/result popup
- first-control readability
- four-soldier squad readability
- selected-state clarity in world and HUD
- infantry-only HUD scope
- touch/camera ergonomics or documented substitute
- invalid command recovery
- assistant ownership/Stop behavior
- performance/freeze/log readiness
- visual readability of ECS animated atlas units and command markers
- projectile/impact VFX scale
- absence of visible legacy `Model`, temporary SpriteRenderer adapter, old per-Model animation output, and separate `Destroyed` child/prefab runtime dependency for M01 infantry

## Completion Report

Do not write final Gate 4 acceptance until both fixes land and the rerun passes.

If a rerun finds blockers, write:

`Design/AgentReports/2026-05-08_qa-hci_gate4-final-rerun.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
