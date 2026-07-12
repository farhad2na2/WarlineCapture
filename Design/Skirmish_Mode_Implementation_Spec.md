# WarlineCapture Skirmish Mode Implementation Spec

Date: 2026-05-22
Status: Active implementation spec

## Purpose

Skirmish is the first production game mode to implement because it can exercise the existing RTS simulation, AI settings, economy pacing, unit production, building placement, combat, selection, transport, and balance probes without waiting for the full Campaign or Operations progression layer.

Player-facing language is `Skirmish`. Internal runtime names may continue to use `QuickCustom`, `QuickGame`, and `QuickCustomSetup` until the migration is complete.

## Design Contract

Skirmish must follow the active 3D single-map direction:

- one large 3D operation map per match
- no separate strategic map and tactical map
- no 2.5D or isometric presentation target
- many soldiers, civilians, hostile cells, vehicles, aircraft, buildings, and support assets in the same world
- command-base UI style for setup and result screens
- config-backed names and descriptions from `Assets/Game/Configs/Prefabs`
- fast replay, AI tuning, and balance testing without becoming the main progression farm

Skirmish answers this player need:

```text
Choose a combat scenario, tune the opposing force and rules, deploy into a 3D town/base operation, review the result, then replay or adjust the setup.
```

## Player Flow

```text
Splash / Loading
  -> Main Menu
    -> Skirmish card or Skirmish left-rail item
      -> SCN-13 Skirmish Setup
        -> Launch Mission
          -> 3D Operation Map / Battle HUD
            -> Mission Result / Skirmish Report
              -> Replay
              -> Adjust Setup
              -> Main Menu
```

Safe returns:

- Back from Skirmish Setup returns to Main Menu and saves the last valid setup.
- Pause during match can resume, restart, or exit to Main Menu.
- Mission Result can replay the same setup, return to setup with values preserved, or return to Main Menu.

## Current Code Baseline

The project already has a partial Skirmish-compatible foundation:

| Runtime Area | Existing File / System | Use In First Slice |
|---|---|---|
| Setup model | `Assets/Game/Scripts/UI/Screens/QuickGameConfig.cs` | Keep as the first Skirmish config model. |
| Setup UI controller | `Assets/Game/Scripts/UI/Screens/QuickCustomScreenController.cs` | Keep route compatibility, but player-facing screen text should read Skirmish. |
| AI runtime mapping | `AISettingsRuntimeState` | `QuickGameConfig.ApplyToRuntimeState()` already maps core AI knobs. |
| Launch fallback | `WarlineCaptureGameLaunchUtility` and `GameBootstrap.BeginGameplay()` | Use until `BeginGameplay(GameLaunchPayload payload)` exists. |
| Temporary session fallback | `ChapterOneMissionCatalog.FirstContactMissionId` | Valid temporary fallback for Skirmish launch until map/preset selection is bound. |
| Save compatibility | `quickgame.json` / `QuickGameSaveData` | Keep filename until save migration. |
| Balance probes | `QuickCustom_Default_Medium`, `QuickCustom_Hard_Swarm` | Rename only in player-facing/report labels when safe. |

Do not block the first Skirmish slice on a full rename. The implementation should hide legacy naming from players while preserving working runtime paths.

## First Shippable Slice

### Scope

Implement one usable Skirmish path:

```text
Main Menu -> Skirmish Setup -> Launch Mission -> current 3D match -> Result -> Replay / Setup / Main Menu
```

The first slice may use the M01/First Contact scenario fallback, but it must be wrapped as a Skirmish session and must apply the selected AI/economy controls before gameplay starts.

### Required Setup Controls

| Control | Data Field | First Slice Behavior |
|---|---|---|
| Preset | `PresetId` or compatibility selection | Defaults to `Skirmish Tutorial Intercept`. |
| Enemy Type | `QuickGameConfig.EnemyType` | Balanced, Military, Defensive, Air, Swarm, Random. Disable unimplemented behavior with a clear locked reason. |
| Enemy Count | `QuickGameConfig.EnemyCount` | Stepper, clamp 1 to 3. |
| Difficulty | `QuickGameConfig.Difficulty` | Easy, Normal, Hard, Brutal. |
| Starting Credits | `QuickGameConfig.StartingMoney` | Player-facing label is Credits, not Money. |
| Income Multiplier | `QuickGameConfig.IncomeMultiplier` | Clamp 0.5 to 3.0. |
| Build Speed | `QuickGameConfig.BuildSpeed` | Slow, Normal, Fast. |
| Unit Production Speed | `QuickGameConfig.UnitProductionSpeed` | Slow, Normal, Fast. |
| Attack Group Size | `QuickGameConfig.AttackGroupSize` | Small, Normal, Large. |
| Attack Frequency | `QuickGameConfig.AttackFrequency` | Rare, Normal, Frequent. |
| Aggression | `QuickGameConfig.Aggression` | Defensive, Balanced, Aggressive. |
| Expansion | `QuickGameConfig.Expansion` | Off, Slow, Normal, Fast. |
| Target Priority | `QuickGameConfig.TargetPriority` | Balanced and implemented runtime priorities only. |
| Player Auto AI | `QuickGameConfig.PlayerAutoAIEnabled` | Developer/test utility; hide or mark advanced in production. |
| Win Condition | `QuickGameConfig.WinCondition` | Destroy All Enemies, Survive Duration, Sandbox. |
| Intel Reveal | `QuickGameConfig.IntelReveal` | Enabled in first slice. |
| Fog Of War | `QuickGameConfig.FogOfWar` | Designed unavailable until fog simulation is active. |
| Starting Resources | `QuickGameConfig.StartingResources` | Standard, Low, High. |
| Map Seed | `QuickGameConfig.MapSeed` | Numeric field with reset/randomize support. |
| Operation Map | future `OperationMapId` | First slice can lock to the validated 3D map fallback. |

