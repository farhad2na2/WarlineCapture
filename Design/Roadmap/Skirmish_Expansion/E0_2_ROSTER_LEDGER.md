# E0.2 Roster Ledger

**Status:** E0.2 deliverable (source/config inspection; not a gameplay or availability claim)  
**Package:** Skirmish Expansion E0.2  
**Repo:** `farhad2na2/WarlineCapture`  
**Inspected branch:** `codex/m03-radar-warning` (`61d7fcd12f76`, commit date 2026-09-20)  
**Method:** GitHub API + `raw.githubusercontent.com` only (no clone)  
**Authority:** `Design/Roadmap/Skirmish_Expansion/{BASELINE,PLAN,DELIVERY}.md`  
**Inventory roots:** `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_*` (51), `Prefab_BuildingDefinition_*` (23)

Disposition enum (required): `standard roster` | `role variant` | `alt-faction` | `scenario-Sandbox-only` | `blocked`

Flags used in Notes: **DUP-NAME**, **NONCOMBATANT**, **MISSING-PRODUCER**, **MISSING-COUNTER**, **UNIMPLEMENTED-ABILITY**, **BAD-PRODUCER**, **POP-CAP-ZERO**, **SKIRMISH-FLATTEN**

---

## Evidence summary (how claims were made)

| Claim area | Evidence path | What was seen |
|---|---|---|
| Inventory counts | `Assets/Game/Configs/Prefabs/` tree on branch | Exactly **51** `Prefab_UnitGrid_*.asset`, **23** `Prefab_BuildingDefinition_*.asset` (matches BASELINE.md) |
| Unit fields | each `Prefab_UnitGrid_*.asset` YAML | `displayName`, `description`, `canRequest`, `price`, `productionDurationSeconds`, combat/transport/threat fields; **`materialsCost` / `groundFuelPerCell` / `airFuelPerCell` almost never serialized** |
| Building fields | each `Prefab_BuildingDefinition_*.asset` YAML | `displayName`, `price`, `materialsCost`, `productions[].spawnUnitPrefab` (+ Barracks `quantity: 4`) |
| Prefab GUID → unit | `Assets/Game/Prefabs/{Characters,Vehicles}/Unit_*.prefab.meta` | GUID map used to resolve producer spawn lists |
| Skirmish pop caps | `Assets/Game/Scripts/Systems/SkirmishPopulationPolicy.cs` | Name-key limits: `soldier`→`InfantryLimitPerFaction` (24), `truck_tray`→2, `truck_tanker`→1, **else limit 0 / cannot queue** |
| Skirmish combat flatten | `Assets/Game/Scripts/Systems/SkirmishCombatPolicy.cs` `ApplyRoster` | Any prefab key containing `soldier` gets same rifle range/damage/cooldown from preset — specialists **not** differentiated at runtime in Skirmish |
| Catalog allowlist | `Assets/Game/Scripts/Systems/SkirmishCatalogPolicy.cs` | Prefab must appear on preset buildingPlacement unit/building spawn lists |
| Production spend | `BuildingProductionCampRequestTransaction.cs`, `BuildingDefinitionUnitProductionResources.cs` | Resolves credits + materials; metadata `Price` mapped into materials path in helper (see E0.5 gaps) |
| Fuel runtime | `VehicleFuelConsumptionSystem.cs`, `GroundVehicleFuelHoldSystem.cs`, `AircraftFuelSafetyReturnSystem.cs`, `UnitGridAuthoring.cs` baker | Consumes `GroundFuelPerCell` / `AirFuelPerCell`; **rates not present on UnitGrid YAML** → UNKNOWN pending authoring defaults / config SO |
| Authoring defaults | `UnitGridAuthoring.cs` | `canRequest` defaults **true** if field omitted; `MaterialsCost` falls back to `UnitGridAuthoringConfig.ResolveLegacyMaterialsCost(price)` (definition **not** found on branch via API search) |

---

## Producer map (resolved spawn GUIDs)

