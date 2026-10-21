# Map cards, Campaign comics and army-size choices

Native follow-up to the approved Campaign/Skirmish atlas. Current status: UI implemented; final evidence is being rerun after a host reboot at 12:37 on 2026-10-21 Europe/Berlin. The reboot interrupted S002 and removed temporary logs. The 35 tests and Campaign journeys had passed before the reboot, but those stages are being rerun to restore complete logs. Current logs are stored in this report folder. No final-source S002 win is claimed yet.

- Skirmish map cards retain artwork instead of suppressing thumbnails. Their crops come from the matching districts in the existing atlas illustration.
- Territory/chapter headings share cyan, amber, blue, green and coral accents between the map and the left rail. The selected choice keeps its cyan outline; progress and availability have distinct colors.
- Campaign mission comics now occupy 40% of the card width with a taller crop. Mission numbers, titles and status retain their own padded areas.
- Standard, War and Large War remain mutually exclusive army-size choices. The real bound buttons use bordered navy faces, blue/cyan selection and one selected check. Unavailable sizes show the existing lock icon and remain disabled.
- No physical maps, release gates, army budgets, mission bindings or gameplay rules change. Campaign keeps the exact existing comic assets.

## Acceptance gates

Visual direction already approved in this task. Final implemented-screen owner review remains pending. Native screen review, focused tests, full normal-input journeys and device acceptance are reported separately below after completion. Earlier evidence in the parent report belongs to the previous source candidate.

The worktree includes preserved pre-existing SCN08 HUD and NotoSansArabic font edits excluded from this PR. Source fingerprints explicitly include those worktree bytes; they do not certify a clean checkout. Clean-checkout, Windows and real player/device acceptance remain pending.

## Actual-game visual preview

These native captures were taken before the reboot, after the requested visual changes. The subsequent source change only adds a review-only Editor entry point. A new complete native review and gameplay run are in progress; the captures below are visual evidence, not final gameplay acceptance.

![Campaign missions and colored chapter rail](screens/campaign-missions-en.png)

![Skirmish map cards and colored map rail](screens/skirmish-maps-en.png)

![Army-size choices](screens/skirmish-s002-en.png)
