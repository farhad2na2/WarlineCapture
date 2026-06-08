Lane: Gameplay
Task: P1 M01 PlayMode log/performance, fixed-road alignment, and legacy runtime guardrails
Files changed:
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs
- Assets/Game/Scripts/UI/RTSSelectionSystem.cs
- Assets/Game/Scripts/UI/RoadBuildSystem.cs
- Assets/Game/Scripts/UI/SharedPrefabPreviewCache.cs
- Assets/Game/Scripts/Environment/DayNightSystem.cs
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/TacticalMaps/Chapter01TacticalAssetManifest.cs
- Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_asset_manifest.asset
- Assets/Game/Data/TacticalMaps/Chapter01/M01_Legacy_Runtime_Guardrails.md
- Assets/Game/Data/TacticalMaps/Chapter01/M01_Legacy_Runtime_Guardrails.md.meta
- Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs
- Assets/Tests/Editor/Chapter01TacticalRuntimeBindingTests.cs
- Assets/Tests/Editor/Chapter01LegacyRuntimeGuardrailTests.cs
- Assets/Tests/Editor/Chapter01LegacyRuntimeGuardrailTests.cs.meta
- Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs
- Design/M01_Legacy_Runtime_Guardrails.md
- Design/AgentReports/2026-05-07_gameplay_m01-log-performance-fixed-roads.md
Contracts touched:
- M01 fixed tactical missions bypass `RuntimeCitySpawnerSystem` random city/road generation.
- M01 initial unit spawning now keeps a compact runtime roster and skips broad faction-base and blocker spawning.
- M01 road metadata is sourced from `TacticalMapDefinition` MainRoad/RoadShoulder surfaces via `Chapter01MissionTacticalRuntimeBinder`.
- `RoadBuildSystem.SetBuildMode(true)` now respects active mission build rejection before setting `InitialUnitsRuntimeState.BuildModeActive`.
- M01 camera focus is preserved when gameplay camera zoom mode initializes.
- Headless batch mode skips project prefab preview rendering when graphics device is null.
- `DayNightSystem` now exposes `RuntimeVisualsEnabled` and can be disabled by `GameBootstrap` for active fixed tactical M01 gameplay.
- M01 atlas manifest entries for player squad, hostile patrol, and command/decor proxy now require fixed-direction baked/contact shadows matched to the tactical ground light.
- M01 legacy render audit documents remaining `Model`/`Destroyed` blockers and the production migration path to animated sprite-atlas rendering.
User-visible behavior:
- M01 should enter the compact tactical slice faster, without spawning full sandbox bases, blockers, or random city roads.
- M01 build/road authoring remains rejected with `MissionDoesNotAllowBuild`; road/build systems remain available as runtime dependencies but not as player-facing M01 authoring.
- The M01 start camera stays aligned to `camera.default_start`.
- Authored tactical road cells remain stable for pathfinding and validation overlays.
- M01 fixed tactical gameplay no longer runs time-of-day visual mutations, dynamic sun/sky/fog changes, or night-vision post-processing through `DayNightSystem`.
- There is no new visual sprite-atlas presenter yet; current visible gameplay can still use legacy prefab rendering while the production art migration is tracked as a major gap.
Validation run:
- `Chapter01LegacyRuntimeGuardrailTests`: `/private/tmp/warlinecapture-m01-legacy-guardrails-results.xml`
- `Chapter01TacticalRuntimeBindingTests`: `/private/tmp/warlinecapture-chapter01-runtime-binding-results.xml`
- `Chapter01M01PlayableRuntimeTests`: `/private/tmp/warlinecapture-m01-playable-results.xml`
- `Chapter01M01PlayModeValidationTests`: `/private/tmp/warlinecapture-m01-log-cleanup-playmode-results.xml`
- Earlier accepted reruns retained for this task: `M01AssistantCommandRuntimeTests` `/private/tmp/warlinecapture-m01-assistant-command-results.xml`, `BattleHudGameplayBridgeConnectionTests` `/private/tmp/warlinecapture-battlehud-bridge-results.xml`
- PlayMode log comparison: baseline `/private/tmp/warlinecapture-m01-playmode.log` vs updated `/private/tmp/warlinecapture-m01-log-cleanup-playmode.log`
Validation result:
- Passed: `Chapter01LegacyRuntimeGuardrailTests` 3/3.
- Passed: `Chapter01TacticalRuntimeBindingTests` 6/6.
- Passed: `Chapter01M01PlayableRuntimeTests` 8/8.
- Passed: `Chapter01M01PlayModeValidationTests` 3/3.
- Previously passed during this task: `M01AssistantCommandRuntimeTests` 10/10 and `BattleHudGameplayBridgeConnectionTests` 6/6.
- Fixed: baseline full `InitialBase` spawning is gone from the updated PlayMode log.
- Fixed: baseline `FreezeDetect` hitch with `RuntimeCitySpawner=1350.3ms` is gone from the updated PlayMode log.
- Fixed-road validation added: binder road buffer now matches authored M01 `TacticalMapDefinition` road surfaces exactly, and `RuntimeCitySpawnerSystem` does not mutate M01 road cells.
- Day/night validation added: PlayMode now asserts `GameBootstrap.DayNight.RuntimeVisualsEnabled == false` for active M01 fixed tactical gameplay.
- Legacy render validation added: audit test proves the current M01 soldier source prefab still has a legacy `Model` child, does not use a separate `Destroyed` child, and documents the migration blocker.
- Atlas-shadow validation added: manifest test proves M01 unit/decor entries require fixed-direction baked/contact shadows.
- Remaining: Unity Entities Graphics/resource-GC `NullReferenceException` entries still appear from `EntitiesGraphicsSystemUtility.RootsHandlerDelegate`.
- Remaining: one preview-scene leak warning still appears at editor shutdown.
- Remaining: headless URP `RenderTexture.Create failed` entries still appear before gameplay validation.
- Remaining: a non-blocking `PerfDiag` entry remains with `RuntimeCitySpawner=0.6ms`, not the previous city-generation hitch.
Known gaps:
- Major production-art gap: M01 is not yet independent of legacy 3D `Model` child rendering. The first production migration slice should add an M01 sprite presenter keyed by `MissionRuntimeEntityId` and atlas sprite ids.
- Major production-art gap: destroyed/damaged production visuals must use atlas states or `vfx.unit.destroyed.small`, not a separate `Destroyed` child.
- Major production-art gap: final M01 unit/building/vehicle atlases are not yet implemented; only the contract and validation guardrails are in place.
- QA recommendation: previous `RuntimeCitySpawner` hitch is fixed, not a blocker.
- QA recommendation: Entities Graphics resource-GC `NullReferenceException` is benign editor/headless unless reproduced in player/device logs, because the stack is Unity package cleanup and no project gameplay stack is involved.
- QA recommendation: preview-scene leak is minor/benign editor-only unless a later diagnostic stack points to a project editor utility.
- QA recommendation: headless URP render-target errors are benign for gameplay logic tests and should not be used as visual readiness evidence.
- Minor follow-up: M01 still logs AI plan noise (`AIProduction MissingProducerBuilding`, `AIBuild Blocked`, `AISquad Waiting`) because generic AI plans remain active against the compact M01 setup.
Cross-lane impacts:
- QA/HCI can validate M01 against fixed-road metadata and downgrade the old city-spawn performance risk.
- UI and Support/FTUE can continue using `MissionDoesNotAllowBuild`; direct road build entry points now obey the same contract.
- Art/production gameplay now has a checked-in guardrail audit for replacing legacy `Model`/`Destroyed` visuals with fixed-light sprite atlases.
- UI should treat day/night controls as legacy/future for current M01 unless the PM/design lane explicitly re-enables them.
Next recommended task:
- Implement the first M01 animated sprite-atlas presenter slice for player squad, hostile patrol, and command/decor proxy, including fixed-direction baked/contact shadows, then validate it at close tactical camera scale against the approved tactical ground.
