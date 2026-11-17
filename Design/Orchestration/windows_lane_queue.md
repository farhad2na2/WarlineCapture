# Windows lane queue

The Mac program lead edits this file. The Windows worker only reads it and never commits changes to it.

Each batch starts with a `## Batch` heading. Its status is `READY` (run it), `DONE` or `STOP` (the worker stops the loop).
Jobs use `CATALOG:SEED:LOCALE`. The first job of every batch is a warm-up (normally `S002:130365:en`; if the batch already contains that job, the batch names another warm-up). It never counts.

## Batch 1 — first-run check (2026-11-17)
status: DONE (result 2026-11-17-1550.md: 104731 en Defeat 192.5 s vs Mac Victory 262.5 s — one sample each, inconclusive; compare with batch 2)
jobs: S002:130365:en,S002:104731:en
note: first job is the warm-up and does not count; this batch is the first-run check.

## Batch 2 — S002 cross-machine comparison (2026-11-17)
status: READY
jobs: S004:104733:en,S002:155923:en,S002:130365:en,S002:104731:en,S002:130365:fa-IR
note: pull origin/missions/integration first (same commit as the Mac acceptance run lane5-s002). First job S004 is the warm-up and does not count. In the result file also give, per S002 job, the defeat/victory reason line, final base HP for both sides, and counts of `event=tray_page` and `[SkirmishGateUnits] event=used` with its unused= value. Mention any `result=Failed` cache or source-dependency lines.

## Batch 3 — Release code optimization fps check (2026-11-17)
status: DONE (2026-11-17-1649.md: S004 13.8 fps in Release vs 3.4 in Debug; S002 startup timeout)
jobs: S004:104733:en,S002:130365:en
note: pull first: the runner now passes -releaseCodeOptimization (Mac compiles in Release; Windows ran in Debug). Expect a script recompile at launch, never a Library rebuild — if the log shows a Library rebuild, stop. Report per job the [FrameRateDiag] fps/cpuMain mean (target ≥ 20 fps), result lines and the Bee .dag name used. Results do not count; this is a performance check.

## Batch 4 — fps re-check after the owner's Windows settings (2026-11-17)
status: DONE (2026-11-17-1741.md: 16–18 fps; S002 130365 en and 155923 fa-IR lost at ~96.6 s to the lone tank, home defence saw no threat)
jobs: S004:104733:en,S002:155923:fa-IR,S002:130365:en
note: owner set power plan Best performance, Bitdefender exclusions and update pause. Pull first. First two jobs are warm-ups (S004, then a distinct S002 job so S002's scene re-bakes before the measured job); neither counts. Report per job [FrameRateDiag] fps and cpuMain means (target ≥ 20 fps), result lines, terminal-frozen result, and the S002 130365 en seconds vs the Mac (~260 s). Results do not count yet.

## HOLD — no other ARIA batches (2026-11-17)
status: HOLD
note: Windows ARIA matches run at 3–4 fps vs ~30 fps on the Mac (CPU-main-bound). No ARIA batch runs until the cause is found and fixed; read-only diagnosis only.

## Next batches

Template (not a batch; do not run):
```
## Batch <N> — <title> (<date>)
status: READY
jobs: S002:130365:en,<CATALOG:SEED:LOCALE>,...
note: <one line>
```
