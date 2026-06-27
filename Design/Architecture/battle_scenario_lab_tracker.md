# Battle Scenario Lab Tracker

Purpose:
Create a reusable isolated battle scenario lab so automated validation and manual Unity scene verification use the same scenario definitions, metrics, and success criteria.

The first target scenario is an air-defense interception test: an enemy ground missile threat or enemy air threat enters the defended area, a friendly air missile launcher detects/tracks/fires, and radar support should improve detection, lock, tracking, and outcome quality.

This tracker is for test infrastructure and scenario validation. It must not change live combat balance or gameplay behavior unless a later scenario explicitly proves the intended balance change and the user approves it.

Last updated:
2026-06-27

## Progress Snapshot

- Checklist progress: `100 / 100 complete (100.0%)`.
- In progress: `0`.
- Remaining open: `0`.
- Current target: `Scenario Lab Next workflow switches live visual tests, clears old scenario entities/VFX/preview markers between runs, and advances AD-011 through jet, helicopter, drone, and attacking-jet production visual variants`.
- First scenario target: `AD-001 Air missile launcher intercepts incoming ground missile, no-radar versus radar-near comparison`.
- Isolated scene status: `scene shell created at Assets/Game/Scenes/ScenarioLab/BattleScenarioLab.unity with neutral ground, camera, root reference holder, defended target marker, passive metrics overlay, Play bootstrap, scenario selector, previous/next buttons, restart button event, manual variant selector, a baked Scenario Lab production-prefab registry SubScene containing launcher/radar plus jet/helicopter/drone production prefabs, and live ECS visual playback that instantiates production launcher/radar/target prefabs and seeds the real launcher systems instead of animating proxy objects`.
- Automated metrics runner status: `AD-001 through AD-011 plus GM-001 and DR-001 scenario assets, asset-backed fixed-step ECS runners, report writer, batch execute methods, suite runner, suite index report, focused tests, and Phase 6 deterministic radar-support tuning implemented`.
- Manual verification status: `scene opens in batchmode smoke validation, has required references, can cycle AD-001 through AD-011 plus GM-001 and DR-001 through Previous/Next or ScenarioSelector, can rerun the selected scenario through a passive restart button event, can run all variants or one selected variant, displays pass/fail plus variant/comparison metrics, resolves the baked production prefab registry in Play Mode, instantiates production ECS launcher/radar/target entities, observes live ground and air missile projectiles, validates the visible ground rocket against the logical projectile, validates rendered visual contact between the air missile and ground rocket before accepting the intercept, validates that the spent ground rocket visual is cleared after the intercept, validates that runtime Next advances from AD-001 to AD-002 and then through AD-011 jet/helicopter/drone/attacking-jet visual variants with old target entities cleaned between runs, hides lab preview markers during live playback, clears pooled VFX and non-prefab renderable debris on every switch, and user Play Mode verification has accepted the AD-001 collision/explosion/clear-after-intercept visual path.`
- Visual proof capture status: `AD-001 static proof captures saved under Design/VisualLockLayered/_BattleScenarioLab/AD-001/: AD-001-A-NoSupport-Normal.png, AD-001-B-RadarNear-Normal.png, AD-001-D-RadarNear-FastThreat.png, and ad001_visual_proof_contact_sheet.png`.
- Validation status: `git diff --check passed after the runtime Next visual-switch fix; static old-value scan found no remaining old radar-support magic values in the relevant runtime/config paths; Unity EditMode BattleScenarioLabModelTests passed 5/5; Unity EditMode BattleScenarioAd001RunnerTests passed 2/2 including asset-backed run after overlay/bootstrap compile; Unity EditMode BattleScenarioAd002RunnerTests passed 2/2; Unity EditMode BattleScenarioAd003RunnerTests passed 2/2; Unity EditMode BattleScenarioEcsSpawnHelpersTests passed 4/4 before the GM-001 helper additions; latest broad BattleScenario EditMode filter passed 15/15 before AD-004; latest shadow-project batch executeMethod BattleScenarioLabSuiteRunner.RunScenarioSuite passed through the shared BattleScenarioLabRuntimeRunner dispatch path with AD-001 through AD-011 plus GM-001 and DR-001 non-skipped; latest shadow AD-011 report passed jet, helicopter, drone, and attacking-jet variants with closest ECS impact separations `2.28m`, `3.20m`, `1.86m`, and `0.66m`; latest shadow-project batch executeMethod BattleScenarioLabSceneBuilder.CreateManualSceneShell passed and regenerated the scene/SubScene with assigned UGUI dropdown templates, ScenarioSelector, Previous/Next controls, serialized scenario list, production-prefab live ECS visual scene, and baked registry SubScene containing launcher/radar plus jet/helicopter/drone prefabs; latest shadow-project smoke validation passed; latest shadow-project Play Mode executeMethod BattleScenarioLabValidationRunner.ValidateManualSceneNextSwitchesVisualPlayback passed after observing runtime Next advance AD-001 -> AD-002 -> AD-011 jet -> AD-011 helicopter -> AD-011 drone -> AD-011 attacking jet with old target entities cleaned between runs and the expanded renderable-debris cleanup active; latest shadow-project Play Mode executeMethod BattleScenarioLabValidationRunner.ValidateManualSceneLiveEcsPlayback passed across all four AD-001 visual variants with visual contact distances <= `0.34m` and clear-after-intercept observed. Known non-blocking batchmode noise remains from Entities Graphics under -nographics. Latest suite index: /private/tmp/warline-scenario-lab-suite-index.json; latest AD-011 suite log: /private/tmp/warline-scenario-lab-ad011-suite-final2.log; latest AD-011 report: /private/tmp/warline-scenario-lab-AD-011_AirMissileLauncher_TracksAndHitsAirTargetClasses.json; latest Next visual-switch log: /private/tmp/warline-scenario-lab-next-cleanup-renderables.log; latest production visual Play Mode log: /private/tmp/warline-scenario-lab-live-ecs-after-next-switch.log.`
- Latest AD-011 validation: `shadow-project suite passed with AD-011 non-skipped; shadow scene regeneration and smoke validation also passed after adding AD-011 to the selectable scenario list. Follow-up navigation fix changed Previous/Next to move between scenarios instead of wrapping inside AD-001 visual variants, added AD-011 to the checked scene list, and shadow smoke validation passed after proving repeated Next reaches AD-011. Follow-up visual playback fix added production live ECS air-target playback for jet/helicopter/drone variants, added those prefabs to the baked Scenario Lab registry SubScene, and added Play Mode validation proving runtime Next stops the current visual and switches AD-001 -> AD-002 -> AD-011 instead of only changing text. Latest cleanup/variant fix hides the yellow defended-target preview marker during live playback, clears pooled missile trail/impact VFX on test switch, destroys spawned wreck/selection/health-bar visual entities before removing Scenario Lab units, recursively destroys runtime child visual trees not present in the original LinkedEntityGroup, sweeps non-prefab render mesh debris in the lab world, and validates Next through AD-011 jet, helicopter, drone, and attacking jet. Logs: /private/tmp/warline-scenario-lab-ad011-suite-final2.log, /private/tmp/warline-scenario-lab-next-reaches-ad011-smoke.log, /private/tmp/warline-scenario-lab-stop-current-visual-smoke.log, /private/tmp/warline-scenario-lab-next-cleanup-renderables.log. The scene smoke logs still contain known Entities Graphics -nographics NullReference shutdown noise after the pass line.`
- Latest visual collision validation: `main-project PlayMode BattleScenarioLabValidationRunner.ValidateManualSceneLiveEcsPlayback now cycles and validates all four AD-001 live visual variants before passing. Latest log /private/tmp/warline-scenario-lab-all-ad001-visual-variants-noquit.log passed A/B/C/D with visual contact distances A=0.29m, B=0.31m, C=0.32m, D=0.35m, visible/logical ground rocket distance 0.00m, and clear-after-intercept observed.`
- Counting rule: only checklist lines beginning with `- [ ]`, `- [x]`, or `- [~]` count toward checklist progress.

