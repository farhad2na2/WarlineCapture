using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    // Refinery District: Supply Line, Power Relay and Split Front share one refinery valley at the city edge.
    // Enemy pressure comes from the western desert; the friendly depot and launcher ridge hold the east.
    internal static class MapVariantRefineryDistrict
    {
        public const string MapId = "RefineryDistrict";
        public const int Seed = 40302;

        private static readonly Rect WorldRect = new(0f, 0f, 1400f, 1200f);
        private static readonly Rect PlayableRect = new(400f, 300f, 600f, 400f);

        private const float MainRoadZ = 505f;
        private const float NorthLaneZ = 625f;
        private const float SouthLaneZ = 385f;
        private const float WestJunctionX = 555f;
        private const float EastJunctionX = 875f;

        private static readonly Rect Ridge = MapVariantBuilder.FromMinMax(900f, 650f, 1080f, 800f);
        private const float RidgeHeight = 9f;
        private const float RidgeSlope = 30f;

        private static readonly Rect RefinerySite = MapVariantBuilder.FromMinMax(566f, 516f, 864f, 614f);
        private static readonly Rect LogisticsYard = MapVariantBuilder.FromMinMax(566f, 396f, 752f, 494f);
        private static readonly Rect SubstationSite = MapVariantBuilder.FromMinMax(764f, 400f, 862f, 492f);
        private static readonly Rect DepotSite = MapVariantBuilder.FromMinMax(888f, 398f, 994f, 494f);
        private static readonly Rect LauncherCompound = MapVariantBuilder.FromMinMax(910f, 658f, 1000f, 700f);
        private static readonly Rect PumpField = MapVariantBuilder.FromMinMax(410f, 522f, 546f, 692f);
        private static readonly Rect WestHamlet = MapVariantBuilder.FromMinMax(406f, 396f, 546f, 494f);

        public static MapVariantBuilder Build()
        {
            var b = new MapVariantBuilder(MapId, WorldRect, PlayableRect, 5f, Seed);
            ShapeTerrain(b);
            LayRoads(b);
            b.FlattenUnderRoads(12f);
            b.BuildGround();
            b.BuildRoads();
            LayPads(b);
            BuildBackdrop(b);

            BuildRefinery(b);
            BuildLogisticsYard(b);
            BuildSubstation(b);
            BuildPumpField(b);
            BuildForwardDepot(b);
            BuildLauncherRidge(b);
            BuildWestHamlet(b);
            BuildRoadside(b);
            BuildCity(b);
            BuildDesertFringe(b);
            b.DesertDressing(WorldRect);
            AddZones(b);

            b.BuildLighting();
            return b;
        }

        public static IEnumerable<MapVariantView> Views(MapVariantBuilder b)
        {
            yield return b.TopDown("01_topdown_playable", PlayableRect);
            yield return b.TopDown("02_topdown_world", WorldRect);
            yield return MapVariantView.Battle("03_battle_supply_line", new Vector2(680f, 470f), 0f);
            yield return MapVariantView.Battle("04_battle_split_front_fork", new Vector2(590f, 540f), 0f);
            yield return MapVariantView.Battle("05_battle_power_relay", new Vector2(810f, 450f), 0f);
            yield return MapVariantView.Battle("06_battle_launcher_ridge", new Vector2(940f, 650f), RidgeHeight * 0.5f);
            yield return MapVariantView.Battle("07_battle_forward_depot", new Vector2(930f, 450f), 0f);
            yield return MapVariantView.Battle("08_battle_refinery_core", new Vector2(760f, 575f), 0f, 95f);
            yield return new MapVariantView("09_hero_sunset", new Vector3(470f, 60f, 330f), new Vector3(15f, 38f, 0f), 50f);
            yield return new MapVariantView("10_hero_ridge_overlook", new Vector3(985f, 30f, 690f), new Vector3(12f, 250f, 0f), 55f);
            yield return new MapVariantView("11_ground_refinery_road", new Vector3(545f, 5f, 500f), new Vector3(5f, 82f, 0f), 55f);
        }

        private static void ShapeTerrain(MapVariantBuilder b)
        {
            b.Height.Apply((x, z, _) =>
            {
                float n = Mathf.PerlinNoise(x * 0.011f + 3.1f, z * 0.011f + 7.7f);
                float h = 0f;
                h += Mathf.Clamp01((330f - x) / 170f) * (2f + n * 10f);
                h += Mathf.Clamp01((x - 1250f) / 120f) * (1f + n * 6f);

                float northRamp = Mathf.Clamp01((z - 920f) / 220f);
                float ridges = Mathf.PerlinNoise(x * 0.006f + 1.3f, z * 0.02f) * 0.7f +
                               Mathf.PerlinNoise(x * 0.018f, z * 0.018f + 4f) * 0.3f;
                h += northRamp * northRamp * (20f + ridges * 110f);

                if (WestHamlet.Contains(new Vector2(x, z)))
                    h += Mathf.Max(0f, Mathf.PerlinNoise(x * 0.02f + 11f, z * 0.02f + 5f) - 0.55f) * 3f;

                float dx = Mathf.Max(Ridge.xMin - x, 0f, x - Ridge.xMax);
                float dz = Mathf.Max(Ridge.yMin - z, 0f, z - Ridge.yMax);
                float ridge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(dx * dx + dz * dz) / RidgeSlope);
                return Mathf.Max(h, ridge * RidgeHeight);
            });
        }

        private static void LayRoads(MapVariantBuilder b)
        {
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(5f, MainRoadZ), new Vector2(1395f, MainRoadZ));

            b.AddRoad(MapVariantRoadKind.Dirt,
                new Vector2(WestJunctionX, MainRoadZ), new Vector2(WestJunctionX, NorthLaneZ),
                new Vector2(EastJunctionX, NorthLaneZ), new Vector2(EastJunctionX, MainRoadZ));
            b.AddRoad(MapVariantRoadKind.Dirt,
                new Vector2(WestJunctionX, MainRoadZ), new Vector2(WestJunctionX, SouthLaneZ),
                new Vector2(EastJunctionX, SouthLaneZ), new Vector2(EastJunctionX, MainRoadZ));

            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(5f, 345f), new Vector2(1395f, 345f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(5f, 245f), new Vector2(1395f, 245f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(455f, 145f), new Vector2(455f, 345f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(705f, 145f), new Vector2(705f, SouthLaneZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1055f, 145f), new Vector2(1055f, MainRoadZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1205f, 145f), new Vector2(1205f, 845f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1005f, 625f), new Vector2(1395f, 625f));

            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(705f, NorthLaneZ), new Vector2(705f, 845f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(305f, 745f), new Vector2(895f, 745f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(305f, 845f), new Vector2(1205f, 845f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(505f, 745f), new Vector2(505f, 845f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(305f, 745f), new Vector2(305f, 845f));
        }

        private static void LayPads(MapVariantBuilder b)
        {
            b.Pad(RefinerySite, MapVariantKits.ConcretePad);
            b.Pad(LogisticsYard, MapVariantKits.AsphaltPad);
            b.Pad(SubstationSite, MapVariantKits.ConcretePad);
            b.Pad(DepotSite, MapVariantKits.DirtPad);
            b.Pad(MapVariantBuilder.FromMinMax(1012f, 452f, 1048f, 494f), MapVariantKits.ConcretePad);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                var c = new Vector2(
                    Mathf.Lerp(PumpField.xMin, PumpField.xMax, (col + 0.5f) / 4f),
                    Mathf.Lerp(PumpField.yMin, PumpField.yMax, (row + 0.5f) / 4f));
                b.Pad(new Rect(c.x - 7f, c.y - 10f, 14f, 20f), MapVariantKits.DirtPad);
            }
        }

        private static void BuildRefinery(MapVariantBuilder b)
        {
            b.FencedCompound(RefinerySite, MapVariantKits.Fence,
                // A service entrance from the main road reaches the west pump pad.
                new MapVariantGate(MapVariantSide.South, 25f, 20f),
                new MapVariantGate(MapVariantSide.South, 150f, 16f),
                new MapVariantGate(MapVariantSide.West, 48f, 12f),
                new MapVariantGate(MapVariantSide.East, 48f, 12f));

            var racked = PlaceOptions.Structure.WithPadding(0.2f).WithBoundsAnchor();
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(572f, 528f), new Vector2(706f, 528f), racked);
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(726f, 528f), new Vector2(858f, 528f), racked);
            b.PlaceLine(MapVariantKits.PipeRack, MapVariantLayer.Industrial, new Vector2(698f, 540f), new Vector2(698f, 610f), racked);

            b.Row(new[] { MapVariantKits.GasSphereTank }, MapVariantLayer.Industrial, new Vector2(582f, 548f), new Vector2(684f, 548f), 17f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false);
            b.Row(new[] { MapVariantKits.StorageTank }, MapVariantLayer.Industrial, new Vector2(582f, 572f), new Vector2(684f, 572f), 15f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Row(new[] { MapVariantKits.StorageTank }, MapVariantLayer.Industrial, new Vector2(582f, 598f), new Vector2(684f, 598f), 15f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);

            b.Place(MapVariantKits.GasHolder, MapVariantLayer.Industrial, new Vector2(835f, 585f), 0f, PlaceOptions.Structure.WithPadding(1f));
            b.Row(MapVariantKits.StacksOnPlatforms, MapVariantLayer.Industrial, new Vector2(716f, 590f), new Vector2(800f, 590f), 14f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false);
            b.Row(new[] { MapVariantKits.OilTower }, MapVariantLayer.Industrial, new Vector2(718f, 555f), new Vector2(800f, 555f), 16f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false, fixedYaw: 180f);
            b.Scatter(new[] { MapVariantKits.Transformer, MapVariantKits.Generator, MapVariantKits.StorageTank }, MapVariantLayer.Industrial,
                MapVariantBuilder.FromMinMax(706f, 536f, 860f, 612f), 60, PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Scatter(MapVariantKits.YardCargo, MapVariantLayer.Props, RefinerySite, 220, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(new[] { MapVariantKits.LightPole }, MapVariantLayer.Props, new Vector2(572f, 537f), new Vector2(858f, 537f), 22f,
                PlaceOptions.Prop);

            b.Place(MapVariantKits.RoadBarrier, MapVariantLayer.Props, new Vector2(716f, 513f), 0f, PlaceOptions.Prop);
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(703f, 520f), 0f, PlaceOptions.Structure.WithPadding(0.3f));
        }

        private static void BuildLogisticsYard(MapVariantBuilder b)
        {
            b.FencedCompound(LogisticsYard, MapVariantKits.Fence,
                new MapVariantGate(MapVariantSide.North, 60f, 18f),
                new MapVariantGate(MapVariantSide.North, 140f, 18f),
                new MapVariantGate(MapVariantSide.South, 90f, 14f));

            b.Row(new[] { MapVariantKits.Warehouse }, MapVariantLayer.Industrial, new Vector2(584f, 414f), new Vector2(734f, 414f), 25f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false, fixedYaw: 0f);
            b.Row(new[] { MapVariantKits.Tanker }, MapVariantLayer.Vehicles, new Vector2(574f, 446f), new Vector2(660f, 446f), 4.6f,
                PlaceOptions.Prop.WithPadding(0.5f), freeYaw: false, fixedYaw: 0f);
            b.Row(new[] { MapVariantKits.TrayTruck, MapVariantKits.CanopyTruck }, MapVariantLayer.Vehicles, new Vector2(668f, 446f),
                new Vector2(746f, 446f), 4.6f, PlaceOptions.Prop.WithPadding(0.5f), freeYaw: false, fixedYaw: 0f);

            string[] stacks = MapVariantKits.ContainerStacks;
            for (float x = 576f; x < 744f; x += 8f)
            for (float z = 466f; z < 490f; z += 3.2f)
            {
                if (Mathf.Abs(x - 612f) < 11f || Mathf.Abs(x - 692f) < 11f)
                    continue;
                b.Place(b.Pick(stacks), MapVariantLayer.Props, new Vector2(x, z), 90f, PlaceOptions.Prop.WithPadding(0.2f));
            }

            b.Scatter(MapVariantKits.YardCargo, MapVariantLayer.Props, LogisticsYard, 160, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
        }

        private static void BuildSubstation(MapVariantBuilder b)
        {
            b.FencedCompound(SubstationSite, MapVariantKits.Fence,
                new MapVariantGate(MapVariantSide.North, 50f, 14f),
                new MapVariantGate(MapVariantSide.West, 46f, 10f));
            Rect bays = MapVariantBuilder.Inset(SubstationSite, 6f);
            b.Grid(MapVariantKits.Transformer, MapVariantLayer.Industrial,
                new Rect(bays.xMin, bays.yMin, bays.width, bays.height * 0.34f), 6, 3, 90f, PlaceOptions.Structure.WithPadding(0.8f));
            b.Grid(MapVariantKits.Generator, MapVariantLayer.Industrial,
                new Rect(bays.xMin, bays.yMin + bays.height * 0.38f, bays.width, bays.height * 0.25f), 8, 2, 0f, PlaceOptions.Structure.WithPadding(0.6f));
            for (float z = bays.yMin + bays.height * 0.7f; z < bays.yMax; z += 6f)
                b.Row(MapVariantKits.PowerPoles, MapVariantLayer.Industrial, new Vector2(bays.xMin, z), new Vector2(bays.xMax, z), 6f,
                    PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false, fixedYaw: 0f);
            b.Row(new[] { MapVariantKits.LightPole }, MapVariantLayer.Props, new Vector2(SubstationSite.xMin + 3f, SubstationSite.yMax - 3f),
                new Vector2(SubstationSite.xMax - 3f, SubstationSite.yMax - 3f), 24f, PlaceOptions.Prop);

            b.Row(MapVariantKits.PowerPoles, MapVariantLayer.Props, new Vector2(868f, 400f), new Vector2(868f, 700f), 24f,
                PlaceOptions.Prop, freeYaw: false, fixedYaw: 90f);
            b.Row(MapVariantKits.PowerPoles, MapVariantLayer.Props, new Vector2(760f, 396f), new Vector2(760f, 200f), 24f,
                PlaceOptions.Prop, freeYaw: false, fixedYaw: 90f);
        }

        private static void BuildPumpField(MapVariantBuilder b)
        {
            b.Grid(MapVariantKits.OilPump, MapVariantLayer.Industrial, PumpField, 4, 4, 0f, PlaceOptions.Structure.WithPadding(1f));
            for (int col = 0; col < 4; col++)
            {
                float x = Mathf.Lerp(PumpField.xMin, PumpField.xMax, (col + 0.5f) / 4f) + 5.5f;
                b.PlaceLine(MapVariantKits.SmallPipe, MapVariantLayer.Industrial, new Vector2(x, PumpField.yMin + 4f),
                    new Vector2(x, PumpField.yMax - 4f), PlaceOptions.Prop.WithPadding(0.1f));
            }

            b.PlaceLine(MapVariantKits.SmallPipe, MapVariantLayer.Industrial, new Vector2(PumpField.xMin + 4f, PumpField.yMin + 1.5f),
                new Vector2(PumpField.xMax - 2f, PumpField.yMin + 1.5f), PlaceOptions.Prop.WithPadding(0.1f));
            b.Scatter(MapVariantKits.YardCargo, MapVariantLayer.Props, PumpField, 60, PlaceOptions.Prop, freeYaw: true);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, PumpField, 120, PlaceOptions.Vegetation);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, PumpField, 40, PlaceOptions.Vegetation, minScale: 0.6f, maxScale: 1.2f);
            b.Scatter(new[] { MapVariantKits.Tanker }, MapVariantLayer.Vehicles, PumpField, 12, PlaceOptions.Prop.WithPadding(1f), freeYaw: false);
        }

        private static void BuildForwardDepot(MapVariantBuilder b)
        {
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(886f, 398f), new Vector2(886f, 494f), PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(890f, 396f), new Vector2(994f, 396f), PlaceOptions.Prop);
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(893f, 403f), 0f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(989f, 403f), 0f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Grid(MapVariantKits.FuelBladder, MapVariantLayer.Military, MapVariantBuilder.FromMinMax(930f, 446f, 990f, 490f), 4, 2, 0f,
                PlaceOptions.Prop.WithPadding(0.8f));
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(900f, 418f), new Vector2(985f, 418f), 12.5f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(900f, 432f), new Vector2(985f, 432f), 12.5f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Row(MapVariantKits.Trucks, MapVariantLayer.Vehicles, new Vector2(897f, 448f), new Vector2(925f, 448f), 4.4f,
                PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            b.Row(MapVariantKits.ArmorParked, MapVariantLayer.Vehicles, new Vector2(897f, 462f), new Vector2(925f, 462f), 5f,
                PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
            for (float z = 474f; z < 490f; z += 3.6f)
                b.Row(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, new Vector2(896f, z), new Vector2(926f, z), 3.2f,
                    PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
            SandbagRing(b, new Vector2(960f, 410f), 5.5f);
            b.Scatter(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, DepotSite, 160, PlaceOptions.Prop.WithPadding(0.4f), freeYaw: false);
        }

        internal static void SandbagRing(MapVariantBuilder b, Vector2 center, float half)
        {
            var bags = PlaceOptions.Prop.WithPadding(0.05f);
            int group = b.NewGroup();
            bags.Group = group;
            b.PlaceLine(MapVariantKits.SandbagRow, MapVariantLayer.Military, center + new Vector2(-half, -half), center + new Vector2(half, -half), bags);
            b.PlaceLine(MapVariantKits.SandbagRow, MapVariantLayer.Military, center + new Vector2(-half, half), center + new Vector2(-half, -half + 1.3f), bags);
            b.PlaceLine(MapVariantKits.SandbagRow, MapVariantLayer.Military, center + new Vector2(half, half), center + new Vector2(half, -half + 1.3f), bags);
        }

        private static void BuildLauncherRidge(MapVariantBuilder b)
        {
            Rect c = LauncherCompound;
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(c.xMin, c.yMin), new Vector2(c.xMax, c.yMin), PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(c.xMin, c.yMin + 3f), new Vector2(c.xMin, c.yMax), PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.RazorWire, MapVariantLayer.Military, new Vector2(c.xMin - 4f, c.yMin - 5f), new Vector2(c.xMax, c.yMin - 5f), PlaceOptions.Prop);
            for (float x = 928f; x <= 988f; x += 15f)
            {
                SandbagRing(b, new Vector2(x, 682f), 5f);
                b.Place(b.Pick(MapVariantKits.Launchers), MapVariantLayer.Military, new Vector2(x, 682f), 270f,
                    PlaceOptions.Structure.WithPadding(0.2f));
            }

            b.Row(new[] { MapVariantKits.RocketTruck }, MapVariantLayer.Vehicles, new Vector2(1018f, 668f), new Vector2(1018f, 700f), 8f,
                PlaceOptions.Structure.WithPadding(0.6f), freeYaw: false, fixedYaw: 270f);
            b.Place(MapVariantKits.MobileRadar, MapVariantLayer.Military, new Vector2(1040f, 700f), 200f, PlaceOptions.Structure);
            b.Place(MapVariantKits.MobileRadar, MapVariantLayer.Military, new Vector2(1060f, 680f), 160f, PlaceOptions.Structure);
            SandbagRing(b, new Vector2(1040f, 700f), 5f);
            b.Row(MapVariantKits.ArmorParked, MapVariantLayer.Vehicles, new Vector2(1030f, 780f), new Vector2(1070f, 780f), 6f,
                PlaceOptions.Prop.WithPadding(0.5f), freeYaw: false);
            RidgeRim(b);
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(1012f, 735f), new Vector2(1070f, 735f), 14f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false);
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(1012f, 760f), new Vector2(1070f, 760f), 14f,
                PlaceOptions.Structure.WithPadding(1f), freeYaw: false);
            b.Scatter(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, c, 50, PlaceOptions.Prop, freeYaw: false);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(880f, 606f, 1100f, 830f), 140,
                PlaceOptions.Vegetation.WithGroundDelta(1.6f), minScale: 0.8f, maxScale: 2f, accept: p => !c.Contains(p));
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(880f, 606f, 1100f, 830f), 120,
                PlaceOptions.Vegetation.WithGroundDelta(1.6f));
        }

        // Boulders follow the plateau shoulder so the ridge reads as a rocky rise instead of a smooth bump.
        private static void RidgeRim(MapVariantBuilder b)
        {
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

            Rect foot = MapVariantBuilder.Inset(Ridge, -RidgeSlope - 20f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, foot, 300, PlaceOptions.Vegetation.WithGroundDelta(3f),
                accept: p => !Ridge.Contains(p));
            b.Scatter(MapVariantKits.Pebbles, MapVariantLayer.Vegetation, foot, 200, PlaceOptions.Vegetation.WithGroundDelta(3f));
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

        private static void BuildWestHamlet(MapVariantBuilder b)
        {
            var ruinsBig = new[] { MapVariantKits.Ruins[4], MapVariantKits.Ruins[5] };
            var ruinsSmall = new[] { MapVariantKits.Ruins[0], MapVariantKits.Ruins[1], MapVariantKits.Ruins[2], MapVariantKits.Ruins[3] };
            Rect north = MapVariantBuilder.FromMinMax(420f, 452f, 540f, 492f);
            Rect south = MapVariantBuilder.FromMinMax(420f, 400f, 540f, 440f);
            foreach (Rect block in new[] { north, south })
            {
                b.Frontage(block, MapVariantSide.North, ruinsBig, 1f, 2.5f);
                b.Frontage(block, MapVariantSide.South, ruinsBig, 1f, 2.5f);
                b.Scatter(ruinsSmall, MapVariantLayer.City, block, 50, PlaceOptions.Structure.WithGroundDelta(0.8f), freeYaw: false);
            }

            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, WestHamlet, 80, PlaceOptions.Vegetation, minScale: 0.7f, maxScale: 1.6f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, WestHamlet, 120, PlaceOptions.Vegetation);
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, WestHamlet, 25, PlaceOptions.Vegetation, minScale: 0.8f, maxScale: 1.1f);
            b.Row(new[] { MapVariantKits.ConcreteBarrier }, MapVariantLayer.Props, new Vector2(548f, 492f), new Vector2(548f, 470f), 2.4f,
                PlaceOptions.Prop.WithPadding(0.1f), freeYaw: false);
        }

        private static void BuildRoadside(MapVariantBuilder b)
        {
            var palmRow = PlaceOptions.Vegetation.WithPadding(0.2f);
            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(20f, MainRoadZ - 8.5f), new Vector2(548f, MainRoadZ - 8.5f), 13f, palmRow, jitter: 0.4f);
            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(20f, MainRoadZ + 8.5f), new Vector2(548f, MainRoadZ + 8.5f), 13f, palmRow, jitter: 0.4f);
            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(882f, MainRoadZ + 8.5f), new Vector2(1392f, MainRoadZ + 8.5f), 13f, palmRow, jitter: 0.4f);
            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(1062f, MainRoadZ - 8.5f), new Vector2(1392f, MainRoadZ - 8.5f), 13f, palmRow, jitter: 0.4f);
            b.Row(new[] { MapVariantKits.StreetLight }, MapVariantLayer.Props, new Vector2(1000f, MainRoadZ - 6f), new Vector2(1390f, MainRoadZ - 6f), 26f,
                PlaceOptions.Prop, freeYaw: false, fixedYaw: 0f);

            Rect grove = MapVariantBuilder.FromMinMax(566f, 634f, 895f, 698f);
            b.Scatter(MapVariantKits.Houses, MapVariantLayer.City, grove, 40, PlaceOptions.Structure.WithPadding(3f), freeYaw: false);
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, grove, 260, PlaceOptions.Vegetation, minScale: 0.9f, maxScale: 1.25f);
            b.Scatter(MapVariantKits.SmallPalms, MapVariantLayer.Vegetation, grove, 160, PlaceOptions.Vegetation);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, grove, 160, PlaceOptions.Vegetation);

            b.Place(MapVariantKits.GasStation, MapVariantLayer.City, new Vector2(1030f, 470f), 0f, PlaceOptions.Structure);
            b.Place(MapVariantKits.WaterTower, MapVariantLayer.City, new Vector2(1030f, 412f), 0f, PlaceOptions.Structure);
        }

        private static void BuildCity(MapVariantBuilder b)
        {
            var frontage = new List<string>(MapVariantKits.Houses);
            frontage.AddRange(MapVariantKits.Shops);
            var infill = new List<string>(MapVariantKits.Houses);

            float[] southRows = { 150f, 250f, 350f };
            float[] columns = { 20f, 460f, 710f, 1060f, 1210f, 1380f };
            for (int r = 0; r + 1 < southRows.Length; r++)
            for (int c = 0; c + 1 < columns.Length; c++)
            {
                float xMin = c == 0 ? columns[c] : columns[c] + 2f;
                b.CityBlock(MapVariantBuilder.FromMinMax(xMin, southRows[r], columns[c + 1] - 12f, southRows[r + 1] - 10f), frontage, infill);
            }

            MapVariantSide[] twoSided = { MapVariantSide.South, MapVariantSide.North };
            b.CityBlock(MapVariantBuilder.FromMinMax(400f, 350f, 548f, 380f), frontage, infill, streetSides: twoSided);
            b.CityBlock(MapVariantBuilder.FromMinMax(562f, 350f, 700f, 380f), frontage, infill, streetSides: twoSided);
            b.CityBlock(MapVariantBuilder.FromMinMax(712f, 350f, 868f, 380f), frontage, infill, streetSides: twoSided);
            b.CityBlock(MapVariantBuilder.FromMinMax(882f, 350f, 1048f, 390f), frontage, infill,
                streetSides: new[] { MapVariantSide.South, MapVariantSide.West });

            b.CityBlock(MapVariantBuilder.FromMinMax(1062f, 350f, 1200f, 491f), frontage, infill);
            b.CityBlock(MapVariantBuilder.FromMinMax(1212f, 350f, 1390f, 491f), frontage, infill);
            b.CityBlock(MapVariantBuilder.FromMinMax(1012f, 519f, 1200f, 618f), frontage, infill);
            b.CityBlock(MapVariantBuilder.FromMinMax(1212f, 519f, 1390f, 618f), frontage, infill);
            b.CityBlock(MapVariantBuilder.FromMinMax(1212f, 632f, 1390f, 840f), frontage, infill);
            b.CityBlock(MapVariantBuilder.FromMinMax(1110f, 812f, 1200f, 840f), frontage, infill);

            Rect[] northBlocks =
            {
                MapVariantBuilder.FromMinMax(312f, 752f, 500f, 840f),
                MapVariantBuilder.FromMinMax(512f, 752f, 700f, 840f),
                MapVariantBuilder.FromMinMax(712f, 752f, 892f, 840f),
                MapVariantBuilder.FromMinMax(312f, 704f, 700f, 740f),
                MapVariantBuilder.FromMinMax(712f, 704f, 892f, 740f),
                MapVariantBuilder.FromMinMax(312f, 852f, 1200f, 905f)
            };
            PlaceLandmarks(b);
            foreach (Rect block in northBlocks)
                b.CityBlock(block, frontage, infill, palmChance: 0.6f);
        }

        private static void PlaceLandmarks(MapVariantBuilder b)
        {
            (Vector2 position, string prefab)[] landmarks =
            {
                (new Vector2(606f, 796f), MapVariantKits.Landmarks[0]),
                (new Vector2(800f, 796f), MapVariantKits.Landmarks[1]),
                (new Vector2(420f, 796f), MapVariantKits.Minarets[0]),
                (new Vector2(660f, 796f), MapVariantKits.Minarets[0]),
                (new Vector2(860f, 880f), MapVariantKits.Minarets[0]),
                (new Vector2(1300f, 560f), MapVariantKits.Minarets[0]),
                (new Vector2(1130f, 425f), MapVariantKits.Landmarks[1]),
                (new Vector2(580f, 200f), MapVariantKits.Minarets[0]),
                (new Vector2(1130f, 200f), MapVariantKits.Landmarks[0])
            };
            foreach ((Vector2 position, string prefab) in landmarks)
            {
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    Vector2 p = position + new Vector2(b.Range(-20f, 20f), b.Range(-14f, 14f));
                    if (b.Place(prefab, MapVariantLayer.City, p, 180f, PlaceOptions.Structure.WithPadding(3f)) != null)
                        break;
                }
            }

            foreach (Vector2 plaza in new[] { new Vector2(606f, 770f), new Vector2(1130f, 395f) })
                b.Place(b.Pick(MapVariantKits.Plazas), MapVariantLayer.City, plaza, 0f, PlaceOptions.Structure);
        }

        private static void BuildDesertFringe(MapVariantBuilder b)
        {
            Rect west = MapVariantBuilder.FromMinMax(20f, 360f, 398f, 930f);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, west, 420, PlaceOptions.Vegetation.WithGroundDelta(2f), minScale: 1f, maxScale: 3.2f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, west, 420, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(300f, 520f, 398f, 700f), 40,
                PlaceOptions.Vegetation.WithGroundDelta(1.2f));
            b.Scatter(MapVariantKits.Ruins, MapVariantLayer.City, west, 40, PlaceOptions.Structure.WithGroundDelta(1f), freeYaw: false);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(20f, 905f, 1380f, 1060f), 380,
                PlaceOptions.Vegetation.WithGroundDelta(4f), minScale: 1.5f, maxScale: 4.5f);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(1100f, 650f, 1395f, 1000f), 80,
                PlaceOptions.Vegetation.WithGroundDelta(1.5f), minScale: 1f, maxScale: 2.5f);
        }

        private static void BuildBackdrop(MapVariantBuilder b)
        {
            b.Place(MapVariantKits.SmokeStackBackground, MapVariantLayer.Backdrop, new Vector2(760f, 960f), 0f, PlaceOptions.Scenery.WithSink(0.2f));
            for (float x = 60f; x < 1400f; x += 150f)
            {
                float z = 1120f + b.Range(-25f, 25f);
                b.Place(b.Pick(MapVariantKits.BackgroundRidges), MapVariantLayer.Backdrop, new Vector2(x + b.Range(-30f, 30f), z),
                    b.Range(-8f, 8f), PlaceOptions.Scenery.WithScale(b.Range(3.6f, 5.2f)).WithSink(0f));
            }

            b.MountainBackdrop(MapVariantBuilder.FromMinMax(60f, 1000f, 1340f, 1090f), 22, 2.4f, 3.6f);
            b.MountainBackdrop(MapVariantBuilder.FromMinMax(30f, 560f, 150f, 940f), 8, 1.6f, 2.6f);
            b.MountainBackdrop(MapVariantBuilder.FromMinMax(30f, 250f, 150f, 450f), 4, 1.4f, 2.2f);
        }

        private static void AddZones(MapVariantBuilder b)
        {
            b.AddZone("SupplyLine_Refinery", "CH02 M02 Supply Line", new Vector2(715f, 500f), new Vector2(340f, 180f),
                "Convoy runs east along the main road past warehouses, tankers and the refinery gate.");
            b.AddZone("PowerRelay_Substation", "CH02 M04 Power Relay", new Vector2(790f, 450f), new Vector2(260f, 150f),
                "Relay crew restores the fenced substation; saboteurs approach through the logistics yard.");
            b.AddZone("SplitFront_Ridge", "CH04 M03 Split Front", new Vector2(760f, 560f), new Vector2(480f, 300f),
                "Enemy enters from the west, forks into north pipeline lane and south service lane; launchers hold the NE ridge.");
            b.AddZone("Anchor_EnemySpawnWest", "all", new Vector2(420f, MainRoadZ), new Vector2(20f, 20f), "Western desert entry.");
            b.AddZone("Anchor_WestFork", "all", new Vector2(WestJunctionX, MainRoadZ), new Vector2(20f, 20f), "Three-lane fork.");
            b.AddZone("Anchor_FriendlyDepot", "all", DepotSite.center, DepotSite.size, "Forward depot with fuel bladders.");
            b.AddZone("Anchor_LauncherCompound", "Split Front", LauncherCompound.center, LauncherCompound.size, "Ridge launcher compound.");
            b.AddZone("Anchor_Substation", "Power Relay", SubstationSite.center, SubstationSite.size, "Substation core.");
            b.AddZone("Anchor_RefineryGate", "Supply Line", new Vector2(716f, 516f), new Vector2(16f, 6f), "Refinery south gate.");
        }
    }
}
