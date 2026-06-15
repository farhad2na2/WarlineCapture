# WarlineCapture Match Unit Command Behavior Spec

Date: 2026-06-15

This is the canonical command-behavior contract for `HOLD`, `STOP`, and `SCAN` in `SCN-08 RTS Battle HUD`.

Parent source: `Match_HUD_And_Gameplay_Implementation_Spec.md` owns the full match HUD. This child spec owns the per-unit behavior, edge cases, HUD feedback, and implementation data needed so command behavior is consistent across infantry, vehicles, helicopters, fixed-wing aircraft, drones, buildings, and mixed selections.

Related sources:

- `Match_HUD_And_Gameplay_Implementation_Spec.md` - visible button rules, match HUD states, bridge feedback, reason codes, and acceptance checks.
- `Match_Selection_Implementation_Spec.md` - selection, command-mode exclusivity, input suppression, and direct world command rules.
- `Combat_Catalog_And_Upgrade_Design.md` - unit/building catalog and capability source.
- `Field_Logistics_Oil_Fuel_Design.md` - fuel, oil, trucks, aircraft logistics, and resource-side restrictions.

## Product Rule

`HOLD`, `STOP`, and `SCAN` must respect the selected unit's movement type and physical behavior. A command may cancel intent, targeting, or aggression, but it must not force an impossible world state.

The most important rule: fixed-wing aircraft never stop in the air. A jet that is attacking, scanning, returning, taking off, or landing must finish a safe air sequence and return to base, carrier, staging lane, or authored loiter route before it becomes base-ready/available. It must never enter an airborne `STOPPING` or airborne `IDLE` state.

## Command Summary

| Command | Interaction Type | Selection Required | Result |
|---|---|---|---|
| `HOLD` | Immediate order. Not a targeting mode. | Yes. | Sets a persistent hold/guard posture for units that can hold. The unit keeps selection and shows `HOLDING` or a more specific hold state. |
| `STOP` | Immediate order. Not a targeting mode. | Yes. | Cancels current interruptible orders and moves each unit into its safest valid post-command state. Ground units may show `STOPPING` then `IDLE`; helicopters may show `HOVERING`; fixed-wing aircraft show `RETURNING`, `LANDING`, or `TAKING OFF`, never airborne `STOPPING`. |
| `SCAN` | Targeting mode unless mission scan is instant/scripted. | No for mission/global scan. Yes for selected-unit scan. | Waits for a valid scan target, then executes a scan profile. Reveals/updates intel and may trigger limited auto-engage only when the unit profile and rules of engagement allow it. |

Button visual behavior:

- `HOLD` and `STOP` flash/press, execute, then return to neutral button state. Their persistent result is shown in the selected-unit order/status display, not by keeping the button selected.
- `SCAN` stays visually active only while `ScanTargeting` is waiting for the player to choose a target area. After a valid target is accepted, the button returns to neutral and the selected panel/order banner shows `SCANNING`.
- None of these commands deselect the unit. Selection is cleared only by the selection rules in `Match_Selection_Implementation_Spec.md`.

## Unit Command Profiles

| Unit Family | `HOLD` Behavior | `STOP` Behavior | `SCAN` Behavior | Auto-Engage While Scanning |
|---|---|---|---|---|
| Infantry / foot squads | Stop advancing, take nearest valid cover/stance if available, hold formation around current reachable cell, defend assigned radius. | Cancel move/attack/patrol/scan. Stop at current reachable cell or nearest safe cell. Clear chase target. | Foot sweep or cone scan. May move a short distance to a valid vantage point if the scan target is outside sensor range. | Default `ReturnFireOnly`. Can engage confirmed close threats in range. Must not chase away from scan/hold area. |
| Ground vehicles / APC / tanks | Halt on nearest valid lane/cell, keep turret/weapon ready, defend radius, do not pursue far targets. | Brake/decelerate to a valid ground stop point. If in a narrow road/bridge/blocked segment, finish to nearest safe segment exit before idle. | Sensor sweep from current location or short movement to target radius if the unit has recon sensors. Combat-only vehicles have shorter scan range. | Default `ConfirmedHostilesOnly` for armed vehicles. Turret may engage confirmed hostile in arc/range if civilian risk passes. No long chase. |
| Artillery / mortar / support vehicles | Hold deployed/packed state according to weapon profile. If deployed, stay deployed unless unsafe. | Cancel queued fire/move/scan. If deploying/packing is non-interruptible, finish the safe transition first. | Optional long-range observer scan only if configured. Otherwise disabled. | Default `Never` unless explicitly configured as armed recon. |
| Helicopters / VTOL | Hover or loiter at safe altitude around current point. If carrying troops, preserve transport state. | Cancel order and brake into hover/loiter if hover-capable. If landing/takeoff is in a locked safety phase, finish the phase first. | Orbit/hover scan around target area. Reveal threats, rooftops, roads, civilians, and suspect structures based on sensor profile. | Default `ConfirmedHostilesOnly` for armed helicopters, `ReturnFireOnly` for transport helicopters. Must respect civilian risk and altitude/weapon arc. |
| Fixed-wing jets | Enter authored holding pattern if `canLoiter` is true. If not, return to base/staging lane and become available there. Never hover. | Abort current follow-on orders, complete safe egress, then return to base/carrier/staging lane. If already returning, keep returning and clear aggressive follow-on behavior. Never stop mid-air. | Execute a scan pass over the target area, then egress to base, staging lane, or loiter route. Does not circle forever unless `canLoiter` is true and mission allows it. | Default `Never` for pure recon pass. `ConfirmedHostilesOnly` only for `ArmedRecon` profile, with confidence/civilian-risk checks. One pass/munition window, then egress. |
| Fixed-wing UAV / recon drone | Enter authored drone loiter route if `canLoiter` is true. If not, return to drone station/recovery lane. Never hover unless the unit is explicitly authored as `VTOLDrone`. | Abort current follow-on orders, complete safe egress, then return to drone station/recovery lane. If already returning, keep returning and clear aggressive behavior. Never stop mid-air. | Primary scan unit. Executes a scan pass, scan corridor, or repeated loiter route around the target area, then returns or continues the authored route until battery/fuel reserve. | Default `Never` if unarmed. Armed drones may use `ConfirmedHostilesOnly` with strict civilian-risk checks. |
| Transport aircraft | Hold only as authored orbit/staging. Cannot hold over arbitrary map cells unless configured. | Abort insertion if still before drop. If mid-drop or landing, finish safety-critical sequence, then return/idle. | Usually disabled unless transport has recon sensor package. | Default `Never`. |
| Sea vessels / boats | Hold station in valid water lane or patrol/loiter around authored harbor/coastal anchor. | Cancel current order and decelerate to a valid water stop/hold point; do not beach or block invalid waterway cells. | Radar/visual/harbor sweep if configured. | Default `ConfirmedHostilesOnly` for armed naval units, `Never` for unarmed landing/logistics craft. |
| Buildings / turrets / radar | Hold means keep current guard sector/fire posture. It does not move. | Cancel active production targeting, attack targeting, or scan pulse where applicable. Idle in-place. | Radar/building scan pulse from fixed location if configured. | Turrets can engage by normal defense rules. Radar does not attack. |
| Logistics / producer units | Hold parks/guards current location, avoids chase, and keeps logistics role available. | Cancel current move/logistics route if interruptible; stop at valid cell. Fuel/oil trucks should not stop in blocked lanes if that prevents pathing. | Usually disabled unless the vehicle has scout sensors. | Default `ReturnFireOnly` or `Never` for unarmed units. |
| Non-controllable civilians / neutral units / enemy-only units | Not player-commandable. | Not player-commandable. | Player scan can inspect/reveal them; they do not execute player scan. | Not applicable. |

