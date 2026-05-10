Lane:
UI

Task:
Fix and prove the UI-owned public M01 launch capture composition for Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch and Quick Custom -> Launch, using `Design/AgentTasks/ui_current.md` as the current priority source.

Files changed:
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`

Contracts touched:
- Public launch evidence capture contract for `Chapter01M01PlayModeValidationTests`: `CapturePlayerViewAtResolution` now requires the WarlineCapture app canvas root and temporarily renders that canvas through the gameplay camera into the evidence `RenderTexture`.
- Capture restore contract: the helper restores the app canvas `renderMode`, `worldCamera`, `planeDistance`, and `sortingOrder`, plus the gameplay camera render target/aspect state, after each capture.
- Route ids unchanged: `WarlineCaptureRoute.SagaMap`, `WarlineCaptureRoute.MissionBriefing`, `WarlineCaptureRoute.LoadoutSquadPrep`, `WarlineCaptureRoute.QuickCustomSetup`, and `WarlineCaptureRoute.Match`.
- Mission id unchanged: `saga.ch01.m01.first_contact`.

User-visible behavior:
No production runtime behavior changed in this UI pass. The public-launch validation evidence now captures the actual player-facing composition: WarlineCapture HUD/objective/assistant/threat/action/minimap UI over the authored M01 tactical terrain and visible units, instead of a gameplay-camera-only image.

Validation run:
- Restored assigned UI validation workspace with tool approval: `git worktree add --detach /Users/farhad/Projects/WarlineCapture-CodexUnity2 HEAD`
- Applied only the UI capture-helper diff into `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.
- Required Unity PlayMode graphics-enabled validation from assigned UI workspace:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-ui-m01-public-launch-results.xml -logFile /private/tmp/warlinecapture-ui-m01-public-launch.log`
- `rg -n "GetComponentInChildren|GetComponentsInChildren|Resources\.FindObjectsOfTypeAll|FindAnyObject|FindFirstObject|GameObject\.Find|Transform\.Find|FindButton|FindMissionNode" Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs` from `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- `git diff --check` from `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- `git diff --check` from `/Users/farhad/Projects/WarlineCapture`
- `sips -g pixelWidth -g pixelHeight` on all four public-launch captures.
- Visual inspection of `campaign-public-m01.png` and `quick-custom-public-m01.png`.

Validation result:
- `Chapter01M01PlayModeValidationTests`: 5/5 passed from `/Users/farhad/Projects/WarlineCapture-CodexUnity2`. Results: `/private/tmp/warlinecapture-ui-m01-public-launch-results.xml`.
- Public campaign smoke passed: `PublicCampaignLaunch_ReachesM01ProductionVisibleSlice` generated:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- Quick Custom smoke passed: `PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute` generated:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- Capture dimensions: campaign and Quick Custom 16:9 are 1280x720; campaign and Quick Custom 20:9 are 1600x720.
- Visual spot-check: campaign and Quick Custom 16:9 captures include the WarlineCapture HUD/canvas over authored M01 terrain/units. They are not route-only, camera-only, flat brown/blank, or old 3D prototype evidence.
- Banned lookup scan on the touched PlayMode test returned no matches.
- `git diff --check`: passed in both the assigned UI workspace and main workspace.

Known gaps:
- The full-screen evidence is a deterministic camera/render-texture capture with the WarlineCapture app canvas temporarily rendered through the gameplay camera for batchmode output, then restored. It is not a platform `ScreenCapture.CaptureScreenshot` artifact.
- Unity log still reports non-fatal shutdown noise after the passing run: preview-scene leak warnings, persistent allocation leak warnings, thread-finalized messages, usbmuxd shutdown output, and an access-token licensing warning. None failed the focused PlayMode suite.
- `WarlineCaptureGameLaunchUtility` still contains pre-existing `Resources.FindObjectsOfTypeAll` scene discovery usage. This pass did not touch production runtime launch code and did not add new scene-search usage.
- The worktree also contains unrelated in-flight files from other lanes: `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`, `Design/AgentReports/2026-05-08_pm_public-launch-handoff-workspace-review.md`, and `Design/AgentReports/2026-05-08_support-ftue_gate4-current-wait.md`. UI did not modify or commit them.

Cross-lane impacts:
- Gameplay's accepted ECS terrain proof plus this UI capture-composition validation means the public-launch blocker now has assigned-workspace evidence for HUD/canvas over the M01 production playfield.
- QA/HCI can rerun public-launch/manual smoke against the campaign and Quick Custom paths using the regenerated captures as expected behavior.
- Support/FTUE assistant binding remains preserved because the public M01 launch leaves the WarlineCapture router/HUD on `WarlineCaptureRoute.Match`.

Next recommended task:
QA/HCI should run the public launch smoke from its assigned workspace/device path for Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch and Quick Custom -> Launch, confirming the player sees the same HUD plus authored M01 tactical playfield and no legacy 3D prototype.
