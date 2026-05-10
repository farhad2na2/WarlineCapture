Status:
accepted for Gameplay handoff; Gate 4 still blocked pending refreshed QA workspace and final HCI review

Lane:
PM

Task:
Review QA/HCI validation of `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` against the M01 golden-playthrough gate and current lane priorities.

Files changed:
- `Design/AgentReports/2026-05-08_pm_qa-hci-m01-opening-control-validation-review.md`

Contracts touched:
- M01 golden playthrough gate: Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy -> select rifle squad -> move to tutorial cover -> attack hostile patrol -> enemy destroyed/neutralized -> objective/result popup.
- M01 infantry-only scope: one player rifle squad type, one enemy patrol type, no player vehicles.
- Public M01 visible-unit contract: ECS animated atlas-backed visible units, four distinct soldier renderers under one squad entity, selected state visible in world and HUD, tactical projectile/impact scale, no public `MissionRuntimeSpriteRendererRuntime`, no visible old `Model` route, no separate child `Destroyed` dependency.
- Gate 4 ownership contract: Gameplay/UI may provide evidence, but QA/HCI owns independent final validation.

User-visible behavior:
- QA/HCI accepted the Gameplay blocker fix evidence for the opening-control window and ECS atlas presentation.
- The user should not review Gate 4 yet because QA/HCI has not completed the independent final HCI pass in the QA workspace.

Validation run:
- Reviewed `Design/AgentReports/2026-05-08_qa-hci_gameplay-m01-opening-control-window-validation.md`.
- Checked the report for the standard WarlineCapture handoff sections.
- Compared QA/HCI claims against `Design/AgentTasks/qa-hci_current.md`, `Design/AgentTasks/gameplay_current.md`, and the current PM Gate 4 acceptance rule in the heartbeat instructions.

Validation result:
- Accepted as a QA/HCI validation of the Gameplay handoff only.
- Not accepted as final Gate 4 closeout.
- QA/HCI reports Gameplay workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity1` passed `Chapter01M01PlayModeValidationTests`: 8/8, including public M01 golden path to result popup, opening-control protection, and ECS atlas presenter assertions.
- QA/HCI also reports focused static checks for `MissionRuntimeAtlasQuadRuntime`, rejection of `MissionRuntimeSpriteRendererRuntime`, stripped M01 `UnitDestroyedVisualReference` / `UnitDestroyedVisualInitialized`, selected-state marker, tactical projectile trace limits, and `FinalAtlasArtReady = 0`.

Known gaps:
- `WarlineCapture-CodexUnity3` is stale and cannot be used for independent final Gate 4 evidence until refreshed with the latest handoff state.
- Final multi-frame infantry atlas art is not approved. `FinalAtlasArtReady = 0` remains an explicit temporary-art marker.
- Final Gate 4 still needs QA/HCI focused HCI review: first-control readability, touch/camera ergonomics or documented substitute, invalid command recovery, assistant ownership/Stop behavior, performance/log readiness, and active visual review of atlas art, selected markers, and projectile/impact VFX.
- The QA/HCI report does not include every standard handoff heading as literal headings. It contains the necessary substance for this validation, but future QA/HCI reports should include all standard fields explicitly.

Cross-lane impacts:
- QA/HCI: refresh `/Users/farhad/Projects/WarlineCapture-CodexUnity3` before final Gate 4 validation, then run the focused HCI pass.
- Gameplay: no immediate fix requested from this validation unless PM/user rejects temporary art readiness or QA/HCI final review finds defects.
- UI: no current action unless final QA/HCI finds HUD/objective/result or selected-state UX gaps.
- Support/FTUE: no current action unless final QA/HCI finds assistant/Stop/FTUE ownership gaps.
- User/Art approval: PM recommends allowing temporary infantry source art into Gate 4 HCI only as a labeled temporary-art pass, while requiring final atlas art approval before M01 visual signoff.

Next recommended task:
- Refresh `WarlineCapture-CodexUnity3` from the latest project state.
- QA/HCI should run one independent final Gate 4 HCI validation pass from the public route.
- Notify the user only when that pass is ready for review, blocked, or needs art approval.
