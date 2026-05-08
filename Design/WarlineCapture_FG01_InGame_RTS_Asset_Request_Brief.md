# FG01 In-Game RTS Asset Request Brief

Date: 2026-05-05

## Purpose

This is the first production request package for WarlineCapture's in-game 2D isometric RTS assets. It covers runtime world sprites only: soldiers, vehicles, aircraft, ships, buildings, construction/destruction states, and tactical VFX overlays.

UI portraits, thumbnails, mode-card art, mission art, and reward/unlock renders are a separate request lane and should not be mixed into this package.

## Source Of Truth

- Request manifest: `Assets/Game/Art/Generated/2DISO/Manifests/FG01_GameplayAssetVerticalSlice_Manifest.json`
- Visual config: `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`
- Art bible: `Design/WarlineCapture_2D_Isometric_Art_Bible.md`
- Visual validation scene: `Assets/Game/Scenes/DesignTargets/FG01_CoastalCommand_UsableIsoMap.unity`
- Visual target: `Assets/Game/Art/Generated/IsometricMaps/Previews/FG-L01_CoastalCommand_Preview.png`

## Shared Direction

All assets must match the WarlineCapture fictional Gulf premium 2D isometric mobile RTS direction:

- fixed isometric gameplay camera
- transparent/alpha-ready runtime sprites
- stable pivots and consistent scale
- readable silhouettes at mobile RTS zoom
- warm upper-left key light and cool readable shadows
- friendly blue/cyan accents and enemy red accents where relevant
- no UI, health bars, selection rings, objective markers, real flags, real insignia, real landmarks, political text, readable signs, or baked gameplay stats

## Requested Runtime Assets

This request has two layers:

1. **FG01 vertical slice:** the first assets needed to validate style, scale, pivots, sorting, and gameplay readability.
2. **Full production coverage backlog:** all remaining soldiers, vehicles, aircraft, ships, buildings, projectiles, explosions, fire, smoke, and missile trails required for the complete RTS asset set.

### Infantry

1. Rifle Soldier
   - Visual id: `visual.unit.chr.soldier.male.02`
   - Entity id: `Unit_Chr_Soldier_Male_02`
   - States: `idle`, `walk`, `run`, `aim`, `fire`, `hit`, `death`
   - Facings: `NE`, `SE`, `SW`, `NW`

2. Rocket Soldier
   - Visual id: `visual.unit.chr.ghillie.male.01`
   - Entity id: `Unit_Chr_Ghillie_Male_01`
   - States: `idle`, `walk`, `run`, `aim`, `fire`, `reload`, `hit`, `death`
   - Facings: `NE`, `SE`, `SW`, `NW`

3. Civilian
   - Visual id: `visual.unit.chr.civilian.male.01`
   - Entity id: `Unit_Chr_Civilian_Male_01`
   - States: `idle`, `walk`, `run_panic`, `cower`, `downed`
   - Facings: `NE`, `SE`, `SW`, `NW`

### Vehicles

1. APC
   - Visual id: `visual.unit.veh.apc.slow`
   - Entity id: `Unit_Veh_APC_Slow`
   - States: `idle`, `move`, `turn`, `fire`, `damaged`, `destroyed`

2. Battle Tank
   - Visual id: `visual.unit.veh.tank.usa`
   - Entity id: `Unit_Veh_Tank_USA`
   - States: `idle`, `move`, `turn`, `turret_fire`, `damaged`, `destroyed`

### Air

1. Transport Helicopter
   - Visual id: `visual.unit.veh.helicopter.transport`
   - Entity id: `Unit_Veh_Helicopter_Transport`
   - States: `hover`, `move`, `land`, `takeoff`, `rotor_loop`, `damaged`, `destroyed`

2. Attack Helicopter
   - Visual id: `visual.unit.veh.helicopter.attack`
   - Entity id: `Unit_Veh_Helicopter_Attack`
   - States: `hover`, `move`, `fire`, `rotor_loop`, `damaged`, `destroyed`

### Sea

1. Harbor Patrol Boat
   - Visual id: `visual.unit.sea.patrol.boat`
   - Entity id: `Unit_Sea_Patrol_Boat`
   - States: `move`, `turn`, `fire`, `damaged`, `destroyed`

### Buildings

1. Command Post
   - Visual id: `visual.building.forward.command.post`
   - Entity id: `Building_ForwardCommandPost`
   - States: `locked`, `construction`, `intact`, `upgraded_t1`, `upgraded_t2`, `damaged`, `heavily_damaged`, `destroyed`

2. Barracks
   - Visual id: `visual.building.barracks`
   - Entity id: `Building_Barracks`
   - States: `locked`, `construction`, `intact`, `upgraded_t1`, `upgraded_t2`, `damaged`, `destroyed`

3. Helipad
   - Visual id: `visual.building.helipad`
   - Entity id: `Building_Helipad`
   - States: `locked`, `construction`, `intact`, `upgraded_t1`, `upgraded_t2`, `damaged`, `destroyed`

