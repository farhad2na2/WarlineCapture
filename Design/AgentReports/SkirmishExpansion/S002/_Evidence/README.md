# S002 Game View evidence

Programmer 1 fills this folder from the Windows Skirmish shadow after a live
Regular Standard match. The focused Editor suites must not write Playable.

Required files for the guarded publication flip:

| File | Source |
|---|---|
| `s002-regular-standard-104731-playing.png` | `Tools/Warline/Skirmish/Launch S002 Regular Standard Game View` |
| `s002-regular-standard-104731-gameview.json` | Written by the same probe (seed/definition/phase; `forcedVictory=0`) |

The probe queues expanded S002 Regular Standard seed `104731`, enters the
match, dumps Game View, and **stays in Play Mode** so a human can finish a
normal-speed game. It does not inject an army or force Victory.

When the PNGs exist and the compiler hashes still match, Programmer 1 may run
`Tools/Warline/Skirmish/Flip S002 Playable If Evidence Ready`. That menu is
the only writer. `EvaluateS002PlayableFlip` / focused tests never persist
Playable.
