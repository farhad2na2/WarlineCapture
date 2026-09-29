using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    // City-Edge Airfield: Airlift, Air Corridor and Forward Post share a runway built against the old city wall.
    // The apron (hangars, tower, helipad, field hospital) faces the runway; a crash field covers the southern approach.
    internal static class MapVariantCityAirfield
    {
        public const string MapId = "CityEdgeAirfield";
        public const int Seed = 10404;

        private static readonly Rect WorldRect = new(0f, 0f, 1400f, 1200f);
        private static readonly Rect PlayableRect = new(400f, 300f, 600f, 400f);

        private const float RunwayZ = 560f;
        private const float TaxiwayZ = 605f;
        private const float PerimeterRoadZ = 445f;
        internal const string Mil = "Assets/PolygonMilitary/Prefabs";

        private static readonly Rect Airfield = MapVariantBuilder.FromMinMax(430f, 520f, 970f, 700f);
        private static readonly Rect Apron = MapVariantBuilder.FromMinMax(470f, 614f, 930f, 694f);
        private static readonly Rect CrashField = MapVariantBuilder.FromMinMax(560f, 456f, 900f, 516f);
        private static readonly Rect ForwardPost = MapVariantBuilder.FromMinMax(462f, 456f, 548f, 516f);

        internal const string Runway = Mil + "/Environment/SM_Env_Runway_01.prefab";
        internal const string Helipad = Mil + "/Environment/SM_Env_Helipad_01.prefab";
        internal const string RunwayLight = Mil + "/Props/SM_Prop_Runway_Light_01.prefab";
        internal const string RunwayBarrier = Mil + "/Props/SM_Prop_Runway_Barrier_01.prefab";
        internal const string TransportPlane = Mil + "/Vehicles/SM_Veh_TransportPlane_01.prefab";
        internal const string TransportHeli = Mil + "/Vehicles/SM_Veh_Helicopter_Transport_01.prefab";
        internal const string AttackHeli = Mil + "/Vehicles/SM_Veh_Helicopter_Attack_01.prefab";
        internal const string Jet = Mil + "/Vehicles/SM_Veh_Jet_01.prefab";
        internal const string FieldHospital = Mil + "/Buildings/SM_Bld_Tent_01.prefab";
        internal const string FieldHospitalOpen = Mil + "/Buildings/SM_Bld_Tent_Open_01.prefab";
        internal const string MedicalBed = Mil + "/Props/Military/SM_Prop_Bed_Medical_01.prefab";
        internal const string Stretcher = Mil + "/Props/Military/SM_Prop_Bed_Stretcher_01.prefab";
        internal const string MedicalBox = Mil + "/Props/Military/SM_Prop_MedicalBox_01.prefab";

        internal static readonly string[] Wreckage =
        {
            Mil + "/Vehicles/Destroyed/SM_Veh_TransportPlane_Destroyed_Cockpit_01.prefab",
            Mil + "/Vehicles/Destroyed/SM_Veh_TransportPlane_Destroyed_Fuselage_01.prefab",
            Mil + "/Vehicles/Destroyed/SM_Veh_TransportPlane_Destroyed_Tail_01.prefab",
            Mil + "/Vehicles/Destroyed/SM_Veh_Helicopter_Transport_01_Destroyed.prefab",
            Mil + "/Vehicles/Destroyed/SM_Veh_Jet_Destroyed_01.prefab",
            Mil + "/Vehicles/Destroyed/SM_Veh_Truck_01_Destroyed.prefab",
            Mil + "/Vehicles/Destroyed/SM_Veh_APC_01_Destroyed.prefab"
        };

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

            BuildRunway(b);
            BuildApron(b);
            BuildPerimeter(b);
            BuildForwardPost(b);
            BuildCrashField(b);
            BuildCity(b);
            BuildDesert(b);
            b.DesertDressing(WorldRect);
            AddZones(b);

            b.BuildLighting();
            return b;
        }

        public static IEnumerable<MapVariantView> Views(MapVariantBuilder b)
        {
            yield return b.TopDown("01_topdown_playable", PlayableRect);
            yield return b.TopDown("02_topdown_world", WorldRect);
            yield return MapVariantView.Battle("03_battle_airlift_helipad", new Vector2(790f, 630f), 0f);
            yield return MapVariantView.Battle("04_battle_air_corridor_tower", new Vector2(640f, 620f), 0f);
            yield return MapVariantView.Battle("05_battle_forward_post", new Vector2(480f, 500f), 0f);
            yield return MapVariantView.Battle("06_battle_crash_field", new Vector2(730f, 500f), 0f);
            yield return MapVariantView.Battle("07_battle_runway_west", new Vector2(520f, 580f), 0f);
            yield return new MapVariantView("08_hero_airfield", new Vector3(560f, 55f, 400f), new Vector3(16f, 20f, 0f), 50f);
            yield return new MapVariantView("09_hero_apron", new Vector3(900f, 22f, 590f), new Vector3(9f, 290f, 0f), 55f);
            yield return new MapVariantView("10_ground_runway", new Vector3(470f, 4f, 560f), new Vector3(4f, 90f, 0f), 55f);
        }

        private static void ShapeTerrain(MapVariantBuilder b)
        {
            b.Height.Apply((x, z, _) =>
            {
                float n = Mathf.PerlinNoise(x * 0.011f + 5.3f, z * 0.011f + 1.7f);
                float h = 0f;
                h += Mathf.Clamp01((330f - x) / 170f) * (2f + n * 10f);
                h += Mathf.Clamp01((x - 1250f) / 120f) * (1f + n * 6f);
                float north = Mathf.Clamp01((z - 930f) / 220f);
                float ridges = Mathf.PerlinNoise(x * 0.006f + 4.4f, z * 0.02f) * 0.7f + Mathf.PerlinNoise(x * 0.02f, z * 0.02f + 2f) * 0.3f;
                h += north * north * (20f + ridges * 120f);
                if (CrashField.Contains(new Vector2(x, z)) || ForwardPost.Contains(new Vector2(x, z)))
                    h += Mathf.Max(0f, Mathf.PerlinNoise(x * 0.03f + 7f, z * 0.03f) - 0.55f) * 2.5f;
                return h;
            });
        }

        private static void LayRoads(MapVariantBuilder b)
        {
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(5f, PerimeterRoadZ), new Vector2(1395f, PerimeterRoadZ));
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(455f, TaxiwayZ), new Vector2(945f, TaxiwayZ));
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(455f, PerimeterRoadZ), new Vector2(455f, TaxiwayZ));
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(945f, PerimeterRoadZ), new Vector2(945f, TaxiwayZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(705f, TaxiwayZ), new Vector2(705f, 885f));

            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(5f, 345f), new Vector2(1395f, 345f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(5f, 245f), new Vector2(1395f, 245f));
            foreach (float x in new[] { 305f, 555f, 805f, 1055f, 1205f })
                b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(x, 145f), new Vector2(x, PerimeterRoadZ));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(305f, 745f), new Vector2(1195f, 745f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(305f, 845f), new Vector2(1195f, 845f));
            foreach (float x in new[] { 505f, 905f })
                b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(x, 745f), new Vector2(x, 845f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1055f, PerimeterRoadZ), new Vector2(1055f, 745f));
            b.AddRoad(MapVariantRoadKind.DirtSidewalk, new Vector2(1055f, 605f), new Vector2(1395f, 605f));
        }

        private static void LayPads(MapVariantBuilder b)
        {
            b.Pad(MapVariantBuilder.FromMinMax(Apron.xMin, Apron.yMin, 700f, Apron.yMax), MapVariantKits.ConcretePad);
            b.Pad(MapVariantBuilder.FromMinMax(710f, Apron.yMin, Apron.xMax, Apron.yMax), MapVariantKits.ConcretePad);
            b.Pad(ForwardPost, MapVariantKits.DirtPad);
        }

        private static void BuildRunway(MapVariantBuilder b)
        {
            // The runway is a driveable surface, so it is not reserved; the parked transport rests on it.
            // Its deck sits 1.2 m above the mesh base, above a bevelled skirt, so it is sunk until the deck is flush.
            PlaceOptions slab = PlaceOptions.Structure.WithSink(1.14f).WithBoundsAnchor();
            slab.Reserve = false;
            b.PlaceLine(Runway, MapVariantLayer.Industrial, new Vector2(490.7f, RunwayZ), new Vector2(909.3f, RunwayZ), slab);
            b.Place(TransportPlane, MapVariantLayer.Vehicles, new Vector2(860f, RunwayZ), 270f,
                PlaceOptions.Structure.WithPadding(1f).WithBoundsAnchor().WithGroundDelta(0.5f).WithSink(-0.06f));
            foreach (float z in new[] { RunwayZ - 18.5f, RunwayZ + 18.5f })
                b.Row(new[] { RunwayLight }, MapVariantLayer.Props, new Vector2(492f, z), new Vector2(908f, z), 12f,
                    PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
            foreach (float x in new[] { 485f, 915f })
                b.Row(new[] { RunwayBarrier }, MapVariantLayer.Props, new Vector2(x, RunwayZ - 12f), new Vector2(x, RunwayZ + 12f), 11.5f,
                    PlaceOptions.Prop.WithPadding(0.1f), freeYaw: false);
        }

        private static void BuildApron(MapVariantBuilder b)
        {
            b.Row(new[] { MapVariantKits.HangarOpen, MapVariantKits.Hangar }, MapVariantLayer.Industrial, new Vector2(495f, Apron.yMax - 19f),
                new Vector2(605f, Apron.yMax - 19f), 34f, PlaceOptions.Structure.WithPadding(1f), freeYaw: false, fixedYaw: 180f);
            b.Place(MapVariantKits.ControlTower, MapVariantLayer.Industrial, new Vector2(650f, Apron.yMax - 12f), 180f,
                PlaceOptions.Structure.WithPadding(1f));
            PlaceOptions pad = PlaceOptions.Structure.WithPadding(0.5f).WithSink(0.72f).WithBoundsAnchor();
            MapVariantPlacement westPad = b.Place(Helipad, MapVariantLayer.Industrial, new Vector2(760f, 648f), 0f, pad);
            MapVariantPlacement eastPad = b.Place(Helipad, MapVariantLayer.Industrial, new Vector2(800f, 648f), 0f, pad);
            b.PlaceOnTop(TransportHeli, MapVariantLayer.Vehicles, westPad, Vector2.zero, 200f);
            b.PlaceOnTop(AttackHeli, MapVariantLayer.Vehicles, eastPad, Vector2.zero, 160f);

            b.Row(new[] { FieldHospital, FieldHospitalOpen }, MapVariantLayer.Military, new Vector2(835f, 680f), new Vector2(915f, 680f), 13.5f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false, fixedYaw: 90f);
            b.Scatter(new[] { MedicalBed, Stretcher, MedicalBox }, MapVariantLayer.Props, MapVariantBuilder.FromMinMax(830f, 620f, 925f, 668f), 70,
                PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(680f, 628f), new Vector2(735f, 628f), 13f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Row(new[] { Jet }, MapVariantLayer.Vehicles, new Vector2(612f, 640f), new Vector2(700f, 640f), 18f,
                PlaceOptions.Structure.WithPadding(0.6f).WithBoundsAnchor(), freeYaw: false, fixedYaw: 180f);
            b.Grid(MapVariantKits.FuelBladder, MapVariantLayer.Military, MapVariantBuilder.FromMinMax(875f, 615f, 928f, 645f), 3, 1, 90f,
                PlaceOptions.Prop.WithPadding(0.6f));
            b.Scatter(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, Apron, 260, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
            b.Scatter(MapVariantKits.Trucks, MapVariantLayer.Vehicles, Apron, 16, PlaceOptions.Prop.WithPadding(0.6f), freeYaw: false);
            b.Row(new[] { MapVariantKits.LightPole }, MapVariantLayer.Props, new Vector2(Apron.xMin + 4f, Apron.yMin + 2f),
                new Vector2(Apron.xMax - 4f, Apron.yMin + 2f), 30f, PlaceOptions.Prop);
        }

        private static void BuildPerimeter(MapVariantBuilder b)
        {
            var wall = PlaceOptions.Prop.WithPadding(0.05f);
            b.PlaceLine(MapVariantKits.TallBarrier, MapVariantLayer.Military, new Vector2(Airfield.xMin, Airfield.yMax + 4f), new Vector2(698f, Airfield.yMax + 4f), wall);
            b.PlaceLine(MapVariantKits.TallBarrier, MapVariantLayer.Military, new Vector2(712f, Airfield.yMax + 4f), new Vector2(Airfield.xMax, Airfield.yMax + 4f), wall);
            b.PlaceLine(MapVariantKits.Fence, MapVariantLayer.Props, new Vector2(462f, Airfield.yMin), new Vector2(938f, Airfield.yMin), wall);
            b.PlaceLine(MapVariantKits.Fence, MapVariantLayer.Props, new Vector2(Airfield.xMin + 4f, Airfield.yMin), new Vector2(Airfield.xMin + 4f, 598f), wall);
            b.PlaceLine(MapVariantKits.Fence, MapVariantLayer.Props, new Vector2(Airfield.xMax - 4f, Airfield.yMin), new Vector2(Airfield.xMax - 4f, 598f), wall);
            foreach (Vector2 corner in new[] { new Vector2(440f, 530f), new Vector2(960f, 530f), new Vector2(440f, 695f), new Vector2(960f, 695f) })
                b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, corner, 0f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Row(MapVariantKits.Checkpoint, MapVariantLayer.Props, new Vector2(700f, 700f), new Vector2(711f, 700f), 2.2f,
                PlaceOptions.Prop.WithPadding(0.05f), freeYaw: false);
        }

        private static void BuildForwardPost(MapVariantBuilder b)
        {
            Rect p = ForwardPost;
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(p.xMin + 2f, p.yMin + 2f), new Vector2(p.xMax - 2f, p.yMin + 2f), PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.SandbagRowLong, MapVariantLayer.Military, new Vector2(p.xMin + 2f, p.yMin + 5f), new Vector2(p.xMin + 2f, p.yMax - 2f), PlaceOptions.Prop);
            b.PlaceLine(MapVariantKits.RazorWire, MapVariantLayer.Military, new Vector2(p.xMin + 6f, p.yMin - 2f), new Vector2(p.xMax - 6f, p.yMin - 2f),
                PlaceOptions.Prop.WithPadding(0.05f));
            b.Row(MapVariantKits.CamoTents, MapVariantLayer.Military, new Vector2(p.xMin + 14f, p.center.y + 8f), new Vector2(p.xMax - 10f, p.center.y + 8f), 13f,
                PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
            b.Place(MapVariantKits.GuardTower, MapVariantLayer.Military, new Vector2(p.xMax - 8f, p.yMin + 8f), 0f, PlaceOptions.Structure.WithPadding(0.3f));
            b.Scatter(MapVariantKits.MilitaryCargo, MapVariantLayer.Props, p, 90, PlaceOptions.Prop.WithPadding(0.3f), freeYaw: false);
            b.Scatter(MapVariantKits.ArmorParked, MapVariantLayer.Vehicles, p, 6, PlaceOptions.Prop.WithPadding(0.6f), freeYaw: false);
        }

        private static void BuildCrashField(MapVariantBuilder b)
        {
            b.Scatter(Wreckage, MapVariantLayer.Vehicles, CrashField, 40, PlaceOptions.Structure.WithPadding(1.5f).WithGroundDelta(1f));
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, CrashField, 120, PlaceOptions.Vegetation.WithGroundDelta(1.2f), minScale: 0.8f, maxScale: 2f);
            b.Scatter(MapVariantKits.Ruins, MapVariantLayer.City, CrashField, 20, PlaceOptions.Structure.WithGroundDelta(0.8f), freeYaw: false);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, CrashField, 120, PlaceOptions.Vegetation);
            b.Scatter(MapVariantKits.Pebbles, MapVariantLayer.Vegetation, CrashField, 90, PlaceOptions.Vegetation);
            b.Scatter(new[] { MapVariantKits.Ruins[4], MapVariantKits.Ruins[5] }, MapVariantLayer.City,
                MapVariantBuilder.FromMinMax(906f, 456f, 1048f, 516f), 14, PlaceOptions.Structure.WithGroundDelta(0.8f), freeYaw: false);
            b.Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(906f, 456f, 1048f, 516f), 40, PlaceOptions.Vegetation);
        }

        private static void BuildCity(MapVariantBuilder b)
        {
            var frontage = new List<string>(MapVariantKits.Houses);
            frontage.AddRange(MapVariantKits.Shops);
            var infill = new List<string>(MapVariantKits.Houses);

            float[] columns = { 20f, 310f, 560f, 810f, 1060f, 1210f, 1380f };
            foreach ((float zMin, float zMax) in new[] { (150f, 240f), (250f, 340f), (350f, 440f) })
                for (int c = 0; c + 1 < columns.Length; c++)
                {
                    float xMin = c == 0 ? columns[c] : columns[c] + 2f;
                    float xMax = columns[c + 1] - (c + 1 == columns.Length - 1 ? 0f : 12f);
                    b.CityBlock(MapVariantBuilder.FromMinMax(xMin, zMin, xMax, zMax), frontage, infill);
                }

            Rect[] northBlocks =
            {
                MapVariantBuilder.FromMinMax(310f, 710f, 698f, 740f),
                MapVariantBuilder.FromMinMax(712f, 710f, 1048f, 740f),
                MapVariantBuilder.FromMinMax(310f, 752f, 498f, 840f),
                MapVariantBuilder.FromMinMax(512f, 752f, 698f, 840f),
                MapVariantBuilder.FromMinMax(712f, 752f, 898f, 840f),
                MapVariantBuilder.FromMinMax(912f, 752f, 1048f, 840f),
                MapVariantBuilder.FromMinMax(310f, 852f, 1200f, 910f),
                MapVariantBuilder.FromMinMax(1062f, 456f, 1390f, 598f),
                MapVariantBuilder.FromMinMax(1062f, 612f, 1390f, 738f),
                MapVariantBuilder.FromMinMax(1062f, 752f, 1390f, 840f),
                MapVariantBuilder.FromMinMax(20f, 456f, 400f, 700f)
            };
            (Vector2 position, string prefab)[] landmarks =
            {
                (new Vector2(600f, 796f), MapVariantKits.Landmarks[0]),
                (new Vector2(810f, 796f), MapVariantKits.Landmarks[1]),
                (new Vector2(420f, 796f), MapVariantKits.Minarets[0]),
                (new Vector2(690f, 790f), MapVariantKits.Minarets[0]),
                (new Vector2(980f, 790f), MapVariantKits.Minarets[0]),
                (new Vector2(1250f, 700f), MapVariantKits.Landmarks[0]),
                (new Vector2(200f, 580f), MapVariantKits.Minarets[0])
            };
            foreach ((Vector2 position, string prefab) in landmarks)
                for (int attempt = 0; attempt < 30; attempt++)
                    if (b.Place(prefab, MapVariantLayer.City, position + new Vector2(b.Range(-18f, 18f), b.Range(-12f, 12f)), 180f,
                            PlaceOptions.Structure.WithPadding(3f)) != null)
                        break;

            foreach (Rect block in northBlocks)
                b.CityBlock(block, frontage, infill, palmChance: 0.6f);

            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(20f, PerimeterRoadZ + 8.5f), new Vector2(446f, PerimeterRoadZ + 8.5f), 13f,
                PlaceOptions.Vegetation.WithPadding(0.2f), jitter: 0.3f);
            b.Row(MapVariantKits.Palms, MapVariantLayer.Vegetation, new Vector2(954f, PerimeterRoadZ + 8.5f), new Vector2(1390f, PerimeterRoadZ + 8.5f), 13f,
                PlaceOptions.Vegetation.WithPadding(0.2f), jitter: 0.3f);
            b.Scatter(MapVariantKits.CivilianVehicles, MapVariantLayer.Vehicles, MapVariantBuilder.FromMinMax(20f, 150f, 1380f, 440f), 70,
                PlaceOptions.Prop.WithPadding(0.8f), freeYaw: false, accept: p => b.DistanceToRoad(p) < 4f);
        }

        private static void BuildDesert(MapVariantBuilder b)
        {
            Rect west = MapVariantBuilder.FromMinMax(20f, 700f, 300f, 950f);
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, west, 260, PlaceOptions.Vegetation.WithGroundDelta(2f), minScale: 1f, maxScale: 3f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, west, 260, PlaceOptions.Vegetation.WithGroundDelta(1.2f));
            b.Scatter(MapVariantKits.Rocks, MapVariantLayer.Vegetation, MapVariantBuilder.FromMinMax(20f, 915f, 1380f, 1060f), 360,
                PlaceOptions.Vegetation.WithGroundDelta(4f), minScale: 1.5f, maxScale: 4.5f);
            b.Scatter(MapVariantKits.Shrubs, MapVariantLayer.Vegetation, MapVariantBuilder.Inset(Airfield, 2f), 200, PlaceOptions.Vegetation,
                accept: p => !Apron.Contains(p) && Mathf.Abs(p.y - RunwayZ) > 22f);
        }

        private static void BuildBackdrop(MapVariantBuilder b)
        {
            for (float x = 60f; x < 1400f; x += 150f)
                b.Place(b.Pick(MapVariantKits.BackgroundRidges), MapVariantLayer.Backdrop, new Vector2(x + b.Range(-30f, 30f), 1120f + b.Range(-25f, 25f)),
                    b.Range(-8f, 8f), PlaceOptions.Scenery.WithScale(b.Range(3.6f, 5.2f)).WithSink(0f));
            b.MountainBackdrop(MapVariantBuilder.FromMinMax(60f, 1000f, 1340f, 1090f), 22, 2.4f, 3.6f);
        }

        private static void AddZones(MapVariantBuilder b)
        {
            b.AddZone("Airlift_Helipad", "CH01 M04 Airlift", new Vector2(800f, 640f), new Vector2(260f, 150f),
                "Evacuate from the helipads beside the field hospital; enemies push in from the crash field.");
            b.AddZone("AirCorridor_Tower", "CH04 M01 Air Corridor", new Vector2(680f, 600f), new Vector2(520f, 220f),
                "Hold the control tower and runway while air support clears the corridor.");
            b.AddZone("ForwardPost_West", "CH01 M02 Forward Post", ForwardPost.center, ForwardPost.size + new Vector2(80f, 60f),
                "Sandbagged outpost at the west end of the runway.");
            b.AddZone("Anchor_ControlTower", "Air Corridor", new Vector2(650f, Apron.yMax - 12f), new Vector2(12f, 12f), "Control tower.");
            b.AddZone("Anchor_Helipads", "Airlift", new Vector2(780f, 648f), new Vector2(70f, 30f), "Twin helipads.");
            b.AddZone("Anchor_FieldHospital", "Airlift", new Vector2(875f, 660f), new Vector2(95f, 50f), "Field hospital tents.");
            b.AddZone("Anchor_CityGate", "all", new Vector2(705f, 700f), new Vector2(14f, 8f), "Gate into the old city.");
            b.AddZone("Anchor_EnemySouth", "all", new Vector2(730f, PerimeterRoadZ), new Vector2(40f, 20f), "Southern approach road.");
        }
    }
}