### Launch Behavior

On `Launch Mission`:

1. Read and validate `QuickGameConfig`.
2. Save last valid setup.
3. Apply values to `AISettingsRuntimeState`.
4. Create or update a Skirmish session marker.
5. Use current M01/First Contact scenario fallback if no Skirmish operation-map preset exists.
6. Start gameplay through the existing bootstrap path.
7. Result screen must know the source route was Skirmish.

Acceptance check: changing enemy count, difficulty, income, aggression, and win condition in Skirmish Setup must be visible in runtime state or result/debug report.

## Production Skirmish Data Model

Keep `QuickGameConfig` for compatibility, but evolve toward this player-facing contract:

```text
SkirmishSessionConfig
  SessionId
  PresetId
  OperationMapId
  ScenarioSetupId
  ObjectivePresetId
  PlayerRosterPresetId
  EnemyProfileId
  EnemyCount
  Difficulty
  EconomyPacing
  AIBehaviorTuning
  CivilianDensity
  HostileIntelConfidence
  Rules
  MapSeed
  ReturnRoute
```

Do not persist raw ECS world state for Skirmish. Persist only the last setup, selected preset, result summary, and balance/report data.

## Skirmish Match Quick Groups

Skirmish uses the `SCN-08` squad tray differently from authored Campaign missions.

Source of truth: `Match_HUD_And_Gameplay_Implementation_Spec.md`.

Rules:

- The four bottom squad-tray cards are dynamic recommended command groups.
- They are populated from the player's current fielded units and tactical situation, not from an authored mission list.
- Each card selects the specific recommended group assigned to that slot.
- A card does not select every unit of the same type globally unless those units are explicitly part of the same command group.
- The runtime should prefer useful groups: active selection, groups under attack, objective-critical groups, idle groups needing attention, armor, transport, air/support, builders, repair, anti-air, or scouts.
- If the player has airplanes instead of helicopters, the air/support card should show the airplane group. Helicopter is only a default visual example, not a fixed rule.
- If no useful group exists for a slot, the slot is hidden/collapsed or disabled with a clear reason.
- Future player pinning may lock a group into a slot; pinned groups should not be replaced by dynamic recommendation until unpinned or invalidated.

## Starter Presets

Skirmish presets should mirror Campaign/Operations mission archetypes so testing remains useful.

| Player-Facing Preset | Compatibility Id | Purpose | Recommended First State |
|---|---|---|---|
| Skirmish Tutorial Intercept | `QuickCustom_Tutorial_Intercept` | Low-pressure movement, attack, selection, civilian readability, and basic hostile contact. | First implemented preset. |
| Convoy Pressure | `QuickCustom_BaseDefense_Convoy` | Tune convoy attack timing, radar warning pressure, and route defense. | Locked until convoy runtime is validated. |
| Airlift Extraction | `QuickCustom_Airlift_Extraction` | Validate transport aircraft, landing-zone pressure, and extraction timing. | Locked until airlift loop is validated. |
| Breach Assault | `QuickCustom_Breach_Assault` | Tune fortified compounds, road barriers, walls, guard towers, and combined-arms pressure. | Locked until breach tools are validated. |
| Hidden Cell Raid | `QuickCustom_Hidden_Cell_Raid` | Test hostile cells embedded near civilians, houses, shops, alleys, and restricted-fire zones. | Design target for the first 3D town map. |

Locked presets should still be visible as future content if the visual design supports locked cards, but Launch must be disabled with a clear reason.

## Roster And Prefab Usage

Skirmish must not invent separate public names for units or buildings. It should read display names and descriptions from:

```text
Assets/Game/Configs/Prefabs
```

Useful first-slice examples:

