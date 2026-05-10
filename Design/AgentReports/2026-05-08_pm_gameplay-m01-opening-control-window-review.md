Gate:
M01 golden playthrough / Gameplay playable-loop blocker

Status:
needs fixes

Reason:
Gameplay landed `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` and fixed important parts of the blocker, but the handoff does not yet satisfy the PM-reset Gate 4 golden-playthrough requirement.

Validation accepted:
- Assigned Gameplay workspace was used: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `Chapter01M01PlayModeValidationTests` passed 7/7.
- Public Quick Custom and Campaign launch still reach the M01 production Match route.
- New coverage proves a protected first-control window before lethal hostile fire.
- New coverage proves select, move-to-cover pathing components, attack, patrol damage/destruction, and result-route readiness at scene/runtime level.
- M01 remains infantry-only in the reported validation: one command squad, one hostile patrol, no player vehicles, no build/transport/base mechanics, and no extra player unit type.
- ECS runtime ids remain preserved for `unit.player.rifle_squad_01` and `unit.enemy.patrol_01`.
- No banned broad runtime scene-search usage was found in the touched focused files.

Validation still needed:
- Full public UI golden playthrough is not yet proven end to end. The Gameplay report says this explicitly: it does not click through the final objective/result popup from the public UI path in one end-to-end UI test.
- Final visible unit presentation is not accepted as final ECS animated atlas infrastructure. The report still uses Unity `SpriteRenderer` as `MissionRuntimeSpriteRendererRuntime`, described as a temporary ECS-owned presentation adapter.
- Final multi-frame atlas art is missing. `FinalAtlasArtReady` remains `0`, and idle/move/attack/damaged state ids fall back to current M01 infantry manifest source art.
- Remaining log warnings still need QA/HCI classification after the Gameplay fix is accepted or rerun.

Cross-lane notices:
- Gameplay made real progress and closed the immediate auto-death issue for automated coverage, but Gate 4 cannot pass yet.
- QA/HCI should not perform final Gate 4 acceptance from this handoff as-is. It can run an affected verification pass only if PM wants early blocker classification, but the full public UI golden-playthrough gap remains.
- UI may become involved if the missing end-to-end public UI result-popup clickthrough is blocked by UI route, input, HUD, or result-flow behavior rather than Gameplay runtime.
- PM/user owns the decision whether a temporary ECS-driven `SpriteRenderer` adapter is acceptable for the next review milestone. Based on the user's stated expectation, PM recommendation is not to accept it as final Gate 4 runtime presentation.

Next gate/task:
Route a follow-up before Gate 4 acceptance:
- Gameplay/UI jointly or Gameplay first should prove the full public UI golden playthrough through objective/result popup.
- Gameplay should either replace the temporary `SpriteRenderer` adapter with the intended ECS/DOTS-compatible animated atlas renderer, or get an explicit user waiver for a temporary ECS-driven adapter.
- QA/HCI reruns final Gate 4 only after the full public golden path is proven or PM explicitly asks for an early blocker-classification pass.