4. Guard Tower
   - Visual id: `visual.building.guard.tower`
   - Entity id: `Building_GuardTower`
   - States: `locked`, `construction`, `intact`, `upgraded_t1`, `upgraded_t2`, `damaged`, `destroyed`

### VFX And Runtime State Overlays

1. Construction and destruction overlays:
   - `construction_scaffold`
   - `construction_dust`
   - `scorch`
   - `rubble_small`
   - `rubble_large`
   - `fire_loop`
   - `smoke_loop`

2. Combat and movement VFX:
   - `muzzle_flash`
   - `hit_spark`
   - `missile_trail`
   - `rocket_backblast`
   - `rotor_dust`
   - `boat_wake`

## Full Production Coverage Backlog

The manifest also requests full project coverage beyond the first FG01 slice:

### Explosions, Fire, Smoke, Missiles, And Trails

- small, medium, and large ground explosions
- vehicle explosion
- building explosion
- airburst
- water impact explosion
- small and large fire loops
- black and white smoke loops
- dust clouds
- burning vehicle and burning building overlays
- rocket projectile
- missile projectile
- tank shell tracer
- anti-air missile
- air-to-ground missile
- short and long missile trails
- rocket backblast
- metal/concrete hit sparks
- water splash impact

### All Soldier/Character Animation Sets

The full backlog covers every character visual id listed in `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`, including:

- bomb suit specialist
- civilians
- contractors
- insurgents
- field commander
- pilots
- soldiers and soldier variants
- marksman, rifle, rocket, sniper, sidearm, gunner, and support silhouettes

Combatant states:

- `idle`
- `walk`
- `run`
- `crouch`
- `aim`
- `fire`
- `reload`
- `throw_or_ability`
- `hit`
- `suppressed`
- `death`

Civilian states:

- `idle`
- `walk`
- `run_panic`
- `cower`
- `help_wave`
- `downed`

### All Ground Vehicle Animation Sets

The full backlog covers:

- Fast APC
- Heavy APC
- Armored APC
- Light Armored Car
- Air Missile Launcher
- Ground Missile Launcher
- Radar Tank
- Battle Tank
- Canopy Truck
- Tanker Truck
- Cargo Truck

Vehicle states include:

- `idle`
- `move`
- `turn`
- `fire` or `fire_missile`
- `deploy` where applicable
- `scan_loop` where applicable
- `turret_rotate` and `turret_fire` where applicable
- `damaged`
- `destroyed`

### All Air Unit Animation Sets

The full backlog covers:

- Recon Drone
- Attack Helicopter
- Light Attack Helicopter
- Transport Helicopter
- Strike Jet
- Fighter Jet
- Transport Plane

Air states include:

- `hover` where applicable
- `fly` or `move`
- `bank_turn`
- `land`
- `takeoff`
- `rope_drop`
- `fire`
- `fire_missile`
- `bomb_or_strike`
- `air_to_ground_strike`
- `rotor_loop`
- `afterburner_trail`
- `damaged`
- `destroyed`

### All Sea Unit Animation Sets

The full backlog covers:

- Coastal Cutter
- Drone Boat
- Interceptor Boat
- Landing Craft
- Missile Patrol Craft
- Harbor Patrol Boat

Sea states include:

- `move`
- `turn`
- `fire`
- `fire_missile`
- `scan_or_attack`
- `beach_or_unload`
- `damaged`
- `destroyed`
- `wake_loop`

### Building State Sets

The full backlog covers priority gameplay buildings from the visual config, including:

- Forward Command Post
- Barracks
- Helipad
- Guard Tower
- Airport
- Ammunition Depot
- Coastal Radar Station
- Logistics Dock
- Field Workshop
- Fuel Bladder
- Heavy Guard Tower
- City Hall
- Medical Station
- Naval Yard
- Oil Pump
- Oil Refinery
- Satellite Dish
- Water Tank

Building states:

- `locked`
- `construction`
- `intact`
- `upgraded_t1`
- `upgraded_t2`
- `upgraded_t3`
- `damaged`
- `heavily_damaged`
- `destroyed`

## Delivery Requirements

Each delivered asset must include:

- transparent PNG frame sequences or sprite sheets
- source prompt/brief
- visual id and entity id
- state list and frame count
- facing list
- Unity import path
- pivot/contact-shadow notes
- scale notes relative to FG-L01 roads and sockets
- any known limitations

## Import Paths

- `Assets/Game/Art/Generated/2DISO/Units/`
- `Assets/Game/Art/Generated/2DISO/Vehicles/`
- `Assets/Game/Art/Generated/2DISO/Air/`
- `Assets/Game/Art/Generated/2DISO/Sea/`
- `Assets/Game/Art/Generated/2DISO/Buildings/`
- `Assets/Game/Art/Generated/2DISO/VFX/`

## Acceptance

The request is not accepted until the assets can be placed over the `FG-L01` scene at gameplay zoom and still read clearly without UI overlays. Buildings must align to sockets. Vehicles and units must preserve stable pivots across animation frames. VFX must layer over runtime entities without changing terrain art.