## Goals

- Let Codex run battle scenarios in isolation without needing the full normal game flow.
- Let the user open a separate Unity scene and visually verify the same scenario.
- Make future combat tests data-driven enough that new scenarios do not require one-off runner architecture.
- Produce useful metrics, not only pass/fail logs.
- Keep the scenario lab aligned with the existing ECS architecture, Canvas runtime path, and managed presentation boundary rules.

## Non-Goals

- Do not build a separate combat simulator that bypasses the real ECS systems.
- Do not add random hit chance as the first implementation.
- Do not add MonoBehaviour `Update()` loops for gameplay simulation.
- Do not move GameObject/VFX/camera ownership into unmanaged systems.
- Do not change live unit balance while building the lab.
- Do not depend on UI Toolkit.

## Architecture Rules

- Scenario setup may use authoring objects, ScriptableObjects, editor tooling, and isolated scene fixtures.
- Gameplay execution must use the same ECS systems used by the game wherever practical.
- MonoBehaviours in the lab are allowed only as scene view/bootstrap/reference holders or UI button event receivers. They must not own per-frame gameplay simulation.
- The runner must support deterministic seeds and fixed time steps.
- Every scenario must produce a structured report.
- Reports must be useful in batchmode and in manual Play Mode.
- Visual proof should be optional. Metrics must still work headless.
- Preserve Unity `.meta` files.
- Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for shadow validation when the main project is locked or when manual main-scene state must not be disturbed.

## Proposed Asset And Code Layout

| Area | Proposed path | Purpose |
| --- | --- | --- |
| Isolated scene | `Assets/Game/Scenes/ScenarioLab/BattleScenarioLab.unity` | Manual visual verification scene. |
| Scene fixtures | `Assets/Game/Prefabs/ScenarioLab/` | Camera rigs, marker roots, optional overlay canvas, neutral ground plane. |
| Scenario definitions | `Assets/Game/Configs/ScenarioLab/` | ScriptableObject scenario assets. |
| Runtime scenario code | `Assets/Game/Scripts/ScenarioLab/` | Data models, runner helpers, metrics structs. |
| Editor validation | `Assets/Game/Scripts/Editor/ScenarioLab/` | Batch runners, report writers, visual captures. |
| Tests | `Assets/Tests/Editor/ScenarioLab/` and optional `Assets/Tests/PlayMode/ScenarioLab/` | Deterministic scenario assertions and scene smoke tests. |
| Reports | `/private/tmp/warline-scenario-lab-*.json` | Batch metrics output. |
| Design evidence | `Design/VisualLockLayered/_BattleScenarioLab/` | Screenshots, contacts, captured reports copied from `/private/tmp` when useful. |

## Core Data Model

### `BattleScenarioDefinition`

ScriptableObject describing one reusable scenario:

- `scenarioId`
- `displayName`
- `description`
- `fixedDeltaTime`
- `maxDurationSeconds`
- `randomSeed`
- `cameraPreset`
- `worldBounds`
- `spawnEntries`
- `scenarioVariants`
- `successCriteria`
- `metricsToCapture`

### `BattleScenarioVariant`

Variant data inside a scenario:

- `variantId`
- `label`
- `supportMode`: none, radar near, radar far, satellite, combined
- `incomingThreatKind`: ground missile, jet, drone, helicopter
- `incomingThreatSpeedMultiplier`
- `incomingThreatStartDistance`
- `incomingThreatAltitude`
- `launcherCount`
- `radarDistanceFromLauncher`
- `expectedOutcome`

### `BattleScenarioSpawnEntry`

Data for scenario setup:

- `sourcePrefabKey`
- `configAsset`
- `factionId`
- `worldPosition`
- `worldRotation`
- `initialHealth`
- `initialCommandState`
- optional override components needed only for scenario setup

### `BattleScenarioMetrics`

Per variant report:

- `scenarioId`
- `variantId`
- `seed`
- `durationSeconds`
- `frames`
- `detected`
- `detectionTimeSeconds`
- `trackingStarted`
- `trackingStartTimeSeconds`
- `locked`
- `lockTimeSeconds`
- `interceptorLaunched`
- `launchTimeSeconds`
- `intercepted`
- `interceptTimeSeconds`
- `incomingThreatImpacted`
- `incomingThreatImpactTimeSeconds`
- `incomingThreatDistanceAtDetection`
- `interceptDistanceFromDefendedTarget`
- `closestInterceptorDistanceToThreat`
- `launcherEffectiveRange`
- `launcherEffectiveLockSeconds`
- `launcherEffectiveTrackingQuality`
- `launcherEffectiveTurnRateDegreesPerSecond`
- `radarProviderUsed`
- `satelliteProviderUsed`
- `failureReason`

### `BattleScenarioResult`

Aggregated report:

- scenario metadata
- one row per variant
- pass/fail summary
- raw metrics
- comparison metrics, such as radar-near improvement over no-radar

## Implementation Phases

## Post-Completion AD-011 Air Target Class Expansion

Purpose:
Add automated coverage for the air missile launcher tracking and hitting the major air target classes, including an attacking air unit, without adding parallel gameplay simulation.

- [x] Add `AD-011_AirMissileLauncher_TracksAndHitsAirTargetClasses` scenario definition and runtime dispatch.
- [x] Add variants for jet patrol, helicopter, drone, and attacking jet air targets using existing ECS support, acquisition, fire-control, homing, and impact systems.
- [x] Add focused AD-011 EditMode coverage and include AD-011 in manual scene scenario cycling.
- [x] Validate AD-011 through the shadow-project Scenario Lab suite with all variants non-skipped and passing.
- [x] Add production live visual playback for air-target Scenario Lab variants using the baked jet, helicopter, and drone production prefabs.
- [x] Add Play Mode validation that runtime Next stops the current visual test and switches AD-001 -> AD-002 -> AD-011 with production target/interceptor entities observed.

Expansion notes:

- AD-011 now uses the same production live ECS visual playback contract as AD-002/AD-003-style air-target scenarios: the lab instantiates production launcher/radar/air-target prefabs, configures lab-only scenario component values, and lets existing ECS acquisition, fire-control, homing, impact, VFX, and launcher visuals own the engagement.
- The AD-011 closest-distance metric records the `AirMissileImpactRequestComponent.VisualSeparation` emitted by the real homing system, so the pass gate matches the existing continuous segment proximity-fuse impact logic instead of a weaker point-sampled frame distance.
- Latest AD-011 shadow report: `/private/tmp/warline-scenario-lab-AD-011_AirMissileLauncher_TracksAndHitsAirTargetClasses.json`.

## Post-Completion Manual Scene Visual Correction

Purpose:
Fix the gap found during user manual verification: the scene had valid metrics but did not visually show the engagement.

