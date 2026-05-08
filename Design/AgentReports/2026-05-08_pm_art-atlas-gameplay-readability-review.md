Status:
needs PM/user art decision; Gate 4 still blocked pending UI HUD fix and QA/HCI rerun

Lane:
PM

Task:
Review the new Art/Atlas and Gameplay handoffs for M01 infantry readability, selected-state clarity, and temporary atlas art readiness.

Files changed:
- `Design/AgentReports/2026-05-08_pm_art-atlas-gameplay-readability-review.md`

Contracts touched:
- M01 public first-control readability.
- M01 ECS atlas-backed infantry presentation.
- M01 selected-state clarity.
- M01 temporary/final art-readiness decision.
- Gate 4 final QA/HCI acceptance.

User-visible behavior:
- Gameplay reports improved public selected first-control captures with four readable infantry figures and a visible cyan selection marker.
- Art/Atlas reports the current soldier sheet is suitable only as a temporary-art approval package, not final/milestone art by default.
- The user needs to approve or reject temporary Gate 4 infantry art before QA/HCI can treat visual signoff as unblocked.

Validation run:
- Reviewed `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`.
- Reviewed `Design/AgentReports/2026-05-08_gameplay_m01-unit-readability-selection-art.md`.
- Checked both reports against the standard WarlineCapture handoff format.
- Compared their findings with `Design/AgentTasks/art-atlas_current.md`, `Design/AgentTasks/gameplay_current.md`, `Design/AgentTasks/qa-hci_current.md`, and the current Gate 4 blocker list.

Validation result:
- Art/Atlas handoff accepted as a valid approval-needed report.
- Gameplay handoff accepted as a valid temporary-art/runtime readability integration report.
- Gate 4 is not ready for final QA/HCI rerun until:
  - PM/user approves or rejects the temporary infantry art package.
  - UI lands `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`.
  - If approved, QA/HCI refreshes the QA workspace and reruns Gate 4 with the latest Gameplay/UI state.
- Gameplay reports PlayMode validation passed 8/8 in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, no new runtime scene-search usage, golden path still reaches result popup, and selected first-control captures were refreshed.

Known gaps:
- `FinalAtlasArtReady` remains `0`.
- Current player soldier sheet is key-pose temporary art, not final multi-frame animation.
- No enemy red-accent/tinted infantry patrol variant is present in the Art/Atlas package.
- `vfx.impact.light` and final destroyed/impact VFX art remain unresolved.
- UI HUD scope fix is still missing; M01 still needs APC/Tank/air support/Build affordances suppressed or locked for the infantry-only tutorial.

Cross-lane impacts:
- PM/user: approve temporary Gate 4 infantry art, request a red-accent enemy variant, or reject and require Art/Atlas to generate/source final/milestone assets.
- Art/Atlas: continue only if PM/user rejects temporary art or asks for an enemy variant/final VFX package.
- Gameplay: continue only after the art decision if integration changes are needed; current runtime readability handoff is acceptable as temporary-art integration evidence.
- UI: still owns M01 HUD scope mismatch.
- QA/HCI: wait for UI fix and PM/user art decision before final Gate 4 rerun.

Next recommended task:
- Ask PM/user to review the selected first-control captures and approve or reject temporary Gate 4 infantry art.
- Continue monitoring for the UI HUD scope handoff.