| Building (displayName) | Config asset | Resolved produced units (prefab stem) | Notes |
|---|---|---|---|
| Barracks | `.../Prefab_BuildingDefinition_Building_Barrack_Config.asset` | `Unit_Chr_Soldier_Male_02_Alt_04` × **quantity 4** | Only Barracks production entry (matches BASELINE) |
| Soldier Tent | `.../Tent_Regular_Config.asset` | 10 soldier appearance/weapon alts (incl. Barracks rifleman) | Campaign-style tent producer |
| Expert Tent | `.../Tent_Expert_Config.asset` | Soldiers + Ghillie + both Pilots | — |
| Contractor Tent | `.../Tent_Contractor_Config.asset` | 3 Security Contractors | — |
| Refugee Tent | `.../Tent_Refugee_Config.asset` | 4 Civilians | Noncombatants |
| Helipad | `.../Helipad_Config.asset` | Light Attack Heli, Attack Heli, Transport Heli | 3 entries (matches BASELINE) |
| Airport | `.../Airport_Config.asset` | Transport Plane, Recon Drone, Strike Jet, Fighter Jet | 4 entries (matches BASELINE) |
| Fuel Bladder | `.../Fuel_Bladder_Config.asset` | **All 11 ground combat/logistics vehicles** | **BAD-PRODUCER**: fuel storage config lists vehicle spawns |
| Water Tank | `.../WaterTank_Config.asset` | Same 3 helis as Helipad | **BAD-PRODUCER**: copy-paste of Helipad productions |

**Units with no producer reference:** Bomb Suit Specialist; all 7 Insurgents; Field Commander.

---

## Unit ledger (51)

Columns: StableId (config filename stem) | displayName | Role family | canRequest | price | Producer(s) | Disposition | Flags / evidence notes

### Infantry — regular / contractor / specialist

| StableId | displayName | weaponDisplayName | Role family | canReq | price | Producer | Disposition | Notes |
|---|---|---|---|---:|---:|---|---|---|
| Chr_Soldier_Male_02_Alt_04 | Rifleman Male IV | Rifle | Rifleman | 1 | 10000 | Barracks (×4), Soldier Tent | **standard roster** | Only Barracks product; materialsCost serialized `0` on this asset only |
| Chr_Soldier_Male_02 | Rifleman Male II | Rifle | Rifleman | 1 | 10000 | Expert Tent | **role variant** | **DUP-NAME** with Male_02_Alt_02 |
| Chr_Soldier_Male_02_Alt_02 | Rifleman Male II | Rifle | Rifleman | 1 | 10000 | Soldier Tent | **role variant** | **DUP-NAME** with Male_02 |
| Chr_Soldier_Male_02_Alt_01 | Sidearm Specialist Male I | Metal Pistol | Sidearm | 1 | 8500 | Soldier Tent | **role variant** | Weak distinct role; **SKIRMISH-FLATTEN** if key contains soldier |
| Chr_Soldier_Male_02_Alt_03 | Sidearm Specialist Male III | Pistol | Sidearm | 1 | 8500 | Soldier Tent | **role variant** | same |
| Chr_Soldier_Male_01 | Heavy Gunner Male I | Machine Gun | Heavy Gunner | 1 | 14000 | Expert Tent | **standard roster** | PLAN ground-core candidate; **SKIRMISH-FLATTEN** risk |
| Chr_Soldier_Male_01_Alt_01 | Marksman Male I | Sniper Rifle | Marksman | 1 | 11500 | Soldier Tent | **standard roster** | PLAN marksman; **SKIRMISH-FLATTEN** |
| Chr_Soldier_Male_01_Alt_02 | Advanced Rifleman Male II | Advanced Rifle | Rifleman | 1 | 12500 | Soldier Tent | **role variant** | — |
| Chr_Soldier_Female_01 | Marksman Female I | Sniper Rifle | Marksman | 1 | 11500 | Expert Tent | **role variant** | appearance/gender variant of marksman |
| Chr_Soldier_Female_01_Alt_01 | Rifleman Female I | Rifle | Rifleman | 1 | 10000 | Soldier Tent | **role variant** | **DUP-NAME** with Female_02_Alt_01 |
| Chr_Soldier_Female_01_Alt_02 | Marksman Female II | Sniper Rifle | Marksman | 1 | 11500 | Soldier Tent | **role variant** | — |
| Chr_Soldier_Female_02 | Rifleman Female II | Rifle | Rifleman | 1 | 10000 | Expert Tent | **role variant** | — |
| Chr_Soldier_Female_02_Alt_01 | Rifleman Female I | Rifle | Rifleman | 1 | 10000 | Soldier Tent | **role variant** | **DUP-NAME** with Female_01_Alt_01 |
| Chr_Soldier_Female_02_Alt_02 | Assault Breacher Female II | SMG | Assault Breacher | 1 | 13000 | Soldier Tent | **standard roster** | PLAN breacher; **SKIRMISH-FLATTEN** |
| Chr_Ghillie_Male_01 | Ghillie Rocketeer | Rocket Launcher | Anti-armor infantry | 1 | 16000 | Expert Tent | **standard roster** | PLAN rocketeer; dmg 260 / range 115 in YAML; **MISSING-COUNTER** until armor differentiated; **SKIRMISH-FLATTEN** if name key still `soldier` |
| Chr_Contractor_Female_01 | Security Contractor Female I | SMG | Contractor | 1 | 9000 | Contractor Tent | **scenario-Sandbox-only** | PLAN extended roster; not ground-core |
| Chr_Contractor_Male_01 | Security Contractor Male I | Pistol | Contractor | 1 | 9000 | Contractor Tent | **scenario-Sandbox-only** | — |
| Chr_Contractor_Male_02 | Security Contractor Male II | Rifle | Contractor | 1 | 9500 | Contractor Tent | **scenario-Sandbox-only** | — |
| Chr_Bombsuit_Male_01 | Bomb Suit Specialist | *(none)* | EOD specialist | DEFAULT(true) | 18000 | **NONE** | **blocked** | **MISSING-PRODUCER**; description claims EOD/hazard role — **UNIMPLEMENTED-ABILITY** (no disposal system evidenced); `canRequest` field omitted→authoring default true |
| Chr_Leader_Male_01 | Field Commander | Metal Pistol | Hero/leader | 0 | 20000 | **NONE** | **scenario-Sandbox-only** | **MISSING-PRODUCER**; canRequest=0 |
| Chr_Pilot_Female_01 | Pilot Female I | Compact Pistol | Air crew | 0 | 7000 | Expert Tent | **scenario-Sandbox-only** | canRequest=0 — listed on tent but not requestable; treat as crew/prop |
| Chr_Pilot_Male_01 | Pilot Male I | Compact Pistol | Air crew | 0 | 7000 | Expert Tent | **scenario-Sandbox-only** | same |

