Lane:
UI

Task:
P0 finish UI-owned SCN-08 Match HUD target alignment despite runtime proof blocker. Source of truth was `Design/AgentTasks/ui_current.md`; layered target authority was `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png` plus `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- Refreshed editor evidence captures under `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/`
- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v2.md`

Contracts touched:
- M01 no-selection HUD keeps the SCN-08 squad tray populated: Rifle visible/interactable; APC, Tank, and Helicopter visible but disabled.
- M01 no-selection command bar keeps target-like chrome visible in order: STOP, HOLD, MOVE, ATTACK, SPECIAL. Buttons are disabled in the no-selection state and labels stay inside the button chrome.
- Build remains unavailable for M01: hidden/disabled through the existing `MissionDoesNotAllowBuild` path.
- Assistant remains closed in M01-01 no-selection.
- SCN-08 fixed-edge anchor assertions now cover the manifest-aligned top-right pause/settings controls, squad tray/card strip, and minimap frame/content.

User-visible behavior:
- The M01 HUD no longer collapses the bottom squad strip to one rifle card plus empty columns.
- Top-right pause/settings controls now use the SCN-08 target size and icon treatment instead of the smaller previous chrome.
- Squad tray and individual squad cards are aligned to the target strip coordinates.
- Minimap frame and map image align to the target panel rectangle and keep the denser lower-right HUD footprint.
- Command labels no longer require root-level fallback overlays in M01 no-selection.

Validation run:
- Prefab builder, main workspace:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen -logFile /private/tmp/warlinecapture-ui-scn08-v2-build-main.log`
- Focused UI EditMode, final state:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-ui-scn08-v2-match-overlay-results-final.xml -logFile /private/tmp/warlinecapture-ui-scn08-v2-match-overlay-tests-final.log`
- Editor capture matrix:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureM01IntegratedCaptureMatrix -logFile /private/tmp/warlinecapture-ui-scn08-v2-capture-matrix.log`
- Static/new lookup scan:
  `rg -n "FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|GameObject\\.Find|Transform\\.Find|Resources\\.FindObjectsOfTypeAll" Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- Whitespace check:
  `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`

Validation result:
- Prefab builder passed. Log shows `[WarlineCaptureUI] Match Overlay screen generated.` and `Exiting batchmode successfully now!`
- `WarlineCaptureUiMatchOverlayTests` passed 20/20 in the final run. Results: `/private/tmp/warlinecapture-ui-scn08-v2-match-overlay-results-final.xml`.
- Editor capture matrix passed and refreshed 1920x1080 and 2400x1080 M01 HUD captures. Primary no-selection evidence:
  `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/M01_Integrated_1920x1080_01_MatchStart.png`
  `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/M01_Integrated_2400x1080_01_MatchStart.png`
- `git diff --check` passed after mechanical cleanup of Unity-generated trailing spaces in the regenerated prefab.
- Static scan found no lookup calls in `M01InfantryOnlyHudScopeController.cs` or `WarlineCaptureUiMatchOverlayTests.cs`. Existing `GameObject.Find` calls remain in `Chapter01M01PlayModeValidationTests.cs` at rejected-wrapper assertions and were not introduced by this UI patch.
- Runtime V5 visual proof remains blocked outside UI. Exact blocker from `/private/tmp/warlinecapture-ui-scn08-m01-v5-capture.log`: `WARLINECAPTURE_M01_GAME_SCENE_CAPTURE_TIMEOUT path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_1920x1080.png`.
- Focused Quick Custom PlayMode route remains blocked outside UI. Exact blocker from `/private/tmp/warlinecapture-ui-scn08-m01-quickcustom-results.xml`: expected sprite containing `m01_tactical_plate_a_pot_2048x1024`, actual `m01_tactical_plate_a_source`.

Target-vs-runtime checklist:
- Objective panel / Star Goals: UI-owned prefab still uses the SCN-08 objective panel region and M01 objective binding; runtime acceptance blocked by capture timeout.
- Top resource bar: already aligned in fixed-edge tests; unchanged in v2.
- Pause/settings: v2 aligned to manifest target rectangles and icons: Pause 88x66 at top-right offset 110/12, Settings 88x66 at offset 10/12.
- Log/threat feed: already aligned in fixed-edge tests; unchanged in v2.
- Squad cards: v2 aligned tray to 12,684,654,218 and cards to Rifle 10,6,176,218; APC 188,10,158,214; Tank 348,10,156,214; Helicopter 506,10,158,214.
- Command bar / M01 states: command order and disabled no-selection state covered by EditMode tests; Build hidden/disabled with `MissionDoesNotAllowBuild`; no fallback labels.
- Minimap: v2 aligned panel to 310x304 at target top/right offset and map image to 12,28,280,262 inside that frame.
- Panel shadows/cyan trim/dark glass: preserved through existing SCN-08 layer assets; runtime pixel acceptance still blocked by M01 capture timeout.

Known gaps:
- No accepted post-change runtime screenshot exists because the normal M01 runtime capture timed out before output.
- Quick Custom PlayMode proof is blocked by the Gameplay-owned tactical ground sprite mismatch, not by the UI HUD code.
- UI evidence is therefore editor-prefab/capture-matrix proof plus focused EditMode tests, not final runtime visual acceptance.
- Existing broad lookup usage in the large prefab builder remains outside this scoped UI patch.

Cross-lane impacts:
- Gameplay owns the Quick Custom blocker: production route expects `m01_tactical_plate_a_pot_2048x1024` but resolves `m01_tactical_plate_a_source`.
- Gameplay/capture support owns the runtime timeout from `WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV5`.
- QA/HCI can review UI-owned target alignment from the refreshed editor captures, but final SCN-08 runtime acceptance should wait for an unblocked M01 runtime capture.
- PM/user should keep the direct Unity 6000.4.0f1 batchmode workflow: quitting stale Unity Hub licensing state and running the editor binary directly resolves entitlement checks.

Next recommended task:
Gameplay should unblock the M01 runtime proof route by resolving the tactical ground sprite mismatch or updating the route assertion, then QA/HCI should rerun the SCN-08 runtime capture comparison against `SCN-08_RTSBattleHUD_Landscape_Target.png`.
