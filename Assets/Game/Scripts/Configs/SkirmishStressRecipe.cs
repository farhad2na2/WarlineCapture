using System;
using UnityEngine;

namespace Game.Configs
{
    public enum SkirmishStressPhase : byte
    {
        Warmup = 0,
        Idle = 1,
        MassMove = 2,
        DenseCombat = 3,
        AirTransport = 4,
        Destruction = 5
    }

    public enum SkirmishStressLayout : byte
    {
        Spread = 0,
        Concentrated = 1
    }

    public enum SkirmishStressScale : int
    {
        P50 = 50,
        P100 = 100,
        P200 = 200,
        P350 = 350,
        P500 = 500
    }

    public enum SkirmishStressUnitKind : byte
    {
        Infantry = 0,
        GroundVehicle = 1,
        Aircraft = 2,
        Support = 3
    }

    [Serializable]
    public readonly struct SkirmishStressUnitLine
    {
        public SkirmishStressUnitLine(string prefabPath, int countPerSide, SkirmishStressUnitKind kind, Vector2Int spreadOffset, Vector2Int concentratedOffset)
        {
            PrefabPath = prefabPath;
            CountPerSide = countPerSide;
            Kind = kind;
            SpreadOffset = spreadOffset;
            ConcentratedOffset = concentratedOffset;
        }

        public string PrefabPath { get; }
        public int CountPerSide { get; }
        public SkirmishStressUnitKind Kind { get; }
        public Vector2Int SpreadOffset { get; }
        public Vector2Int ConcentratedOffset { get; }
        public string SourceKey => System.IO.Path.GetFileNameWithoutExtension(PrefabPath);
        public bool IsCombat => Kind is SkirmishStressUnitKind.Infantry or SkirmishStressUnitKind.GroundVehicle or SkirmishStressUnitKind.Aircraft;
        public bool IsAir => Kind == SkirmishStressUnitKind.Aircraft;
        public bool IsSupport => Kind == SkirmishStressUnitKind.Support;
    }

    public readonly struct SkirmishStressRequestedCounts
    {
        public SkirmishStressRequestedCounts(int combat, int air, int support, int buildings)
        {
            Combat = combat;
            Air = air;
            Support = support;
            Buildings = buildings;
        }

        public int Combat { get; }
        public int Air { get; }
        public int Support { get; }
        public int Buildings { get; }
        public int GroundCombat => Combat - Air;
    }

    // Deterministic E0.3 recipe. Counts here are requested authoring inputs only;
    // runtime census must report entities that actually exist.
    public static class SkirmishStressRecipe
    {
        public const int FixedSeed = 104729;
        public const int ScenarioIndex = SkirmishPresetConfig.StressScaleProbeScenarioIndex;
        public const string ResourceName = SkirmishPresetConfig.StressResourceName;
        public const string HostMapId = "opmap.skirmish.desert_base_01";
        public const string ReportMarker = "[SkirmishStress]";
        public const int BuildingsPerSide = 6;
        public const int DefaultWarmupHoldSeconds = 2;
        public const int DefaultIdleHoldSeconds = 5;
        public const int DefaultMassMoveSeconds = 8;
        public const int DefaultDenseCombatSeconds = 12;
        public const int DefaultAirTransportSeconds = 10;
        public const int DefaultDestructionSeconds = 15;

        public const string RiflePrefab = "Characters/Unit_Chr_Soldier_Male_02_Alt_04";
        public const string GhilliePrefab = "Characters/Unit_Chr_Ghillie_Male_01";
        public const string ArmoredCarPrefab = "Vehicles/Unit_Veh_Light_Armored_Car";
        public const string ApcFastPrefab = "Vehicles/Unit_Veh_APC_Fast";
        public const string ApcHeavyPrefab = "Vehicles/Unit_Veh_APC_Heavy";
        public const string TankPrefab = "Vehicles/Unit_Veh_Tank_USA";
        public const string AttackHeliPrefab = "Vehicles/Unit_Veh_Helicopter_Attack";
        public const string AttackHeliSmallPrefab = "Vehicles/Unit_Veh_Helicopter_Attack_Small";
        public const string TransportHeliPrefab = "Vehicles/Unit_Veh_Helicopter_Transport";
        public const string TankerPrefab = "Vehicles/Unit_Veh_Truck_Tanker";
        public const string TrayPrefab = "Vehicles/Unit_Veh_Truck_Tray";

        public static readonly Vector2Int PlayerBaseCell = new(835, 610);
        public static readonly Vector2Int EnemyBaseCell = new(1210, 580);
        public static readonly string[] BuildingPrefabs =
        {
            "Buildings/Building_Barrack",
            "Buildings/Building_Ammunition_Depot",
            "Buildings/Building_OilPump",
            "Buildings/Building_Refinery",
            "Buildings/Building_Fuel_Bladder",
            "Buildings/Building_Helipad"
        };

