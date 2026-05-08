# PM Manual Opening Control Regression

Lane: PM

Task: Reclassify the temporary-art approval request after user manual review found the enemy kills the player before a relaxed art check is possible.

Files changed:
- `Design/AgentReports/2026-05-08_pm_manual-opening-control-regression.md`

Contracts touched:
- Gate 4 PM routing.
- M01 First Contact opening-control acceptance.
- Temporary-art approval flow.

User-visible behavior:
- User opened M01 manually and was killed by the enemy before having a chance to inspect/select/move the squad and review temporary art.

Validation run:
- Compared user manual finding against current lane tasks and the Gameplay opening-control handoff.
- Reviewed `Design/AgentTasks/gameplay_current.md`.
- Reviewed `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md`.

Validation result:
- Needs fixes / blocked.
- The prior PM approval request for temporary art was premature because the requested user review path is not stable.
- Art approval is no longer the immediate blocker until Gameplay proves the manual public M01 path gives the player a safe first-control window.
- The automated Gameplay report claims protection exists, but the user-facing Unity path contradicts that claim. Manual-user-path evidence outranks the report for PM gating.

Known gaps:
- Unknown whether the manual Unity project opened by the user has the latest Gameplay changes, whether the protection release condition fires too early, or whether the public route bypasses the protected runtime composition.
- No fresh PM-run Unity validation was completed in this report.
- Temporary art remains unsigned, but approval should not be requested again until the opening-control path is reviewable.

Cross-lane impacts:
- Gameplay owns the immediate fix/proof: M01 must open with enemy fire suppressed or non-lethal until the player can select the rifle squad and issue the first move order.
- QA/HCI should not rerun Gate 4 or ask for art approval until Gameplay provides fresh manual/public-route evidence.
- Art/Atlas remains waiting, but is no longer the next user-facing decision.
- UI and Support/FTUE should remain waiting unless Gameplay/QA finds a concrete UI or teaching issue.

Next recommended task:
- Gameplay should produce a focused fix/report proving the exact user review route: open M01 in Unity, deploy, wait briefly without input, select rifle squad, issue first move, and confirm the enemy cannot kill the squad before that first movement review.
