Lane:
Designer

Task:
Revise the README/design-index dedupe pass so the structure remains concise but the unapproved offensive-command premise is not treated as canonical.

Files changed:
- `README.md`
- `Design/README.md`
- `Design/AgentReports/2026-05-08_designer_readme-dedupe-neutral-premise.md`

Contracts touched:
- Documentation source-of-truth hierarchy only.
- Root README remains the concise project entry point.
- `Design/README.md` remains the complete design index.
- `Design/Command_Offensive_Premise_Alignment.md` is now marked as a pending PM/user premise proposal, not an accepted canonical product premise.
- M01 critical-path scope, Gate 4 criteria, lane ownership, validation requirements, PM commit/push ownership, and runtime contracts were preserved.

User-visible behavior:
No runtime behavior changed. Documentation now avoids presenting the offensive-command premise as accepted before PM/user approval.

Validation run:
- `sed -n '1,220p' Design/AgentReports/2026-05-08_pm_designer-readme-dedupe-review.md`
- `sed -n '1,220p' Design/AgentReports/2026-05-08_pm_designer-offensive-command-premise-review.md`
- `sed -n '1,120p' README.md`
- `sed -n '1,70p' Design/README.md`
- `rg -n "preparing and executing command operations|proactive field commander|hostile factions embedded|Command_Offensive|pending PM/user|canonical product premise|Source Of Truth|designer_heartbeat|designer_current|pm_heartbeat|M01_CRITICAL_PATH|commit/push|ready to expand" README.md Design/README.md`
- `test -f` checks for every README/design-index source-of-truth link used by the edited sections.
- `git diff -- README.md Design/README.md`
- `git status --short README.md Design/README.md Assets/Game/Scripts Assets/Tests`

Validation result:
- Passed for the focused documentation fix.
- Root README no longer says the product direction is "preparing and executing command operations against hostile factions embedded in civilian districts."
- Root README no longer lists `Design/Command_Offensive_Premise_Alignment.md` as a high-priority canonical design source; it is separated under `Pending PM/user premise decision`.
- `Design/README.md` still indexes the command-premise doc, but marks it as pending and non-canonical until PM/user explicitly accepts it.
- PM/Designer heartbeat paths, `designer_current.md`, `M01_CRITICAL_PATH.md`, and PM commit/push ownership remain referenced.
- No source/runtime files were modified by this Designer pass. Git status still shows unrelated pre-existing runtime/source changes from other lanes; those were not touched or reverted.

Known gaps:
- PM/user still needs to decide whether to accept, reject, or defer the offensive-command premise.
- Several deeper design docs may still contain premise changes from the earlier proposal. This pass only corrected README/design-index canonicalization because that is the current Designer priority.
- Root README still contains contributor-facing architecture, UI roadmap, gameplay roadmap, runtime-pattern, and testing guidance. A later PM-approved contributor-guide split could reduce it further.

Cross-lane impacts:
- PM can now review the dedupe structure separately from the unresolved product-premise decision.
- Gameplay, UI, QA/HCI, Art/Atlas, and Support/FTUE should not change current Gate 4 work based on the pending command-premise proposal.
- Designer did not change lane task files, source code, Unity prefabs, captures, assets, or validation gates.

Next recommended task:
PM should review this revised README/design-index dedupe handoff. Separately, PM/user should decide whether the offensive-command premise is accepted, rejected, or deferred before any deeper product-premise docs are canonicalized.
