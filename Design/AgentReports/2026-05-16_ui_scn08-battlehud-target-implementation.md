Lane:
UI

Task:
P0 SCN-08 RTS Battle HUD layered target match for the M01 battle HUD, using `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png` and `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation.md`

Contracts touched:
- M01 no-selection HUD now keeps the SCN-08 squad strip visible instead of leaving empty tray columns. Rifle remains interactable; APC, Tank, and Helicopter cards are visible but disabled.
- M01 no-selection command bar now keeps target-like button chrome visible in order: STOP, HOLD, MOVE, ATTACK, SPECIAL. Those command buttons are disabled in the no-selection state and labels stay inside the actual button chrome.
- Build remains unavailable for M01: Build is disabled and hidden with the existing `MissionDoesNotAllowBuild` feedback copy preserved.
- Assistant entry remains closed/hidden for M01-01 no-selection.
- M01 PlayMode HUD assertion updated from the old "hide all non-infantry cards" contract to the new SCN-08 layered target contract where non-M01 squad cards can remain visible but unusable.

User-visible behavior:
- M01-01 battle HUD no longer collapses the bottom tray to one rifle card plus empty space.
- The command bar no longer creates floating fallback text labels over the scene for M01 no-selection; command labels render through the normal button LabelText surfaces.
- The HUD better matches the SCN-08 target composition while still preventing Build, vehicle, air, and special command actions from being available in M01-01.

Validation run:
- Synced scoped UI files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Unity licensing workflow used: quit Unity Hub to remove the stale Hub V1 licensing client, then run direct Unity 6000.4.0f1 batchmode outside the Codex sandbox.
- Prefab builder:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen -logFile /private/tmp/warlinecapture-ui-scn08-build-codex1-fresh.log`
- Focused UI EditMode:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-ui-scn08-match-overlay-results.xml -logFile /private/tmp/warlinecapture-ui-scn08-match-overlay-tests.log`
- Runtime capture attempt:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV5 -logFile /private/tmp/warlinecapture-ui-scn08-m01-v5-capture.log`
- Focused M01 public route attempt:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute -testResults /private/tmp/warlinecapture-ui-scn08-m01-quickcustom-results.xml -logFile /private/tmp/warlinecapture-ui-scn08-m01-quickcustom.log`
- Static/new lookup scan:
  `rg -n "FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|GameObject\\.Find|Transform\\.Find|Resources\\.FindObjectsOfTypeAll" Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- Whitespace check:
  `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`

Validation result:
- Prefab builder passed. Log shows `[WarlineCaptureUI] Match Overlay screen generated.` and `Exiting batchmode successfully now!`
- `WarlineCaptureUiMatchOverlayTests` passed 20/20. Results: `/private/tmp/warlinecapture-ui-scn08-match-overlay-results.xml`.
- `git diff --check` passed.
- Static scan found no new lookup calls in the changed M01 controller or changed test lines. Existing `GameObject.Find` calls remain in `Chapter01M01PlayModeValidationTests.cs` at pre-existing rejected-wrapper assertions.
- Runtime V5 capture did not produce accepted post-change visual proof. Log reports `WARLINECAPTURE_M01_GAME_SCENE_CAPTURE_TIMEOUT path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_1920x1080.png`.
- Focused Quick Custom PlayMode route did not pass, but failed on pre-existing Gameplay-owned tactical ground art expectation: expected sprite name containing `m01_tactical_plate_a_pot_2048x1024`, actual `m01_tactical_plate_a_source`. The updated HUD scope assertion was reached only behind that gameplay assertion and is not the reported failure.

Known gaps:
- No accepted post-change runtime screenshot was produced because `WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV5` timed out before capture completion.
- The focused PlayMode route is blocked by a Gameplay data/art mismatch in `/private/tmp/warlinecapture-ui-scn08-m01-quickcustom-results.xml`, not by the UI HUD code.
- Existing broad lookup usage in the large UI prefab builder remains outside this patch.

Cross-lane impacts:
- Gameplay owns the Quick Custom PlayMode blocker: tactical ground sprite expected `m01_tactical_plate_a_pot_2048x1024`, actual `m01_tactical_plate_a_source`.
- QA/HCI can continue UI visual review from the prefab-builder and EditMode evidence, but full runtime visual acceptance still needs a fresh M01 runtime capture after the Gameplay route blocker or capture timeout is resolved.
- PM should keep the Unity licensing workflow note: for Unity 6000 batchmode, quitting Unity Hub removed the stale V1 licensing client and allowed direct editor batchmode to resolve entitlements.

Next recommended task:
Gameplay should restore the expected M01 production tactical ground asset in `WarlineCapture-CodexUnity1` or confirm the assertion update, then QA/HCI should rerun the M01 normal-flow runtime capture for SCN-08 HUD visual acceptance.