### Infantry — alt-faction (Insurgent)

| StableId | displayName | weapon | canReq | price | Producer | Disposition | Notes |
|---|---|---|---:|---:|---|---|---|
| Chr_Insurgent_Female_01 | Insurgent Rifleman Female I | Rifle | 0 | 8500 | NONE | **alt-faction** | **MISSING-PRODUCER** for player roster |
| Chr_Insurgent_Female_02 | Insurgent Sidearm Fighter Female II | Pistol | 0 | 7500 | NONE | **alt-faction** | — |
| Chr_Insurgent_Male_01 | Insurgent Rocketeer Male I | RPG | 0 | 15000 | NONE | **alt-faction** | Mirror of Ghillie role for opposing roster |
| Chr_Insurgent_Male_02 | Insurgent Gunner Male II | Machine Gun | 0 | 12000 | NONE | **alt-faction** | — |
| Chr_Insurgent_Male_03 | Insurgent Raider Male III | SMG | 0 | 9000 | NONE | **alt-faction** | — |
| Chr_Insurgent_Male_04 | Insurgent Sniper Male IV | Sniper Rifle | 0 | 13000 | NONE | **alt-faction** | — |
| Chr_Insurgent_Male_05 | Insurgent Rifleman Male V | Rifle | 0 | 8500 | NONE | **alt-faction** | — |

### Noncombatants

| StableId | displayName | canReq | price | canAttack | Producer | Disposition | Notes |
|---|---|---:|---:|---:|---|---|---|
| Chr_Civilian_Female_01 | Civilian Female I | 0 | 0 | 1 | Refugee Tent | **scenario-Sandbox-only** | **NONCOMBATANT**; civilian YAML still has `canAttack: 1` / trivial damage — BASELINE warning confirmed |
| Chr_Civilian_Female_02 | Civilian Female II | 0 | 0 | 1 | Refugee Tent | **scenario-Sandbox-only** | same |
| Chr_Civilian_Male_01 | Civilian Male I | 0 | 0 | 1 | Refugee Tent | **scenario-Sandbox-only** | same |
| Chr_Civilian_Male_02 | Civilian Male II | 0 | 0 | 1 | Refugee Tent | **scenario-Sandbox-only** | same |

