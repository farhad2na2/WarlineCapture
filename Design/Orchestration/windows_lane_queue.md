# Windows lane queue

The Mac program lead edits this file. The Windows worker only reads it and never commits changes to it.

Each batch starts with a `## Batch` heading. Its status is `READY` (run it), `DONE` or `STOP` (the worker stops the loop).
Jobs use `CATALOG:SEED:LOCALE`. The first job of every batch is the warm-up `S002:130365:en`. It never counts.

## Batch 1 — first-run check (2026-11-17)
status: READY
jobs: S002:130365:en,S002:104731:en
note: first job is the warm-up and does not count; this batch is the first-run check.

## Next batches

Template (not a batch; do not run):
```
## Batch <N> — <title> (<date>)
status: READY
jobs: S002:130365:en,<CATALOG:SEED:LOCALE>,...
note: <one line>
```
