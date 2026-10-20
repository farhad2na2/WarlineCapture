# Approved Campaign and Skirmish map menus

Owner approved both map-based directions on 2026-10-20. The native implementation is committed as `c160864`; PR70 remains draft and unmerged. Owner review of the implemented screens is pending.

## Native screens

[Latest native gallery](REVIEW.md) · [40 original Game view captures and checksum manifest](20261020-211119/).

- Campaign: chapter atlas with comic covers, then five large individual comic mission cards on the selected chapter map. Selected preview uses the corresponding comic. Main-page comic coverage is repaired from 14 to 25 without replacing existing bindings.
- Skirmish: choose a map, then scroll four objective regions with six large thumbnail choices each. Objective tabs jump to those same regions. Five maps ×four regions ×six catalog slots provides 120 capacity; access and published status remain authoritative. Coming-soon entries are not made playable.
- Shared Back, navy/steel panels, typography, mobile spacing, cyan selection, blue secondary and green primary commands. Native mission-size controls survive repeated prefab authoring. Campaign briefing return preserves the mission page.
- Current Campaign physical maps, mission definitions, budgets and scene bindings remain unchanged. New aerial city artwork is a menu illustration.

## Validation

| Gate | Result and scope |
| --- | --- |
| Mockup direction | Owner approved Campaign A and Skirmish scrolling mission regions. |
| Native owner visual review | Pending; review the gallery before merging. |
| Native UI and input | Review16 passed all four pages at 2400×1080,1920×1080 and1440×1080 in EN/fa-IR. Real touch covers chapter/map/mission selection, objective jumps, map swiping, Back, Campaign briefing and return. Layout progression fixtures are labelled and do not count as gameplay. |
| Focused automated checks | 34 passed, 0 failed, 0 skipped. Library selection/access/launch/result models, medal ledger and main-menu navigation. [Exact XML](evidence/focused-tests-34.xml). |
| Campaign normal input | M01 run04 passed English and Persian real victories, settlement and menu return. Menu, briefing, selection drag, controls, world destinations and result action use Input System touch. Outcomes injected:0. Other missions are not certified by this journey. The isolated validation store also ensures M01–M05 availability; it seeds no wins or outcomes. |
| Final Skirmish normal input | EN and FA run02 passed actual normal-speed ARIA victory, native result, selected S002 return, earned card medal and disk reload. Full-source start/finish fingerprint: `43e104e05cc1437e04b31ef27afa9cddf39ea8a4f248900077c4905c9e0602eb`. Earlier831a checkpoint wins are historical only. |
| Final cold restart | EN restart02 passed in a distinct Editor process: native touch, earned card medal, profileSeeded=0. FA restart02 also passed with the same zero-seeding and native-touch checks. |
| Player/device, Windows and clean-checkout acceptance | Pending. Native worktree checks do not establish these gates. |

Native checks run in the managed worktree with two preserved local source differences outside this menu commit: the existing Match HUD prefab and Arabic narrative font asset. Their [path/hash record](evidence/preserved-local-source.json) makes this qualification explicit. Neither is silently included in the PR. Automatic approval review rejected restoring the unrelated HUD prefab because it could discard uncommitted work; it remains intact. A clean-checkout pass remains a separate pre-merge gate.

## Evidence and retained failures

[Run index and validation/error excerpts](evidence/run-index.json). Complete original logs remain at the indexed /private/tmp paths; earlier raw captures and gameplay traces remain locally preserved.

Native review01–05 and07–11 retain failures covering authoring, shell transition scaling, old capture targets, localized text fitting and glyph bounds. Review06 passed the first bilingual flow;12–16 passed progressively broader matrices. Review16 is the final layout candidate.

Campaign run01 stalled because the older driver did not perform the visible tutorial selection drag. Run02 had a harness-only missing namespace compile error and was stopped as an incomplete run; its verified Editor received SIGTERM and exited143. Run03 passed pointer/command replay in both languages. Run04 replaced remaining gestures with Input System touch and passed both real victories/returns. No outcome, unit health, position or time-scale injection establishes these results.

[Approved Campaign reference](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/campaign-A-five-missions-on-map-02/campaign-five-comic-missions-on-map.png) · [Approved Skirmish reference](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/selected-A-iteration-02-two-step/04-missions-scrolling-regions.png).

Earlier PR validation history remains in the [previous status report](../SkirmishBattleLibrary/2026-10-20/STATUS.md). Its wider optional run18 failed 131/28/2 before later fixture repairs; Windows filesystem/junction cases remain unverified on macOS. The focused 34 tests do not claim a rerun of the whole project suite, and model simulations do not count as native wins.
