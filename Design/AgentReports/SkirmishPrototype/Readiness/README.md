# Skirmish readiness validation — 2026-09-18

The Base Assault prototype is ready for internal Editor play in English and Farsi. See the [completion decision and case-by-case coverage](../../../Roadmap/Skirmish_Prototype/COMPLETION.md). This report preserves development failures as well as final evidence; older checkpoints are not the current result. Android, application lifecycle on device, unfamiliar-player comprehension and the original pacing target remain unaccepted.

## Completed checks

- 309 checks passed after regeneration: [303 focused/shared regression cases](skirmish-final-regressions.txt), including parameterized audio lifecycle and guidance cases, plus [six setup/binding/navigation cases](skirmish-regenerated-setup-tests.txt).
- Ten consecutive actual UI sessions across seeds 37, 7919 and 104729: replay, adjusted setup, pause/frozen clock, canceled restart, surrender, and final Main Menu. All fresh rosters had 17 owned entities per side before recruitment; all settled startup entity counts were 46,008. No skirmish session, squad members or pause ownership remained in the menu. One automation click hit a still-transitioning popup; the same control succeeded after settling. [Unedited trace](lifecycle-ten-sessions.txt).
- Managed memory varied with garbage collection (approximately 2.42–2.78 GB in this Editor); this is not a phone-memory acceptance result. Entity counts did not accumulate.
- The isolated campaign profile remained byte-for-byte unchanged across the measured replay/return cycles (SHA256 `467a0e61eeb0fd5f936b15e0d3113e6f657e81892792dbdffed64d59c3c5a686`).
- All ten authored starting buildings pass exact-origin, road, sidewalk and canal checks. [Layout audit](dry-layout-audit.txt).
- Setup rendered and inspected in EN/FA at 1280×720 and 2400×1080. Rules and Start Match now use explicit configured localization keys; the prior empty bindings left English text visible in Farsi.
- Pause uses live skirmish time, base health, population and surrender/restart actions. Farsi current/maximum numbers are separate from RTL labels; help describes squad cards and Commands rather than old rectangle/portrait gestures.

## Defects fixed during validation

1. Lifetime infantry reservations blocked later Barracks production. Completed infantry now release skirmish production slots while physical spawn occupancy still applies.
2. Paid reinforcements now rally once near their own base without replacing later player Move/Hold/Attack orders.
3. AI attack groups now respond to defenders near any member, replenish destroyed groups, and use traversable long routes.
4. Late streamed scenery and default spawn configurations no longer add unexpected owned vehicles or overwrite skirmish Fuel.
5. Supply/base footprints crossed the authored canal. Enemy anchors and all supply sites were revalidated; the placement mask reserves actual transformed water geometry.
6. Seed 37 exposed starting trucks occupying pending building sites. Skirmish now completes exact building placement before spawning its units. The subsequent ten-session run starts successfully across all three seeds.
7. Retry inherited the previous startup boundary failure. A new attempt clears completed status/results while preserving the request sequence.
8. A cached setup document could overwrite a newer saved match result. Applying setup now reloads the document before changing configuration; a dedicated regression reproduces this ownership boundary.
9. Base focus now frames the Barracks and nearby defense. The opening still uses zoom 40.
10. Setup localization, Farsi numeric order, pause help and live selected-unit locale changes were corrected.
11. Attackers include armed Watchtowers when choosing nearby threats, instead of ignoring them while advancing toward the base. Unarmed supply structures remain excluded from that threat scan.
12. Canceling paid skirmish production returns the cost of its undelivered units exactly once. Receipts are cleared when pooled; partial deliveries receive a proportional refund. Campaign and unpaid scripted production behavior are preserved.

13. Skirmish Oil allocation now reserves 80 Materials for continued recruitment and uses a practical 160-Fuel target instead of the industrial tank’s 5,000-barrel capacity. Empty tank space no longer starves fabrication. Fuel below 10% of that target still takes emergency priority. Campaign allocation remains unchanged; both priorities have regression coverage.

14. Paid infantry could spawn in disconnected pockets near the Barracks and then fail both rally and Attack. Skirmish now checks connection to the approach before instantiation and selects an accessible spawn. It does not move already-created units to hide a bad spawn.
15. Both factions’ skirmish Watchtowers now use range 55 and 10 damage every 0.8 seconds (previous inherited values: range 100, 10 every 0.3 seconds). Health and campaign stats are unchanged. Final normal-speed validation follows this balance change.

