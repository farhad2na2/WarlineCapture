Lane:
Designer

Task:
Define a concise M01 tactical metric scale/readability contract after the temporary Gate 4 art rejection, so Art/Atlas, Gameplay, and QA/HCI do not guess scale, selection, movement, or public unit-presentation rules.

Files changed:
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/README.md`
- `Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md`

Contracts touched:
- Added a dedicated M01 metric scale/readability contract.
- M01 visual approval now uses soldier height about `1.8m`, building door height about `2.3m`, and road/building footprint context as scale calibration.
- Building scale must be derived from door/footprint/readability, not tiny decor values.
- Selection treatment must be small, grounded, per-soldier or equivalent subtle formation treatment, not a huge screen-covering marker.
- Movement must show plausible infantry run/move animation while units travel.
- Public M01 unit visuals must be ECS entity / atlas-backed and must not present SpriteRenderer unit presentation as the accepted public path.
- Replaced the old M01 production-contract `0.10` infantry scale reference with the new metric scale/readability contract.
- Added the new contract to `Design/README.md`.

User-visible behavior:
No runtime behavior changed. The design contract now gives implementation and QA lanes a concrete standard for the next M01 visual/readability pass.

Validation run:
- `sed -n '1,240p' Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `sed -n '1,220p' Design/AgentTasks/designer_pm_message.md`
- `sed -n '1,220p' Design/AgentTasks/M01_CRITICAL_PATH.md`
- `sed -n '1,320p' Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `sed -n '1,260p' Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
- `rg -n "M01_Metric|1\\.8m|2\\.3m|0\\.10|SpriteRenderer|selection|run animation|ECS|atlas-backed|tiny decor|door" Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md Design/README.md`
- `test -f` checks for the new contract and required input docs/reports.
- `git diff -- Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md Design/README.md`
- `git status --short Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md Design/README.md Assets/Game/Scripts Assets/Tests`

Validation result:
- Passed for documentation/contract clarity.
- The new contract includes the user anchors: `1.8m` soldier, `2.3m` door, and road/context calibration.
- The M01 production contract now references the new metric contract and no longer accepts the old `0.10` infantry scale anchor.
- The new contract explicitly rejects tiny building/decor scale, huge selection markers, sliding/teleport movement, missing run animation, and SpriteRenderer public unit presentation as an accepted Gate 4 path.
- `Design/README.md` now indexes the new contract.
- No source/runtime files were modified by this Designer pass. Git status still shows unrelated source/runtime changes from other lanes, including gameplay scale/presentation work; those were not touched or reverted.

Known gaps:
- This is a design contract, not implementation.
- Art/Atlas still needs to provide or approve an M01 scale/readability asset package against this contract.
- Gameplay still owns runtime ECS/atlas presentation, scale application, selection treatment, movement speed, and run animation proof.
- QA/HCI still needs to rerun Gate 4 after Art/Atlas and Gameplay handoffs are complete.

Cross-lane impacts:
- Art/Atlas should use `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md` for asset scale/readability recommendations.
- Gameplay should use it for runtime scale, selected-state treatment, movement speed, and atlas move/run validation.
- QA/HCI should use it as the visual/readability checklist for the next public M01 rerun.
- UI and Support/FTUE have no immediate action unless later QA/HCI finds concrete HUD, assistant, or FTUE regressions.

Next recommended task:
PM should review this Designer handoff. Then Art/Atlas and Gameplay should align their rejected-temp-art fix evidence to this contract before QA/HCI reruns Gate 4.
