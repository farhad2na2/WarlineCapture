# S005 Game View evidence

Programmer 1 fills this folder from the Windows Skirmish shadow after a live
Regular Standard match. The focused Editor suite must not write Playable and
must not stamp Victory.

Playing PNG for the first-visit English cell is **TBD**. `s005-regular-standard-104734-playing.png`
is absent on this tip and was also absent on PR #31 tip `b6ee0b86`. Do not
invent that PNG, a Game View sidecar, or a Victory row. Scenario assets for
`skirmish.s005` live on PR #31 and are not copied here.

The watch queues catalog **S005** / Regular / Standard / seed `104734` through
the shared driver once the in-memory definition compiles. It does not inject
an army or force Victory.

ARIA matrix traces are separate from any later Game View flip. They are not a
mission-complete claim and they do not stamp Victory on their own.
`Tools/Warline/Skirmish/Launch S005 ARIA Watch …` writes
`s005-aria-live-{seed}-{en|fa}.jsonl` while the match runs, then copies a
finished or aborted trace to `s005-aria-rs-{seed}-{en|fa}-{n}.jsonl` plus
`s005-aria-rs-…-log.txt`. Live samples include `elapsed`, `simulationActive`,
and `clockElapsed`. A stuck-zero clock after Playing must Abort with
`simulationNotAdvancing` within about 45 seconds rather than waiting for the
1500 second wall budget. Those files appear only after a live Editor run.
Do not commit a Victory row ahead of that run. `runs.csv` stays header-only
in git until a real terminal outcome is appended. Live AriaWon for S005 waits
until the Skirmish Editor lock is free. This folder does not mark the mission
or the AriaWon matrix complete.