        public static readonly string[] RosterPrefabPaths =
        {
            RiflePrefab, GhilliePrefab, ArmoredCarPrefab, ApcFastPrefab, ApcHeavyPrefab, TankPrefab,
            AttackHeliPrefab, AttackHeliSmallPrefab, TransportHeliPrefab, TankerPrefab, TrayPrefab
        };

        public static SkirmishStressScale DefaultScale => SkirmishStressScale.P100;
        public static SkirmishStressLayout DefaultLayout => SkirmishStressLayout.Spread;
        public static SkirmishStressPhase DefaultPhase => SkirmishStressPhase.Warmup;

        public static bool IsSupportedScale(int scale) =>
            scale is 50 or 100 or 200 or 350 or 500;

        public static SkirmishStressScale NormalizeScale(int scale) =>
            IsSupportedScale(scale) ? (SkirmishStressScale)scale : DefaultScale;

        public static SkirmishStressPhase[] AllPhases { get; } =
        {
            SkirmishStressPhase.Warmup, SkirmishStressPhase.Idle, SkirmishStressPhase.MassMove,
            SkirmishStressPhase.DenseCombat, SkirmishStressPhase.AirTransport, SkirmishStressPhase.Destruction
        };

        public static Vector2Int SpawnCell(int factionId) =>
            factionId == 2 ? EnemyBaseCell : PlayerBaseCell;

        public static int SpawnRadiusCells(SkirmishStressScale scale, SkirmishStressLayout layout)
        {
            int radius = scale switch
            {
                SkirmishStressScale.P50 => 12,
                SkirmishStressScale.P100 => 18,
                SkirmishStressScale.P200 => 28,
                SkirmishStressScale.P350 => 40,
                _ => 52
            };
            return layout == SkirmishStressLayout.Concentrated ? Mathf.Max(8, radius / 2) : radius;
        }

        public static float PhaseHoldSeconds(SkirmishStressPhase phase) => phase switch
        {
            SkirmishStressPhase.Warmup => DefaultWarmupHoldSeconds,
            SkirmishStressPhase.Idle => DefaultIdleHoldSeconds,
            SkirmishStressPhase.MassMove => DefaultMassMoveSeconds,
            SkirmishStressPhase.DenseCombat => DefaultDenseCombatSeconds,
            SkirmishStressPhase.AirTransport => DefaultAirTransportSeconds,
            _ => DefaultDestructionSeconds
        };

        public static SkirmishStressUnitLine[] UnitsFor(SkirmishStressScale scale, SkirmishStressPhase phase, bool sequence)
        {
            bool includeGround = sequence || phase != SkirmishStressPhase.AirTransport;
            bool includeAir = sequence || phase == SkirmishStressPhase.AirTransport;
            bool airEscortOnly = !sequence && phase == SkirmishStressPhase.AirTransport;
            var ground = airEscortOnly ? AirEscort(scale) : includeGround ? GroundCombat(scale).ToLines() : Array.Empty<SkirmishStressUnitLine>();
            var air = includeAir ? Air(scale).ToLines() : Array.Empty<SkirmishStressUnitLine>();
            var support = Support();
            var lines = new SkirmishStressUnitLine[ground.Length + air.Length + support.Length];
            ground.CopyTo(lines, 0);
            air.CopyTo(lines, ground.Length);
            support.CopyTo(lines, ground.Length + air.Length);
            return lines;
        }

        public static SkirmishStressRequestedCounts Requested(SkirmishStressScale scale, SkirmishStressPhase phase, bool sequence)
        {
            var lines = UnitsFor(scale, phase, sequence);
            int combat = 0, air = 0, support = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                int both = lines[i].CountPerSide * 2;
                if (lines[i].IsSupport) support += both;
                if (lines[i].IsCombat) combat += both;
                if (lines[i].IsAir) air += both;
            }
            return new SkirmishStressRequestedCounts(combat, air, support, BuildingsPerSide * 2);
        }

        public static Vector2Int UnitOffset(SkirmishStressUnitLine line, int factionId, SkirmishStressLayout layout)
        {
            Vector2Int offset = layout == SkirmishStressLayout.Concentrated ? line.ConcentratedOffset : line.SpreadOffset;
            return factionId == 2 ? new Vector2Int(-offset.x, offset.y) : offset;
        }

        public static Vector2Int MoveGoal(int factionId, SkirmishStressLayout layout)
        {
            if (layout == SkirmishStressLayout.Concentrated)
                return new Vector2Int(1022, 595);
            return factionId == 2 ? new Vector2Int(980, 595) : new Vector2Int(1060, 595);
        }

        public static GroundTable GroundCombat(SkirmishStressScale scale) => scale switch
        {
            SkirmishStressScale.P50 => new GroundTable(16, 4, 2, 2, 0, 1),
            SkirmishStressScale.P100 => new GroundTable(30, 8, 4, 4, 2, 2),
            SkirmishStressScale.P200 => new GroundTable(60, 16, 8, 8, 4, 4),
            SkirmishStressScale.P350 => new GroundTable(110, 28, 14, 12, 6, 5),
            _ => new GroundTable(160, 40, 20, 16, 8, 6)
        };

