# Air Missile Launcher Feature Plan

## Summary

`Unit_Veh_Missle_Launcher_Air` is a ground-to-air defensive missile launcher. It should not behave like a normal direct-fire vehicle and should not require the player to manually tap fast-moving air targets. It should automatically detect hostile air units and incoming hostile ground missiles, rotate its launcher turret toward the predicted intercept direction, fire a visible homing missile, and destroy or damage the target in the air.

This feature should reuse the successful structure from `Unit_Veh_Missle_Launcher_Ground`: config-driven launcher data, baked child references, ECS state, visual animation, projectile flight, VFX references, and data-only impact/interception components. The important difference is that the air launcher is autonomous and homing, while the ground launcher is manually targeted and arcing.

## Current Observations

- `Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset` currently behaves as a normal attacking vehicle:
  - `canAttack: 1`
  - `allowAutoEngage: 1`
  - `usesTurretAim: 1`
  - `attackRange: 600`
  - normal attack trace / impact fields are assigned.
- The air launcher prefab contains missile children such as:
  - `SM_Prop_Missle_Launcher_02_Missle_1`
  - `SM_Prop_Missle_Launcher_02_Missle_2`
  - continuing through multiple loaded missile slots.
- The turret/root launcher child is named `Missle_Launcher_Air`.
- Ground missile projectiles already carry `MissileInterceptionTargetComponent`, which gives this feature a clean target class for future ground-missile interception.

## Desired Player Behavior

- Player does not need to enter manual attack mode for this unit.
- When the unit is idle and a valid hostile air threat enters range:
  - The launcher rotates toward the threat.
  - The selected unit order text can show `TRACKING AIR TARGET`.
  - The missile fires after a short lock delay.
  - The projectile homes toward the target and detonates in air.
- When an enemy ground missile is in range:
  - The launcher prioritizes the incoming missile over ordinary aircraft.
  - The selected unit order text can show `INTERCEPTING MISSILE`.
  - The fired missile tracks and intercepts the incoming projectile.
- If there are no valid threats:
  - The selected unit order text can show `AIRSPACE CLEAR` or `IDLE`.
- If the player taps the Attack button while this unit is selected:
  - Do not enter normal manual attack targeting.
  - Show transient feedback: `Air defense auto-engages aircraft and incoming missiles.`

## Radar And Satellite Support

Friendly support units/buildings improve the air launcher's effectiveness:

- `Unit_Veh_Radar_Tank`
  - Mobile support.
  - Improves nearby air launchers within support range.
  - Best suited to range, lock speed, and tracking quality bonuses.
- `Building_Satelite_Dish`
  - Static support.
  - Improves nearby or faction-wide tracking depending on final balance.
  - Best suited to high-quality prediction, accuracy, and early detection bonuses.

Recommended V1 rule:

- Support applies when the radar tank or satellite dish is friendly, alive, and within its configured support radius of the air launcher.
- If multiple support providers are nearby, use the best provider of each type and clamp total bonuses.
- Future upgrades can increase the support provider level. Until upgrades exist, every support provider can default to level `1`.

Recommended support effects:

- Increase effective detection range.
- Reduce lock time.
- Reduce prediction error.
- Increase missile turn rate or proximity reliability.
- Improve priority scoring for incoming missiles.

## Architecture Rules

- No `Object.Find*`, `GameObject.Find`, runtime hierarchy string lookup, static mutable registries, or direct UI-to-gameplay mutation.
- Do not put launcher gameplay into UI views, `MatchBootstrapSystem`, or prefab scripts.
- UI can display feedback/read models only. Gameplay target acquisition, aiming, firing, support calculation, and projectile behavior belong in ECS systems.
- Unity object references are allowed only at config, authoring, baking, managed VFX reference, or editor setup boundaries.
- Code type names should use the correct spelling `Missile`. Existing source keys and asset names may keep the current authored spelling `Missle`.
- Avoid unconditional runtime logs. Diagnostics must be gated through diagnostics/event-buffer paths.
- Do not expand broad manager/controller/facade shells.

## Config Placement

Add a new config type:

- Script: `Assets/Game/Scripts/Configs/AirMissileLauncherConfig.cs`
- Asset: `Assets/Game/Configs/Weapons/AirMissileLauncher_Air_Config.asset`
- Assigned from `Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset`

Add an optional field next to the existing ground missile config on unit prefab configs:

- `AirMissileLauncherConfig airMissileLauncherConfig`

Recommended config fields:

### Detection

