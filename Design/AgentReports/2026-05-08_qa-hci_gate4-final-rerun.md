Status:
accepted for route stability and focused Gate 4 rerun; needs PM/user temporary-art decision before final Gate 4 visual signoff

Lane:
QA/HCI

Task:
Rerun focused M01 Gate 4 from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` after accepted UI infantry-only HUD scope, Gameplay manual opening-control proof, Gameplay readability/selection handoff, and Art/Atlas temporary-art readiness report.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_gate4-final-rerun.md`

Contracts touched:
- M01 public golden route: Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy -> select rifle squad -> move to tutorial cover -> attack hostile patrol -> enemy neutralized -> result popup.
- M01 infantry-only scope: one player rifle squad type, one enemy patrol type, no player vehicles, no vehicle production, no transport, no base/build mechanics.
- M01 visible presentation: ECS atlas-backed infantry presentation, no visible legacy `Model`, no temporary SpriteRenderer adapter, no old per-Model animation output, no separate `Destroyed` child runtime dependency for M01 infantry.
- M01 HCI readiness: first-control readability, four-soldier squad readability, selected-state clarity, infantry-only HUD scope, projectile/impact scale, route stability for temporary-art review.

User-visible behavior:
- Public M01 now gives the player a safe first-control window after Deploy.
- The player can select `unit.player.rifle_squad_01`, see a readable selected rifle squad HUD card, issue Move, issue Attack, neutralize the hostile patrol, and reach the public result popup.
- The public HUD no longer presents APC, Tank, air support, Build, vehicle production, transport, or base/build affordances as usable M01 options.
- Selected first-control captures now show four readable infantry figures under one command identity and a visible cyan world selection marker.
- The route is now stable enough for a short PM/user temporary-art review.

Validation run:
- Refreshed `/Users/farhad/Projects/WarlineCapture-CodexUnity3` with focused handoff files from UI, Gameplay, and Art/Atlas:
  - `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
  - `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
  - `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs.meta`
  - `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
  - `Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs`
  - `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
  - `Assets/Game/Scripts/Campaign/Chapter01M01SpritePresenterCatalog.cs`
  - `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
  - `Assets/Game/Scripts/Systems/MissionRuntimeOpeningControlProtectionSystem.cs`
  - `Assets/Game/Scripts/Systems/MissionRuntimeOpeningControlProtectionSystem.cs.meta`
  - `Assets/Game/Scripts/Systems/MissionRuntimeSpritePresenterSystem.cs`
  - `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitAttackSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitEngagementSystem.cs`
  - `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- Verified current QA workspace symbols before the run:
  - `M01InfantryOnlyHudScopeController`
  - `MissionRuntimeOpeningControlProtection`
  - `MissionRuntimeAtlasQuadRuntime`
  - `PublicCampaignLaunch_M01GoldenPlaythroughShowsResultPopup`
  - `GameScene_M01OpeningControlWindowPreventsLethalEnemyFireUntilFirstMove`
  - M01 HUD scope assertions
- Ran:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-qa-gate4-final-rerun-results.xml -logFile /private/tmp/warlinecapture-qa-gate4-final-rerun.log`
- Reviewed refreshed captures:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`

Validation result:
- Focused PlayMode rerun passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`: `Chapter01M01PlayModeValidationTests` 8/8, exit code 0.
- Public Campaign golden path passed through result popup.
- Public Campaign and Quick Custom launch paths still reach the M01 production Match route.
- Opening-control validation passed, including the no-input/first-control safety window before lethal hostile fire.
- Select, move-to-cover, attack, hostile neutralization/result readiness, and build-rejection feedback coverage passed.
- Infantry-only HUD scope passed: APC, Tank, air support, Build, production, transport, and base/build affordances are suppressed for M01.
- ECS atlas presentation assertions passed: `MissionRuntimeAtlasQuadRuntime` is required, `MissionRuntimeSpriteRendererRuntime` is rejected, legacy model suppression is asserted, four player soldier renderers are required, and separate M01 destroyed-child runtime dependencies are rejected.
- Visual HCI review of selected first-control captures: four-soldier player squad and cyan selected marker are readable enough for temporary-art review in both 16:9 and 20:9; selected rifle squad HUD card is readable; infantry-only command surface is coherent.
- Projectile/impact scale remains covered by tactical trace assertions in the focused test surface.

Known gaps:
- This is not final art signoff. `FinalAtlasArtReady` remains `0`.
- PM/user still owns approval or rejection of the temporary M01 infantry art package described in `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`.
- Current infantry sheet is key-pose temporary art, not final multi-frame animation.
- Enemy patrol red-accent/final variant and final `vfx.impact.light` / destroyed-impact art remain unresolved by Art/Atlas.
- Manual device/touch ergonomics were not executed on physical hardware; this rerun uses automated public route coverage plus capture review as the documented substitute.
- Assistant ownership/Stop behavior did not receive a dedicated new manual pass in this rerun; no concrete assistant regression appeared in the focused route. Support/FTUE should remain watch-only unless PM keeps assistant Stop as a hard Gate 4 manual requirement.
- Logs still include repeated `Animator is not playing an AnimatorController` warnings from `MenuView` panel show/hide, XcodeApplications plist warnings, preview scene leak warning, persistent allocation leak warning, and usbmuxd shutdown noise. Tests passed despite this noise; the batchmode run should not be used as performance acceptance because pregame frame diagnostics showed low initial batchmode fps.

Cross-lane impacts:
- PM/user can now review the temporary infantry art package without the prior route-stability blocker.
- Art/Atlas owns any follow-up if PM/user rejects the temporary art package or requests enemy variant/final VFX assets.
- Gameplay owns follow-up only if PM/user rejects current temporary art integration or QA/HCI/user finds a concrete readability/pacing defect.
- UI can remain waiting; infantry-only HUD scope is accepted in this focused rerun.
- Support/FTUE can remain waiting unless PM asks for a dedicated assistant Stop/Show Me/manual recovery pass.

Next recommended task:
- PM/user should approve or reject temporary Gate 4 infantry art using the selected first-control captures and the Art/Atlas package.
- If approved, PM can treat M01 as route-stable for temporary-art review and decide whether remaining art gaps are acceptable for the next milestone.
- If rejected, Art/Atlas should provide final/milestone player and enemy infantry atlas frames plus VFX/impact assets, then Gameplay should integrate and QA/HCI should rerun the focused visual pass.
