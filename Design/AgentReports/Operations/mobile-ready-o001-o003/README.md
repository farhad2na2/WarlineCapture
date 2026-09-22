# Operations O001–O003 mobile-ready (Programmer 1)

Branch: `cursor/ops-o001-o003-mobile-ready-e190` off `main` tip (not Skirmish PR #35).

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

## Programmer 2 coordination (do not duplicate shell)

**Do not invent a second void HUD.** Bind these Content frames in the Ops presentation shell when ready:

| Frame | Bind loci |
|---|---|
| `OperationsCoachFrame` | O001 active Match HUD coach rail |
| `OperationsEscortRepairControlFrame` | O002/O003 fat-thumb chip bar + warning banner |
| `OperationsMissionResultUiFrame` | MissionResult outcome + 3 delta rows + Continue / Practice |
| `OperationsPartialTeachFrame` | Conclude affordance vs separate Withdraw confirm |

**Shared files left untouched by Programmer 1 (P2 owns):**

- `Assets/Game/Scripts/Operations/Capture/OperationsAriaPlayModePresentation.cs`
- Any shipping Match HUD / void presentation shell P2 opens this sprint

If the presentation shell is missing, treat the Content frames above as the contract and leave shell chrome as a P2 blocker — do not fork a second OnGUI void UI on this branch.

## New Ops-owned EN/FA keys (for Game Design)

Not added to `V3UiLocalizationCatalog`. Living in `OperationsLocalizedCopy`:

- `operations.coach.o001.scan|evidence|extract|done.title/body`
- `operations.controls.escort.go|hold`, `operations.controls.route.main|safe`, `operations.controls.repair`, `operations.controls.defend.hold`
- `operations.warning.clinic|pumps`, `operations.warning.severity.watch|critical`
- `operations.result.continue|practice`, `operations.result.delta.trust|intel|heat|generic`
- `operations.teach.partial.conclude.title`, `operations.teach.partial.withdraw.title`

## Screenshot loci (Programmer 1)

When P2 shell binds (or Capture is extended later), capture:

1. **O001 coach steps** — Scan, Evidence, Extract coach cards (≤3s soft block proof: input still works).
2. **Escort chips** — O002 Go / Hold / Main street / Service loop + clinic warning readable on phone width.
3. **Result deltas** — outcome line + Trust / Intel / Heat rows + Continue (and Practice on Withdraw/Defeat).

## Windows Ops shadow validation (no Mac)

Refresh `D:\Projects\WarlineCapture-Operations` onto this branch. Do not open `D:\Projects\WarlineCapture`.

```powershell
# Host (no Unity) — optional on any machine with dotnet
python Tools/Operations/check_mobile_ready.py

# Editor (Windows Ops shadow only)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsMobileReadyValidation.ps1

# Still green after O003 trim
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsP4Validation.ps1
```

Menus / executeMethod:

- `Game.Tests.Editor.Operations.OperationsMobileReadyValidation.RunFocusedValidation`
- Marker: `[OperationsMobileReadyValidation] result=Passed checks=8`
- Existing: `Game.Tests.Editor.Operations.OperationsP4Validation.RunFocusedValidation`

## No Victory stamping

This slice does not claim Victory / AriaWon / playable-complete. O003 content hash bumped; historic `host-aria-evidence/.../1104` still records v1 until P2 re-captures.
