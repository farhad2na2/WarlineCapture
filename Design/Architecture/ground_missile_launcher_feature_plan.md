# Ground Missile Launcher Feature Plan

## Summary

`Unit_Veh_Missle_Launcher_Ground` is a long-range ground-to-ground rocket artillery vehicle. It should not behave like a tank with instant direct-fire damage. When it receives a valid long-range attack order, the launcher battery `SM_Veh_Rocket_Truck_01_Rocket_Battery` should elevate smoothly to about `-30` degrees on local X, launch one visible rocket child such as `SM_Veh_Rocket_Truck_01_Rocket_1`, show smoke from the launcher, fly the rocket in a readable arcing path at a slower cinematic speed, then apply impact damage and VFX at the target area.

This feature must stay ECS-aligned. UI and input produce attack intents only. Gameplay systems own validation, launcher state, projectile flight, damage, and future interception hooks. Unity object references are allowed only at authoring/baking or managed visual-reference edges.

## Desired Player Behavior

- Select `Unit_Veh_Missle_Launcher_Ground`.
- Tap `Attack`.
- HUD enters attack targeting mode and shows feedback.
- Valid target:
  - Must be hostile or valid hostile ground position/object.
  - Must be inside max artillery range.
  - Must be outside a minimum arming range so nearby targets in the camera view are rejected with useful feedback.
- If target is too close:
  - Do not fire.
  - Keep attack mode active.
  - Feedback: `TARGET TOO CLOSE FOR MISSILE LAUNCHER`.
- If target is out of range:
  - Do not fire.
  - Keep attack mode active.
  - Feedback: `TARGET OUT OF RANGE`.
- On valid target:
  - Vehicle stays selected.
  - Launcher battery elevates smoothly.
  - Rocket leaves the rack visibly with smoke.
  - Rocket flies high enough to read as an artillery missile.
  - Damage applies on impact, not at launch.
  - Attack mode exits after the command is accepted, matching existing Attack command behavior.

## Architecture Rules

- No `Object.Find*`, `GameObject.Find`, runtime hierarchy string lookup, static mutable registries, or direct gameplay mutation from UI.
- Do not put launcher gameplay into `MatchBootstrapSystem`, UI views, or prefab scripts.
- Use ECS data components for launcher type, launcher state, projectile flight, projectile damage, and future interception state.
- Use authoring/baking or editor prefab setup to bind `SM_Veh_Rocket_Truck_01_Rocket_Battery` and rocket children by serialized/reference-time setup, not by runtime name search.
- Keep generic `UnitAttackSystem` behavior for rifles/tanks/direct-fire units. Add a separate missile-artillery path for units with missile-launcher components.
- Avoid unconditional runtime logs. Diagnostics, if needed, must be gated through the diagnostics/event-buffer path.

## Proposed Data Model

### Unit Components

- `GroundMissileLauncherComponent`
  - `MinRange`
  - `MaxRange`
  - `PrepareSeconds`
  - `LaunchCooldownSeconds`
  - `BatteryElevatedAngleDegrees`
  - `RocketSpeed`
  - `ArcHeight`
  - `DamageRadius`
  - `Damage`
  - `AmmoCount` or `LoadedRocketIndex` for V1 visual sequencing

- `GroundMissileLauncherStateComponent`
  - `Phase`: `Idle`, `Preparing`, `Launching`, `Recovering`, `Reloading`
  - `TargetEntity`
  - `TargetCell`
  - `TargetWorldPosition`
  - `Timer`
  - `SelectedRocketSlot`

- `GroundMissileLauncherVisualReferenceComponent`
  - `BatteryEntity`
  - Optional `MuzzleEntity`
  - Optional `SmokeSpawnEntity`

- `GroundMissileLauncherRocketVisualBuffer`
  - One entry per authored rocket child under the rack.
  - Fields: `RocketEntity`, `SlotIndex`, `InitialLocalPosition`, `InitialLocalRotation`, `VisibleWhenLoaded`.

### Projectile Components