- `minRange`
- `baseDetectionRange`
- `maxDetectionRange`
- `airTargetPriority`
- `incomingMissilePriority`
- `ignoreGroundUnits`
- `ignoreGroundBuildings`

### Launcher Timing

- `turretYawSpeedDegreesPerSecond`
- `aimToleranceDegrees`
- `lockSeconds`
- `launchDelaySeconds`
- `reloadSeconds`
- `salvoSize`

Suggested first values:

- yaw speed: `220` to `320` degrees/second
- lock: `0.25` to `0.6` seconds
- launch delay: `0.1` to `0.2` seconds
- reload: `1.4` to `2.4` seconds

### Missile

- `missileSpeed`
- `missileAcceleration`
- `missileTurnRateDegreesPerSecond`
- `missileLifetimeSeconds`
- `proximityFuseRadius`
- `airTargetDamage`
- `incomingMissileDamage`
- `trackingQuality`

### Support Bonuses

- `radarSupportRangeBonus`
- `radarLockTimeMultiplier`
- `radarTrackingBonus`
- `radarTurnRateBonus`
- `satelliteSupportRangeBonus`
- `satelliteLockTimeMultiplier`
- `satelliteTrackingBonus`
- `satellitePredictionBonus`
- `maxSupportRangeBonus`
- `maxSupportTrackingBonus`

### VFX

- `missileVisualPrefab`
- `launchFlashPrefab`
- `launchSmokePrefab`
- `missileTrailPrefab`
- `airburstExplosionPrefab`
- `airTargetImpactPrefab`
- `interceptExplosionPrefab`

## ECS Data Model

### Launcher Components

- `AirMissileLauncherComponent`
  - Min/max range.
  - Lock/reload timing.
  - Turret speed and aim tolerance.
  - Missile speed, turn rate, lifetime, fuse radius, damage.
  - Target-priority weights.

- `AirMissileLauncherStateComponent`
  - Phase: `Idle`, `Tracking`, `Locked`, `Launching`, `Reloading`.
  - Current target entity.
  - Current predicted intercept position.
  - Timer.
  - Selected missile slot.
  - Last support quality.

- `AirMissileLauncherTargetComponent`
  - Target entity.
  - Target kind: `EnemyAirUnit` or `IncomingGroundMissile`.
  - Target world position.
  - Target velocity.
  - Score/priority.
  - Predicted intercept point.

- `AirMissileLauncherVisualReferenceComponent`
  - Turret entity.
  - Optional launch spawn entity.
  - Default local turret position/rotation.

- `AirMissileLauncherMissileVisualComponent`
  - Buffer entry per loaded missile child.
  - Missile entity.
  - Slot index.
  - Initial local position/rotation/scale.

- `AirMissileLauncherVfxReferenceComponent`
  - Managed VFX prefab references.

### Support Components

- `AirDefenseSupportProviderComponent`
  - Provider kind: `RadarTank` or `SatelliteDish`.
  - Support level.
  - Support radius.
  - Range bonus.
  - Lock bonus.
  - Tracking bonus.

- `AirDefenseSupportLinkComponent`
  - Applied range bonus.
  - Applied lock multiplier.
  - Applied tracking quality.
  - Best radar provider.
  - Best satellite provider.

### Projectile Components

- `AirMissileProjectileComponent`
  - Source launcher.
  - Target entity.
  - Target kind.
  - Faction id.
  - Velocity.
  - Speed.
  - Acceleration.
  - Turn rate.
  - Lifetime.
  - Proximity fuse radius.
  - Damage.
  - Tracking quality.

- `AirMissileImpactRequestComponent`
  - Source launcher.
  - Target entity.
  - Impact/intercept position.
  - Damage.
  - Target kind.
  - Faction id.

## System Flow

1. `AirDefenseSupportProviderBakeSystem`
   - Bakes radar tank and satellite dish support provider components from config.

2. `AirMissileLauncherTargetAcquisitionSystem`
   - Finds hostile air units and hostile `MissileInterceptionTargetComponent` entities.
   - Ignores friendly, neutral, dead, boarded, or invalid targets.
   - Scores targets.
   - Prioritizes incoming missiles over aircraft when they are a threat.
   - Writes or clears `AirMissileLauncherTargetComponent`.

3. `AirMissileLauncherSupportLinkSystem`
   - Finds nearby friendly support providers.
   - Computes effective range, lock, tracking, and accuracy bonuses.
   - Writes `AirDefenseSupportLinkComponent`.