### Ground vehicles

| StableId | displayName | canReq | price | HP/DMG/RNG | Transport/Haul/Threat | Producer | Disposition | Notes |
|---|---|---:|---:|---|---|---|---|---|
| Veh_Light_Armored_Car | Light Armored Car | 1 | 28000 | 500/18/85 | — | Fuel Bladder | **blocked** | PLAN vehicle; **BAD-PRODUCER**; **POP-CAP-ZERO** (not soldier/tray/tanker) |
| Veh_APC_Fast | Fast APC | 1 | 34000 | 550/10/2 | soldierCap 10; canAttack 0 | Fuel Bladder | **blocked** | transport APC; **BAD-PRODUCER**; **POP-CAP-ZERO** |
| Veh_APC_Slow | Armored APC | 1 | 38000 | 700/10/2 | soldierCap 10; canAttack 0 | Fuel Bladder | **blocked** | **BAD-PRODUCER**; **POP-CAP-ZERO** |
| Veh_APC_Heavy | Heavy APC | 1 | 45000 | 900/22/95 | soldierCap 10; canAttack 1 | Fuel Bladder | **blocked** | armed APC; **BAD-PRODUCER**; **POP-CAP-ZERO** |
| Veh_Tank_USA | Battle Tank | 1 | 65000 | 1250/180/120 | — | Fuel Bladder | **blocked** | PLAN tank; **BAD-PRODUCER**; **POP-CAP-ZERO**; **MISSING-COUNTER** until rocketeer differentiated |
| Veh_Missle_Launcher_Air | Air Missile Launcher | 1 | 46000 | 520/320/420 | airMissileLauncherConfig set | Fuel Bladder | **blocked** | PLAN AA; **BAD-PRODUCER**; **POP-CAP-ZERO** |
| Veh_Missle_Launcher_Ground | Ground Missile Launcher | 1 | 42000 | 560/650/600 | groundMissileLauncherConfig set | Fuel Bladder | **blocked** | siege; later package; **BAD-PRODUCER**; **POP-CAP-ZERO** |
| Veh_Radar_Tank | Radar Tank | 1 | 32000 | 500/10/2 | canAttack 0; threatKind 1 / 240 cells | Fuel Bladder | **blocked** | description claims ground-vehicle detect; **UNIMPLEMENTED-ABILITY** vs shared fog (PLAN §8); **BAD-PRODUCER**; **POP-CAP-ZERO** |
| Veh_Truck_Canopy | Canopy Truck | 1 | 13000 | 220/10/2 | soldierCap 10; canAttack 0 | Fuel Bladder | **blocked** | transport truck; **POP-CAP-ZERO** unless name key maps to tray (prefab `Truck_Canopy` — **not** `truck_tray`) |
| Veh_Truck_Tray | Cargo Truck | 1 | 12000 | 220/10/2 | haul barrels 8 | Fuel Bladder | **standard roster** *(logistics)* | Only if prefab name contains `truck_tray` → pop cap 2; **verify name key** (`Unit_Veh_Truck_Tray` → key may be `truck_tray`); **BAD-PRODUCER** still |
| Veh_Truck_Tanker | Tanker Truck | 1 | 16000 | 260/10/2 | haul barrels 8 | Fuel Bladder | **standard roster** *(logistics)* | pop cap 1 if name contains `truck_tanker`; **BAD-PRODUCER** |

### Air

