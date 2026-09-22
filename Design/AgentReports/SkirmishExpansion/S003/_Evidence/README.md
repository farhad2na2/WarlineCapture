# S003 Game View evidence

Programmer 1 fills this folder from the Windows Skirmish shadow after a live
Regular Standard match. The focused Editor suites must not write Playable.

Required files for the guarded publication flip:

| File | Source |
|---|---|
| `s003-regular-standard-104732-playing.png` | Primary dump is repo-root `_Evidence/`; this folder gets a copy |
| `s003-regular-standard-104732-gameview.json` | Written by the same probe (seed/definition/phase; `forcedVictory=0`) |

The probe queues expanded S003 Regular Standard seed `104732`, enters the
match, dumps Game View, and **stays in Play Mode** so a human can finish a
normal-speed game. It does not inject an army or force Victory.

When the PNGs exist and the compiler hashes still match, Programmer 1 may run
`Tools/Warline/Skirmish/Flip S003 Playable If Evidence Ready`. That menu is
the only writer. `RunFocusedFlipS003DryRun` never persists Playable, and the
write does not change the S002 row.

ARIA matrix traces are separate from the Game View flip. They are not a
mission-complete claim and they do not stamp Victory on their own.
`Tools/Warline/Skirmish/Launch S003 ARIA Watch …` writes
`s003-aria-live-{seed}-{en|fa}.jsonl` while the match runs, then copies a
finished or aborted trace to `s003-aria-rs-{seed}-{en|fa}-{n}.jsonl` plus
`s003-aria-rs-…-log.txt`. Live samples include `elapsed`, `simulationActive`,
and `clockElapsed`. A stuck-zero clock after Playing must Abort with
`simulationNotAdvancing` within about 45 seconds rather than waiting for the
1500 second wall budget. Those files appear only after a live Editor run.
Do not commit a Victory row ahead of that run. `runs.csv` stays header-only
in git until a real terminal outcome is appended. Live AriaWon for S003 waits
until after the S002 Windows proof and until the Skirmish Editor lock is free.
