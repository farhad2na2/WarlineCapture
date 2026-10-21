# Mission atlas color and card polish — review candidate

The original native menu had oversized gold objective icons, a dark scrollbar, weak rectangular territory edges and crowded card copy. This revision uses compact cyan/silver objective symbols, a blue thumb contained by its track, stronger irregular districts, colorful native medals and larger padded Skirmish artwork/title layouts. Campaign retains its exact chapter and mission comics. The shared Back, header, settings and command styling stays consistent.

Aircraft illustrations reuse the existing Jet 01 and Jet 02 action portraits. No A-10 draft was imported. New ground illustrations are menu artwork only; physical maps, scenario sources, availability, army budgets and mission rules are unchanged.

## Visual comparison

[Approved Skirmish mockup](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/skirmish-polish-03/02-missions-scrolling-regions.png) · [previous native screen](../ApprovedMissionAtlas/20261020-225027/skirmish-s002-en.png)

![Current Skirmish native screen after an earned victory](screens/skirmish-earned-en.png)

![Current Campaign native screen](../ApprovedMissionAtlas/20261021-092003/campaign-false-front-en.png)

[Complete native matrix: 40 captures](../ApprovedMissionAtlas/20261021-092003/) · [Capture SHA-256 manifest](../ApprovedMissionAtlas/20261021-092003/SHA256.json)

## Acceptance gates

| Gate | Result |
|---|---|
| Visual self-review against approved direction | Completed; owner review pending |
| Native layout and normal touch navigation | Passed: 24 page/locale/ratio cases; EN + fa-IR; 2400×1080, 1920×1080, 1440×1080 |
| Real glyph spacing, card padding, clipping, scrollbar bounds, centered settings icon | Passed |
| Focused automated tests | Passed 35/35 on final frozen source |
| Complete normal-input M01 + S002/ARIA/result/return | Passed EN + fa-IR, fresh victories and earned-card medals |
| Independent-process medal reload | Passed EN + fa-IR, distinct producer/restart PIDs, profileSeeded=0 |
| Clean checkout, Windows and real player/device acceptance | Pending; not certified by these Editor runs |

Native captures use isolated validation profiles. The layout matrix uses progression fixtures to expose Campaign chapters; its mission wins are not gameplay acceptance. Complete M01 and S002 gameplay journeys are recorded separately in VALIDATION.json; these checks do not certify other missions or all 120 Skirmishes.

The tested worktree contains pre-existing local changes in the match HUD prefab and Noto Arabic narrative font asset. Those files are preserved and excluded from this change. SOURCE_MANIFEST.json identifies the exact tested bytes; the worktree proof does not certify a clean checkout.

Full visual logs, including failed iterations, are preserved in visual-review-logs.zip. VISUAL_RUNS.json records their hashes and verdicts. Raw failed screenshot directories remain preserved locally. Earlier successful revisions are historical evidence, not acceptance for the final candidate.

This follow-up remains unmerged for owner review.

Frozen worktree source hash: `dd1cfb5c276aa6b0505c2d8e5532bd536580b4afbc87dc9c87b9a8c43ffd70b3`. [Full validation receipts](VALIDATION.json) · [Original normal-input Campaign evidence](campaign-journey-evidence.zip) · [English S002 evidence](s002-en-evidence.zip) · [Persian S002 evidence](s002-fa-IR-evidence.zip) · [Final full logs and 35-test XML](final-validation-logs.zip).