| StableId | displayName | canReq | price | isAir | Producer | Disposition | Notes |
|---|---|---:|---:|---:|---|---|---|
| Veh_Helicopter_Attack_Small | Light Attack Helicopter | 1 | 52000 | 1 | Helipad, **Water Tank** | **blocked** | Later air package; **POP-CAP-ZERO**; Water Tank BAD-PRODUCER |
| Veh_Helicopter_Attack | Attack Helicopter | 1 | 70000 | 1 | Helipad, Water Tank | **blocked** | same |
| Veh_Helicopter_Transport | Transport Helicopter | 1 | 62000 | 1 | Helipad, Water Tank | **blocked** | soldierCap 10; isProductionTransportUnit 1; **POP-CAP-ZERO** |
| Veh_Drone | Recon Drone | 1 | 8000 | 1 | Airport | **blocked** | recon waits for intel model (PLAN); **POP-CAP-ZERO**; **UNIMPLEMENTED-ABILITY** if sold as scout under full vision |
| Veh_Jet_01 | Strike Jet | 1 | 85000 | 1 | Airport | **blocked** | advanced air; **POP-CAP-ZERO** |
| Veh_Jet_02 | Fighter Jet | 1 | 90000 | 1 | Airport | **blocked** | same |
| Veh_Plane_Transport | Transport Plane | 1 | 95000 | 1 | Airport | **blocked** | soldierCap 24; requires runway flags; delivery vs tactical classification open (PLAN); **POP-CAP-ZERO** |

**Unit disposition counts:** standard roster **6** (5 infantry roles + logistics trucks tentatively) · role variant **12** · alt-faction **7** · scenario-Sandbox-only **10** · blocked **16** (+ logistics trucks may flip after name-key verify). *All 51 rows dispositioned; several carry dual flags (e.g. standard design intent + blocked by pop/producer — primary disposition reflects E0.2 playability blocker when present).*

**Primary disposition recount (playability-first):**  
- standard roster: 5 infantry core (Rifleman Male IV, Heavy Gunner, Marksman Male I, Assault Breacher, Ghillie) + Cargo/Tanker trucks if name keys match = **5–7**  
- role variant: remaining requestable soldier/contractor-looking alts not chosen as core ≈ **11**  
- alt-faction: **7** insurgents  
- scenario-Sandbox-only: **4** civilians + **3** contractors + **2** pilots + **1** commander = **10**  
- blocked: bomb suit + all non-truck vehicles/air + name-key failures ≈ **16–18**

---

## Building ledger (23)

| StableId | displayName | canReq | price | materialsCost | role/wall/threat | productions | Disposition | Notes |
|---|---|---:|---:|---:|---|---|---|---|
| Building_Barrack | Barracks | 1 | 40000 | 90 | role 5 | 1 unit ×4 | **standard roster** | Infantry producer |
| Helipad | Helipad | ABSENT | 35000 | 80 | — | 3 helis | **standard roster** | Air producer (E4+); canRequest field omitted |
| Airport | Airport | 1 | 120000 | 300 | role 0 | 4 air | **standard roster** | Advanced air producer (E6) |
| Fuel_Bladder | Fuel Bladder | ABSENT | 18000 | 40 | fuelStorage 5000 | **11 vehicles** | **blocked** | Economy storage OK; **BAD-PRODUCER** vehicle list must not ship as factory |
| WaterTank | Water Tank | ABSENT | 14000 | 30 | — | **3 helis (dup Helipad)** | **blocked** | Utility; **BAD-PRODUCER** |
| GuardTower | Guard Tower | 1 | 22000 | 50 | canAttack 1 | 0 | **standard roster** | Defense structure |
| GuardTower_Big | Heavy Guard Tower | 1 | 30000 | 70 | canAttack 1; oil/fuel stor 10000 | 0 | **standard roster** | — |
| Building_Satelite_Dish | Satellite Dish | ABSENT | 20000 | 45 | threatKind **2** / 240 | 0 | **blocked** | Description: air detect, cannot attack — **UNIMPLEMENTED-ABILITY** until shared intel (PLAN §8) |
| OilPump | Oil Pump | ABSENT | 50000 | 100 | oil 50/day, stor 200 | 0 | **standard roster** | Economy |
| OilRefinery | Oil Refinery | 1 | 80000 | 160 | fuel 100/day, stor 5000 | 0 | **standard roster** | — |
| OilRefinery_Big | Large Oil Refinery | 1 | 140000 | 260 | fuel 200/day, stor 10000 | 0 | **standard roster** | — |
| Ammunition_Depot | Field Fabrication Depot | 1 | 45000 | 100 | oil stor 24 | 0 | **standard roster** | Materials fabrication (description); verify cycle fields in full YAML during E0.5 |
| Tent_Regular | Soldier Tent | 1 | 12000 | 25 | — | 10 soldiers | **scenario-Sandbox-only** | Alternate infantry producer; not Skirmish Barracks path |
| Tent_Expert | Expert Tent | 1 | 10000 | 25 | — | 7 (soldiers/ghillie/pilots) | **scenario-Sandbox-only** | — |
| Tent_Contractor | Contractor Tent | 1 | 8000 | 20 | — | 3 contractors | **scenario-Sandbox-only** | — |
| Tent_Refugee | Refugee Tent | 1 | 6000 | 15 | role 4; refugeeCap 10 | 4 civilians | **scenario-Sandbox-only** | **NONCOMBATANT** housing |
| House | House | 0 | 9000 | 20 | role 1 | 0 | **scenario-Sandbox-only** | Civilian structure |
| Shop | Shop | 0 | 14000 | 30 | role 2 | 0 | **scenario-Sandbox-only** | — |
| Hall | City Hall | 0 | 50000 | 110 | role 3 | 0 | **scenario-Sandbox-only** | — |
| Portaloo_ | Portable Toilet | 1 | 1000 | 2 | — | 0 | **scenario-Sandbox-only** | Flavor/utility |
| Road_Barrier | Road Barrier | ABSENT | 6000 | 15 | — | 0 | **standard roster** | Fortification |
| Wall_Dirt_Straight | Dirt Wall | ABSENT | 10000 | 15 | isWall 1 | 0 | **standard roster** | — |
| Wall_Fence_Straight | Fence Wall | 1 | 7000 | 10 | isWall 1 | 0 | **standard roster** | — |

