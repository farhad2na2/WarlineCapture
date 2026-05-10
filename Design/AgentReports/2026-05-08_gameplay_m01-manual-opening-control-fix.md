Lane:
Gameplay

Task:
Fix/prove the manual public M01 opening-control window after PM/user reported the hostile patrol could kill `unit.player.rifle_squad_01` before the player could inspect, select, or issue the first move.

Files changed:
- `Assets/Game/Scripts/Systems/MissionRuntimeOpeningControlProtectionSystem.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeOpeningControlProtectionSystem.cs.meta`
- `Assets/Game/Scripts/Systems/UnitAttackSystem.cs`
- `Assets/Game/Scripts/Systems/UnitEngagementSystem.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- `Design/AgentReports/2026-05-08_gameplay_m01-manual-opening-control-fix.md`

Contracts touched:
- M01 opening-control protection now suppresses hostile patrol combat and hostile auto-engage while `MissionRuntimeOpeningControlProtection` is active.
- M01 opening-control protection no longer releases because of incidental hostile patrol damage or uncommanded player auto-engage; uncommanded command-squad `EngageTarget` is cleared during the protected opening.
- Added a survival guard that runs after combat damage and before death finalization while opening protection is active, restoring the command squad health and clearing death/damage state if any queued or bypassed damage reaches it.
- `UnitAttackSystem` now skips protected opening-control attackers and skips applying aggregated damage to the M01 command squad while any opening protection is active.
- `UnitEngagementSystem` now excludes protected entities from auto-engage target acquisition.
- Public M01 route tests now prove a no-input opening review window before selection/first move.

User-visible behavior:
- Before: PM/user manual review could reach M01 and be killed by the hostile patrol before a relaxed art/readability review, selection, or first move.
- After: public Campaign deploy has a protected no-input opening window. The player can wait briefly, select the rifle squad, see the selected state, issue move-to-cover, and continue into attack/result flow with the command squad alive.
- Squad readability and selected marker behavior from `Design/AgentReports/2026-05-08_gameplay_m01-unit-readability-selection-art.md` remain in place.
- M01 remains infantry-only at gameplay runtime: one player rifle squad type and one hostile patrol type. No vehicle, transport, base, or build gameplay was added.

Validation run:
- Synced focused Gameplay files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Synced accepted UI dependency files needed by the current shared PlayMode test surface into `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
  - `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs.meta`
  - `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- Ran `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-m01-manual-opening-control-results.xml -logFile /private/tmp/warlinecapture-m01-manual-opening-control.log`
- Checked touched Gameplay/test files for new runtime scene-search usage with `rg -n "FindObject|FindObjects|GameObject\\.Find|Resources\\.FindObjects|Object\\.Find|FindFirstObject|FindAnyObject" Assets/Game/Scripts/Systems/MissionRuntimeOpeningControlProtectionSystem.cs Assets/Game/Scripts/Systems/UnitAttackSystem.cs Assets/Game/Scripts/Systems/UnitEngagementSystem.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

Validation result:
- Focused PlayMode validation passed: 8/8 in `/private/tmp/warlinecapture-m01-manual-opening-control-results.xml`.
- Earlier validation attempts failed and drove the fix:
  - First run failed to compile because CodexUnity1 was missing the accepted UI HUD-scope controller dependency.
  - Next runs reproduced the manual-route problem: after a no-input public deploy window, the command squad entity was gone.
  - One intermediate run passed the public-route proof but failed the old release-on-attack assertion; the final proof now validates the safer current contract: first-control survival through no-input wait, selection, first move, and attack/result transition.
- Final public Campaign golden path still reaches the public mission result popup.
- Public Campaign route still generates selected first-control captures:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- ECS atlas presentation remains the public visible path: tests still reject `MissionRuntimeSpriteRendererRuntime`, require `MissionRuntimeAtlasQuadRuntime`, and require four player soldier renderers under one squad identity.
- No new runtime scene-search usage found in the touched Gameplay/test files.

Known gaps:
- This does not claim final Art/Atlas readiness. `FinalAtlasArtReady` remains `0`.
- Temporary M01 infantry art approval remains a PM/user decision after this manual-route blocker is reviewed.
- The protected opening now favors first-mission reviewability and player safety over immediate hostile lethality. QA/HCI should confirm this is acceptable for Gate 4 pacing.
- Manual device/touch ergonomics, assistant Stop/Show Me/result behavior, and invalid-command recovery remain QA/HCI or Support/FTUE scope if they reappear in the next rerun.

Cross-lane impacts:
- PM/user can resume temporary-art review only after accepting this manual opening-control proof.
- QA/HCI can rerun the focused Gate 4 public route with the manual opening-control blocker addressed.
- UI HUD scope remains accepted by PM and was only synced to CodexUnity1 as a validation dependency.
- Art/Atlas remains waiting for PM/user temporary-art decision or a request for replacement/final assets.
- Support/FTUE remains no-action unless QA/HCI reports a concrete assistant or tutorial issue.

Next recommended task:
PM should review this Gameplay proof, then route QA/HCI to rerun Gate 4 from the refreshed public M01 state. If QA/HCI accepts the manual opening-control window and UI HUD scope, PM/user can revisit the temporary M01 infantry art decision.
