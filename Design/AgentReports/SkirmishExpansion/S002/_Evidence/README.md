# S002 Game View evidence

Programmer 1 fills this folder from the Windows Skirmish shadow after a live
Regular Standard match. The focused Editor suites must not write Playable.

Required files for the guarded publication flip:

| File | Source |
|---|---|
| `s002-regular-standard-104731-playing.png` | Primary dump is repo-root `_Evidence/`; this folder gets a copy |
| `s002-regular-standard-104731-gameview.json` | Written by the same probe (seed/definition/phase; `forcedVictory=0`) |

The probe queues expanded S002 Regular Standard seed `104731`, enters the
match, dumps Game View, and **stays in Play Mode** so a human can finish a
normal-speed game. It does not inject an army or force Victory.

ARIA matrix traces are separate. `Tools/Warline/Skirmish/Launch S002 ARIA Watch …`
writes `s002-aria-live-{seed}-{en|fa}.jsonl` while the match runs, then copies
a finished or aborted trace to `s002-aria-rs-{seed}-{en|fa}-{n}.jsonl` plus
`s002-aria-rs-…-log.txt`. Live samples include `elapsed`, `simulationActive`,
and `clockElapsed`. A stuck-zero clock after Playing must Abort with
`simulationNotAdvancing` within ~45s rather than waiting for the 1500s wall
budget. Those files appear only after a live Editor run. Do not commit a
Victory row ahead of that run. `runs.csv` stays header-only in git until
Programmer 1 appends a real terminal outcome.

When the PNGs exist and the compiler hashes still match, Programmer 1 may run
`Tools/Warline/Skirmish/Flip S002 Playable If Evidence Ready`. That menu is
the only writer. `EvaluateS002PlayableFlip` / focused tests never persist
Playable.
