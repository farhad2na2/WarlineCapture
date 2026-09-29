using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    // Ash Line Logistics Port: Route Reopened, Supply Yard and Convoy Approach share a harbour hub.
    // A canal splits the port; the two bridges on the arteries are the chokepoints between the banks.
    internal static class MapVariantLogisticsPort
    {
        public const string MapId = "AshLinePort";
        public const int Seed = 20502;

        private static readonly Rect WorldRect = new(0f, 0f, 1400f, 1200f);
        private static readonly Rect PlayableRect = new(400f, 300f, 600f, 400f);

        private const float WaterLevel = -0.8f;
        private const float SeaFloor = -4f;
        private const float QuayEdgeZ = 740f;
        private const float CanalWest = 660f;
        private const float CanalEast = 700f;
        private const float CanalSouth = 260f;
        private const float MainRoadZ = 505f;
        private const float SecondRoadZ = 385f;
        private const float QuayRoadZ = 695f;

        private static readonly Rect Quay = MapVariantBuilder.FromMinMax(420f, 700f, 980f, QuayEdgeZ);
        private static readonly Rect HubBlock = MapVariantBuilder.FromMinMax(462f, 514f, 598f, 689f);
        private static readonly Rect WestQuayBlock = MapVariantBuilder.FromMinMax(612f, 514f, 654f, 689f);
        private static readonly Rect EastQuayBlock = MapVariantBuilder.FromMinMax(706f, 514f, 748f, 689f);
        private static readonly Rect SupplyYard = MapVariantBuilder.FromMinMax(762f, 514f, 938f, 689f);
        private static readonly Rect CheckpointBlock = MapVariantBuilder.FromMinMax(952f, 514f, 1048f, 689f);
        private static readonly Rect CustomsYard = MapVariantBuilder.FromMinMax(462f, 394f, 598f, 496f);
        private static readonly Rect WestCanalSouth = MapVariantBuilder.FromMinMax(612f, 394f, 654f, 496f);
        private static readonly Rect EastCanalSouth = MapVariantBuilder.FromMinMax(706f, 394f, 748f, 496f);
        private static readonly Rect EastWarehouses = MapVariantBuilder.FromMinMax(762f, 394f, 938f, 496f);
        private static readonly Rect EastGate = MapVariantBuilder.FromMinMax(952f, 394f, 1048f, 496f);

        public static MapVariantBuilder Build()
        {
            var b = new MapVariantBuilder(MapId, WorldRect, PlayableRect, 5f, Seed) { WaterLevel = WaterLevel };
            ShapeTerrain(b);
            LayRoads(b);
            b.FlattenUnderRoads(10f, (x, z, h) => h < WaterLevel);
            b.BuildGround();
            b.BuildWater(MapVariantBuilder.FromMinMax(-200f, QuayEdgeZ - 20f, 1600f, 1400f), WaterLevel, MapVariantKits.WaterMaterial);
            b.BuildWater(MapVariantBuilder.FromMinMax(CanalWest - 2f, CanalSouth - 2f, CanalEast + 2f, QuayEdgeZ), WaterLevel, MapVariantKits.WaterMaterial);
            b.BuildRoads();
            LayPads(b);
            BuildBackdrop(b);

            BuildQuay(b);
            BuildCanalBanks(b);
            BuildHub(b);
            BuildSupplyYard(b);
            BuildCustomsAndWarehouses(b);
            BuildCheckpoint(b);
            BuildBeaches(b);
            BuildRoadside(b);
            BuildCity(b);
            b.DesertDressing(WorldRect);
            AddZones(b);

            b.BuildLighting();
            return b;
        }

        public static IEnumerable<MapVariantView> Views(MapVariantBuilder b)
        {
            yield return b.TopDown("01_topdown_playable", PlayableRect);
            yield return b.TopDown("02_topdown_world", WorldRect);
            yield return MapVariantView.Battle("03_battle_route_reopened_hub", new Vector2(535f, 600f), 0f);
            yield return MapVariantView.Battle("04_battle_convoy_bridge", new Vector2(680f, 520f), 0f);
            yield return MapVariantView.Battle("05_battle_supply_yard", new Vector2(850f, 600f), 0f);
            yield return MapVariantView.Battle("06_battle_quay_piers", new Vector2(620f, 720f), 0f);
            yield return MapVariantView.Battle("07_battle_customs_south", new Vector2(560f, 440f), 0f);
            yield return new MapVariantView("08_hero_harbour", new Vector3(430f, 70f, 390f), new Vector3(18f, 40f, 0f), 50f);
            yield return new MapVariantView("09_hero_canal", new Vector3(680f, 28f, 330f), new Vector3(10f, 0f, 0f), 55f);
            yield return new MapVariantView("10_ground_bridge", new Vector3(630f, 4f, 507f), new Vector3(4f, 90f, 0f), 55f);
        }

        private static void ShapeTerrain(MapVariantBuilder b)
        {
            b.Height.Apply((x, z, _) =>
            {
                float n = Mathf.PerlinNoise(x * 0.012f + 2.3f, z * 0.012f + 9.1f);
                float h = 0f;

                bool quayFront = x >= Quay.xMin && x <= Quay.xMax;
                if (quayFront)
                {
                    if (z > QuayEdgeZ)
                        h = SeaFloor;
                }
                else
                {
                    float beach = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(705f, 800f, z));
                    h = Mathf.Lerp(0.6f + n * 1.5f, SeaFloor, beach);
                }

                if (x > CanalWest && x < CanalEast && z > CanalSouth && z <= QuayEdgeZ + 5f)
                    h = SeaFloor;

                float farShore = Mathf.Clamp01((z - 1040f) / 120f);
                h = Mathf.Lerp(h, 6f + n * 55f, farShore * farShore);

                h += Mathf.Clamp01((180f - x) / 160f) * n * 5f;
                h += Mathf.Clamp01((x - 1230f) / 150f) * n * 5f;
                return h;
            });
        }

        private static void LayRoads(MapVariantBuilder b)
        {
            foreach (float z in new[] { MainRoadZ, SecondRoadZ })
            {
                b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(5f, z), new Vector2(655f, z));
                b.AddBridge(MapVariantRoadKind.Asphalt, 0f, new Vector2(665f, z), new Vector2(695f, z));
                b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(705f, z), new Vector2(1395f, z));
            }

            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(425f, QuayRoadZ), new Vector2(655f, QuayRoadZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(705f, QuayRoadZ), new Vector2(975f, QuayRoadZ));
            foreach (float x in new[] { 455f, 605f, 755f, 945f })
                b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(x, x == 455f || x == 945f ? 145f : SecondRoadZ), new Vector2(x, QuayRoadZ));

            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(5f, 245f), new Vector2(1395f, 245f));
            foreach (float x in new[] { 305f, 555f, 805f, 1055f, 1205f })
                b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(x, 145f), new Vector2(x, SecondRoadZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1055f, SecondRoadZ), new Vector2(1055f, 685f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(305f, SecondRoadZ), new Vector2(305f, 655f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1055f, 605f), new Vector2(1305f, 605f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(95f, 605f), new Vector2(305f, 605f));
        }

        private static void LayPads(MapVariantBuilder b)
        {
            b.Pad(MapVariantBuilder.FromMinMax(Quay.xMin, Quay.yMin, CanalWest, Quay.yMax), MapVariantKits.ConcretePad);
            b.Pad(MapVariantBuilder.FromMinMax(CanalEast, Quay.yMin, Quay.xMax, Quay.yMax), MapVariantKits.ConcretePad);
            b.Pad(HubBlock, MapVariantKits.AsphaltPad);
            b.Pad(SupplyYard, MapVariantKits.ConcretePad);
            b.Pad(CustomsYard, MapVariantKits.AsphaltPad);
            b.Pad(EastWarehouses, MapVariantKits.ConcretePad);
            foreach (Rect bank in new[] { WestQuayBlock, EastQuayBlock, WestCanalSouth, EastCanalSouth })
                b.Pad(bank, MapVariantKits.ConcretePad);
        }

        private static void BuildQuay(MapVariantBuilder b)
        {
            var wall = PlaceOptions.Prop.WithPadding(0f);
            b.PlaceLine(MapVariantKits.PortWall, MapVariantLayer.Industrial, new Vector2(Quay.xMin, QuayEdgeZ), new Vector2(CanalWest, QuayEdgeZ), wall);
            b.PlaceLine(MapVariantKits.PortWall, MapVariantLayer.Industrial, new Vector2(CanalEast, QuayEdgeZ), new Vector2(Quay.xMax, QuayEdgeZ), wall);

            foreach (float x in new[] { 470f, 560f, 760f, 880f })
            {
                Rect pier = MapVariantBuilder.FromMinMax(x - 2.3f, QuayEdgeZ + 0.5f, x + 2.3f, QuayEdgeZ + 3.2f + 9.5f * 5.1f + 2.6f);
                if (!b.IsFree(pier))
                    continue;
                for (int i = 0; i < 10; i++)
                    b.PlaceAt(MapVariantKits.Pier, MapVariantLayer.Industrial, new Vector2(x, QuayEdgeZ + 3.2f + i * 5.1f), 0f, 0f, false,
                        reserve: false);
                b.Occupy(pier);

                for (int i = 0; i < 3; i++)
                {
                    b.Afloat(MapVariantKits.Boat, new Vector2(x - 5.2f, QuayEdgeZ + 10f + i * 14f), b.Range(-4f, 4f), 0.35f);
                    b.Afloat(MapVariantKits.Boat, new Vector2(x + 5.2f, QuayEdgeZ + 12f + i * 14f), 180f + b.Range(-4f, 4f), 0.35f);
                }
            }

            for (int i = 0; i < 18; i++)
                b.Afloat(MapVariantKits.Boat, new Vector2(b.Range(380f, 1020f), b.Range(820f, 960f)), b.Range(0f, 360f), 0.35f);

            b.Row(new[] { MapVariantKits.BoatTie }, MapVariantLayer.Props, new Vector2(Quay.xMin + 2f, QuayEdgeZ - 1f),
                new Vector2(CanalWest - 2f, QuayEdgeZ - 1f), 6f, PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            b.Row(new[] { MapVariantKits.BoatTie }, MapVariantLayer.Props, new Vector2(CanalEast + 2f, QuayEdgeZ - 1f),
                new Vector2(Quay.xMax - 2f, QuayEdgeZ - 1f), 6f, PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            b.Row(new[] { MapVariantKits.Spotlight }, MapVariantLayer.Props, new Vector2(Quay.xMin + 10f, QuayEdgeZ - 6f),
                new Vector2(Quay.xMax - 10f, QuayEdgeZ - 6f), 38f, PlaceOptions.Prop, freeYaw: false, fixedYaw: 180f);

            string[] stacks = MapVariantKits.ContainerStacks;
            for (float x = Quay.xMin + 8f; x < Quay.xMax - 8f; x += 7.5f)
            for (float z = Quay.yMin + 6f; z < QuayEdgeZ - 12f; z += 3.2f)
            {
                if (Mathf.Abs(x - 605f) < 10f || Mathf.Abs(x - 755f) < 10f || Mathf.Abs(x - 470f) < 6f || Mathf.Abs(x - 880f) < 6f ||
                    b.Chance(0.18f))
                    continue;
                b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
            }

            b.Scatter(MapVariantKits.PortCargo, MapVariantLayer.Props, Quay, 260, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
        }

        private static void BuildCanalBanks(MapVariantBuilder b)
        {
            var wall = PlaceOptions.Prop.WithPadding(0f);
            foreach ((float from, float to) in new[] { (CanalSouth, SecondRoadZ - 6f), (SecondRoadZ + 6f, MainRoadZ - 6f), (MainRoadZ + 6f, QuayEdgeZ) })
            {
                b.PlaceLine(MapVariantKits.PortWall, MapVariantLayer.Industrial, new Vector2(CanalWest, from), new Vector2(CanalWest, to), wall);
                b.PlaceLine(MapVariantKits.PortWallAlt, MapVariantLayer.Industrial, new Vector2(CanalEast, from), new Vector2(CanalEast, to), wall);
            }

            b.PlaceLine(MapVariantKits.PortWall, MapVariantLayer.Industrial, new Vector2(CanalWest, CanalSouth), new Vector2(CanalEast, CanalSouth), wall);

            for (float z = CanalSouth + 20f; z < QuayEdgeZ - 10f; z += 22f)
            {
                if (Mathf.Abs(z - MainRoadZ) < 16f || Mathf.Abs(z - SecondRoadZ) < 16f)
                    continue;
                b.Afloat(MapVariantKits.Boat, new Vector2(CanalWest + 5f, z + b.Range(-3f, 3f)), b.Range(-3f, 3f), 0.35f);
                if (b.Chance(0.6f))
                    b.Afloat(MapVariantKits.Boat, new Vector2(CanalEast - 5f, z + 11f), 180f + b.Range(-3f, 3f), 0.35f);
            }

            foreach (Rect bank in new[] { WestQuayBlock, EastQuayBlock, WestCanalSouth, EastCanalSouth })
            {
                b.Scatter(MapVariantKits.PortCargo, MapVariantLayer.Props, bank, 70, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
                b.Scatter(MapVariantKits.Containers, MapVariantLayer.Props, bank, 16, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
                b.Scatter(MapVariantKits.Trucks, MapVariantLayer.Vehicles, bank, 6, PlaceOptions.Prop.WithPadding(0.6f), freeYaw: false);
                b.Row(new[] { MapVariantKits.BoatTie }, MapVariantLayer.Props,
                    new Vector2(bank.center.x < CanalWest ? bank.xMax - 1f : bank.xMin + 1f, bank.yMin + 2f),
                    new Vector2(bank.center.x < CanalWest ? bank.xMax - 1f : bank.xMin + 1f, bank.yMax - 2f), 8f,
                    PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            }
        }

        private static void BuildHub(MapVariantBuilder b)
        {
            b.Row(new[] { MapVariantKits.PortWarehouse, MapVariantKits.Warehouse }, MapVariantLayer.Industrial,
                new Vector2(HubBlock.xMin + 12f, HubBlock.yMax - 13f), new Vector2(HubBlock.xMax - 12f, HubBlock.yMax - 13f), 24.5f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false, fixedYaw: 0f);
            b.Place(MapVariantKits.ControlTower, MapVariantLayer.Industrial, new Vector2(HubBlock.xMax - 9f, HubBlock.yMin + 9f), 180f,
                PlaceOptions.Structure.WithPadding(1f));
            b.Row(new[] { MapVariantKits.Tanker }, MapVariantLayer.Vehicles, new Vector2(HubBlock.xMin + 4f, HubBlock.yMax - 33f),
                new Vector2(HubBlock.xMax - 4f, HubBlock.yMax - 33f), 4.4f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false, fixedYaw: 0f);

            string[] stacks = MapVariantKits.ContainerStacks;
            for (float x = HubBlock.xMin + 6f; x < HubBlock.xMax - 20f; x += 7.5f)
            for (float z = HubBlock.yMin + 6f; z < HubBlock.yMax - 44f; z += 3.2f)
            {
                if (Mathf.Abs(z - (HubBlock.yMin + 60f)) < 5f || b.Chance(0.12f))
                    continue;
                b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
            }

            b.Scatter(MapVariantKits.PortCargo, MapVariantLayer.Props, HubBlock, 160, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
            b.Row(new[] { MapVariantKits.LightPole }, MapVariantLayer.Props, new Vector2(HubBlock.xMin + 2f, HubBlock.yMin + 60f),
                new Vector2(HubBlock.xMax - 2f, HubBlock.yMin + 60f), 20f, PlaceOptions.Prop);
        }

        private static void BuildSupplyYard(MapVariantBuilder b)
        {
            b.FencedCompound(SupplyYard, MapVariantKits.Fence,
                new MapVariantGate(MapVariantSide.South, 88f, 18f),
                new MapVariantGate(MapVariantSide.North, 88f, 18f),
                new MapVariantGate(MapVariantSide.West, 90f, 12f));
            b.Row(new[] { MapVariantKits.GasSphereTank }, MapVariantLayer.Industrial, new Vector2(SupplyYard.xMin + 12f, SupplyYard.yMax - 14f),
                new Vector2(SupplyYard.xMin + 80f, SupplyYard.yMax - 14f), 17f, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Row(new[] { MapVariantKits.StorageTank }, MapVariantLayer.Industrial, new Vector2(SupplyYard.xMin + 100f, SupplyYard.yMax - 12f),
                new Vector2(SupplyYard.xMax - 10f, SupplyYard.yMax - 12f), 15f, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(SupplyYard.xMin + 6f, SupplyYard.yMax - 32f),
                new Vector2(SupplyYard.xMax - 6f, SupplyYard.yMax - 32f), PlaceOptions.Structure.WithPadding(0.2f).WithBoundsAnchor());
            b.Grid(MapVariantKits.FuelBladder, MapVariantLayer.Industrial,
                MapVariantBuilder.FromMinMax(SupplyYard.xMin + 6f, SupplyYard.yMin + 8f, SupplyYard.xMin + 78f, SupplyYard.yMin + 70f), 5, 3, 0f,
                PlaceOptions.Prop.WithPadding(0.8f));
            b.Row(new[] { MapVariantKits.Tanker }, MapVariantLayer.Vehicles, new Vector2(SupplyYard.xMin + 98f, SupplyYard.yMin + 40f),
                new Vector2(SupplyYard.xMax - 6f, SupplyYard.yMin + 40f), 4.4f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(new[] { MapVariantKits.Tanker }, MapVariantLayer.Vehicles, new Vector2(SupplyYard.xMin + 98f, SupplyYard.yMin + 20f),
                new Vector2(SupplyYard.xMax - 6f, SupplyYard.yMin + 20f), 4.4f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Scatter(MapVariantKits.YardCargo, MapVariantLayer.Props, SupplyYard, 180, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
        }

        private static void BuildCustomsAndWarehouses(MapVariantBuilder b)
        {
            b.Row(MapVariantKits.Trucks, MapVariantLayer.Vehicles, new Vector2(CustomsYard.xMin + 4f, CustomsYard.yMax - 10f),
                new Vector2(CustomsYard.xMax - 4f, CustomsYard.yMax - 10f), 4.4f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(MapVariantKits.Trucks, MapVariantLayer.Vehicles, new Vector2(CustomsYard.xMin + 4f, CustomsYard.yMax - 24f),
                new Vector2(CustomsYard.xMax - 4f, CustomsYard.yMax - 24f), 4.4f, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(new[] { MapVariantKits.Shed, MapVariantKits.Barracks }, MapVariantLayer.Industrial, new Vector2(CustomsYard.xMin + 10f, CustomsYard.yMin + 14f),
                new Vector2(CustomsYard.xMax - 10f, CustomsYard.yMin + 14f), 16f, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false, fixedYaw: 180f);
            b.Scatter(MapVariantKits.PortCargo, MapVariantLayer.Props, CustomsYard, 120, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);

            b.Row(new[] { MapVariantKits.PortWarehouse, MapVariantKits.Warehouse }, MapVariantLayer.Industrial,
                new Vector2(EastWarehouses.xMin + 12f, EastWarehouses.yMin + 14f), new Vector2(EastWarehouses.xMax - 12f, EastWarehouses.yMin + 14f),
                24.5f, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false, fixedYaw: 180f);
            string[] stacks = MapVariantKits.ContainerStacks;
            for (float x = EastWarehouses.xMin + 6f; x < EastWarehouses.xMax - 6f; x += 7.5f)
            for (float z = EastWarehouses.yMin + 32f; z < EastWarehouses.yMax - 6f; z += 3.2f)
            {
                if (Mathf.Abs(x - 850f) < 8f || b.Chance(0.2f))
                    continue;
                b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
            }
        }

        private static void BuildCheckpoint(MapVariantBuilder b)
        {
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(990f, 492f), 0f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(990f, 518f), 180f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Row(MapVariantKits.Checkpoint, MapVariantLayer.Props, new Vector2(1002f, 497f), new Vector2(1040f, 497f), 2.4f,
                PlaceOptions.Prop.WithPadding(0.1f), freeYaw: false);
            b.Row(MapVariantKits.Checkpoint, MapVariantLayer.Props, new Vector2(1002f, 513f), new Vector2(1040f, 513f), 2.4f,
                PlaceOptions.Prop.WithPadding(0.1f), freeYaw: false);
            foreach (Rect block in new[] { CheckpointBlock, EastGate })
            {
                b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(block.xMin + 8f, block.center.y),
                    new Vector2(block.xMax - 8f, block.center.y), 13f, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
                b.Scatter(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, block, 90, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
                b.Scatter(MapVariantKits.ArmorParked, MapVariantLayer.Vehicles, block, 8, PlaceOptions.Prop.WithPadding(0.6f), freeYaw: false);
            }
        }

        private static void BuildBeaches(MapVariantBuilder b)
        {
            foreach (Rect beach in new[] { MapVariantBuilder.FromMinMax(20f, 620f, 415f, 760f), MapVariantBuilder.FromMinMax(985f, 620f, 1390f, 760f) })
            {
                b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, beach, 160, PlaceOptions.Vegetation.WithGroundDelta(1.4f), minScale: 0.9f, maxScale: 1.25f);
                b.Scatter(MapVariantKits.SmallPalms, MapVariantLayer.Vegetation, beach, 90, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
                b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, beach, 90, PlaceOptions.Vegetation.WithGroundDelta(2f), minScale: 0.8f, maxScale: 2.2f);
                b.Scatter(MapVariantKits.Houses, MapVariantLayer.City, beach, 25, PlaceOptions.Structure.WithPadding(3f), freeYaw: false);
            }

            for (int i = 0; i < 16; i++)
                b.Afloat(MapVariantKits.Boat, new Vector2(b.Range(40f, 400f), b.Range(790f, 900f)), b.Range(0f, 360f), 0.35f);
            for (int i = 0; i < 16; i++)
                b.Afloat(MapVariantKits.Boat, new Vector2(b.Range(1000f, 1360f), b.Range(790f, 900f)), b.Range(0f, 360f), 0.35f);
        }

        private static void BuildRoadside(MapVariantBuilder b)
        {
            var palmRow = PlaceOptions.Vegetation.WithPadding(0.2f);
            foreach (float z in new[] { MainRoadZ, SecondRoadZ })
            {
                b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(20f, z - 8.5f), new Vector2(455f, z - 8.5f), 13f, palmRow, jitter: 0.3f);
                b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(20f, z + 8.5f), new Vector2(455f, z + 8.5f), 13f, palmRow, jitter: 0.3f);
                b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(1050f, z - 8.5f), new Vector2(1390f, z - 8.5f), 13f, palmRow, jitter: 0.3f);
                b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(1050f, z + 8.5f), new Vector2(1390f, z + 8.5f), 13f, palmRow, jitter: 0.3f);
                b.Row(new[] { MapVariantKits.StreetLight }, MapVariantLayer.Props, new Vector2(460f, z - 6f), new Vector2(1045f, z - 6f), 28f,
                    PlaceOptions.Prop, freeYaw: false, fixedYaw: 0f);
            }

            b.Row(MapVariantKits.Checkpoint, MapVariantLayer.Props, new Vector2(640f, MainRoadZ - 6.5f), new Vector2(654f, MainRoadZ - 6.5f), 2.2f,
                PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            b.Row(MapVariantKits.Checkpoint, MapVariantLayer.Props, new Vector2(706f, MainRoadZ + 6.5f), new Vector2(720f, MainRoadZ + 6.5f), 2.2f,
                PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            b.Scatter(MapVariantKits.CivilianVehicles, MapVariantLayer.Vehicles, MapVariantBuilder.FromMinMax(20f, 150f, 1380f, 380f), 70,
                PlaceOptions.Prop.WithPadding(0.8f), freeYaw: false, accept: p => b.DistanceToRoad(p) < 4f);
        }

        private static void BuildCity(MapVariantBuilder b)
        {
            var frontage = new List<string>(MapVariantKits.Houses);
            frontage.AddRange(MapVariantKits.Shops);
            var infill = new List<string>(MapVariantKits.Houses);

            float[] rows = { 150f, 250f };
            float[] rowTops = { 240f, 380f };
            float[] columns = { 20f, 310f, 560f, 810f, 1060f, 1210f, 1380f };
            for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c + 1 < columns.Length; c++)
            {
                float xMin = c == 0 ? columns[c] : columns[c] + 2f;
                float xMax = columns[c + 1] - (c + 1 == columns.Length - 1 ? 0f : 12f);
                b.CityBlock(MapVariantBuilder.FromMinMax(xMin, rows[r], xMax, rowTops[r]), frontage, infill);
            }

            Rect[] sideBlocks =
            {
                MapVariantBuilder.FromMinMax(20f, 394f, 298f, 496f),
                MapVariantBuilder.FromMinMax(312f, 394f, 448f, 496f),
                MapVariantBuilder.FromMinMax(20f, 514f, 298f, 598f),
                MapVariantBuilder.FromMinMax(312f, 514f, 448f, 620f),
                MapVariantBuilder.FromMinMax(1062f, 394f, 1200f, 496f),
                MapVariantBuilder.FromMinMax(1212f, 394f, 1390f, 496f),
                MapVariantBuilder.FromMinMax(1062f, 514f, 1390f, 598f),
                MapVariantBuilder.FromMinMax(1062f, 612f, 1300f, 680f)
            };
            foreach (Rect block in sideBlocks)
                b.CityBlock(block, frontage, infill);

            (Vector2 position, string prefab)[] landmarks =
            {
                (new Vector2(200f, 440f), MapVariantKits.Landmarks[0]),
                (new Vector2(1130f, 440f), MapVariantKits.Landmarks[1]),
                (new Vector2(380f, 560f), MapVariantKits.Minarets[0]),
                (new Vector2(1150f, 640f), MapVariantKits.Minarets[0]),
                (new Vector2(680f, 200f), MapVariantKits.Landmarks[0]),
                (new Vector2(930f, 320f), MapVariantKits.Minarets[0])
            };
            foreach ((Vector2 position, string prefab) in landmarks)
            {
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    if (b.Place(prefab, MapVariantLayer.City, position + new Vector2(b.Range(-18f, 18f), b.Range(-12f, 12f)), 180f,
                            PlaceOptions.Structure.WithPadding(3f)) != null)
                        break;
                }
            }
        }

        private static void BuildBackdrop(MapVariantBuilder b)
        {
            for (float x = -40f; x < 1440f; x += 140f)
            {
                b.Place(b.Pick(MapVariantKits.BackgroundRidges), MapVariantLayer.Backdrop, new Vector2(x + b.Range(-30f, 30f), 1150f + b.Range(-20f, 20f)),
                    b.Range(-8f, 8f), PlaceOptions.Scenery.WithScale(b.Range(3.8f, 5.4f)).WithSink(0f));
            }

            b.MountainBackdrop(MapVariantBuilder.FromMinMax(40f, 1080f, 1360f, 1150f), 18, 2.4f, 3.6f);
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(20f, 1040f, 1380f, 1100f), 160,
                PlaceOptions.Vegetation.WithGroundDelta(3f), minScale: 1f, maxScale: 1.4f);
        }

        private static void AddZones(MapVariantBuilder b)
        {
            b.AddZone("RouteReopened_Hub", "CH02 M05 Route Reopened", HubBlock.center, HubBlock.size + new Vector2(40f, 40f),
                "Capture the Ash Line hub (warehouses + watchtower) and reopen the disrupted lane across the canal.");
            b.AddZone("SupplyYard_East", "CH02 M02 Supply Yard", SupplyYard.center, SupplyYard.size + new Vector2(60f, 40f),
                "Fuel storage and tanker lines on the east bank; defend relief stock.");
            b.AddZone("ConvoyApproach_Bridge", "CH01 M03 Convoy Approach", new Vector2(680f, MainRoadZ), new Vector2(560f, 200f),
                "Convoy enters from the west along the main artery and must hold the canal bridge.");
            b.AddZone("Anchor_MainBridge", "all", new Vector2(680f, MainRoadZ), new Vector2(40f, 10f), "Main artery canal bridge.");
            b.AddZone("Anchor_SouthBridge", "all", new Vector2(680f, SecondRoadZ), new Vector2(40f, 10f), "Second artery canal bridge.");
            b.AddZone("Anchor_Watchtower", "Route Reopened", new Vector2(HubBlock.xMax - 9f, HubBlock.yMin + 9f), new Vector2(12f, 12f), "Hub watchtower.");
            b.AddZone("Anchor_EastCheckpoint", "all", new Vector2(1000f, MainRoadZ), new Vector2(40f, 30f), "Friendly checkpoint gate.");
            b.AddZone("Anchor_EnemyWest", "all", new Vector2(420f, MainRoadZ), new Vector2(20f, 20f), "Western approach.");
        }
    }
}
