using System.Collections.Generic;
using UnityEngine;
using Air = Game.Editor.MapVariants.MapVariantCityAirfield;

namespace Game.Editor.MapVariants
{
    // Frontier: a 2048 x 1024 m campaign theatre. One highway links the canal port (west), the refinery valley
    // (centre) and the city-edge airfield (east); a city belt runs along the south and the sea and mountains close the north.
    internal static class MapVariantFrontier
    {
        public const string MapId = "Frontier";
        public const int Seed = 90101;

        private static readonly Rect WorldRect = new(0f, 0f, 2400f, 1500f);
        private static readonly Rect PlayableRect = new(176f, 176f, 2048f, 1024f);

        private const float WaterLevel = -0.8f;
        private const float SeaFloor = -4f;
        private const float HighwayZ = 705f;
        private const float SouthRoadZ = 305f;
        private const float CityRoadZ = 145f;
        private const float PortLaneZ = 565f;
        private const float QuayRoadZ = 1065f;
        private const float QuayEdgeZ = 1180f;
        private const float CanalWest = 520f;
        private const float CanalEast = 560f;
        private const float CanalSouth = 620f;
        private const float RefineryLaneZ = 925f;
        private const float TaxiwayZ = 555f;
        private const float RunwayZ = 470f;

        private static readonly float[] SouthColumns = { 305f, 755f, 905f, 1205f, 1405f, 1555f, 1805f, 2055f, 2305f };

        private static readonly Rect Quay = MapVariantBuilder.FromMinMax(185f, 1072f, 895f, QuayEdgeZ);
        private static readonly Rect PortHub = MapVariantBuilder.FromMinMax(312f, 717f, 512f, 1055f);
        private static readonly Rect PortSupply = MapVariantBuilder.FromMinMax(566f, 717f, 748f, 1055f);
        private static readonly Rect PortWarehouses = MapVariantBuilder.FromMinMax(762f, 717f, 895f, 1055f);
        private static readonly Rect Customs = MapVariantBuilder.FromMinMax(312f, 572f, 512f, 693f);
        private static readonly Rect CanalYard = MapVariantBuilder.FromMinMax(566f, 572f, 748f, 693f);

        private static readonly Rect Refinery = MapVariantBuilder.FromMinMax(917f, 717f, 1493f, 913f);
        private static readonly Rect Grove = MapVariantBuilder.FromMinMax(917f, 937f, 1310f, 1100f);
        private static readonly Rect Ridge = MapVariantBuilder.FromMinMax(1335f, 975f, 1490f, 1085f);
        private const float RidgeHeight = 9f;
        private const float RidgeSlope = 30f;
        private static readonly Rect LauncherCompound = MapVariantBuilder.FromMinMax(1350f, 995f, 1475f, 1040f);
        private static readonly Rect PumpField = MapVariantBuilder.FromMinMax(917f, 452f, 1193f, 693f);
        private static readonly Rect Hamlet = MapVariantBuilder.FromMinMax(917f, 317f, 1193f, 440f);
        private static readonly Rect LogisticsYard = MapVariantBuilder.FromMinMax(1217f, 560f, 1393f, 693f);
        private static readonly Rect Substation = MapVariantBuilder.FromMinMax(1217f, 440f, 1393f, 548f);
        private static readonly Rect Depot = MapVariantBuilder.FromMinMax(1417f, 560f, 1545f, 693f);
        private static readonly Rect NoMansLand = MapVariantBuilder.FromMinMax(1417f, 317f, 1545f, 548f);

        private static readonly Rect Apron = MapVariantBuilder.FromMinMax(1667f, 567f, 2133f, 692f);
        private static readonly Rect CrashField = MapVariantBuilder.FromMinMax(1667f, 317f, 2133f, 432f);

        public static MapVariantBuilder Build()
        {
            var b = new MapVariantBuilder(MapId, WorldRect, PlayableRect, 5f, Seed) { WaterLevel = WaterLevel };
            ShapeTerrain(b);
            LayRoads(b);
            b.FlattenUnderRoads(12f, (x, z, h) => h < WaterLevel);
            b.BuildGround();
            b.BuildWater(MapVariantBuilder.FromMinMax(-2500f, QuayEdgeZ - 20f, 1300f, 4000f), WaterLevel, MapVariantKits.WaterMaterial);
            b.BuildWater(MapVariantBuilder.FromMinMax(CanalWest - 2f, CanalSouth - 2f, CanalEast + 2f, QuayEdgeZ), WaterLevel, MapVariantKits.WaterMaterial);
            b.BuildRoads();
            LayPads(b);
            BuildBackdrop(b);

            BuildQuay(b);
            BuildCanal(b);
            BuildPortYards(b);
            BuildRefinery(b);
            BuildLauncherRidge(b);
            BuildPumpFieldAndHamlet(b);
            BuildLogistics(b);
            BuildDepotFront(b);
            BuildAirfield(b);
            BuildRoadside(b);
            BuildCity(b);
            BuildWilds(b);
            b.DesertDressing(WorldRect);
            AddZones(b);

            b.BuildLighting();
            return b;
        }

        public static IEnumerable<MapVariantView> Views(MapVariantBuilder b)
        {
            yield return b.TopDown("01_topdown_playable", PlayableRect);
            yield return b.TopDown("02_topdown_world", WorldRect);
            yield return MapVariantView.Battle("03_battle_port_hub", new Vector2(420f, 900f), 0f);
            yield return MapVariantView.Battle("04_battle_canal_bridge", new Vector2(540f, 690f), 0f);
            yield return MapVariantView.Battle("05_battle_refinery", new Vector2(1200f, 800f), 0f, 110f);
            yield return MapVariantView.Battle("06_battle_launcher_ridge", new Vector2(1410f, 985f), RidgeHeight * 0.5f);
            yield return MapVariantView.Battle("07_battle_pump_field", new Vector2(1060f, 560f), 0f);
            yield return MapVariantView.Battle("08_battle_depot_front", new Vector2(1480f, 600f), 0f);
            yield return MapVariantView.Battle("09_battle_airfield_helipads", new Vector2(2010f, 610f), 0f);
            yield return MapVariantView.Battle("10_battle_crash_field", new Vector2(1900f, 400f), 0f);
            yield return new MapVariantView("11_hero_frontier", new Vector3(260f, 120f, 190f), new Vector3(16f, 55f, 0f), 55f);
            yield return new MapVariantView("12_hero_airfield", new Vector3(1640f, 55f, 250f), new Vector3(15f, 30f, 0f), 52f);
            yield return new MapVariantView("13_hero_harbour", new Vector3(760f, 60f, 820f), new Vector3(15f, 320f, 0f), 52f);
            yield return new MapVariantView("14_ground_highway", new Vector3(620f, 4f, HighwayZ + 1f), new Vector3(4f, 90f, 0f), 55f);
        }