Every unit in the combat catalog must map to exactly one command profile. The implementation must not infer command behavior from prefab names, display names, portraits, or model type. If a catalog unit cannot map to one of these profiles, add a new explicit command profile before implementing the unit.

Recommended catalog mapping field:

```text
commandProfileId
```

Valid starter values:

```text
InfantryFootSquad
GroundCombatVehicle
GroundTransportVehicle
GroundLogisticsVehicle
ArtillerySupportVehicle
HelicopterVTOL
FixedWingJet
FixedWingDrone
VTOLDrone
TransportAircraft
SeaCombatVessel
SeaTransportVessel
StaticBuilding
StaticTurret
StaticRadar
NonControllableCivilian
EnemyOnly
```

## `HOLD` Command Contract

`HOLD` means: keep this unit/group near its current valid position or authored hold anchor, defend locally, and do not chase far targets.

Execution:

1. Suppress the UI click so it cannot also issue a world command.
2. Validate selection and capability.
3. Cancel active targeting modes: `MOVE`, `ATTACK`, `SCAN`, `SUPPORT`, `BUILD`, and command-wheel targeting.
4. For each selected unit, resolve the unit's hold profile.
5. Apply hold posture and local engagement radius.
6. Keep the current selection.
7. Update HUD selected status and group cards with `HOLDING`, `HOLDING COVER`, `HOLDING ORBIT`, or the authored status text.

Hold profile examples:

| Hold Profile | Used By | Behavior |
|---|---|---|
| `PositionGuard` | infantry, vehicles, logistics | Defend a radius around the current valid cell. No long pursuit. |
| `CoverGuard` | infantry/special forces | Move a short distance to nearest valid cover if immediately available, then hold. |
| `VehicleGuard` | tanks/APC | Stop on valid lane/cell, keep turret active, defend radius. |
| `HoverLoiter` | helicopters/VTOL | Hover or orbit at safe altitude around current point. |
| `AirLoiter` | fixed-wing aircraft with loiter support | Use authored holding pattern. If no pattern exists, reject or convert to return-to-base based on config. |
| `StaticGuard` | buildings/turrets | Maintain current guard/fire posture in-place. |

Hold engagement rules:

- Holding units may defend themselves and allies inside their hold radius.
- Holding units do not chase enemies outside their hold radius unless mission script or player attack order overrides it.
- Civilian-risk checks still apply. Holding state is not permission to fire into crowds or uncertain targets.
- Hold does not pause production, resource extraction, passive radar, or passive repair unless the unit profile explicitly says so.

### Fixed-Wing `AirLoiter` Hold Sequence

A flying fixed-wing jet can hold only by entering a valid loiter pattern. It never brakes, hovers, freezes, or snaps rotation.

When the player presses `HOLD` on an airborne jet:

1. Validate the jet is airborne, controllable, not in a locked safety phase, has `canLoiter = true`, has enough fuel to enter and exit the hold pattern, and has a valid `holdRouteProfile` or authored fallback hold route.
2. Suppress the UI click and clear active command targeting. Attack/scan follow-on orders are canceled unless the jet is already past a non-interruptible pass commit point.
3. Resolve a hold anchor:
   - Use the mission-authored or unit-authored `holdAnchorId` if present.
   - Otherwise use the current command area's safe airspace anchor if the jet was operating around an attack/scan/objective area.
   - Otherwise project the jet forward along its current heading and choose the nearest valid airspace anchor inside map bounds, outside blocker/no-fly volumes, and outside restricted civilian-risk airspace.
4. Build a racetrack/oval loiter route around the anchor using the jet's configured turn radius, altitude band, cruise speed, and map airspace rules.
5. Set order state to `EnteringLoiter`.
6. Fly to the route's safe join point. If the jet is already inside the route bounds, it should still turn through a legal arc instead of snapping to the loop.
7. Once joined, set order state to `Loitering` and keep flying the loop until a new command, fuel reserve, damage state, mission script, or player `STOP`/return command overrides it.

If the jet is currently attacking or scanning:

- Before the pass commit point: abort the pass, clear the target, and route to the hold pattern.
- After the pass commit point: finish the pass and safe egress, then route to the hold pattern instead of returning to base.
- During landing, takeoff, emergency return, or scripted safety lock: reject `HOLD` with `CommandLockedBySafetyPhase`; do not queue a hidden hold.

Loiter route requirements:

| Requirement | Rule |
|---|---|
| Shape | Racetrack/oval or large circular loop. No hover, instant pivot, or tiny circle. |
| Radius | At least the jet's authored minimum turn radius. |
| Altitude | Use authored loiter altitude or safe air corridor altitude. |
| Speed | Maintain cruise/loiter speed; do not slow below valid fixed-wing flight speed. |
| Position | Prefer safe airspace near the current operation area, not directly above dense civilian/high-threat zones unless mission-authored. |
| Exit | New `MOVE`, `ATTACK`, `SCAN`, `STOP`, support script, fuel reserve, or emergency state exits the loop through a safe route point. |
| Auto-engage | Default `Never`. A loitering jet does not fire just because enemies are visible. Only an authored armed-patrol profile may engage confirmed hostiles, and still must pass civilian-risk and weapon checks. |

HUD feedback for airborne jet hold:

