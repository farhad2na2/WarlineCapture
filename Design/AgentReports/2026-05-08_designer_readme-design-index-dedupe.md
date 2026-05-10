Lane:
Designer

Task:
Reduce duplication between the root `README.md` and `Design/README.md`, keeping the root README as a concise project entry point and `Design/README.md` as the complete design index.

Files changed:
- `README.md`
- `Design/AgentReports/2026-05-08_designer_readme-design-index-dedupe.md`

Files reviewed but not edited in this pass:
- `Design/README.md`
- `Design/AgentTasks/README.md`
- `Design/AgentTasks/pm_heartbeat.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_Designer_Role_And_Documentation_Workflow.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`
- `Design/AgentReports/2026-05-08_designer_docs-readme-optimization.md`
- `Design/AgentReports/2026-05-08_pm_designer-docs-readme-optimization-review.md`

Contracts touched:
- Documentation source-of-truth hierarchy only.
- Root README now explicitly identifies itself as the project entry point and `Design/README.md` as the complete design index.
- Root README now lists only high-priority design links and lane entry points instead of duplicating the full design, visual-lock, production-reference, audio, monetization, marketing, art, and balance inventories.
- M01 critical-path scope, Gate 4 criteria, lane ownership, validation requirements, PM commit/push ownership, product scope, and implementation contracts were preserved.

User-visible behavior:
No runtime behavior changed. The root README is shorter and more navigable for project contributors while `Design/README.md` remains the complete design index.

Validation run:
- `sed -n '1,240p' Design/AgentTasks/designer_heartbeat.md`
- `sed -n '1,260p' Design/AgentTasks/designer_current.md`
- `sed -n '1,260p' README.md`
- `sed -n '1,280p' Design/README.md`
- `sed -n '1,220p' Design/AgentTasks/README.md`
- `sed -n '1,220p' Design/AgentTasks/pm_heartbeat.md`
- `sed -n '1,230p' Design/AgentTasks/M01_CRITICAL_PATH.md`
- `sed -n '1,220p' Design/WarlineCapture_Designer_Role_And_Documentation_Workflow.md`
- `sed -n '1,260p' Design/WarlineCapture_Agent_Coordination_Workflow.md`
- `sed -n '1,220p' Design/AgentReports/2026-05-08_designer_docs-readme-optimization.md`
- `sed -n '1,220p' Design/AgentReports/2026-05-08_pm_designer-docs-readme-optimization-review.md`
- `wc -l README.md Design/README.md`
- `rg -n "Design/README|designer_current|designer_heartbeat|pm_heartbeat|M01_CRITICAL_PATH|commit/push|ready to expand|VisualLock|SCN-|POP-|PREFAB-|Source Of Truth" README.md Design/README.md Design/AgentTasks/README.md Design/WarlineCapture_Agent_Coordination_Workflow.md`
- `test -f` checks for every root README source-of-truth link added or preserved in the edited sections.
- `git status --short README.md Design/README.md Design/AgentReports/2026-05-08_designer_readme-design-index-dedupe.md Assets/Game/Scripts Assets/Tests`
- `git diff -- README.md`

Validation result:
- Passed for documentation structure.
- Root README line count reduced from 477 lines to 292 lines.
- Root README still mentions the correct PM and Designer heartbeat files, current Designer task file, M01 critical-path file, and PM commit/push ownership.
- Root README still preserves Saga/Operation/Quick Custom structure, 2D isometric tactical/strategic split, M01 infantry-only production lock, and Designer workflow link.
- The edited README no longer duplicates the full visual-lock target inventory or long design/reference inventories that belong in `Design/README.md`.
- No source/runtime files were modified by this Designer pass. Current git status still shows pre-existing source/runtime changes from other lane work; those were not touched or reverted.

Known gaps:
- `Design/README.md` still owns a large full index by design. This pass did not prune the design index because the assignment explicitly keeps it as the complete design map.
- Root README still contains architecture, UI roadmap, gameplay roadmap, runtime-pattern, and testing guidance. Those are contributor-facing enough to remain for now; a future PM-approved pass could move some of that into a dedicated contributor guide if desired.
- The repo contains unrelated uncommitted runtime/source changes from other active lane work, so PM should review/commit documentation changes separately from implementation changes.

Cross-lane impacts:
- PM has a clearer README/design-index split to review.
- Designer did not change any active lane task file, source code, Unity prefab, capture, asset, or validation gate.
- Gameplay, UI, QA/HCI, Support/FTUE, and Art/Atlas keep their current M01 ownership and blockers unchanged.

Next recommended task:
PM should review this README dedupe handoff. If accepted, PM can either keep Designer waiting for the next documentation priority or route a later contributor-guide split for architecture/UI/gameplay implementation guidance.
