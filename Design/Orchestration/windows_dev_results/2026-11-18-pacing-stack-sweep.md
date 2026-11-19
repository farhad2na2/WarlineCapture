# Windows dev results — combined pacing sweep on the local stack (2026-11-19, INCOMPLETE: paused)

Branch `missions/win-pacing-stack-sweep`, from integration `b29f5de4f`. Results file only. The Windows dev lead did this task.
- **Build:** local merge `9dfde3121` (`local/w33-pacing-stack`, not pushed) = integration `7de2a3c79` + W31d `c6913f307` + W27b `5d35eb4d9` + W32b `2c86d7acc`.
- **Runs:** NON-counted. `run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, en. Unity 6000.5.8f1 via `InvokeUnity.ps1 -GuiLicensing`, main checkout detached at `9dfde3121`.
- **Logs:** `win-dev-unity\pacing-stack-sweep\` (`menu\unity.log`, `gate3.txt`). Harness evidence moved to `harness-evidence\`, not committed.

**Status: incomplete.** The owner paused the program (lead message, 2026-11-19 21:2x WEST). I let the in-progress S041@130365 run finish, then stopped the verified queue tree (python 34520 → InvokeUnity 11600 → Unity 6140 and its import workers). Hub and the licensing client were not touched.
- **Done:** 23 of 29 jobs.
- **Not run:** S042@130365 and all five FC runs (S009, S011, S031, S033, S035 @130365). gate3.txt shows S042@130365 as INCOMPLETE; that is this stop, not a game result.

## Results (game-side `[SkirmishTerminalObjective]`; every run Victory, lost=0)

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
| S042 BT comb. est. | 279.0 | 51.66 | not run | — | PASS 1/1 run |

- CE deadline 525 s, BT deadline 540 s (both as logged by the game).
- Every finished run: `unused_starting=-`, `roster=match`.

## Proposed per-family deadlines (data only; median near 40 %)
- **DB CE: 525 s (no change).** Median of 12 runs 39.7 % (≈208 s). The family splits: field rows ≈34 %, established rows 44.5–50.5 %.
- **CC BT: 670 s.** Median of 11 runs 49.56 % (≈268 s); 268 / 0.40 ≈ 670. At 670 s the 11 runs would land at 30.3–41.6 %.
- **FC: no proposal.** None of the five FC runs ran before the pause.

## Open
- Resume: S042@130365 and the five FC runs, then the FC proposal.
- W16 (diagnosis done in this session, no code yet), W34, W35, W33 not started.
