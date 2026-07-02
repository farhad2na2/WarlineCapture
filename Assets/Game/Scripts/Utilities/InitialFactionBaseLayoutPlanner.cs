using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public enum InitialFactionBasePlacementKind : byte
    {
        Wall = 0,
        Gate = 1,
        CoreBuilding = 2,
        Tent = 3,
        SupportBuilding = 4
    }

    public readonly struct InitialFactionBasePlacement
    {
        public InitialFactionBasePlacement(InitialFactionBasePlacementKind kind, string prefabKey, Vector2Int offset, bool rotateVertical = false)
        {
            Kind = kind;
            PrefabKey = prefabKey;
            Offset = offset;
            RotateVertical = rotateVertical;
        }

        public InitialFactionBasePlacementKind Kind { get; }
        public string PrefabKey { get; }
        public Vector2Int Offset { get; }
        public bool RotateVertical { get; }
    }

    public readonly struct InitialFactionBaseWallRun
    {
        public InitialFactionBaseWallRun(Vector2Int startOffset, Vector2Int endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public Vector2Int StartOffset { get; }
        public Vector2Int EndOffset { get; }
    }

    public readonly struct InitialFactionBaseGateFlankWall
    {
        public InitialFactionBaseGateFlankWall(Vector2Int originOffset, bool rotateVertical)
        {
            OriginOffset = originOffset;
            RotateVertical = rotateVertical;
        }

        public Vector2Int OriginOffset { get; }
        public bool RotateVertical { get; }
    }

    public static class InitialFactionBaseLayoutPlanner
    {
        public const int DefaultGateHalfGapCells = 20;

        public static readonly string[] RequiredBuildingKeys =
        {
            "Building_Ammunition_Depot",
            "Building_Airport",
            "Building_Barrack",
            "Building_Fuel_Bladder",
            "Building_GuardTower",
            "Building_GuardTower_Big",
            "Building_Helipad",
            "Building_OilPump",
            "Building_Refinery",
            "Building_Satelite_Dish",
            "Building_WaterTank"
        };

        public static readonly string[] TentKeys =
        {
            "Tent_Regular",
            "Tent_Contractor",
            "Tent_Expert",
            "Tent_Refugee",
            "Portaloo"
        };

        public static void BuildPlacements(
            int halfWidthCells,
            int halfHeightCells,
            List<InitialFactionBasePlacement> placements)
        {
            if (placements == null)
                return;

            placements.Clear();
            halfWidthCells = Mathf.Max(80, halfWidthCells);
            halfHeightCells = Mathf.Max(60, halfHeightCells);

            placements.Add(new InitialFactionBasePlacement(InitialFactionBasePlacementKind.Gate, "Building_Road_Barrier", new Vector2Int(0, -halfHeightCells)));
            placements.Add(new InitialFactionBasePlacement(InitialFactionBasePlacementKind.Gate, "Building_Road_Barrier", new Vector2Int(halfWidthCells, 0), true));

            AddBuilding(placements, "Building_Ammunition_Depot", new Vector2Int(-64, -28), InitialFactionBasePlacementKind.CoreBuilding);
            AddBuilding(placements, "Building_Airport", new Vector2Int(8, 18));
            AddBuilding(placements, "Building_Barrack", new Vector2Int(-84, 28));
            AddBuilding(placements, "Building_Fuel_Bladder", new Vector2Int(20, -48));
            AddBuilding(placements, "Building_Satelite_Dish", new Vector2Int(-104, 42));
            AddBuilding(placements, "Building_Helipad", new Vector2Int(64, -56));
            AddBuilding(placements, "Building_Helipad", new Vector2Int(84, -56));
            AddBuilding(placements, "Building_Helipad", new Vector2Int(104, -56));

            AddBuilding(placements, "Building_GuardTower", new Vector2Int(-18, -halfHeightCells + 10));
            AddBuilding(placements, "Building_GuardTower", new Vector2Int(halfWidthCells - 12, 16));
            AddBuilding(placements, "Building_GuardTower_Big", new Vector2Int(-halfWidthCells + 8, -halfHeightCells + 8));
            AddBuilding(placements, "Building_GuardTower_Big", new Vector2Int(halfWidthCells - 16, -halfHeightCells + 8));
            AddBuilding(placements, "Building_GuardTower_Big", new Vector2Int(-halfWidthCells + 8, halfHeightCells - 16));
            AddBuilding(placements, "Building_GuardTower_Big", new Vector2Int(halfWidthCells - 16, halfHeightCells - 16));

            AddBuilding(placements, "Building_WaterTank", new Vector2Int(-108, 26));
            AddBuilding(placements, "Building_WaterTank", new Vector2Int(-96, 26));
            AddBuilding(placements, "Building_WaterTank", new Vector2Int(-84, 26));

            AddBuilding(placements, "Building_Refinery", new Vector2Int(-70, -halfHeightCells - 38));
            AddBuilding(placements, "Building_OilPump", new Vector2Int(-halfWidthCells - 30, -34));
            AddBuilding(placements, "Building_OilPump", new Vector2Int(-halfWidthCells - 30, 36));
            AddBuilding(placements, "Building_OilPump", new Vector2Int(halfWidthCells + 30, 42));
            AddBuilding(placements, "Building_OilPump", new Vector2Int(36, halfHeightCells + 30));

            AddTentCluster(placements, "Tent_Regular", new Vector2Int(-108, -12), 2);
            AddTentCluster(placements, "Tent_Regular", new Vector2Int(-94, -12), 3);
            AddTentCluster(placements, "Tent_Contractor", new Vector2Int(-80, -12), 3);
            AddTentCluster(placements, "Tent_Expert", new Vector2Int(-66, -12), 2);
            AddTentCluster(placements, "Tent_Refugee", new Vector2Int(-52, -12), 3);
            AddTentCluster(placements, "Portaloo", new Vector2Int(-38, -12), 2);
        }

        public static void BuildWallRuns(int halfWidthCells, int halfHeightCells, List<InitialFactionBaseWallRun> wallRuns)
        {
            BuildWallRuns(halfWidthCells, halfHeightCells, DefaultGateHalfGapCells, wallRuns);
        }

        public static void BuildWallRuns(int halfWidthCells, int halfHeightCells, int gateHalfGapCells, List<InitialFactionBaseWallRun> wallRuns)
        {
            if (wallRuns == null)
                return;

            wallRuns.Clear();
            halfWidthCells = Mathf.Max(80, halfWidthCells);
            halfHeightCells = Mathf.Max(60, halfHeightCells);
            int gateHalfGap = Mathf.Clamp(gateHalfGapCells, 4, Mathf.Min(halfWidthCells, halfHeightCells) - 4);

            wallRuns.Add(new InitialFactionBaseWallRun(new Vector2Int(-halfWidthCells, halfHeightCells), new Vector2Int(halfWidthCells, halfHeightCells)));
            wallRuns.Add(new InitialFactionBaseWallRun(new Vector2Int(-halfWidthCells, -halfHeightCells), new Vector2Int(-gateHalfGap, -halfHeightCells)));
            wallRuns.Add(new InitialFactionBaseWallRun(new Vector2Int(gateHalfGap, -halfHeightCells), new Vector2Int(halfWidthCells, -halfHeightCells)));
            wallRuns.Add(new InitialFactionBaseWallRun(new Vector2Int(-halfWidthCells, -halfHeightCells), new Vector2Int(-halfWidthCells, halfHeightCells)));
            wallRuns.Add(new InitialFactionBaseWallRun(new Vector2Int(halfWidthCells, -halfHeightCells), new Vector2Int(halfWidthCells, -gateHalfGap)));
            wallRuns.Add(new InitialFactionBaseWallRun(new Vector2Int(halfWidthCells, gateHalfGap), new Vector2Int(halfWidthCells, halfHeightCells)));
        }

        private static void AddTentCluster(List<InitialFactionBasePlacement> placements, string prefabKey, Vector2Int startOffset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2Int offset = startOffset + new Vector2Int(0, i * 10);
                placements.Add(new InitialFactionBasePlacement(InitialFactionBasePlacementKind.Tent, prefabKey, offset));
            }
        }

        private static void AddBuilding(
            List<InitialFactionBasePlacement> placements,
            string prefabKey,
            Vector2Int offset,
            InitialFactionBasePlacementKind kind = InitialFactionBasePlacementKind.SupportBuilding)
        {
            placements.Add(new InitialFactionBasePlacement(kind, prefabKey, offset));
        }

        public static bool IsInsideInterior(Vector2Int offset, int halfWidthCells, int halfHeightCells)
        {
            return offset.x > -halfWidthCells + 4 &&
                   offset.x < halfWidthCells - 4 &&
                   offset.y > -halfHeightCells + 4 &&
                   offset.y < halfHeightCells - 4;
        }

        public static bool IsOutsideBase(Vector2Int offset, int halfWidthCells, int halfHeightCells)
        {
            return offset.x < -halfWidthCells ||
                   offset.x > halfWidthCells ||
                   offset.y < -halfHeightCells ||
                   offset.y > halfHeightCells;
        }

        public static int CalculateGateHalfGap(Vector2Int bottomGateFootprint, Vector2Int sideGateFootprint)
        {
            int bottomLength = Mathf.Max(1, bottomGateFootprint.x);
            int sideLength = Mathf.Max(1, sideGateFootprint.y);
            int longestGateLength = Mathf.Max(bottomLength, sideLength);
            return Mathf.Max(DefaultGateHalfGapCells, Mathf.CeilToInt(longestGateLength * 0.5f) + 2);
        }

        public static int CalculateGateHalfGap(
            Vector2Int bottomGateFootprint,
            Vector2Int sideGateFootprint,
            Vector2Int bottomWallFootprint,
            Vector2Int sideWallFootprint)
        {
            int bottomGateHalf = Mathf.CeilToInt(Mathf.Max(1, bottomGateFootprint.x) * 0.5f);
            int sideGateHalf = Mathf.CeilToInt(Mathf.Max(1, sideGateFootprint.y) * 0.5f);
            int bottomReach = bottomGateHalf + Mathf.Max(1, bottomWallFootprint.x) - 1;
            int sideReach = sideGateHalf + Mathf.Max(1, sideWallFootprint.y) - 1;
            int maxConnectorReach = Mathf.Min(bottomReach, sideReach);
            int preferredGap = Mathf.Min(DefaultGateHalfGapCells, maxConnectorReach);
            return Mathf.Max(Mathf.Max(bottomGateHalf, sideGateHalf) + 2, preferredGap);
        }

        public static void BuildGateFlankWalls(
            int halfWidthCells,
            int halfHeightCells,
            Vector2Int bottomGateFootprint,
            Vector2Int sideGateFootprint,
            Vector2Int bottomWallFootprint,
            Vector2Int sideWallFootprint,
            List<InitialFactionBaseGateFlankWall> flankWalls)
        {
            if (flankWalls == null)
                return;

            flankWalls.Clear();
            halfWidthCells = Mathf.Max(80, halfWidthCells);
            halfHeightCells = Mathf.Max(60, halfHeightCells);
            bottomGateFootprint = ClampFootprint(bottomGateFootprint);
            sideGateFootprint = ClampFootprint(sideGateFootprint);
            bottomWallFootprint = ClampFootprint(bottomWallFootprint);
            sideWallFootprint = ClampFootprint(sideWallFootprint);

            InitialFactionBasePlacement bottomGate = new(InitialFactionBasePlacementKind.Gate, "Building_Road_Barrier", new Vector2Int(0, -halfHeightCells));
            Vector2Int bottomGateOrigin = ResolvePlacementOrigin(Vector2Int.zero, bottomGate, bottomGateFootprint);
            flankWalls.Add(new InitialFactionBaseGateFlankWall(bottomGateOrigin - new Vector2Int(bottomWallFootprint.x, 0), false));
            flankWalls.Add(new InitialFactionBaseGateFlankWall(bottomGateOrigin + new Vector2Int(bottomGateFootprint.x, 0), false));

            InitialFactionBasePlacement sideGate = new(InitialFactionBasePlacementKind.Gate, "Building_Road_Barrier", new Vector2Int(halfWidthCells, 0), true);
            Vector2Int sideGateOrigin = ResolvePlacementOrigin(Vector2Int.zero, sideGate, sideGateFootprint);
            flankWalls.Add(new InitialFactionBaseGateFlankWall(sideGateOrigin - new Vector2Int(0, sideWallFootprint.y), true));
            flankWalls.Add(new InitialFactionBaseGateFlankWall(sideGateOrigin + new Vector2Int(0, sideGateFootprint.y), true));
        }

        public static Vector2Int ResolvePlacementOrigin(
            Vector2Int anchor,
            InitialFactionBasePlacement placement,
            Vector2Int footprintCells)
        {
            Vector2Int origin = anchor + placement.Offset;
            if (placement.Kind != InitialFactionBasePlacementKind.Gate)
                return origin;

            footprintCells = new Vector2Int(Mathf.Max(1, footprintCells.x), Mathf.Max(1, footprintCells.y));
            if (placement.Offset.x == 0 && placement.Offset.y != 0)
                origin.x -= footprintCells.x / 2;
            else if (placement.Offset.y == 0 && placement.Offset.x != 0)
                origin.y -= footprintCells.y / 2;

            return origin;
        }

        private static Vector2Int ClampFootprint(Vector2Int footprint)
        {
            return new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
        }
    }
}
