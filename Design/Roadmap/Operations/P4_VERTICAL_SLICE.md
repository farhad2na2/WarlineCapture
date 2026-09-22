# Operations P4 three-mission vertical slice (D01)

Recorded 2026-09-22. Package 4 authors O001–O003 on Old Quarter, wires ARIA visible-control win paths, day-report projection, and multi-day scaffolding. It does not claim playable/complete without verified ARIA win evidence (Programmer 2 / QA later). Device baseline is not required for a playable claim per Farhad.

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

Checks cover: compile of all three missions, localized EN/FA keys, manual win paths for O001–O003, ARIA visible-control win wiring, partial/conclude/withdraw, protect defeat, checkpoint recovery, day report, multi-day O001→EndDay→O002→EndDay→O003, two approaches, family district deltas, and Operations ownership.

## Playable claim

**Not claimed.** ARIA win paths are wired and host-driven. Programmer 2 / QA must record unassisted ARIA wins before any playable/complete status.

## ARIA evidence gate (next)

See [host-aria-evidence README](../../AgentReports/Operations/host-aria-evidence/README.md). Catalog rows for O001–O003 are **Authored**; `aria_win_acceptance` remains **Pending**.

Host marker for the evidence harness (does not claim AriaWon):

```text
[OperationsAriaEvidenceValidation] result=Passed checks=8
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaEvidenceValidation.ps1
```

## Game PM seams still closed

`SaveDataModel`, `MatchSceneView`, shared asmdefs, shipping localization catalog, Demo 2 scene import, SkirmishExpansion. Shipping Watch virtual-touch (`AriaPlayCapability` Operations row) needs an explicit Game PM seam before it can replace the Operations loop visible-control evidence path.

## Programmer 2

Refresh `D:\Projects\WarlineCapture-Operations` onto this branch. Do not open `D:\Projects\WarlineCapture`.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsP4Validation.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaEvidenceValidation.ps1
```

Required log markers: `[OperationsP4Validation] result=Passed checks=14` and `[OperationsAriaEvidenceValidation] result=Passed checks=8`. Then run Play Mode / Watch capture for O001–O003 Regular EN (seeds 1102/1103/1104) before flipping AriaWon.
