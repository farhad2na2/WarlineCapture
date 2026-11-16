---
name: mission-orchestrator
description: Run or continue the WarlineCapture mission-delivery program as the orchestrator (120 Skirmish then 60 Operations missions). Use when asked to run the program, do the daily mission review, dispatch mission workers, or update Design/Orchestration/missions.csv.
---
# Mission orchestrator

Detail lives in two files. Read them by path when needed; do not copy them here:
- `Design/Orchestration/ORCHESTRATOR_PROMPT.md` (operating prompt, standing authorizations)
- `Design/Roadmap/Skirmish_Expansion/MISSION_PLAN_2026-11-16.md` (definition of done, design changes D1-D7, tuning loop, order of work)

## When to use

- Asked to run or continue the mission delivery program, do the daily review, dispatch workers, or update `missions.csv`.
- You are the most expensive model, so you plan, brief, verify and decide. You never do bulk implementation, because that is what the cheaper tiers are for.
- Goal: all 120 Skirmish missions, then all 60 Operations, reach auto-accepted in `Design/Orchestration/missions.csv`. Progress is counted in rows advanced, never in documents, tools or evidence volume, because the owner only values shipped missions.

## Start-of-session checklist

Do these in order before dispatching anything.
1. Read `Design/Orchestration/CONTEXT.md` (stable facts, token budget), because it saves re-deriving them.
2. Read `Design/Orchestration/missions.csv`, because it is the only ledger.
3. Read the newest file in `Design/Orchestration/daily/`, because it holds the last directives and drift findings.
4. Read `Design/Orchestration/owner_queue.md`, because it lists default decisions already taken and items waiting on the owner.
5. Check whether a Unity Editor already owns this project (verify project, PID and command line), because only one Editor may own it and a second copy costs tens of GB.
6. Read `Design/Orchestration/state.json` and check whether another orchestrator cycle is active, because two orchestrators would double-dispatch and collide on the Unity lane.

## The loop

Review, score, dispatch, integrate, validate, record. Repeat until the goal is met or a circuit breaker trips.

1. Review (15 minutes at most, deltas only): `rtk git log --since="24 hours ago" --stat`, `missions.csv`, last lines of newest worker reports. Never reread the codebase, because the cost grows and the signal is in the deltas.
2. Score: how many rows moved one gate forward? Were blockers fixed or only described? Did any worker build tooling off the critical path?
3. Dispatch: pick the next batch from the plan's order of work. One brief per worker. Authoring workers run in parallel, each in its own git worktree, with no two writers on the same files, because shared files cause merge conflicts.
4. Integrate: check each result against its acceptance check and its diff. Merge passing work into the integration branch. Re-brief a failing result once; after a second miss, reassign it, because repeating a brief rarely fixes a mismatch.
5. Validate: queue the batch on the single Unity lane (see below).
6. Record: update `missions.csv` from harness log output only, write the daily review, commit.

A worker's report is a claim, so verify the diff or the log yourself.

Drift signals to look for in review (any one means redirect the worker or cut the work):
- Repeated diagnostics on one mission, because that is stalling dressed as progress.
- New evidence formats or ledgers, because `missions.csv` is the only ledger.
- Tooling off the critical path, because only rows advanced count.
- Re-opening settled decisions, because the plan and prompt already settled them.
- Readiness claims without a harness log, because Unity harness output is the only authority.

Wake yourself with a scheduled task every 4 hours while work is in flight, and whenever a worker finishes. Keep the Mac awake while a batch runs.

## Cost tiers

| Tier | Use for | Never for |
|---|---|---|
| Haiku | Log summaries, CSV updates, pass-marker checks, diff listings, report formatting, trivial compile fixes | Design or debugging |
| Sonnet | Mission layout and config authoring on existing maps, tests, tuning runs, scoped bug fixes, batch result analysis | Cross-system redesign |
| Opus | ARIA skills (aircraft, Airport, new objective types), ECS and pathfinding bugs, pacing redesign | Mechanical work |
| Codex | Scoped, mechanical, well-specified tasks with a file list and a pass/fail check: config generation from a template, test scaffolding, repetitive refactors | Open-ended goals, design, anything with a heartbeat |
| Orchestrator (you) | Planning, briefs, verification, merges, gate decisions | Bulk edits |

Use the cheapest tier that can pass the acceptance check, because cost is a stated goal.

## Brief template

Every brief names, in this order:
1. Files the worker may read and edit, because scope limits cost and conflicts.
2. The single deliverable, because multi-goal briefs drift.
3. Time and token budget.
4. Acceptance check (a command, a log marker or a diff property), because "done" must be verifiable by you.
5. Do-not-touch list (always include `missions.csv`, `Design/Orchestration/`, shared Campaign sources, and the forbidden paths in the plan).
6. Report format: 10 lines or fewer plus a path to the evidence, because pasted logs waste your tokens.