- `GroundMissileProjectileComponent`
  - `Source`
  - `TargetEntity`
  - `TargetCell`
  - `StartPosition`
  - `TargetPosition`
  - `ElapsedSeconds`
  - `DurationSeconds`
  - `ArcHeight`
  - `Damage`
  - `DamageRadius`
  - `FactionId`
  - `Interceptable`

- `GroundMissileProjectileVisualReferenceComponent`
  - Managed prefab/reference for the spawned rocket visual if using GameObject visuals.
  - Prefer ECS render entity clone if practical; otherwise keep the managed visual isolated to a visual system.

- `GroundMissileImpactRequestComponent`
  - Data-only impact request emitted when flight completes.
  - Consumed by damage/VFX systems.

### Future Air Defense Hook

- `MissileInterceptionTargetComponent`
  - Added to active missile projectile entities.
  - Gives `Unit_Veh_Missle_Launcher_Air` a clean target class later.
- `MissileInterceptedComponent`
  - Stops impact damage and triggers mid-air explosion VFX.

## Authoring And Prefab Binding

1. Add a narrow launcher config:
   - Script: `Assets/Game/Scripts/Configs/GroundMissileLauncherConfig.cs`
   - Ground launcher asset: `Assets/Game/Configs/Weapons/GroundMissileLauncher_Ground_Config.asset`
   - `minRange`
   - `maxRange`
   - `batteryElevatedAngleDegrees`
   - `prepareSeconds`
   - `reloadSeconds`
   - `rocketSpeed`
   - `arcHeight`
   - `damageRadius`
   - `damage`
   - `launcherBackfirePrefab`
   - `rocketTrailPrefab`
   - `impactExplosionPrefab`
   - `impactSmokePrefab`
2. Add authoring/baker logic in `UnitGridAuthoring.BakerImpl`:
   - If config marks ground missile launcher, add launcher components.
   - Bind battery transform and rocket child transforms through an editor setup script that serializes references or writes stable authoring fields.
   - Do not use runtime transform-name lookup.
3. For `Unit_Veh_Missle_Launcher_Ground`:
   - Bind `SM_Veh_Rocket_Truck_01_Rocket_Battery` as the elevating battery.
   - Bind rocket children such as `SM_Veh_Rocket_Truck_01_Rocket_1` as loaded rocket slots.
   - Keep existing unit selection, health, movement, production, portrait, and faction tint behavior unchanged.

### Selected VFX Assets

- Launcher backfire smoke: `Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_01.prefab`
- Rocket trail first pass: `Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_Small_01.prefab`
- Impact explosion: `Assets/PolygonMilitary/Prefabs/FX/FX_Explosion_Large_Dark_01.prefab`
- Impact smoke: `Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_01.prefab`

## Gameplay Flow

1. Attack order selection remains the same:
   - `Attack` button enters attack target mode.
   - `SelectionAttackCommandRequestSystem` / `AttackOrderCommandSystem` validates target.
2. If selected attacker has `GroundMissileLauncherComponent`:
   - Use missile-specific min/max range validation.
   - Create/update `EngageTarget` only for valid targets.
   - Publish a command result message for too-close/out-of-range/invalid target.
3. `GroundMissileLauncherPrepareSystem`:
   - Watches missile launchers with valid `EngageTarget` and cooldown ready.
   - Moves state from `Idle` to `Preparing`.
   - Reserves one rocket slot.
   - Does not apply damage.
4. `GroundMissileLauncherBatteryAnimationSystem`:
   - Smoothly rotates battery local X toward `-30` degrees during `Preparing`.
   - Holds elevated while launching.
   - Smoothly returns to default when recovering or idle.
5. `GroundMissileLauncherFireSystem`:
   - When prepare timer completes, creates a `GroundMissileProjectileComponent`.
   - Hides or detaches the selected loaded rocket visual from the rack.
   - Emits smoke request at the launcher.
   - Starts cooldown/reload.
6. `GroundMissileProjectileFlightSystem`:
   - Moves missile along an arc from launcher to target.
   - Uses slower readable visual speed for large rockets.
   - Orients the rocket along velocity.
   - Marks projectile interceptable.
