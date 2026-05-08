Status:
accepted; QA/HCI still blocked on PM/user temporary-art decision before final Gate 4 rerun

Lane:
PM

Task:
Review UI handoff `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md` for the M01 infantry-only HUD scope blocker.

Files changed:
- `Design/AgentReports/2026-05-08_pm_ui-m01-infantry-only-hud-scope-review.md`

Contracts touched:
- M01 infantry-only player-facing HUD scope.
- Public M01 golden path HUD support.
- Gate 4 final QA/HCI rerun readiness.

User-visible behavior:
- Accepted UI claim: public M01 no longer presents APC, Tank, air support, Build, vehicle production, transport, or base/build affordances as usable first-mission options.
- Rifle squad selected-state HUD and Move/Attack command flow remain available.

Validation run:
- Reviewed `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`.
- Checked the report against the standard WarlineCapture handoff format.
- Read `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`.
- Searched touched UI/test surfaces for the new controller, suppressed HUD roots, and banned runtime scene-search patterns.

Validation result:
- Accepted for PM handoff.
- UI reports prefab rebuild passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.
- UI reports `Chapter01M01PlayModeValidationTests` passed 8/8.
- UI reports public Campaign and Quick Custom routes still reach the production slice, golden playthrough still reaches result popup, and selected-first-control captures show the infantry-only HUD state.
- The new runtime controller uses serialized root references and does not introduce runtime scene-search usage.

Known gaps:
- PM/user still must approve or reject the temporary M01 infantry art package from `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`.
- `FinalAtlasArtReady` remains `0`.
- QA/HCI still needs the final Gate 4 rerun after the art decision.
- Manual touch/camera ergonomics, invalid-command recovery, assistant Stop/Show Me/result behavior, and final log/performance classification remain QA/HCI scope for the rerun.

Cross-lane impacts:
- UI can move to waiting after this accepted handoff.
- QA/HCI is unblocked on the HUD issue but still waiting for PM/user temporary-art decision.
- Gameplay and Art/Atlas remain waiting unless the art decision or QA/HCI rerun creates concrete follow-up work.
- Support/FTUE remains waiting unless QA/HCI finds a concrete assistant/FTUE issue.

Next recommended task:
- PM/user should approve or reject the temporary M01 infantry art package.
- After that decision, QA/HCI should rerun final Gate 4 from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