- On accepted press: toast `Entering hold pattern.`, banner `HOLD PATTERN`, selected status `ENTERING HOLD`.
- Once the jet joins the loop: selected status `LOITERING`.
- Optional world feedback: show a short-lived air-loop marker or minimap loop marker around the hold anchor.
- If the route cannot be generated: toast `No hold pattern available.`, status unchanged, reason `UnitCannotHold`.
- If fuel reserve blocks holding: toast `Not enough fuel to hold.`, status unchanged, reason `InsufficientResources` or a future fuel-specific reason.

### Fixed-Wing Drone Hold/Stop/Scan Sequence

The default `Unit_Veh_Drone`-style behavior should match fixed-wing aircraft, not helicopters. A fixed-wing drone uses routes, passes, and loiter loops. It does not hover, brake in place, or become airborne `IDLE`.

`HOLD` on an airborne fixed-wing drone:

1. Validate the drone is airborne, controllable, not in launch/recovery/emergency return, has `canLoiter = true`, has enough battery/fuel reserve, and has a valid `holdRouteProfile` or drone loiter anchor.
2. Clear active command targeting and cancel interruptible follow-on scan/attack orders.
3. Resolve a drone hold anchor using authored `holdAnchorId`, current scan/operation area, or nearest valid safe airspace ahead of the drone.
4. Build a drone loiter route around the anchor using the drone's turn radius, altitude band, cruise speed, battery/fuel reserve, and no-fly rules.
5. Set status `ENTERING HOLD`.
6. Fly to the safe join point through legal turns; do not snap or hover.
7. Once joined, set status `LOITERING`.

`STOP` on an airborne fixed-wing drone:

1. Cancel interruptible scan/attack/move follow-on orders.
2. If before a scan/attack pass commit point, abort immediately and route to drone station/recovery lane.
3. If after a pass commit point, finish the pass and safe egress, then route to drone station/recovery lane.
4. If already returning, keep returning and clear aggressive follow-on behavior.
5. Status must be `RETURNING`, `RECOVERING`, or authored equivalent; never airborne `STOPPING` or airborne `IDLE`.

`SCAN` with a fixed-wing drone:

1. Enter `ScanTargeting` and accept a valid area/corridor target.
2. Build a scan route: single pass, corridor sweep, or repeated oval/racetrack scan loop according to `scanProfile`.
3. Fly the route at valid speed/altitude. The drone may repeat scan pulses while on the route.
4. Default auto-engage is `Never`. Armed drone variants may use `ConfirmedHostilesOnly` only if confidence, weapon, and civilian-risk checks pass.
5. Exit to recovery lane when the scan completes, battery/fuel reserve is reached, player presses `STOP`, or mission script ends the scan.

Fixed-wing drone feedback:

- `HOLD` accepted: toast `Drone entering hold pattern.`, banner `DRONE HOLD`, selected status `ENTERING HOLD`, then `LOITERING`.
- `STOP` accepted: toast `Drone returning to station.`, banner `RETURNING TO STATION`, selected status `RETURNING`.
- `SCAN` accepted: toast `Drone scan route started.`, banner `DRONE SCAN`, selected status `SCANNING ROUTE`.
- Battery/fuel reserve blocks command: toast `Drone reserve too low. Returning to station.`, banner `RETURNING TO STATION`, selected status `RETURNING`.
- Launch/recovery safety lock: toast `Drone launch or recovery in progress.`, banner `COMMAND LOCKED`, selected status `LAUNCHING`, `RECOVERING`, or authored equivalent.

### Infantry / Foot Squad Command Sequence

Infantry covers rifle squads, leaders, contractors, marksmen, breachers, anti-armor soldiers, pilots on foot, and other controllable character units.

`HOLD`:

1. Validate the squad is controllable, alive, not inside transport unless transport command rules allow squad-level hold, and not locked by stun/suppression state.
2. Clear active targeting and queued chase orders.
3. Choose hold center at the current reachable cell or the squad's current formation anchor.
4. If valid cover exists inside the authored quick-cover radius, move members into cover slots; otherwise hold formation around the anchor.
5. Set status `HOLDING` or `HOLDING COVER`.
6. Defend only inside the hold radius. Do not chase enemies beyond hold radius.

`STOP`:

1. Cancel active move, attack-chase, patrol, scan, and queued follow-on orders.
2. Resolve a reachable stop cell for each squad member while preserving spacing.
3. Clear chase target and path preview.
4. Set status `STOPPING`, then `IDLE` once members reach valid stop cells.

`SCAN`:

1. Validate `canScan` or role-based scan permission such as scout/marksman/breacher.
2. Enter `ScanTargeting` and accept a valid area, suspect building, route, or objective clue.
3. If the scan target is outside sensor range but reachable, move to the nearest valid vantage/cover point first.
4. Perform a foot cone/radius sweep using configured scan radius, duration, confidence, and civilian-risk rules.
5. Default auto-engage is `ReturnFireOnly`; infantry must not start a long chase because scan revealed a hostile.

Feedback:

- `HOLD`: `Holding position.` or `Holding cover.`
- `STOP`: `Stopping.`
- `SCAN`: `Scout scan started.`
- If suppressed or stunned: `Squad cannot act.`
- If inside transport and not commandable: `Squad is inside transport.`

### Ground Vehicle / Logistics Command Sequence

Ground vehicles cover APCs, tanks, armored cars, missile launchers, radar tanks, trucks, tanker trucks, oil trucks, and other wheeled/tracked controllable vehicles.

`HOLD`:

1. Validate the vehicle is controllable, mobile or able to guard in-place, not locked by deploy/repair/load sequence, and not inside invalid path geometry.
2. Clear active targeting and long-chase orders.
3. Resolve a valid hold cell/lane near the current position.
4. If the vehicle is on a bridge, gate, road choke, depot entrance, or narrow lane, move only to the nearest safe hold segment.
5. Set status `HOLDING` or `HOLDING LANE`.
6. Armed vehicles keep turret/weapon readiness and defend within hold radius. Logistics vehicles avoid chase and keep cargo/fuel state unchanged.

`STOP`:

1. Cancel active move, convoy, chase, scan, or logistics route if interruptible.
2. Decelerate along the current path; do not snap rotation or stop inside blockers.
3. If stopping immediately would block invalid geometry or a choke, continue to the nearest safe segment endpoint.
4. Set status `STOPPING`, then `IDLE` or authored parked/logistics status.

`SCAN`:

1. Validate the vehicle has a sensor package or recon role.
2. If scan can run in-place, perform a sensor sweep from current hold/stop point.
3. If the scan target requires movement, route to a valid sensor point, then sweep.
4. Ground combat vehicles may use `ConfirmedHostilesOnly`; logistics vehicles default to `ReturnFireOnly` or `Never`.

