# Lane
Designer

# Task
Assess the QA/HCI selected-readability regression gate against the accepted rejection-informed M01 visual/scale/readability contract.

# Files changed
- `Design/AgentReports/2026-05-08_designer_qa-hci-user-feedback-regression-gate-review.md`

# Contracts touched
- No product contracts changed.
- Reviewed against:
  - `Design/AgentTasks/designer_current.md`
  - `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`
  - `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`
  - `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`

# User-visible behavior
No runtime behavior changed by Designer.

The latest selected-readability evidence is ready for PM/user review from Designer scope. The refreshed captures no longer show the previously rejected screen-covering green marker, yellow placeholder selection square, visibly squashed infantry, or unexplained red sitting enemy artifact.

# Validation run
- Read `Design/AgentTasks/designer_current.md`.
- Confirmed both expected reports now exist:
  - `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`
  - `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`
- Read `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`.
- Checked capture files:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- Viewed both selected first-control captures.

# Validation result
accepted for PM/user review

Designer accepts QA/HCI's regression gate for the current rejection-fix review, with final acceptance still dependent on PM/user visual review.

The QA/HCI matrix maps every user rejection row to direct evidence and reports the final `Chapter01M01PlayModeValidationTests` pass at `8/8`, `0` failed. This satisfies the Designer requirement that the user feedback matrix be closed before asking for another review.

# Designer assessment
- ECS/atlas direction: accepted by report evidence. QA/HCI validates that public unit visuals use ECS visual entities and rejects the old wrapper path.
- Scale/aspect: accepted for review. The still captures read smaller and less squashed than the rejected pass; QA/HCI reports runtime scale near `0.15`.
- Markers: accepted for review. The rejected huge marker and yellow placeholder square are not visible in the reviewed selected captures.
- Animation/pose: accepted for review based on QA/HCI automated motion evidence. Still captures alone cannot prove movement quality, but QA/HCI covered phase/local-pose movement.
- Enemy artifact: accepted for review. The reviewed captures do not show the previous unexplained red sitting object.

# Known gaps
- This is not final art signoff. Final atlas art, final multi-frame animation polish, enemy variant polish, and final VFX remain Art/Atlas responsibilities.
- The selected first-control still captures make only two world soldiers clearly legible, while runtime validation reports four ECS soldier visual entities. This is not a blocker for the rejected-item gate, but PM should ask the user whether the squad read is acceptable in the new review.
- Selection rings are intentionally small/grounded. They no longer read as placeholder squares, but PM/user should still confirm that selected-state readability is strong enough on device.
- Motion quality is proven by automated PlayMode evidence, not by a video artifact.

# Cross-lane impacts
- PM can now request user review of the refreshed selected-readability captures from Designer scope.
- Gameplay and QA/HCI have closed the expected handoffs for the current Designer waiting state.
- Designer has no further fallback work unless PM assigns a concrete follow-up after user review.

# Next recommended task
PM should ask the user to review the refreshed selected-readability captures and explicitly confirm:

1. Unit scale/readability is acceptable at the new smaller target.
2. Selection state is readable enough without returning to placeholder squares.
3. No huge target marker or unclear red enemy artifact remains.
4. Temporary art is acceptable to proceed while Art/Atlas continues final production polish.
