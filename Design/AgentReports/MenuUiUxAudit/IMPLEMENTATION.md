# Approved menu implementation — 2026-10-03

Workspace: `/Volumes/T7/Projects/WarlineCapture-MenuUiUx`, branch `codex/menu-ui-ux`. The project and Library were copied into the T7 workspace. Existing unrelated work is preserved.

## Implemented

- Rebuilt Main Menu, Campaign, Skirmish, Operations and Commander native prefabs, plus the loading screen.
- Credits, Commander, ARIA, Store and Armory share the same Main Menu width and left/right edges. Settings is beside Credits.
- Shared header placement, safe-area-aware outer frame, tablet height expansion and Persian panel/arrow/action-order mirroring.
- Campaign briefing measures localized text and scrolls its objectives/rewards/goals. Canonical mission artwork is preserved; numbered map pins retain route lines.
- Skirmish has a larger map preview, scrollable rules, search/filter controls and player-facing copy. The Persian map preview stays on-screen.
- Operations has readable district backplates, neutral new-city instructions and a continuous footer.
- Commander reads saved level, XP, combat totals and selected portrait. Removed invented progress/history and added a portrait picker that persists the existing profile selection.
- Removed loading diagnostic chips and player-facing prototype wording.

## Evidence and limits

1. **Visual direction:** user-approved five ImageGen mockups, including the shared Main Menu column revision. Reviewed native phone/wide/tablet samples in English and Persian. The complete 30-image matrix is in `After/20261003-201216`. Capture success establishes completeness, not an automatic visual-quality verdict.
2. **Automated checks:** all native builders passed; checked macOS wrapper exit 0; 30/30 requested screenshots. Source diff checks passed. Full logs and exit receipts accompany the captures.
3. **Normal menu input:** checked wrapper exit 0; actual InputSystem touch events drove buttons in en/fa-IR through Campaign, Skirmish, Operations and Commander and back. Briefing scroll, on-screen Skirmish preview, aligned Main Menu columns, saved nonzero Commander facts and portrait selection/return passed. Credits and mission totals stayed unchanged. Twelve journey images are in `After/input-20261003-201452`. This is automated touch input, not a human/device acceptance session.
4. **Pending acceptance:** a real phone/Device Simulator safe-area and touch-size review, human/player acceptance, and a complete normal-input mission including ARIA, result and return. The menu journey does not deploy a mission and does not claim those gates.

`findings.md` remains an informational geometry audit. Clipped scroll content and its estimated dp counts must be checked against native images and physical-device sizing; no zero-overflow or universal 44dp target pass is claimed.

Failed/intermediate candidates remain under `After/`, with their logs and review notes. Early runs failed with an Editor crash, domain reloads, a startup observer timeout, hidden-target selection and post-navigation test logging. The final checked runs above supersede those candidates.

## Native previews

### main-menu

![main-menu](/Volumes/T7/Projects/WarlineCapture-MenuUiUx/Design/AgentReports/MenuUiUxAudit/After/20261003-201216/main-menu-en-2400x1080.png)

### campaign

![campaign](/Volumes/T7/Projects/WarlineCapture-MenuUiUx/Design/AgentReports/MenuUiUxAudit/After/20261003-201216/campaign-en-2400x1080.png)

### skirmish

![skirmish](/Volumes/T7/Projects/WarlineCapture-MenuUiUx/Design/AgentReports/MenuUiUxAudit/After/20261003-201216/skirmish-en-2400x1080.png)

### operations

![operations](/Volumes/T7/Projects/WarlineCapture-MenuUiUx/Design/AgentReports/MenuUiUxAudit/After/20261003-201216/operations-en-2400x1080.png)

### commander

![commander](/Volumes/T7/Projects/WarlineCapture-MenuUiUx/Design/AgentReports/MenuUiUxAudit/After/20261003-201216/commander-en-2400x1080.png)

