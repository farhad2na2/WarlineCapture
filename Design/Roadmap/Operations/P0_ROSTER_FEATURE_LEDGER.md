# Operations P0 roster and feature availability ledger

Machine-readable copy: `Game.Operations.Contracts.OperationsRosterLedger`. Every row was checked against source on `1e0eb9da2`. Semantic Operations roles are **not** existing prefab IDs.

## Roster roles

| Semantic role | Availability | Verified evidence | Authoring rule |
|---|---|---|---|
| Rifle infantry | Campaign-bound | `unit.jrc.rifle_*` in M01/M02 builders; `Unit_Chr_Soldier_*` prefabs | Reuse certified keys after a roster bind |
| Recon infantry | Semantic only | No `unit.jrc.recon_*`; drone/ghillie are different classes | Do not infer recon from mesh names |
| Support infantry | Semantic only | Campaign command squads are rifle units | New bind required |
| Repair specialist | Missing | No specialist prefab; no `TacticalCommandMode.Repair` | P2 blocker |
| Anti-armor infantry | Present in source | `Unit_Chr_Ghillie_Male_01` / M03 "Ghillie Rocketeer" | Bind only after role certification |
| Anti-air infantry | Missing | `Unit_Veh_Missle_Launcher_Air` is a vehicle | Do not substitute the launcher |
| APC | Campaign-bound | M04 `Unit_Veh_APC_Fast` / `role.friendly.rescue_apc` | Reuse after Operations role bind |
| Tank | Present in source | `Unit_Veh_Tank_USA`, Skirmish stress recipe | Present, not Operations-bound |
| Transport helicopter | Campaign-bound | M04 `Unit_Veh_Helicopter_Transport` / `role.friendly.airlift` | Extract generic facts; do not launch as M04 |
| Light vehicle | Present in source | `Unit_Veh_Light_Armored_Car` | Present, not Operations-bound |
| Cargo truck | Present in source | `Unit_Veh_Truck_Tray`, `Unit_Veh_Truck_Canopy` | Hostile cargo identities are not authored |
| Civilian | Campaign-bound | `Unit_Chr_Civilian_*`, `role.civilian.protected`, `FactionIdentity.NeutralFactionId=0` | Escort/follow mechanic is still missing |

Resolved enough for later authoring discussion: rifle, APC, transport helicopter, civilian, plus present-in-source tank/light vehicle/cargo/ghillie. Not resolved: recon, support, repair specialist, anti-air infantry.

## Features

| Feature ID | Availability | Verified evidence |
|---|---|---|
| `feature.operation_map` | Campaign-bound | `MissionDefinitionConfig.RequiredFeatureIds` |
| `feature.unit_catalog` | Campaign-bound | `RequireUnitCatalogReady` |
| `feature.tactical_commands` | Campaign-bound | `TacticalCommandMode` |
| `feature.tactical.scan` | Present | `TacticalCommandMode.Scan`, `ScanIntelCommandSystem` |
| `feature.tactical.hold` | Present | `TacticalCommandMode.Hold` |
| `feature.tactical.board` | Campaign-bound | Board command + M04 extraction |
| `feature.tactical.repair` | Missing | No command mode |
| `feature.tactical.interact` | Missing | No command mode |
| `feature.operations.civilian_escort` | Missing | Named new mechanic in MISSION_IMPLEMENTATION |
| `feature.operations.evidence_carry` | Missing | Named new mechanic in MISSION_IMPLEMENTATION |
| `feature.operations.partial_outcome` | Missing | Campaign `MissionOutcomeKind` is Victory/Defeat |
| `feature.operations.run_state` | Missing | No profile Operations envelope |
| `feature.operations.id_namespace` | Missing | Shared identity rules reject operations IDs |

P4 O001–O003 must not be authored until the required missing features for those families (recon scan binding, escort cargo identities, repair interaction) are implemented or explicitly waived by Game PM.
