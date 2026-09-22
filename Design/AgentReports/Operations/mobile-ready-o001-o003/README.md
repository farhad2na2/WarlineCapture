# Operations O001–O003 mobile-ready (Programmer 1)

Branch: `cursor/ops-o001-o003-mobile-ready-e190`, rebased/merged onto `main` after PR #41 (`4acfb615b`).

## Scope landed (this PR)

| Item | Status | Owner layer |
|---|---|---|
| 4. O001 onboarding coach Scan → Evidence → Extract; ≤3s input unblock | **Implemented** | `OperationsOnboardingCoach` (Content) |
| 5. Fat-thumb escort/repair Go / Hold / route chips; clinic/pump warnings | **Implemented** | `OperationsEscortRepairControls` (Content) |
| 6. Result screen outcome + Trust / Intel / Heat + Continue | **Implemented** | `OperationsMissionResultProjection` (Content) |
| 7. Teach Partial (Conclude stakes; Withdraw separate) | **Implemented** | `OperationsPartialTeach` + `ConcludeAvailable` |
| 8. Trim O003 long hold + light defend beat | **Implemented** | `OperationsAuthoredMissions` O003 v2 (hold 60→20, wave group 2) |
| 9. Practice one-tap from fail/result | **Implemented** | `TryContinueAndPractice` |

## Protected

- Teach order O001 → O002 → O003 unchanged (multi-day still EndDay gated).
- AriaWon play paths still wired via `OperationsAriaInputSkills` (O003 re-verified in mobile-ready checks).
- Zero-credit mandatory on Partial/Withdraw covered in checks.
- Partial / Withdraw semantics unchanged (read-only `IsPartialPredicateSatisfied` expose).
- D01 graphs: O001/O002 untouched; O003 hold/wave trim only (content hash → `ops-authored-o003-v2`). Re-capture AriaWon O003 evidence after merge.

## Shell binding (landed P2 shell, no second void UI)

Content frames bind inside `OperationsAriaPlayModePresentation` (the landed URP world + phone-mock shell):

| Frame | Bind loci |
|---|---|
| `OperationsCoachFrame` | O001 coach card on the phone panel (Scan / Evidence / Extract) |
| `OperationsEscortRepairControlFrame` | Fat-thumb chip bar + clinic/pump warning banner |
| `OperationsMissionResultUiFrame` | `ShowMissionResult`: outcome + Trust / Intel / Heat + Continue |
| `OperationsPartialTeachFrame` | Content contract only (Conclude vs Withdraw). Not a second HUD. |

## New Ops-owned EN/FA keys (for Game Design)

Not added to `V3UiLocalizationCatalog`. Living in `OperationsLocalizedCopy`:

- `operations.coach.o001.scan|evidence|extract|done.title/body`
- `operations.controls.escort.go|hold`, `operations.controls.route.main|safe`, `operations.controls.repair`, `operations.controls.defend.hold`
- `operations.warning.clinic|pumps`, `operations.warning.severity.watch|critical`
- `operations.result.continue|practice`, `operations.result.delta.trust|intel|heat|generic`
- `operations.teach.partial.conclude.title`, `operations.teach.partial.withdraw.title`

## Game View capture (Windows Ops shadow)

Refresh `D:\Projects\WarlineCapture-Operations` onto this branch. Do not open `D:\Projects\WarlineCapture`. No Mac.

PNGs land in `Design/AgentReports/Operations/mobile-ready-o001-o003/_Evidence/`.

```powershell
# Host (no Unity)
python Tools/Operations/check_mobile_ready.py

# Editor validation — marker checks=8
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsMobileReadyValidation.ps1

# Game View capture (omits Unity -quit; runner calls EditorApplication.Exit)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsMobileReadyCapture.ps1 -Shot All
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsMobileReadyCapture.ps1 -Shot Coach
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsMobileReadyCapture.ps1 -Shot Escort
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsMobileReadyCapture.ps1 -Shot Result
```

| Shot | Menu | executeMethod | PNG |
|---|---|---|---|
| O001 coach Scan / Evidence / Extract | `Operations/Mobile Ready/Capture O001 Coach` | `Game.Tests.Editor.Operations.OperationsMobileReadyPlayModeCapture.RunO001Coach` | `o001-coach-scan.en.png`, `o001-coach-evidence.en.png`, `o001-coach-extract.en.png` |
| O002 escort chips + clinic warning | `Operations/Mobile Ready/Capture O002 Escort Chips` | `Game.Tests.Editor.Operations.OperationsMobileReadyPlayModeCapture.RunO002Escort` | `o002-escort-chips.en.png` |
| Result outcome + Trust / Intel / Heat + Continue | `Operations/Mobile Ready/Capture Result Deltas` | `Game.Tests.Editor.Operations.OperationsMobileReadyPlayModeCapture.RunResultDeltas` | `result-trust-intel-heat.en.png` |
| All of the above | `Operations/Mobile Ready/Capture All Mobile-Ready Evidence` | `Game.Tests.Editor.Operations.OperationsMobileReadyPlayModeCapture.RunAllEvidence` | all five PNGs |

Validation executeMethod (checks=8):

- `Game.Tests.Editor.Operations.OperationsMobileReadyValidation.RunFocusedValidation`
- Marker: `[OperationsMobileReadyValidation] result=Passed checks=8`
- Capture marker: `[OperationsMobileReadyPlayModeCapture] result=Passed`

## No Victory stamping

This slice does not claim Victory / AriaWon / playable-complete. O003 content hash bumped; historic `host-aria-evidence/.../1104` still records v1 until P2 re-captures.