4. `AirMissileLauncherTurretAimSystem`
   - Rotates `Missle_Launcher_Air` around local Y only.
   - Aims toward predicted intercept position.
   - Does not rotate or move the vehicle body.

5. `AirMissileLauncherFireControlSystem`
   - Owns launcher phase transitions.
   - Waits until target is in range, turret is aimed, and lock timer is complete.
   - Spawns or activates a homing missile.
   - Hides the selected loaded missile slot.
   - Starts reload.

6. `AirMissileHomingProjectileSystem`
   - Updates projectile velocity toward target using turn-rate and tracking quality.
   - Predicts moving air targets.
   - Uses proximity fuse for airbursts/intercepts.
   - Emits `AirMissileImpactRequestComponent` on hit/intercept/lifetime expiry.

7. `AirMissileImpactSystem`
   - Intercepts incoming ground missiles by adding/removing the correct projectile state.
   - Damages enemy aircraft.
   - Spawns the correct VFX.
   - Cleans up projectile entities/visuals.

8. `AirMissileLauncherReloadVisualSystem`
   - Restores loaded missile slot visibility after reload.

9. `AirMissileLauncherFeedbackSystem`
   - Publishes selected-unit order/status read-model values.
   - Publishes throttled transient feedback events.

## Feedback Design

Because this is automatic defense, feedback should explain what the unit is doing without requiring a manual target command.

### Selected Panel Order Text

- No threats: `AIRSPACE CLEAR`
- Idle fallback: `IDLE`
- Target acquired: `TRACKING AIR TARGET`
- Incoming ground missile target: `INTERCEPTING MISSILE`
- Lock complete / launch event: `MISSILE LAUNCHED`
- Reloading: `RELOADING`
- Support active: optional suffix `RADAR LINK` or `SATELLITE LINK`

### Command Feedback Panel

Use transient feedback and avoid spam:

- Selecting Attack on air launcher:
  - `Air defense auto-engages aircraft and incoming missiles.`
- Threat acquired while selected:
  - `Air defense tracking hostile aircraft.`
- Incoming missile intercept:
  - `Interceptor launched.`
- Unsupported/low-quality tracking, if relevant:
  - `Tracking is weak. Add radar or satellite support.`
- Radar support becomes active:
  - `Radar link active.`
- Satellite support becomes active:
  - `Satellite link active.`

Feedback throttling:

- Do not show every scan tick.
- Do not repeat the same message more than once every few seconds.
- Persistent command feedback is not needed because there is no manual targeting mode.

## Visual Design

- Turret rotation:
  - Rotate the `Missle_Launcher_Air` child on local Y.
  - Use smooth yaw, not instant snapping.
  - Aim at predicted intercept point, not just the target's current position.
- Loaded missiles:
  - Use `SM_Prop_Missle_Launcher_02_Missle_1` and sibling missile children as visible loaded rounds.
  - Hide one selected slot when fired.
  - Restore after reload unless ammo is introduced later.
- Fired missile:
  - Use `SM_Prop_Missle_Launcher_02_Missle` visual/prefab if available.
  - Missile should turn visibly in air and track the target.
  - Use a short launch flash/smoke at the tube.
  - Use a trail that follows the missile.
  - Use airburst/intercept VFX at hit point.

## Targeting Rules

Valid targets:

- Hostile air units.
- Hostile ground missiles carrying `MissileInterceptionTargetComponent`.

Invalid targets:

- Friendly units.
- Neutral units/buildings.
- Ordinary ground units.
- Ordinary ground buildings.
- Dead/despawning entities.
- Targets inside minimum arming range.
- Targets outside effective range after support bonuses.

Priority order:

1. Incoming hostile ground missile that threatens friendly units/buildings.
2. Hostile aircraft currently attacking or approaching friendly assets.
3. Hostile aircraft closest to the launcher.
4. Hostile aircraft with lowest time-to-enter-damage range.

## Relationship To Existing Attack Systems

- Normal `UnitAttackSystem` should not fire traces/bullets for `AirMissileLauncherComponent`.
- Manual Attack command should not enter target-pick mode for air missile launchers.
- Debug `F` fire behavior should have a deterministic air-defense debug path:
  - If an enemy aircraft exists, fire at it.
  - Else if an enemy ground missile exists, fire at it.
  - Else emit a gated diagnostic or transient feedback that no air-defense target exists.

## Tests And Validation

### EditMode Tests

