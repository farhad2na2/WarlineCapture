# Operations P4 three-mission vertical slice (D01)

Recorded 2026-09-22; scope clarified after O001 review. Package 4 authors O001–O003 on the abstract Old Quarter model, wires planner-to-loop API win paths, day-report projection and multi-day scaffolding. Regular EN automated Play Mode captures exist. **This establishes a rules/presentation prototype, not normal-input player readiness.** [P4R](O001_PLAYER_READY_IMPLEMENTATION.md) is the next integration package. Device acceptance remains separate from manual Editor playability.

## What landed

| Area | Behavior |
|---|---|
| `OperationsAuthoredMissions` | Data-driven O001 (RECON), O002 (ESCORT), O003 (REPAIR) graphs on `opmap.operations.old_quarter` |
| Clear / Protect | Package verbs in `Game.Operations.Tactical` for O002/O003; Attack issues visible hostile kills |
| `Game.Operations.Content` | EN/FA Operations-owned copy, ARIA planner/input skills, day-report projection, multi-day slice helper |
| Loop launch | Mission-keyed compile prefers authored O001–O003; greybox remains for other catalog offers (for example O011) |

## Exit evidence (coding)

Host marker:

```text
[OperationsP4Validation] result=Passed checks=14
```

Checks cover: compile of all three missions, localized EN/FA keys, scripted and planner-driven loop API win paths for O001–O003, partial/conclude/withdraw, protect defeat, model checkpoint recovery, day report, multi-day O001→EndDay→O002→EndDay→O003, approach fixtures, family district deltas, and Operations ownership. “Manual” helper names in host tests do not establish a human pointer/touch playthrough.

## Current readiness claim

O001–O003 Regular EN have automated Windows Ops shadow Play Mode captures (seeds 1102/1103/1104) and win-screen PNGs under `Design/AgentReports/Operations/host-aria-evidence/`. The earlier “Yes” playable statement referred to this narrower internal capture milestone. The actual player-flow, shipping Watch input, durable profile and device gates remain open. FA visible-input acceptance is also pending. **No player-ready mission is certified by these captures.**

## ARIA evidence gate (next)

See [host-aria-evidence README](../../AgentReports/Operations/host-aria-evidence/README.md). Catalog rows for O001–O003 are **Authored**; the existing `aria_win_acceptance=AriaWon` labels describe the Regular EN loop/capture harness. They must not be treated as fulfillment of the shipping visible-input matrix in [ACCEPTANCE](ACCEPTANCE.md). P4R requires new real-input evidence before that gate closes.

Host marker for the evidence harness (does not claim AriaWon):

```text
[OperationsAriaEvidenceValidation] result=Passed checks=8
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaEvidenceValidation.ps1
```

Play Mode capture **wiring** marker (distinct from live AriaWon; does not claim playable):

```text
[OperationsAriaPlayModeCaptureValidation] result=Passed checks=7
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCaptureValidation.ps1
```

Historical internal capture milestone (2026-09-22): Windows Ops shadow **Play Mode** mid-mission world + win screen for O001–O003 Regular EN (seeds 1102/1103/1104), beyond the host harness alone. The capture uses an Ops-owned URP tactical world and phone-mock HUD/victory presentation; it does not open shipping Watch input or Match scene integration. Live capture invoke **omits `-quit`** so Play Mode can finish; the runner exits the Editor after writing evidence. This procedure is retained for prototype regression, not substituted for P4R's normal-input acceptance.

Mobile-ready host marker:

```text
[OperationsMobileReadyValidation] result=Passed checks=8
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsMobileReadyValidation.ps1
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O001
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O002
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O003
```

Live marker: `[OperationsAriaPlayModeCapture] result=Passed`. Checked-in Regular EN evidence includes O001 URP recapture (`play-02-mid.en.png` world + selection + localized objectives, `win-screen.en.png` victory card) and O002/O003 capture trees. A fresh macOS O001 review at seed 22102 also reached Victory at tick 39; see the [review and evidence limits](../../AgentReports/Operations/O001_READINESS_REVIEW_20260922.md). These results do not close the normal player-flow gate.

## Shipping integration still required

P4 left `SaveDataModel`, `MatchSceneView`, shared asmdefs, shipping localization and Watch input untouched. P4R assigns those required integration changes to the integration owner. D01's real environment is also required; broad Demo 2 imports and Skirmish expansion remain separate scope. Historical content-package boundaries must not be carried forward as a reason to ship a disconnected prototype.

## Programmer 2

Refresh `D:\Projects\WarlineCapture-Operations` onto this branch. Do not open `D:\Projects\WarlineCapture`. Keep Hub signed in.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsP4Validation.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaEvidenceValidation.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsMobileReadyValidation.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCaptureValidation.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O001
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O002
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O003
```

Required wiring markers: `[OperationsP4Validation] result=Passed checks=14`, `[OperationsAriaEvidenceValidation] result=Passed checks=8`, `[OperationsMobileReadyValidation] result=Passed checks=8`, `[OperationsAriaPlayModeCaptureValidation] result=Passed checks=7`. Live capture marker: `[OperationsAriaPlayModeCapture] result=Passed`. Evidence under `Design/AgentReports/Operations/host-aria-evidence/operation.o00{1,2,3}/Regular/{1102,1103,1104}/` (`play-02-mid.en.png`, `win-screen.en.png`, `result.en.json`).