- [x] Add an AD-001 visual playback component with no gameplay `Update()` loop.
- [x] Wire manual Play bootstrap to pass selected/default AD-001 variant metrics into the visual playback.
- [x] Regenerate the manual scene with ground launcher, air launcher, radar, defended target, incoming missile, interceptor, trails, launch flashes, intercept explosion, and camera cuts.
- [x] Reduce the metrics overlay footprint so it supports, rather than hides, visual validation.
- [x] Extend manual scene smoke validation to require the visual playback component, visual objects, and serialized references.

Correction notes:

- The first manual scene visual was a deterministic presentation replay driven by the same AD-001 variant metrics.
- Automated pass/fail still comes from the fixed-step ECS runner.
- The proxy replay is now superseded by the production live ECS visual correction below.

## Post-Completion Production ECS Visual Correction

Purpose:
Fix the second manual-verification gap: the visual scene must use actual production launcher prefabs, actual missile child visuals, real launcher timing, and existing ECS systems instead of programmer proxy objects or parallel GameObject gameplay.

- [x] Replace generated primitive launcher/missile/trail props with production `Unit_Veh_Missle_Launcher_Ground`, `Unit_Veh_Missle_Launcher_Air`, and `Unit_Veh_Radar_Tank` prefab instances.
- [x] Replace metric-driven proxy animation with live ECS seeding that only resets state, sets factions/positions/support, and starts `GroundMissileLauncherStateComponent` in `Preparing`.
- [x] Preserve ground launcher battery open/rotate/post-open/fire timing by using `GroundMissileLauncherTiming.PrepareAndHoldSeconds(launcher.PrepareSeconds)` and the existing `GroundMissileLauncherVisualSystem`/`GroundMissileLauncherFireSystem`.
- [x] Preserve air launcher turret aim, missile child detach, homing, trail, and impact VFX by using the existing air missile launcher ECS systems.
- [x] Update manual scene builder and smoke validation to require production prefab instances and live ECS playback references.
- [x] User Play Mode visual verification confirms battery opening/rotation, actual rocket launch, air missile launch, VFX/trails, camera framing, and Run Again behavior.

Correction notes:

- The manual scene now uses a dedicated Scenario Lab baked prefab registry SubScene with explicit production launcher/radar prefab references.
- The playback component does not own gameplay simulation and has no `Update()` loop; it is a scene bootstrap/camera presenter that seeds the existing ECS systems.
- Shadow-project Play Mode validation now confirms the scene resolves the production registry, instantiates launcher entities, creates both live missile projectiles, keeps the visible ground rocket synced to the logical projectile at `0.00m` sampled offset, samples a near-contact intercept at `2.33m`, and keeps the ground missile at `8.33m` max altitude in the validated run. Manual verification is still needed for full in-editor cinematic visual quality.

## Phase 0: Contract And Inventory

Purpose:
Lock the scenario lab contract before adding code.

- [x] Confirm existing missile/radar systems used by AD-001.
- [x] Confirm existing configs and prefabs for air missile launcher, ground missile launcher, radar tank, drone, and any jet/air unit candidate.
- [x] Decide whether AD-001 starts from an already spawned incoming `GroundMissileProjectileComponent` or from a real enemy ground launcher firing.
- [x] Confirm which systems must run in the scenario world for air missile interception.
- [x] Confirm which systems must run in the visual scene for VFX only.
- [x] Define metric names and report JSON schema.
- [x] Define failure reason enum/string list.
- [x] Add this tracker to any relevant automation prompt only after implementation starts.

Phase 0 notes:

- Existing systems found:
  - `AirMissileLauncherSupportLinkSystem`
  - `AirMissileLauncherTargetAcquisitionSystem`
  - `AirMissileLauncherFireControlSystem`
  - `AirMissileHomingProjectileSystem`
  - `AirMissileImpactSystem`
  - `GroundMissileProjectileFlightSystem`
  - `GroundMissileImpactSystem`
- Existing validation runner found:
  - `Assets/Game/Scripts/Editor/AirMissileLauncherValidationRunner.cs`
- Existing configs/prefabs found:
  - `Assets/Game/Configs/Weapons/AirMissileLauncher_Air_Config.asset`
  - `Assets/Game/Configs/Weapons/GroundMissileLauncher_Ground_Config.asset`
  - `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset`
  - `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset`
  - `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Radar_Tank.asset`
  - `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Drone_Config.asset`
  - `Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Air.prefab`
  - `Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Ground.prefab`
  - `Assets/Game/Prefabs/Vehicles/Unit_Veh_Radar_Tank.prefab`
  - `Assets/Game/Prefabs/Vehicles/Unit_Veh_Drone.prefab`
- AD-001 implementation decision:
  - The first automated metric fixture starts from an already spawned incoming `GroundMissileProjectileComponent` plus `MissileInterceptionTargetComponent` so the air-defense interception loop can be isolated and deterministic.
  - A later visual/integration extension can add the real enemy ground launcher firing path after the interception metrics are trustworthy.
  - Required automated ECS path is support link, target acquisition, fire control, homing projectile, air missile impact, and ground missile projectile flight/impact where the incoming missile needs movement.
  - Visual scene VFX is not part of Phase 1/2 correctness; VFX presentation remains passive/managed and is deferred to the isolated manual scene/proof phases.

## Phase 1: Scenario Lab Core

Purpose:
Build the reusable scenario runner and metrics model with no visual scene dependency.

- [x] Add `BattleScenarioDefinition` ScriptableObject type.
- [x] Add serializable `BattleScenarioVariant` type.
- [x] Add serializable `BattleScenarioSpawnEntry` type.
- [x] Add `BattleScenarioMetrics` struct/class.
- [x] Add `BattleScenarioResult` aggregate type.
- [x] Add `BattleScenarioFailureReason` enum.
- [x] Add deterministic fixed-step runner helper.
- [x] Add report writer that emits stable JSON.
- [x] Add scenario result comparison helper.
- [x] Add unit-test coverage for report formatting and comparison calculations.
- [x] Run `git diff --check`.

Acceptance:

- A scenario can be constructed in memory and run without opening any scene.
- A report can be emitted in batchmode.
- The runner can execute multiple variants from one scenario definition.

## Phase 2: AD-001 Automated Air Defense Scenario

Purpose:
Add the first concrete test: air missile launcher interception against an incoming ground missile, comparing no support with radar support nearby.

Scenario ID:
`AD-001_AirMissileLauncher_InterceptIncomingGroundMissile_RadarComparison`

Core question:
Does nearby radar improve the air missile launcher's interception performance through earlier detection, shorter effective lock time, better tracking, or higher success under harder geometry?

Initial variants:

| Variant | Setup | Expected result |
| --- | --- | --- |
| `AD-001-A-NoSupport-Normal` | One friendly air missile launcher, incoming enemy ground missile, no radar provider. | Intercepts if geometry is favorable; metrics establish baseline. |
| `AD-001-B-RadarNear-Normal` | Same as A, plus friendly radar tank inside support radius. | Must detect no later than A, lock faster than A, and intercept at least as reliably as A. |
| `AD-001-C-NoSupport-FastThreat` | Same as A, faster incoming missile. | May fail or intercept late; metrics establish stress baseline. |
| `AD-001-D-RadarNear-FastThreat` | Same as C, plus friendly radar tank inside support radius. | Must improve outcome or failure margin compared with C. |

Initial deterministic setup:

