Status: accepted
Topic:
QA/HCI validation of Gameplay M01 opening-control and ECS atlas presentation handoff

Lane:
QA/HCI

Validated handoff:
- `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md`

Task:
Validate the Gameplay handoff for the M01 first-control blocker and visible infantry presentation blocker without producing final Gate 4 closeout.

Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-gameplayws-m01-opening-control-window-results.xml -logFile /private/tmp/warlinecapture-gameplayws-m01-opening-control-window.log`
- `rg -n "MissionRuntimeAtlasQuadRuntime|MissionRuntimeSpriteRendererRuntime|MissionRuntimeOpeningControlProtection|UnitDestroyedVisualReference|UnitDestroyedVisualInitialized|MaterialAnimationIndex|SelectedUnitTag|TraceWidth|TraceVisibleSeconds|TraceDashDensity|FinalAtlasArtReady" Assets/Game/Scripts Assets/Tests/PlayMode`
- `rg -n "GameScene_M01OpeningControlWindowPreventsLethalEnemyFireUntilFirstMove|GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds|PublicCampaignLaunch_M01GoldenPlaythroughShowsResultPopup|four|MissionRuntimeAtlasQuadRuntime|MissionRuntimeSpriteRendererRuntime|TraceWidth|UnitDestroyedVisualReference" Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `rg -n "GetComponentInChildren|GetComponentsInChildren|Resources\\.FindObjectsOfTypeAll|FindAnyObject|FindFirstObject|GameObject\\.Find|Transform\\.Find|FindButton|FindMissionNode" Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs Assets/Game/Scripts/Components/MissionRuntimeComponents.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs Assets/Game/Scripts/Systems/MissionRuntimeOpeningControlProtectionSystem.cs`

Validation result:
- Accepted for the Gameplay handoff.
- Gameplay source workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity1` passed `Chapter01M01PlayModeValidationTests`: 8/8, exit code 0.
- Passed tests include:
  - `PublicCampaignLaunch_M01GoldenPlaythroughShowsResultPopup`
  - `GameScene_M01OpeningControlWindowPreventsLethalEnemyFireUntilFirstMove`
  - `GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds`
  - existing public launch, quick custom launch, anchored spawn, select/attack/result, and build rejection coverage
- Static focused checks confirm the touched test/code surfaces assert or implement:
  - `MissionRuntimeOpeningControlProtection`
  - `MissionRuntimeAtlasQuadRuntime`
  - rejection of `MissionRuntimeSpriteRendererRuntime`
  - removal of `UnitDestroyedVisualReference` / `UnitDestroyedVisualInitialized` from M01 infantry entities
  - selected-state marker through `SelectedUnitTag`
  - tactical projectile trace limits
  - explicit `FinalAtlasArtReady = 0` temporary-art marker
- No banned broad scene-search usage was found in the focused touched files scanned above.

QA/HCI acceptance notes:
- The handoff satisfies the immediate Gameplay blocker contract for automated proof: public M01 launch still reaches the production slice, the public golden path reaches result popup, opening control protection prevents lethal hostile fire through the move teaching step, and select/move/attack/result flow remains reachable.
- The handoff satisfies the automated presentation contract for this validation pass: public M01 infantry is asserted as ECS runtime presentation through `MissionRuntimeAtlasQuadRuntime`, not the temporary `MissionRuntimeSpriteRendererRuntime`; M01 infantry runtime strips separate destroyed visual components; and the player rifle squad is asserted as four distinct soldier renderers under one squad identity.
- This is not final Gate 4 QA/HCI closeout. Final Gate 4 still needs QA/HCI focused HCI review after PM accepts this gameplay handoff, including readability of first-control state, touch/camera ergonomics or documented substitute, invalid command recovery, assistant ownership/Stop behavior, performance/log readiness, and visual review of current atlas art/markers/VFX in the active QA workspace.

Blockers / gaps:
- `WarlineCapture-CodexUnity3` is stale for this handoff. Running the same PlayMode filter there passed only the older 5-test M01 suite and did not include the new handoff report/tests, so it cannot be used as independent final Gate 4 evidence until refreshed.
- The main `/Users/farhad/Projects/WarlineCapture` workspace could not be opened by Unity batchmode because another Unity instance already had that project open.
- Final multi-frame infantry atlas art is still not accepted; the handoff explicitly marks `FinalAtlasArtReady = 0`. QA/HCI accepts this as a known art-readiness gap for the gameplay blocker only, not as final visual approval.
- Unity logs still include known editor/tooling noise: Animator warnings, XcodeApplications plist warnings, preview scene leak warning, persistent allocation leak warning, and usbmuxd shutdown noise. They did not fail the focused validation.

Owner lane:
- Gameplay: accepted for this handoff.
- PM: should review/accept the Gameplay handoff and decide whether temporary M01 infantry source art can proceed into Gate 4 HCI review or needs Art/user approval first.
- QA/HCI: can continue to focused Gate 4 HCI only after PM accepts the gameplay handoff and the QA workspace is refreshed with the new test/report state.

Can another lane continue:
- PM can continue immediately with handoff acceptance/rejection.
- Gameplay can continue only if PM requests fixes or art/readability follow-up.
- UI and Support/FTUE do not need new work from this validation unless final QA/HCI finds a concrete UI or assistant issue.