16. Recruitment connectivity now includes the actual MapSurface movement mask, not just the compatibility grid. Surface-only rock barriers and enclosed pockets have parameterized regression coverage. The known bad spawn cell (814,575) is rejected on the live map; the Burst check averages 1.17 ms over ten runs on the 2048×1024 grid (previous compatibility-only managed check: 7.89 ms).
17. The skirmish base/clock strip hides while the full tactical map is open, preventing it from covering the map title and content. Live visual verification follows below.

18. Full tactical map now shows the actual skirmish base health and clock instead of campaign placeholder information. Center on HQ focuses the designated player Main Base. [Farsi map](fa-full-map-live-status.png).
19. Quick-select cards distribute a full 24-soldier army as four stable groups of six, retaining existing members when recruits arrive.
20. Skirmish combat approach positions respect actual terrain and vehicle footprints, including surface-only rock barriers.
21. The bilingual ARIA briefing is shorter and remains readable without clipping. [Farsi briefing](fa-briefing-fit.png).
22. A passing vehicle could replace an infantry Move order with a short displacement, leaving soldiers halfway to their destination. Active manual moves now retain their path; idle infantry can still yield. Ordinary and segmented-path regressions pass. Both subsequent EN/FA runs delivered all 25 combat units to both rally points without reissued orders.

## Normal-speed outcomes observed

All battle outcomes below used ordinary paid construction/recruitment and UI/world commands; no health, position, resource or outcome injection.

| Route | Locale | Seed | Outcome | Duration | Notes |
|---|---|---:|---|---:|---|
| Direct pressure | EN | 104729 | Defeat | 2:56 | Earlier layout/balance; retained as development evidence |
| Direct pressure | FA | 104729 | Defeat | 3:05 | Earlier layout; retained as development evidence |
| Defensive buildup/counterattack | EN | 104729 | Victory | 5:37 | Two paid Watchtowers and four rifle batches; before dry-ground enemy relocation |
| Defensive buildup/counterattack | FA | 104729 | Draw | 15:00 | Enemy base at 30 HP; follow-up reinforcements were late; earlier layout |
| Unattended defense | EN | 37 | Defeat | 2:51 | Corrected building-before-units startup; actual enemy Main Base attack |
| Defensive buildup/counterattack | FA | 104729 | Victory | 5:26 | Final dry-ground layout; 18/32 unit losses; before tower-targeting follow-up |
| Delayed defense/counterattack | EN | 104729 | Surrender | 5:50 | Towers built late, attack failed with 20/32 losses; enemy base 1026 HP; not a destruction defeat |
| Unattended during recovery/production QA | FA | 104729 | Defeat | 2:51 | After tower-targeting fix; enemy destroyed the actual Main Base while Build was open |

A subsequent EN tower-focused counterattack reached a normal-speed 15:00 draw with both bases intact and 35/32 unit losses. Its supply trace exposed the enemy fabrication starvation fixed above; it is development evidence, not final pacing acceptance.

The interrupted Farsi run after a script reload is excluded. Final dry-ground bilingual journeys are recorded below. Earlier runs do not prove the final layout is balanced.

## Live recovery follow-up

- Injected a failed startup boundary (mechanics test, not a played battle), captured the Farsi error, clicked Retry, and verified a different session reached Playing with no startup failure. [Trace](skirmish-live-recovery.txt).
- Confirmed Restart through Pause: new session, same seed 104729, clock under two seconds, nine initial combat units, zero losses, time scale 1. [Trace](skirmish-live-restart.txt).
- Paid recruitment closed Build. Reopening and canceling returned Materials from 260 → 240 → 260. A subsequent automation step mistakenly used the placement-only Cancel control to close the drawer; this is retained in the unedited trace. [Trace](skirmish-live-cancel.txt).
- The enemy destroyed the Main Base with Build open. The result overlay blocked underlying controls, and Replay reached a new playable match. [Capture](fa-defeat-while-build-open.png).
- Applying setup after the Farsi victory preserved its result/session instead of writing an older cached result. Only match music was playing at result navigation; no gameplay voice remained.

## Acceptance scope

The final tactical journeys below cover direct pressure, defense and a coordinated flank. Shared M1–M5 regressions passed; the ten campaign entry checks are not a full fresh campaign playthrough. The original 8–12-minute pacing target is not demonstrated: successful coordinated attacks took about six minutes, unsupported rushes lost before three, and a late failed counterattack drew at fifteen. Keep that actual range visible in balance decisions.