Feedback:

- `HOLD`: `Holding lane.`
- `STOP`: `Stopping at safe point.` when movement to a safe segment is required.
- `SCAN`: `Sensor sweep started.`
- If carrying passengers/cargo: status must preserve passenger/cargo/fuel indicators.

### Artillery / Support Vehicle Command Sequence

Artillery and support vehicles cover mortar carriers, missile launchers, deployed fire-support vehicles, repair vehicles, and support platforms.

`HOLD`:

1. Validate controllable state and deployed/packed state.
2. If deployed, hold deployed posture unless danger or mission script requires pack-up.
3. If deployment/pack-up is in a non-interruptible safety window, keep current state and reject incompatible hold changes.
4. Set status `HOLDING FIRE POSITION`, `DEPLOYED`, or authored support state.

`STOP`:

1. Cancel queued fire missions, movement, scan, and follow-on orders if interruptible.
2. If deployment/pack-up is safety-locked, finish the transition first.
3. Clear target queue but do not destroy ammo/resource state.
4. Set status `ORDER CANCELLED`, `DEPLOYED`, `PACKED`, or `IDLE`.

`SCAN`:

1. Disabled by default unless the vehicle has observer/radar/sensor capability.
2. If enabled, scan from deployed sensor posture or route to a valid observer point.
3. Auto-engage default is `Never` unless explicitly configured as armed recon.

Feedback:

- `HOLD`: `Holding fire position.`
- `STOP`: `Fire order cancelled.`
- `SCAN`: `Observer scan started.`
- If deployment is locked: `Deployment in progress.`

### Helicopter / VTOL Command Sequence

Helicopters and VTOL aircraft can hover. They are not treated like fixed-wing jets.

`HOLD`:

1. Validate helicopter is airborne/controllable or can enter a safe hover/loiter from current state.
2. If landing/takeoff/rope drop is in a safety-locked phase, reject or defer according to the authored safety profile.
3. Resolve safe hover/loiter anchor and altitude.
4. Brake into hover or orbit through a smooth turn; do not snap.
5. Preserve passengers and transport state.
6. Set status `HOLDING ORBIT` or `HOVERING`.

`STOP`:

1. Cancel interruptible move, attack, scan, insert, or extraction order.
2. If safe, brake into hover at current or nearest safe hover anchor.
3. If landing/takeoff/drop is safety-locked, finish that phase first.
4. Set status `HOVERING`, `LANDING`, or authored transport state.

`SCAN`:

1. Validate scan sensors or recon role.
2. Move to target area and enter hover/orbit scan at safe altitude.
3. Reveal rooftops, roads, suspect buildings, civilians, traps, and patrol hints per sensor profile.
4. Armed helicopters may use `ConfirmedHostilesOnly`; transport helicopters default to `ReturnFireOnly`.

Feedback:

- `HOLD`: `Holding orbit.`
- `STOP`: `Holding hover.`
- `SCAN`: `Air scan started.`
- If safety-locked: `Flight transition in progress.`

### Transport Aircraft Command Sequence

Transport aircraft are route-based aircraft. Unless explicitly authored as VTOL, they behave closer to fixed-wing aircraft than helicopters.

`HOLD`:

1. Validate authored staging/orbit route exists.
2. If airborne and route exists, enter staging orbit through a safe join point.
3. If no route exists, reject with `No hold pattern available.`
4. Never hold over arbitrary ground cells.

`STOP`:

1. If before insertion/drop commit, abort insertion and return to staging/base.
2. If mid-drop, landing, or extraction safety window has started, finish safety-critical sequence.
3. Preserve passenger/cargo state.
4. Set status `RETURNING`, `LANDING`, `TAKING OFF`, or authored transport state.

`SCAN`:

1. Disabled by default.
2. Enabled only if a sensor package/profile is authored.
3. If enabled, use route-based scan pass and return/stage after completion.

Feedback:

- `HOLD`: `Entering staging orbit.`
- `STOP`: `Returning to base.`
- `SCAN`: `Transport scan started.` only if sensor-equipped.
- If mid-drop safety lock: `Drop in progress.`

### Sea Vessel / Boat Command Sequence

Sea vessels cover patrol boats, interceptor boats, landing craft, cutters, missile craft, drone boats, and other naval/coastal units.

`HOLD`:

1. Validate the vessel is controllable and in valid water/naval path space.
2. Resolve a hold station in valid water lane, harbor anchor, patrol ring, or coastal route.
3. Decelerate/turn through valid water movement; do not beach, clip land, or snap.
4. Armed vessels guard the station within naval engagement radius.
5. Set status `HOLDING STATION`.

`STOP`:

1. Cancel active move, patrol, attack, scan, landing, or transport route if interruptible.
2. If inside a narrow channel/harbor entrance, continue to nearest safe water stop point.
3. Preserve passengers/cargo for landing craft.
4. Set status `STOPPING`, then `HOLDING STATION` or `IDLE` depending naval profile.

`SCAN`:

1. Validate radar/sonar/harbor scan profile.
2. Perform sweep from current station or route to valid scan lane.
3. Reveal harbor threats, coastal patrols, landing routes, mines/traps if authored, and civilian/collateral risk markers.
4. Auto-engage defaults to `ConfirmedHostilesOnly` for armed vessels and `Never` for unarmed landing/logistics craft.

Feedback:

- `HOLD`: `Holding station.`
- `STOP`: `Stopping at safe water.`
- `SCAN`: `Harbor scan started.`
- If target is outside water/naval route: `Cannot reach that waterway.`

### Static Building / Turret / Radar Command Sequence

Static entities cover buildings, turrets, radar buildings, watchtowers, production structures, and fixed defenses.

`HOLD`:

1. Validate the selected structure supports guard/fire posture.
2. Keep position fixed.
3. Turrets/watchtowers maintain guard sector and normal defense rules.
4. Radar/sensor buildings keep passive detection active.
5. Production buildings keep production queues running unless a separate production-cancel command is used.
6. Set status `GUARDING`, `SCANNING`, `PRODUCING`, or authored static status.

`STOP`:

1. Cancel active target order, manual attack target, scan pulse, placement target, or rally target if interruptible.
2. Do not cancel production queues, extraction, refining, passive radar, or passive repair unless the player uses the dedicated system UI for that action.
3. Set status `ORDER CANCELLED` or return to authored passive status.

`SCAN`:

