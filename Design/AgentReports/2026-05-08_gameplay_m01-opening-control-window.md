Lane:
Gameplay

Task:
Fix the M01 First Contact opening so the hostile patrol cannot kill `unit.player.rifle_squad_01` before the player gets the select/move teaching window, and prove the visible M01 units are ECS runtime entities with atlas-state presentation rather than legacy unit bodies.

Files changed:
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

Contracts touched:
- M01 mission id preserved: `saga.ch01.m01.first_contact`.
- Runtime entity ids preserved: `unit.player.rifle_squad_01` and `unit.enemy.patrol_01`.
- M01 hostile patrol remains ECS-authored through `MissionRuntimeEnemyPatrolTag`, `MissionRuntimePatrolRoute`, `UnitCombat`, `UnitAttack`, and `EngageTarget`.
- Added M01-only `MissionRuntimeOpeningControlProtection` ECS component. While present, the hostile patrol cannot auto-engage or deal damage.
- Protection remains through the move-to-cover teaching step and releases on explicit player attack/engagement or patrol damage. It does not make M01 impossible to lose after the player advances to the attack step.
- M01 sprite presenters now carry explicit atlas-state ids for idle, move, attack, damaged, and destroyed. Move state resolves from ECS pathing intent (`UnitTarget`, `UnitPathRequest`, `UnitPathFollow`) as well as physical movement, so presentation state responds to the first move order.
- Replaced the public M01 infantry `MissionRuntimeSpriteRendererRuntime`/`SpriteRenderer` adapter with `MissionRuntimeAtlasQuadRuntime`, an ECS-owned textured mesh quad that consumes the same `MissionRuntimeSpritePresenter` atlas state ids. Legacy ECS mesh/model rendering remains suppressed through `MissionRuntimeSpritePresenterSuppressesLegacyModelTag`.
- M01 sprite-presenter binding now strips `UnitDestroyedVisualReference` and `UnitDestroyedVisualInitialized` from the M01 infantry runtime entities. The authored prefab/config identity remains in use, but the old separate `Destroyed` child is no longer a runtime destruction presentation dependency for `unit.player.rifle_squad_01` or `unit.enemy.patrol_01`.
- The player rifle squad presentation now fans out into four atlas-driven soldier renderers under the single `unit.player.rifle_squad_01` gameplay/selection entity. The hostile patrol remains one infantry/patrol presentation entity.
- Added a world selection marker renderer under the atlas presentation runtime and drive it from `SelectedUnitTag`, so selection has a visible ground marker in addition to HUD selected state.
- M01 infantry attack tracer values are clamped at mission binding to tactical scale: narrow trace width, brief visible lifetime, and higher dash density so enemy fire reads as tracer/impact feedback rather than oversized arcade bullets.

