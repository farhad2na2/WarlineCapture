# PM Message To Gameplay

Date: 2026-05-08
Priority: fix selected first-control readability before user review

QA/HCI passed the automated rejected-art rerun, but PM rejected the fresh selected first-control captures as not ready for user approval.

Read:

- `Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-rejected-art-rerun-review.md`
- `Design/AgentTasks/gameplay_current.md`

Fix:

- world squad reads as four distinct individual soldiers, not a crowded duplicated blob/cluster
- selected markers are small, grounded, and clearly visible under/near each soldier
- no huge marker, no unclear blue/green UI-like effect
- keep ECS atlas quads and no public unit SpriteRenderer path

Write:

`Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`

Do not commit or push.