1. Validate radar/watchtower/sensor scan profile.
2. Run fixed-location radial/cone pulse.
3. Reveal/update intel using configured range, duration, cooldown, and confidence.
4. Radar does not auto-engage. Turrets use normal defense rules, not scan-specific fire permission.

Feedback:

- `HOLD`: `Guarding sector.`
- `STOP`: `Order cancelled.`
- `SCAN`: `Radar pulse started.`
- If structure has no scan: `This building cannot scan.`

### Non-Controllable Unit Command Rule

Civilians, neutral props, enemy-only units, surrendered units, dead units, projectiles, and decorative meshes do not receive player `HOLD`, `STOP`, or `SCAN` commands.

Rules:

1. They must not be selected as controllable units.
2. If tapped with no selected player unit, return inspect/intel behavior only if such mode exists; otherwise no command.
3. If included inside a drag rectangle, exclude them from selection.
4. Player scan can reveal, mark, classify, or update their intel confidence, but cannot order them to scan.

Feedback:

- Enemy with no selected unit: `Select a squad first.`
- Civilian/neutral tap: no command toast unless inspect mode is active.
- Scan reveals civilian risk: `Target marked. Civilian risk too high.`

## `STOP` Command Contract

`STOP` is the player-facing command label. For ground units it means stop movement. For helicopters it can mean brake into hover. For fixed-wing aircraft and fixed-wing drones it means abort the current interruptible task and return/egress through a safe flight path; it never means stopping in the air.

Execution:

1. Suppress the UI click so it cannot also issue a world command.
2. Validate selection and active/interruption-capable order.
3. Clear active command targeting and queued follow-on orders.
4. For each selected unit, resolve the unit's stop profile.
5. Apply a physically valid stop, hover, idle, or return behavior.
6. Keep the current selection.
7. Update HUD selected status and group cards with the unit-appropriate status: ground units may use `STOPPING` then `IDLE`; helicopters may use `HOVERING`; fixed-wing aircraft must use `RETURNING`, `LANDING`, `TAKING OFF`, or another authored airborne flight state, never airborne `STOPPING`.

Stop profile examples:

| Stop Profile | Used By | Behavior |
|---|---|---|
| `StopAtReachableCell` | infantry, most ground units | Stop at current or nearest valid reachable cell. |
| `StopAtSafeSegmentEnd` | vehicles in narrow roads/bridges/gates | Continue only far enough to avoid blocking invalid geometry, then idle. |
| `HoverBrake` | helicopters/VTOL | Brake into hover/loiter if flight state allows. |
| `ReturnToBase` | fixed-wing aircraft, fixed-wing drones, transport aircraft | Clear orders, egress safely, return to base/staging/drone station, then become available. |
| `FinishSafetyPhaseThenStop` | landing/takeoff/drop/deploy sequences | Finish the non-interruptible safety phase, then stop/idle/return. |
| `StaticCancel` | buildings/turrets/radar | Cancel active target/order and remain in-place. |

Fixed-wing aircraft stop rules:

- A jet never stops at its current world position.
- If the jet is attacking, scanning, patrolling, or moving to a target, `STOP` cancels follow-on orders, completes a safe egress, and returns to base/carrier/staging lane.
- If the jet is already returning, `STOP` does not reverse or pause it. It confirms return, clears aggressive behavior, and keeps the jet on the return path.
- If the jet is landing or taking off, `STOP` waits until the safety-critical phase completes, then the jet becomes idle/available at base or staging.
- If the jet is low on fuel, damaged, out of ammo, or in emergency return, `STOP` cannot cancel the emergency return. The HUD should show `RETURNING - EMERGENCY` or equivalent.

Ground stop rules:

- Ground units should stop as soon as practical, but not inside invalid blockers, impassable water, forbidden civilian-only areas, or path cells that would break the grid/path system.
- A vehicle on a bridge, gate, road choke, or narrow nav corridor may roll forward to the nearest safe stop segment.
- If a formation receives stop, each unit resolves its own safe stop point while preserving spacing as much as possible.

## `SCAN` Command Contract

`SCAN` means: reveal or update battlefield intel for a target area using a mission scan source or the selected unit's sensor profile.

Scan source types:

| Scan Source | Selection Required | Example |
|---|---|---|
| `MissionScan` | No. | Mission-authored satellite/radar/intel scan, tutorial scan, limited charges. |
| `SelectedUnitScan` | Yes. | Scout infantry, recon vehicle, helicopter, jet recon pass, fixed-wing drone route scan, radar building. |
| `SupportAbilityScan` | Depends on support ability. | Recon drone called from support panel. |

Execution:

1. Pressing `SCAN` enters `ScanTargeting` if the scan needs a target area.
2. HUD shows scan radius/shape preview and banner text such as `SCAN - TAP AREA`.
3. Player taps a valid map area, enemy suspect marker, building, route, or objective anchor according to scan profile.
4. The command validates map bounds, cooldown, charges, resources, selected unit capability, path/flight route, civilian-risk rules, and mission restrictions.
5. Accepted scan starts the unit's scan profile or mission scan effect.
6. Scan reveals/updates hidden enemies, suspect buildings, traps, patrol hints, objective clues, civilian density/risk, minimap markers, and confidence values.
7. Scan targeting exits after accepted execution unless the scan profile is explicitly marked repeatable.
8. Selection remains unchanged.

Scan profiles:

| Scan Profile | Used By | Behavior |
|---|---|---|
| `FootSweep` | scout infantry, special forces | Short-range sweep or cone. May move to nearby cover/vantage first. |
| `VehicleSensorSweep` | recon vehicles, APC with sensor package | Medium radius sweep from current/target position. May move to valid target area. |
| `HelicopterOrbitScan` | helicopters | Move/hover/orbit above target radius, scan rooftops/roads/structures. |
| `JetReconPass` | fixed-wing jets | Fast scan pass over target corridor/area, then egress/return/loiter. |
| `DroneLoiterScan` | fixed-wing drones | Route-based loiter/corridor scan with repeated intel pulses; no hover unless profile is explicitly `VTOLDrone`. |
| `StaticRadarPulse` | radar buildings/watchtowers | Fixed-location radial scan pulse. |
| `MissionIntelPulse` | mission/global scan | Instant or delayed scan effect from mission source, not tied to selected unit. |

Fixed-wing aircraft scan rules:

