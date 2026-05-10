Gate:
M01 ECS animated atlas unit presentation

Status:
accepted for architecture; needs art approval / QA-HCI rerun before Gate 4 pass

Reason:
Gameplay updated `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` and now reports the key architectural blockers as closed for M01 infantry: the temporary `SpriteRenderer` adapter is replaced, public M01 infantry uses ECS-owned atlas quad presentation, and separate `Destroyed` child runtime dependencies are stripped from the M01 rifle squad and hostile patrol.

Validation accepted:
- Public M01 infantry now renders through `MissionRuntimeAtlasQuadRuntime`, an ECS-owned textured mesh/material atlas quad.
- `MissionRuntimeSpriteRendererRuntime` / Unity `SpriteRenderer` is reported as no longer used for public M01 infantry presentation.
- Visible M01 units remain tracked by ECS runtime ids:
  - `unit.player.rifle_squad_01`
  - `unit.enemy.patrol_01`
- Presenter state resolves idle/move/attack/destroyed atlas ids.
- Legacy ECS mesh/model rendering remains suppressed through `MissionRuntimeSpritePresenterSuppressesLegacyModelTag`.
- `UnitDestroyedVisualReference` and `UnitDestroyedVisualInitialized` are stripped from M01 infantry runtime entities.
- Destroyed/death feedback for public M01 infantry resolves through the atlas state machine, currently `vfx.unit.destroyed.small`, not through a separate `Destroyed` child visual.
- Public Campaign golden path is reported as passed by focused automated coverage through result popup.
- M01 remains infantry-only in the report.
- No banned broad runtime scene-search usage is reported in touched focused files.

Validation still needed:
- Final/milestone art approval remains open. `FinalAtlasArtReady` is still `0`, and idle/move/attack/damaged state ids fall back to approved M01 infantry manifest source art until final Chapter 1 unit atlas frames are approved.
- QA/HCI still needs to rerun final Gate 4 from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` against the updated architecture.
- QA/HCI should classify remaining Unity/editor log warnings and verify the public golden path visually and interactively enough for Gate 4.

Cross-lane notices:
- Gameplay architecture is now acceptable for the user's stated direction: existing prefab/config identity is preserved, old visible `Model`/SpriteRenderer presentation is replaced for M01 infantry, and separate `Destroyed` child dependency is removed from M01 infantry runtime.
- UI and Support/FTUE have no new task unless QA/HCI finds a concrete issue.
- User may need to approve final or milestone infantry atlas frames if Gameplay/QA cannot proceed with the current manifest source art.

Next gate/task:
QA/HCI should rerun focused Gate 4 against the updated public M01 path and report whether it is ready for user review, blocked on art approval, or needs fixes.
