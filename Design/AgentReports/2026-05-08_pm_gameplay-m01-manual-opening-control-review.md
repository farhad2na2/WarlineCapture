# PM Gameplay M01 Manual Opening Control Review

Lane: PM

Task: Review Gameplay handoff `Design/AgentReports/2026-05-08_gameplay_m01-manual-opening-control-fix.md`.

Files changed:
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-manual-opening-control-review.md`

Contracts touched:
- M01 First Contact opening-control acceptance.
- Gate 4 routing.
- Temporary-art approval sequencing.

User-visible behavior:
- No runtime behavior changed by PM.
- Gameplay reports that the public M01 route now gives the player a protected no-input first-control window before selection/first move.

Validation run:
- Reviewed the Gameplay handoff.
- Reviewed focused diffs in `MissionRuntimeOpeningControlProtectionSystem.cs`, `UnitAttackSystem.cs`, `UnitEngagementSystem.cs`, and `Chapter01M01PlayModeValidationTests.cs`.
- Checked current lane priorities in `Design/AgentTasks/*_current.md`.

Validation result:
- Accepted for QA/HCI rerun.
- The handoff is complete and uses the standard report format.
- Gameplay reproduced the manual-route failure in validation, changed the opening-control guard, and reports focused PlayMode validation passed 8/8 in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- The acceptance is not final Gate 4 approval and is not final art approval. It only means the blocker is ready for QA/HCI verification.

Known gaps:
- PM has not personally completed a manual Unity review in this report.
- `FinalAtlasArtReady` remains `0`.
- The protected opening intentionally prioritizes first-mission reviewability and player safety over immediate hostile lethality; QA/HCI must verify pacing and readability.
- User art approval should wait until QA/HCI confirms the route is reviewable.

Cross-lane impacts:
- Gameplay can move back to waiting unless QA/HCI finds a concrete regression.
- QA/HCI now owns the next focused Gate 4 rerun from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- Art/Atlas remains waiting until QA/HCI confirms the review route is stable enough for a renewed PM/user art decision.
- UI remains waiting unless QA/HCI finds a concrete HUD regression.
- Support/FTUE remains waiting unless QA/HCI finds a concrete assistant or tutorial issue.

Next recommended task:
- QA/HCI should rerun the focused M01 Gate 4 route, specifically validating no-input opening safety, select, first move, attack/result flow, infantry-only HUD, ECS atlas presentation, selected state, projectile scale, and whether the route is now suitable for a short PM/user temporary-art review.
