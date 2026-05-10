Gate:
M01 golden playthrough / Gameplay playable-loop blocker follow-up

Status:
needs fixes

Reason:
Gameplay updated `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` after the prior PM review. The updated handoff now reports focused automated coverage for the public Campaign route through result popup, which addresses the earlier public-result-popup proof gap. However, the handoff still does not meet the user's stated Gate 4 runtime-presentation expectation because final multi-frame atlas art is missing and visible units still render through a temporary ECS-driven Unity `SpriteRenderer` adapter.

Validation accepted:
- Public Campaign golden path is now reported as passed by focused automated coverage through result popup.
- Combat-damage flow remains covered by `GameScene_M01SelectionAttackAndResultRouteRespectSurvivalGuard`.
- M01 remains infantry-only in the report: one command squad and one hostile patrol, with no player vehicles, build entry, transport, base, or extra player unit type.
- ECS runtime ids and presenter state ownership are reported for `unit.player.rifle_squad_01` and `unit.enemy.patrol_01`.
- No banned broad runtime scene-search usage is reported in the touched focused files.

Validation still needed:
- Final visible unit presentation remains below the user's stated bar. The report still says `MissionRuntimeSpriteRendererRuntime` is a temporary ECS-driven `SpriteRenderer` adapter.
- Final multi-frame Chapter 1 unit atlas art is still missing. `FinalAtlasArtReady` remains `0`.
- The PM/user must not be asked to accept Gate 4 as final while the report itself says the current adapter is "not final animated atlas infrastructure."
- QA/HCI final Gate 4 should wait for either:
  - the intended ECS/DOTS-compatible animated atlas renderer and final multi-frame atlas frames, or
  - an explicit user waiver accepting the temporary ECS-driven `SpriteRenderer` adapter for this milestone.

Cross-lane notices:
- Gameplay made meaningful progress on playability and golden-path proof.
- The remaining blocker is now presentation/infrastructure/art readiness, not first-control pacing.
- UI and Support/FTUE have no new task from this report unless QA/HCI later finds a concrete regression.
- PM recommendation is to route final animated atlas runtime presentation rather than waive it, because the user explicitly objected to seeing SpriteRenderer in the game.

Next gate/task:
Gameplay/Art should replace the temporary `MissionRuntimeSpriteRendererRuntime` adapter with the intended ECS/DOTS-compatible animated atlas presentation and provide final or explicitly milestone-approved multi-frame infantry atlas states before QA/HCI final Gate 4 acceptance.
