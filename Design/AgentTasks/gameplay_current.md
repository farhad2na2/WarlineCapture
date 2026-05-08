# Gameplay Current Task

Date: 2026-05-08
Status: active
Priority: P1 public M01 launch path blocker

## Assignment

Fix the public M01 launch path from the Gameplay side: when UI routes into `saga.ch01.m01.first_contact`, the tactical world under the HUD must show the authored M01 terrain/map, readable unit/target scale, correct old-world suppression, and usable gameplay camera framing. UI owns the canvas/HUD/route-button/capture composition over that world. Do not start M02-M05, broad renderer/art refactors, final atlas packaging, or unrelated gameplay work.

## Context

Read first:

- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-log-performance-fixed-roads.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-log-performance-fixed-roads-review.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-presenter.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-atlas-renderer-review.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-capture-update-review.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer-capture-fix.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-capture-fix-review.md`
- `Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md`
- `Design/AgentReports/2026-05-07_pm_qa-hci-m01-watcher-smoke-regression-review.md`
- `Design/WarlineCapture_M01_Legacy_Runtime_Guardrails.md`
- `Assets/Game/Data/TacticalMaps/Chapter01/WarlineCapture_M01_Legacy_Runtime_Guardrails.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_2D_Isometric_Production_Direction.md`
- `Design/WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`
- `Design/AgentReports/2026-05-08_pm_manual-test-quick-custom-launches-legacy-3d.md`
- `Design/AgentReports/2026-05-08_pm_workflow-public-launch-smoke-gate.md`
- `Design/AgentReports/2026-05-08_pm_manual-test-test-custom-still-legacy-scene.md`

Gameplay Gate 1 is accepted. The first M01 sprite-presenter contract slice is accepted: `MissionRuntimeSpritePresenter` now covers `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point`.

The renderer hookup and close tactical capture fix are accepted for current review-art evidence. QA/HCI can now use `M01_SpriteRenderer_CloseCapture.png` for grounding, scale, and readability review.

QA/HCI automated smoke is green. The Gameplay log-health portion is accepted, and the UI simulated safe-area profile matrix is accepted. Gate 4 is now blocked by public launch-path mismatch: the user confirmed Quick Custom, Test/Custom game mode, and Saga Map/campaign launch paths still show the old 3D prototype instead of the current M01 2D/isometric production direction.

PM rejected the earlier route-only launch evidence for manual readiness. `WarlineCaptureRoute.Match` plus inactive `UI_Canvas` is not enough if `GameBootstrap.BeginGameplay()` still shows the old rendered 3D scene. The blocker is the player-visible scene/camera/rendered gameplay after launch.

PM also rejected the latest public-launch visible-scene captures because they show HUD chrome over a mostly flat brown world with tiny centered gameplay content. The current blocker is not solved by merely hiding legacy 3D roots. The player-visible launch must render the authored M01 tactical map/terrain and gameplay camera at a readable scale comparable to the accepted gameplay reference evidence.

The user manually reported on 2026-05-08 that the world/ground map under the soldiers is upside down in the public launch/manual test view. Treat this as a Gameplay-owned public-launch blocker until fixed and revalidated. The tactical ground orientation must match the accepted M01 visual direction, metadata anchors, road/objective layout, minimap/camera mapping, and unit placement. Do not accept a capture where the units are readable but the authored terrain is rotated/flipped relative to the intended tactical layout.

PM reviewed the updated Gameplay handoff in `Design/AgentReports/2026-05-08_pm_gameplay-m01-ground-orientation-review.md`. The visual orientation evidence is improved, but the handoff remains `needs fixes` because the visible tactical ground is still evidenced through `TacticalMapRuntimeLoader.GroundRenderer` as a standalone SpriteRenderer/GameObject, and touched PlayMode validation still uses broad child-component discovery. Continue this task until the ECS world-source proof/fix and no-broad-lookup validation requirements are satisfied.

## Required Work

- Trace and fix the public launch flow for `saga.ch01.m01.first_contact` so at least one user-facing path reaches the current M01 production slice:
  - `Main Menu -> Saga Map -> Mission Briefing/Loadout -> Launch`.
  - Test / Custom / Quick Custom launch paths.
  - Any direct/quick/test path that remains legacy must be clearly labeled sandbox/legacy, and a separate production M01 test path must exist.
- Do not count router state alone as success. The first player-visible rendered scene must not be the old 3D prototype.
- Do not count a flat brown/blank world with tiny centered M01 sprites as success. Public M01 launch must show the authored tactical terrain, readable unit/target scale, correct world camera framing, and enough context for the first select/move/attack task.
- Do not count an upside-down, rotated, or mirrored tactical ground/map as success. Fix the runtime ground orientation in the ECS-backed tactical map presentation and verify roads, objective anchors, command squad, hostile patrol, blockers, camera bounds, and minimap mapping align with the intended M01 layout.
- Keep Gameplay work scoped to the tactical world under the HUD: mission runtime, tactical map loader output, authored terrain visibility, old-world suppression, unit/target world scale, and gameplay camera framing. Do not edit UI canvas/HUD layout, assistant surfaces, safe-area layout, or UI capture chrome except to provide explicit data/camera references UI needs.
- Preserve WarlineCapture as an ECS gameplay project. Only Canvas UI is allowed to be non-ECS GameObjects. The playable tactical world state, terrain/map surfaces, units, decor, markers, objectives, commands, health, damaged/destroyed state, and result readiness must be ECS entities/components driven by authored tactical metadata and mission runtime systems.
- `MissionRuntimeSpriteRendererSystem`, `SpriteRenderer`, and GameObject presentation objects are allowed only as ECS-driven visual presentation objects for ECS entities. They must not carry independent gameplay state, tactical ownership, command state, hit state, selection state, objective state, or screenshot-only stand-ins. They must be created, updated, selected, damaged/destroyed, and validated through ECS entity data such as `MissionRuntimeEntityId`, `MissionRuntimeSpritePresenter`, `LocalTransform`, `UnitGrid`, `UnitHealth`, selection/command components, and tactical metadata. If any non-Canvas visible world GameObject or sprite exists without a corresponding ECS entity/source-of-truth, it is a blocker.
- Audit whether `GameBootstrap.BeginGameplay()` is still the wrong visible runtime entry for M01 production launch. If it is, provide a production M01 entry path or isolate/label the legacy path.
- Coordinate with UI if the fix belongs in `WarlineCaptureGameLaunchUtility`, route buttons, Mission Briefing/Loadout buttons, router state, or UI shell activation.
- Preserve the accepted M01 mission session id and tactical metadata ids.
- Preserve `MissionRuntimeSpritePresenter`/`MissionRuntimeSpriteRendererSystem` consumption for M01 entities.
- Preserve ECS as the source of truth for command squad, hostile patrol, command point/decor, objectives, selection, move/attack orders, health, damage/destroyed state, and result readiness. Do not add parallel MonoBehaviour-only state for those systems.
- Do not route M01 manual validation through the old legacy 3D prototype unless the path is explicitly labeled sandbox/legacy and a separate production test path exists.
- Do not ask the user for manual HCI/balance testing until the public launch smoke passes.
- Preserve the current `MissionRuntimeSpritePresenter` consumption for `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point`.
- Preserve the accepted presenter state mapping for idle, move, attack, damaged, and destroyed.
- Preserve `vfx.unit.destroyed.small` or atlas state data for destruction feedback; do not toggle or reintroduce a separate `Destroyed` child GameObject for M01 production visuals.
- Preserve visible legacy `Model` child suppression for covered M01 entities while keeping legacy assets intact for non-M01/fallback paths.
- Do not replace the accepted capture path unless QA/HCI or PM reports a concrete blocker.
- The accepted gameplay visual reference for camera scale/readability is `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`. Public-launch gameplay framing does not need to be identical, but it must clearly preserve the authored terrain/map context and readable unit scale. If the runtime map loader or camera is producing the brown/empty field, fix that runtime path before asking QA/HCI or the user to test.
- If final art integration is assigned later, move from current review PNGs to final packed atlas/config or Addressables without reintroducing legacy visible `Model` or separate `Destroyed` child dependencies.
- If the public launch path cannot be fixed entirely in Gameplay, write the exact UI-owned blocker and required file/component in the report.
- Preserve the accepted AI plan guardrail: generic `AIBuildPlan`, `AIProductionPlan`, and `AISquadPlan` entities are disabled only when `Chapter01M01PlayableRuntime.IsActiveMission()` is active.
- Preserve fixed-direction baked/contact shadow requirements from the atlas manifest.
- Keep legacy prefab children/assets in place until non-M01 systems are migrated or explicitly marked legacy/future.
- Do not re-enable day/night for M01.
- Do not reintroduce random/procedural road or city generation into M01.
- Do not change UI assistant surfaces or Support/FTUE recommendation logic.
- If the public launch still fails because HUD/canvas/full-screen capture composition is missing or wrong while the gameplay world is correct, report the exact UI-owned blocker instead of changing UI layout code.
- Do not add runtime scene searches (`FindObjectOfType`, `FindObjectsOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, or name/tag lookup). Use serialized references, runtime registries, mission/session data, or typed provider APIs.
- Do not mark current AI-generated unit/building/tactical-map sprites complete. They remain `exists_needs_review` until the user approves final PNGs.