7. `GroundMissileProjectileImpactSystem`:
   - On arrival, applies radial damage to hostile units/buildings in `DamageRadius`.
   - Plays impact VFX/smoke.
   - Removes projectile entity/visual.
8. `GroundMissileLauncherReloadSystem`:
   - Restores rocket slot visibility if V1 uses infinite ammo.
   - Later can consume ammo/reload resources.

## UI Feedback

Use the existing match HUD command feedback path, not direct UI calls.

- No selection: `SELECT A UNIT FIRST`.
- Selected unit cannot attack: `SELECT AN ATTACK UNIT`.
- Missile launcher target too close: `TARGET TOO CLOSE FOR MISSILE LAUNCHER`.
- Target out of range: `TARGET OUT OF RANGE`.
- Valid target accepted: transient `MISSILE LAUNCH ORDERED`.
- Preparing/firing current order text:
  - selected panel order can show `PREPARING MISSILE`, `MISSILE AWAY`, then `RELOADING`.

## Visual Direction

- Battery rotation:
  - Default: current authored local rotation.
  - Fire pose: default plus local X `-30` degrees.
  - Smooth ease: about `0.45s` to `0.75s`.
- Rocket launch:
  - Use one authored rocket child as the visible round.
  - Rocket should travel above terrain, visibly slower than bullets/traces.
  - Add trail smoke behind rocket.
  - Add smoke burst at launcher and larger smoke/explosion at impact.
- Avoid the generic attack trace for this unit unless a very subtle targeting line is needed.

## Step-By-Step Progress Tracker

1. [~] Inspect and document current `Unit_Veh_Missle_Launcher_Ground` prefab/config.
   - Confirm base prefab path.
   - Confirm battery child and rocket child hierarchy.
   - Confirm current `UnitAttack` range/cooldown/damage values.
   - Progress: ground config asset found at `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset`; current values are range `600`, cooldown `3`, damage `90`, health `450`.
   - Progress: user confirmed `SM_Veh_Rocket_Truck_01_Rocket_Battery` and `SM_Veh_Rocket_Truck_01_Rocket_1` live under the `Model` subtree.

2. [x] Add launcher ECS components.
   - Add `GroundMissileLauncherComponent`.
   - Add `GroundMissileLauncherStateComponent`.
   - Add visual reference and rocket-slot buffer components.
   - Add projectile and impact request components.
   - Completed: components added in `Assets/Game/Scripts/Components/CombatComponents.cs`, including projectile and future interception components.

3. [x] Add authoring/config projection.
   - Add ground missile launcher config fields.
   - Bake components only for `Unit_Veh_Missle_Launcher_Ground`.
   - Preserve existing non-launcher unit behavior.
   - Progress: added `GroundMissileLauncherConfig` and assigned `GroundMissileLauncher_Ground_Config.asset` from `Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset`.
   - Completed: `UnitGridAuthoring` now bakes launcher state, visual reference, rocket-slot buffer, and VFX references only when a `GroundMissileLauncherConfig` is assigned.

4. [x] Add prefab reference setup without runtime lookup.
   - Add serialized authoring references or editor setup for battery and rocket children.
   - Bind `SM_Veh_Rocket_Truck_01_Rocket_Battery`.
   - Bind rocket slot children including `SM_Veh_Rocket_Truck_01_Rocket_1`.
   - Validate no missing references in prefab.
   - Completed: `Unit_Veh_Missle_Launcher_Ground.prefab` serializes the battery, smoke-spawn reference, and all 12 rocket-slot transforms; focused editor tests cover the assignments.

5. [x] Split missile launcher attack from direct-fire damage.
   - Ensure `UnitAttackSystem` does not instantly damage for entities with `GroundMissileLauncherComponent`.
   - Add launcher-specific fire request/state transition system.
   - Keep tanks/rifles unchanged.
   - Completed: `UnitAttackSystem` now arms `GroundMissileLauncherStateComponent` instead of aggregating direct damage for missile launchers, while existing non-launcher direct-fire behavior remains unchanged.

