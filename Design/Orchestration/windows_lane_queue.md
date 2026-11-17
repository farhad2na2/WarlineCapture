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

## Next batches

Template (not a batch; do not run):
```
## Batch <N> — <title> (<date>)
status: READY
jobs: S002:130365:en,<CATALOG:SEED:LOCALE>,...
note: <one line>
```
