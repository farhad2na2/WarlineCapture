#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureGameTerrain4PlayableLandAudit
{
    private const string TargetScenePath = "Assets/Game/Scenes/Game_Terrain4.unity";
    private const string IslandRootName = "Island";
    private const string FoundationName = "ExpandedIsland_SourceGameTerrain3PrefabsOnly";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain4_PlayableLandAudit";
    private const string AuditJsonPath = DataRoot + "/game_terrain4_playable_land_audit.json";
    private const string AuditReportPath = "Design/AgentReports/2026-05-25_gameplay_game-terrain4-playable-land-audit.md";
    private const int MapGridSize = 2024;
    private const float MapGridMaxCoordinate = MapGridSize - 1f;

    [MenuItem("WarlineCapture/Design/Game Terrain4/Audit Playable Land Footprint")]
    public static void AuditPlayableLandFootprint()
    {
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        GameObject island = FindRootGameObject(scene, IslandRootName);
        if (island == null)
            throw new InvalidOperationException("Missing root Island object in " + TargetScenePath);

        Transform foundation = FindDirectChild(island.transform, FoundationName);
        if (foundation == null)
            throw new InvalidOperationException("Missing foundation child under Island: " + FoundationName);

        List<CategoryBounds> foundationCategories = ScanFoundationCategories(foundation);
        List<CategoryBounds> generatedCategories = ScanGeneratedCategories(island.transform);
        CategoryBounds foundationOverall = BuildCategoryBounds("FoundationOverall", "All renderers under the preserved island foundation.", foundation.GetComponentsInChildren<Renderer>(true));
        CategoryBounds generatedOverall = BuildGeneratedOverall(generatedCategories);
        CategoryBounds mapTarget = CategoryBounds.FromRect("MapTarget2024", "Full 2024x2024 gameplay map target rect.", -MapGridMaxCoordinate * 0.5f, -MapGridMaxCoordinate * 0.5f, MapGridMaxCoordinate * 0.5f, MapGridMaxCoordinate * 0.5f, 0);
        BeachPlacementInfo beachPlacement = BuildBeachPlacementInfo(foundation, mapTarget);

        WriteJson(foundationOverall, mapTarget, foundationCategories, generatedOverall, generatedCategories, beachPlacement);
        WriteReport(foundationOverall, mapTarget, foundationCategories, generatedOverall, generatedCategories, beachPlacement);
        AssetDatabase.Refresh();

        CategoryBounds green = FindCategory(foundationCategories, "GreenPlayableGround");
        Debug.Log($"WARLINECAPTURE_GAME_TERRAIN4_PLAYABLE_LAND_AUDIT_READY mapWidth={mapTarget.Width:0.###} mapDepth={mapTarget.Depth:0.###} foundationWidth={foundationOverall.Width:0.###} foundationDepth={foundationOverall.Depth:0.###} greenWidth={green.Width:0.###} greenDepth={green.Depth:0.###} beachCentersInsideMap={beachPlacement.CentersInsideMap} report={AuditReportPath}");
    }

    private static List<CategoryBounds> ScanFoundationCategories(Transform foundation)
    {
        Dictionary<string, CategoryAccumulator> accumulators = new(StringComparer.Ordinal);
        foreach (Renderer renderer in foundation.GetComponentsInChildren<Renderer>(true))
        {
            string category = CategoryForFoundationObject(renderer.transform.name);
            if (!accumulators.TryGetValue(category, out CategoryAccumulator accumulator))
            {
                accumulator = new CategoryAccumulator(category, DescriptionForCategory(category));
                accumulators[category] = accumulator;
            }

            accumulator.Add(renderer);
        }

        List<CategoryBounds> categories = new();
        foreach (CategoryAccumulator accumulator in accumulators.Values)
            categories.Add(accumulator.ToBounds());
        categories.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return categories;
    }

    private static List<CategoryBounds> ScanGeneratedCategories(Transform island)
    {
        List<CategoryBounds> categories = new();
        foreach (string groupName in new[] { "Generated_Mountains", "Generated_Trees", "Generated_Bushes", "Generated_Rocks" })
        {
            Transform group = FindDirectChild(island, groupName);
            if (group == null)
                continue;

            categories.Add(BuildCategoryBounds(groupName, "Generated dressing group bounds.", group.GetComponentsInChildren<Renderer>(true)));
        }

        categories.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return categories;
    }

    private static CategoryBounds BuildGeneratedOverall(List<CategoryBounds> categories)
    {
        CategoryAccumulator accumulator = new("GeneratedDressingOverall", "All generated mountain/tree/bush/rock dressing renderer bounds.");
        foreach (CategoryBounds category in categories)
            accumulator.Add(category);
        return accumulator.ToBounds();
    }

    private static CategoryBounds BuildCategoryBounds(string id, string description, Renderer[] renderers)
    {
        CategoryAccumulator accumulator = new(id, description);
        foreach (Renderer renderer in renderers)
            accumulator.Add(renderer);
        return accumulator.ToBounds();
    }

    private static string CategoryForFoundationObject(string objectName)
    {
        if (objectName.StartsWith("GroundFill_", StringComparison.Ordinal) ||
            objectName.StartsWith("GroundShore_", StringComparison.Ordinal) ||
            objectName.StartsWith("GrassDetail_", StringComparison.Ordinal))
            return "GreenPlayableGround";

        if (objectName.StartsWith("BeachCoast_", StringComparison.Ordinal))
            return "OuterBeachCoast";

        if (objectName.StartsWith("BeachBlend_", StringComparison.Ordinal) ||
            objectName.StartsWith("BeachInner_", StringComparison.Ordinal) ||
            objectName.StartsWith("BeachLandEdge_", StringComparison.Ordinal))
            return "InnerBeachBlend";

        return "OtherFoundation";
    }

    private static string DescriptionForCategory(string category)
    {
        if (category == "GreenPlayableGround")
            return "Grass/dirt foundation prefabs that should carry the 2024x2024 gameplay map after the rebuild.";
        if (category == "OuterBeachCoast")
            return "Outer beach/coast prefabs that should remain outside the playable map.";
        if (category == "InnerBeachBlend")
            return "Beach blend/land-edge prefabs currently mixed into the island interior.";
        return "Foundation renderers not classified by current naming rules.";
    }

    private static BeachPlacementInfo BuildBeachPlacementInfo(Transform foundation, CategoryBounds mapTarget)
    {
        int total = 0;
        int centersInsideMap = 0;
        foreach (Transform transform in foundation.GetComponentsInChildren<Transform>(true))
        {
            if (!IsPlacedBeachPrefab(transform.name))
                continue;

            total++;
            Vector3 position = transform.position;
            if (position.x >= mapTarget.XMin && position.x <= mapTarget.XMax && position.z >= mapTarget.ZMin && position.z <= mapTarget.ZMax)
                centersInsideMap++;
        }

        return new BeachPlacementInfo(total, centersInsideMap);
    }

    private static bool IsPlacedBeachPrefab(string objectName)
    {
        return objectName.StartsWith("BeachCoast_", StringComparison.Ordinal)
            || objectName.StartsWith("BeachBlend_", StringComparison.Ordinal)
            || objectName.StartsWith("BeachInner_", StringComparison.Ordinal)
            || objectName.StartsWith("BeachLandEdge_", StringComparison.Ordinal);
    }

    private static void WriteJson(CategoryBounds foundationOverall, CategoryBounds mapTarget, List<CategoryBounds> foundationCategories, CategoryBounds generatedOverall, List<CategoryBounds> generatedCategories, BeachPlacementInfo beachPlacement)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"auditId\": \"GameTerrain4_PlayableLandFootprintAudit\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"mapTarget\": " + CategoryToJson(mapTarget, 1) + ",");
        json.AppendLine("  \"foundationOverall\": " + CategoryToJson(foundationOverall, 1) + ",");
        json.AppendLine("  \"foundationVsMapShortfall\": " + ShortfallToJson(foundationOverall, mapTarget) + ",");
        json.AppendLine("  \"foundationCategories\": [");
        for (int i = 0; i < foundationCategories.Count; i++)
        {
            string comma = i == foundationCategories.Count - 1 ? string.Empty : ",";
            json.AppendLine("    " + CategoryToJson(foundationCategories[i], 2) + comma);
        }
        json.AppendLine("  ],");
        json.AppendLine("  \"beachPlacementVsMap\": " + BeachPlacementToJson(beachPlacement) + ",");
        json.AppendLine("  \"generatedOverall\": " + CategoryToJson(generatedOverall, 1) + ",");
        json.AppendLine("  \"generatedCategories\": [");
        for (int i = 0; i < generatedCategories.Count; i++)
        {
            string comma = i == generatedCategories.Count - 1 ? string.Empty : ",";
            json.AppendLine("    " + CategoryToJson(generatedCategories[i], 2) + comma);
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(AuditJsonPath), json.ToString());
    }

    private static void WriteReport(CategoryBounds foundationOverall, CategoryBounds mapTarget, List<CategoryBounds> foundationCategories, CategoryBounds generatedOverall, List<CategoryBounds> generatedCategories, BeachPlacementInfo beachPlacement)
    {
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(AuditReportPath)));

        CategoryBounds green = FindCategory(foundationCategories, "GreenPlayableGround");
        CategoryBounds innerBeach = FindCategory(foundationCategories, "InnerBeachBlend");
        CategoryBounds outerBeach = FindCategory(foundationCategories, "OuterBeachCoast");

        StringBuilder report = new();
        report.AppendLine("# Game_Terrain4 Playable Land Footprint Audit");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-25");
        report.AppendLine();
        report.AppendLine("Step: 1/2/3 - audit current island footprint, verify the larger-island foundation rebuild, and confirm remapped dressing bounds.");
        report.AppendLine();
        bool foundationCoversTarget = ShortfallWest(foundationOverall, mapTarget) <= 0f
            && ShortfallEast(foundationOverall, mapTarget) <= 0f
            && ShortfallSouth(foundationOverall, mapTarget) <= 0f
            && ShortfallNorth(foundationOverall, mapTarget) <= 0f;
        bool greenCoversTarget = ShortfallWest(green, mapTarget) <= 0f
            && ShortfallEast(green, mapTarget) <= 0f
            && ShortfallSouth(green, mapTarget) <= 0f
            && ShortfallNorth(green, mapTarget) <= 0f;
        if (foundationCoversTarget && greenCoversTarget)
            report.AppendLine("Conclusion: the rebuilt island foundation covers the 2024x2024 gameplay image with green/dirt playable terrain, and generated dressing has been remapped to that playable-land contract.");
        else
            report.AppendLine("Conclusion: the current island foundation is still smaller than the 2024x2024 gameplay image, or the usable green/dirt playable land does not fully cover it.");
        report.AppendLine();
        report.AppendLine("Target map footprint:");
        AppendCategory(report, mapTarget);
        report.AppendLine();
        report.AppendLine("Current foundation footprint:");
        AppendCategory(report, foundationOverall);
        report.AppendLine("- Shortfall versus target: west " + ShortfallWest(foundationOverall, mapTarget).ToString("0.###", CultureInfo.InvariantCulture) + ", east " + ShortfallEast(foundationOverall, mapTarget).ToString("0.###", CultureInfo.InvariantCulture) + ", south " + ShortfallSouth(foundationOverall, mapTarget).ToString("0.###", CultureInfo.InvariantCulture) + ", north " + ShortfallNorth(foundationOverall, mapTarget).ToString("0.###", CultureInfo.InvariantCulture));
        report.AppendLine();
        report.AppendLine("Current foundation categories:");
        AppendCategory(report, green);
        AppendCategory(report, innerBeach);
        AppendCategory(report, outerBeach);
        report.AppendLine("- Beach prefab centers inside 2024 gameplay map: " + beachPlacement.CentersInsideMap.ToString(CultureInfo.InvariantCulture) + " / " + beachPlacement.TotalBeachPrefabs.ToString(CultureInfo.InvariantCulture));
        foreach (CategoryBounds category in foundationCategories)
        {
            if (category.Id != "GreenPlayableGround" && category.Id != "InnerBeachBlend" && category.Id != "OuterBeachCoast")
                AppendCategory(report, category);
        }
        report.AppendLine();
        report.AppendLine("Generated mask/dressing footprint:");
        AppendCategory(report, generatedOverall);
        foreach (CategoryBounds category in generatedCategories)
            AppendCategory(report, category);
        report.AppendLine();
        report.AppendLine("Design implication for step 4:");
        report.AppendLine("- Regenerate `Game_Terrain4` from the larger island foundation plus the remapped mask dressing contract whenever the map art changes.");
        report.AppendLine("- Keep beach/coast prefabs as visual border content outside the gameplay contract.");
        report.AppendLine("- Keep generated dressing/reserve checks tied to the 2024 playable map footprint, not the full foundation/beach footprint.");
        report.AppendLine();
        report.AppendLine("Data: `" + AuditJsonPath + "`");

        File.WriteAllText(ProjectPath(AuditReportPath), report.ToString());
    }

    private static void AppendCategory(StringBuilder report, CategoryBounds category)
    {
        report.AppendLine("- `" + category.Id + "`: count " + category.RendererCount.ToString(CultureInfo.InvariantCulture)
            + ", width " + category.Width.ToString("0.###", CultureInfo.InvariantCulture)
            + ", depth " + category.Depth.ToString("0.###", CultureInfo.InvariantCulture)
            + ", x [" + category.XMin.ToString("0.###", CultureInfo.InvariantCulture) + ", " + category.XMax.ToString("0.###", CultureInfo.InvariantCulture) + "]"
            + ", z [" + category.ZMin.ToString("0.###", CultureInfo.InvariantCulture) + ", " + category.ZMax.ToString("0.###", CultureInfo.InvariantCulture) + "]");
    }

    private static string CategoryToJson(CategoryBounds category, int indentLevel)
    {
        return "{ \"id\": \"" + EscapeJson(category.Id) + "\", \"description\": \"" + EscapeJson(category.Description) + "\", \"rendererCount\": " + category.RendererCount.ToString(CultureInfo.InvariantCulture)
            + ", \"xMin\": " + category.XMin.ToString("0.###", CultureInfo.InvariantCulture)
            + ", \"xMax\": " + category.XMax.ToString("0.###", CultureInfo.InvariantCulture)
            + ", \"zMin\": " + category.ZMin.ToString("0.###", CultureInfo.InvariantCulture)
            + ", \"zMax\": " + category.ZMax.ToString("0.###", CultureInfo.InvariantCulture)
            + ", \"width\": " + category.Width.ToString("0.###", CultureInfo.InvariantCulture)
            + ", \"depth\": " + category.Depth.ToString("0.###", CultureInfo.InvariantCulture) + " }";
    }

    private static string BeachPlacementToJson(BeachPlacementInfo beachPlacement)
    {
        return "{ \"totalBeachPrefabs\": " + beachPlacement.TotalBeachPrefabs.ToString(CultureInfo.InvariantCulture)
            + ", \"centersInsideMap\": " + beachPlacement.CentersInsideMap.ToString(CultureInfo.InvariantCulture)
            + ", \"passed\": " + (beachPlacement.CentersInsideMap == 0 ? "true" : "false") + " }";
    }

    private static string ShortfallToJson(CategoryBounds measured, CategoryBounds target)
    {
        return "{ \"west\": " + ShortfallWest(measured, target).ToString("0.###", CultureInfo.InvariantCulture)
            + ", \"east\": " + ShortfallEast(measured, target).ToString("0.###", CultureInfo.InvariantCulture)
            + ", \"south\": " + ShortfallSouth(measured, target).ToString("0.###", CultureInfo.InvariantCulture)
            + ", \"north\": " + ShortfallNorth(measured, target).ToString("0.###", CultureInfo.InvariantCulture) + " }";
    }

    private static float ShortfallWest(CategoryBounds measured, CategoryBounds target)
    {
        return Mathf.Max(0f, measured.XMin - target.XMin);
    }

    private static float ShortfallEast(CategoryBounds measured, CategoryBounds target)
    {
        return Mathf.Max(0f, target.XMax - measured.XMax);
    }

    private static float ShortfallSouth(CategoryBounds measured, CategoryBounds target)
    {
        return Mathf.Max(0f, measured.ZMin - target.ZMin);
    }

    private static float ShortfallNorth(CategoryBounds measured, CategoryBounds target)
    {
        return Mathf.Max(0f, target.ZMax - measured.ZMax);
    }

    private static CategoryBounds FindCategory(List<CategoryBounds> categories, string id)
    {
        foreach (CategoryBounds category in categories)
        {
            if (category.Id == id)
                return category;
        }

        return CategoryBounds.Empty(id, "Category not found.");
    }

    private static GameObject FindRootGameObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath ?? string.Empty);
    }

    private readonly struct CategoryBounds
    {
        public readonly string Id;
        public readonly string Description;
        public readonly int RendererCount;
        public readonly float XMin;
        public readonly float XMax;
        public readonly float ZMin;
        public readonly float ZMax;

        public CategoryBounds(string id, string description, int rendererCount, float xMin, float xMax, float zMin, float zMax)
        {
            Id = id;
            Description = description;
            RendererCount = rendererCount;
            XMin = xMin;
            XMax = xMax;
            ZMin = zMin;
            ZMax = zMax;
        }

        public float Width => Mathf.Max(0f, XMax - XMin);
        public float Depth => Mathf.Max(0f, ZMax - ZMin);

        public static CategoryBounds Empty(string id, string description)
        {
            return new CategoryBounds(id, description, 0, 0f, 0f, 0f, 0f);
        }

        public static CategoryBounds FromRect(string id, string description, float xMin, float zMin, float xMax, float zMax, int rendererCount)
        {
            return new CategoryBounds(id, description, rendererCount, xMin, xMax, zMin, zMax);
        }
    }

    private readonly struct BeachPlacementInfo
    {
        public readonly int TotalBeachPrefabs;
        public readonly int CentersInsideMap;

        public BeachPlacementInfo(int totalBeachPrefabs, int centersInsideMap)
        {
            TotalBeachPrefabs = totalBeachPrefabs;
            CentersInsideMap = centersInsideMap;
        }
    }

    private sealed class CategoryAccumulator
    {
        private readonly string id;
        private readonly string description;
        private Bounds bounds;
        private bool hasBounds;

        public int RendererCount { get; private set; }

        public CategoryAccumulator(string id, string description)
        {
            this.id = id;
            this.description = description;
        }

        public void Add(Renderer renderer)
        {
            RendererCount++;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        public void Add(CategoryBounds category)
        {
            if (category.RendererCount == 0)
                return;

            RendererCount += category.RendererCount;
            Bounds categoryBounds = new(
                new Vector3((category.XMin + category.XMax) * 0.5f, 0f, (category.ZMin + category.ZMax) * 0.5f),
                new Vector3(category.Width, 0f, category.Depth));
            if (!hasBounds)
            {
                bounds = categoryBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(categoryBounds);
            }
        }

        public CategoryBounds ToBounds()
        {
            if (!hasBounds)
                return CategoryBounds.Empty(id, description);

            return new CategoryBounds(
                id,
                description,
                RendererCount,
                bounds.min.x,
                bounds.max.x,
                bounds.min.z,
                bounds.max.z);
        }
    }
}
#endif
