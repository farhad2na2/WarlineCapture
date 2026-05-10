Lane:
UI

Task:
Fix the UI-owned Gate 4 blocker for M01 First Contact: the public mission is an infantry-only teaching slice, so the player-facing HUD must not invite APC, Tank, air support, Build, vehicle production, transport, or base/build actions while preserving the rifle squad selected state and golden-path command flow.

Files changed:
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs.meta`
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`

Contracts touched:
- Added `M01InfantryOnlyHudScopeController` on `Screen_MatchOverlay.prefab`.
- For active mission id `saga.ch01.m01.first_contact`, the controller suppresses the generated HUD roots for `Squad_APC`, `Squad_Tank`, `Squad_AirSupport`, `SpecialButton`, `BuildButton`, the build drawer, and the command wheel.
- Preserved the rifle squad card and command bar roots for M01 so the golden path can still use Stop, Hold, Move, and Attack.
- Added PlayMode assertions to `Chapter01M01PlayModeValidationTests` proving the public M01 Match HUD has the infantry-only scope controller active and its suppressed roots hidden.
- Route ids unchanged: `WarlineCaptureRoute.SagaMap`, `WarlineCaptureRoute.MissionBriefing`, `WarlineCaptureRoute.LoadoutSquadPrep`, `WarlineCaptureRoute.QuickCustomSetup`, and `WarlineCaptureRoute.Match`.
- Mission id unchanged: `saga.ch01.m01.first_contact`.

User-visible behavior:
Before this pass, public M01 captures showed first-mission HUD affordances for APC, Tank, air support, Build, and related production surfaces even though the mission scope is infantry-only. After this pass, M01 player-facing HUD shows the rifle squad card and the core command bar only; APC/Tank/air support/Build/vehicle/base production affordances are not presented as usable options. Selecting `unit.player.rifle_squad_01` still leaves a readable selected-state panel and the Move/Attack command flow remains available.

Validation run:
- Regenerated the Match Overlay prefab in the assigned UI Unity workspace:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen -logFile /private/tmp/warlinecapture-ui-m01-infantry-hud-build.log`
- Required focused PlayMode validation from assigned UI workspace:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-ui-m01-infantry-hud-results.xml -logFile /private/tmp/warlinecapture-ui-m01-infantry-hud-playmode.log`
- Static no-runtime-scene-search scan on touched runtime/test files:
  `rg -n "GetComponentInChildren|GetComponentsInChildren|Resources\.FindObjectsOfTypeAll|FindAnyObject|FindFirstObject|GameObject\.Find|Transform\.Find|FindButton|FindMissionNode" Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `git diff --check`
- `sips -g pixelWidth -g pixelHeight` on refreshed public M01 captures.
- Visual inspection of `campaign-public-m01-selected-first-control.png`.

Validation result:
- Prefab builder: passed. Log path: `/private/tmp/warlinecapture-ui-m01-infantry-hud-build.log`.
- `Chapter01M01PlayModeValidationTests`: 8/8 passed. Results path: `/private/tmp/warlinecapture-ui-m01-infantry-hud-results.xml`.
- Public M01 campaign route still reaches the production slice.
- Public Quick Custom route still reaches the production slice.
- Golden playthrough still reaches the result popup after select -> move -> attack.
- M01 HUD scope assertions passed in campaign, Quick Custom, and golden-path tests.
- Selected-first-control capture generated at 1280x720 and 1600x720; visual spot-check confirms the selected rifle squad HUD is readable and the player-facing HUD no longer shows APC/Tank/air support/Build as first-mission options.
- Static no-runtime-scene-search scan returned no matches on the touched runtime/test files.
- `git diff --check`: passed.

Known gaps:
- This pass only owns the UI affordance mismatch. Gameplay/Art still owns world-scale unit readability, four-soldier presentation, selected marker clarity, projectile scale, and final atlas art readiness per the current PM/QA handoffs.
- Manual device/touch ergonomics, invalid-command recovery, assistant Stop, and Show Me/result explanation checks remain QA/HCI and Support/FTUE follow-up areas if PM keeps them in Gate 4 scope.
- The working tree contains unrelated in-flight changes from Gameplay, Art, PM, QA/HCI, and Support/FTUE lanes. UI did not revert, commit, or claim those files.

Cross-lane impacts:
- QA/HCI can rerun Gate 4 against public M01 with the HUD-scope blocker fixed and PlayMode coverage in place.
- Gameplay/Art can continue readability/art fixes without needing to change the UI suppression contract.
- Support/FTUE remains no-action unless QA/HCI finds assistant or command-recovery regressions after the rerun.

Next recommended task:
QA/HCI should rerun the Gate 4 focused HCI pass using the refreshed captures and public M01 route, specifically checking that the infantry-only HUD scope is accepted and that remaining findings are limited to Gameplay/Art readability or manual ergonomics.
