# Lane worker 44, 2026-11-19: first Mac runs of City Crossroads Convoy Escort S043-S048

HEAD 589e7a4c5 at start (combat-VFX merge), branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-19/lane44/`.
No asset or code changes. disk_guard 0 before each launch; one Editor at a time (main checkout); Fig/Fig-SquadPlay untouched; no Editor stopped.
The stale working-tree files from eaa64700c were left alone; this commit stages explicit paths and a STATUS blob built from HEAD.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 193 s (recompile); python 159 OK | compile1.log:1753; Library Redirect Path :93; python.txt:33-35 |
| 2 queue, S043-S048 130365 en (6 jobs, one launch) | 3/6 Victory, 3/6 Abort ariaStopped:Blocked; Completed wins=3 sourceUnchanged=1 exitCode=0 | q/unity.log:82636 |
| 4 gate 3 | 6/6 gate3=FAIL (unused starting roles) | gate3.txt:1-6 |

Flags: `--menu-journey --validation-library --no-table --timeout 20000`; validation library enabled=1 (:601). No "First-visit ARIA seed" rejection, so no re-runs.
Queue ran 13:34-14:03. New source hash d1b145af20836510a37ddfca0deb208a1cbba4211d5941ea39c1339da171092e.

## Per job (q/unity.log, deadline 1080 s)

| Row | Result | Clock (deadline %) | Delivered / lost | Menu | Return probe | AriaBlocked | First failing line |
|---|---|---|---|---|---|---|---|
| S043 | Abort ariaStopped:Blocked :31157 | 206.5 s (19.12) :31143 | 0/2, 0 | Passed :2120 (TheaterPin_CC>ObjectiveTab_CE) | none (abort) | 101 Waiting ObserveObjective :31096, control=unknown :31112 | :30928 frame_missing t=186.4 after group 15 Move/Hold oscillation on ServiceOriginalTruck :29536-30675 |
| S044 | Victory ConvoyDelivered | 180.0 s (16.66) :42804 | 2/2, 0 | Passed :31983 | Passed MAIN MENU :43114 | 0 | - |
| S045 | Abort ariaStopped:Blocked :54638 | 226.8 s (21.00) :54624 | 1/2, 1 | Passed :43791 | none (abort) | 101 Waiting ObserveObjective :54577, control=PreviousSquadPage :54593 | :52770 truck lost (lost=1) t=102.0 |
| S046 | Victory ConvoyDelivered | 162.0 s (15.00) :65500 | 2/2, 0 | Passed :55391 | Passed MAIN MENU :65754 | 0 | - |
| S047 | Abort ariaStopped:Blocked :75137 | 278.2 s (25.76) :75123 | 0/2, 1 | Passed :66509 | none (abort) | 101 Waiting ObserveObjective :75076, control=Hold :75092 | :73548 truck lost (lost=1) t=93.9; then Move/Hold oscillation :74753-74969 |
| S048 | Victory ConvoyDelivered | 135.2 s (12.52) :82086 | 2/2, 0 | Passed :75859 | Passed MAIN MENU :82358 | 0 | - |

## Findings

- The three aborts are the same rows and the same block Windows W19 saw: reason 101, kind Waiting, intent ObserveObjective, after the objective frame is lost (frame_missing, frame_lost, then AriaBlocked).
- S045 and S047 each lose a truck early (t=102 and t=94). S043 loses none but never delivers: group 15 alternates Move/Hold on ServiceOriginalTruck every ~20 s.
- Wins at 12.52-16.66% of 1080 s: below the 20% floor and the owner's 25-50% target (W31b deadline work is still pending).
- Gate 3 fails on all six: roster=match, but starting roles go unused (gate3.txt).
- Whole log: 0 [StartingGrantPlacement] failed, 0 startupFailure, 0 BlockedSpawn, 0 order_retry/order_blocked, 0 physicalTakeover, 0 error CS; [CanonicalAircraftGeometry] result=Failed stale stamp 6x (Codex 040, did not block play).

runs.csv rows appended under `<ID>/_Evidence/candidates/d1b145af.../runs.csv` for S043-S048 (committed; trace folders left untracked).