Latest EN defensive test with sustained AI supply and revised towers reached a normal-speed 15:00 draw (bases 1200/1080; unit losses 43/128). Both structure-focused rushes failed. Its two remaining stranded recruits led to the surface-aware spawn fix above; this is not final gameplay acceptance. [Trace](en-balanced-draw-trace.txt), [supply ledger](en-balanced-supply-ledger.txt).

## Campaign return check

All ten M1–M5 entries (English and Farsi) passed after leaving skirmish, with no SkirmishMatchState or SkirmishSquadMember leakage into the campaign. M1 replay played all 17 first-launch story voice clips in each language without another language/guide choice, then showed actionable guidance. M2–M5 entry stories were intentionally skipped through the narrative UI; their first instructions and available guidance were checked. This is an entry/first-action regression, not ten complete mission playthroughs. [Trace](campaign-return-en-fa.txt), [observed Farsi narrative clips](campaign-return-fa-audio.txt).

The FA road-flank diagnostic ended by surrender at 9:28 after its attacking army was lost (25/88 unit losses; bases 1200/1176). Its first movement leg left 11 soldiers behind and exposed the vehicle-displacement defect above. Later, Move orders entered enemy range before Attack, incurring heavy losses. This is diagnostic evidence, not a final accepted journey. [Trace](fa-flank-before-yield-fix.txt), [actions](fa-flank-before-yield-actions.txt).

## Movement and terminal-state follow-up

- EN seed 104729: victory at 6:02.973, 16/51 unit losses, player base 1200 HP. All 25 combat units reached both flank rally points without reissued orders. The paid army destroyed the tower, then the enemy Main Base. This run exposed continued combat behind the result screen. [Actions](en-movement-fixed-actions.txt), [trace](en-movement-fixed-trace.txt).
- Terminal matches now own a time-scale freeze until teardown, and the attack system rejects further skirmish attacks after the authoritative result. The pause system releases its ownership on replay/menu without reactivating the old session.
- FA seed 7919: victory at 5:59.043, 12/51 losses, player base 1200 HP. Both rally checks were 25/25. [Actions](fa-seed7919-actions.txt), [trace](fa-seed7919-victory-trace.txt), [result](fa-seed7919-frozen-victory.png).
- Thirteen surviving combat units kept exactly the same health and positions for twelve seconds after the result; Materials, fabrication/spending totals and clock were unchanged. Only match music was playing. [Freeze check](terminal-freeze-check.txt). Actual Replay then reached a new session at normal speed with zero losses. [Replay check](terminal-replay-check.txt).
- The help audit found that the visible Field guide button had no skirmish route. The localized four-page skirmish guide now opens through the actual Field guide button, pauses the match, pages correctly and resumes on close in both languages. Farsi pages were visually inspected for clipping. [EN interaction](guide-en-check.txt), [FA interaction](guide-fa-check.txt), [Farsi objective](guide-fa-page1.png), [Farsi supply](guide-fa-page4.png).

## Current direct-pressure checks

EN seed 7919: real defeat at 2:55.632 after sending the starting squads/car against the defended enemy base without reinforcement. The result explicitly says the enemy destroyed your Main Base; it freezes the completed world. [Trace](en-direct-pressure-trace.txt), [result](en-direct-pressure-result.png).

FA seed 37: real defeat at 2:45.519 with 9/0 unit losses and enemy base intact. Actual Adjust Setup returned to the correct setup, then launched the next EN match. [Trace](fa-direct-pressure-trace.txt), [result](fa-direct-pressure-result.png).

## Final-layout defensive route

EN seed 104729: normal-speed 15:00 draw, both bases 1200 HP, unit losses 25/128. Two paid Watchtowers and four rifle batches held repeated enemy pressure. A late frontal counterattack lost its army; the defense alone did not grant victory. Time spent inspecting supply/selection is included, so this is a timeout/recovery check, not an unbiased match-duration sample. [Trace](skirmish-en-defense-final-trace.txt), [actions](skirmish-en-defense-final-actions.txt), [draw](skirmish-en-defense-final-result.png).