- Friendly air missile launcher at `(0, 0, 0)`.
- Friendly defended target marker at `(110, 0, 0)`.
- Incoming enemy ground missile starts at `(170, 12, 0)` and moves toward defended target.
- Radar-near provider starts at `(8, 0, 0)` with the existing radar support values.
- Simulation uses fixed timestep `0.05s` or `0.1s`.
- Scenario time limit starts at `12s`.

Checklist:

- [x] Add AD-001 scenario definition asset.
- [x] Add in-memory AD-001 fixture builder for editmode validation.
- [x] Spawn friendly air missile launcher with `AirMissileLauncherComponent`, `AirMissileLauncherStateComponent`, `AirDefenseSupportLinkComponent`, `Faction`, `UnitHealth`, and `LocalTransform`.
- [x] Spawn incoming enemy ground missile with `GroundMissileProjectileComponent`, `MissileInterceptionTargetComponent`, `Faction`-equivalent faction id, and `LocalTransform`.
- [x] Spawn radar support provider for radar variants using `AirDefenseSupportProviderComponent`.
- [x] Run support, acquisition, fire control, homing, impact, and any required projectile movement systems in fixed order.
- [x] Capture detection, tracking, lock, launch, intercept, impact, and failure metrics.
- [x] Assert radar-near effective range is greater than no-support effective range.
- [x] Assert radar-near effective lock seconds is less than no-support lock seconds.
- [x] Assert radar-near effective tracking quality is greater than no-support tracking quality.
- [x] Assert normal radar-near variant intercepts.
- [x] Assert fast radar-near variant is equal or better than fast no-support by success, intercept time, closest distance, or defended-target distance.
- [x] Emit `/private/tmp/warline-scenario-lab-ad001-air-defense.json`.
- [x] Add focused editmode runner method for batchmode.
- [x] Add focused test method for Unity Test Runner.
- [x] Run AD-001 focused validation in the main project or shadow project.
- [x] Run `git diff --check`.

Acceptance:

- AD-001 can run in batchmode without the full menu/match scene.
- AD-001 produces JSON metrics for all variants.
- Radar-near improvement is numerically visible, even if no-support also succeeds in easy variants.
- Failures include actionable reasons: no detection, no lock, no launch, interceptor timeout, incoming missile impacted target, target entity missing, invalid setup.

## Phase 3: Isolated Manual Scene

Purpose:
Create a separate scene that the user can open and run to visually verify the same AD-001 scenario.

- [x] Create `Assets/Game/Scenes/ScenarioLab/BattleScenarioLab.unity`.
- [x] Add a neutral ground plane or simple terrain reference.
- [x] Add a fixed camera rig with a clear view of launcher, radar, incoming missile path, and defended target.
- [x] Add a scenario lab root GameObject with reference-only/bootstrap components.
- [x] Add a passive Canvas overlay for scenario name, variant, phase, result, and key metrics.
- [x] Add simple scene markers for defended target, launcher, radar, and incoming threat path.
- [x] Wire the scene to run the selected scenario on Play without depending on menu routing.
- [x] Add a manual variant selector if this can be done without runtime C# complexity.
- [x] Add restart support through an editor menu item or button event, not through a custom gameplay `Update()` loop.
- [x] Save a screenshot proof for AD-001 no-support and radar-near variants.
- [x] Run scene smoke validation.
- [x] Run `git diff --check`.

Acceptance:

- The user can open `BattleScenarioLab.unity`, press Play, and watch AD-001.
- The visual scene uses the same scenario setup as the automated runner.
- The scene overlay displays the same metrics that the JSON report records.
- The scene does not require the main menu, UI Toolkit, or normal match bootstrap.

## Phase 4: Visual Proof Capture

Purpose:
Allow Codex to capture visual evidence without manual scene interaction.

- [x] Add editor-only visual proof capture runner for AD-001.
- [x] Capture no-support normal variant.
- [x] Capture radar-near normal variant.
- [x] Capture radar-near fast-threat variant if visually useful.
- [x] Save images under `Design/VisualLockLayered/_BattleScenarioLab/AD-001/`.
- [x] Add a small contact sheet comparing no-radar and radar-near.
- [x] Record latest artifact paths in this tracker.
- [x] Run `git diff --check`.

Acceptance:

- Batch capture opens the scenario lab scene or creates an isolated temporary preview scene.
- Captures show launcher, incoming missile, interceptor, radar, and intercept/explosion when available.
- Captures are not used as the only correctness proof; metrics remain authoritative.

## Phase 5: Scenario Suite Expansion

Purpose:
Make adding future battle scenarios repeatable.

- [x] Add template docs for new scenario definitions.
- [x] Add helper methods for common unit spawns.
- [x] Add helper methods for common projectile/threat spawns.
- [x] Add helper methods for support providers.
- [x] Add reusable metrics comparison helpers.
- [x] Add aggregate suite runner that runs all `ScenarioLab` scenarios.
- [x] Add report index JSON containing all scenario results.
- [x] Add CI/batchmode command documentation.
- [x] Run suite validation.

Initial future scenario backlog:

- [x] `AD-002` enemy jet enters air-defense range, no radar versus radar-near.
- [x] `AD-003` enemy drone scout enters radar range and is tracked/intercepted.
- [x] `AD-004` two incoming ground missiles, one friendly air missile launcher.
- [x] `AD-005` two incoming ground missiles, two friendly air missile launchers.
- [x] `AD-006` radar destroyed or disabled mid-scenario.
- [x] `AD-007` incoming missile starts outside base range but inside radar-extended range.
- [x] `AD-008` saturated mixed attack: drone plus ground missile.
- [x] `AD-009` support comparison: no support, radar, satellite, radar plus satellite.
- [x] `AD-010` interception geometry sweep: side shot, head-on shot, tail chase, crossing shot.
- [x] `GM-001` ground missile launcher fires visible rocket and damages target in isolated scene.
- [x] `DR-001` drone recon detection and threat warning behavior.

## Phase 6: Balance And Tuning Loop

Purpose:
Use scenario metrics to tune the air-defense system only after tests are trustworthy.

- [x] Establish baseline metrics from current configs.
- [x] Decide target outcomes for normal and fast threats.
- [x] Tune config values only if measured outcomes miss target behavior.
- [x] Prefer tuning existing deterministic fields first:
  - detection range
  - lock seconds
  - tracking quality
  - missile turn rate
  - missile lifetime
  - proximity fuse radius
  - radar support range bonus
  - radar lock multiplier
  - radar tracking bonus
  - radar turn-rate bonus
- [x] Re-run AD-001 after every tuning change.
- [x] Update reports and tracker notes.
- [x] Add stochastic hit chance only if deterministic geometry/timing cannot produce the desired gameplay feel.

Phase 6 tuning decision:
Keep V1 deterministic. The approved tuning slice promotes the Scenario-Lab-proven radar support values into shared gameplay constants and the live air-missile config asset: radar range bonus `100`, radar lock multiplier `0.5`, radar tracking bonus `0.2`, and radar turn-rate bonus `50`. No stochastic hit chance was added because the deterministic geometry/timing path is still measurable and debuggable.

Acceptance:

- Tuning changes are backed by scenario report deltas.
- The user can visually verify tuned behavior in the isolated scene.
- No tuning change is made only because one manual play looked good or bad.

## AD-001 Detailed Expected Report Shape

Example JSON fields:

