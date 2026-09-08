# M03 Radar Warning — Complete Unit Class Coverage

Date: 2026-09-08

Status: Planning inventory; no class is certified by this table

Source: `Design/BalanceConfigs/Combat_Balance_Config_v0_1.json` (inspected checkout)

This appendix accounts for every one of the 57 unit identities in the inspected design catalog. The proposed M03 roster uses only the explicitly marked active rows plus selected protected civilian variants. All other classes remain reference content or later Campaign content. This preserves a manageable third mission while covering the requested class scope.

The source catalog calls 51 units implemented and six naval units `designReadyNeedsUnityPrefab`. Those are source classifications, not fresh runtime validation. Exact Unity config capabilities, faction assignment, unlock state, command support and content readiness must be checked at M03RW-005. Older role labels may disagree with current gameplay; in particular, do not label the ghillie unit a sniper merely from its appearance, or claim an APC shoots when `canAttack` is false.

See the [production plan](M03_Radar_Warning_Production_Plan.md) for class teaching policy and the [technical architecture](Architecture/m03_radar_warning_technical_architecture.md) for the C# class responsibility map.

| Catalog identity | Catalog role tags | Proposed M03 treatment | Catalog status |
|---|---|---|---|
| `Unit_Chr_Bombsuit_Male_01` | breach, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Civilian_Female_01` | civilian, groundCharacter, noncombatant, protectable | Protected civilian variant candidate; exact four-person set frozen at map gate | Catalog says implemented; verify runtime |
| `Unit_Chr_Civilian_Female_02` | civilian, groundCharacter, noncombatant, protectable | Protected civilian variant candidate; exact four-person set frozen at map gate | Catalog says implemented; verify runtime |
| `Unit_Chr_Civilian_Male_01` | civilian, groundCharacter, noncombatant, protectable | Protected civilian variant candidate; exact four-person set frozen at map gate | Catalog says implemented; verify runtime |
| `Unit_Chr_Civilian_Male_02` | civilian, groundCharacter, noncombatant, protectable | Protected civilian variant candidate; exact four-person set frozen at map gate | Catalog says implemented; verify runtime |
| `Unit_Chr_Contractor_Female_01` | groundCharacter, patrol, security | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Contractor_Male_01` | groundCharacter, patrol, security | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Contractor_Male_02` | groundCharacter, patrol, security | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Ghillie_Male_01` | antiArmor, groundCharacter, playerRoster, siege | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Insurgent_Female_01` | antiInfantry, enemy, groundCharacter, irregular | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Insurgent_Female_02` | antiInfantry, enemy, groundCharacter, irregular | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Insurgent_Male_01` | antiArmor, enemy, groundCharacter, irregular, siege | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Insurgent_Male_02` | antiInfantry, enemy, groundCharacter, irregular | Active: proposed hostile convoy escorts | Catalog says implemented; verify runtime |
| `Unit_Chr_Insurgent_Male_03` | enemy, groundCharacter, irregular | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Insurgent_Male_04` | antiInfantry, enemy, groundCharacter, irregular, longRange | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Insurgent_Male_05` | antiInfantry, enemy, groundCharacter, irregular | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Leader_Male_01` | groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Pilot_Female_01` | aircrew, groundCharacter, support | M04/later reference; no M03 transport/flight action | Catalog says implemented; verify runtime |
| `Unit_Chr_Pilot_Male_01` | aircrew, groundCharacter, support | M04/later reference; no M03 transport/flight action | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Female_01` | antiInfantry, groundCharacter, longRange, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Female_01_Alt_01` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Female_01_Alt_02` | antiInfantry, groundCharacter, longRange, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Female_02` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Female_02_Alt_01` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Female_02_Alt_02` | breach, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Male_01` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Male_01_Alt_01` | antiInfantry, groundCharacter, longRange, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Male_01_Alt_02` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Male_02` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Male_02_Alt_01` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Male_02_Alt_02` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Male_02_Alt_03` | antiInfantry, groundCharacter, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Chr_Soldier_Male_02_Alt_04` | antiInfantry, groundCharacter, playerRoster | Active: Alpha/Bravo and optional reinforcement | Catalog says implemented; verify runtime |
| `Unit_Sea_Coastal_Cutter` | sea, command, antiAir, antiVehicle | Future/reference; unavailable for deployment; missing prefab gate | Design only; Unity prefab needed |
| `Unit_Sea_Drone_Boat` | sea, recon, detector | Future/reference; unavailable for deployment; missing prefab gate | Design only; Unity prefab needed |
| `Unit_Sea_Interceptor_Boat` | sea, fastResponse, antiInfantry | Future/reference; unavailable for deployment; missing prefab gate | Design only; Unity prefab needed |
| `Unit_Sea_Landing_Craft` | sea, transport, amphibious | Future/reference; unavailable for deployment; missing prefab gate | Design only; Unity prefab needed |
| `Unit_Sea_Missile_Craft` | sea, siege, antiAir, antiArmor | Future/reference; unavailable for deployment; missing prefab gate | Design only; Unity prefab needed |
| `Unit_Sea_Patrol_Boat` | sea, patrol, antiInfantry, recon | Future/reference; unavailable for deployment; missing prefab gate | Design only; Unity prefab needed |
| `Unit_Veh_APC_Fast` | groundVehicle, playerRoster, transport | Active: unarmed breach-threatening carrier | Catalog says implemented; verify runtime |
| `Unit_Veh_APC_Heavy` | groundVehicle, playerRoster, transport | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_APC_Slow` | groundVehicle, playerRoster, transport | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Drone` | air, playerRoster, recon | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Helicopter_Attack` | air, airSupport, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Helicopter_Attack_Small` | air, airSupport, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Helicopter_Transport` | air, airSupport, playerRoster, transport | M04/later reference; no M03 transport/flight action | Catalog says implemented; verify runtime |
| `Unit_Veh_Jet_01` | air, airSupport, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Jet_02` | air, airSupport, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Light_Armored_Car` | groundVehicle, playerRoster | Active: armed vanguard/main-body vehicle | Catalog says implemented; verify runtime |
| `Unit_Veh_Missle_Launcher_Air` | antiArmor, groundVehicle, playerRoster, siege | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Missle_Launcher_Ground` | antiArmor, groundVehicle, playerRoster, siege | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Plane_Transport` | air, playerRoster, transport | M04/later reference; no M03 transport/flight action | Catalog says implemented; verify runtime |
| `Unit_Veh_Radar_Tank` | groundVehicle, playerRoster, recon | Active: mission-loaned Ground sensor; no production unlock | Catalog says implemented; verify runtime |
| `Unit_Veh_Tank_USA` | armor, groundVehicle, playerRoster | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Truck_Canopy` | groundVehicle, playerRoster, transport | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Truck_Tanker` | armor, groundVehicle, playerRoster, transport | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |
| `Unit_Veh_Truck_Tray` | groundVehicle, playerRoster, transport | Reference/later Campaign; no M03 deployment or mandatory tutorial | Catalog says implemented; verify runtime |

## Building and support roles associated with M03

| Identity / role | M03 plan | Capability/unlock rule |
|---|---|---|
| `Building_Barrack` | Existing producer; optional rifle order | Use actual quantity-aware transaction and accepted canonical four-member output. |
| `Building_GuardTower` | Optional placement and permanent first-clear reward | Current config has real weapon behavior; temporary mission build access before first clear. |
| `Building_Road_Barrier` | Optional placement | Temporary M03 access; preserve Chapter 2 permanent gate; validate real obstruction/reroute/breach behavior. |
| `Building_Satelite_Dish` | Outside ground-warning build lesson | Current config is Air detector; preserve its identity/capability. |
| `Tent_Regular` | Hidden in M03 teaching | Avoid duplicating the accepted Barracks production role. |
| Forward post | Existing accepted M02 building-health role | Do not require the design-only `Building_CommandPost` prefab. |
| `ability.radar_ping` | Proposed two-use mission loan, then permanent first-clear support reward | Catalog currently says code needed; full transaction/detection acceptance required. |

## Guide-card acceptance

Every reference card resolves its identity, portrait, localized name, role, capabilities, availability and any displayed stats from canonical projections. Missing or contradictory metadata has an explicit review item; it never becomes a fabricated command or an availability claim. English and Persian share card structure and stable IDs. Future cards avoid M04+ plot spoilers and cannot emit M03 gameplay requests.

The class-reference count is exactly 57 unit identities. It does not imply 57 new unit implementations, 57 playable M03 choices, or a request to unlock all classes.
