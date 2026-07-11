# First-Launch Storyboard Contract Validation

Date: 2026-07-10

Status: Gate 4 passed internally; 22 storyboard frames, manifest, contact sheet, and both mobile safe-area reviews are locked

## Automated Contract Checks

| Check | Result |
|---|---|
| Declared final-panel count | Pass: `22` |
| Actual panel records | Pass: `22` |
| Unique panel IDs | Pass: `22` |
| IDs cover `FL-P01` through `FL-P22` | Pass |
| Required manifest fields on every panel | Pass |
| Static fallback declared on every panel | Pass |
| Skip destination declared on every panel | Pass |
| Reduced-motion behavior declared on every panel | Pass |
| Gameplay handoff anchor | Pass: `FL-P18` uses `camera.ch01.m01.planning` |
| Fixed pre-handoff visual duration | Pass: `64` seconds |
| Logo maximum | Pass: `2.5` seconds |
| Post-gameplay review duration | Pass: `17` seconds |
| Identity can continue with a default | Pass by auxiliary-state contract |
| Guidance can continue with a default | Pass: `FullGuidance` |

## Canonical Coverage

- Seven `seq.prologue.command_lost` beats: Pass (`FL-P01`-`FL-P07`).
- Two `seq.prologue.commander_identity` visual states: Pass (`FL-P08`-`FL-P09`).
- Guidance choice: Pass as a separate runtime state after `FL-P09`.
- Five `seq.ch01.open.first_response` beats: Pass (`FL-P10`-`FL-P14`).
- Three `seq.ch01.m01.brief` beats: Pass (`FL-P15`-`FL-P17`).
- Illustrated handoff: Pass (`FL-P18`).
- Review-only gameplay placeholder and Jump To Debrief: Pass as a separate auxiliary state.
- Three `seq.ch01.m01.debrief` beats: Pass (`FL-P19`-`FL-P21`).
- Command-base reveal: Pass (`FL-P22`).

## Remaining Gate 4 Checks

- Generate one accepted visual storyboard thumbnail for each panel: Pass, `22/22`.
- Assemble the numbered contact sheet: Pass, `3000 x 1296`.
- Review 16:9 and 20:9 crops with subtitle and Skip safe areas: Pass.
- Correct phone-scale face, weapon, route, evidence, and action ambiguity: Pass.
- Re-run timing and state-graph checks after visual revisions: Pass.

## Deterministic Contact-Sheet Tooling

`Tools/NarrativeVision/build_first_launch_storyboard_contact_sheet.sh` accepts the three approved 3-by-3 storyboard sheets, extracts `FL-P01` through `FL-P22`, normalizes each frame to `640 x 360`, and creates:

- the numbered storyboard contact sheet;
- a 16:9 subtitle/story/Skip safe-area contact sheet;
- a centered 20:9 safe-area contact sheet; and
- a machine-readable validation record.

The script does not alter visual content beyond deterministic crop, scale, label, and review-overlay operations. It must only run on internally accepted storyboard sheets.

Tool verification passed on 2026-07-10 using the rejected Sheet 1 image as a temporary three-input fixture: it produced 22 uniquely numbered `640 x 360` frames, a `3000 x 1296` contact sheet, matching 16:9 evidence, a `2480 x 1944` 20:9 evidence sheet, and schema-valid JSON. Those temporary visual outputs are not tracked because the source candidate is rejected.

## Accepted Storyboard Sheets

| Sheet | Panels | Result | Evidence |
|---|---|---|---|
| `STORYBOARD-SHEET-01_P01-P07_CandidateB_ACCEPTED.png` | `FL-P01`-`FL-P07` | Pass | `evidence/visual/storyboard/STORYBOARD-SHEET-01_P04_CORRECTION_COMPARE.png` |
| `STORYBOARD-SHEET-02_P08-P16_CandidateB_ACCEPTED.png` | `FL-P08`-`FL-P16` | Pass | `evidence/visual/storyboard/STORYBOARD-SHEET-02_CORRECTION_COMPARE.png` |
| `STORYBOARD-SHEET-03_P17-P22_CandidateA_REJECTED_SAFEAREA.png` | `FL-P17`-`FL-P22` | Reject | `evidence/visual/storyboard/STORYBOARD-SHEET-03_CandidateA_SAFEAREA_REJECTION.png` |
| `STORYBOARD-SHEET-03_P17-P22_CandidateB_PARTIAL.png` | `FL-P17`-`FL-P22` | Partial | `evidence/visual/storyboard/STORYBOARD-SHEET-03_CandidateB_SAFEAREA_REVIEW.png`; P18 passes, P20 fails |
| `STORYBOARD-SHEET-03_P17-P22_CandidateC_ACCEPTED.png` | `FL-P17`-`FL-P22` | Pass | Standalone P20 Candidate C plus deterministic full-cell replacement; final safe-area contact sheets pass |

These sheets are Gate 4 composition artifacts. They do not increment the final Gate 6 cinematic-frame count.

The first complete contact-sheet build was generated successfully, then rejected during visual safe-area review. Its frame set, numbered contact sheet, 16:9 evidence, 20:9 evidence, and validation JSON are preserved with the `CONTACT-CANDIDATE-A_REJECTED` prefix. This verifies the pipeline while preventing the failed P18/P20 compositions from becoming the canonical storyboard.

`Tools/NarrativeVision/replace_storyboard_grid_cell.sh` provides deterministic full-cell correction for the fixed `1672 x 941` 3-by-3 storyboard format. It validates the source dimensions and cell index, normalizes one AI-generated replacement to the complete cell rectangle, preserves all other pixels and gutters, and verifies the output dimensions. P20 is cell index `3` in Sheet 3.

The replacement tool passed a round-trip test using Candidate B's original P20 crop: the reconstructed sheet remained `1672 x 941`, and ImageMagick absolute-error comparison reported `0` changed pixels. This proves that only pixels supplied by a different complete replacement frame can change.

## Final Gate 4 Evidence

- `first_launch_contact_sheet.png`: 22 ordered and numbered storyboard frames.
- `first_launch_safe_area_contact_sheet_16x9.png`: centered story-safe rectangle, lower subtitle reserve, and top-right Skip reserve.
- `first_launch_safe_area_contact_sheet_20x9.png`: centered 16:9 story composition inside the 20:9 runtime canvas with the same reserves.
- `first_launch_contact_sheet_validation.json`: source sizes, panel count, ID range, normalized frame size, and contact-sheet size.
- `frames/FL-P01.png` through `frames/FL-P22.png`: normalized `640 x 360` animatic inputs.

All canonical beats are present and ordered. Commander identity remains dynamic and faceless where required. Dalia, Samira, ARIA, JRC, civilians, and the three-person Ash patrol remain distinguishable. Old Market and Relay geography persist. No approved frame contains readable generated text, a clinic cross, real insignia, a religious symbol, or graphic harm. P18 now separates JRC player anchor, empty decision lane, and distant Ash patrol. P20 keeps fragmentary evidence above the subtitle reserve.

## Gate 4 Decision

Gate 4 passes internally. Storyboard composition coverage is `22/22`; final cinematic-art completion remains `0/22`. Phase 6 animatic production may begin without user approval.
