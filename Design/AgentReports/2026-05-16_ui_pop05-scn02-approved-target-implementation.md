# WarlineCapture Handoff

Lane: UI

Task: POP-05 Mission Result and SCN-02 Main Menu approved target implementation

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Popups/MissionResultPopupController.cs`
- `Assets/Tests/Editor/WarlineCaptureUiComponentPrefabTests.cs`
- `Assets/Tests/Editor/UIMainMenuTests.cs`
- `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/**`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/**`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/*.spriteatlas`
- Evidence captures under `Design/AgentReports/Captures/`

Contracts touched:
- POP-05 reward card contract now uses Commander XP, Credits, Materials, Intel.
- POP-05 objective row contract now uses `Destroy hostile patrol` first and preserves live TMP binding.
- Mission result controller reward/objective binding paths were updated to the new canonical row names.
- SCN-02 resource contract now exposes Credits, Materials, Command Authority.
- SCN-02 route contract keeps Saga Campaign, Persistent Operation, Quick Custom Game routes and adds designed-unavailable badges for Inbox, Store, Events, Ranking, Command Feed.
- Main Menu atlas contract now includes the `LayeredOneGo` Icons, Frames, and Content folders.

User-visible behavior:
- Mission Result preview now shows `M01 First Contact`, mission/scenario/level/map identity, `Destroy hostile patrol`, canonical rewards, a visible city/civilian consequence row, and Replay/Continue states.
- Main Menu now uses the approved SCN-02 layered profile/resource/mode-card/footer/sidebar assets, live resource labels, commander profile fallback art, and designed-unavailable badges on non-live routes.
- Runtime/live text ownership is preserved; target text is not baked into reusable UI elements.

Validation run:
- Built POP-05 prefab:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMissionResultPopup -logFile /private/tmp/warlinecapture-pop05-build.log`
- Built SCN-02 prefab:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-scn02-build.log`
- Focused POP-05 test:
  `/private/tmp/warlinecapture-pop05-test-results.xml`
- Focused SCN-02 test:
  `/private/tmp/warlinecapture-scn02-test-results.xml`
- Full component prefab regression:
  `/private/tmp/warlinecapture-component-prefab-test-results.xml`
- Captures:
  `Design/AgentReports/Captures/SCN-02_MainMenu_ApprovedTargetImplementation_1672x941.png`
  `Design/AgentReports/Captures/POP-05_MissionResult_ApprovedTargetImplementation_1672x941.png`
- Comparisons:
  `Design/AgentReports/Captures/SCN-02_MainMenu_ApprovedTargetImplementation_vs_Target_Comparison.png` MSE `672.70`
  `Design/AgentReports/Captures/POP-05_MissionResult_ApprovedTargetImplementation_vs_Target_Comparison.png` MSE `854.19`

Validation result:
- PASS: `WarlineCaptureUiMainMenuTests` 7/7.
- PASS: `WarlineCaptureUiComponentPrefabTests` 17/17.
- PASS: POP-05 focused content contract 1/1.
- PASS: Unity prefab builders completed without build errors.
- PASS: Captures are nonblank at 1672x941, matching target reference resolution.

Known gaps:
- Captures were produced at the approved target reference resolution, 1672x941. 1920x1080 aliases were created before the dimension check, but the correctly named 1672x941 files are the authoritative evidence.
- Pixel comparison still shows nonzero MSE because the live Unity prefabs preserve TMP/component structure and reuse some existing generated UI masks/emblems/buttons where the approved layer package does not provide replacement parts.
- SCN-02 Persistent Operation, Inbox, Store, Events, Ranking, Command Feed remain designed-unavailable routes by design; this handoff only binds the designed-unavailable state.

Cross-lane impacts:
- Art/Atlas source targets were not modified by UI in this pass.
- QA/PM can review the two capture/comparison pairs against the approved VisualLockLayered targets.
- Gameplay remains owner for live mission result data production beyond the current M01 preview/binding contract.

Next recommended task:
- PM/QA review this POP-05 and SCN-02 handoff. If accepted, update `Design/AgentTasks/ui_current.md` with the next UI priority; UI should not start POP-11, POP-10, SCN-11, SCN-12, or POP-06 until the task file changes.
