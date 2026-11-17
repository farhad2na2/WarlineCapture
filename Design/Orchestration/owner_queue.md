# Owner queue

Decisions the orchestrator took by default, and items waiting for the owner. Newest first.
Nothing here blocks production.

## Waiting for the owner

- **Owner gates (non-blocking):** manual play sample (one mission per layout), native UI review and
  device acceptance. First review packet is due when 30 missions are auto-accepted.

## Decisions taken by default

| Date | Decision | Why |
|---|---|---|
| 2026-11-16 | Snapshot branch `missions/snapshot-2026-11-16` (commit `329d61ba3`) holds game code, tools, plans. Left out: run logs, untracked evidence dumps under `Design/AgentReports`, `Design/CampaignFilm` media, Unity recovery scenes, simulator build outputs. | Keep the snapshot small and source-only. Nothing was deleted; the left-out files are still on disk, untracked. |
| 2026-11-16 | Saved the measured layouts for S026, S050 and S074 to `Design/Orchestration/salvage/` before the simulator log folder was deleted. None was found for S049 or S098. | Authoring input only; not evidence. |
| 2026-11-16 | Historical Unity wins are recorded in `missions.csv` notes but every gate is `unverified on current tree`. | All recorded wins predate three weeks of uncommitted gameplay changes. |

## Corrections

- **2026-11-16 — S004.** The audit said S004 had never won with ARIA. That was wrong: `runs.csv` under
  `Design/AgentReports/SkirmishExpansion/S004` records 45 ARIA wins on 3–4 October (27 English,
  18 Farsi) alongside 3 defeats and 22 aborts. S073 is still a draw with no win.

## Decisions taken by default — 2026-11-17

- **Git background maintenance is off for this repository** (`maintenance.auto=false`, `gc.auto=0`).
  Each commit had started a full repack of the 30 GB history; eleven ran at once and left 183 GB of
  temp files, which took free disk from 208 GB to 25 GB and paused Unity for six hours. The temp
  files were deleted on your instruction (06:29); the real pack files are intact. To compact the
  repository later, run one `git gc` by hand while nothing else is running.
- **Still on disk, your call:** `Logs/private-candidates` (50 GB) and `T Logs/SkirmishProduction`
  (32 GB), the old simulator logs. Not needed by this program; not deleted.
