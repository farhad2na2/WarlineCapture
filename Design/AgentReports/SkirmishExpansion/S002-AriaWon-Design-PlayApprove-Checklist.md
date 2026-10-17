# S002 AriaWon — Design play / approve

Design gate for Skirmish mission ordinal **4** after Programmer 1 checks in a finished Regular English ARIA win. Same shape as the Operations O001–O003 gate under `Design/AgentReports/Operations/`: an AriaWon evidence row, a Play Mode win screen / result card, then a Design decision of `APPROVE` or `changes-requested`. This file does not record a win.

Live evidence stays with Programmer 1 on the Windows Skirmish shadow `D:\Projects\WarlineCapture-Skirmish`. Leave `D:\Projects\WarlineCapture` closed. Drafted by Programmer 2 / Game PM (Cursor Auto).

Farhad bar: playable / complete needs a verified ARIA win. A Game View flip while the match is still Playing does not pass.

## Tip to play

- [ ] Write `tip_played` as the full commit named on the landed AriaWon row (`code_hash` or the report header). Play that tip on the Skirmish shadow.
- [ ] Leave this checklist commit off the play tip.
- [ ] The Game View publication flip (`7f0e1def7`, publication tip `2b5f0f6e9`) is an earlier gate. It is not the AriaWon tip.
- [ ] Open harness branches (#35, #43, #44, #45) wire fail-fast capture. They do not stamp Victory and are not this play tip.

`tip_played`:

## Mission and seed

| Field | Value |
|---|---|
| Catalog | `S002` |
| Ordinal | 4 |
| Identity | Desert Base · Base Assault · Ground Maneuver · Established Base |
| Definition | `skirmish.s002` |
| First visit | Regular / Standard |
| Seed | `104731` |
| Locale | `en` only |

Win condition: the original enemy Barracks is destroyed and the original player Barracks is still standing. Both original bases dead on the same tick, or both still standing at the deadline, is a Draw and fails this gate. A replacement Barracks is not the designated base.

## Regular English only

- [ ] The evidence row locale is `en`.
- [ ] The result card shows the English victory line: "Enemy main base destroyed." (`skirmish.s002.result.victory`).
- [ ] Farsi is out of this gate unless Farhad reopens it.

## Where the AriaWon row lives

Operations pattern this gate mirrors (leave those files in place):

```text
Design/AgentReports/Operations/host-aria-evidence/<mission>/Regular/<seed>/result.en.json
Design/AgentReports/Operations/host-aria-evidence/<mission>/Regular/<seed>/win-screen.en.png
```

A passing Ops row has `status=AriaWon`, `victory=true`, `language=en`, and `capture_path` on `win-screen.en.png`. Mobile-ready result-card shots sit under `Design/AgentReports/Operations/mobile-ready-o001-o003/_Evidence/`.

S002 rows, once Programmer 1 lands them:

- `Design/AgentReports/SkirmishExpansion/S002/runs.csv` — one finished row. Id shape `S002-aria-rs-104731-en-1` (the next free id if that slot is already used).
- `Design/AgentReports/SkirmishExpansion/S002/_Evidence/<run-id>.jsonl`
- `Design/AgentReports/SkirmishExpansion/S002/_Evidence/<run-id>-log.txt`

Already under `_Evidence/`, and not an AriaWon row: `s002-regular-standard-104731-playing.png` and `s002-regular-standard-104731-gameview.json` (`phase=Playing`, `outcome=None`, `forcedVictory=0`).

## Play Mode checks

Play at `tip_played` with Game View focused. The screen Design signs is the finished win / result card.

- [ ] Regular, Standard, seed `104731`, locale `en`, executor `aria`, normal speed.
- [ ] The match clock advances to a finished outcome. Stuck elapsed, Abort, timeout, or `simulationNotAdvancing` fails the gate.
- [ ] The win screen / result card is visible and reads "Enemy main base destroyed."
- [ ] `runs.csv` for that attempt: `result=Victory`, designated-base `end_reason`, `executor=aria`, `locale=en`, `seed=104731`, `normal_speed=1`, `input_violations=0`, `human_interventions=0`.
- [ ] The trace and log named on that row exist. `forcedVictory` is `0`. No injected army and no victory stamped from a reducer.
- [ ] A Playing-phase Game View PNG, with no result card, is `changes-requested`.

## Outcome

Fill one decision. `APPROVE` only when the Play Mode win screen and the Regular EN evidence row agree. Anything short of that is `changes-requested`.

| Field | Value |
|---|---|
| `design` | `APPROVE` or `changes-requested` |
| `tip_played` | |
| `evidence_row` | `S002/runs.csv` row plus the `_Evidence` trace |
| `win_screen` | path of the Play Mode win / result card capture |
| `notes` | |

`APPROVE` covers this one Regular EN AriaWon. It does not close the rest of the matrix in `S002/acceptance.md`, device review, or publication Accepted.

## Out of scope

- Farsi AriaWon (`fa-IR`), unless Farhad reopens it.
- Watch virtual-touch and the MatchSceneView seam, unless Farhad reopens them.
- Combined Arms Field is catalog S005. Do not attach S003, S004, S005, or any later mission’s files to this gate, and do not invent those rows.
- Seeds `130365` and `155923`, Recruit / Veteran / Commander, and War / Large War stay on the later matrix.
