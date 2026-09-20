# E0.5 Producer / Cost / Fuel Map

**Status:** E0.5 deliverable (source/config inspection; not a match-readiness or availability claim)  
**Package:** Skirmish Expansion E0.5  
**Repo / branch:** `farhad2na2/WarlineCapture` @ `codex/m03-radar-warning`  
**Method:** GitHub API + raw only (no clone)  
**Depends on:** `Design/Roadmap/Skirmish_Expansion/E0_2_ROSTER_LEDGER.md`  
**Authority:** DELIVERY.md E0.5 — first 5 infantry + 3 vehicle roles; cost/production/fuel table; producer mapping before E1.

---

## 1. Locked formulas (evidence)

### 1.1 Materials from credits (`ResolveLegacyMaterialsCost`)

**Path:** `Assets/Game/Scripts/Configs/GameplayConfigModels.cs` — class `UnitGridAuthoringConfig`

```csharp
public int MaterialsCost => materialsCost > 0
    ? materialsCost
    : ResolveLegacyMaterialsCost(Price);

public static int ResolveLegacyMaterialsCost(int legacyPrice)
{
    const int creditsPerMaterial = 500;
    return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0, legacyPrice) / (float)creditsPerMaterial));
}
```

| Rule | Value |
|---|---|
| If serialized `materialsCost > 0` | use that |
| Else (0 / absent) | `max(1, ceil(price / 500))` |
| Authored `materialsCost: 0` on Rifleman Male IV | still triggers legacy (because `> 0` check) → **20** |

Same fallback on MonoBehaviour path: `UnitGridAuthoring.MaterialsCost` → `UnitGridAuthoringConfig.ResolveLegacyMaterialsCost(price)` when no config SO / local materialsCost ≤ 0  
(`Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs`).

### 1.2 Fuel-per-cell defaults

**Same file** `UnitGridAuthoringConfig`:

| Accessor | Default when serialized value is 0 / unset |
|---|---|
| `GroundFuelPerCell` | **0.05** if vehicle (vehicle motion / `Unit_Veh_` name / transport or hauler capacity); **0** if air; **0** if infantry |
| `AirFuelPerCell` | **0.25** if `isAirUnit`; else **0** |

Helpers: `ResolveDefaultGroundFuelPerCell`, `ResolveDefaultAirFuelPerCell`.

Baker only adds `UnitFuelConsumption` when `(UsesVehicleMotion \|\| IsAirUnit) && (ground > 0 \|\| air > 0)`  
(`UnitGridAuthoring.cs` baker). Infantry with 0/0 → **no fuel component**.

Runtime drain: `VehicleFuelConsumptionSystem.cs` (per-cell × faction storage). Hold/return: `GroundVehicleFuelHoldSystem.cs`, `AircraftFuelSafetyReturnSystem.cs`.

### 1.3 Enqueue charge mapping (credits vs materials)

| Step | Path | Behavior |
|---|---|---|
| Metadata from prefab | `BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata` | `CreditsCost = authoring.Price`; `Price = authoring.MaterialsCost` *(field name `Price` on metadata holds materials)* |
| Resolve at request | `BuildingDefinitionUnitProductionResources.TryResolveConfiguredUnitResourceCosts` | `creditsCost = metadata.CreditsCost`; `materialsCost = metadata.Price` |
| Spend / refund | `BuildingProductionCampRequestTransaction.TrySpendUnitProductionResources` / `Restore…` | `TrySpendConstructionResources(creditsCost, materialsCost)` or materials-only fallback |
| UI catalog chip | `UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata` | exposes both `MaterialsCost` and `Price` |

**Column mapping for the E0.5 table**

