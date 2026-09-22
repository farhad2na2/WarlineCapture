# Operations host tools

- `check_p0.py` — Linux/host schema and ownership checks. No Unity.
- `check_p1.py` — Linux/host city and profile transaction checks. No Unity.
- `check_p2.py` — Linux/host tactical rule checks. No Unity.
- `check_p3.py` — Linux/host launch, return, and recovery checks. No Unity.
- `check_p4.py` — Linux/host O001–O003 vertical-slice checks. No Unity.
- `check_aria_evidence.py` — Linux/host O001–O003 ARIA evidence harness checks. No Unity. Marker `[OperationsAriaEvidenceValidation] result=Passed checks=8`.
- `check_aria_playmode_capture.py` — Linux/host Play Mode capture **wiring** checks. No Unity. Marker `[OperationsAriaPlayModeCaptureValidation] result=Passed checks=7`. Does not claim AriaWon.
- `check_mobile_ready.py` — Linux/host O001–O003 mobile-ready content checks (coach/escort/result/partial/practice) plus landed presentation/pacing guards. No Unity. Marker `[OperationsMobileReadyValidation] result=Passed checks=8`. Does not claim playable.
- `Ensure-OperationsShadowWorktree.ps1` — Programmer 2 later: create/refresh `D:\Projects\WarlineCapture-Operations`. The script's default branch is the P0 branch; point the worktree at the P1 branch before P1 Editor validation.
- `Invoke-OperationsP0Validation.ps1` — Programmer 2 later: P0 Unity validation against that shadow project only.
- `Invoke-OperationsP1Validation.ps1` — Programmer 2 later: P1 Unity validation against that shadow project only.
- `Invoke-OperationsP2Validation.ps1` — Programmer 2 later: P2 Unity validation against that shadow project only. Point the shadow worktree at the P2 branch first.
- `Invoke-OperationsP3Validation.ps1` — Programmer 2 later: P3 Unity validation against that shadow project only. Point the shadow worktree at the P3 branch first.
- `Invoke-OperationsP4Validation.ps1` — Programmer 2 later: P4 Unity validation against that shadow project only. Point the shadow worktree at the P4 branch first.
- `Invoke-OperationsAriaEvidenceValidation.ps1` — Programmer 2 later: ARIA evidence harness Unity validation against that shadow project only.
- `Invoke-OperationsAriaPlayModeCaptureValidation.ps1` — Programmer 2: Play Mode capture wiring Unity validation (marker checks=7). Not a live AriaWon claim.
- `Invoke-OperationsAriaPlayModeCapture.ps1 -Mission O001|O002|O003` — Programmer 2: live Play Mode mid-mission + win-screen capture on the Ops shadow. **Omits Unity `-quit`** (async EnterPlaymode; runner calls `EditorApplication.Exit`). Treats pass marker + win-screen/result JSON as success even when InvokeUnity reports a null/non-zero exit under GuiLicensing. Marker `[OperationsAriaPlayModeCapture] result=Passed`. Watch seam not opened.
- `Invoke-OperationsMobileReadyValidation.ps1` — O001–O003 mobile-ready Unity validation against the Ops shadow. Marker `[OperationsMobileReadyValidation] result=Passed checks=8`.
- `check_o001_player_ready.py` — Linux/host P4R O001 shared launch: Package 3 deploy with `InvokesSharedSceneView`, shipping control names, envelope round trip, settle, and hub return. No Unity. Marker `[OperationsO001PlayerShellValidation] result=Passed checks=5`. That marker is not a player-ready certification. Design-APPROVED O001–O003 and historical Ops AriaWon captures are WIP/foundation. There is no finished Old Quarter art scene.
- `Invoke-OperationsO001PlayerReadyValidation.ps1` — Programmer 2: the same O001 shared-launch checks in the Editor on `D:\Projects\WarlineCapture-Operations`. Manual play uses the Ops D01 district button, Raid confirm, match Select/Move/Attack/Scan/Hold/Board, and Continue. `Operations/P4R/Arm Regular EN Aria` makes Regular EN Aria invoke those same buttons. `Operations/P4R/Clear Aria Arm` leaves the clock on manual play. The profile is `OperationsO001Player`. Passing the marker is not a player-ready certification.
- `Invoke-OperationsMobileReadyCapture.ps1 -Shot All|Coach|Escort|Result` — Game View capture of coach steps, escort chips, and result deltas into `Design/AgentReports/Operations/mobile-ready-o001-o003/_Evidence/`. **Omits Unity `-quit`**. Marker `[OperationsMobileReadyPlayModeCapture] result=Passed`. Uses the landed presentation shell.

Do not point Unity at `D:\Projects\WarlineCapture`.