| Gameplay Role | Config-Backed Examples |
|---|---|
| Player infantry | Heavy Gunner Male I, Rifleman Male II, Rifleman Female II, Bomb Suit Specialist, Ghillie Rocketeer |
| Hostile cells | Insurgent Rocketeer Male I, Insurgent Raider Male III, Insurgent Sniper Male IV, Insurgent Rifleman Male V |
| Civilians | Civilian Male I, Civilian Male II, Civilian Female I, Civilian Female II |
| Fast movement | Fast APC, Light Armored Car, Cargo Truck, Canopy Truck |
| Recon and air | Recon Drone, Attack Helicopter, Light Attack Helicopter, Transport Helicopter |
| Base and defense | Barracks, Guard Tower, Heavy Guard Tower, Field Fabrication Depot, Helipad, Satellite Dish |
| Town/civilian context | House, Shop, Market Shop, City Hall, Refugee Tent |
| Barriers and routes | Road Barrier, Dirt Wall, Fence Wall |

Roster inspection from Skirmish Setup should route to `SCN-19 Armory` or a compact read-only detail popup, not create a separate one-off Skirmish-only unit encyclopedia.

## UI Specification

`SCN-13 Skirmish Setup` should use the command-base visual style established by the new Main Menu direction.

Required screen regions:

| Region | Content |
|---|---|
| Top header | Back, title `SKIRMISH`, Credits/Supplies/Command resource strip, Settings shortcut. |
| Left preset rail | Preset cards with lock/readiness state and compact objective type. |
| Center operation preview | 3D operation-map preview image, selected preset name, objective, map seed, civilian-risk/intel-confidence badges. |
| Right rule panel | Enemy, difficulty, economy, aggression, objective, fog/intel, starting resources. |
| Bottom action bar | Reset, Randomize Seed, Launch Mission. |

Control rules:

- Numeric values use minus/value/plus steppers where practical.
- Sliders are acceptable for continuous values such as income multiplier.
- Dropdown labels must not touch dropdown boxes.
- Locked controls must show the reason: `Requires Fog Runtime`, `Preset Not Validated`, `Dev Only`, or `Coming In Operations`.
- The primary CTA label is `LAUNCH MISSION`.

## Gameplay Rules

### Win Conditions

First implemented:

- Destroy All Enemies
- Survive Duration
- Sandbox

Next:

- Capture Objective
- Defend Civilians
- Extract Convoy
- Neutralize Cell Leader
- Breach Compound

### Civilian And Collateral Rules

Even in Skirmish, civilians are part of the AAA direction. First slice can start with readable civilian presence and result counters. Later slices should add:

- civilian casualties
- civilian panic
- restricted-fire zones
- infrastructure damage
- collateral penalty
- intel confidence changes

Skirmish result should report these as training/balance metrics, not as permanent Operations district consequences unless the session was launched from Operations.

### Rewards

Skirmish should not bypass Campaign or Operations progression.

First slice:

- no Campaign stars
- no Operations district mutation
- optional small repeatable Credits/Supplies reward if economy design allows it
- result report focused on time, enemy defeated, casualties, civilian safety, buildings lost, and AI pressure

## Implementation Milestones

### S1 - Production Skirmish Launch

- Main Menu Skirmish card opens `SCN-13`.
- Setup values bind to `QuickGameConfig`.
- Launch applies `AISettingsRuntimeState`.
- Existing gameplay starts from Skirmish.
- Result route returns to Skirmish, Main Menu, or replay.

### S2 - Presets And Map Contract

- Add preset definitions.
- Add `OperationMapId` or equivalent map preset field.
- Bind map preview to a validated 3D operation scene/capture.
- Disable unvalidated presets with reasons.

### S3 - Objectives And Result Report

- Bind first win conditions to objective/result runtime.
- Add Skirmish result metrics.
- Add replay and adjust-setup payload flow.

### S4 - 3D Town Combat Validation

- Validate hidden-cell raid in a 3D Middle Eastern-inspired town map.
- Include civilians, shops/houses, road barriers, guard towers, and hostile ambush positions.
- Capture performance/readability metrics for many soldiers/units.

### S5 - Balance Probe Integration

- Map presets to automated probes.
- Write Markdown/JSON result reports.
- Keep opt-in balance probes explicit and non-blocking for normal tests.

## Required Tests

Add or keep tests covering:

- `QuickGameConfig_AppliesToAISettingsRuntimeState`
- Skirmish setup reads/writes all visible controls.
- Enemy count clamps to 1 to 3.
- Launch applies difficulty, enemy count, income, build speed, production speed, aggression, expansion, and target priority.
- Launch starts gameplay from Skirmish route.
- Back returns to Main Menu without starting gameplay.
- Last valid setup persists and reloads.
- Locked fog/preset controls cannot launch unsupported rules.
- Result route supports Replay, Adjust Setup, and Main Menu.

## Acceptance Gate

Skirmish is implementation-ready when:

- The player can reach it from Main Menu using player-facing `Skirmish` language.
- The setup screen matches the command-base UI direction.
- Launch applies selected AI/economy/rule values to gameplay.
- The match plays on the current 3D operation-map runtime path.
- The result flow can return to setup or Main Menu.
- The first preset can be used for manual QA and automated balance probes.
- No active doc describes Skirmish as 2.5D, isometric, or a separate strategic/tactical-map mode.
