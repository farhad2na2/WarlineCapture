# Campaign inner screens and remaining reachable UI — 2026-10-03

Workspace: `/Volumes/T7/Projects/WarlineCapture-MenuUiUx`; branch `codex/campaign-inner-ui`; starts at the merged approved-menu commit `a83e29731`. Existing unrelated changes remain uncommitted. Inbox is explicitly excluded by the user. Events and Ranking have no identified normal player entry in the current menu and are excluded from this pass. Command Feed is reachable from the Operations footer.

## Native audit completed

The checked macOS wrapper returned exit 0 and `[InnerScreenUiAudit] result=Passed`. All 21 requested screenshots exist: Campaign reference plus Mission Briefing, Squad Preparation, District Detail, Store, Armory and Command Feed, each at English 1920x1080, Persian 2400x1080 and Persian 2048x1536. These are route-jump audit captures, not normal-input or real-device acceptance.

Evidence: [Before/20261003-204421](Before/20261003-204421). The geometry estimates in findings.md are informational; clipping, wrapping and scroll content need native visual review. Capture completion does not establish visual quality.

## Concrete fixes identified

| Screen | Observed defect | Intended correction |
| --- | --- | --- |
| Mission Briefing | Wrong static Chapter I label for False Front, abbreviated mission number, clipped objectives, tiny conditions/intel/rewards, generic star goals | Approved shared Back/title/settings/credits header; actual selected chapter and mission; readable wrapping; scrolling intel sheet; mission-specific goals and rewards; aligned preparation/deploy footer |
| Squad Preparation | Static unrelated Forward HQ objectives, example unit levels/power/gear bonuses and deployment cost, no normal Back control, extensive Persian overflow | Selected-mission force and purpose; no invented progression or cost; existing art; visible Back; readable mission objectives/conditions/goals; functional Armory browse and deploy |
| District Detail | Tiny labels, Persian overflow, tablet letterboxing, static example metrics/confidence/activity times | Shared header and safe-area frame; readable scrollable detail; bind actual district/action state or use accurate empty state |
| Store | Older header, clipped bundle descriptions, Persian overflow, prominent purchase control while unavailable, technical receipt-service guidance | Shared aligned header; readable cards/details; scroll long inclusions; localized player-facing availability and accurately disabled purchase state; preserve existing purchase behavior |
| Armory | Header has blank gaps and inconsistent credits icon, tiny card facts, long Persian names overflow, edge-touching controls | Shared aligned header/coin; room for localized names and facts; adaptive catalog grid and scrolling inspection; preserve real catalog selection and existing navigation |
| Command Feed | Older header, tiny rows and controls, substantial Persian overflow | Shared header; readable wrapped/scrollable rows, aligned filters and navigation; retain existing reachable actions |
| Story Archive / comic playback / mission field guide / HUD / Pause / result / Settings | Further inspection needed; not covered by this route-only matrix | Capture reachable surfaces and apply the same spacing, readable copy and safe-area treatment where defects are confirmed; retain mission purpose, objectives and visible ARIA Play/Stop; camera guidance must not issue troop orders |

## Visual direction review

Two ImageGen mockups use the actual current Mission Briefing/Squad Preparation captures and the approved native Campaign reference. They are layout directions, not shipped raster UI. Mission Briefing v02 corrects the first generated draft's inherited generic star goals. Native implementation must use the selected mission's full localized copy, actual rewards and existing game portraits; generated ARIA portrait variation is not a new asset request.

- [Mission Briefing v02](Mockups/mission-briefing-v02.png)
- [Squad Preparation v01](Mockups/squad-preparation-v01.png)

The user approved both substantially redesigned mission screens before implementation, satisfying the AGENTS.md visual-direction gate. The prior five-menu approval remains valid for shared header, typography, spacing and alignment reuse.

## Remaining gates

- Native implementation and after captures: complete; see IMPLEMENTATION.md for visual issues found and corrected.
- Focused builders and captures passed. Normal-input navigation passed an earlier run; final expanded run evidence is recorded in IMPLEMENTATION.md.
- Complete normal-input mission including ARIA, result and return: failed on operation-map rendering/state errors; pending, with failed evidence retained.
- Physical phone/player acceptance: pending.

## Visual direction approval

User approved Mission Briefing v02 and Squad Preparation v01 on 2026-10-03. Native implementation is complete. Navigation and mission evidence are reported separately in IMPLEMENTATION.md. Inbox is excluded.
