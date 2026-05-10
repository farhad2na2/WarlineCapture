Lane:
Designer

Task:
Refresh the M01 visual/scale/readability contract after the user rejected the selected-readability pass and identified repeated issues with renderer-wrapper visuals, marker size, selection affordance, animation pose/state, and process coverage.

Files changed:
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`

Contracts touched:
- M01 visual scale/readability contract.
- M01 First Contact visual gate.
- User feedback regression gate expectations for the next QA/HCI pass.

User-visible behavior:
No runtime behavior changed. The design contract now gives Art/Atlas, Gameplay, UI, and QA/HCI explicit rejection checks before another user review is requested.

Validation run:
- `sed -n '1,260p' Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `sed -n '1,220p' Design/AgentTasks/designer_pm_message.md`
- `sed -n '1,220p' Design/AgentTasks/user_feedback_review_gate.md`
- `sed -n '1,280p' Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `sed -n '1,240p' Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
- `rg -n "1\\.8m|2\\.3m|0\\.15|0\\.2|two soldier footsteps|foot pixels|body/formation|crouched|sitting|idle animation|MeshRenderer|MeshFilter|SpriteRenderer|placeholder|User Feedback Regression|repeated feedback" Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `test -f` checks for required PM report, designer PM message, user feedback gate, and M01 contract files.
- `git diff -- Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `git status --short Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md Assets/Game/Scripts Assets/Tests`

Validation result:
- Passed for documentation/contract coverage.
- Contract now includes the user's scale anchors: soldier about `1.8m`, building door about `2.3m`, and road/building footprint context.
- Contract now states scale must preserve aspect ratio and should not force a too-large/squashed value; it records the user's note that the current soldier visual read better near `0.15` than `0.2`.
- Contract now defines selection usability: body/formation footprint must be selectable, not only exact foot pixels.
- Contract now defines selected-state and point-command marker limits, including move/attack/target markers around two soldier footsteps wide.
- Contract now rejects placeholder squares, huge green/blue blobs, and screen-covering markers.
- Contract now requires correct idle animation, standing run/move animation while moving, and rejects crouched/sitting/kneeling/hit/death/artifact frames for normal movement.
- Contract now blocks accepted public M01 unit/building gameplay presentation through `SpriteRenderer`, `MeshRenderer`, `MeshFilter`, old child `Model`, or unclassified GameObject renderer-wrapper paths.
- Contract now includes a QA-readable user feedback regression matrix, including repeated-feedback process failure as a rejection condition.
- No source/runtime files were modified by this Designer pass. Git status still shows unrelated source/runtime changes from other lanes; those were not touched or reverted.

Known gaps:
- This is a design contract refresh only, not implementation or QA validation.
- Gameplay still owns ECS visual path, runtime marker behavior, selection hit targeting, movement/idle animation integration, scale/aspect application, and enemy/artifact fixes.
- Art/Atlas still owns marker, animation-frame, scale/aspect, enemy clarity, and artifact guidance/assets.
- UI still owns marker/selection overlay audit if any marker/selection affordance is UI-owned.
- QA/HCI still owns the rejection-aware validation matrix and must not request user review until the feedback items are fixed, blocked with owners, or waived by the user.

Cross-lane impacts:
- Gameplay, Art/Atlas, UI, and QA/HCI should use `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md` as the rejection-informed visual checklist.
- PM should enforce `Design/AgentTasks/user_feedback_review_gate.md` before asking the user for another selected-readability review.
- Support/FTUE has no immediate action unless later QA/HCI finds a concrete assistant or FTUE issue.

Next recommended task:
PM should review this Designer handoff, then keep the rejection gate blocked until Gameplay, Art/Atlas, UI if applicable, and QA/HCI close every user feedback item with evidence or explicit user waiver.
