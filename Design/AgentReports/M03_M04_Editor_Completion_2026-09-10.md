# M3 / M4 Editor completion QA — 10 September 2026

Status: implementation and final Editor acceptance in progress. This report supersedes readiness counts in the earlier M3 and M4 snapshots. Android validation is excluded by user instruction. Captures remain under `/private/tmp`; no evidence images are stored in Design.

## Implemented corrections

- Moving infantry prefer the available Run/Walk or moving-combat clips and propagate the resolved animation to their actual GPU visual roots. Far representations update more frequently while moving.
- Cinematic overviews frame mission anchors within the usable HUD viewport; player camera requests clear stale cinematic smoothing. M4 follows the live APC/helicopter through the normal CAMERA button.
- Seven ECB singleton queries now include system entities, restoring real combat/presentation updates. M4's finale no longer applies M1's combat hold to the entire mission; pursuing enemies can attack.
- AI production reserves the actual Credits and Materials separately, settles once, and refunds both resources on rejection. Building and UI ownership remain explicit, with existing architecture ceilings retained.
- Oil/Fuel use the canonical Farsi names نفت / بنزین. Locale identity is `fa-IR`; narrative glyphs survive regeneration. M3/M4 ARIA guidance updates immediately and uses the mission's own twelve-step sequence.
- The approved M3 payload produced 46 installed local voice clips. Generated ARIA event constants/hashes match the catalog. M4's separate 38-clip payload remains pending external-generation approval; its required-voice test remains enabled.
- M3's Move lesson requires a new accepted manual movement receipt followed by arrival, so initial deployment and synchronously drained UI receipts cannot silently complete or stall the lesson. Optional controls now clear ARIA's column, and the contact banner fits its full warning.
- Result compositions detect changes to the root canvas size before rendering, keeping header and Retry controls inside 20:9 bounds.
- Rebuilding the shared field guide preserves both mission catalogs. Squad shortcuts use the matching vehicle/helicopter/jet labels in English and Farsi.

## Test evidence

| Check | Result | Evidence |
| --- | --- | --- |
| Complete Editor suite, run 03 | 4,352 passed / 8 failed / 2 existing opt-in probes skipped | `/private/tmp/warline-completion-editmode-all-03.xml` |
| Nine core architecture fixtures within that suite | 139/139 passed | Same XML |
| Remaining audio/config/UI regression fixtures | 63/63 passed | `/private/tmp/warline-completion-editmode-regressions-04.xml` |
| Final Move receipts, shared-guide regeneration, bilingual HUD bounds, movement and source growth | 50/50 passed | `/private/tmp/warline-completion-guide-regeneration-01.xml` |
| Result resize, four save-headline variants, precise Editor-cloud classification and source growth | 27/27 passed | `/private/tmp/warline-completion-result-headline-unit-02.xml` |
| Python repository/tooling suite | 435/435 passed | `/private/tmp/warline-completion-python-tests-11.log` |

The seven fixable failures from the full suite were resolved and rerun. The known remaining required-voice failure belongs to M4. A final integrated suite is still required after the latest gameplay/layout changes and regenerated assembly inventory.

## Live journeys

- M3 guidance run 05 (`/private/tmp/warline-completion-m03-final-guidance-05.log`, exit 0): real Victory at 199.692 simulated seconds after Move, arrival, Hold and Stop. All seven enemies defeated; first-clear rewards, M4 availability, finale, debrief, bilingual/aspect results, guide return and Campaign return passed. Twenty-four large-caption briefing/debrief captures covered English/Farsi and 16:9/20:9. The final Farsi 20:9 result was visually inspected: stars, losses, all rewards and Continue were readable.
- M3 motion recording: 624 visible moving-infantry observations, all Run/RunAim; 2,761 visible renderer targets matched, with 503 distinct shader frame values. Playback-index differences were the existing 0.5-second crossfade: all 112 affected mesh samples had nonzero blends into a different target frame. These were not frozen Idle poses.
- M4 player-camera run 04 (`/private/tmp/warline-completion-m04-player-camera-04.log`, exit 0): fresh captures, both HUD languages/aspects, all 12 guide topics and 57 classes in both languages, APC/helicopter rescue, debrief, saved unlocks, results, Campaign return, actual Replay carrier-loss defeat and clean Retry passed. The idle retry lost through normal combat at 158.482 simulated seconds without reaching the timeout or granting duplicate rewards.
- M4 motion recording: 622 visible moving-infantry observations, all Run; 2,277 renderer targets matched, 499 distinct shader frames. All 130 playback-index transition samples had nonzero crossfades. APC road movement and helicopter departure were visually inspected: their parts remained together, the normal CAMERA action kept them inside the usable viewport, and corrected Farsi shortcut labels were visible. Captures are local under `/private/tmp/warline-m04-readiness`.

These are sampled Editor observations, not a guarantee about every possible frame or a device performance claim.

## Remaining acceptance

HUD layout, M3 guidance, and the combined M4 rescue/replay/idle journey now pass. Sensor-loss recovery passed (run 06, exit 0, Victory at 185.384 seconds with the scout warning retained and unavailable Ping uncharged). Defeat run 09 passed with the canvas-resize correction, real Retry, and viewport checks in both languages/aspects; the fresh Farsi 20:9 image was visually verified. Earlier run 06 had clipped vertically and is superseded. Save recovery run 07 also passed (exit 0): an actual failed atomic write left the profile unchanged; four complete bilingual/aspect error screens preceded successful Retry Save, exactly one reward grant, debrief and normal victory. M4 final run 05 passed (exit 0), including wide victory/defeat checks in both languages and normal-combat idle Defeat at 164.011 seconds. The wide victory and Farsi defeat images were visually inspected; all result content and actions fit. Current performance observations and the final integrated test rerun are pending. Inclusive Editor allocation measurements must be reported separately from owner-attributed gameplay allocations; no zero-allocation gate or performance threshold is relaxed.