## Validation Required

- Public launch smoke must be reported:
  - Entry path used.
  - Expected mission id and visual direction.
  - Actual first visible gameplay state.
  - Whether legacy `UI_Canvas`, old 3D gameplay, wrong scene, or wrong mission appears.
  - Whether current M01 2D/isometric sprite-presenter/sprite-renderer visuals are actually visible to the player.
  - Screenshot/capture path when practical.
  - Confirmation that authored M01 terrain/map art is visible, not a flat brown/blank field.
  - Confirmation that the tactical ground/map under the soldiers is not upside down, rotated, or mirrored, and that metadata anchors still line up with the visible roads/objectives/blockers after the orientation fix.
  - Confirmation that unit/target scale and camera framing are comparable to the accepted M01 gameplay reference capture and usable for the first task.
  - Confirmation that every non-Canvas visible world object in the M01 tactical slice is backed by ECS entity state and is not a standalone screenshot-only GameObject/SpriteRenderer.
  - Explicit UI/GamePlay ownership split: list what Gameplay changed, what UI evidence/blocker remains for HUD/canvas/capture composition, and whether the gameplay world under the HUD is ready for UI/QA capture.
- Do not ask for product permission to run required focused Unity EditMode/PlayMode validation, non-headless/player log classification, or Android/device smoke checks for this task. Gameplay's assigned Unity workspace is `/Users/farhad/Projects/WarlineCapture-CodexUnity`. Do not use the UI workspace (`WarlineCapture-CodexUnity2`) or QA/HCI workspace (`WarlineCapture-CodexUnity3`) unless PM explicitly reassigns a temporary workspace.
- Only pause if Codex itself displays a sandbox/tool approval requirement. If that happens, make clear the request is for tool permission, not a gameplay/product decision. If the tool UI allows it, request that Codex remember the narrow Unity batchmode permission for this lane's required WarlineCapture validation commands so future focused validation can continue automatically. Use wording like: `Codex needs tool approval to run the focused Unity renderer validation required by the active gameplay task. Please approve and remember the Unity batchmode tool permission for this lane's required WarlineCapture validation commands so validation can continue automatically.`
- If Unity batchmode hits `LicenseClient-farhad` reconnect/time-out loops before tests start, rerun the same required command with Codex escalation/out-of-sandbox execution in `/Users/farhad/Projects/WarlineCapture-CodexUnity`. QA/HCI confirmed this resolves the sandbox licensing issue. Do not switch to UI/QA workspaces to work around licensing.
- Rerun the smallest focused runtime/log validation only if QA/HCI reports a new gameplay-owned blocker.
- If player/device validation is not possible, report the exact blocker and the strongest available non-headless/editor evidence.
- Validate M01 no longer needs visible legacy `Model` child rendering for the covered entities.
- Validate destroyed/damaged feedback does not require a separate `Destroyed` child object.
- Validate fixed-direction baked/contact shadow requirement remains present in the manifest/contract and is visible or explicitly traceable in capture evidence.
- Validate no new scene-search warnings, obsolete `FindFirstObjectByType` warnings, or banned runtime lookup calls were introduced in the touched gameplay runtime files.

## Completion Report

Write the report to:

`Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