- A jet scan is a pass, not a stop-and-hover action.
- The scan target defines a flight corridor, pass center, or recon radius.
- After the pass, the jet returns to base/staging or enters authored loiter if `canLoiter` is true.
- If the jet is already returning because of fuel, damage, ammo, or prior stop, a new scan order should be rejected unless the mission explicitly allows recall.
- If scan route validation fails, return `TargetUnreachable`, `TargetOutOfBounds`, `ScanUnavailable`, or the closest existing typed reason.

## Auto-Engage While Scanning

Scanning is primarily for information. Auto-engage during scan is allowed only when the unit profile and rules of engagement allow it.

Use this policy model:

| Policy | Meaning |
|---|---|
| `Never` | Scan never fires weapons. It only reveals/marks intel. |
| `ReturnFireOnly` | Unit may fire only if attacked or directly threatened. |
| `ConfirmedHostilesOnly` | Unit may fire at confirmed hostile combatants inside allowed range/arc if civilian-risk checks pass. |
| `AggressiveConfirmedHostiles` | Unit may engage confirmed hostiles during scan within the scan area, but still cannot fire into high civilian risk or unknown targets. |
| `MissionScripted` | Mission script defines the behavior for a specific cinematic/tutorial/objective moment. |

Auto-engage validation must pass all checks:

- Target is confirmed hostile, not unknown, civilian, neutral, surrendered, or only suspected.
- Intel confidence meets or exceeds the unit/profile threshold.
- Civilian risk is below the unit/profile threshold.
- Weapon is available, in range/arc, and not blocked.
- Unit is not under hold-fire, transport-only, out of ammo, jammed, stunned, or mission-restricted.
- The scan objective does not explicitly require reconnaissance-only behavior.

Default auto-engage by unit family:

| Unit Family | Default Scan Auto-Engage |
|---|---|
| Infantry / foot squads | `ReturnFireOnly` |
| Ground combat vehicles | `ConfirmedHostilesOnly` |
| Recon/logistics vehicles | `ReturnFireOnly` or `Never`, based on weapon profile |
| Helicopters | `ConfirmedHostilesOnly` for armed helicopters; `ReturnFireOnly` for transports |
| Fixed-wing jets | `Never` unless profile is `ArmedRecon`; then `ConfirmedHostilesOnly` for one pass |
| Fixed-wing drones | `Never` if unarmed; `ConfirmedHostilesOnly` if armed and rules pass |
| Buildings / turrets | Radar scans never attack; turrets use normal defense rules |

Civilian/hidden hostile rule:

- A hostile hiding between civilians may be revealed or marked by scan, but scan does not automatically authorize fire.
- Auto-engage must be blocked when civilian density/risk is above threshold, even if the hostile is confirmed.
- HUD feedback should say `TARGET MARKED - CIVILIAN RISK` or `AUTO ENGAGE BLOCKED - CIVILIANS` instead of silently doing nothing.

## Mixed Selection Rules

When multiple selected units receive `HOLD`, `STOP`, or `SCAN`:

- `HOLD` applies to every selected unit that can hold. Units that cannot hold are skipped with partial-result feedback.
- `STOP` applies to every selected unit with an interruptible order. Fixed-wing units return safely; ground units stop locally; helicopters hover/return according to profile.
- `SCAN` uses one selected scan source by default, not every selected unit. The source should be the best valid scan-capable unit based on priority: explicitly selected primary unit, scout/fixed-wing drone/radar, recon vehicle, helicopter, aircraft, then mission scan source. If the UI later supports multi-source scan, it must show that clearly before execution.
- A mixed selection must never fire hidden orders for units that do not support the command.

Partial feedback examples:

- `3 units holding, 1 cannot hold.`
- `Jet returning to base. Infantry stopped.`
- `No scan-capable unit selected.`
- `Drone scanning route. Tank remains selected.`

## HUD Feedback Contract

Required player feedback:

| Situation | Required Feedback |
|---|---|
| `HOLD` accepted | Button press flash, selected panel/order display `HOLDING`, optional hold-radius marker for a short pulse. |
| `STOP` accepted for ground unit | Button press flash, selected panel/order display `STOPPING` then `IDLE`. |
| `STOP` accepted for jet | Button press flash, command banner `RETURNING TO BASE`, selected panel status `RETURNING`. |
| `SCAN` pressed | `SCAN` button active, banner `SCAN - TAP AREA`, target radius/shape preview. |
| Scan accepted | Scan pulse/marker, intel feed row, selected panel `SCANNING` or `RETURNING` for aircraft after pass. |
| Scan reveals hostile but blocks auto-engage | Threat marker plus reason: `TARGET MARKED - CIVILIAN RISK` or `AUTO ENGAGE BLOCKED`. |
| Command rejected | Typed invalid-command toast from canonical reason code. |

Button state requirements:

- `HOLD` and `STOP` must not stay visually selected after execution.
- `SCAN` must clear active state after valid execution, cancel/back, `SELECT`, another command, modal open, pause, or result route.
- If a unit remains in a persistent order state, show that in selected panel/card/order label, not by keeping the command button highlighted.

## Canonical Feedback Text

Use these exact player-facing strings for the current English implementation unless a localization table overrides them. Toast text is sentence case. Command banners and selected-unit status labels are uppercase because they are short tactical state labels.

Implementation rule: do not show raw enum names such as `UnitCannotHold` or `CommandLockedBySafetyPhase` to the player.

### `HOLD` Feedback

| Condition | Toast Text | Banner Text | Selected Status | Button Result | Reason Code |
|---|---|---|---|---|---|
| No selected unit | `Select a squad first.` | none | unchanged | rejected flash | `NoSelection` |
| Infantry/ground vehicle accepts hold | `Holding position.` | `HOLDING POSITION` | `HOLDING` | accepted flash | none |
| Infantry moves to immediate cover before hold | `Holding cover.` | `HOLDING COVER` | `HOLDING COVER` | accepted flash | none |
| Ground vehicle accepts hold on road/lane | `Holding lane.` | `HOLDING LANE` | `HOLDING` | accepted flash | none |
| Helicopter/VTOL accepts hold | `Holding orbit.` | `HOLDING ORBIT` | `HOLDING ORBIT` | accepted flash | none |
| Fixed-wing jet has authored loiter route | `Entering hold pattern.` | `HOLD PATTERN` | `ENTERING HOLD`, then `LOITERING` after joining loop | accepted flash | none |
| Fixed-wing jet has no loiter route and config converts to return | `No hold pattern. Returning to base.` | `RETURNING TO BASE` | `RETURNING` | accepted flash | `OrderCannotStopInPlace` |
| Fixed-wing jet has no loiter route and config rejects | `No hold pattern available.` | none | unchanged | rejected flash | `UnitCannotHold` |
| Fixed-wing jet is landing | `Jet is landing. Hold unavailable.` | `LANDING - COMMAND LOCKED` | `LANDING` | rejected flash | `CommandLockedBySafetyPhase` |
| Fixed-wing jet is taking off | `Jet is taking off. Hold unavailable.` | `TAKING OFF - COMMAND LOCKED` | `TAKING OFF` | rejected flash | `CommandLockedBySafetyPhase` |
| Aircraft is emergency returning | `Emergency return in progress.` | `RETURNING - EMERGENCY` | `RETURNING` | rejected flash | `CommandLockedBySafetyPhase` |
| Static building/turret accepts hold | `Guarding sector.` | `GUARDING SECTOR` | `GUARDING` | accepted flash | none |
| Unit cannot hold | `This unit cannot hold position.` | none | unchanged | rejected flash | `UnitCannotHold` |
| Mixed selection partial hold | `{acceptedCount} holding, {skippedCount} skipped.` | `PARTIAL HOLD` | mixed summary | accepted warning flash | `PartialCommandAccepted` |
| Unit already holding | `Already holding.` | `HOLDING POSITION` or current hold banner | current hold status | neutral/accepted flash | none |