```json
{
  "scenarioId": "AD-001_AirMissileLauncher_InterceptIncomingGroundMissile_RadarComparison",
  "generatedAtUtc": "2026-06-26T00:00:00Z",
  "fixedDeltaTime": 0.05,
  "variants": [
    {
      "variantId": "AD-001-A-NoSupport-Normal",
      "detected": true,
      "detectionTimeSeconds": 0.15,
      "locked": true,
      "lockTimeSeconds": 0.55,
      "interceptorLaunched": true,
      "launchTimeSeconds": 0.7,
      "intercepted": true,
      "interceptTimeSeconds": 2.15,
      "launcherEffectiveRange": 220.0,
      "launcherEffectiveLockSeconds": 0.35,
      "launcherEffectiveTrackingQuality": 0.75,
      "launcherEffectiveTurnRateDegreesPerSecond": 220.0,
      "radarProviderUsed": false,
      "failureReason": "None"
    }
  ],
  "comparisons": [
    {
      "baselineVariantId": "AD-001-A-NoSupport-Normal",
      "supportedVariantId": "AD-001-B-RadarNear-Normal",
      "radarImprovedDetectionTime": true,
      "radarImprovedLockTime": true,
      "radarImprovedOrMatchedOutcome": true
    }
  ]
}
```

## Validation Commands

Draft command shape after implementation:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture \
  -executeMethod BattleScenarioLabValidationRunner.RunAirDefenseAd001 \
  -logFile /private/tmp/warline-scenario-lab-ad001.log
```

Shadow-project variant:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 \
  -executeMethod BattleScenarioLabValidationRunner.RunAirDefenseAd001 \
  -logFile /private/tmp/warline-scenario-lab-ad001-shadow.log
```

## Risks And Guardrails

| Risk | Guardrail |
| --- | --- |
| Scenario runner becomes a parallel gameplay implementation. | Use existing ECS systems and components; only setup/metrics are lab-specific. |
| Manual scene differs from automated test. | Both must consume the same scenario definition and runner. |
| Metrics pass while visuals are broken. | Add Phase 4 visual proof captures after Phase 2 metrics pass. |
| Visual scene works but batch validation cannot run. | Metrics runner must be scene-independent before scene work starts. |
| Radar improvement is hidden because no-support already always wins. | Include stress variants where radar can change the margin or outcome. |
| Random hit chance makes failures hard to debug. | Keep V1 deterministic; add seeded randomness only after deterministic behavior is measured. |
| MonoBehaviour gameplay loop creeps in. | Restrict lab MonoBehaviours to bootstrap/view/input-event roles; simulation advances through runner/ECS systems. |

## Latest Notes

