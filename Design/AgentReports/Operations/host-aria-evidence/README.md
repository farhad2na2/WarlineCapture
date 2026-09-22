# Operations ARIA evidence scaffolding (O001–O003)

Recorded with the Operations ARIA evidence harness gate after P4 coding (`28b955f2` / PR #36) and the Play Mode capture runner (PR #38 / PR #39, plus the URP O001 recapture on this branch). **Playable Operations mission ready is not claimed.** Catalog `aria_win_acceptance` is AriaWon for O001–O003 Regular EN; Programmer 2 still spot-checks the O001 URP PNGs on the Windows Ops shadow.

## Gap verified

P4 host marker `[OperationsP4Validation] result=Passed checks=14` proves coding wiring only. Host evidence marker `[OperationsAriaEvidenceValidation] result=Passed checks=8` proves planner-driven loop wins and schema scaffolding only — **not** Farhad’s Play Mode win-screen bar.

Shipping Watch virtual-touch has **no** Operations row this sprint; do not open Match scene / Watch shared-UI seams. The Operations-owned capture path is:

1. Public loop observation (`TryReadHud`, node TargetIds, `CopyPublicActors` / facts)
2. Generic `OperationsAriaObjectivePlanner` (no mission-ID switch)
3. Play Mode enter → planner ARIA through Loop visible-controls → **Ops-owned URP tactical world + phone-mock HUD / victory card** (`Game.Operations.Capture`, runtime assembly so Play Mode `AddComponent` works) → `ScreenCapture` PNG + `result.en.json`

Mobile-ready presentation (URP world mesh, localized objectives, compressed scan/repair/hold pacing with hard deadlines 720/840/900 preserved) is validated by:

```bash
python3 Tools/Operations/check_mobile_ready.py
```

Marker: `[OperationsMobileReadyValidation] result=Passed checks=8`

## Folder layout (ACCEPTANCE)

```text
Design/AgentReports/Operations/<build>/<mission>/<difficulty>/<seed>/
```

This scaffold uses build id `host-aria-evidence`.

| Mission | Canonical Regular seed | EN scaffold | FA scaffold seed |
|---|---|---|---|
| O001 | 1102 | `operation.o001/Regular/1102/result.en.json` | `9102` |
| O002 | 1103 | `operation.o002/Regular/1103/result.en.json` | `9103` |
| O003 | 1104 | `operation.o003/Regular/1104/result.en.json` | `9104` |

Regular EN `result.en.json` files are recorded **AriaWon** captures (`victory: true`, `capture_path` at `win-screen.en.png`). O001 Regular/1102 is the URP world-shell recapture. O002 Regular/1103 and O003 Regular/1104 are the PR #39 captures. FA seeds `9102–9104` stay **PendingAriaWon** scaffolds. Catalog `aria_win_acceptance` is AriaWon for these three rows; **playable is not claimed** by this tree.

Expected after a successful capture (per mission):

```text
.../Regular/<seed>/play-01-start.en.png
.../Regular/<seed>/play-02-mid.en.png
.../Regular/<seed>/play-03-terminal.en.png
.../Regular/<seed>/win-screen.en.png
.../Regular/<seed>/result.en.json   # victory=true, status=AriaWon
```

## Host proof (Linux / no Unity)

Evidence harness (loop wins, not Play Mode):

```bash
python3 Tools/Operations/check_aria_evidence.py
```

Marker: `[OperationsAriaEvidenceValidation] result=Passed checks=8`

Play Mode capture **wiring only** (does not claim AriaWon):

```bash
python3 Tools/Operations/check_aria_playmode_capture.py
```

Marker: `[OperationsAriaPlayModeCaptureValidation] result=Passed checks=7`

## Windows shadow procedure (Programmer 2)

Use **only** `D:\Projects\WarlineCapture-Operations`. Never open `D:\Projects\WarlineCapture`.

1. Refresh the Operations shadow worktree onto this branch tip.
2. Keep Unity Hub open and signed in.
3. Optional coding / harness refresh:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsP4Validation.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaEvidenceValidation.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCaptureValidation.ps1
```

Markers:

- `[OperationsP4Validation] result=Passed checks=14`
- `[OperationsAriaEvidenceValidation] result=Passed checks=8`
- `[OperationsAriaPlayModeCaptureValidation] result=Passed checks=7`

4. **Farhad bar — Play Mode win screen capture** (O001, then O002, then O003).

   The live capture invoke **omits Unity `-quit`**. `EnterPlaymode` is async; the runner logs `[OperationsAriaPlayModeCapture] result=Passed` (or Failed) and then calls `EditorApplication.Exit`. Do not route this through the sync executeMethod helper that always adds `-quit`. The invoke treats pass marker + `win-screen.en.png` + `result.en.json` as success even if InvokeUnity reports a null exit code under GuiLicensing.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O001
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O002
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O003
```

Live capture marker (per mission): `[OperationsAriaPlayModeCapture] result=Passed`

5. Verify each `win-screen.en.png` shows the Ops victory UI and `result.en.json` has `victory: true` and `status: AriaWon`. Catalog `aria_win_acceptance` for O001–O003 Regular EN is already AriaWon. Device baseline is **not** required for the playable claim (Farhad). Watch seam stays closed. **Playable is not claimed by this merge.**

## Status

| Mission | Authored | Host unassisted path | Play Mode capture runner | AriaWon | Playable claim |
|---|---|---|---|---|---|
| O001 | Yes | Runnable via harness | Wired; URP recapture checked in at Regular/1102 | Recorded (URP world + phone HUD) | Not claimed |
| O002 | Yes | Runnable via harness | Wired; PR #39 capture checked in at Regular/1103 | Recorded | Not claimed |
| O003 | Yes | Runnable via harness | Wired; PR #39 capture checked in at Regular/1104 | Recorded | Not claimed |
