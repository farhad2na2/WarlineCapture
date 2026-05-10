Lane:
Gameplay

Task:
P0 M01 public first-control unit readability, selected marker clarity, and atlas art readiness follow-up after QA/HCI Gate 4 rejection.

Files changed:
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- `Design/AgentReports/2026-05-08_gameplay_m01-unit-readability-selection-art.md`

Contracts touched:
- M01 public production camera framing now prefers the player spawn and `tutorial.move_target.cover_01` anchor pair before falling back to the wider objective-frame anchors.
- `unit.player.rifle_squad_01` keeps the ECS-owned `MissionRuntimeAtlasQuadRuntime` path and now applies a player-only readability multiplier at runtime.
- The player rifle squad remains one gameplay squad identity while rendering four distinct soldier quad instances.
- The selected world marker remains driven by `SelectedUnitTag` and is scaled wider for the four-soldier formation.
- `FinalAtlasArtReady` remains `0`; this is a temporary-art/public-readability integration pass, not final Art/Atlas signoff.

User-visible behavior:
- Before: public first-control captures showed the command squad too small at gameplay camera scale, difficult to parse as four soldiers, with a selected state that did not read clearly in the player-facing composition.
- After: the selected first-control public captures show the command squad in playable space with four separate infantry figures under one command identity and a visible cyan world selection marker.
- M01 remains infantry-only at gameplay runtime: one player rifle squad type and one hostile patrol type. No player vehicle, transport, base, or build gameplay was added.
- Move-to-cover, attack, hostile neutralization, and result popup flow remain reachable through the public golden route.
- Tactical projectile/impact trace sizing remains clamped to the existing tactical-scale assertions.

Validation run:
- Synced the focused changed files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Ran `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-m01-readability-camera-art-results.xml -logFile /private/tmp/warlinecapture-m01-readability-camera-art.log`
- Checked touched files for new runtime scene-search usage with `rg -n "FindObject|FindObjects|GameObject\\.Find|Resources\\.FindObjects|Object\\.Find|FindFirstObject|FindAnyObject" Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- Reviewed generated selected first-control captures:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`

Validation result:
- Focused PlayMode validation passed: 8/8 in `/private/tmp/warlinecapture-m01-readability-camera-art-results.xml`.
- No new runtime scene-search usage found in the touched files.
- Public capture generation logged the selected first-control 16:9 and 20:9 PNGs plus refreshed public launch and quick custom captures.
- Visual review of the selected first-control captures: four infantry figures are readable at public gameplay camera scale, and the selected cyan marker is visible around the squad in both 16:9 and 20:9 captures.
- Golden playthrough impact: public campaign route still selects the squad, issues move-to-cover, issues attack, neutralizes the hostile patrol, and reaches the public mission result popup.
- ECS atlas path impact: tests continue to reject `MissionRuntimeSpriteRendererRuntime`, require `MissionRuntimeAtlasQuadRuntime`, require four soldier renderers for the player squad, and reject old separate `Destroyed` child dependencies.

Known gaps:
- `FinalAtlasArtReady` remains `0`; final visual/art readiness is still blocked on PM/user approval of the temporary-art package or a final Art/Atlas package.
- Art/Atlas report `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md` says the current infantry sheet is suitable only as a focused temporary-art approval package.
- Enemy patrol variant/final VFX art readiness remains unresolved by Art/Atlas.
- UI still owns the separate M01 HUD mismatch where APC, Tank, air support, and Build affordances appear in an infantry-only teaching slice.

Cross-lane impacts:
- PM/user owns temporary-art approval or rejection before QA/HCI can treat this as visual signoff.
- Art/Atlas owns final or milestone infantry atlas package if the current temporary package is rejected.
- UI owns suppressing or locking non-M01 HUD affordances for the infantry-only first mission.
- QA/HCI owns the next Gate 4 rerun after this Gameplay handoff and the UI HUD fix are both available in the QA workspace.

Next recommended task:
PM/user should review the refreshed selected first-control captures as the temporary-art approval package, UI should complete the M01 HUD scope fix, and QA/HCI should rerun focused Gate 4 after those two inputs are available.