6. [x] Implement min/max range validation and feedback.
   - Reject nearby targets with `TARGET TOO CLOSE FOR MISSILE LAUNCHER`.
   - Reject far targets with `TARGET OUT OF RANGE`.
   - Keep attack targeting mode active on rejection.
   - Completed: attack order validation rejects too-close and out-of-range missile targets with explicit HUD feedback text, and the attack request/result bridge preserves the message.

7. [x] Implement battery elevation animation.
   - Smooth local X rotation to `-30` degrees.
   - Return smoothly after launch/reload.
   - Verify it works on live detailed visual and does not affect mid/low LOD unexpectedly.
   - Completed: `GroundMissileLauncherVisualSystem` drives the serialized battery entity from default rotation to configured elevated X rotation during prepare, then returns during reload.

8. [x] Implement rocket launch visual.
   - Hide/detach selected loaded rocket slot.
   - Spawn or convert projectile visual.
   - Add launcher smoke request.
   - Avoid per-frame allocation.
   - Completed: selected serialized rocket slot detaches from its rack as a temporary flying mesh visual, follows the same arc as the projectile data, then restores to its original parent; launcher smoke VFX plays on launch.

9. [x] Implement projectile flight.
   - Arc flight with readable speed.
   - Rotate missile along path.
   - Keep projectile entity data suitable for future interception.
   - Completed: `GroundMissileProjectileComponent` flies in a slow arcing path and carries source, faction, target, radius, damage, and future interception marker data.

10. [x] Implement impact damage and VFX.
    - Radial damage against hostile units/buildings.
    - Impact smoke/explosion.
    - Recent damage health-bar visibility.
    - Recent attacker/counter-engage behavior if appropriate.
    - Completed: impact applies radial hostile-only damage, marks recent attacker/recent damage visibility, and plays configured impact smoke/explosion VFX from the launcher config.

11. [x] Add reload/ammo visual reset.
    - V1 can restore rocket visual after cooldown if ammo is infinite.
    - Later ammo can consume production/resource data.
    - Completed: selected rocket slot is restored after reload for V1 infinite-ammo behavior.

12. [x] Add tests.
    - Authoring/baker test confirms launcher components and visual refs.
    - Range validation tests for too-close, valid, out-of-range.
    - Direct-fire regression test confirms normal tank/rifle damage still applies instantly.
    - Missile projectile test confirms damage applies on impact, not launch.
    - Completed: authoring tests cover config/VFX and prefab visual refs; runtime tests cover no instant launch damage, detached rocket visual restore, delayed area impact damage, min/max range rejection, and command-result message propagation.

13. [x] Runtime validation.
    - Validate in `WarlineCapture-CodexUnity1` shadow project.
    - Select launcher, attack near target: gets too-close feedback.
    - Attack valid distant target: battery opens, rocket launches, smoke appears, impact damages target.
    - Confirm no FPS regression, no unconditional logs, no runtime hierarchy lookup.
    - Completed: focused Unity EditMode validation in `WarlineCapture-CodexUnity1` passed for authoring/runtime systems and direct-fire regression. PlayMode validation passed for visible rocket detach/restore and impact damage.
    - Note: manual graphics inspection can still tune exact rocket orientation, smoke offset, and in-camera feel, but the runtime behavior path is covered.

## V1 Design Decisions

- V1 target type: hostile entity targets only. Ground-position artillery fire is deferred.
- V1 damage model: splash/radial impact damage only. No direct damage is applied at launch.
- V1 ammo: infinite visual reload. Production/resource ammo is deferred.
- Minimum range: config-driven; first value is `35` world units.
- Projectile speed: config-driven; first value is `42` world units/second with `28` arc height.

## Validation Commands

- `git diff --check`
- Focused EditMode tests for launcher authoring/range/projectile systems.
- Shadow project compile/tests when main Unity is open:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GroundMissileLauncher -testResults /private/tmp/ground_missile_launcher_tests.xml -logFile /private/tmp/ground_missile_launcher_tests.log`
