# M3 / M4 Editor completion QA — 10 September 2026

**Status: functional Editor journeys passed; overall readiness remains incomplete.** M4's separately prepared voice payload is still awaiting approval, and current Editor performance measurements exceed their existing limits. The full suite and subsequent focused repair rerun have completed. Android validation is excluded by user instruction. Evidence images and recordings remain under `/private/tmp`, outside Design and Git.

Functional implementation checkpoint: `720b161af6fbcecebdc7eaa5de8353c81efc2a90` (pushed). Final performance-validation correction: `ae88ed74af37f57197f0a1719f95be5e0be26bd6`. Final HUD/test correction: `6d5e916be8a09316274a002c017072f1dfeac537`. Later documentation/evidence commits do not change runtime behavior. This report supersedes the earlier readiness snapshots; the workplan preserves the rejected runs and their corrections.

## Implemented corrections

- Moving infantry select available Run/Walk or moving-combat clips and propagate the resolved animation to their actual GPU visual roots. Moving far representations update more frequently.
- Cinematic overviews reserve the usable HUD viewport for mission anchors. Explicit player camera requests clear stale cinematic smoothing. M4's normal CAMERA button resolves the current APC/helicopter or boarded-team position instead of an abandoned pickup point.
- Seven ECB singleton queries include system entities, restoring real combat and presentation updates. M4 no longer applies M1's finale combat hold throughout the rescue; pursuing enemies attack normally.
- AI production reserves Credits and Materials independently, settles once, and refunds both on rejection. Existing architecture ceilings and explicit ownership boundaries remain in force.
- Oil/Fuel use the canonical Farsi names نفت / بنزین. Locale identity is `fa-IR`; narrative glyphs survive regeneration. Bilingual HUD labels, ARIA text, guide topics and class entries remain intact after rebuilding shared prefabs.
- M3 Move guidance observes a retained, accepted manual-order receipt followed by arrival. Initial deployment and synchronously drained UI receipts cannot silently complete or stall the lesson. Optional controls clear ARIA's column, and the complete contact warning fits its banner.
- Result layouts respond to root-canvas size changes before rendering. Victory, defeat, and save-failure headings and controls fit both aspects; the full English save-failure title is no longer ellipsized.
- The shared guide rebuild retains the M4 catalog. Squad shortcuts now name the vehicle/helicopter/jet groups actually selected, in both languages.
- The approved M3 payload generated and installed all **46 local voice clips**. Generated event constants/hashes match the catalog, and nested mission Voice folders receive the intended mono, compressed, background-load import policy.
- Performance validation now tests the allocation counter with a real 8,192-byte positive control. An unavailable counter cannot pass as zero. Four regression cases retain the original allocation and frame-time thresholds. The M3 probe reports the actual Game camera resolution and supports a verified maximized Game view capture.

## Automated checks

| Check | Result | Evidence |
| --- | --- | --- |
| Full Editor suite, run 04 | **4,370 passed / 3 failed / 2 existing opt-in probes skipped**, 4,375 total, exit 2 | `M03M04Completion/editor-tests-20260910.xml.gz` |
| Nine core architecture fixtures in that full run | **139/139 passed** | Same XML |
| Final affected-fixture rerun, 05 | **46 passed / 1 failed** (only the required M4 voices), 47 total, exit 2 | `M03M04Completion/editor-regressions-20260910.xml.gz` |
| Source growth and authorization, checkpoint 12 | **18/18 passed**, wrapper exit 0 | `ArchitectureMaturity/Logs/m03_m04_completion_source_growth_20260910_final.log.gz` |
| Refreshed dependency inventory | **Passed**: 23 first-party assemblies, 132 dependency edges | `2026-07-10_aph-700_first_party_assembly_dependencies.json` |
| Final repository/tooling suite, run 12 | **435/435 passed**, 319.615 seconds, exit 0 | `/private/tmp/warline-completion-python-tests-12.log` |

Full run 04 found two additional regressions beyond the known M4 voice requirement: the Edit Mode stale-profile voice test did not pump Addressables, and a runtime-owned squad subtitle retained an unlocalized authoring placeholder. Both were corrected and passed in rerun 05, along with the four allocation-evidence regression cases. The actual Persian clip is still required; no voice assertion or localization coverage rule was disabled. Coverage passed across 38 prefabs, 1,199 bindings and 1,885 keys in each language. The full-run counts above remain unchanged; the subsequent scoped corrections are supported by the 47-test rerun, not presented as another full-suite pass. Portable results and log hashes are in `M03M04Completion/validation-summary.json`.

## Live Editor journeys