- 2026-06-26: Initial tracker created. AD-001 is defined as the first scenario target.
- 2026-06-26: Phase 0 inventory/contract completed. Added `Assets/Game/Scripts/ScenarioLab/BattleScenarioLabModels.cs` with definition, variants, spawn entries, metrics, result, failure reasons, report JSON, comparison helper, and deterministic fixed-step runner. Added focused editor tests in `Assets/Tests/Editor/ScenarioLab/BattleScenarioLabModelTests.cs`. Validation: `git diff --check` passed; Unity EditMode `BattleScenarioLabModelTests` passed 5/5. Next target is the AD-001 automated ECS fixture builder.
- 2026-06-26: Phase 2 automated AD-001 runner implemented in `Assets/Game/Scripts/ScenarioLab/BattleScenarioAd001Runner.cs`. It runs four code-backed variants in isolated ECS worlds using the real support link, ground missile flight, target acquisition, fire control, homing, air impact, and ground impact systems. Added `Assets/Game/Scripts/Editor/ScenarioLab/BattleScenarioLabValidationRunner.cs` for batch report generation and `Assets/Tests/Editor/ScenarioLab/BattleScenarioAd001RunnerTests.cs` for focused Unity Test Runner coverage. Report path: `/private/tmp/warline-scenario-lab-ad001-air-defense.json`. Validation: model tests 5/5 passed, AD-001 tests 1/1 passed, batch execute method passed, and `git diff --check` passed.
- 2026-06-26: Added asset-backed AD-001 scenario definition at `Assets/Game/Configs/ScenarioLab/AD001_AirMissileLauncher_InterceptIncomingGroundMissile_RadarComparison.asset`. Split `BattleScenarioDefinition` into its own matching script file so Unity can create valid ScriptableObject assets. Updated the batch runner to load the asset when present and added focused coverage for `RunDefinition_UsesAd001ScenarioAsset`. Validation: AD-001 tests 2/2 passed, asset creation execute method saved the asset without the previous bad-script warning, and asset-backed batch AD-001 report generation passed.
- 2026-06-26: Created the Phase 3 manual scene shell at `Assets/Game/Scenes/ScenarioLab/BattleScenarioLab.unity` using `BattleScenarioLabSceneBuilder.CreateManualSceneShell`. Added passive `BattleScenarioLabSceneReferences` with serialized references only and no `Update`. The scene has a neutral ground plane, fixed camera, AD-001 launcher/radar/incoming/defended-target markers, and a reference to the AD-001 definition asset. Remaining Phase 3 work: passive Canvas overlay, Play wiring to run the scenario, optional variant selector, restart support, screenshot proof, and scene smoke validation.
- 2026-06-26: Added passive manual-scene metrics UI with `BattleScenarioLabOverlayView` and one-shot `BattleScenarioLabPlayBootstrap`. The bootstrap runs the asset-backed AD-001 runner on Play and writes pass/fail, variant timings, and radar comparison deltas into the overlay without adding a gameplay `Update()` loop. Regenerated `BattleScenarioLab.unity` with the overlay/bootstrap wiring. Validation: `git diff --check` passed before tracker update and Unity EditMode `BattleScenarioAd001RunnerTests` passed 2/2 after the overlay/bootstrap compile path.
- 2026-06-26: Added `BattleScenarioLabValidationRunner.ValidateManualSceneSmoke`, which opens `BattleScenarioLab.unity`, verifies the root, camera, markers, overlay text references, bootstrap references, and runs the same AD-001 definition used by automated metrics. Validation: smoke executeMethod passed and `git diff --check` passed. Unity still logs non-blocking licensing access-token noise in batchmode.
- 2026-06-26: Added a passive `RestartScenarioButton` to the manual scene overlay and wired it to `BattleScenarioLabPlayBootstrap.RunScenario` through a persistent button event. Updated smoke validation to assert the button and listener exist. Validation: scene regeneration passed, smoke validation passed, and `git diff --check` passed.
- 2026-06-26: Added `BattleScenarioLabVisualProofCapture.CaptureAd001VisualProof`, which opens the isolated scene, runs the asset-backed AD-001 definition, positions proof markers for no-support normal, radar-near normal, and radar-near fast-threat variants, captures PNGs, and builds a contact sheet. Artifacts: `Design/VisualLockLayered/_BattleScenarioLab/AD-001/AD-001-A-NoSupport-Normal.png`, `Design/VisualLockLayered/_BattleScenarioLab/AD-001/AD-001-B-RadarNear-Normal.png`, `Design/VisualLockLayered/_BattleScenarioLab/AD-001/AD-001-D-RadarNear-FastThreat.png`, and `Design/VisualLockLayered/_BattleScenarioLab/AD-001/ad001_visual_proof_contact_sheet.png`. Validation: visual proof executeMethod passed and `git diff --check` passed.
- 2026-06-26: Added `Design/Architecture/battle_scenario_lab_new_scenario_template.md` with the new-scenario contract, definition checklist, runner checklist, manual-scene checklist, suite-registration notes, and batch commands. Added `BattleScenarioLabSuiteRunner.RunScenarioSuite`, which discovers `BattleScenarioDefinition` assets under `Assets/Game/Configs/ScenarioLab`, runs registered scenario runners, writes individual reports, and writes `/private/tmp/warline-scenario-lab-suite-index.json`. Validation: suite executeMethod passed with AD-001 discovered, run, and reported as passed; `git diff --check` passed before tracker update.
- 2026-06-26: Added `BattleScenarioEcsSpawnHelpers` for common isolated ECS setup: air missile launcher unit spawn, incoming ground missile threat spawn, and air-defense support provider spawn. Refactored AD-001 to use these helpers without changing scenario constants or expected outcomes. Added `BattleScenarioEcsSpawnHelpersTests` for helper-created components and reran broad `BattleScenario` EditMode validation. Validation: `BattleScenario` filter passed 10/10, suite executeMethod passed after the helper refactor, and `git diff --check` passed.
- 2026-06-26: Added `BattleScenarioLabBaselineCapture.CaptureAd001BaselineMetrics` and captured current AD-001 baseline metrics without changing live combat balance. Artifacts: `Design/Architecture/battle_scenario_lab_ad001_baseline_metrics.md` and `Design/Architecture/battle_scenario_lab_ad001_baseline_metrics.json`. Current target outcomes recorded in the baseline: radar-near normal intercepts, radar-near normal improves lock time and matches/improves outcome, and radar-near fast-threat improves detection and matches/improves outcome. Validation: baseline capture executeMethod passed, suite executeMethod passed after capture, and `git diff --check` passed.
- 2026-06-26: Implemented `AD-002_AirMissileLauncher_InterceptEnemyJet_RadarComparison`. Added `BattleScenarioAd002Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd002DefinitionAsset`, AD-002 suite runner dispatch, AD-002 scenario asset, and focused `BattleScenarioAd002RunnerTests`. AD-002 uses a real ECS air target with `UnitAirMovement`, moves it through the fixed-step runner, and uses the existing support, acquisition, fire-control, homing, and air-impact systems. Validation: AD-002 asset creation passed, broad `BattleScenario` EditMode filter passed 13/13, suite executeMethod passed with AD-001 and AD-002 both non-skipped and passing, and `git diff --check` passed.
- 2026-06-26: Implemented `AD-003_AirMissileLauncher_TrackAndInterceptDroneScout_RadarComparison`. Added `BattleScenarioAd003Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd003DefinitionAsset`, AD-003 suite runner dispatch, AD-003 scenario asset, and focused `BattleScenarioAd003RunnerTests`. AD-003 uses a real ECS air target with `UnitAirMovement`, moves a slower drone scout from just outside base launcher range into air-defense range, and uses the existing support, acquisition, fire-control, homing, and air-impact systems. Validation: AD-003 asset creation passed, broad `BattleScenario` EditMode filter passed 15/15, suite executeMethod passed with AD-001, AD-002, and AD-003 all non-skipped and passing, and `git diff --check` passed.
- 2026-06-26: Implemented `AD-004_AirMissileLauncher_InterceptTwoIncomingGroundMissiles_RadarComparison`. Added `BattleScenarioAd004Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd004DefinitionAsset`, AD-004 suite runner dispatch, AD-004 scenario asset, and focused `BattleScenarioAd004RunnerTests`. AD-004 uses two staggered real ECS incoming `GroundMissileProjectileComponent` threats against one friendly air missile launcher, with no-support and radar-near variants. Latest report: no-support intercepted both threats at 4.00s with 0.90s lock time, radar-near intercepted both threats at 3.30s with 0.45s lock time, and no incoming threat impacted. Validation: AD-004 asset creation passed, suite executeMethod passed with AD-001, AD-002, AD-003, and AD-004 all non-skipped and passing, and `git diff --check` passed. Unity Test Runner CLI returned success but skipped writing the XML result file during the AD-004 focused test attempt, so focused AD-004 test execution remains to be retried if the test runner path is required.
- 2026-06-26: Implemented `AD-005_TwoAirMissileLaunchers_InterceptTwoIncomingGroundMissiles_RadarComparison`. Added `BattleScenarioAd005Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd005DefinitionAsset`, AD-005 suite runner dispatch, AD-005 scenario asset, and focused `BattleScenarioAd005RunnerTests`. AD-005 uses two friendly air missile launcher entities and two staggered real ECS incoming `GroundMissileProjectileComponent` threats, with aggregate launcher metrics captured from either launcher. Latest report: no-support intercepted both threats at 4.15s with 0.90s lock time, radar-near intercepted both threats at 3.30s with 0.45s lock time, and no incoming threat impacted. Validation: AD-005 asset creation passed, suite executeMethod passed with AD-001 through AD-005 all non-skipped and passing, and `git diff --check` passed.
- 2026-06-26: Implemented `AD-006_AirMissileLauncher_RadarDisabledMidScenario_RadarComparison`. Added `BattleScenarioAd006Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd006DefinitionAsset`, AD-006 suite runner dispatch, AD-006 scenario asset, and focused `BattleScenarioAd006RunnerTests`. AD-006 compares no support, persistent nearby radar, and nearby radar disabled mid-scenario by destroying the isolated radar provider entity at 0.70s so the existing support link system recomputes base launcher values. Latest report: no-support intercepted at 1.90s with 0.90s lock time; persistent radar intercepted at 1.50s with 0.45s lock time; disabled radar also intercepted at 1.50s after using radar early, and final effective range/lock/tracking returned to the no-support baseline. Validation: AD-006 asset creation passed, suite executeMethod passed with AD-001 through AD-006 all non-skipped and passing, and `git diff --check` passed.
- 2026-06-26: Implemented `AD-007_AirMissileLauncher_ThreatStartsInsideRadarExtendedRange_RadarComparison`. Added `BattleScenarioAd007Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd007DefinitionAsset`, AD-007 suite runner dispatch, AD-007 scenario asset, and focused `BattleScenarioAd007RunnerTests`. AD-007 starts the incoming ground missile at 220 units, outside the no-support 140 range but inside the radar-near 230 effective range. Latest report: no-support detected at 2.75s around 139.94 units and intercepted at 4.55s; radar-near detected at 0.00s around 218.79 units, locked 3.20s faster, and intercepted at 2.10s. Validation: AD-007 asset creation passed, suite executeMethod passed with AD-001 through AD-007 all non-skipped and passing, and `git diff --check` passed.
- 2026-06-26: Implemented `AD-008_AirMissileLauncher_SaturatedMixedDroneAndGroundMissile_RadarComparison`. Added `BattleScenarioAd008Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd008DefinitionAsset`, AD-008 suite runner dispatch, AD-008 scenario asset, and focused `BattleScenarioAd008RunnerTests`. AD-008 runs one friendly air missile launcher against a simultaneous incoming ground missile and enemy drone scout, and only passes when the missile is intercepted and the drone is destroyed. Latest report: no-support cleared both threats at 4.05s with detection at 0.25s and lock at 1.15s; radar-near cleared both threats at 3.10s with detection at 0.00s and lock at 0.45s. Validation: AD-008 asset creation passed, suite executeMethod passed with AD-001 through AD-008 all non-skipped and passing, and `git diff --check` passed.
- 2026-06-26: Implemented `AD-009_AirMissileLauncher_SupportModeComparison_RadarSatelliteCombined`. Added `BattleScenarioAd009Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd009DefinitionAsset`, AD-009 suite runner dispatch, AD-009 scenario asset, and focused `BattleScenarioAd009RunnerTests`. AD-009 compares no support, radar, satellite, and radar plus satellite against the same incoming ground missile. Latest report: no-support intercepted at 4.55s with 140 range and 0.90s effective lock; radar intercepted at 2.10s with 230 range and 0.45s lock; satellite intercepted at 2.20s with 260 range and 0.585s lock; combined support linked both providers, reached 280 range, 1.00 tracking quality, 240 deg/s turn rate, and intercepted at 2.10s. Validation: AD-009 asset creation passed, suite executeMethod passed with AD-001 through AD-009 all non-skipped and passing, and `git diff --check` passed.
- 2026-06-26: Implemented `AD-010_AirMissileLauncher_InterceptionGeometrySweep`. Added `BattleScenarioAd010Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateAd010DefinitionAsset`, AD-010 suite runner dispatch, AD-010 scenario asset, and focused `BattleScenarioAd010RunnerTests`. AD-010 runs four nearby-radar-supported ground-missile interception geometries through the real ECS support, acquisition, fire-control, homing, air-impact, ground-flight, and ground-impact systems: head-on, side shot, tail chase, and crossing shot. Latest report: head-on intercepted at 1.40s, side shot at 1.45s, tail chase at 1.40s, and crossing shot at 1.55s; all variants detected at 0.00s, locked at 0.40s, launched at 0.45s, used radar support, and had no incoming impact. Validation: AD-010 asset creation passed, suite executeMethod passed with AD-001 through AD-010 all non-skipped and passing, focused EditMode test command returned 0 but emitted no XML result file, and `git diff --check` passed.
- 2026-06-26: Implemented `GM-001_GroundMissileLauncher_FiresVisibleRocketAndDamagesTarget`. Added `BattleScenarioGm001Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateGm001DefinitionAsset`, GM-001 suite runner dispatch, GM-001 scenario asset, focused `BattleScenarioGm001RunnerTests`, and common `BattleScenarioEcsSpawnHelpers` methods for isolated ground missile launchers and ground targets. GM-001 arms a temporary enemy ground missile launcher, adds a rocket visual slot, and runs the real `GroundMissileLauncherFireSystem`, `GroundMissileFlyingRocketVisualSystem`, `GroundMissileProjectileFlightSystem`, and `GroundMissileImpactSystem` against a friendly ground target. Latest report: target assigned at 0.00s, launcher reached launch phase at 1.05s, projectile and flying rocket visual were observed, impact damage landed at 2.20s, closest projectile-to-target distance was 0.91, and target damage was 140. Validation: GM-001 asset creation passed, suite executeMethod passed with AD-001 through AD-010 plus GM-001 all non-skipped and passing, focused EditMode test command returned 0 but emitted no XML result file, and `git diff --check` passed.
- 2026-06-26: Implemented `DR-001_DroneReconDetectionAndThreatWarning`. Added `BattleScenarioDr001Runner`, `BattleScenarioLabValidationRunner.CreateOrUpdateDr001DefinitionAsset`, DR-001 suite runner dispatch, DR-001 scenario asset, and focused `BattleScenarioDr001RunnerTests`. DR-001 uses the real `ThreatDetectionWarningSystem` and `ThreatWarningRuntimeState` with a player air-threat detector and an enemy drone threat that is first outside detector radius, then moved inside radius with a path goal toward the sensor. Latest report: the out-of-range tick at 0.00s stayed quiet, the drone entered the 8-cell air detector at 7 cells, an air warning was requested at 0.05s, ETA was 3.5s, and one threat was counted. Validation: DR-001 asset creation passed, suite executeMethod passed with AD-001 through AD-010 plus GM-001 and DR-001 all non-skipped and passing, focused EditMode test command returned 0 but emitted no XML result file, and `git diff --check` passed.
- 2026-06-27: Phase 6 deterministic tuning applied after user approval. Centralized radar/satellite support values in `AirDefenseSupportTuning`, updated live `AirMissileLauncher_Air_Config.asset` radar support values to the Scenario-Lab-proven values, and routed `UnitGridAuthoring`, `BuildingRuntimeEntityCompositionSystemHelper`, and Scenario Lab support providers through the shared constants. No stochastic hit chance was added. Validation: static old-value scan clean and `git diff --check` passed; post-tuning Unity suite rerun is blocked before tests start by the known licensing loop in both the main project and `/Users/farhad/Projects/WarlineCapture-CodexUnity1` shadow-project workaround.
- 2026-06-27: Added the optional AD-001 manual variant selector without adding any gameplay `Update()` loop. `BattleScenarioLabPlayBootstrap` now populates a Unity UI dropdown and can run either all AD-001 variants or one selected AD-001 variant through the existing `BattleScenarioAd001Runner`. Regenerated `Assets/Game/Scenes/ScenarioLab/BattleScenarioLab.unity` and extended smoke validation to assert `VariantSelector` options and bootstrap wiring. Validation: `BattleScenarioLabSceneBuilder.CreateManualSceneShell` saved the scene, `BattleScenarioLabValidationRunner.ValidateManualSceneSmoke` passed, and `git diff --check` passed.
- 2026-06-27: Post-tuning validation reran successfully after the licensing issue cleared. Latest AD-001 report: no-support normal intercepts at 1.85s with 140 range and 0.90s lock; radar-near normal intercepts at 1.45s with 240 range and 0.45s lock; no-support fast detects at 1.20s and intercepts at 2.70s; radar-near fast detects at 0.00s and intercepts at 1.65s. Latest AD-009 report confirms tuned radar reaches 240 range, 0.45s effective lock, 0.95 tracking, and 190 deg/s turn rate; combined support reaches 280 range, 1.00 tracking, and 240 deg/s turn rate. Validation: `BattleScenarioLabSuiteRunner.RunScenarioSuite` passed with AD-001 through AD-010 plus GM-001 and DR-001 all non-skipped; suite index written to `/private/tmp/warline-scenario-lab-suite-index.json`.
- 2026-06-27: Reproduced the user-reported manual Play Mode failure where live visual playback could not find baked production launcher entities. Replaced the unreliable empty registry SubScene with `BattleScenarioLabUnitPrefabRegistryAuthoring` in the authoring assembly, regenerated `BattleScenarioLabBakedPrefabs.unity` with the production ground launcher, air launcher, and radar prefabs, increased first-run SubScene wait tolerance to 30s, and added `BattleScenarioLabValidationRunner.ValidateManualSceneLiveEcsPlayback`. Validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: scene builder passed, smoke validation passed, and live Play Mode validation passed after observing the production registry, instantiated launcher entities, and projectile creation.
- 2026-06-27: Fixed the user-reported visual intercept mismatch where the ground missile flew too high and the air missile explosion appeared away from the target. The Scenario Lab live visual playback now applies lab-only ECS component overrides after instantiating the real production prefabs: ground launcher arc is capped to `8`, and air missile proximity fuse is capped to `0.75`, while preserving the existing production launcher timing, projectile systems, models, trails, and VFX path. Updated `AirMissileHomingProjectileSystem` so incoming-ground-missile intercept VFX is queued at the incoming missile position instead of the interceptor position. Strengthened `ValidateManualSceneLiveEcsPlayback` so it fails unless it observes both live projectiles, near-contact distance, and acceptable ground missile altitude. Validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: live Play Mode validation passed with closest sampled missile distance `2.36m` and max ground missile altitude `7.29m`; `git diff --check` passed. Known batchmode-only Entities Graphics `-nographics` shutdown noise remains non-blocking.
- 2026-06-27: Corrected the follow-up visual mismatch reported from the in-editor screenshot: the visible ground rocket mesh was following a separate high Bezier arc while the logical interceptable projectile followed a lower configured arc. `GroundMissileFlyingRocketVisualSystem` now evaluates the same arced lerp as `GroundMissileProjectileFlightSystem`, and `GroundMissileLauncherFireSystem` seeds the logical projectile from the selected rocket visual slot when available. Tightened the Scenario Lab live fuse cap to `0.25` and updated `AirMissileHomingProjectileSystem` to use closest approach along the frame movement segment. Strengthened live validation to require a visible ground rocket and a synced visible/logical ground missile distance. Validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: live Play Mode validation passed with `groundVisual=0.00m`, closest sampled air/ground missile distance `2.33m`, and max ground missile altitude `8.33m`; `git diff --check` passed.
- 2026-06-27: Corrected the third visual mismatch report where the ground rocket and air missile visually crossed but the air missile exploded late. The prior automated pass was too weak because it measured logical projectile distance, not rendered visual-to-visual contact. `AirMissileHomingProjectileSystem` now samples `GroundMissileFlyingRocketVisualComponent` positions keyed by launcher and uses that rendered ground rocket path as the incoming missile target when available; on intercept it snaps the interceptor transform to the computed closest visual impact point before queuing impact. `AirMissileImpactRequestComponent` and `MissileInterceptedComponent` now carry `VisualSeparation`, and `BattleScenarioLabValidationRunner.ValidateManualSceneLiveEcsPlayback` fails unless the visual air missile and visible ground rocket contact distance is within `0.75m`. Validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: live Play Mode validation passed with `closest=0.26m`, `groundVisual=0.00m`, `visualIntercept=0.11m`, and `maxGroundAltitude=8.36m`; `git diff --check` passed. Known batchmode-only Entities Graphics `-nographics` shutdown noise remains non-blocking.
- 2026-06-27: Fixed the follow-up visual issue where the explosion now occurred at contact but the spent incoming ground rocket visual continued flying afterward. `AirMissileImpactSystem` now resolves the intercepted `GroundMissileProjectileComponent.Source`, clears any matching `GroundMissileFlyingRocketVisualComponent`, restores it to the launcher parent with scale `0`, and removes the flying component before destroying the logical target. `BattleScenarioLabValidationRunner.ValidateManualSceneLiveEcsPlayback` now requires `GroundRocketClearedAfterIntercept` before passing. Validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: live Play Mode validation passed with `closest=0.28m`, `groundVisual=0.00m`, `visualIntercept=0.20m`, `maxGroundAltitude=8.36m`, and `ground rocket clear-after-intercept observed`; `git diff --check` passed. Known batchmode-only Entities Graphics `-nographics` shutdown noise remains non-blocking.
- 2026-06-27: Added the manual runtime scenario-cycling workflow requested for the Scenario Lab scene. `BattleScenarioLabRuntimeRunner` dispatches the existing asset-backed scenario definitions through their existing ECS runners, `BattleScenarioLabPlayBootstrap` now owns a serialized scenario list plus Previous/Next/ScenarioSelector controls, and AD-001 remains the only scenario with live ECS visual playback while the other scenarios run their metrics validation and update the overlay. `BattleScenarioLabSceneBuilder.CreateManualSceneShell` now serializes AD-001 through AD-010 plus GM-001 and DR-001 into `BattleScenarioLab.unity`, and smoke validation asserts the selector, previous/next listeners, scenario list, variant selector, restart listener, production prefab registry, SubScene, and live playback references. `BattleScenarioLabSuiteRunner` now uses the same runtime dispatcher as the manual scene so suite validation exercises the same path as the Next/Run workflow. Validation: scene generation passed; `BattleScenarioLabValidationRunner.ValidateManualSceneSmoke` passed with known non-blocking Entities Graphics `-nographics` log noise; `BattleScenarioLabSuiteRunner.RunScenarioSuite` passed with all 12 scenario assets non-skipped; `git diff --check` passed.
- 2026-06-27: Fixed the user-reported Scenario Lab workflow regression where clicking the generated dropdown logged `The dropdown template is not assigned` and Next appeared not to run. The scene builder now creates a full lightweight UGUI dropdown template for both `ScenarioSelector` and `VariantSelector` with `Template/Viewport/Content/Item`, an item `Toggle`, background, checkmark, and label. `BattleScenarioLabPlayBootstrap` now updates its current scenario reference when selection changes and treats Previous/Next as visual-variant controls while the selected scenario supports live playback, so the default AD-001 visual path cycles AD-001-A/B/C/D instead of jumping immediately into metrics-only AD-002. Smoke validation now requires dropdown templates and item toggles and verifies the Next-selection path runs the first AD-001 visual variant. Validation: scene regeneration passed; `BattleScenarioLabValidationRunner.ValidateManualSceneSmoke` passed; `BattleScenarioLabSuiteRunner.RunScenarioSuite` passed with all 12 scenario assets non-skipped; `git diff --check` passed.
- 2026-06-27: Fixed the user-reported AD-001 live visual variant mismatch where some variants could visually pass through each other while the scenario still reported an intercept. Root cause: the live production-prefab playback used different launcher/threat component values than the AD-001 fixed-step metrics, and the PlayMode validation only proved one preferred playback variant. `BattleScenarioLabVisualPlayback` now applies lab-only AD-001 proven air-defense and ground-threat component values to the instantiated production prefabs before existing ECS systems run, keeping production models, missile visuals, trails, VFX, and launcher/battery systems in use. `BattleScenarioLabValidationRunner.ValidateManualSceneLiveEcsPlayback` now cycles and validates all four AD-001 live visual variants before passing. Validation: `BattleScenarioLabValidationRunner.ValidateManualSceneSmoke` passed; no-quit PlayMode validation passed A/B/C/D with visual contact distances A=0.29m, B=0.31m, C=0.32m, D=0.35m, synced visible/logical ground rocket distance 0.00m, and clear-after-intercept observed; `git diff --check` passed. Known non-blocking batchmode noise remains from Entities Graphics under `-nographics`.
- 2026-06-27: Fixed the user-reported runtime Next workflow regression where the overlay text changed but the visual test did not switch. `BattleScenarioLabPlayBootstrap` now passes the selected scenario and selected/default variant into visual playback, and `BattleScenarioLabVisualPlayback` supports both ground-missile and production air-target visual paths. The baked Scenario Lab prefab registry now includes the production jet, attack helicopter, and drone prefabs, and switching clears spawned lab launcher/projectile/air-target entities before starting the next visual. Added `BattleScenarioLabValidationRunner.ValidateManualSceneNextSwitchesVisualPlayback`, which enters Play Mode in the shadow project and proves runtime Next advances AD-001 -> AD-002 -> AD-011 with production target/interceptor entities observed. Validation: shadow scene builder passed, smoke validation passed, no-quit PlayMode Next switch validation passed, no-quit AD-001 all-variant visual validation passed, and `git diff --check` passed.
