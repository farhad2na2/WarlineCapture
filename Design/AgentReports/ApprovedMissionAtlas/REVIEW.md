# Native Campaign and Skirmish review

Actual Unity Game view captures of source commit `2864809`, after the approved polish. PR #70 was merged on October 21. This is the earlier native gallery; the later #71 English and Persian visual style was accepted by Farhad on November 5. [Acceptance and remaining technical gates](https://github.com/farhad2na2/WarlineCapture/issues/69).

## November 5 native menu recheck

[PR #79](https://github.com/farhad2na2/WarlineCapture/pull/79), source `3728a38af1f22d70b0ed833653096d6929b07534`, fixes stale aircraft source-dependency stamps without changing geometry. Eleven maintenance controls and the original three geometry verifiers pass. A separate repository-wrapper run passes native menu input and all 24 page/locale/ratio cases in English and Persian at 1920×1080, 1440×1080 and 2400×1080, producing 40 local captures. Selection, difficulty, real army-size availability/rejection, region navigation, briefing and Back are covered. Reviewed current screens retain the accepted later #71 style.

The run used an isolated checkout with no preserved HUD/font or other runtime-source overrides. Burst stayed enabled; synchronous Editor compilation was requested during validation setup. Runtime jobs, scheduling and numeric/safety settings are unchanged. [Compact source/evidence receipt](2026-11-05-menu-recheck.json) records exact identities; raw logs, backups, failed attempts and PNGs remain local and are not uploaded. The successful menu wrapper exited 0 and emitted `[ApprovedMissionAtlasReview] result=Passed`. Earlier failures remain failures; one produced a diagnostic failure screenshot rather than a comparison gallery. A shutdown warning reported three persistent allocations, so this is not a leak-free claim.

This closes the fresh menu engineering check associated with #69. The layout/progression fixture does not establish current gameplay victories. Full mission/result/return, Windows, player/device and physical notch-device acceptance remain separate pending gates. Historical gameplay evidence below retains its original source and limitations.

## Campaign

Choose a chapter with its comic cover, then select one of five large individual comic cards on that chapter's map. The selected preview uses the same mission's exact existing comic. Names lead; mission numbers are secondary. Green completion marks and released/locked/coming-soon states come from the existing Campaign model.

![Native chapter atlas](20261020-225027/campaign-chapters-en.png)

![Native five-mission map](20261020-225027/campaign-false-front-en.png)

[Polished chapter reference](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/campaign-polish-04/01-chapter-atlas.png) · [Polished mission reference](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/campaign-polish-04/02-five-comic-missions.png).

## Skirmish

Choose a map, then browse four scrolling objective regions, each with six large thumbnail cards. The fixed objective rail jumps to those regions; the current heading stays pinned, and the next-region command makes the continuation visible. Five maps × four regions × six catalog slots provides 120 capacity. Release and ownership gates remain authoritative; planned slots stay unavailable.

![Native map selection](20261020-225027/skirmish-maps-en.png)

![Native six-mission scrolling map](20261020-225027/skirmish-s002-en.png)

[Polished map reference](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/skirmish-polish-03/01-map-selection.png) · [Polished mission reference](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/skirmish-polish-03/02-missions-scrolling-regions.png).

Mission names, catalog IDs and army/start profiles have separate hierarchy. Four medal slots represent the actual four difficulty tiers; earned medals and green completion marks remain distinct from cyan selection. The six released mission previews were recaptured from the existing Campaign geometry, with distinct camera angles and an identical selected-card preview. Planned cards use a subdued map fallback. The generated reference illustrations are not passed off as playable scenery.

## Consistency and native QA

Both modes share Back, the frame, steel borders, navy backgrounds, typography, mobile spacing, blue secondary and green primary commands. There are no decorative yellow rectangular strokes. Only selected territories use a restrained cyan wash and boundary. The settings gear is centered inside its own button; native review asserts its screen-space center within 0.5 px at every ratio.

Review14 passed all four pages at 2400×1080, 1920×1080 and 1440×1080 in English and Persian:24 page/ratio/language cases and 40 captures. Actual Input System touches cover chapter/map/mission selection, difficulty changes, next-region jump, map swipe, pinned heading, Back, briefing and return. Every active label is checked for truncation; card glyph checks use each character's actual font metrics. Manual screenshot review also caught and fixed shortening the Campaign purpose before translation.

[4:3 Campaign](20261020-225027/campaign-missions-1440x1080-en.png) · [4:3 Skirmish](20261020-225027/skirmish-missions-1440x1080-en.png) · [Persian Campaign](20261020-225027/campaign-false-front-fa-IR.png) · [Persian Skirmish](20261020-225027/skirmish-s002-fa-IR.png) · [40-capture checksum manifest](20261020-225027/sha256.json).

The gallery uses an isolated layout/progression fixture. Its example wins are not gameplay evidence. Native counts intentionally differ from illustrative mockup counts. Campaign retains its actual objectives and briefing; Skirmish retains existing size, difficulty, access and launch commands. Gameplay maps, mission definitions, budgets and fixed bindings are unchanged.

[Fresh35-test XML](polish-evidence/focused-tests-35.xml) · [Fresh bilingual Campaign journey](polish-evidence/campaign/source.json) · [English victory](polish-evidence/campaign/1-journey-victory.png) · [Persian victory](polish-evidence/campaign/6-journey-victory.png).

The fresh English and Persian S002 journeys won through normal-speed ARIA, displayed the native result, returned to the selected mission, refreshed its earned Regular medal and reloaded it from disk. Separate Editor processes then verified each actual native card without profile seeding.

![Actual S002 earned medal after victory and return](polish-evidence/skirmish-en/returned-card.png)

[English medal proof](polish-evidence/skirmish-en/medal-proof.json) · [English cold restart](polish-evidence/skirmish-en/cold-restart.json) · [Persian medal proof](polish-evidence/skirmish-fa/medal-proof.json) · [Persian cold restart](polish-evidence/skirmish-fa/cold-restart.json) · [Persian returned card](polish-evidence/skirmish-fa/returned-card.png).

[Validation status, retained failures and separate acceptance gates](STATUS.md).
