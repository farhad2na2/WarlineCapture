# M04 Airlift — Editor QA

> Current completion work and superseding results: [M3 / M4 Editor completion QA, 10 September](../M03_M04_Editor_Completion_2026-09-10.md). This older report retains its original checkpoint scope.

Updated combined-build audit: [M3/M4 readiness](m03_m04_readiness_audit.md) reports **106/137 architecture tests passing and 31 failing**. This supersedes the older source-growth-only architecture scope below.

Design-folder review images referenced here remain local and are excluded from Git at the user's request. Production game artwork under `Assets` is included.

Date: 9 September 2026. The playable implementation and core end-to-end Editor checks are delivered; full readiness remains open. The combined audit above records additional ending fixes, Campaign-card presentation gaps, pacing concerns and all 31 architecture failures. The earlier vehicle-panel repair passed a fresh real rescue, but did not reproduce or resolve the reported movement fragmentation. The runs below retain their original scope.

## Implemented and exercised

- Seven M3-style comic panels, dedicated Laila portrait, English/Persian captions, three narrative sequences, and stable addressable Sprite viewports.
- RTS camera start → specialist pickup → landing zone → original RTS camera, with normal completion around 14 seconds and no watchdog fallback.
- Eight rifle escorts, four protected specialists, canonical APC and helicopter, four delayed hostile pursuers; real ground boarding, movement, disembarkation, helicopter boarding, uninterrupted 20-second clearance and airborne departure.
- Twelve fact-driven tutorial topics, contextual ARIA actions, and all 57 guide class entries in both languages.
- Oil/Fuel binding and Persian نفت / بنزین; correct Credits/Fuel icon; connected Persian action labels.
- Typed result details, one-time rewards/unlocks, debrief, campaign return, replay failure, clean retry, and no rewards on failure.

## Passing validation

| Lane | Evidence | Result |
|---|---|---|
| Full Editor acceptance | `Validation/warline-m04-final-05-markers.txt` | PASS, checked wrapper exit 0 |
| Regression suites | Same run | 14 suites, including M1/M2/M3, M4 rules/integration, transport in both languages, HUD, audio routing and camera |
| Live guide and HUD | Same run and `EditorProbe` captures | EN/FA; 16:9/20:9 HUD; all 12 topics and 57 classes; one resident class and pause/resume checks |
| First-clear rescue | Same run | All four delivered; no loss; ~75.6 simulation seconds; 20 seconds of clearance; real airborne departure |
| Replay and Retry | Same run | APC destruction via normal selected-unit command produces defeat; Retry removes old actors and creates a clean 18-actor manifest; settled XP/credits unchanged |
| Idle retry | Same run | No tactical orders; natural deadline defeat at 600,001 ms using 4× simulation; zero passenger/escort losses; no duplicate rewards |
| Comic presentation | `Validation/warline-m04-comics-02-markers.txt`, `ComicQA` | 56 captures: 7 panels × 2 languages × 2 aspects × 2 caption/accessibility settings; 15 stable Sprite IDs |
| Result presentation | `Validation/warline-m04-result-04-markers.txt`, `ResultQA` | PASS, wrapper exit 0; eight locale/aspect/outcome renders; live victory and defeat also inspected |
| Vehicle panel after final repair | `Validation/warline-m04-vehicles-02-markers.txt`, `EditorProbe/vehicle-stage-3-1.png` and `vehicle-stage-6-1.png` | PASS, wrapper exit 0; Persian health values have connected glyphs and complete numbers; localized transport descriptions; real rescue still passes |
| Existing art preservation | `preexisting_art_check.json` | All 61 earlier comic/portrait PNG hashes unchanged from the approved M3 art alignment |

Every pass above requires the explicit marker and wrapper exit 0. Earlier data-only probes did not count as result visual signoff.

## Defects found and corrected

1. TMP numeric formatting and stale binding could leave incorrect resource values. Numeric formatting now runs before TMP and authoritative values repair overwritten placeholders.
2. Persian transport rejection text exceeded a 64-byte ECS string; the full result now uses 512 bytes, and compact assistant metadata truncates safely.
3. The opening camera could fight edge clamping until its watchdog. The camera clamps against the viewport footprint and waits for the perspective transition to settle.
4. Normal VTOL movement did not publish airborne state. The movement owner now observes actual altitude above boarding tolerance.
5. Hidden/disabled passengers could survive attempt cleanup. Cleanup includes disabled actors and validates direct manifest identity.
6. A missing result-region reference left the result invisible after closing the guide. The reference is serialized and resolved from the bounded existing parent; the region resets when results open.
7. Result objective/status columns and four reward lines overlapped after responsive layout. The result reapplies its layout after the responsive owner; compact bilingual objective labels fit both aspects.
8. Manifest duplicates and a carrier-transfer edge case now fail or latch correctly. Successful transfer cannot be undone retroactively by later disembarkation.
9. APC review exposed an unbound Persian health label and untranslated transport descriptions. Final presentation verification records the binding/space/copy repair separately.

## Limits and remaining observations

- Editor only; Android/device validation was explicitly excluded.
- M4 has English/Persian captions but no recorded voice clips. No further external voice request was made after the earlier approval rejection.
- The user-reported moving APC fragmentation was not reproduced in the tested M4 Fast APC. See `vehicle_review.md`; no vehicle fix is claimed.
- The intended first-time 6–9 minute duration is unmeasured. The expert automated route leaves before the four pursuers activate at 120 seconds. The earlier initial-screen/alternate-cover proposal was replaced by this approachable transport lesson and is documented in the production plan. Idle play loses at the deadline, not in the observed combat.
- Final architecture audit: 11 pass / 6 fail. All 15 reported source paths have the same hashes as the prior M3 audit; no new M4 violation paths appeared. See `architecture_attribution.json` and `Validation/warline-m04-architecture-03-markers.txt`. Guard rules were not weakened. Its wrapper exited 0, but the explicit failed audit marker is authoritative.
- The QA snapshot preceded Git delivery. The user subsequently authorized committing and pushing all pending M3/M4 work, including the existing art changes, on `codex/m03-radar-warning`. The task delivery records the verified commit and remote result.
