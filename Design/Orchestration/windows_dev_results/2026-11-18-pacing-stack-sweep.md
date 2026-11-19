# Windows dev results — W36 combined pacing sweep on the local stack (2026-11-19)

Branch `missions/win-pacing-stack-sweep`, from integration `b29f5de4f`. Results file only. The Windows dev lead did this task.
- **Build:** local merge `9dfde3121` (`local/w33-pacing-stack`, not pushed) = integration `7de2a3c79` + W31d `c6913f307` + W27b `5d35eb4d9` + W32b `2c86d7acc`. The same content is now on integration `c11d6172a`.
- **Runs:** NON-counted. `run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, en. Unity 6000.5.8f1 via `InvokeUnity.ps1 -GuiLicensing`, main checkout detached at `9dfde3121`.
- **Logs:** `win-dev-unity\pacing-stack-sweep\` (23 runs) and `pacing-stack-sweep-resume\` (6 runs). Each has `menu\unity.log`, `terminal.txt`, `gate3.txt`. Harness evidence moved to `harness-evidence\`, not committed.

**Status: complete, 29/29 runs.** 28 Victory, 1 Defeat (S011, tickets). Gate 3 PASS on every run.

## Pause and resume
- **Pause.** The owner paused the program after 23 runs. I let S041@130365 finish, then stopped the verified queue tree (python → InvokeUnity → Unity and its import workers). Hub and the licensing client were not touched.
- **First relaunch: failed, not counted.** It stalled at startup on Unity's "Recovering Scene Backups" dialog, left by the hard stop. I stopped that verified tree and moved `Temp\__Backupscenes` (a 0.5 MB harness scene backup) to `pacing-stack-sweep\scene-backup-after-stop\`. That attempt's logs are kept as `pacing-stack-sweep-resume-stuck-dialog\`.
- **Second relaunch:** ran the 6 remaining jobs cleanly.

## Results (game-side `[SkirmishTerminalObjective]`)

| Row | 155923 s | 155923 % | 130365 s | 130365 % | gate 3 |
|---|---|---|---|---|---|
| S019 CE ground field | 179.2 | 34.13 | 179.2 | 34.12 | PASS 2/2 |
| S020 CE ground est. | 233.7 | 44.52 | 241.9 | 46.07 | PASS 2/2 |
| S021 CE air field | 114.2 | 21.74 | 183.2 | 34.89 | PASS 2/2 |
| S022 CE air est. | 256.9 | 48.94 | 265.1 | 50.50 | PASS 2/2 |
| S023 CE comb. field | 177.9 | 33.89 | 179.7 | 34.23 | PASS 2/2 |
| S024 CE comb. est. | 247.2 | 47.08 | 254.7 | 48.51 | PASS 2/2 |
| S037 BT ground field | 267.7 | 49.58 | 256.8 | 47.56 | PASS 2/2 |
| S038 BT ground est. | 202.8 | 37.55 | 209.6 | 38.81 | PASS 2/2 |
| S039 BT air field | 260.9 | 48.32 | 268.8 | 49.78 | PASS 2/2 |
| S040 BT air est. | 267.6 | 49.56 | 272.7 | 50.49 | PASS 2/2 |
| S041 BT comb. field | 252.5 | 46.76 | 269.3 | 49.87 | PASS 2/2 |
| S042 BT comb. est. | 279.0 | 51.66 | 264.9 | 49.06 | PASS 2/2 |

- CE deadline 525 s, BT deadline 540 s (both as logged by the game). All 24 runs Victory, lost=0.

| FC row @130365 | Result | s | % of 1200 | zones own/opp | tickets | gate 3 |
|---|---|---|---|---|---|---|
| S009 DB FC air field | Victory | 640.9 | 53.41 | 2/1 | 467/0 | PASS |
| S011 DB FC comb. field | **Defeat** | 760.5 | 63.38 | 1/2 | 0/356 | PASS |
| S031 CC FC ground field | Victory | 675.8 | 56.32 | 2/1 | 444/0 | PASS |
| S033 CC FC air field | Victory | 760.6 | 63.38 | 2/1 | 341/0 | PASS |
| S035 CC FC comb. field | Victory | 660.3 | 55.03 | 2/1 | 441/0 | PASS |

- Every run: `unused_starting=-`, `roster=match`.
- All FC results end on `TicketsDepleted`. S011's loss is on tickets: the enemy held two zones. A longer deadline would not change it.

## Per-family deadlines (median near 40 %)
- **DB CE: 525 s, no change.** Median of 12 runs 39.7 % (≈208 s). Field rows ≈34 %, established rows 44.5–50.5 %.
- **CC BT: 670 s** (War 931, LargeWar 1117). Median of 12 runs 49.3 % (≈266 s). At 670 s the 12 runs land at 30.3–41.6 %.
- **FC: 1690 s** (War 2113, LargeWar 2535). Median of 5 runs 56.3 % (676 s). At 1690 s they land at 37.9–45.0 %.
  - The FC deadline is only the `TicketsAtDeadline` timeout. Each side has 500 tickets, drained at 1 ticket/s while the other holds two zones, so FC cannot end in under about 500 s.
  - Applied to DB FC S007–S012 and CC FC S031–S036 only, the families measured.
  - MP FC S055–S060 stays at 1200 for W16. S079+ have no drafts.
- The data branch is `missions/win-w36-bt-670` on integration `c11d6172a`; its own results file covers it.