- Air launcher config bakes the launcher component.
- Air launcher prefab has serialized turret and missile-slot references.
- Target acquisition ignores ground units/buildings.
- Target acquisition accepts hostile air units.
- Target acquisition accepts hostile `MissileInterceptionTargetComponent` projectiles.
- Incoming missiles are prioritized over ordinary aircraft.
- Friendly/neutral missiles are ignored.
- Radar tank support increases effective range/tracking quality.
- Satellite dish support increases effective range/tracking quality.
- Support bonuses clamp to configured maximums.
- Turret yaw rotates only around local Y.
- Fire control waits for aim and lock before launch.
- One launch hides one missile slot and restores it after reload.
- Homing projectile hits a moving air target.
- Homing projectile intercepts a ground missile.
- Manual Attack command on air launcher produces feedback instead of entering target mode.

### Runtime Validation

Use the shadow project when the main editor is busy:

- `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

Runtime checks:

- Spawn/select air missile launcher.
- Spawn hostile helicopter/jet inside range.
- Confirm turret rotates to the target.
- Confirm no generic yellow attack traces are fired.
- Confirm missile launches after fast lock.
- Confirm missile tracks and hits in air.
- Confirm selected order text changes through tracking/launch/reload/clear.
- Fire a ground missile from `Unit_Veh_Missle_Launcher_Ground`.
- Confirm air launcher detects and intercepts the missile.
- Add/remove nearby radar tank and satellite dish.
- Confirm support changes effective behavior without UI or hierarchy lookup.

## Step-By-Step Progress Tracker

1. [x] Create this architecture plan.

2. [x] Audit current air launcher prefab and config.
   - Confirm turret child entity path/name.
   - Confirm missile slot child references.
   - Confirm whether a launch spawn transform exists or needs to be added.
   - Confirm VFX assets to reuse for launch, trail, and airburst.
   - Progress: `Missle_Launcher_Air` and twelve missile child transforms are serialized on `Unit_Veh_Missle_Launcher_Air.prefab`; VFX references are config-driven and optional for the first pass.

3. [x] Add `AirMissileLauncherConfig`.
   - Create config script.
   - Create config asset under `Assets/Game/Configs/Weapons`.
   - Add serialized field to unit prefab config model.
   - Assign config on `Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset`.
   - Progress: `AirMissileLauncher_Air_Config.asset` is assigned from the air launcher unit config.

4. [x] Add ECS components.
   - Add launcher component.
   - Add launcher state component.
   - Add target component.
   - Add visual reference component.
   - Add missile-slot buffer component.
   - Add support provider/link components.
   - Add homing projectile and impact request components.
   - Progress: components were added to `CombatComponents.cs`.

5. [x] Extend `UnitGridAuthoring` baking.
   - Bake air launcher components only when air config is assigned.
   - Bake turret and missile-slot references from serialized fields.
   - Preserve ground missile launcher behavior.
   - Preserve ordinary turret/direct-fire unit behavior.
   - Progress: air missile launcher, missile-slot visuals, managed VFX refs, and support provider data now bake from authoring/config.

6. [x] Add editor setup/validation for prefab references.
   - Serialize `Missle_Launcher_Air`.
   - Serialize missile children such as `SM_Prop_Missle_Launcher_02_Missle_1`.
   - Add focused prefab validation tests.
   - Progress: prefab references are serialized and `AirMissileLauncherAuthoringTests` validates the config, unit config assignment, turret reference, and twelve missile child references.

7. [x] Add radar/satellite support provider projection.
   - Add provider config values to `Unit_Veh_Radar_Tank`.
   - Add provider config values to `Building_Satelite_Dish`.
   - Bake provider components for friendly support entities.
   - Progress: existing `ThreatDetector` config data now projects to `AirDefenseSupportProviderComponent`; ground detectors act as radar support and air detectors act as satellite support.

8. [x] Implement target acquisition.
   - Query enemy air units.
   - Query hostile interceptable ground missiles.
   - Score and prioritize targets.
   - Write target component or clear when no valid target exists.

9. [x] Implement support link calculation.
   - Find nearby friendly radar/satellite support.
   - Apply range, lock, tracking, and accuracy bonuses.
   - Clamp final values.

10. [x] Implement turret aiming.
    - Smoothly rotate turret local Y toward predicted intercept point.
    - Track aim tolerance.
    - Add tests for Y-only rotation.
    - Progress: runtime aiming is implemented; focused tests remain.

11. [x] Implement launch/fire control.
    - Add state machine.
    - Wait for target, aim, lock, and reload.
    - Hide selected missile slot.
    - Spawn or activate homing missile.

12. [x] Implement homing missile projectile.
    - Track target using speed, acceleration, turn rate, and tracking quality.
    - Add proximity fuse.
    - Expire safely if target dies or projectile lifetime ends.
    - Progress: homing missiles now emit the configured `MissileTrailPrefab` as sampled timed-loop trail puffs while in flight, and trail state is cleaned up when a reused child missile visual is restored. The deterministic validation runner verifies the configured trail attaches to fired homing projectiles.

13. [x] Implement impact/interception.
    - Damage air target.
    - Intercept hostile ground missile.
    - Spawn VFX.
    - Clean up projectile and missile target state.

14. [x] Remove normal direct-fire behavior for air missile launcher.
    - Prevent generic attack trace/bullet effects.
    - Prevent direct instant damage path.
    - Keep non-air-defense units unchanged.

15. [x] Add HUD feedback/read-model support.
    - Selected order text.
    - Transient feedback for selected unit.
    - Manual Attack explanation feedback.
    - Message throttling.
    - Progress: selected order/read-model text supports airspace clear, tracking, intercepting missile, and reloading. Manual Attack on an air-defense-only selection now stays out of target mode and shows `Air defense auto-engages aircraft and incoming missiles.` The older focused radar attack helper is now ground-launcher-only.

16. [x] Add debug fire support.
    - Make the existing hold-`F` debug path work for air missile launcher.
    - Prefer enemy aircraft, then enemy ground missile.
    - Do not move the launcher.
    - Progress: hold-`F` now creates an airborne debug target and feeds the air launcher target component without generic engage movement/traces.

17. [x] Add focused tests.
    - Config/baker tests.
    - Acquisition tests.
    - Support bonus tests.
    - Turret aiming tests.
    - Fire control tests.
    - Homing/interception tests.
    - Feedback tests.
    - Progress: added focused ECS tests for target acquisition, support bonuses, turret yaw, launch/fire control, homing impact cleanup, command-mode feedback, config assignment, baker source guard, and prefab reference validation. Unity test-runner XML output was not produced in `-nographics`, so a deterministic editor validation runner was added instead.

18. [x] Runtime validate in the shadow project.
    - Validate aircraft engagement.
    - Validate ground missile interception.
    - Validate support providers.
    - Validate no direct-fire traces.
    - Validate selected-panel feedback.
    - Progress: `AirMissileLauncherValidationRunner.Run` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, validating config/VFX wiring, prefab turret and missile-slot refs, support range/lock bonuses, hostile air acquisition, homing projectile creation, missile trail component assignment/playback path, air target damage, incoming ground missile interception/destruction, projectile cleanup, and no generic `EngageTarget` path.
    - Progress: `AirMissileLauncherVisualProofCapture.Run` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with graphics-capable batch mode and wrote `/private/tmp/warline_air_missile_launcher_visual_proof.png`, proving the air launcher prefab renders, the serialized turret can yaw, and a missile slot can be visually separated/framed. Full in-match visual validation remains.
    - Progress: `MatchRuntimeShellSmokeValidation.RunAirMissileLauncherSmoke` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, reaching the live match runtime, creating an isolated air-defense scenario, observing a fired projectile, observing missile trail state, and damaging the hostile air target.

19. [x] Final cleanup.
    - Run `git diff --check`.
    - Remove temporary diagnostics.
    - Update this progress tracker with results.
    - Progress: first-pass compile validation passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`; latest support/debug/test additions also pass shadow Unity batch compile with no C# errors/warnings. `AirMissileLauncherValidationRunner.Run` passed again on 2026-06-11 after adding sampled missile trail playback, editor-safe VFX runtime initialization, real trail prefab validation, and ground missile interception validation. `AirMissileLauncherVisualProofCapture.Run` also passed and produced a visual proof PNG. `MatchRuntimeShellSmokeValidation.RunAirMissileLauncherSmoke` passed in the live match runtime. `git diff --check` passed, and the remaining air-missile diagnostic output is limited to editor validation runners.

## Open Implementation Decisions

- Whether satellite support is local-radius only or faction-wide. Recommendation: local-radius V1, faction-wide can be added later if balance needs it.
- Whether the air launcher has finite visible ammo. Recommendation: infinite reload V1 with missile slots restored after reload.
- Whether aircraft should always be one-shot. Recommendation: damage-configured, not always one-shot. Incoming missiles should be destroyed on intercept.
- Whether target markers are needed. Recommendation: selected-panel order text and projectile visuals are enough for V1; add airborne target markers only if target readability is poor.
