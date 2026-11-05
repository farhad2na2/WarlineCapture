# Map cards, Campaign comics and army-size choices

PR #71 was merged on October 21. On November 5, Farhad explicitly accepted the four displayed English native screens and the four displayed Persian native screens from this later UI version, after comparison with the earlier #70 gallery. This records acceptance of the shown visual style; it does not relabel these historical captures as a fresh validation of current main. [Acceptance record](https://github.com/farhad2na2/WarlineCapture/issues/69).

## November 5 native menu recheck

[PR #79](https://github.com/farhad2na2/WarlineCapture/pull/79), source `3728a38af1f22d70b0ed833653096d6929b07534`, fixes stale aircraft source-dependency stamps without changing geometry. Eleven maintenance controls and the original three geometry verifiers pass. A separate repository-wrapper run passes native menu input and all 24 page/locale/ratio cases in English and Persian at 1920×1080, 1440×1080 and 2400×1080, producing 40 local captures. Selection, difficulty, real army-size availability/rejection, region navigation, briefing and Back are covered. Reviewed current screens retain the accepted later #71 style.

The run used an isolated checkout with no preserved HUD/font or other runtime-source overrides. Burst stayed enabled; synchronous Editor compilation was requested during validation setup. Runtime jobs, scheduling and numeric/safety settings are unchanged. [Compact source/evidence receipt](../../ApprovedMissionAtlas/2026-11-05-menu-recheck.json) records exact identities; raw logs, backups, failed attempts and PNGs remain local and are not uploaded. The successful menu wrapper exited 0 and emitted `[ApprovedMissionAtlasReview] result=Passed`. Earlier failures remain failures; one produced a diagnostic failure screenshot rather than a comparison gallery. A shutdown warning reported three persistent allocations, so this is not a leak-free claim.

This closes the fresh menu engineering check associated with #69. The layout/progression fixture does not establish current gameplay victories. Full mission/result/return, Windows, player/device and physical notch-device acceptance remain separate pending gates. Historical gameplay evidence below retains its original source and limitations.

- Map cards now retain thumbnails. Skirmish crops the matching district from the existing atlas painting; Campaign uses its existing exact chapter comics.
- Map and chapter headings share their accent colors between the map and left rail. Progress and availability have distinct colors, with a cyan outline for selection.
- Campaign mission comics occupy 40% of each card width with a taller crop and padded number/title/status areas.
- Standard, War and Large War remain mutually exclusive army-size choices. Their real bound buttons have bordered navy faces, a blue/cyan selected state and one check. Unpublished sizes retain their lock and disabled state.
- Physical maps, publication gates, scenario bindings, army budgets and gameplay rules are unchanged.

## Actual game screens

![Campaign missions with larger comics and colored chapter rail](screens/campaign-false-front-en.png)

![Skirmish map cards and colored rail](screens/skirmish-maps-en.png)

![Army-size choices](screens/skirmish-s002-en.png)

[Narrow Campaign](screens/campaign-missions-1440x1080-en.png), [narrow Persian Campaign](screens/campaign-missions-1440x1080-fa-IR.png), [narrow Persian Skirmish](screens/skirmish-missions-1440x1080-fa-IR.png).

## Separate validation and acceptance gates

Frozen worktree source SHA-256: `51846da24a6269d7e309becf85737adbf1749961594824a7c3d7a6da17606ef8`. Independent start/end source censuses match.

- Native visuals: 40 screenshots and 24 page/locale/ratio cases pass in EN and fa-IR at 2400, 1920 and 1440 x 1080. Touch navigation, map choices, scroll, back/return, difficulty and locked army-size rejection pass. Artwork presence, map/rail color equality, glyph padding, truncation, scrollbar bounds and gear centering pass.
- Automated checks: 35 focused tests pass.
- Normal-input Campaign: fresh M01 touch victories and returns in both locales, first-launch fixture only, outcomesInjected=0.
- Normal-input Skirmish: fresh Standard/Regular S002 normal-speed ARIA victories, native results, returned mission-card medals and independent-process reloads in both locales. War and Large War remain unpublished and disabled. These journeys do not certify those sizes or all mission IDs.
- Owner visual style review: accepted by Farhad on November 5 for the shown English and Persian native screens. Fresh-current-build engineering, clean-checkout, Windows and real player/device acceptance remain separate gates.

The exact worktree includes preserved pre-existing SCN08 HUD and NotoSansArabic font edits excluded from this PR. Its fingerprints do not certify a clean checkout. Layout screenshots use progress fixtures; the separate fresh normal-input journey archives supply result evidence.

## Interrupted evidence

A host reboot at 12:37 on 2026-10-21 Europe/Berlin interrupted the pre-reboot S002 run and removed its temporary logs. Interrupted status/source census and native captures were retained. Pre-reboot visual attempts caught an assumption that unpublished sizes were enabled, incomplete test-touch dispatch and Persian label wrapping; one attempt also timed out returning through Campaign briefing. Those missing raw logs are not represented as final evidence.

The post-reboot Editor waited at a Recovering Scene Backups dialog. The exact recovery file was preserved outside Assets before declining the additional Assets/_Recovery copy. The recovery receipt and startup process sample are retained; the backup itself remains local, outside the PR. Final validation uses persistent project logs. Failed or timed-out post-reboot attempts, if any, remain in the full log archive.

Final native and journey logs, test XML, all 40 visual captures and hashes, exact source manifest and native journey evidence accompany this report.