| Journey | Observed result | Log (all exit 0) |
| --- | --- | --- |
| M3 complete guidance and victory | Real Move → arrival → Hold → Stop → restored Hold; Victory at **199.692 simulated seconds**; all seven enemies defeated; finale, debrief, saved rewards, M4 availability and actual Campaign return; old actors disposed | `/private/tmp/warline-completion-m03-final-guidance-05.log` |
| M3 sensor-loss recovery | Sensor-only fault, scout warning retained, unavailable Ping uncharged, both charges retained; normal rifle defense won at **185.384 seconds** | `/private/tmp/warline-completion-m03-sensor-06.log` |
| M3 defeat and retry | Actual retreat caused core-breach Defeat at **109.046 seconds**; both languages/aspects, guide return and Retry restored all 20 actors, resources and Ping charges | `/private/tmp/warline-completion-m03-defeat-resize-09.log` |
| M3 save-failure recovery | Victory at **192.821 seconds**; actual atomic-write failure left the saved profile unchanged; four complete error-screen variants, actual Retry Save and duplicate rejection; exactly one durable reward grant, debrief and normal victory | `/private/tmp/warline-completion-m03-save-recovery-07.log` |
| M4 rescue, replay, defeat and retry | APC boarding/movement/unloading, helicopter boarding, 20-second clearance and departure; debrief, exactly-once rewards/unlocks and Campaign return; actual Replay carrier loss and fresh Retry; idle retry lost through ordinary combat at **164.011 seconds**, before timeout | `/private/tmp/warline-completion-m04-player-camera-05.log` |

M3 captured all six briefing/debrief panels with large captions in English/Farsi at 16:9 and 20:9 (24 variants). M4 verified all 12 guide topics and 57 classes in both languages; opening the guide paused the mission. Both missions' final victory/defeat screens passed viewport and text checks in both languages/aspects. Wide victory, defeat and save-error screens were visually inspected with complete headings, objectives/reasons, rewards and action buttons.

M3's motion audit recorded **624 visible moving-infantry observations, all Run/RunAim**, 2,761 matching renderer targets and 503 distinct shader frames. M4's accepted run-04 audit recorded **622 visible moving-infantry observations, all Run**, 2,277 matching renderer targets and 499 distinct shader frames. All observed old playback indices were nonzero crossfades into different target frames, matching the existing half-second transition. APC movement and helicopter departure were visually inspected with intact parts and the subject inside the usable camera viewport. Current M4 screenshots are under `/private/tmp/warline-m04-readiness`; the separate run-04 motion/capture evidence remains preserved.

These are concrete Editor observations of readable action, visible locomotion, meaningful pressure, recovery and reward feedback. They do not certify subjective enjoyment or every possible frame.

## Performance and allocations — not accepted

The unchanged Editor limit is **20 ms P95**, with at least 180 measured frames and a current-thread allocation budget of zero. No limit or historical accepted baseline was relaxed or replaced.

| M3 real-time run | Preparation / vanguard / warning / main P95 (ms) | Result |
| --- | --- | --- |
| Normal Editor, 06 | 43.9 / 51.5 / 51.1 / 44.5 | Failed frame limit; combat completed |
| Initial focus attempt, 07 | 16.8 / 22.7 / 29.1 / 25.7 | Failed; reported `gameViewMaximized=false`, so not accepted as a maximized-view capture |
| Verified maximized Game view, 08 | 23.9 / 36.5 / 50.5 / 46.2 | Failed frame limit; combat completed at 184.463 seconds, 1920×1080, normal simulation speed |

Environment: Unity 6000.5.2f1, macOS 26.6.2, Apple M5. The wide run-to-run variation is observed, not attributed to an unproven cause. Data resides in `M03RadarWarning/LaunchProbe/editor-performance*-20260910.json`. The .NET current-thread counter failed its positive control in these runs, so its allocation result is unavailable. Unity's whole-frame allocation counter includes Editor work and cannot be attributed to M3 alone.

The separate large-match regression (run 12, wrapper exit 1) measured **256 frames, 15.732 ms average, 23.092 ms P95**, with 5,082 unit entities, 4,977 runtime buildings and 47 estimated visible models, exceeding the fixture's minimum counts. Its frame budget failed. Its positive control also rejected the allocation counter; `-1` explicitly means unavailable, not zero. Fresh results are `performance_regression_match_completion_20260910.{json,md}`. Historical `performance_regression_match_baseline` files are preserved byte-for-byte.

Raw Profiler allocation attribution is valid independently of the unavailable .NET counter. Run 06 (wrapper exit 0, source checkpoint `720b161af`) recorded four windows of 196/210/210/210 frames with **8,224-byte positive controls**, and samples for all eleven owners. Ten mission-system owners total **48 bytes**, one `Mono.JIT` child in threat detection. HUD refresh totals **25,930 bytes in 31 changed-text calls**; the unchanged preparation and late-combat HUD windows allocated zero. This is attribution evidence, **not a zero-allocation acceptance pass**. Raw data: `M03RadarWarning/LaunchProbe/allocation-attribution-completion-20260910.json`.

## Remaining work

1. **M4 voice generation and final integration:** the prepared 19 English/Farsi pairs (38 clips) in `M04Airlift/voice_payload_review.json` await separate approval. The user's approved M3 payload was fully completed. M4's required-voice test stays enabled; its captions work, and the narration gateway prevents unrelated M3 audio from playing for the M4 guide. M4 audio generation, import, mission-specific ARIA routing and final voice validation remain outstanding.
2. **Performance:** investigate the Editor frame-time excess and establish a valid allocation acceptance measurement. Existing frame and zero-allocation gates remain red; functional victories do not waive them.
The implemented repairs, test results and documentation are committed for delivery on `codex/m03-radar-warning`. M3/M4 are not marked fully complete while the two outstanding items above remain.