User-visible behavior:
- Before: the hostile patrol could engage and kill the command squad immediately after launch, before the player understood they had controllable soldiers or could issue the first move.
- After: M01 opens with a protected first-control window. The player can see/select the command squad and issue the move toward `tutorial.move_target.cover_01` without immediate lethal hostile fire. Attack/objective/result flow remains reachable after the player explicitly starts the attack step.
- Visible M01 units are tracked by ECS runtime ids `unit.player.rifle_squad_01` and `unit.enemy.patrol_01`; their presenter state resolves idle/move/attack/destroyed atlas ids. Public M01 infantry now renders through ECS-owned atlas quad presentation rather than the old visible `Model` path or the temporary `SpriteRenderer` adapter.
- The player rifle squad now reads as four distinct soldier quads in formation while preserving one squad identity for selection, commands, combat, objective, and HUD state.
- Selecting the rifle squad enables a visible world selection marker under the squad and still applies the existing HUD selected state.
- Enemy/player fire uses the mission-bound tactical tracer scale instead of wide, long-lived projectile strips.
- Destroyed/death feedback for public M01 infantry resolves through the atlas state machine (`vfx.unit.destroyed.small` for the current destroyed state), not through a separate `Destroyed` child visual.
- The public Campaign route now has focused PlayMode coverage from Saga Map through Mission Briefing, Loadout, Deploy, rifle-squad select, move-to-cover command, attack command, hostile patrol neutralization, and public mission result popup display.

Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-m01-four-soldier-selection-trace-results.xml -logFile /private/tmp/warlinecapture-m01-four-soldier-selection-trace.log`
- `rg -n "GetComponentInChildren|GetComponentsInChildren|Resources\\.FindObjectsOfTypeAll|FindAnyObject|FindFirstObject|GameObject\\.Find|Transform\\.Find|FindButton|FindMissionNode" Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs Assets/Game/Scripts/Components/MissionRuntimeComponents.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`

Validation result:
- Passed in assigned Gameplay workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `Chapter01M01PlayModeValidationTests` 8/8, exit code 0.
- Added `PublicCampaignLaunch_M01GoldenPlaythroughShowsResultPopup`, covering public Saga Map -> Mission Briefing -> Loadout -> Deploy -> select rifle squad -> move-to-cover command -> attack command -> hostile patrol neutralized -> public mission result popup.
- Added `GameScene_M01OpeningControlWindowPreventsLethalEnemyFireUntilFirstMove`, covering protected opening fire, tactical move-to-cover path components, survival after the move command, and release on explicit attack step.
- Added `GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds`, covering ECS runtime ids, presenter-owned atlas state ids, no separate destroyed-child presenter flag, no `UnitDestroyedVisualReference`/`UnitDestroyedVisualInitialized`, temporary adapter removal, legacy model suppression, move/attack/death state resolution, and destroyed VFX id resolution.
- Updated visible-unit validation to require `MissionRuntimeAtlasQuadRuntime`, reject `MissionRuntimeSpriteRendererRuntime`, verify mesh/material texture assignment from presenter state, require four distinct player soldier renderers under one squad entity, verify selected marker visibility after selection, assert tactical projectile trace width/lifetime/dash density, and keep legacy model rendering suppressed.
- Existing M01 public launch, select/attack/result, build rejection, terrain/ECS backing, and no-legacy-visible-world coverage still passes.
- Golden playthrough impact: passed by focused automated coverage for the public Campaign route through result popup. The combat-damage portion remains covered by `GameScene_M01SelectionAttackAndResultRouteRespectSurvivalGuard`; the public route test drives the public result popup after issuing the attack command and neutralizing the patrol to avoid timing the whole damage race inside one UI route test.
- M01 infantry-only scope is preserved: tests still cover one command squad and one hostile patrol; no player vehicles, build entry, transport, base, or extra player unit type was added.
- No banned broad runtime scene-search usage was found in the touched focused files.
- `UnitDestroyedVisualReference` / separate `Destroyed` child usage is removed for public M01 infantry runtime. If a prefab still has authored `Destroyed` content for other lanes or later units, M01 runtime binding bypasses it for the rifle squad and hostile patrol.
- Player rifle squad distinct-soldier requirement: passed by runtime assertion of four enabled soldier renderers with distinct formation positions under `unit.player.rifle_squad_01`.
- Selected-state visibility requirement: passed by public golden path after selecting the squad; `SelectedUnitTag` enables the atlas runtime selection marker and the existing HUD selected state remains active.
- Projectile/impact scale requirement: passed by mission-bound `UnitAttack` trace assertions (`TraceWidth <= 0.035`, `TraceVisibleSeconds <= 0.16`, `TraceDashDensity >= 8`) for M01 infantry combat entities.

Known gaps:
- This is a scoped M01 runtime teaching-window guard, not a broad combat rebalance.
- Protection currently releases on explicit attack/engagement or patrol damage, not on a separate authored trigger volume. If design wants a cover-arrival trigger later, add it as authored M01 ECS metadata rather than a scene search.
- Final multi-frame atlas art is still review-dependent. `FinalAtlasArtReady` remains `0`, and idle/move/attack/damaged state ids currently fall back to the approved M01 infantry manifest source art until the final Chapter 1 units atlas frames are approved. Destroyed resolves to `vfx.unit.destroyed.small`. The four-soldier squad layout is runtime composition from current atlas source art, not final per-soldier animation art.
- `MissionRuntimeSpriteRendererRuntime`/`SpriteRenderer` is no longer used for public M01 infantry presentation. The replacement is an ECS-owned mesh/material atlas quad (`MissionRuntimeAtlasQuadRuntime`) that consumes `MissionRuntimeSpritePresenter` state ids. A future Entities Graphics baker can replace the managed mesh GameObject wrapper without changing the presenter contract.
- No known M01 runtime dependency remains on the old separate `Destroyed` child visual. This report does not remove the authoring baker path globally because other non-M01 vehicle/wreck flows still use `UnitDestroyedVisualReference`.
- Unity log still includes known editor/tooling noise such as XcodeApplications plist warnings, Animator warnings, preview-scene leak warnings, persistent allocation warnings, and usbmuxd shutdown noise. None failed validation.

Cross-lane impacts:
- QA/HCI can rerun Gate 4 closeout knowing the player should have a readable first-control window before hostile fire, a public Campaign-route result popup proof, four readable player soldier renderers under one squad entity, visible world selection marker after selection, tactical-scale attack traces, no `MissionRuntimeSpriteRendererRuntime`/`SpriteRenderer` adapter, and no separate `Destroyed` child reference in public M01 infantry presentation.
- PM/user still owns art approval for final multi-frame infantry atlas frames. Gameplay has replaced the runtime presentation adapter with an ECS-owned atlas quad path and preserved existing unit prefab identity for ids/stats/pathing/combat/selection.
- UI and Support/FTUE remain unaffected unless QA/HCI finds a new concrete UI/assistant issue.
- PM marker/VFX and final packaging waiver decisions remain separate from this gameplay blocker.

Next recommended task:
PM should review the atlas quad replacement and decide whether the current M01 infantry source art is acceptable for Gate 4 review or whether Art should provide final multi-frame infantry atlas frames before QA/HCI final Gate 4.