        private static void ShapeTerrain(MapVariantBuilder b)
        {
            b.Height.Apply((x, z, _) =>
            {
                float n = Mathf.PerlinNoise(x * 0.011f + 6.1f, z * 0.011f + 2.9f);
                float h = 0f;

                if (x >= Quay.xMin && x <= Quay.xMax)
                {
                    if (z > QuayEdgeZ)
                        h = SeaFloor;
                }
                else if (x < Quay.xMin)
                {
                    h = Mathf.Lerp(0.5f + n * 1.5f, SeaFloor, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1075f, 1175f, z)));
                }
                else
                {
                    float coast = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(905f, 1120f, x));
                    float shore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1110f, 1210f, z));
                    h = Mathf.Lerp(n * shore, SeaFloor, coast * shore);
                }

                if (x > CanalWest && x < CanalEast && z > CanalSouth && z <= QuayEdgeZ + 5f)
                    h = SeaFloor;

                float north = Mathf.Clamp01((z - 1180f) / 260f) * Mathf.Clamp01((x - 1060f) / 160f);
                float ridges = Mathf.PerlinNoise(x * 0.005f + 3.3f, z * 0.02f) * 0.7f + Mathf.PerlinNoise(x * 0.02f, z * 0.02f + 8f) * 0.3f;
                h += north * north * (20f + ridges * 150f);
                h += Mathf.Clamp01((x - 2250f) / 140f) * (1f + n * 6f);
                h += Mathf.Clamp01((110f - z) / 100f) * (1f + n * 5f);

                if (Hamlet.Contains(new Vector2(x, z)) || NoMansLand.Contains(new Vector2(x, z)) || CrashField.Contains(new Vector2(x, z)))
                    h += Mathf.Max(0f, Mathf.PerlinNoise(x * 0.03f + 7f, z * 0.03f) - 0.55f) * 2.5f;

                float dx = Mathf.Max(Ridge.xMin - x, 0f, x - Ridge.xMax);
                float dz = Mathf.Max(Ridge.yMin - z, 0f, z - Ridge.yMax);
                float ridge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(dx * dx + dz * dz) / RidgeSlope);
                return ridge > 0f ? Mathf.Max(h, ridge * RidgeHeight) : h;
            });
        }

        private static void LayRoads(MapVariantBuilder b)
        {
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(5f, HighwayZ), new Vector2(515f, HighwayZ));
            b.AddBridge(MapVariantRoadKind.Asphalt, 0f, new Vector2(525f, HighwayZ), new Vector2(555f, HighwayZ));
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(565f, HighwayZ), new Vector2(2395f, HighwayZ));

            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(5f, SouthRoadZ), new Vector2(2395f, SouthRoadZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(5f, CityRoadZ), new Vector2(2395f, CityRoadZ));
            foreach (float x in SouthColumns)
                b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(x, CityRoadZ), new Vector2(x, SouthRoadZ));

            foreach (float x in new[] { 305f, 755f })
                b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(x, SouthRoadZ), new Vector2(x, QuayRoadZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(5f, PortLaneZ), new Vector2(905f, PortLaneZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(195f, QuayRoadZ), new Vector2(515f, QuayRoadZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(565f, QuayRoadZ), new Vector2(885f, QuayRoadZ));

            b.AddRoad(MapVariantRoadKind.Dirt, new Vector2(905f, SouthRoadZ), new Vector2(905f, RefineryLaneZ),
                new Vector2(1505f, RefineryLaneZ), new Vector2(1505f, HighwayZ));
            b.AddRoad(MapVariantRoadKind.Dirt, new Vector2(1205f, SouthRoadZ), new Vector2(1205f, HighwayZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1405f, SouthRoadZ), new Vector2(1405f, HighwayZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1555f, SouthRoadZ), new Vector2(1555f, 1135f));

            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(1655f, TaxiwayZ), new Vector2(2145f, TaxiwayZ));
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(1655f, TaxiwayZ), new Vector2(1655f, HighwayZ));
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(2145f, TaxiwayZ), new Vector2(2145f, HighwayZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(2305f, SouthRoadZ), new Vector2(2305f, 1135f));

            foreach (float z in new[] { 815f, 925f, 1035f, 1135f })
                b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1555f, z), new Vector2(2395f, z));
            foreach (float x in new[] { 1805f, 2055f })
                b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(x, HighwayZ), new Vector2(x, 1135f));
        }

        private static void LayPads(MapVariantBuilder b)
        {
            b.Pad(MapVariantBuilder.FromMinMax(Quay.xMin, Quay.yMin, CanalWest - 2f, Quay.yMax), MapVariantKits.ConcretePad);
            b.Pad(MapVariantBuilder.FromMinMax(CanalEast + 2f, Quay.yMin, Quay.xMax, Quay.yMax), MapVariantKits.ConcretePad);
            b.Pad(PortHub, MapVariantKits.AsphaltPad);
            b.Pad(PortSupply, MapVariantKits.ConcretePad);
            b.Pad(PortWarehouses, MapVariantKits.ConcretePad);
            b.Pad(Customs, MapVariantKits.AsphaltPad);
            b.Pad(CanalYard, MapVariantKits.ConcretePad);
            b.Pad(Refinery, MapVariantKits.ConcretePad);
            b.Pad(LogisticsYard, MapVariantKits.AsphaltPad);
            b.Pad(Substation, MapVariantKits.ConcretePad);
            b.Pad(Depot, MapVariantKits.DirtPad);
            b.Pad(Apron, MapVariantKits.ConcretePad);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 5; col++)
            {
                Vector2 c = PumpSlot(col, row);
                b.Pad(new Rect(c.x - 7f, c.y - 10f, 14f, 20f), MapVariantKits.DirtPad);
            }
        }

        private static Vector2 PumpSlot(int col, int row) => new(
            Mathf.Lerp(PumpField.xMin, PumpField.xMax, (col + 0.5f) / 5f),
            Mathf.Lerp(PumpField.yMin, PumpField.yMax, (row + 0.5f) / 4f));

        private static void BuildQuay(MapVariantBuilder b)
        {
            var wall = PlaceOptions.Prop.WithPadding(0f);
            b.PlaceLine(MapVariantKits.PortWall, MapVariantLayer.Industrial, new Vector2(Quay.xMin, QuayEdgeZ), new Vector2(CanalWest, QuayEdgeZ), wall);
            b.PlaceLine(MapVariantKits.PortWall, MapVariantLayer.Industrial, new Vector2(CanalEast, QuayEdgeZ), new Vector2(Quay.xMax, QuayEdgeZ), wall);

            foreach (float x in new[] { 240f, 400f, 660f, 830f })
            {
                Rect pier = MapVariantBuilder.FromMinMax(x - 2.3f, QuayEdgeZ + 0.5f, x + 2.3f, QuayEdgeZ + 3.2f + 9.5f * 5.1f + 2.6f);
                if (!b.IsFree(pier))
                    continue;
                for (int i = 0; i < 10; i++)
                    b.PlaceAt(MapVariantKits.Pier, MapVariantLayer.Industrial, new Vector2(x, QuayEdgeZ + 3.2f + i * 5.1f), 0f, 0f, false, reserve: false);
                b.Occupy(pier);
                for (int i = 0; i < 3; i++)
                {
                    b.Afloat(MapVariantKits.Boat, new Vector2(x - 5.2f, QuayEdgeZ + 10f + i * 14f), b.Range(-4f, 4f), 0.35f);
                    b.Afloat(MapVariantKits.Boat, new Vector2(x + 5.2f, QuayEdgeZ + 12f + i * 14f), 180f + b.Range(-4f, 4f), 0.35f);
                }
            }

            for (int i = 0; i < 40; i++)
                b.Afloat(MapVariantKits.Boat, new Vector2(b.Range(20f, 980f), b.Range(1260f, 1460f)), b.Range(0f, 360f), 0.35f);

            b.Row(new[] { MapVariantKits.BoatTie }, MapVariantLayer.Props, new Vector2(Quay.xMin + 2f, QuayEdgeZ - 1f),
                new Vector2(CanalWest - 2f, QuayEdgeZ - 1f), 6f, PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            b.Row(new[] { MapVariantKits.BoatTie }, MapVariantLayer.Props, new Vector2(CanalEast + 2f, QuayEdgeZ - 1f),
                new Vector2(Quay.xMax - 2f, QuayEdgeZ - 1f), 6f, PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            b.Row(new[] { MapVariantKits.Spotlight }, MapVariantLayer.Props, new Vector2(Quay.xMin + 10f, QuayEdgeZ - 6f),
                new Vector2(Quay.xMax - 10f, QuayEdgeZ - 6f), 40f, PlaceOptions.Prop, freeYaw: false, fixedYaw: 180f);

            string[] stacks = MapVariantKits.ContainerStacks;
            for (float x = Quay.xMin + 8f; x < Quay.xMax - 8f; x += 7.5f)
            for (float z = Quay.yMin + 6f; z < QuayEdgeZ - 12f; z += 3.2f)
            {
                if (Mathf.Abs(x - 305f) < 10f || Mathf.Abs(x - 755f) < 10f || Mathf.Abs(x - 540f) < 26f || b.Chance(0.2f))
                    continue;
                b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
            }

            b.Scatter(MapVariantKits.PortCargo, MapVariantLayer.Props, Quay, 320, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
        }

        private static void BuildCanal(MapVariantBuilder b)
        {
            var wall = PlaceOptions.Prop.WithPadding(0f);
            foreach ((float from, float to) in new[] { (CanalSouth, HighwayZ - 6f), (HighwayZ + 6f, QuayEdgeZ) })
            {
                b.PlaceLine(MapVariantKits.PortWall, MapVariantLayer.Industrial, new Vector2(CanalWest, from), new Vector2(CanalWest, to), wall);
                b.PlaceLine(MapVariantKits.PortWallAlt, MapVariantLayer.Industrial, new Vector2(CanalEast, from), new Vector2(CanalEast, to), wall);
            }

            b.PlaceLine(MapVariantKits.PortWall, MapVariantLayer.Industrial, new Vector2(CanalWest, CanalSouth), new Vector2(CanalEast, CanalSouth), wall);
            for (float z = CanalSouth + 20f; z < QuayEdgeZ - 10f; z += 24f)
            {
                if (Mathf.Abs(z - HighwayZ) < 16f)
                    continue;
                b.Afloat(MapVariantKits.Boat, new Vector2(CanalWest + 5f, z + b.Range(-3f, 3f)), b.Range(-3f, 3f), 0.35f);
                if (b.Chance(0.5f))
                    b.Afloat(MapVariantKits.Boat, new Vector2(CanalEast - 5f, z + 12f), 180f + b.Range(-3f, 3f), 0.35f);
            }

            b.Row(MapVariantKits.Checkpoint, MapVariantLayer.Props, new Vector2(500f, HighwayZ - 6.5f), new Vector2(514f, HighwayZ - 6.5f), 2.2f,
                PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            b.Row(MapVariantKits.Checkpoint, MapVariantLayer.Props, new Vector2(566f, HighwayZ + 6.5f), new Vector2(580f, HighwayZ + 6.5f), 2.2f,
                PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
        }

        private static void BuildPortYards(MapVariantBuilder b)
        {
            string[] stacks = MapVariantKits.ContainerStacks;

            b.Row(new[] { MapVariantKits.PortWarehouse, MapVariantKits.Warehouse }, MapVariantLayer.Industrial,
                new Vector2(PortHub.xMin + 12f, PortHub.yMax - 13f), new Vector2(PortHub.xMax - 12f, PortHub.yMax - 13f), 24.5f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false, fixedYaw: 0f);
            b.Place(MapVariantKits.ControlTower, MapVariantLayer.Industrial, new Vector2(PortHub.xMax - 9f, PortHub.yMin + 9f), 180f,
                PlaceOptions.Structure.WithPadding(1f));
            for (float x = PortHub.xMin + 6f; x < PortHub.xMax - 20f; x += 7.5f)
            for (float z = PortHub.yMin + 6f; z < PortHub.yMax - 44f; z += 3.2f)
            {
                if (Mathf.Abs(z - 860f) < 6f || Mathf.Abs(z - 960f) < 6f || b.Chance(0.12f))
                    continue;
                b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
            }

            b.Row(new[] { MapVariantKits.LightPole }, MapVariantLayer.Props, new Vector2(PortHub.xMin + 2f, 860f), new Vector2(PortHub.xMax - 2f, 860f), 20f,
                PlaceOptions.Prop);
            b.Scatter(MapVariantKits.PortCargo, MapVariantLayer.Props, PortHub, 200, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);

            b.FencedCompound(PortSupply, MapVariantKits.Fence,
                new MapVariantGate(MapVariantSide.South, 90f, 18f),
                new MapVariantGate(MapVariantSide.North, 90f, 18f),
                new MapVariantGate(MapVariantSide.East, 170f, 12f));
            b.Row(new[] { MapVariantKits.GasSphereTank }, MapVariantLayer.Industrial, new Vector2(PortSupply.xMin + 12f, PortSupply.yMax - 14f),
                new Vector2(PortSupply.xMax - 12f, PortSupply.yMax - 14f), 17f, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Row(new[] { MapVariantKits.StorageTank }, MapVariantLayer.Industrial, new Vector2(PortSupply.xMin + 10f, PortSupply.yMax - 36f),
                new Vector2(PortSupply.xMax - 10f, PortSupply.yMax - 36f), 15f, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(PortSupply.xMin + 6f, PortSupply.yMax - 54f),
                new Vector2(PortSupply.xMax - 6f, PortSupply.yMax - 54f), PlaceOptions.Structure.WithPadding(0.2f).WithBoundsAnchor());
            b.Grid(MapVariantKits.FuelBladder, MapVariantLayer.Industrial,
                MapVariantBuilder.FromMinMax(PortSupply.xMin + 6f, PortSupply.yMin + 8f, PortSupply.xMax - 6f, PortSupply.yMin + 110f), 5, 5, 0f,
                PlaceOptions.Prop.WithPadding(0.8f));
            b.Row(new[] { MapVariantKits.Tanker }, MapVariantLayer.Vehicles, new Vector2(PortSupply.xMin + 8f, PortSupply.yMin + 132f),
                new Vector2(PortSupply.xMin + 90f, PortSupply.yMin + 132f), 4.4f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(new[] { MapVariantKits.TrayTruck, MapVariantKits.CanopyTruck }, MapVariantLayer.Vehicles, new Vector2(PortSupply.xMin + 100f, PortSupply.yMin + 132f),
                new Vector2(PortSupply.xMax - 8f, PortSupply.yMin + 132f), 4.6f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Grid(MapVariantKits.Shed, MapVariantLayer.Industrial,
                MapVariantBuilder.FromMinMax(PortSupply.xMin + 8f, PortSupply.yMin + 150f, PortSupply.xMax - 8f, PortSupply.yMin + 180f), 4, 1, 0f,
                PlaceOptions.Structure.WithPadding(0.8f));
            for (float x = PortSupply.xMin + 8f; x < PortSupply.xMax - 8f; x += 7.5f)
            for (float z = PortSupply.yMin + 190f; z < PortSupply.yMax - 62f; z += 3.2f)
            {
                if (Mathf.Abs(x - PortSupply.center.x) < 9f || b.Chance(0.3f))
                    continue;
                b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
            }
            b.Scatter(MapVariantKits.YardCargo, MapVariantLayer.Props, PortSupply, 200, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);

            foreach (Rect yard in new[] { PortWarehouses, CanalYard })
            {
                b.Row(new[] { MapVariantKits.PortWarehouse, MapVariantKits.Warehouse }, MapVariantLayer.Industrial,
                    new Vector2(yard.xMin + 12f, yard.yMin + 14f), new Vector2(yard.xMax - 12f, yard.yMin + 14f), 24.5f,
                    PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false, fixedYaw: 180f);
                for (float x = yard.xMin + 6f; x < yard.xMax - 6f; x += 7.5f)
                for (float z = yard.yMin + 34f; z < yard.yMax - 6f; z += 3.2f)
                {
                    if (Mathf.Abs(z - (yard.yMin + 100f)) < 6f || b.Chance(0.25f))
                        continue;
                    b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
                }

                b.Scatter(MapVariantKits.Trucks, MapVariantLayer.Vehicles, yard, 10, PlaceOptions.Prop.WithPadding(0.6f), freeYaw: false);
            }

            b.Row(MapVariantKits.Trucks, MapVariantLayer.Vehicles, new Vector2(Customs.xMin + 4f, Customs.yMax - 10f),
                new Vector2(Customs.xMax - 4f, Customs.yMax - 10f), 4.4f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(MapVariantKits.Trucks, MapVariantLayer.Vehicles, new Vector2(Customs.xMin + 4f, Customs.yMax - 24f),
                new Vector2(Customs.xMax - 4f, Customs.yMax - 24f), 4.4f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(new[] { MapVariantKits.Shed, MapVariantKits.Barracks }, MapVariantLayer.Industrial, new Vector2(Customs.xMin + 10f, Customs.yMin + 14f),
                new Vector2(Customs.xMax - 10f, Customs.yMin + 14f), 16f, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false, fixedYaw: 180f);
            b.Scatter(MapVariantKits.PortCargo, MapVariantLayer.Props, Customs, 140, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
        }

        private static void BuildRefinery(MapVariantBuilder b)
        {
            Rect r = Refinery;
            b.FencedCompound(r, MapVariantKits.Fence,
                new MapVariantGate(MapVariantSide.South, 120f, 16f),
                new MapVariantGate(MapVariantSide.South, 420f, 16f),
                new MapVariantGate(MapVariantSide.West, 100f, 12f),
                new MapVariantGate(MapVariantSide.East, 100f, 12f));

            var racked = PlaceOptions.Structure.WithPadding(0.2f).WithBoundsAnchor();
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(r.xMin + 6f, r.yMin + 12f), new Vector2(1110f, r.yMin + 12f), racked);
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(1130f, r.yMin + 12f), new Vector2(r.xMax - 6f, r.yMin + 12f), racked);
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(1120f, r.yMin + 24f), new Vector2(1120f, r.yMax - 6f), racked);
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(1335f, r.yMin + 24f), new Vector2(1335f, r.yMax - 6f), racked);

            foreach (float z in new[] { 752f, 780f })
                b.Row(new[] { MapVariantKits.GasSphereTank }, MapVariantLayer.Industrial, new Vector2(935f, z), new Vector2(1100f, z), 20f,
                    PlaceOptions.Structure.WithPadding(1f), freeYaw: false);
            b.Row(new[] { MapVariantKits.StorageTank }, MapVariantLayer.Industrial, new Vector2(940f, 812f), new Vector2(1100f, 812f), 22f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Grid(MapVariantKits.FuelBladder, MapVariantLayer.Industrial, MapVariantBuilder.FromMinMax(930f, 832f, 1105f, 905f), 5, 3, 0f,
                PlaceOptions.Prop.WithPadding(0.8f));

            b.Row(new[] { MapVariantKits.OilTower }, MapVariantLayer.Industrial, new Vector2(1145f, 760f), new Vector2(1320f, 760f), 30f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false, fixedYaw: 180f);
            b.Row(MapVariantKits.StacksOnPlatforms, MapVariantLayer.Industrial, new Vector2(1150f, 798f), new Vector2(1320f, 798f), 24f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false);
            b.Scatter(new[] { MapVariantKits.Transformer, MapVariantKits.Generator, MapVariantKits.Warehouse, MapVariantKits.Shed }, MapVariantLayer.Industrial,
                MapVariantBuilder.FromMinMax(1128f, 820f, 1328f, 908f), 50, PlaceOptions.Structure.WithPadding(1.5f), freeYaw: false);

            b.Place(MapVariantKits.GasHolder, MapVariantLayer.Industrial, new Vector2(1415f, 780f), 0f, PlaceOptions.Structure.WithPadding(1f));
            b.Place(MapVariantKits.GasHolder, MapVariantLayer.Industrial, new Vector2(1415f, 860f), 0f, PlaceOptions.Structure.WithPadding(1f));
            b.Scatter(new[] { MapVariantKits.StorageTank, MapVariantKits.Generator }, MapVariantLayer.Industrial,
                MapVariantBuilder.FromMinMax(1342f, 740f, 1488f, 908f), 30, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);

            b.Scatter(MapVariantKits.YardCargo, MapVariantLayer.Props, r, 380, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(new[] { MapVariantKits.LightPole }, MapVariantLayer.Props, new Vector2(r.xMin + 6f, r.yMin + 21f), new Vector2(r.xMax - 6f, r.yMin + 21f), 26f,
                PlaceOptions.Prop);
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(r.xMin + 108f, r.yMin + 4f), 0f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(r.xMin + 440f, r.yMin + 4f), 0f, PlaceOptions.Structure.WithPadding(0.3f));

            b.Scatter(MapVariantKits.Houses, MapVariantLayer.City, Grove, 45, PlaceOptions.Structure.WithPadding(3f).WithGroundDelta(0.8f), freeYaw: false);
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, Grove, 360, PlaceOptions.Vegetation.WithGroundDelta(1.2f), minScale: 0.9f, maxScale: 1.25f);
            b.Scatter(MapVariantKits.SmallPalms, MapVariantLayer.Vegetation, Grove, 200, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, Grove, 200, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
        }

        private static void BuildLauncherRidge(MapVariantBuilder b)
        {
            Rect c = LauncherCompound;
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(c.xMin, c.yMin), new Vector2(c.xMax, c.yMin), PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(c.xMin, c.yMin + 3f), new Vector2(c.xMin, c.yMax), PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.RazorWire, MapVariantLayer.Military, new Vector2(c.xMin - 4f, c.yMin - 5f), new Vector2(c.xMax, c.yMin - 5f), PlaceOptions.Prop);
            for (float x = c.xMin + 18f; x <= c.xMax - 10f; x += 16f)
            {
                MapVariantRefineryDistrict.SandbagRing(b, new Vector2(x, c.center.y), 5f);
                b.Place(b.Pick(MapVariantKits.Launchers), MapVariantLayer.Military, new Vector2(x, c.center.y), 180f, PlaceOptions.Structure.WithPadding(0.2f));
            }

            b.Place(MapVariantKits.MobileRadar, MapVariantLayer.Military, new Vector2(1370f, 1065f), 200f, PlaceOptions.Structure);
            b.Place(MapVariantKits.MobileRadar, MapVariantLayer.Military, new Vector2(1455f, 1065f), 160f, PlaceOptions.Structure);
            b.Row(new[] { MapVariantKits.RocketTruck }, MapVariantLayer.Vehicles, new Vector2(1390f, 1062f), new Vector2(1435f, 1062f), 8f,
                PlaceOptions.Structure.WithPadding(0.6f), freeYaw: false, fixedYaw: 90f);
            b.Scatter(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, MapVariantBuilder.Inset(Ridge, 4f), 80, PlaceOptions.Prop, freeYaw: false);

            var boulders = new[] { MapVariantKits.Rocks[4], MapVariantKits.Rocks[5], MapVariantKits.Rocks[6], MapVariantKits.Rocks[7], MapVariantKits.Rocks[3] };
            foreach (float band in new[] { 0.2f, 0.5f, 0.8f })
            {
                Rect rim = MapVariantBuilder.Inset(Ridge, -RidgeSlope * band);
                float perimeter = 2f * (rim.width + rim.height);
                for (float t = b.Range(0f, 4f); t < perimeter; t += b.Range(3.5f, 7f))
                {
                    Vector2 p = PointOnPerimeter(rim, t) + new Vector2(b.Range(-4f, 4f), b.Range(-4f, 4f));
                    if (b.DistanceToRoad(p) < 6f)
                        continue;
                    b.Place(b.Pick(boulders), MapVariantLayer.Vegetation, p, b.Range(0f, 360f),
                        PlaceOptions.Vegetation.WithGroundDelta(7f).WithScale(b.Range(0.9f, 2.3f - band)));
                }
            }

            Rect foot = MapVariantBuilder.Inset(Ridge, -RidgeSlope - 15f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, foot, 260, PlaceOptions.Vegetation.WithGroundDelta(3f), accept: p => !Ridge.Contains(p));
            b.Scatter(MapVariantKits.Pebbles, MapVariantLayer.Vegetation, foot, 160, PlaceOptions.Vegetation.WithGroundDelta(3f));
        }

        private static Vector2 PointOnPerimeter(Rect r, float t)
        {
            if (t < r.width) return new Vector2(r.xMin + t, r.yMin);
            t -= r.width;
            if (t < r.height) return new Vector2(r.xMax, r.yMin + t);
            t -= r.height;
            if (t < r.width) return new Vector2(r.xMax - t, r.yMax);
            t -= r.width;
            return new Vector2(r.xMin, r.yMax - t);
        }

        private static void BuildPumpFieldAndHamlet(MapVariantBuilder b)
        {
            b.Grid(MapVariantKits.OilPump, MapVariantLayer.Industrial, PumpField, 5, 4, 0f, PlaceOptions.Structure.WithPadding(1f));
            for (int col = 0; col < 5; col++)
            {
                float x = PumpSlot(col, 0).x + 5.5f;
                b.PlaceLine(MapVariantKits.SmallPipe, MapVariantLayer.Industrial, new Vector2(x, PumpField.yMin + 4f), new Vector2(x, PumpField.yMax - 4f),
                    PlaceOptions.Prop.WithPadding(0.1f));
            }

            b.PlaceLine(MapVariantKits.SmallPipe, MapVariantLayer.Industrial, new Vector2(PumpField.xMin + 4f, PumpField.yMin + 1.5f),
                new Vector2(PumpField.xMax - 2f, PumpField.yMin + 1.5f), PlaceOptions.Prop.WithPadding(0.1f));
            b.Scatter(MapVariantKits.YardCargo, MapVariantLayer.Props, PumpField, 90, PlaceOptions.Prop, freeYaw: true);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, PumpField, 200, PlaceOptions.Vegetation);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, PumpField, 60, PlaceOptions.Vegetation, minScale: 0.6f, maxScale: 1.2f);
            b.Scatter(new[] { MapVariantKits.Tanker }, MapVariantLayer.Vehicles, PumpField, 16, PlaceOptions.Prop.WithPadding(1f), freeYaw: false);

            var ruinsBig = new[] { MapVariantKits.Ruins[4], MapVariantKits.Ruins[5] };
            var ruinsSmall = new[] { MapVariantKits.Ruins[0], MapVariantKits.Ruins[1], MapVariantKits.Ruins[2], MapVariantKits.Ruins[3] };
            foreach (Rect block in new[] { MapVariantBuilder.FromMinMax(930f, 382f, 1180f, 432f), MapVariantBuilder.FromMinMax(930f, 322f, 1180f, 372f) })
            {
                b.Frontage(block, MapVariantSide.North, ruinsBig, 1f, 2.5f);
                b.Frontage(block, MapVariantSide.South, ruinsBig, 1f, 2.5f);
                b.Scatter(ruinsSmall, MapVariantLayer.City, block, 70, PlaceOptions.Structure.WithGroundDelta(0.8f), freeYaw: false);
            }

            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, Hamlet, 90, PlaceOptions.Vegetation.WithGroundDelta(1.2f), minScale: 0.7f, maxScale: 1.6f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, Hamlet, 140, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, Hamlet, 30, PlaceOptions.Vegetation.WithGroundDelta(1.2f), minScale: 0.8f, maxScale: 1.1f);
        }

        private static void BuildLogistics(MapVariantBuilder b)
        {
            Rect y = LogisticsYard;
            b.FencedCompound(y, MapVariantKits.Fence,
                new MapVariantGate(MapVariantSide.North, 50f, 18f),
                new MapVariantGate(MapVariantSide.North, 130f, 18f),
                new MapVariantGate(MapVariantSide.West, 60f, 12f));
            b.Row(new[] { MapVariantKits.Warehouse }, MapVariantLayer.Industrial, new Vector2(y.xMin + 18f, y.yMin + 16f), new Vector2(y.xMax - 18f, y.yMin + 16f), 25f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false, fixedYaw: 0f);
            b.Row(new[] { MapVariantKits.Tanker }, MapVariantLayer.Vehicles, new Vector2(y.xMin + 8f, y.yMin + 50f), new Vector2(y.xMin + 90f, y.yMin + 50f), 4.6f,
                PlaceOptions.Prop.WithPadding(0.5f), freeYaw: false, fixedYaw: 0f);
            b.Row(new[] { MapVariantKits.TrayTruck, MapVariantKits.CanopyTruck }, MapVariantLayer.Vehicles, new Vector2(y.xMin + 98f, y.yMin + 50f),
                new Vector2(y.xMax - 8f, y.yMin + 50f), 4.6f, PlaceOptions.Prop.WithPadding(0.5f), freeYaw: false, fixedYaw: 0f);
            string[] stacks = MapVariantKits.ContainerStacks;
            for (float x = y.xMin + 8f; x < y.xMax - 8f; x += 8f)
            for (float z = y.yMin + 72f; z < y.yMax - 6f; z += 3.2f)
            {
                if (Mathf.Abs(x - (y.xMin + 50f)) < 11f || Mathf.Abs(x - (y.xMin + 130f)) < 11f)
                    continue;
                b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
            }

            b.Scatter(MapVariantKits.YardCargo, MapVariantLayer.Props, y, 160, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);

            Rect s = Substation;
            b.FencedCompound(s, MapVariantKits.Fence, new MapVariantGate(MapVariantSide.North, 88f, 14f), new MapVariantGate(MapVariantSide.West, 54f, 10f));
            Rect bays = MapVariantBuilder.Inset(s, 6f);
            b.Grid(MapVariantKits.Transformer, MapVariantLayer.Industrial, new Rect(bays.xMin, bays.yMin, bays.width, bays.height * 0.34f), 8, 3, 90f,
                PlaceOptions.Structure.WithPadding(0.8f));
            b.Grid(MapVariantKits.Generator, MapVariantLayer.Industrial, new Rect(bays.xMin, bays.yMin + bays.height * 0.38f, bays.width, bays.height * 0.25f), 10, 2, 0f,
                PlaceOptions.Structure.WithPadding(0.6f));
            for (float z = bays.yMin + bays.height * 0.7f; z < bays.yMax; z += 6f)
                b.Row(MapVariantKits.PowerPoles, MapVariantLayer.Industrial, new Vector2(bays.xMin, z), new Vector2(bays.xMax, z), 6f,
                    PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false, fixedYaw: 0f);
            b.Row(MapVariantKits.PowerPoles, MapVariantLayer.Props, new Vector2(1212f, 320f), new Vector2(1212f, 435f), 24f,
                PlaceOptions.Prop, freeYaw: false, fixedYaw: 90f);

            b.CityBlock(MapVariantBuilder.FromMinMax(1217f, 317f, 1393f, 428f), MapVariantKits.Houses, MapVariantKits.Houses);
        }

        private static void BuildDepotFront(MapVariantBuilder b)
        {
            Rect d = Depot;
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(d.xMin + 2f, d.yMin), new Vector2(d.xMax - 2f, d.yMin), PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(d.xMax, d.yMin + 3f), new Vector2(d.xMax, d.yMax - 2f), PlaceOptions.Prop);
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(d.xMin + 6f, d.yMin + 6f), 0f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(d.xMax - 6f, d.yMin + 6f), 0f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Grid(MapVariantKits.FuelBladder, MapVariantLayer.Military, MapVariantBuilder.FromMinMax(d.xMin + 50f, d.yMin + 70f, d.xMax - 6f, d.yMax - 6f), 4, 3, 0f,
                PlaceOptions.Prop.WithPadding(0.8f));
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(d.xMin + 12f, d.yMin + 22f), new Vector2(d.xMax - 12f, d.yMin + 22f), 12.5f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(d.xMin + 12f, d.yMin + 40f), new Vector2(d.xMax - 12f, d.yMin + 40f), 12.5f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Row(MapVariantKits.Trucks, MapVariantLayer.Vehicles, new Vector2(d.xMin + 6f, d.yMin + 70f), new Vector2(d.xMin + 44f, d.yMin + 70f), 4.4f,
                PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(MapVariantKits.ArmorParked, MapVariantLayer.Vehicles, new Vector2(d.xMin + 6f, d.yMin + 90f), new Vector2(d.xMin + 44f, d.yMin + 90f), 5f,
                PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Scatter(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, d, 180, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);

            Rect n = NoMansLand;
            b.PlaceLine(MapVariantKits.RazorWire, MapVariantLayer.Military, new Vector2(n.xMin + 4f, n.yMax - 4f), new Vector2(n.xMax - 4f, n.yMax - 4f),
                PlaceOptions.Prop.WithPadding(0.05f));
            for (int i = 0; i < 5; i++)
                MapVariantRefineryDistrict.SandbagRing(b, new Vector2(b.Range(n.xMin + 12f, n.xMax - 12f), b.Range(n.yMin + 12f, n.yMax - 20f)), 5f);
            b.Scatter(Air.Wreckage, MapVariantLayer.Vehicles, n, 10, PlaceOptions.Structure.WithPadding(1.5f).WithGroundDelta(1f));
            b.Scatter(MapVariantKits.Ruins, MapVariantLayer.City, n, 18, PlaceOptions.Structure.WithGroundDelta(0.8f), freeYaw: false);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, n, 90, PlaceOptions.Vegetation.WithGroundDelta(1.2f), minScale: 0.8f, maxScale: 1.8f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, n, 120, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
        }

        private static void BuildAirfield(MapVariantBuilder b)
        {
            PlaceOptions slab = PlaceOptions.Structure.WithSink(1.14f).WithBoundsAnchor();
            slab.Reserve = false;
            b.PlaceLine(Air.Runway, MapVariantLayer.Industrial, new Vector2(1690.7f, RunwayZ), new Vector2(2109.3f, RunwayZ), slab);
            b.Place(Air.TransportPlane, MapVariantLayer.Vehicles, new Vector2(2050f, RunwayZ), 270f,
                PlaceOptions.Structure.WithPadding(1f).WithBoundsAnchor().WithGroundDelta(0.5f).WithSink(-0.06f));
            foreach (float z in new[] { RunwayZ - 18.5f, RunwayZ + 18.5f })
                b.Row(new[] { Air.RunwayLight }, MapVariantLayer.Props, new Vector2(1692f, z), new Vector2(2108f, z), 12f,
                    PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            foreach (float x in new[] { 1683f, 2117f })
                b.Row(new[] { Air.RunwayBarrier }, MapVariantLayer.Props, new Vector2(x, RunwayZ - 12f), new Vector2(x, RunwayZ + 12f), 11.5f,
                    PlaceOptions.Prop.WithPadding(0.1f), freeYaw: false);
            b.PlaceLine(MapVariantKits.Fence, MapVariantLayer.Props, new Vector2(1667f, 440f), new Vector2(2133f, 440f), PlaceOptions.Prop.WithPadding(0.05f));

            Rect a = Apron;
            b.Row(new[] { MapVariantKits.HangarOpen, MapVariantKits.Hangar }, MapVariantLayer.Industrial, new Vector2(a.xMin + 22f, a.yMax - 19f),
                new Vector2(a.xMin + 160f, a.yMax - 19f), 34f, PlaceOptions.Structure.WithPadding(1f), freeYaw: false, fixedYaw: 180f);
            b.Place(MapVariantKits.ControlTower, MapVariantLayer.Industrial, new Vector2(a.xMin + 200f, a.yMax - 12f), 180f, PlaceOptions.Structure.WithPadding(1f));
            b.Row(new[] { Air.Jet }, MapVariantLayer.Vehicles, new Vector2(a.xMin + 186f, a.yMin + 36f), new Vector2(a.xMin + 280f, a.yMin + 36f), 18f,
                PlaceOptions.Structure.WithPadding(0.6f).WithBoundsAnchor(), freeYaw: false, fixedYaw: 180f);
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(a.xMin + 225f, a.yMax - 14f), new Vector2(a.xMin + 300f, a.yMax - 14f), 13f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);

            PlaceOptions pad = PlaceOptions.Structure.WithPadding(0.5f).WithSink(0.72f).WithBoundsAnchor();
            MapVariantPlacement westPad = b.Place(Air.Helipad, MapVariantLayer.Industrial, new Vector2(1990f, 640f), 0f, pad);
            MapVariantPlacement eastPad = b.Place(Air.Helipad, MapVariantLayer.Industrial, new Vector2(2030f, 640f), 0f, pad);
            b.PlaceOnTop(Air.TransportHeli, MapVariantLayer.Vehicles, westPad, Vector2.zero, 200f);
            b.PlaceOnTop(Air.AttackHeli, MapVariantLayer.Vehicles, eastPad, Vector2.zero, 160f);
            b.Row(new[] { Air.FieldHospital, Air.FieldHospitalOpen }, MapVariantLayer.Military, new Vector2(2060f, 675f), new Vector2(2128f, 675f), 13.5f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false, fixedYaw: 90f);
            b.Scatter(new[] { Air.MedicalBed, Air.Stretcher, Air.MedicalBox }, MapVariantLayer.Props, MapVariantBuilder.FromMinMax(2055f, 600f, 2130f, 662f), 70,
                PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
            b.Grid(MapVariantKits.FuelBladder, MapVariantLayer.Military, MapVariantBuilder.FromMinMax(1900f, 572f, 1965f, 605f), 3, 1, 90f,
                PlaceOptions.Prop.WithPadding(0.6f));
            b.Scatter(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, a, 280, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
            b.Scatter(MapVariantKits.Trucks, MapVariantLayer.Vehicles, a, 18, PlaceOptions.Prop.WithPadding(0.6f), freeYaw: false);
            b.Row(new[] { MapVariantKits.LightPole }, MapVariantLayer.Props, new Vector2(a.xMin + 4f, a.yMin + 2f), new Vector2(a.xMax - 4f, a.yMin + 2f), 30f,
                PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.TallBarrier, MapVariantLayer.Military, new Vector2(a.xMin, 696.5f), new Vector2(1895f, 696.5f), PlaceOptions.Prop.WithPadding(0.05f));
            b.PlaceLine(MapVariantKits.TallBarrier, MapVariantLayer.Military, new Vector2(1915f, 696.5f), new Vector2(a.xMax, 696.5f), PlaceOptions.Prop.WithPadding(0.05f));

            Rect c = CrashField;
            b.Scatter(Air.Wreckage, MapVariantLayer.Vehicles, c, 45, PlaceOptions.Structure.WithPadding(1.5f).WithGroundDelta(1f));
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, c, 150, PlaceOptions.Vegetation.WithGroundDelta(1.2f), minScale: 0.8f, maxScale: 2f);
            b.Scatter(MapVariantKits.Ruins, MapVariantLayer.City, c, 24, PlaceOptions.Structure.WithGroundDelta(0.8f), freeYaw: false);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, c, 160, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
            b.Scatter(MapVariantKits.Pebbles, MapVariantLayer.Vegetation, c, 100, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(1662f, 444f, 2140f, 548f), 160, PlaceOptions.Vegetation,
                accept: p => Mathf.Abs(p.y - RunwayZ) > 22f);
        }

        private static void BuildRoadside(MapVariantBuilder b)
        {
            var palmRow = PlaceOptions.Vegetation.WithPadding(0.2f);
            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(20f, HighwayZ - 8.5f), new Vector2(298f, HighwayZ - 8.5f), 13f, palmRow, jitter: 0.3f);
            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(20f, HighwayZ + 8.5f), new Vector2(298f, HighwayZ + 8.5f), 13f, palmRow, jitter: 0.3f);
            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(1562f, HighwayZ + 8.5f), new Vector2(2298f, HighwayZ + 8.5f), 13f, palmRow, jitter: 0.3f);
            b.Row(new[] { MapVariantKits.StreetLight }, MapVariantLayer.Props, new Vector2(600f, HighwayZ - 6f), new Vector2(1640f, HighwayZ - 6f), 30f,
                PlaceOptions.Prop, freeYaw: false, fixedYaw: 0f);
            b.Place(MapVariantKits.GasStation, MapVariantLayer.City, new Vector2(1600f, 672f), 0f, PlaceOptions.Structure);
            b.Place(MapVariantKits.WaterTower, MapVariantLayer.City, new Vector2(1600f, 620f), 0f, PlaceOptions.Structure);
            b.Scatter(MapVariantKits.CivilianVehicles, MapVariantLayer.Vehicles, MapVariantBuilder.FromMinMax(20f, 150f, 2380f, 300f), 110,
                PlaceOptions.Prop.WithPadding(0.8f), freeYaw: false, accept: p => b.DistanceToRoad(p) < 4f);
        }

        private static void BuildCity(MapVariantBuilder b)
        {
            var frontage = new List<string>(MapVariantKits.Houses);
            frontage.AddRange(MapVariantKits.Shops);
            var infill = new List<string>(MapVariantKits.Houses);

            var edges = new List<float> { 5f };
            edges.AddRange(SouthColumns);
            edges.Add(2395f);
            for (int i = 0; i + 1 < edges.Count; i++)
            {
                float xMin = i == 0 ? 20f : edges[i] + 7f;
                float xMax = i + 2 == edges.Count ? 2380f : edges[i + 1] - 7f;
                b.CityBlock(MapVariantBuilder.FromMinMax(xMin, CityRoadZ + 12f, xMax, SouthRoadZ - 12f), frontage, infill);
            }

            Rect[] blocks =
            {
                MapVariantBuilder.FromMinMax(20f, 317f, 298f, 558f),
                MapVariantBuilder.FromMinMax(312f, 317f, 748f, 558f),
                MapVariantBuilder.FromMinMax(762f, 317f, 898f, 558f),
                MapVariantBuilder.FromMinMax(20f, 572f, 298f, 693f),
                MapVariantBuilder.FromMinMax(762f, 572f, 898f, 693f),
                MapVariantBuilder.FromMinMax(20f, 717f, 298f, 1055f),
                MapVariantBuilder.FromMinMax(1562f, 317f, 1648f, 600f),
                MapVariantBuilder.FromMinMax(2157f, 317f, 2298f, 693f),
                MapVariantBuilder.FromMinMax(2312f, 317f, 2385f, 693f)
            };
            foreach (Rect block in blocks)
                b.CityBlock(block, frontage, infill);

            float[] xs = { 1555f, 1805f, 2055f, 2305f, 2395f };
            float[] zs = { HighwayZ, 815f, 925f, 1035f, 1135f };
            for (int i = 0; i + 1 < xs.Length; i++)
            for (int j = 0; j + 1 < zs.Length; j++)
            {
                float xMax = i + 2 == xs.Length ? 2385f : xs[i + 1] - 7f;
                b.CityBlock(MapVariantBuilder.FromMinMax(xs[i] + 7f, zs[j] + 12f, xMax, zs[j + 1] - 7f), frontage, infill, palmChance: 0.5f);
            }

            (Vector2 position, string prefab)[] landmarks =
            {
                (new Vector2(1680f, 870f), MapVariantKits.Landmarks[0]),
                (new Vector2(1930f, 980f), MapVariantKits.Landmarks[1]),
                (new Vector2(2180f, 870f), MapVariantKits.Minarets[0]),
                (new Vector2(1930f, 760f), MapVariantKits.Minarets[0]),
                (new Vector2(530f, 440f), MapVariantKits.Landmarks[0]),
                (new Vector2(160f, 880f), MapVariantKits.Minarets[0]),
                (new Vector2(1060f, 220f), MapVariantKits.Landmarks[1]),
                (new Vector2(1680f, 220f), MapVariantKits.Minarets[0])
            };
            foreach ((Vector2 position, string prefab) in landmarks)
                for (int attempt = 0; attempt < 30; attempt++)
                    if (b.Place(prefab, MapVariantLayer.City, position + new Vector2(b.Range(-18f, 18f), b.Range(-12f, 12f)), 180f,
                            PlaceOptions.Structure.WithPadding(3f)) != null)
                        break;
        }

        private static void BuildWilds(MapVariantBuilder b)
        {
            Rect westShore = MapVariantBuilder.FromMinMax(20f, 1060f, 180f, 1170f);
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, westShore, 80, PlaceOptions.Vegetation.WithGroundDelta(1.4f), minScale: 0.9f, maxScale: 1.25f);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, westShore, 40, PlaceOptions.Vegetation.WithGroundDelta(2f), minScale: 0.8f, maxScale: 2f);

            Rect coast = MapVariantBuilder.FromMinMax(910f, 1105f, 1330f, 1230f);
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, coast, 160, PlaceOptions.Vegetation.WithGroundDelta(1.6f), minScale: 0.9f, maxScale: 1.3f);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, coast, 90, PlaceOptions.Vegetation.WithGroundDelta(2.5f), minScale: 1f, maxScale: 2.6f);

            Rect foothills = MapVariantBuilder.FromMinMax(1100f, 1145f, 2390f, 1300f);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, foothills, 520, PlaceOptions.Vegetation.WithGroundDelta(4f), minScale: 1.5f, maxScale: 4.5f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, foothills, 360, PlaceOptions.Vegetation.WithGroundDelta(3f));

            Rect south = MapVariantBuilder.FromMinMax(20f, 20f, 2380f, 133f);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, south, 260, PlaceOptions.Vegetation.WithGroundDelta(2f), minScale: 1f, maxScale: 2.6f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, south, 360, PlaceOptions.Vegetation.WithGroundDelta(1.5f));
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, south, 120, PlaceOptions.Vegetation.WithGroundDelta(1.5f));
        }

        private static void BuildBackdrop(MapVariantBuilder b)
        {
            b.Place(MapVariantKits.SmokeStackBackground, MapVariantLayer.Backdrop, new Vector2(1420f, 1240f), 0f, PlaceOptions.Scenery.WithSink(0.2f));
            for (float x = 1100f; x < 2440f; x += 150f)
                b.Place(b.Pick(MapVariantKits.BackgroundRidges), MapVariantLayer.Backdrop, new Vector2(x + b.Range(-30f, 30f), 1440f + b.Range(-25f, 25f)),
                    b.Range(-8f, 8f), PlaceOptions.Scenery.WithScale(b.Range(3.8f, 5.4f)).WithSink(0f));
            b.MountainBackdrop(MapVariantBuilder.FromMinMax(1150f, 1310f, 2360f, 1400f), 24, 2.4f, 3.8f);
            b.MountainBackdrop(MapVariantBuilder.FromMinMax(2260f, 320f, 2390f, 1100f), 8, 1.4f, 2.4f);
        }

        private static void AddZones(MapVariantBuilder b)
        {
            b.AddZone("Frontier_PortSector", "CH01 M03 / CH02 M05 / CH02 M02", new Vector2(540f, 880f), new Vector2(720f, 600f),
                "Canal port: harbour hub, supply yard and the highway bridge chokepoint.");
            b.AddZone("Frontier_RefinerySector", "CH02 M02 / CH02 M04 / CH04 M03", new Vector2(1210f, 700f), new Vector2(620f, 800f),
                "Refinery valley: fenced plant, pump field, substation and the launcher ridge.");
            b.AddZone("Frontier_AirfieldSector", "CH01 M04 / CH04 M01", new Vector2(1900f, 520f), new Vector2(600f, 420f),
                "City-edge airfield: runway, apron, helipads, field hospital and crash field.");
            b.AddZone("Anchor_CanalBridge", "all", new Vector2(540f, HighwayZ), new Vector2(40f, 10f), "Highway bridge over the canal.");
            b.AddZone("Anchor_PortWatchtower", "port", new Vector2(PortHub.xMax - 9f, PortHub.yMin + 9f), new Vector2(12f, 12f), "Port hub watchtower.");
            b.AddZone("Anchor_LauncherRidge", "refinery", LauncherCompound.center, LauncherCompound.size, "Launcher compound on the ridge.");
            b.AddZone("Anchor_FriendlyDepot", "refinery", Depot.center, Depot.size, "Forward depot.");
            b.AddZone("Anchor_Helipads", "airfield", new Vector2(2010f, 640f), new Vector2(70f, 30f), "Twin helipads.");
            b.AddZone("Anchor_ControlTower", "airfield", new Vector2(Apron.xMin + 200f, Apron.yMax - 12f), new Vector2(12f, 12f), "Control tower.");
            b.AddZone("Anchor_EnemyWest", "all", new Vector2(190f, HighwayZ), new Vector2(20f, 20f), "Western highway entry.");
            b.AddZone("Anchor_EnemyEast", "all", new Vector2(2210f, HighwayZ), new Vector2(20f, 20f), "Eastern highway entry.");
        }
    }
}
