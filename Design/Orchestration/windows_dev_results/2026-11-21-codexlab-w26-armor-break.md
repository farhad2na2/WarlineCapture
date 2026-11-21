# W26 Armor Break source fit for S061–S072

The source-fit gate is blocked for all 12 rows. Armor Break allows construction only in the existing Campaign fuel-reserve lot, XZ `[1305,485–1331,510)`. The required 28 × 15 m barracks cannot fit in the 26 × 25 m lot in either orientation. This blocks both opening bases before objective or air-pad placement. The assessment used the existing CH04-M05 geometry and opening footprints.

All logs and probe fixtures are under `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/mp-bt-ce-armorbreak`.

## Editor probes

Tooling commit `c1600712d`. Set `WARLINE_MISSION_DRAFT` and leave `WARLINE_SKIRMISH_CATALOG` unset to select a measurement host from the draft's `campaign_source`. For Armor Break the Editor scopes a copy of the expansion catalog to the existing staged S049 definition. Both probes verify the loaded map identity. The explicit catalog invocations retain their previous dispatch.

- Warm and cold diagnostics use seed 130365 in English. The cold layout selected staged S049 at `armor-layout.log:1662` and the runtime selected `opmap.ch04.armor_break` at line 2244. Staged S049's first opening failure is `MissingReference reason=noExactStructurePad` at line 2403. The dump continued and completed at line 10109, with 34,193 tested samples and 11 native passes. The cold wrapper exited 2 after `candidateSourceChangedDuringRun` at line 25529. The first import added a scene dependency `.meta` during the hash interval.
- The draft-selected area probe completed at `armor-area.log:1974` and exited 0 at line 17842. It loaded the same source and preserved the staged host's opening failure at line 1908 in the receipt. The diagnostic rectangle has 472 live vehicle-passable cells and 408 legal 3 × 3 truck origins. The route flood returned 476 m. The same receipt's existing native query reported 1 m for that 476 m leg, so it supplies no native convoy travel-time evidence.
- The warm barracks scan completed at `armor-barracks-warm.log:2518` and exited 0 at line 20832. It tested 4,788 origins/orientations for the existing barracks and accepted zero.
- The existing explicit S050 path remeasured the current Citywide Alert S061 draft at `citywide-area.log:2431` and exited 0 at line 3064. Every field in the resulting JSON matches the archived S061 r2 receipt. See `citywide-area-comparison.log:1–4`.
- The explicit S050 layout dump completed at `citywide-layout.log:2482` and exited 0 at line 3118. Source, bounds and sample counts match the archived dump. The historical bitmap equality check first failed at `citywide-layout-comparison-first.log:13`. Six blocks have different scenery flags. The live run reached Playing with 132 valid samples. The archived run had `BlockedSpawn` and 394 valid samples. See `citywide-layout-comparison.log:15–18`.
- The targeted Python suite passed 76 tests at `python-focused.log:93–95`. The Armor Break BT/CE allowlist case is at line 1. All 88 existing drafts passed at line 173.
- The full Python suite had the same three quickcheck failures before and after the change. The first failure starts at `python-before.log:193` and recurs at `python-after.log:194`. The runs report an OldQuarter asset pin mismatch and two absent `operations` report keys.

## Source-fit evidence

- The source playable window is XZ `[748,268–2012,772)`. The dump reports a 1280 × 520 native grid with 1 m cells. All 11 successful samples lie inside the same reserve lot. The dump's measured 28 × 15 barracks has zero successful samples. See `armor-native-candidates.log:1–22`.
- The exhaustive scan decoded all 665,600 cells of the pinned Campaign surface. Exactly 650 carry `BuildingPlacement`, all in the single reserve lot. The 669 × 56 m clear stripe at XZ `[1343,616–2012,672)` has 37,464 vehicle-passable cells and zero construction cells. See `armor-fit.log:1–4,8–11`.
- The first invocation of the offline scan had a helper filename mismatch at `armor-fit-first.log:4`. The corrected invocation recorded the source-fit blocker at `armor-fit.log:11`.
- The baked-grid scan found a passable 24 × 24 m rectangle within the stripe at XZ `[1848,634–1872,658)`. Its centroid is 160 m from the east camera bound and 134 m from the north bound. A four-neighbor infantry and vehicle path from (1384,646) to (1860,646) still exists with the intervening stripe XZ `[1400,616–1840,672)` removed. Corridors confined to that intervening stretch can be bypassed on the baked grid. The exhaustive construction test rules out openings in other sectors of the source. See `armor-fit.log:5–7`.

The source-fit gate prevented S061 and S067 from advancing to mission r1, authoring, containment or either template smoke. Sibling authoring, W24 air-pad checks, normal-input journeys, visual review and player/device acceptance remain pending.