### `STOP` Feedback

| Condition | Toast Text | Banner Text | Selected Status | Button Result | Reason Code |
|---|---|---|---|---|---|
| No selected unit | `Select a squad first.` | none | unchanged | rejected flash | `NoSelection` |
| Selected unit has no stoppable order | `No active order to stop.` | none | unchanged | rejected flash | `NoStoppableOrder` |
| Infantry/ground unit accepts stop | `Stopping.` | `STOPPING` | `STOPPING`, then `IDLE` | accepted flash | none |
| Ground vehicle must clear choke before stop | `Stopping at safe point.` | `STOPPING AT SAFE POINT` | `STOPPING` | accepted flash | none |
| Helicopter/VTOL accepts stop | `Holding hover.` | `HOLDING HOVER` | `HOVERING` | accepted flash | none |
| Helicopter/VTOL is landing | `Landing in progress.` | `LANDING` | `LANDING` | accepted or rejected safety flash based on profile | `CommandLockedBySafetyPhase` when rejected |
| Fixed-wing jet is attacking/moving/scanning | `Returning to base.` | `RETURNING TO BASE` | `RETURNING` | accepted flash | `OrderCannotStopInPlace` |
| Fixed-wing jet is already returning | `Already returning to base.` | `RETURNING TO BASE` | `RETURNING` | neutral/accepted flash | none |
| Fixed-wing jet is landing | `Landing in progress.` | `LANDING` | `LANDING` | neutral safety flash | `CommandLockedBySafetyPhase` if implementation treats it as reject |
| Fixed-wing jet is taking off | `Takeoff in progress.` | `TAKING OFF` | `TAKING OFF` | neutral safety flash | `CommandLockedBySafetyPhase` if implementation treats it as reject |
| Aircraft is emergency returning | `Emergency return in progress.` | `RETURNING - EMERGENCY` | `RETURNING` | rejected flash | `CommandLockedBySafetyPhase` |
| Building/turret/radar cancels active order | `Order cancelled.` | `ORDER CANCELLED` | `IDLE` or authored static status | accepted flash | none |
| Unit cannot be interrupted | `Command locked.` | `COMMAND LOCKED` | current status | rejected flash | `CommandLockedBySafetyPhase` |
| Mixed selection partial stop | `{acceptedCount} stopping, {skippedCount} skipped.` | `PARTIAL STOP` | mixed summary | accepted warning flash | `PartialCommandAccepted` |

### `SCAN` Feedback

| Condition | Toast Text | Banner Text | Selected Status | Button Result | Reason Code |
|---|---|---|---|---|---|
| `SCAN` pressed and target required | none | `SCAN - TAP AREA` | current status | active/selected | none |
| Scan canceled/back/other command | `Scan cancelled.` | none | previous status | neutral | none |
| Mission/global scan accepted | `Scanning area.` | `SCANNING AREA` | unchanged unless no selection | accepted flash | none |
| Infantry/scout scan accepted | `Scout scan started.` | `SCANNING AREA` | `SCANNING` | accepted flash | none |
| Ground vehicle scan accepted | `Sensor sweep started.` | `SENSOR SWEEP` | `SCANNING` | accepted flash | none |
| Helicopter scan accepted | `Air scan started.` | `AIR SCAN` | `SCANNING` or `HOLDING ORBIT` | accepted flash | none |
| Jet recon pass accepted | `Recon pass started.` | `RECON PASS` | `SCANNING`, then `RETURNING` | accepted flash | none |
| Fixed-wing drone scan accepted | `Drone scan route started.` | `DRONE SCAN` | `SCANNING ROUTE`, then `RETURNING` when complete | accepted flash | none |
| Static radar pulse accepted | `Radar pulse started.` | `RADAR PULSE` | authored static status | accepted flash | none |
| No scan-capable unit and no mission scan | `No scan source available.` | none | unchanged | rejected flash | `ScanUnavailable` |
| Selected unit cannot scan | `This unit cannot scan.` | none | unchanged | rejected flash | `UnitCannotScan` |
| Scan target out of bounds | `Target outside mission area.` | `SCAN - TAP AREA` if retry remains active | unchanged | rejected flash | `TargetOutOfBounds` |
| Scan target blocked/unreachable | `Cannot scan that area.` | `SCAN - TAP AREA` if retry remains active | unchanged | rejected flash | `TargetUnreachable` or `TargetBlocked` |
| Scan cooldown active | `Scan recharging: {seconds}s.` | none or current scan banner | unchanged | rejected flash | `AbilityOnCooldown` |
| Scan has no charges | `No scan charges available.` | none | unchanged | rejected flash | `ScanUnavailable` |
| Insufficient resources | `Not enough {resourceName}.` | none | unchanged | rejected flash | `InsufficientResources` |
| Jet is returning and cannot be recalled | `Jet is returning. Scan unavailable.` | `RETURNING TO BASE` | `RETURNING` | rejected flash | `CommandLockedBySafetyPhase` |
| Jet is landing or taking off | `Jet unavailable during flight transition.` | `COMMAND LOCKED` | `LANDING` or `TAKING OFF` | rejected flash | `CommandLockedBySafetyPhase` |
| Scan finds confirmed hostile and auto-engage allowed | `Hostile confirmed. Engaging.` | `HOSTILE CONFIRMED` | `ENGAGING` or current scan state | accepted combat pulse | none |
| Scan finds hostile but civilian risk blocks fire | `Target marked. Civilian risk too high.` | `AUTO ENGAGE BLOCKED` | current scan state | warning pulse | `CivilianRiskTooHigh` |
| Scan finds uncertain target | `Target not confirmed.` | `TARGET MARKED` | current scan state | warning pulse | `IntelConfidenceTooLow` |
| Scan completes with no contacts | `No contacts found.` | `SCAN COMPLETE` | previous/idle status | completion pulse | none |
| Mixed selection uses one scan source | `{sourceName} scanning.` | `SCANNING AREA` | source `SCANNING`, others unchanged | accepted flash | none |