**Building disposition counts (playability-first):** standard roster **12** · scenario-Sandbox-only **9** · blocked **2** (Fuel Bladder producer abuse + Satellite intel; Water Tank counted blocked) → adjust Water Tank blocked → **standard roster 12 / Sandbox 9 / blocked 3** if Water Tank utility retained but productions stripped.

---

## Cross-cutting flags

### Duplicated names
- `Rifleman Female I` → `Chr_Soldier_Female_01_Alt_01` **and** `Chr_Soldier_Female_02_Alt_01`
- `Rifleman Male II` → `Chr_Soldier_Male_02` **and** `Chr_Soldier_Male_02_Alt_02`  
Portrait GUID scan on `portraitSprite` / `portraitCardSprite` / `portraitActionSprite`: **no shared GUIDs across units** (duplicates are name-only).

### Noncombatants
Civilians (+ Refugee Tent). Civilian assets advertise `canAttack: 1` with ~1 damage — eligibility must stay explicit (BASELINE).

### Missing producers
Bomb Suit, Field Commander, all Insurgents (by design for player list). Ground combat vehicles lack an honest factory (only Fuel Bladder list).

### Missing counters
Until Skirmish stops flattening all `soldier*` to one rifle profile, Heavy Gunner / Marksman / Breacher / Rocketeer **do not counter differently**. Armor vs rocketeer also unverified under Skirmish combat policy.

### Unimplemented advertised abilities
- Bomb Suit EOD (description only)
- Radar Tank ground-vehicle detection (threat fields set; shared fog/intel absent per PLAN)
- Satellite Dish air detection (threatKind 2 / 240; same intel blocker)
- Recon Drone “awareness” under full-map vision preset

### Bad producer data
- Fuel Bladder → 11 vehicles  
- Water Tank → copy of Helipad heli list  

---

## Top blockers (with evidence paths)

1. **Skirmish population policy zeroes almost all vehicles/air** — `Assets/Game/Scripts/Systems/SkirmishPopulationPolicy.cs` (`PopulationKey` / `CanQueue`: only soldier / truck_tray / truck_tanker).  
2. **No honest ground vehicle producer** — vehicle spawns only on `Prefab_BuildingDefinition_Fuel_Bladder_Config.asset` (and helis wrongly on Water Tank). PLAN E1 requires explicit ground staging/producer.  
3. **Skirmish combat flattens infantry roles** — `SkirmishCombatPolicy.ApplyRoster` string-match `soldier` → one rifle profile; blocks E0.5 “differentiated infantry” acceptance.  
4. **Barracks produces a single appearance** — `Building_Barrack_Config` → only `Soldier_Male_02_Alt_04` ×4; other roles live on tents / Expert Tent, not Skirmish Barracks path.  
5. **Intel/recon buildings/units blocked on vision model** — Radar Tank + Satellite Dish + Drone claims vs PLAN §8 full-vision ground slice.  
6. **Cost/fuel spreadsheet inputs incomplete** — UnitGrid YAML lacks `materialsCost` / fuel-per-cell; runtime fuel systems exist but rates UNKNOWN; `ResolveLegacyMaterialsCost` definition not located on branch.

