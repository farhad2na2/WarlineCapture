# PM Designer README Dedupe Review

Lane: PM

Task: Review Designer handoff `Design/AgentReports/2026-05-08_designer_readme-design-index-dedupe.md`.

Files changed:
- `Design/AgentReports/2026-05-08_pm_designer-readme-dedupe-review.md`

Contracts touched:
- Documentation source-of-truth hierarchy only.
- No runtime implementation contract changed.

User-visible behavior:
- No runtime behavior changed.

Validation run:
- Reviewed the Designer handoff.
- Reviewed the root `README.md` diff.
- Checked current lane priorities in `Design/AgentTasks/*_current.md`.

Validation result:
- Needs fixes before PM acceptance.
- The dedupe structure is directionally correct: root README becomes the project entry point and `Design/README.md` remains the complete index.
- However, the current README diff also promotes the offensive-command premise language and links `Design/WarlineCapture_Command_Offensive_Premise_Alignment.md` as a high-priority design source.
- PM has not accepted that premise as canonical because `Design/AgentReports/2026-05-08_pm_designer-offensive-command-premise-review.md` explicitly requested PM/user decision first.
- Therefore the README dedupe should be revised to avoid canonicalizing the offensive-command premise unless PM/user accepts that direction.

Known gaps:
- User has not answered `accept offensive premise` or `defer premise change`.
- Root README dedupe cannot be safely committed as canonical while it includes unaccepted product-premise language.

Cross-lane impacts:
- Designer owns a narrow revision: keep the README/design-index dedupe, but neutralize or mark the offensive-command premise as pending until PM/user accepts it.
- Gameplay, UI, QA/HCI, Art/Atlas, and Support/FTUE should not change current Gate 4 work based on the unaccepted premise.
- PM should keep the current QA/HCI Gate 4 routing unchanged.

Next recommended task:
- Designer should produce `Design/AgentReports/2026-05-08_designer_readme-dedupe-neutral-premise.md` with a root README dedupe that does not make the offensive-command premise canonical unless PM/user approves it.
