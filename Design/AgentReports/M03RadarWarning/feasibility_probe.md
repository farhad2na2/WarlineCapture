# M03 feasibility probe

## Prefab_BuildingDefinition_Building_Barrack_Config

Asset: `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset`

- price: 40000
- materialsCost: 90
- productionDurationSeconds: 30
- canAttack: False
- maxHealth: 1200
- attackRange: 0
- attackDamage: 0
- threatDetectionKind: 0
- threatDetectionRadiusCells: 0

## Prefab_BuildingDefinition_GuardTower_Config

Asset: `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_GuardTower_Config.asset`

- price: 22000
- materialsCost: 50
- productionDurationSeconds: 30
- canAttack: True
- maxHealth: 700
- attackRange: 100
- attackDamage: 10
- threatDetectionKind: 0
- threatDetectionRadiusCells: 0

## Prefab_BuildingDefinition_Road_Barrier_Config

Asset: `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Road_Barrier_Config.asset`

- price: 6000
- materialsCost: 15
- productionDurationSeconds: 30
- canAttack: False
- maxHealth: 800
- attackRange: 0
- attackDamage: 0
- threatDetectionKind: 0
- threatDetectionRadiusCells: 0

## Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config

Asset: `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset`

- price: 10000
- materialsCost: 0
- productionDurationSeconds: 5
- canAttack: True
- maxHealth: 125
- attackRange: 85
- attackDamage: 16
- speed: 4.8
- roadSpeedMultiplier: 1.15
- threatDetectionKind: 0
- threatDetectionRadiusCells: 0
- footprintCells: (1, 1)

## Prefab_UnitGrid_Veh_Radar_Tank

Asset: `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Radar_Tank.asset`

- price: 32000
- materialsCost: 0
- productionDurationSeconds: 5
- canAttack: False
- maxHealth: 500
- attackRange: 2
- attackDamage: 10
- speed: 7
- roadSpeedMultiplier: 1.3
- threatDetectionKind: 1
- threatDetectionRadiusCells: 240
- footprintCells: (3, 3)

## Prefab_UnitGrid_Veh_Light_Armored_Car_Config

Asset: `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset`

- price: 28000
- materialsCost: 0
- productionDurationSeconds: 5
- canAttack: True
- maxHealth: 500
- attackRange: 85
- attackDamage: 18
- speed: 13
- roadSpeedMultiplier: 1.35
- threatDetectionKind: 0
- threatDetectionRadiusCells: 0
- footprintCells: (3, 3)

## Prefab_UnitGrid_Veh_APC_Fast_Config

Asset: `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_APC_Fast_Config.asset`

- price: 34000
- materialsCost: 0
- productionDurationSeconds: 5
- canAttack: False
- maxHealth: 550
- attackRange: 2
- attackDamage: 10
- speed: 11
- roadSpeedMultiplier: 1.35
- threatDetectionKind: 0
- threatDetectionRadiusCells: 0
- footprintCells: (3, 3)

Barracks metadata and real request queue: four rifle members per order passed.

## Surface sampling

Window: cells x=500..1119, z=250..489. Image north is up; grey is road/highway, sand is wheel-accessible surface, red is unavailable to wheels. Runtime building footprints still require a separate collision probe.

- int2(580, 300): found=True, type=Blocked, movement=None, height=0.009179778
- int2(700, 300): found=True, type=Blocked, movement=None, height=0.009179778
- int2(825, 315): found=True, type=Road, movement=AllGroundUnits, AirGrounded, height=0.5191798
- int2(850, 330): found=True, type=Road, movement=AllGroundUnits, AirGrounded, height=0.1791798
- int2(885, 342): found=True, type=Terrain, movement=65535, height=1.17918
- int2(910, 350): found=True, type=Blocked, movement=None, height=1.00918
- int2(937, 348): found=True, type=Blocked, movement=None, height=0.009179778
- int2(1026, 340): found=True, type=Terrain, movement=65535, height=0.009179778
- int2(1060, 430): found=True, type=Road, movement=AllGroundUnits, AirGrounded, height=0.4491798
