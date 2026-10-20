# Skirmish saga menu — implementation and evidence

Working branch: `codex/skirmish-saga-menu`. **No merge authorized: owner visual review required.**

The approved saga replaces the grid and the six battlefield cards with a winding, native 24-stop road. It retains the existing catalog, medal ledger, ownership evaluator, launch resolver, Campaign map sources, localized TMP typography, and colorful command buttons. Theatre adds a dotted route and objective medal trails. Existing legacy Grid code remains dormant pending the owner’s deletion decision.

## Unity Game view scale bug

The shell used a 4800×2160 logical canvas, but `FillComposition` reset its child scale to one. This stretched the map while fixed 80-point stops and 100-point footer controls rendered at half their intended size. `SkirmishLibraryFrame.FitComposition` now shares a 1080-point reference-height calculation with the offline geometry checker. Both live and inset-device layout use that calculation.

A second binding bug collapsed `MapPreviewClip` to zero size after a scenario refresh. That obsolete anchor reset is removed. Stops also now register their medallions as native raycast targets.

Actual normal-input Play evidence from attempt 05: 2400×1080 rendered frame, footer 100 pixels, stop diameter 79.99994 pixels, complete native Main Menu → Theatre → Zoom → objective → S002 → Launch navigation. The menu screenshot is in `../../SkirmishExpansion/S002/_Evidence/`, artifact ID `1827490ac90045c1a3a4dd364e5e855e`. This run failed later in combat; it is not a successful mission journey.

## Separate acceptance records

| Gate | Latest verified state |
| --- | --- |
| Native EditMode, ledger, access, launch and menu tests | Tests 17: 78 passed, 0 failed, 0 skipped |
| ARIA plan/control checks | Tests 17: 141 cases passed |
| Dense logical canvas regression | Scale 03 passed EN 20:9 and FA 4:3; actual Play 05 also measured native scale |
| Full 16 native captures | Matrix 09 captured 16 but failed 4:3 EN Airfield Plains area/interior visibility; focused 4:3 EN capture 05 now passes all unchanged gates |
| Focused native cue captures | Native 12 passed 5; AP FA 4:3 passed separately |
| Strict theatre geometry | Full 09 passed on the compact-coast repair: 113 seated, 0 blocked |
| Full normal-input ARIA/result/return | Attempt 06 passed the actual S002 ARIA victory/result/return/disk flow; its separately observed briefing visual defect is being fixed before final acceptance |
| Medal persistence across an actual process restart | Restart 01 passed in a different Editor PID on the same candidate, using the original earned profile and native Touch navigation |
| Owner visual review | Pending |
| Real player/device acceptance | Pending |

## Preserved failure evidence

All failed and interrupted runs remain on disk. `iterations/`, `tests/`, `normal-input/`, and the unique S002 trace artifacts record their own candidate and outcomes. A stale `failure.txt` or `failures.txt` in a reused capture directory is historical; use the explicit per-run log and manifest. Do not delete it or treat its absence as validation.

The three aircraft geometry dependency-stamp updates were measured separately against exact backups. Geometry, binding fields, GUIDs and original strict guards were preserved; see `normal-input/cache-stamp-preparation/`. This is dependency metadata maintenance, not a physical map change.

Tracked handoff recovery scenes remain in `Assets/_Recovery`; an audit copy is retained in `baseline/recovery-scenes/`. No scene source was changed by this task.

## Outstanding theatre coverage gate

The current legal coast layout remains below the handoff’s 0.65–0.70 territory coverage target (roughly 0.42 at 16:9, 0.32–0.37 at 20:9, and 0.50 at 4:3). All 12-pixel halo and 10-percent aspect constraints remain in force. Isolated 15/20-percent aspect experiments are retained under `tests/`; they do not prove that higher coverage is impossible and were not shipped. A strict checker pass must not be reported as satisfying this separate visual coverage target. The owner decision is still open.
