# Lane integration 34 — 2026-11-18

Brief: `WarlineCapture-program-logs/briefs/lane-integration34-2026-11-18.md`. Logs: `WarlineCapture-program-logs/2026-11-18/lane34/` (L).
HEAD at start 7970b1c54 (W11+W12 merge 1772a51b4, W14 merge 94ec16fee). Wrapper only, GUI licensing, one Editor at a time,
disk_guard 0 before each launch, startup `Library Redirect Path` seen within 3 min on every launch. No asset or code changes.

## Step 1 — compile and Python
- Compile: exit 0, `L/compile1.log:1069` `[SkirmishNativeRenderingDiagnosticSerialization] result=Passed`, 0 `error CS`, Editor 80 s.
- Python: `Ran 150 tests`, `OK` (`L/python.txt:33-35`).

## Step 2 — counted S002 (W12 under test)
Command as briefed plus `--no-table` (the brief forbids editing missions.csv; the queue writes it otherwise). `L/q-s002.out.txt`, `L/s002/unity.log`.

| job | result | clock | deadline | menu journey | probe |
|---|---|---|---|---|---|
| 130365 en | Victory | 231.8 s | 21.46% (:358334) | Passed :2006 | Passed :358574 |
| 155923 en | Victory | 226.7 s | 20.99% (:691808) | Passed :359354 | Passed :692048 |
| 104731 en | Victory | 229.5 s | 21.25% (:918043) | Passed :692861 | Passed :918283 |
| 130365 fa-IR | Victory | 237.9 s | 22.02% (:1058134) | Passed :919018 | Passed :1058386 |

- `[SkirmishAriaQueue] result=Completed jobs=4 recorded=4 wins=4 sourceUnchanged=1` (:1058634).
- 0 `[AriaBlocked]`/`[AriaBlockedControl]`, so no reason 114. 0 `[StartingGrantPlacement] failed`, 0 startupFailure.
- **W12 was not exercised.** Each match had one base alert, late and with no damage: baseHealth=800 at t=220.9/224.5/222.3/223.5
  (:357929, :691644, :917754, :1057674). Every match ended with playerBaseHp=800. There was no second alert, so the stage-5
  re-plan path never ran. (Lane 31's run on the same seed had alerts at t=61.8 and 238.5. In this tree ARIA wins before the second one.)
- Home defense: job 1 triggered at stage 2 (:358002), then ordered group 0 at stage 5 with attackMode=True and selectedMask=1 (:358157).
  Jobs 2-4 triggered at stage 1 with recovery=4/4/32 (:691716, :917828, :1057691). Each match ended 5-14 s after its alert.
  No home-defense action waited more than 60 s. The only `kind=Waiting` line is the ARIA input stop at match end (:358301).
- Gate-3 unused: none on the en jobs; apc_fast, breacher, marksman and radar on fa-IR.
- runs.csv: `S002/_Evidence/candidates/98606b5b.../runs.csv`, 4 rows, committed. The candidate evidence folder (~22 MB) is untracked.

## Step 3 — CE established menu launches (W14)
`L/q-ce.out.txt`, `L/ce/unity.log`; `--validation-library` (:604 enabled=1) and `--no-table`.

| row | result | clock | deadline | menu journey | probe |
|---|---|---|---|---|---|
| S020 | Victory ConvoyDelivered 2/2 lost 0 | 143.5 s | 13.29% (:6391) | Passed :2034 | Passed :6645 |
| S022 | Victory ConvoyDelivered 2/2 lost 0 | 178.2 s | 16.50% (:12766) | Passed :7324 | Passed :13151 |
| S024 | Victory ConvoyDelivered 2/2 lost 0 | 287.2 s | 26.60% (:19371) | Passed :13828 | Passed :19697 |

- `[SkirmishAriaQueue] result=Completed jobs=3 recorded=3 wins=3 sourceUnchanged=1` (:19953).
- Whole log: 0 BlockedSpawn, 0 `[SkirmishStartingForce] failed`, 0 startupFailure, 0 `[StartingGrantPlacement] failed`, 0 `[AriaBlocked]`.
- The harness appended runs.csv rows with countedWin=1 under `S020|S022|S024/_Evidence/candidates/98606b5b.../runs.csv`; the files are committed.
  Whether those rows count is the lead's decision. The rows stay planned.

## Open items
- W12 is still not proven natively. A run that reaches a second, damaging base alert is needed (for example a longer or harder seed, or the
  lane-31 trace conditions). The four counted wins do not show the re-plan.
- missions.csv was not touched. The lead records S002 and CE.