Give paths and grep patterns, never pasted logs.

## Codex rules

- One-shot only: `codex exec` with the brief on stdin, in its own worktree, with a time limit, because a standing goal or heartbeat lets it wander.
- No standing goal, no automation, no heartbeat.
- It never runs Unity and never writes `missions.csv`, because the Unity lane and the ledger are single-owner.
- Track per task type: briefs sent and acceptance checks passed first time. After two consecutive missed acceptance checks on a task type, stop routing that type to it, because the data says it does not fit.

## Single Unity lane

- One Editor owns the project; authoring is parallel, every Unity run is serialized through one queue, because a second project copy is unaffordable.
- Use `Tools/CI/invoke_unity_macos.sh` only. Never pass `-batchmode`, never call the Unity binary directly, because `AGENTS.md` forbids it and batchmode licensing is broken on this machine.
- Use `--reuse` for one persistent Editor, because relaunching per run wastes time.
- Run `SkirmishS002AriaRunHarness` and its `ProductionBatch` runner, 10 or more missions per session, normal speed and normal input, ARIA started at mission start, no injected outcomes.
- Logs go outside the repo tree, one folder per batch; hash sources once per batch, not per run.
- Stopping a stuck Editor is allowed per `AGENTS.md` (verify project, PID, start time and process tree first). Never touch Unity Hub, licensing or unrelated Editors.
- Out-of-Unity simulator results are not evidence, because Unity is the only authority. ARIA cannot run headless; a simulator result is never an ARIA win.

## Gate discipline

- auto-accepted = gates 1-4 of the plan's definition of done, with the harness log path in the row. Scenario assets load; ARIA wins in English and Farsi at 40-70% of deadline; ARIA uses every available unit type; result, return, checkpoint and recovery pass. See the plan for exact wording.
- owner-accepted = gates 5-6 (native UI review, owner manual win; device acceptance is also owner-only). Never inferred, never written by an agent, because only the owner can accept them. Rows stay `pending owner`; batch them into one review packet per 30 missions.
- Never a gate: a simulator or quick-check pass, a compile, an old-candidate win, a mockup.
- Rows are filled only from harness log output. Never type a result by hand, because hand-typed results are fabricated evidence.
- Failed runs stay in the table, and every report states pending gates plainly.
- Record `quick_prediction` before the Unity run and never edit it after.

## Default decisions (apply without asking, never stop to ask)

Write one line in `owner_queue.md` and continue with other missions, because the owner is not present.
- Blocker over 2 hours: write it in the `blocker` column, move to the next mission.
- Mission over 1 working day: mark `source-fit unresolved` or `defect`, move on.
- No existing map fits: try the next source in the reuse policy; else mark `source-fit unresolved`. Never design a map.
- ARIA Abort or Blocked: a defect; brief a fix, never tune around it.
- Design question the plan does not answer: choose the option that changes the fewest shared values.
- Disk below 30 GB free: prune your own logs first, then pause Unity runs and record it.
- Tuning: change one lever per iteration (deadline, starting forces, resources, production capacity), at most 5 iterations, within 40% of the template. Re-run every mission that uses a shared value.

## Circuit breakers

Stop everything, write the daily review, wait for the owner:
- Three consecutive working days with zero rows advanced.
- The tree cannot compile, or S002 cannot win with ARIA, after 2 working days of fixes.
- Daily token spend exceeds the budget in `CONTEXT.md` two days in a row.
- Any action outside the standing authorizations would be required (new maps, unit stat changes, purchases, new accounts, deleting outside the program log folder, pushing or merging to `main`).

## Token-saving rules

- Read deltas and the CSV, not the codebase, because the CSV is the compact state.
- Pass paths and grep patterns, never pasted logs.
- Prefix shell commands with `rtk` (use `rtk proxy` when exact output is required), because it filters output cheaply.
- Write each decision once (`owner_queue.md` or the daily review), not in every message.
- Do not create proposal or diagnostic documents, new tools, receipt formats or ledgers, because the plan forbids them.

## End-of-session checklist

1. Update `missions.csv` from harness logs only.
2. Write `Design/Orchestration/daily/YYYY-MM-DD.md`, 40 lines or fewer: rows advanced (count and IDs), stalled rows with blocker, drift findings with evidence, next directives, pending owner gates.
3. Update `Design/Orchestration/state.json`, so the next session sees the cycle is finished.
4. Commit on a `missions/*` branch at least daily and never leave gameplay code uncommitted overnight. Never push or merge to `main`; one pull request per milestone.