---

## E0.5 draft — first roles + cost/production/fuel

### Proposed first 5 infantry roles (from ledger)

| # | Role | Representative StableId | Justification (evidence) |
|---|---|---|---|
| 1 | Rifleman | `Chr_Soldier_Male_02_Alt_04` | Barracks production entry; general infantry; price 10000 / Rifle |
| 2 | Heavy Gunner | `Chr_Soldier_Male_01` | PLAN ground core; Machine Gun; HP 145 / dmg 10 / range 100 |
| 3 | Marksman | `Chr_Soldier_Male_01_Alt_01` | PLAN; Sniper Rifle; dmg 52 / range 130 |
| 4 | Assault Breacher | `Chr_Soldier_Female_02_Alt_02` | PLAN; SMG; HP 140 / range 60 |
| 5 | Ghillie Rocketeer | `Chr_Ghillie_Male_01` | PLAN anti-armor; Rocket Launcher; dmg 260 / range 115 |

*Appearance variants (other gender/alts) stay under role-variant picker — not separate tray buttons (PLAN §3).*

### Proposed first 3 vehicle roles

| # | Role | Representative StableId | Justification |
|---|---|---|---|
| 1 | Light Armored Car | `Veh_Light_Armored_Car` | PLAN flank/suppress; already in prototype lore; price 28000 |
| 2 | Armored APC | `Veh_APC_Slow` *or* `Veh_APC_Heavy` | PLAN transport / protected assault; soldierCap 10; Heavy also shoots |
| 3 | Battle Tank | `Veh_Tank_USA` | PLAN heavy armor; HP 1250 / dmg 180 |

**All three vehicles remain disposition `blocked` until producer + pop policy change — do not claim available.**

### Cost / production / fuel table (numbers only where evidenced)

| Role | Credits (`price` YAML) | Materials | Supply (PLAN proposal) | Build time (`productionDurationSeconds`) | Fuel/cell | Producer (intended) | Evidence gap |
|---|---:|---:|---:|---:|---:|---|---|
| Rifleman | 10000 | UNKNOWN | 1 (PLAN text only) | 5 | n/a | Barracks | materialsCost absent; legacy resolver unevidenced |
| Heavy Gunner | 14000 | UNKNOWN | 1 | 5 | n/a | Barracks *(not currently)* | same; not on Barracks productions |
| Marksman | 11500 | UNKNOWN | 1 | 5 | n/a | Barracks *(not currently)* | same |
| Assault Breacher | 13000 | UNKNOWN | 1 | 5 | n/a | Barracks *(not currently)* | same |
| Ghillie Rocketeer | 16000 | UNKNOWN | 1 | 5 | n/a | Barracks *(not currently)*; on Expert Tent | same |
| Light Armored Car | 28000 | UNKNOWN | 4 (PLAN) | 5 | UNKNOWN | **TBD ground producer** | Fuel Bladder only today; fuel rates unserialized |
| Armored/Heavy APC | 38000 / 45000 | UNKNOWN | 4 (PLAN) | 5 | UNKNOWN | TBD | same |
| Battle Tank | 65000 | UNKNOWN | 6 (PLAN) | 5 | UNKNOWN | TBD | same |

Building construction costs **are** evidenced (`price` + `materialsCost` on BuildingDefinition YAML) — use those for facility columns in the E0.5 sheet; unit materials remain UNKNOWN.

### Candidate runtime code paths (E0.5 mapping)