The Field Fabrication Depot was demolished through its visible Commands wedge. It disappeared from live supply ownership; a replacement was paid for (100 Materials) and placed on valid nearby ground. The original footprint remained blocked. A click in the radial button's rectangular center missed its shaped hit area; a click on the visible wedge label worked. The late replacement was still awaiting Oil at timeout, so supply recovery is being repeated earlier in the next match and is not marked passed here. [Unedited recovery trace](skirmish-supply-recovery.txt), [actual economy ledger](skirmish-en-defense-ledger.txt).

## Supply recovery accepted

FA seed 37, normal speed: demolished the Field Fabrication Depot at approximately 3:10 through Commands, paid 100 Materials to place its replacement on nearby valid ground, and let the real logistics vehicles deliver Oil. No resource/health/position injection was used. The new depot received 8 Oil and completed a 4-Oil → 20-Materials cycle: Materials 20 → 40; lifetime fabrication 80 → 100; lifetime spending 280. [Trace](skirmish-fa-supply-recovery.txt), [capture](skirmish-fa-supply-recovered.png). The logistics suite separately tests blocked routes, destroyed destinations, reservation cleanup and typed stall reasons.

EN→FA live switching and all four English guide pages also passed during this match; guide open paused the clock and close restored normal speed. [English objective page](guide-en-page1.png). The game returned to Farsi before the counterattack.

## Six completed tactical journeys

| Approach | Locale | Seed | Result | Duration |
|---|---|---:|---|---:|
| Unsupported early pressure | EN | 7919 | Enemy destroyed player Main Base | 2:55.632 |
| Unsupported early pressure | FA | 37 | Enemy destroyed player Main Base | 2:45.519 |
| Defensive buildup / late direct counterattack | EN | 104729 | 15-minute draw | 15:00 |
| Defensive buildup / direct counterattack / supply rebuild | FA | 37 | Player surrendered after army loss | 8:30.423 |
| Gathered infantry/car flank, tower then base | EN | 104729 | Victory | 6:02.973 |
| Gathered infantry/car flank, tower then base | FA | 7919 | Victory | 5:59.043 |

All used the final dry-ground layout and current combat/movement tuning. The EN flank preceded the terminal-freeze fix; its combat outcome remains valid and the subsequent FA victory separately verifies the frozen result. No injected health, economy, objectives or positions manufactured these outcomes. The defensive runs include QA inspection/repair time; do not treat the sample median (about 6:01) as a typical-player pacing measurement. Observed range is 2:45–15:00; the 8–12-minute target remains a tuning hypothesis for unfamiliar-player testing.

FA defense lost 25 combat units after its frontal counterattack and surrendered through Pause → Surrender → Confirm at 8:30.423, with both bases still intact. The result correctly says the player withdrew, not that the base was destroyed. One QA lookup first chose an unavailable duplicate Pause control; selecting the active interactable header control worked. [Actions](skirmish-fa-defense-final-actions.txt), [trace](skirmish-fa-defense-final-trace.txt), [exit](skirmish-fa-defense-exit.txt), [result](skirmish-fa-defense-final-result.png).

## Final regeneration, persistence and restoration

The config, setup-prefab and localization builders completed in Edit mode. All eight gameplay configuration assets were byte-identical afterward; the setup prefab regenerated and its six setup/binding/navigation checks passed. The 303 focused/shared regressions also passed after regeneration. [Builder result](skirmish-regeneration.txt), [asset comparison](skirmish-regeneration-hashes.txt), [setup checks](skirmish-regenerated-setup-tests.txt).

A fresh replay was interrupted at 1.996 seconds. The persisted setup/prior result remained unchanged, and reopening setup retained seed 37 with no active match or fabricated result/resume. This is an Editor Play-session reload, not an OS process-termination test. [Trace](skirmish-interrupted-session.txt). The isolated post-campaign-entry profile remained byte-for-byte unchanged across the subsequent skirmish runs. [Profile evidence](final-profile-preservation.txt).

The regenerated Farsi setup was visually inspected, then its actual Deploy button launched a new match. All ten buildings again passed exact-origin, road, sidewalk and water checks. [Setup](skirmish-regenerated-setup-fa.png), [opening](skirmish-regenerated-opening-fa.png), [layout](skirmish-regenerated-live-layout.txt).

The normal Editor was restored to clean Match/Edit mode with no QA save override. [Restoration](skirmish-editor-restored.txt). The [input manifest](tested-inputs.sha256) records the tested working-tree source/assets based on `b0b4dc72f2b41a36c943955b6a8709aee86227e0`; the changes have not been committed by this completion step.
