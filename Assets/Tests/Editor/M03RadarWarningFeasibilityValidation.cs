using System;
using System.IO;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public static class M03RadarWarningFeasibilityValidation
{
    public const string EvidenceRoot = "Design/AgentReports/M03RadarWarning";

    public static void RunFocusedValidation()
    {
        try
        {
            Directory.CreateDirectory(EvidenceRoot);
            M02EstablishBaseForwardPostWindowValidation.ValidateCurrentDefinition();
            StringBuilder report = new("# M03 feasibility probe\n\n");
            string[] paths =
            {
                M02EstablishBaseConfigBuilder.BarracksConfigPath,
                "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_GuardTower_Config.asset",
                "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Road_Barrier_Config.asset",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Radar_Tank.asset",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_APC_Fast_Config.asset"
            };
            foreach (string path in paths)
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                Assert.NotNull(asset, path);
                SerializedObject serialized = new(asset);
                report.AppendLine($"## {asset.name}\n\nAsset: `{path}`\n");
                foreach (string field in new[] { "price", "materialsCost", "productionDurationSeconds", "canAttack",
                             "maxHealth", "attackRange", "attackDamage", "speed", "roadSpeedMultiplier",
                             "threatDetectionKind", "threatDetectionRadiusCells", "footprintCells" })
                {
                    SerializedProperty value = serialized.FindProperty(field);
                    if (value == null) continue;
                    report.AppendLine($"- {field}: {PropertyText(value)}");
                }
                report.AppendLine();
            }
            new M02EstablishBaseBarracksProductionTests().ExistingProductionMetadataResolvesTheApprovedRifle();
            new M02EstablishBaseBarracksProductionTests().OneBarracksOrderQueuesTheCanonicalFourSoldierSquad();
            report.AppendLine("Barracks metadata and real request queue: four rifle members per order passed.\n");
            OperationMapDefinition source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                M02EstablishBaseForwardPostWindowValidation.SourceDefinitionPath);
            MapSurfaceDataAsset surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(
                AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID));
            Assert.IsTrue(surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob));
            using (blob)
                SampleApproach(ref blob.Value, report);
            File.WriteAllText(Path.Combine(EvidenceRoot, "feasibility_probe.md"), report.ToString());
            Debug.Log("[M03RadarWarningFeasibilityValidation] result=Passed physicalSource=unchanged barracksQuantity=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M03RadarWarningFeasibilityValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    private static string PropertyText(SerializedProperty value) => value.propertyType switch
    {
        SerializedPropertyType.Float => value.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
        SerializedPropertyType.Boolean => value.boolValue.ToString(),
        SerializedPropertyType.Vector2Int => value.vector2IntValue.ToString(),
        _ => value.intValue.ToString()
    };

    private static void SampleApproach(ref MapSurfaceBlob surface, StringBuilder report)
    {
        const int left = 500, bottom = 250, width = 620, height = 240;
        Texture2D image = new(width, height, TextureFormat.RGBA32, false);
        try
        {
            Color32[] pixels = new Color32[width * height];
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                Color32 color = new(80, 30, 30, 255);
                if (MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(left + x, bottom + z), out MapSurfaceSample sample))
                {
                    bool drive = (sample.MovementMask & MapSurfaceMovementMask.WheeledVehicle) != 0;
                    bool road = (sample.Flags & (MapSurfaceFlags.Road | MapSurfaceFlags.Highway)) != 0;
                    color = drive ? road ? new Color32(140, 145, 150, 255) : new Color32(198, 180, 130, 255)
                        : new Color32(80, 30, 30, 255);
                }
                pixels[z * width + x] = color;
            }
            image.SetPixels32(pixels);
            image.Apply(false);
            File.WriteAllBytes(Path.Combine(EvidenceRoot, "approach_surface.png"), image.EncodeToPNG());
            report.AppendLine("## Surface sampling\n\nWindow: cells x=500..1119, z=250..489. Image north is up; grey is road/highway, sand is wheel-accessible surface, red is unavailable to wheels. Runtime building footprints still require a separate collision probe.\n");
            foreach (int2 cell in new[] { new int2(580, 300), new int2(700, 300), new int2(825, 315),
                         new int2(850, 330), new int2(885, 342), new int2(910, 350), new int2(937, 348),
                         new int2(1026, 340), new int2(1060, 430) })
            {
                bool found = MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, cell, out MapSurfaceSample sample);
                report.AppendLine($"- {cell}: found={found}, type={sample.SurfaceType}, movement={sample.MovementMask}, height={sample.Height}");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
}