| Table column | Runtime source | YAML / formula |
|---|---|---|
| Credits | `UnitGridAuthoring.Price` | `price:` on UnitGrid config |
| Materials | `UnitGridAuthoring.MaterialsCost` | `materialsCost:` if > 0; else `ceil(price/500)` min 1 |
| Build time (s) | `productionDurationSeconds` | UnitGrid YAML (infantry/vehicles sampled = **5**) |
| Supply | *(none in code)* | PLAN proposal only → **UNKNOWN / not charged** |
| Ground fuel / cell | `GroundFuelPerCell` | default **0.05** vehicles; **0** infantry |
| Air fuel / cell | `AirFuelPerCell` | default **0.25** air; **0** otherwise |
| Producer | BuildingDefinition `productions` + catalog allow | see §3–4 |

Buildings (placement): charge **materials only** from `BuildingDefinition.MaterialsCost` (credits arg 0) in camp transaction building branch.

---

## 2. Truck name vs population policy

| Prefab file | `prefab.name` (inferred) | `PopulationKey` substring | Match? | Cap |
|---|---|---|---|---|
| `Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tray.prefab` | contains `Truck_Tray` | `truck_tray` | **YES** (case-insensitive `Contains`) | **2** |
| `Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tanker.prefab` | contains `Truck_Tanker` | `truck_tanker` | **YES** | **1** |
| `Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Canopy.prefab` | `Truck_Canopy` | neither | **NO** | **0** (blocked) |

**Policy path:** `Assets/Game/Scripts/Systems/SkirmishPopulationPolicy.cs` — `PopulationKey` / `CanQueue`.  
Soldiers: key contains `soldier` → `SkirmishPresetConfig.InfantryLimitPerFaction` (**24**).

---

## 3. Skirmish catalog allowlists (both presets)

Presets: `Assets/Game/Resources/SkirmishBaseAssault.asset`, `SkirmishCityCrossroads.asset`  
→ `buildingPlacement` = `Assets/Game/Configs/Skirmish/Construction.asset` (shared spawnables; City Construction asset mirrors same 6 GUIDs)  
→ `unitPrefabRegistryConfig` = `Assets/Game/Configs/Skirmish/UnitRegistry.asset` (**shared**)

**Catalog gate:** `SkirmishCatalogPolicy.Allows` uses  
`preset.buildingPlacement.Spawnables` (buildings) and  
`preset.buildingPlacement.UnitPrefabRegistryConfig.UnitSpawnPrefabs` (units).  
Impostor atlas entries are **not** the allowlist.

### Units in `UnitSpawnPrefabs` (exactly 3)

| GUID | Prefab | displayName | Notes |
|---|---|---|---|
| `ff73c8b7cbf844778916da330e9cd3e3` | `Unit_Chr_Soldier_Male_02_Alt_04` | Rifleman Male IV | Barracks product |
| `57f2dccc00faf49bca391aa1cc44d70a` | `Unit_Veh_Truck_Tray` | Cargo Truck | pop cap 2 |
| `e9015a8d3f0294ca6ac7bf8614ac2e7e` | `Unit_Veh_Truck_Tanker` | Tanker Truck | pop cap 1 |

### Buildings in `spawnables` (exactly 6)

| Prefab | Role in Skirmish |
|---|---|
| `Building_Barrack` | Infantry producer |
| `Building_GuardTower` | Defense |
| `Building_Ammunition_Depot` | Field Fabrication Depot |
| `Building_OilPump` | Oil income |
| `Building_Refinery` | Fuel conversion |
| `Building_Fuel_Bladder` | Fuel storage **and** (mis-authored) vehicle productions |

**Not catalogued for Skirmish build/train UI:** Helipad, Airport, Heavy Guard Tower, Large Refinery, walls/barriers, all tents, civilian buildings, Satellite Dish, Water Tank, all APCs/tanks/air/missile/radar units, Canopy Truck, specialist infantry beyond Rifleman Male IV.

`InitialForces.asset` may **spawn** additional prefabs at match start (including a non-catalog vehicle GUID); that is placement, not catalog eligibility.

---

## 4. Producer mapping (E0.5 roles)