| Concern | Path |
|---|---|
| Queue eligibility / pop caps | `Assets/Game/Scripts/Systems/SkirmishPopulationPolicy.cs` |
| Catalog allowlist | `Assets/Game/Scripts/Systems/SkirmishCatalogPolicy.cs` |
| Skirmish rifle flatten | `Assets/Game/Scripts/Systems/SkirmishCombatPolicy.cs` |
| Preset limits / rifle scalars | `Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs` (`InfantryLimitPerFaction = 24`) |
| Camp request + spend/refund | `Assets/Game/Scripts/Systems/BuildingProductionCampRequestTransaction.cs` |
| Unit credit/material resolve | `Assets/Game/Scripts/Systems/BuildingDefinitionUnitProductionResources.cs` |
| Production request pipeline | `Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper*.cs` |
| Construction materials tx | `Assets/Game/Scripts/Systems/BuildingConstructionResourceTransactionSystemHelper.cs` |
| Unit authoring cost/fuel fields | `Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs` |
| Fuel drain | `Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs` |
| Ground fuel hold | `Assets/Game/Scripts/Systems/GroundVehicleFuelHoldSystem.cs` |
| Air fuel return | `Assets/Game/Scripts/Systems/AircraftFuelSafetyReturnSystem.cs` |
| Producer definitions | `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_{Building_Barrack,Helipad,Airport,Fuel_Bladder,WaterTank,_Tent_*}_Config.asset` |

---

## What remains incomplete

- Exact **materials** numbers per unit (YAML gap + missing `ResolveLegacyMaterialsCost` body).  
- Exact **fuel per cell** defaults (unserialized; need baker/config SO defaults or play-mode probe).  
- Confirm **Cargo/Tanker** prefab name keys match `truck_tray` / `truck_tanker` substrings used by population policy.  
- Preset **allowlist** contents (`SkirmishPresetConfig` / buildingPlacement registries) not fully enumerated here — catalog may hide assets even if producers exist.  
- Localization keys / portrait LOD / wreck/death presentation / delivery transport prefabs — not fully filled per BASELINE column list.  
- Fabrication cycle fields on Field Fabrication Depot — partial.  
- Dual-count disposition when an entry is both “design standard” and “runtime blocked” — parent should pick policy for E1 gating.  
- No gameplay/device run; ledger is source/config inspection only (same caveat as BASELINE).

---

*E0.2 deliverable for WarlineCapture Skirmish Expansion — usable for E0.5 role picks and producer/cost path finding; not an availability claim.*

---

## Disposition tally (playability-first)

### Units (51) — sums to 51

| Disposition | Count | Members (short) |
|---|---:|---|
| standard roster | 5 | Rifleman Male IV; Heavy Gunner Male I; Marksman Male I; Assault Breacher Female II; Ghillie Rocketeer |
| role variant | 10 | Remaining requestable `Soldier_*` alts in rifle/marksman/sidearm/advanced families |
| alt-faction | 7 | All 7 `Insurgent_*` (canRequest=0, no producer) |
| scenario-Sandbox-only | 10 | 4 Civilians; 3 Security Contractors; 2 Pilots; Field Commander |
| blocked | 19 | Bomb Suit Specialist (no producer + unimplemented EOD); all 18 vehicles/air (Fuel Bladder / wrong producer and/or `SkirmishPopulationPolicy` limit 0; Radar Tank also intel-gated) |

**Arithmetic:** 5+10+7+10+19 = **51**.

**Optional flip:** if prefab names contain `truck_tray` / `truck_tanker`, Cargo Truck + Tanker Truck may move to standard roster (then standard 7 / blocked 17). Canopy Truck name does not match those keys → stays blocked for Skirmish queue. Confirm against `Unit_Veh_Truck_*.prefab` names.

### Buildings (23) — sums to 23

| Disposition | Count | Members (short) |
|---|---:|---|
| standard roster | 12 | Barracks; Helipad; Airport; Guard Tower; Heavy Guard Tower; Oil Pump; Oil Refinery; Large Oil Refinery; Field Fabrication Depot; Road Barrier; Dirt Wall; Fence Wall |
| scenario-Sandbox-only | 8 | House; Shop; City Hall; Soldier/Expert/Contractor/Refugee Tents; Portable Toilet |
| blocked | 3 | Fuel Bladder (vehicle productions on storage building); Water Tank (Helipad heli list copy-paste); Satellite Dish (air-detect claim gated on intel model) |

**Arithmetic:** 12+8+3 = **23**.
