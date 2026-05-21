# PM UI Correction: Plan-Only Report Rejected, Implementation Required

Date: 2026-05-16
Lane: PM
Target lane: UI
Status: active

## Decision

`Design/AgentReports/2026-05-16_ui_visual-target-match-plan-pop05-scn02-scn08.md` is rejected as a deliverable.

Reason: it produced no user-visible implementation and explicitly reported:

- no Unity prefab, script, generated asset, runtime binding, or capture changes
- user-visible behavior: none
- planning-only gate completed

That is not acceptable. The purpose of the visual target-match process is to implement the approved target-lock UI, not to produce paperwork instead of implementation.

## Corrected UI Task

UI must now produce:

`Design/AgentReports/2026-05-16_ui_visual-target-match-implementation-v2.md`

Scope:

- `SCN-02_MainMenu`
- `POP-05_MissionResult`
- `SCN-08_RTSBattleHUD` / M01 Match HUD

Required behavior:

- Implement UI-owned target-match fixes in Unity prefabs/scripts/assets.
- Use the rejected planning report only as a mismatch checklist.
- Start with `SCN-02_MainMenu`, then `POP-05_MissionResult`, then UI-owned `SCN-08_RTSBattleHUD` regions.
- Replace old shell/prefab composition when it conflicts with the target.
- Do not preserve old shells merely because they already have live data wiring.
- Preserve live TMP/data binding after the visual structure is matched.

## Missing Assets Or Gameplay Blockers

Missing assets are not permission to skip all implementation.

If an exact Art/Atlas slice is missing, UI must:

- implement all nonblocked surrounding UI regions now
- list the exact missing asset name, target region, required dimensions if known, and Art/Atlas as owner
- include fresh capture evidence showing what was implemented and what remains blocked

Gameplay camera/background/unit blockers are not permission to skip HUD work.

If SCN-08 runtime background or units are Gameplay-owned, UI must:

- implement the UI-owned HUD alignment now
- isolate the Gameplay blocker in the report
- include fresh HUD capture evidence and target comparison

## Acceptance

No UI report may claim completion because tests pass, live TMP fields are bound, or the result is only compositionally aligned.

Completion requires fresh screenshots that visually match the approved target mockup region by region, direct comparison images, and a remaining mismatch table with exact owners.