Placeholder rules:

- `{acceptedCount}` and `{skippedCount}` are integers from the command result.
- `{seconds}` should be rounded up to the next whole second for player readability.
- `{resourceName}` should use the player-facing resource name, such as `Credits`, `Supplies`, `Command`, or `Fuel`.
- `{sourceName}` should use the selected unit or group display name, such as `Recon Drone`, `Recon APC`, or `Scout Squad`.
- If a string would exceed the available HUD toast width, the UI may use the shorter fallback in the same row's banner/status column, but the meaning must not change.

## Edge Cases

| Edge Case | Required Behavior |
|---|---|
| No selected unit presses `HOLD` or `STOP` | Reject `NoSelection`; no world click leak. |
| No selected unit presses `SCAN` | Use `MissionScan` only if available; otherwise reject `NoSelection` or `ScanUnavailable` depending mission config. |
| Selected unit cannot hold | Disable `HOLD` or reject `CommandUnavailable` / `UnitCannotHold` if that reason exists. |
| Selected unit has no active order and presses `STOP` | Disable `STOP` or reject with no stoppable order. Do not change state. |
| Selected unit cannot scan | If mission/global scan exists, use that source only if the UI clearly indicates it. Otherwise reject `ScanUnavailable` / `UnitCannotScan`. |
| Fixed-wing jet presses `STOP` while returning | Keep returning, clear aggressive follow-on behavior, show `RETURNING TO BASE`. |
| Fixed-wing jet presses `STOP` during attack or scan pass | Finish safe egress, return to base/staging, do not freeze or snap. |
| Fixed-wing jet receives `HOLD` without loiter support | Convert to return-to-base if configured, otherwise reject `CommandUnavailable`. |
| Helicopter presses `STOP` while landing/taking off | Finish safety-critical phase, then hover/land/idle according to profile. |
| Ground vehicle is inside a narrow choke | Stop at nearest safe segment end, not inside invalid geometry. |
| Unit is stunned, jammed, suppressed, out of fuel, or disabled | Follow authored emergency behavior; reject normal command if player control is unavailable. |
| Transport has passengers | `STOP` preserves passengers. `SCAN` disabled unless transport has sensor package. `HOLD` must not eject passengers. |
| Hidden hostile among civilians during scan | Reveal/mark based on confidence; auto-engage blocked unless civilian-risk and confidence checks pass. |
| Scan target is out of bounds or blocked | Reject typed reason; stay in scan targeting if UX supports correction, otherwise exit with reason. |
| Repeated button taps | Commands are idempotent: duplicate `HOLD` keeps hold, duplicate `STOP` does not reset return path, duplicate `SCAN` while targeting refreshes feedback but does not spend resources until execution. |
| Pause/result/modal opens during command mode | Cancel scan targeting and block world input. Immediate hold/stop results already accepted remain in gameplay state. |
| Tutorial disables command | Button disabled/neutral and no mutation. |

## Required Capability Data

Unit/building configs should expose command capability data in addition to existing movement, attack, fuel, and sensor data.

Recommended fields:

```text
canHold
holdProfile
holdRadiusMeters
holdEngagementPolicy

canStop
stopProfile
stopCancelsQueuedOrders
safeStopRequiresSegmentExit

canScan
scanProfile
scanRadiusMeters
scanDurationSeconds
scanCooldownSeconds
scanCostResourceId
scanCostAmount
scanRevealConfidence
scanAutoEngagePolicy
scanAutoEngageConfidenceMin
scanAutoEngageCivilianRiskMax
scanRepeatable

canLoiter
returnOnStop
returnAnchorId
safeExitBehavior
```

Implementation may store these as authored prefab config fields, catalog entries, ECS components, or generated runtime read models, but the command systems must not infer them from display names or sprite/icon choices.

## Suggested Runtime States

Command systems should report clear order states to the HUD:

```text
Idle
Moving
Attacking
Holding
Stopping
Scanning
ReturningToBase
Loitering
Landing
TakingOff
Jammed
Disabled
Destroyed
```

Fixed-wing aircraft and fixed-wing drones should use `ReturningToBase`, `ReturningToStation`, `Recovering`, `Loitering`, `Landing`, or `TakingOff` while still airborne. They must not use airborne `Stopping` or airborne `Idle`.

## Acceptance Tests

Implementation/review should prove:

- `HOLD` keeps selection, clears active targeting, enters a persistent hold/order state, and does not keep the button highlighted.
- `STOP` keeps selection, clears active targeting, cancels interruptible orders, and reports status through the selected panel.
- A ground unit can stop locally at a valid reachable cell.
- A ground vehicle in a narrow lane stops at a safe segment, not in invalid geometry.
- A helicopter can stop into hover/loiter when physically valid.
- `HOLD` pressed on a landing jet rejects with `CommandLockedBySafetyPhase`, shows `Jet is landing. Hold unavailable.`, keeps selected status `LANDING`, and does not queue hidden hold behavior.
- A fixed-wing jet never freezes in place after `STOP`; it returns to base/staging and reports `RETURNING`.
- Pressing `STOP` on an already returning jet keeps the return path and clears aggressive follow-on orders.
- A fixed-wing drone follows fixed-wing-style behavior: hold enters a route-based loiter, scan uses a route/corridor, and stop returns to station/recovery lane without airborne `STOPPING` or hover.
- `SCAN` enters targeting, accepts valid target areas, reveals intel, spends scan cost/charge only on accepted execution, and exits targeting after execution unless repeatable.
- A jet scan performs a scan pass and returns/loiters; it does not hover over the target.
- Scan auto-engage follows unit policy and blocks fire when confidence is too low or civilian risk is too high.
- Every `HOLD`, `STOP`, and `SCAN` accepted/rejected path uses the canonical feedback text matrix in this spec.
- Mixed selections produce partial-result feedback and do not issue unsupported hidden commands.
- Disabled/rejected commands use typed reason codes and never leak UI clicks into world commands.