| Role | UnitGrid / prefab | Intended producer (PLAN) | Actual producer asset | In Skirmish catalog? | Pop policy |
|---|---|---|---|---|---|
| Rifleman | `Chr_Soldier_Male_02_Alt_04` / `Unit_Chr_Soldier_Male_02_Alt_04` | Barracks | Barracks `productions` ×4 | **YES** | soldier ≤24 |
| Heavy Gunner | `Chr_Soldier_Male_01` | Barracks | Expert Tent only today | **NO** | would be soldier if catalogued |
| Marksman | `Chr_Soldier_Male_01_Alt_01` | Barracks | Soldier Tent | **NO** | same |
| Assault Breacher | `Chr_Soldier_Female_02_Alt_02` | Barracks | Soldier Tent | **NO** | same |
| Ghillie Rocketeer | `Chr_Ghillie_Male_01` | Barracks | Expert Tent | **NO** | name has no `soldier` → **limit 0** unless renamed/keyed |
| Light Armored Car | `Veh_Light_Armored_Car` | TBD ground factory | Fuel Bladder only | **NO** | limit 0 |
| Armored / Heavy APC | `Veh_APC_Slow` / `Veh_APC_Heavy` | TBD | Fuel Bladder | **NO** | limit 0 |
| Battle Tank | `Veh_Tank_USA` | TBD | Fuel Bladder | **NO** | limit 0 |
| Cargo Truck (logistics) | `Veh_Truck_Tray` | logistics | Fuel Bladder | **YES** | tray ≤2 |
| Tanker Truck (logistics) | `Veh_Truck_Tanker` | logistics | Fuel Bladder | **YES** | tanker ≤1 |

---

## 5. Cost / production / fuel table (E0.5 picks)

Numbers below use **locked** credits YAML + **derived** materials/fuel. Supply left UNKNOWN (no runtime field).

| Role | Credits (`price`) | Materials (derived) | Supply | Build time (s) | Ground fuel/cell | Air fuel/cell | Producer (actual) | Skirmish playable now? |
|---|---:|---:|---:|---:|---:|---:|---|---|
| Rifleman Male IV | **10000** | **20** | UNKNOWN | **5** | **0** | **0** | Barracks | **YES** (catalog + pop + producer) |
| Heavy Gunner Male I | **14000** | **28** | UNKNOWN | **5** | **0** | **0** | Expert Tent only | NO — not in UnitRegistry; Skirmish flatten |
| Marksman Male I | **11500** | **23** | UNKNOWN | **5** | **0** | **0** | Soldier Tent | NO — not in UnitRegistry; flatten |
| Assault Breacher Female II | **13000** | **26** | UNKNOWN | **5** | **0** | **0** | Soldier Tent | NO — not in UnitRegistry; flatten |
| Ghillie Rocketeer | **16000** | **32** | UNKNOWN | **5** | **0** | **0** | Expert Tent | NO — not in UnitRegistry; pop key risk |
| Light Armored Car | **28000** | **56** | UNKNOWN (PLAN 4) | **5** | **0.05** | **0** | Fuel Bladder (bad) | NO |
| Armored APC | **38000** | **76** | UNKNOWN (PLAN 4) | **5** | **0.05** | **0** | Fuel Bladder (bad) | NO |
| Heavy APC | **45000** | **90** | UNKNOWN (PLAN 4) | **5** | **0.05** | **0** | Fuel Bladder (bad) | NO |
| Battle Tank | **65000** | **130** | UNKNOWN (PLAN 6) | **5** | **0.05** | **0** | Fuel Bladder (bad) | NO |
| Cargo Truck | **12000** | **24** | UNKNOWN | **5** | **0.05** | **0** | Fuel Bladder | **Partial** — catalog+pop OK; producer semantics bad |
| Tanker Truck | **16000** | **32** | UNKNOWN | **5** | **0.05** | **0** | Fuel Bladder | **Partial** — same |