        public static AirTable Air(SkirmishStressScale scale) => scale switch
        {
            SkirmishStressScale.P50 => new AirTable(1, 0, 1),
            SkirmishStressScale.P100 => new AirTable(1, 1, 1),
            SkirmishStressScale.P200 => new AirTable(2, 1, 2),
            SkirmishStressScale.P350 => new AirTable(3, 2, 2),
            _ => new AirTable(4, 2, 3)
        };

        public readonly struct GroundTable
        {
            public GroundTable(int rifle, int ghillie, int car, int apcFast, int apcHeavy, int tank)
            {
                Rifle = rifle; Ghillie = ghillie; Car = car; ApcFast = apcFast; ApcHeavy = apcHeavy; Tank = tank;
            }
            public int Rifle { get; }
            public int Ghillie { get; }
            public int Car { get; }
            public int ApcFast { get; }
            public int ApcHeavy { get; }
            public int Tank { get; }
            public int CombatPerSide => Rifle + Ghillie + Car + ApcFast + ApcHeavy + Tank;
            public static implicit operator SkirmishStressUnitLine[](GroundTable table) => table.ToLines();
            public SkirmishStressUnitLine[] ToLines() => new[]
            {
                Line(RiflePrefab, tableSplit(Rifle, 0), SkirmishStressUnitKind.Infantry, new Vector2Int(30, -8), new Vector2Int(160, -8)),
                Line(RiflePrefab, tableSplit(Rifle, 1), SkirmishStressUnitKind.Infantry, new Vector2Int(30, 12), new Vector2Int(160, 12)),
                Line(RiflePrefab, tableSplit(Rifle, 2), SkirmishStressUnitKind.Infantry, new Vector2Int(48, 2), new Vector2Int(175, 2)),
                Line(GhilliePrefab, Ghillie, SkirmishStressUnitKind.Infantry, new Vector2Int(20, 22), new Vector2Int(148, 20)),
                Line(ArmoredCarPrefab, Car, SkirmishStressUnitKind.GroundVehicle, new Vector2Int(20, 0), new Vector2Int(140, 0)),
                Line(ApcFastPrefab, ApcFast, SkirmishStressUnitKind.GroundVehicle, new Vector2Int(16, -22), new Vector2Int(136, -18)),
                Line(ApcHeavyPrefab, ApcHeavy, SkirmishStressUnitKind.GroundVehicle, new Vector2Int(12, -30), new Vector2Int(128, -28)),
                Line(TankPrefab, Tank, SkirmishStressUnitKind.GroundVehicle, new Vector2Int(8, -38), new Vector2Int(120, -36))
            };
            private int tableSplit(int total, int part)
            {
                if (total <= 0) return 0;
                int a = total / 3, b = total / 3, c = total - a - b;
                return part == 0 ? a : part == 1 ? b : c;
            }
        }

        public readonly struct AirTable
        {
            public AirTable(int attack, int small, int transport)
            {
                Attack = attack; Small = small; Transport = transport;
            }
            public int Attack { get; }
            public int Small { get; }
            public int Transport { get; }
            public int CombatPerSide => Attack + Small + Transport;
            public static implicit operator SkirmishStressUnitLine[](AirTable table) => table.ToLines();
            public SkirmishStressUnitLine[] ToLines() => new[]
            {
                Line(AttackHeliPrefab, Attack, SkirmishStressUnitKind.Aircraft, new Vector2Int(4, 42), new Vector2Int(150, 34)),
                Line(AttackHeliSmallPrefab, Small, SkirmishStressUnitKind.Aircraft, new Vector2Int(-8, 42), new Vector2Int(142, 40)),
                Line(TransportHeliPrefab, Transport, SkirmishStressUnitKind.Aircraft, new Vector2Int(-16, 36), new Vector2Int(134, 46))
            };
        }

        private static SkirmishStressUnitLine[] AirEscort(SkirmishStressScale scale)
        {
            int rifle = scale >= SkirmishStressScale.P200 ? 16 : 8;
            int apc = scale >= SkirmishStressScale.P200 ? 2 : 1;
            return new[]
            {
                Line(RiflePrefab, rifle, SkirmishStressUnitKind.Infantry, new Vector2Int(30, -8), new Vector2Int(160, -8)),
                Line(ApcFastPrefab, apc, SkirmishStressUnitKind.GroundVehicle, new Vector2Int(16, -22), new Vector2Int(136, -18))
            };
        }

        private static SkirmishStressUnitLine[] Support() => new[]
        {
            Line(TankerPrefab, 1, SkirmishStressUnitKind.Support, new Vector2Int(10, 30), new Vector2Int(110, 24)),
            Line(TrayPrefab, 2, SkirmishStressUnitKind.Support, new Vector2Int(0, 30), new Vector2Int(100, 28))
        };

        private static SkirmishStressUnitLine Line(string prefab, int count, SkirmishStressUnitKind kind, Vector2Int spread, Vector2Int concentrated) =>
            new(prefab, Mathf.Max(0, count), kind, spread, concentrated);
    }
}