**Derivation check:** `materials = max(1, ceil(credits/500))`. Evidence: Rifleman `materialsCost: 0` + `price: 10000` → 20.

### Example building costs (catalogued facilities)

| Building | Credits (`price`) | Materials (`materialsCost` YAML) |
|---|---:|---:|
| Barracks | 40000 | **90** (authored, not derived) |
| Fuel Bladder | 18000 | 40 |
| Guard Tower | 22000 | 50 |
| Oil Pump | 50000 | 100 |
| Refinery | 80000 | 160 |
| Field Fabrication Depot | 45000 | 100 |

---

## 6. Code paths (E1 wiring checklist)

| Concern | Path |
|---|---|
| Legacy materials + fuel defaults | `Assets/Game/Scripts/Configs/GameplayConfigModels.cs` (`UnitGridAuthoringConfig`) |
| Authoring mirrors | `Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs` |
| Metadata Credits/Materials swap fields | `…/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs` |
| Resolve costs | `…/Systems/BuildingDefinitionUnitProductionResources.cs` |
| Enqueue spend/refund | `…/Systems/BuildingProductionCampRequestTransaction.cs` |
| Catalog allow | `…/Systems/SkirmishCatalogPolicy.cs` + `Configs/Skirmish/{Construction,UnitRegistry}.asset` |
| Population caps | `…/Systems/SkirmishPopulationPolicy.cs` + `SkirmishPresetConfig.InfantryLimitPerFaction` |
| Combat flatten | `…/Systems/SkirmishCombatPolicy.cs` `ApplyRoster` |
| Fuel drain / hold | `VehicleFuelConsumptionSystem.cs`, `GroundVehicleFuelHoldSystem.cs`, `AircraftFuelSafetyReturnSystem.cs` |

---

## 7. Remains UNKNOWN / open

- **Supply** capacity cost — PLAN numbers only; no UnitGrid/Skirmish field found.  
- Whether any UnitGrid overrides `groundFuelPerCell`/`airFuelPerCell` to non-default on vehicle assets (sampled Light Armored Car / trucks: field absent → defaults). Full 18-vehicle audit not re-run this pass.  
- Whether `Ghillie` prefab name contains `soldier` for pop key (likely **no** — blocker for queue even after catalog add).  
- Construction resource wallet field names / starting credits-materials for Skirmish economy balance.  
- Whether Fuel Bladder vehicle menu is reachable when unit is catalogued (trucks) despite being a storage building — needs play-mode confirmation.  
- City vs Base: Construction spawnables match; confirm City Construction meta GUID vs Resource reference if scenarios diverge later.

---

## 8. E0.5 readiness for E1 planning

| Question | Answer |
|---|---|
| Are materials + fuel **formulas** locked with evidence? | **YES** |
| Is enqueue **credits vs materials** charge path mapped? | **YES** |
| Is Skirmish **allowlist** enumerated? | **YES** (3 units / 6 buildings) |
| Can the proposed **5 infantry + 3 combat vehicles** be treated as currently playable Skirmish roles? | **NO** — only Rifleman (+ logistics trucks) are catalogued; combat vehicles blocked by catalog + pop + bad producer; specialists need Barracks entries + combat un-flatten |
| **Ready for E1 planning?** | **YES, as an evidence pack** for what E1 must change (catalog, producers, pop keys, combat roles, optional Supply). **NO** as a claim that the eight roles are implementation-complete or match-ready. |

**Recommended E1 entry work (from this map):**  
1) Expand `UnitRegistry.unitSpawnPrefabs` + Barracks `productions` for the five infantry.  
2) Add honest ground vehicle producer; remove vehicle list from Fuel Bladder.  
3) Extend `SkirmishPopulationPolicy` beyond soldier/tray/tanker.  
4) Stop `ApplyRoster` rifle flatten for named roles.  
5) Author explicit `materialsCost` on UnitGrids if designers dislike price/500.  
6) Decide Supply accounting (new field) before advertising Supply costs.
