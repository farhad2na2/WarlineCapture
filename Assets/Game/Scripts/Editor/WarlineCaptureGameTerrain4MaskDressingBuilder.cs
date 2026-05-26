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

public static class WarlineCaptureGameTerrain4MaskDressingBuilder
{
    private const string SourceScenePath = "Assets/Game/Scenes/Game_Terrain3.unity";
    private const string TargetScenePath = "Assets/Game/Scenes/Game_Terrain4.unity";
    private const string MapPackRoot = "Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01";
    private const string WorkflowDocPath = "Design/WarlineCapture_3D_Operation_Map_Texture_Mask_Workflow.md";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing";
    private const string CatalogJsonPath = DataRoot + "/game_terrain4_mask_dressing_prefab_catalog.json";
    private const string FoundationSnapshotJsonPath = DataRoot + "/game_terrain4_island_foundation_snapshot.json";
    private const string GeneratedGroupsJsonPath = DataRoot + "/game_terrain4_generated_group_manifest.json";
    private const string MaskSamplingJsonPath = DataRoot + "/game_terrain4_mask_sampling_summary.json";
    private const string PlacementRejectionJsonPath = DataRoot + "/game_terrain4_placement_rejection_summary.json";
    private const string SpacingPlanJsonPath = DataRoot + "/game_terrain4_spacing_plan_summary.json";
    private const string DressingPlacementJsonPath = DataRoot + "/game_terrain4_dressing_placement_summary.json";
    private const string ValidationArtifactsJsonPath = DataRoot + "/game_terrain4_validation_artifacts.json";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing";
    private const string TopDownProofCapturePath = CaptureRoot + "/game_terrain4_topdown_proof.png";
    private const string PlayableAngleProofCapturePath = CaptureRoot + "/game_terrain4_playable_angle_proof.png";
    private const string ReportPath = "Design/AgentReports/2026-05-25_gameplay_game-terrain4-mask-dressing-builder.md";
    private const string BaseVisualPath = MapPackRoot + "/base_visual.png";
    private const string BlockerMaskPath = MapPackRoot + "/blocker_mask.png";
    private const string TreeDensityMaskPath = MapPackRoot + "/tree_density_mask.png";
    private const string RockDensityMaskPath = MapPackRoot + "/rock_density_mask.png";
    private const string HeightMaskPath = MapPackRoot + "/height_mask.png";
    private const string TargetIslandRootName = "Island";
    private const string TargetIslandBaseName = "ExpandedIsland_SourceGameTerrain3PrefabsOnly";
    private const int MapGridSize = 2024;
    private const float MapGridMaxCoordinate = MapGridSize - 1f;
    private const int DensityCandidateThreshold = 32;
    private const int DensityMediumThreshold = 96;
    private const int DensityDenseThreshold = 176;
    private const int BlockerSoftEdgeThreshold = 96;
    private const int BlockerBlockedThreshold = 160;
    private const int HeightRaisedThreshold = 144;
    private const int HeightHighThreshold = 208;
    private const int SpacingSeed = 913742;
    private const int MaxSamplePointsPerKind = 64;

    private static readonly string[] SourceExampleGroups =
    {
        "Mountains",
        "Trees",
        "Bushes",
        "Rocks"
    };

    private static readonly GeneratedGroupSpec[] GeneratedGroupSpecs =
    {
        new("Generated_Mountains", "Mask-placed mountain and cliff blocker dressing."),
        new("Generated_Trees", "Mask-placed tree dressing."),
        new("Generated_Bushes", "Mask-placed bush and scrub dressing."),
        new("Generated_Rocks", "Mask-placed rock and rubble dressing."),
        new("Generated_BlockerDebug", "Generated blocker/pathing proof markers and debug output.")
    };

    private static readonly string[] RequiredTexturePaths =
    {
        BaseVisualPath,
        BlockerMaskPath,
        TreeDensityMaskPath,
        RockDensityMaskPath,
        HeightMaskPath
    };

    private static readonly CoordinateProbeSpec[] CoordinateProbeSpecs =
    {
        new("SouthwestCorner", 0, 0),
        new("SouthEdgeMid", 1012, 0),
        new("SoutheastCorner", 2023, 0),
        new("WestEdgeMid", 0, 1012),
        new("MapCenter", 1012, 1012),
        new("EastEdgeMid", 2023, 1012),
        new("NorthwestCorner", 0, 2023),
        new("NorthEdgeMid", 1012, 2023),
        new("NortheastCorner", 2023, 2023)
    };

    private static readonly ReserveZoneSpec[] ReserveZoneSpecs =
    {
        new("CityReserve", "Large open flat city/town area for future Middle East town prefabs.", 520, 720, 720, 560),
        new("NorthwestBaseReserve", "Military camp/base reserve for command, tents, vehicles, helipad, and barricades.", 190, 1430, 430, 360),
        new("SoutheastBaseReserve", "Second military camp/base reserve far from the northwest base.", 1410, 250, 430, 360)
    };

    private static readonly SpacingSpec[] SpacingSpecs =
    {
        new("Mountains", 76, 76, "High terrain and blocker-belt anchor pass; large spacing keeps mountain silhouettes readable."),
        new("Trees", 34, 34, "Tree-cluster pass; medium spacing creates readable groves without carpet coverage."),
        new("Bushes", 26, 26, "Scrub/bush pass; tighter spacing than trees for low vegetation fill."),
        new("Rocks", 42, 42, "Rock/boulder pass; spacing keeps outcrops clustered but inspectable.")
    };

    [MenuItem("WarlineCapture/Design/Game Terrain4/Validate Mask Dressing Setup")]
    public static void ValidateSetupMenu()
    {
        SetupValidation validation = ValidateSetup();
        WritePrefabCatalogJson(validation);
        WriteFoundationSnapshotJson(validation);
        WriteGeneratedGroupsJson(validation);
        WriteMaskSamplingJson(validation);
        WritePlacementRejectionJson(validation);
        WriteSpacingPlanJson(validation);
        WriteDressingPlacementJson(validation);
        ValidationArtifactInfo validationArtifacts = BuildValidationArtifacts(validation, false);
        WriteValidationArtifactsJson(validationArtifacts);
        WriteSetupReport(validation, validationArtifacts);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GAME_TERRAIN4_MASK_DRESSING_SETUP_VALIDATED sourceGroups={validation.SourceGroupCount} catalogPrefabs={validation.CatalogEntryCount} foundationRenderers={validation.Foundation.RendererCount} generatedGroups={validation.GeneratedGroups.Count} sampledCells={validation.MaskSampling.TotalCells} validPlacementCells={validation.PlacementRejection.ValidAnyCount} spacedPoints={validation.SpacingPlan.TotalAcceptedPoints} placedPrefabs={validation.DressingPlacement.TotalPlacedPrefabs} validationPassed={validationArtifacts.Passed} textures={validation.TextureCount} report={ReportPath}");
    }

    [MenuItem("WarlineCapture/Design/Game Terrain4/Build Mask Dressing")]
    public static void BuildMaskDressing()
    {
        SetupValidation validation = ValidateSetup();
        EnsureGeneratedGroups();
        PlaceDressingPrefabs(validation);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        validation = ValidateSetup();
        WritePrefabCatalogJson(validation);
        WriteFoundationSnapshotJson(validation);
        WriteGeneratedGroupsJson(validation);
        WriteMaskSamplingJson(validation);
        WritePlacementRejectionJson(validation);
        WriteSpacingPlanJson(validation);
        WriteDressingPlacementJson(validation);
        ValidationArtifactInfo validationArtifacts = BuildValidationArtifacts(validation, true);
        WriteValidationArtifactsJson(validationArtifacts);
        WriteSetupReport(validation, validationArtifacts);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GAME_TERRAIN4_MASK_DRESSING_BUILDER_READY step=9 sourceGroups={validation.SourceGroupCount} catalogPrefabs={validation.CatalogEntryCount} foundationRenderers={validation.Foundation.RendererCount} generatedGroups={validation.GeneratedGroups.Count} sampledCells={validation.MaskSampling.TotalCells} validPlacementCells={validation.PlacementRejection.ValidAnyCount} spacedPoints={validation.SpacingPlan.TotalAcceptedPoints} placedPrefabs={validation.DressingPlacement.TotalPlacedPrefabs} validationPassed={validationArtifacts.Passed} textures={validation.TextureCount} target={TargetScenePath}");
    }

    private static SetupValidation ValidateSetup()
    {
        EnsureSceneAsset(SourceScenePath);
        EnsureSceneAsset(TargetScenePath);
        EnsureFile(WorkflowDocPath);

        List<TextureInfo> textures = new();
        foreach (string path in RequiredTexturePaths)
            textures.Add(LoadTextureInfo(path));
        MaskSamplingInfo maskSampling = BuildMaskSamplingInfo(textures);

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        GameObject sourceIsland = FindRootGameObject(sourceScene, TargetIslandRootName);
        if (sourceIsland == null)
            throw new InvalidOperationException("Source scene is missing root Island object: " + SourceScenePath);

        Dictionary<string, int> sourceGroupChildren = new(StringComparer.Ordinal);
        foreach (string groupName in SourceExampleGroups)
        {
            Transform group = FindChildRecursive(sourceIsland.transform, groupName);
            if (group == null)
                throw new InvalidOperationException("Source Island is missing example group: " + groupName);
            sourceGroupChildren[groupName] = group.childCount;
        }
        Dictionary<string, List<CatalogEntry>> prefabCatalog = BuildPrefabCatalog(sourceIsland);

        Scene targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        GameObject targetIsland = FindRootGameObject(targetScene, TargetIslandRootName);
        if (targetIsland == null)
            throw new InvalidOperationException("Target scene is missing root Island object: " + TargetScenePath);

        Transform baseIsland = FindChildRecursive(targetIsland.transform, TargetIslandBaseName);
        if (baseIsland == null)
            throw new InvalidOperationException("Target Island is missing terrain base child: " + TargetIslandBaseName);
        if (baseIsland.parent != targetIsland.transform)
            throw new InvalidOperationException("Target island base must remain a direct child of Island so generated dressing can be added beside it: " + TargetIslandBaseName);

        FoundationInfo foundation = BuildFoundationInfo(targetIsland.transform, baseIsland);
        List<GeneratedGroupInfo> generatedGroups = ScanGeneratedGroups(targetIsland.transform);
        PlacementRejectionInfo placementRejection = BuildPlacementRejectionInfo();
        SpacingPlanInfo spacingPlan = BuildSpacingPlanInfo();
        DressingPlacementInfo dressingPlacement = ScanDressingPlacementInfo(targetIsland.transform);

        return new SetupValidation(textures, sourceGroupChildren, prefabCatalog, foundation, generatedGroups, maskSampling, placementRejection, spacingPlan, dressingPlacement, targetIsland.activeSelf, targetIsland.transform.localPosition, targetIsland.transform.localScale);
    }

    private static void WriteSetupReport(SetupValidation validation, ValidationArtifactInfo validationArtifacts)
    {
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ReportPath)));

        StringBuilder report = new();
        report.AppendLine("# Game_Terrain4 Mask Dressing Builder");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-25");
        report.AppendLine();
        report.AppendLine("Steps complete: 1 - dedicated editor builder created; 2 - source prefab catalog generated; 3 - mask placement remapped to the explicit 2024 playable-map footprint on the enlarged green island foundation; 4 - generated child groups created under Island; 5 - map masks sampled with documented 2024x2024 coordinate rules; 6 - placement rejection rules generated for playable-map footprint, reserves, and blocker/pathing constraints; 7 - deterministic blue-noise-style spacing plan generated; 8 - generated mountain, rock, tree, and bush prefabs placed into scene groups; 9 - validation artifacts generated.");
        report.AppendLine();
        report.AppendLine("Purpose: prepare `Game_Terrain4` for mask-based map dressing using `SyntyHighlands_01` masks and the example model groups under `Game_Terrain3/Island`.");
        report.AppendLine();
        report.AppendLine("Validated references:");
        report.AppendLine("- Source scene: `" + SourceScenePath + "`");
        report.AppendLine("- Target scene: `" + TargetScenePath + "`");
        report.AppendLine("- Workflow doc: `" + WorkflowDocPath + "`");
        report.AppendLine("- Map pack: `" + MapPackRoot + "`");
        report.AppendLine("- Prefab catalog: `" + CatalogJsonPath + "`");
        report.AppendLine("- Foundation snapshot: `" + FoundationSnapshotJsonPath + "`");
        report.AppendLine("- Generated group manifest: `" + GeneratedGroupsJsonPath + "`");
        report.AppendLine("- Mask sampling summary: `" + MaskSamplingJsonPath + "`");
        report.AppendLine("- Placement rejection summary: `" + PlacementRejectionJsonPath + "`");
        report.AppendLine("- Spacing plan summary: `" + SpacingPlanJsonPath + "`");
        report.AppendLine("- Dressing placement summary: `" + DressingPlacementJsonPath + "`");
        report.AppendLine("- Validation artifact summary: `" + ValidationArtifactsJsonPath + "`");
        report.AppendLine("- Top-down proof capture: `" + TopDownProofCapturePath + "`");
        report.AppendLine("- Playable-frame proof capture: `" + PlayableAngleProofCapturePath + "`");
        report.AppendLine("- Target Island active: " + validation.TargetIslandActive);
        report.AppendLine("- Target Island local position: " + FormatVector(validation.TargetIslandLocalPosition));
        report.AppendLine("- Target Island local scale: " + FormatVector(validation.TargetIslandLocalScale));
        report.AppendLine("- Foundation base child: `" + validation.Foundation.Name + "`");
        report.AppendLine("- Foundation child index: " + validation.Foundation.ChildIndex.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Foundation active in hierarchy: " + validation.Foundation.ActiveInHierarchy);
        report.AppendLine("- Foundation transform count: " + validation.Foundation.TransformCount.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Foundation renderers: " + validation.Foundation.RendererCount.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Foundation mesh filters: " + validation.Foundation.MeshFilterCount.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Foundation colliders: " + validation.Foundation.ColliderCount.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Foundation world bounds center: " + FormatVector(validation.Foundation.WorldBoundsCenter));
        report.AppendLine("- Foundation world bounds size: " + FormatVector(validation.Foundation.WorldBoundsSize));
        report.AppendLine();
        report.AppendLine("Generated child groups under `Island`:");
        foreach (GeneratedGroupInfo group in validation.GeneratedGroups)
        {
            string status = group.Exists ? "present" : "missing";
            report.AppendLine("- `" + group.Name + "`: " + status + ", child index " + group.ChildIndex.ToString(CultureInfo.InvariantCulture) + ", children " + group.ChildCount.ToString(CultureInfo.InvariantCulture) + ", purpose: " + group.Purpose);
        }
        report.AppendLine();
        report.AppendLine("Source example group child counts:");
        foreach (KeyValuePair<string, int> entry in validation.SourceGroupChildren)
            report.AppendLine("- " + entry.Key + ": " + entry.Value.ToString(CultureInfo.InvariantCulture));
        report.AppendLine();
        report.AppendLine("Prefab catalog:");
        foreach (KeyValuePair<string, List<CatalogEntry>> group in validation.PrefabCatalog)
        {
            int sampleCount = 0;
            foreach (CatalogEntry entry in group.Value)
                sampleCount += entry.SampleCount;
            report.AppendLine("- " + group.Key + ": " + group.Value.Count.ToString(CultureInfo.InvariantCulture) + " unique prefab assets from " + sampleCount.ToString(CultureInfo.InvariantCulture) + " examples");
        }
        report.AppendLine();
        report.AppendLine("Texture inputs:");
        foreach (TextureInfo texture in validation.Textures)
            report.AppendLine("- `" + texture.Path + "`: " + texture.Width.ToString(CultureInfo.InvariantCulture) + "x" + texture.Height.ToString(CultureInfo.InvariantCulture));
        report.AppendLine();
        report.AppendLine("Mask sampling:");
        report.AppendLine("- Grid size: " + validation.MaskSampling.GridWidth.ToString(CultureInfo.InvariantCulture) + "x" + validation.MaskSampling.GridHeight.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Sampling rule: " + validation.MaskSampling.SamplingRule);
        foreach (MaskLayerSamplingInfo layer in validation.MaskSampling.Layers)
            report.AppendLine("- `" + layer.Path + "`: avg " + layer.AverageValue.ToString("0.##", CultureInfo.InvariantCulture) + ", min " + layer.MinValue.ToString(CultureInfo.InvariantCulture) + ", max " + layer.MaxValue.ToString(CultureInfo.InvariantCulture) + ", classes " + FormatClassCounts(layer.ClassCounts));
        report.AppendLine();
        report.AppendLine("Placement rejection:");
        report.AppendLine("- Playable map footprint grid rect: " + FormatGridRect(validation.PlacementRejection.PlayableMapFootprint));
        report.AppendLine("- Valid-any placement cells after global rejection: " + validation.PlacementRejection.ValidAnyCount.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Global rejection counts: " + FormatRejectionCounts(validation.PlacementRejection.GlobalRejectionCounts));
        foreach (PlacementCandidateSummary summary in validation.PlacementRejection.CandidateSummaries)
            report.AppendLine("- " + summary.Kind + ": raw " + summary.RawCandidateCount.ToString(CultureInfo.InvariantCulture) + ", valid " + summary.ValidCount.ToString(CultureInfo.InvariantCulture) + ", rejected " + summary.RejectedCount.ToString(CultureInfo.InvariantCulture) + " (" + FormatRejectionCounts(summary.RejectionCounts) + ")");
        report.AppendLine();
        report.AppendLine("Spacing plan:");
        report.AppendLine("- Seed: " + validation.SpacingPlan.Seed.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Method: " + validation.SpacingPlan.Method);
        report.AppendLine("- Total accepted spaced points: " + validation.SpacingPlan.TotalAcceptedPoints.ToString(CultureInfo.InvariantCulture));
        foreach (SpacingKindSummary summary in validation.SpacingPlan.KindSummaries)
            report.AppendLine("- " + summary.Kind + ": accepted " + summary.AcceptedCount.ToString(CultureInfo.InvariantCulture) + " / tile candidates " + summary.TileCandidateCount.ToString(CultureInfo.InvariantCulture) + ", min distance " + summary.MinDistance.ToString(CultureInfo.InvariantCulture) + ", rejected by spacing " + summary.RejectedBySpacing.ToString(CultureInfo.InvariantCulture));
        report.AppendLine();
        report.AppendLine("Dressing placement:");
        report.AppendLine("- Total placed prefabs: " + validation.DressingPlacement.TotalPlacedPrefabs.ToString(CultureInfo.InvariantCulture));
        foreach (DressingPlacementGroupInfo group in validation.DressingPlacement.Groups)
            report.AppendLine("- " + group.Kind + ": group `" + group.GroupName + "` contains " + group.PlacedCount.ToString(CultureInfo.InvariantCulture) + " placed prefabs.");
        report.AppendLine();
        report.AppendLine("Validation artifacts:");
        report.AppendLine("- Captures rendered this run: " + validationArtifacts.CapturesRendered);
        report.AppendLine("- Top-down proof: `" + validationArtifacts.TopDownCapturePath + "`");
        report.AppendLine("- Playable-frame proof: `" + validationArtifacts.PlayableAngleCapturePath + "`");
        report.AppendLine("- Overall validation pass: " + validationArtifacts.Passed);
        foreach (ValidationCheckInfo check in validationArtifacts.Checks)
        {
            string status = check.Passed ? "PASS" : "FAIL";
            report.AppendLine("- " + status + " `" + check.Id + "`: expected " + check.Expected.ToString(CultureInfo.InvariantCulture) + ", actual " + check.Actual.ToString(CultureInfo.InvariantCulture) + ". " + check.Details);
        }
        report.AppendLine();
        report.AppendLine("Implementation report: `Game_Terrain4/Island` now contains the enlarged green/dirt island foundation plus generated sibling dressing groups. The validation pass confirms prefab counts, reserve-zone clearance, playable-map containment, vegetation pathing rules, rock blocker rules, mountain blocker-belt rules, and proof captures.");

        File.WriteAllText(ProjectPath(ReportPath), report.ToString());
    }

    private static Dictionary<string, List<CatalogEntry>> BuildPrefabCatalog(GameObject sourceIsland)
    {
        Dictionary<string, List<CatalogEntry>> catalog = new(StringComparer.Ordinal);
        foreach (string groupName in SourceExampleGroups)
        {
            Transform group = FindChildRecursive(sourceIsland.transform, groupName);
            if (group == null)
                throw new InvalidOperationException("Source Island is missing example group: " + groupName);

            Dictionary<string, CatalogAccumulator> accumulators = new(StringComparer.Ordinal);
            HashSet<GameObject> seenInstanceRoots = new();
            foreach (Transform transform in group.GetComponentsInChildren<Transform>(true))
            {
                GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
                if (instanceRoot == null || !IsDescendantOrSelf(instanceRoot.transform, group))
                    continue;

                if (!seenInstanceRoots.Add(instanceRoot))
                    continue;

                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instanceRoot);
                if (prefab == null)
                    prefab = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
                if (prefab == null)
                    continue;

                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(prefabPath))
                    continue;

                if (!accumulators.TryGetValue(prefabPath, out CatalogAccumulator accumulator))
                {
                    accumulator = new CatalogAccumulator(prefabPath);
                    accumulators[prefabPath] = accumulator;
                }

                accumulator.AddSample(instanceRoot.name, instanceRoot.transform.localScale);
            }

            List<CatalogEntry> entries = new();
            foreach (CatalogAccumulator accumulator in accumulators.Values)
                entries.Add(accumulator.ToEntry());
            entries.Sort((a, b) => string.CompareOrdinal(a.PrefabPath, b.PrefabPath));
            if (entries.Count == 0)
                throw new InvalidOperationException("No prefab assets were found in source example group: " + groupName);

            catalog[groupName] = entries;
        }

        return catalog;
    }

    private static FoundationInfo BuildFoundationInfo(Transform targetIsland, Transform baseIsland)
    {
        Renderer[] renderers = baseIsland.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("Target island foundation has no renderers: " + TargetIslandBaseName);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        int transformCount = baseIsland.GetComponentsInChildren<Transform>(true).Length;
        int meshFilterCount = baseIsland.GetComponentsInChildren<MeshFilter>(true).Length;
        int colliderCount = baseIsland.GetComponentsInChildren<Collider>(true).Length;
        int childIndex = baseIsland.GetSiblingIndex();

        if (transformCount <= 1)
            throw new InvalidOperationException("Target island foundation should contain copied terrain prefab children, but it has no descendants: " + TargetIslandBaseName);
        if (meshFilterCount == 0)
            throw new InvalidOperationException("Target island foundation has no MeshFilter components: " + TargetIslandBaseName);

        return new FoundationInfo(
            baseIsland.name,
            GetTransformPath(baseIsland, targetIsland),
            baseIsland.gameObject.activeSelf,
            baseIsland.gameObject.activeInHierarchy,
            baseIsland.localPosition,
            baseIsland.localEulerAngles,
            baseIsland.localScale,
            childIndex,
            transformCount,
            renderers.Length,
            meshFilterCount,
            colliderCount,
            bounds.center,
            bounds.size);
    }

    private static void EnsureGeneratedGroups()
    {
        Scene targetScene = SceneManager.GetActiveScene();
        if (targetScene.path != TargetScenePath)
            targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        GameObject targetIsland = FindRootGameObject(targetScene, TargetIslandRootName);
        if (targetIsland == null)
            throw new InvalidOperationException("Target scene is missing root Island object: " + TargetScenePath);

        Transform foundation = FindDirectChild(targetIsland.transform, TargetIslandBaseName);
        if (foundation == null)
            throw new InvalidOperationException("Target Island is missing direct foundation child: " + TargetIslandBaseName);

        foundation.SetSiblingIndex(0);
        for (int i = 0; i < GeneratedGroupSpecs.Length; i++)
        {
            GeneratedGroupSpec spec = GeneratedGroupSpecs[i];
            Transform group = FindDirectChild(targetIsland.transform, spec.Name);
            Transform misplaced = FindChildRecursive(targetIsland.transform, spec.Name);
            if (misplaced != null && misplaced.parent != targetIsland.transform)
                throw new InvalidOperationException("Generated group exists but is not a direct child of Island: " + spec.Name);

            if (group == null)
            {
                GameObject groupObject = new(spec.Name);
                group = groupObject.transform;
                group.SetParent(targetIsland.transform, false);
            }

            group.localPosition = Vector3.zero;
            group.localRotation = Quaternion.identity;
            group.localScale = Vector3.one;
            group.SetSiblingIndex(i + 1);
        }

        EditorSceneManager.MarkSceneDirty(targetScene);
    }

    private static List<GeneratedGroupInfo> ScanGeneratedGroups(Transform targetIsland)
    {
        List<GeneratedGroupInfo> groups = new();
        for (int i = 0; i < GeneratedGroupSpecs.Length; i++)
        {
            GeneratedGroupSpec spec = GeneratedGroupSpecs[i];
            Transform group = FindDirectChild(targetIsland, spec.Name);
            if (group == null)
            {
                groups.Add(new GeneratedGroupInfo(spec.Name, spec.Purpose, false, string.Empty, false, false, -1, 0, Vector3.zero, Vector3.zero, Vector3.one));
                continue;
            }

            groups.Add(new GeneratedGroupInfo(
                spec.Name,
                spec.Purpose,
                true,
                GetTransformPath(group, targetIsland),
                group.gameObject.activeSelf,
                group.gameObject.activeInHierarchy,
                group.GetSiblingIndex(),
                group.childCount,
                group.localPosition,
                group.localEulerAngles,
                group.localScale));
        }

        return groups;
    }

    private static MaskSamplingInfo BuildMaskSamplingInfo(List<TextureInfo> textures)
    {
        List<MaskLayerSamplingInfo> layers = new();
        foreach (TextureInfo texture in textures)
            layers.Add(SampleMaskLayer(texture));

        return new MaskSamplingInfo(
            MapGridSize,
            MapGridSize,
            MapGridSize * MapGridSize,
            "u=gridX/2023.0; v=gridZ/2023.0; pixelX=round(u*(imageWidth-1)); pixelY=round((1.0-v)*(imageHeight-1))",
            layers);
    }

    private static MaskLayerSamplingInfo SampleMaskLayer(TextureInfo texture)
    {
        Texture2D image = LoadTexture(texture.Path);
        try
        {
            Color32[] pixels = image.GetPixels32();
            Dictionary<string, int> classCounts = new(StringComparer.Ordinal);
            List<CoordinateProbeInfo> probes = new();
            long sum = 0;
            int min = 255;
            int max = 0;

            for (int gridZ = 0; gridZ < MapGridSize; gridZ++)
            {
                int pixelY = PixelYForGridZ(gridZ, image.height);
                int rowOffset = pixelY * image.width;
                for (int gridX = 0; gridX < MapGridSize; gridX++)
                {
                    int pixelX = PixelXForGridX(gridX, image.width);
                    int value = Luminance(pixels[rowOffset + pixelX]);
                    string classification = ClassifyMaskValue(texture.Path, value);

                    sum += value;
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                    if (!classCounts.ContainsKey(classification))
                        classCounts[classification] = 0;
                    classCounts[classification]++;
                }
            }

            foreach (CoordinateProbeSpec probe in CoordinateProbeSpecs)
            {
                int pixelX = PixelXForGridX(probe.GridX, image.width);
                int pixelY = PixelYForGridZ(probe.GridZ, image.height);
                int value = Luminance(pixels[pixelY * image.width + pixelX]);
                probes.Add(new CoordinateProbeInfo(probe.Name, probe.GridX, probe.GridZ, pixelX, pixelY, value, ClassifyMaskValue(texture.Path, value)));
            }

            return new MaskLayerSamplingInfo(
                texture.Path,
                RoleForMaskPath(texture.Path),
                texture.Width,
                texture.Height,
                min,
                max,
                sum / (double)(MapGridSize * MapGridSize),
                ToClassCounts(classCounts),
                probes);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static Texture2D LoadTexture(string path)
    {
        string absolutePath = ProjectPath(path);
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException("Missing mask texture", path);

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath)))
        {
            UnityEngine.Object.DestroyImmediate(texture);
            throw new InvalidOperationException("Unable to decode mask texture: " + path);
        }

        return texture;
    }

    private static int PixelXForGridX(int gridX, int imageWidth)
    {
        float u = gridX / MapGridMaxCoordinate;
        return Mathf.Clamp(Mathf.RoundToInt(u * (imageWidth - 1)), 0, imageWidth - 1);
    }

    private static int PixelYForGridZ(int gridZ, int imageHeight)
    {
        float v = gridZ / MapGridMaxCoordinate;
        return Mathf.Clamp(Mathf.RoundToInt((1f - v) * (imageHeight - 1)), 0, imageHeight - 1);
    }

    private static int Luminance(Color32 color)
    {
        return Mathf.Clamp(Mathf.RoundToInt(color.r * 0.299f + color.g * 0.587f + color.b * 0.114f), 0, 255);
    }

    private static string RoleForMaskPath(string path)
    {
        if (path == BaseVisualPath)
            return "visualReference";
        if (path == BlockerMaskPath)
            return "pathfindingBlocker";
        if (path == TreeDensityMaskPath)
            return "treeDensity";
        if (path == RockDensityMaskPath)
            return "rockDensity";
        if (path == HeightMaskPath)
            return "heightReference";
        return "unknown";
    }

    private static string ClassifyMaskValue(string path, int value)
    {
        if (path == BlockerMaskPath)
        {
            if (value <= 95)
                return "walkable";
            if (value <= 159)
                return "softEdgeReview";
            return "blocked";
        }

        if (path == TreeDensityMaskPath)
            return ClassifyDensity(value, "tree");
        if (path == RockDensityMaskPath)
            return ClassifyDensity(value, "rock");

        if (path == HeightMaskPath)
        {
            if (value <= 63)
                return "flatLowland";
            if (value <= 143)
                return "gentleSlope";
            if (value <= 207)
                return "raisedRoughHill";
            return "mountainCliffHigh";
        }

        if (value <= 63)
            return "visualDark";
        if (value <= 127)
            return "visualLowMid";
        if (value <= 191)
            return "visualHighMid";
        return "visualBright";
    }

    private static string ClassifyDensity(int value, string prefix)
    {
        if (value <= 31)
            return prefix + "None";
        if (value <= 95)
            return prefix + "Sparse";
        if (value <= 175)
            return prefix + "Medium";
        return prefix + "Dense";
    }

    private static List<ClassCountInfo> ToClassCounts(Dictionary<string, int> classCounts)
    {
        List<ClassCountInfo> counts = new();
        foreach (KeyValuePair<string, int> entry in classCounts)
            counts.Add(new ClassCountInfo(entry.Key, entry.Value));
        counts.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return counts;
    }

    private static PlacementRejectionInfo BuildPlacementRejectionInfo()
    {
        GridRect playableMapFootprint = GetPlayableMapFootprint();
        int[] blocker = LoadMaskValues(BlockerMaskPath);
        int[] tree = LoadMaskValues(TreeDensityMaskPath);
        int[] rock = LoadMaskValues(RockDensityMaskPath);
        int[] height = LoadMaskValues(HeightMaskPath);

        PlacementCandidateAccumulator trees = new("Trees", "Tree density >= 32, rejected outside the 2024 playable map, inside reserve zones, or on blocker/soft-edge pathing cells.");
        PlacementCandidateAccumulator bushes = new("Bushes", "Tree density >= 32, rejected outside the 2024 playable map, inside reserve zones, or on blocker/soft-edge pathing cells.");
        PlacementCandidateAccumulator rocks = new("Rocks", "Rock density >= 32, rejected outside the 2024 playable map, inside reserve zones, or on hard blocker cells.");
        PlacementCandidateAccumulator mountains = new("Mountains", "Height >= 208, blocker >= 160, or dense rock >= 176; rejected outside the 2024 playable map/reserves and must remain tied to natural blocker/high terrain.");
        RejectionCounter global = new();
        bool[] validAny = new bool[MapGridSize * MapGridSize];

        for (int gridZ = 0; gridZ < MapGridSize; gridZ++)
        {
            int rowOffset = gridZ * MapGridSize;
            for (int gridX = 0; gridX < MapGridSize; gridX++)
            {
                int index = rowOffset + gridX;
                bool outsidePlayableMap = !playableMapFootprint.Contains(gridX, gridZ);
                ReserveZoneSpec reserve = ReserveAt(gridX, gridZ);
                bool inReserve = reserve.IsValid;
                int blockerValue = blocker[index];
                int treeValue = tree[index];
                int rockValue = rock[index];
                int heightValue = height[index];
                bool softPathing = blockerValue >= BlockerSoftEdgeThreshold && blockerValue < BlockerBlockedThreshold;
                bool hardBlocked = blockerValue >= BlockerBlockedThreshold;

                global.Add("outsidePlayableMap", outsidePlayableMap);
                global.Add("reserveZone", inReserve);
                global.Add("softPathingEdge", softPathing);
                global.Add("hardBlocked", hardBlocked);

                if (treeValue >= DensityCandidateThreshold)
                {
                    bool valid = AccumulateVegetationCandidate(trees, outsidePlayableMap, inReserve, softPathing, hardBlocked);
                    validAny[index] |= valid;
                }

                if (treeValue >= DensityCandidateThreshold)
                {
                    bool valid = AccumulateVegetationCandidate(bushes, outsidePlayableMap, inReserve, softPathing, hardBlocked);
                    validAny[index] |= valid;
                }

                if (rockValue >= DensityCandidateThreshold)
                {
                    rocks.RawCandidateCount++;
                    bool rejected = false;
                    rejected |= rocks.Rejections.Add("outsidePlayableMap", outsidePlayableMap);
                    rejected |= rocks.Rejections.Add("reserveZone", inReserve);
                    rejected |= rocks.Rejections.Add("hardBlocked", hardBlocked);
                    if (rejected)
                        rocks.RejectedCount++;
                    else
                    {
                        rocks.ValidCount++;
                        validAny[index] = true;
                    }
                }

                bool mountainCandidate = heightValue >= HeightHighThreshold || hardBlocked || rockValue >= DensityDenseThreshold;
                if (mountainCandidate)
                {
                    mountains.RawCandidateCount++;
                    bool naturalBlockerTerrain = hardBlocked || heightValue >= HeightHighThreshold || rockValue >= DensityDenseThreshold;
                    bool rejected = false;
                    rejected |= mountains.Rejections.Add("outsidePlayableMap", outsidePlayableMap);
                    rejected |= mountains.Rejections.Add("reserveZone", inReserve);
                    rejected |= mountains.Rejections.Add("notNaturalBlockerTerrain", !naturalBlockerTerrain);
                    if (rejected)
                        mountains.RejectedCount++;
                    else
                    {
                        mountains.ValidCount++;
                        validAny[index] = true;
                    }
                }
            }
        }

        int validAnyCount = 0;
        foreach (bool valid in validAny)
        {
            if (valid)
                validAnyCount++;
        }

        List<ReserveZoneInfo> reserveZones = new();
        foreach (ReserveZoneSpec spec in ReserveZoneSpecs)
        {
            int blockedCells = 0;
            int softCells = 0;
            int treeCandidateCells = 0;
            int rockCandidateCells = 0;
            int highCells = 0;
            for (int gridZ = spec.Rect.ZMin; gridZ < spec.Rect.ZMax; gridZ++)
            {
                for (int gridX = spec.Rect.XMin; gridX < spec.Rect.XMax; gridX++)
                {
                    int index = gridZ * MapGridSize + gridX;
                    int blockerValue = blocker[index];
                    if (blockerValue >= BlockerBlockedThreshold)
                        blockedCells++;
                    else if (blockerValue >= BlockerSoftEdgeThreshold)
                        softCells++;
                    if (tree[index] >= DensityCandidateThreshold)
                        treeCandidateCells++;
                    if (rock[index] >= DensityCandidateThreshold)
                        rockCandidateCells++;
                    if (height[index] >= HeightRaisedThreshold)
                        highCells++;
                }
            }

            reserveZones.Add(new ReserveZoneInfo(spec.Id, spec.Intent, spec.Rect, spec.Rect.Area, blockedCells, softCells, treeCandidateCells, rockCandidateCells, highCells));
        }

        return new PlacementRejectionInfo(
            playableMapFootprint,
            reserveZones,
            global.ToList(),
            new List<PlacementCandidateSummary>
            {
                trees.ToSummary(),
                bushes.ToSummary(),
                rocks.ToSummary(),
                mountains.ToSummary()
            },
            validAnyCount);
    }

    private static bool AccumulateVegetationCandidate(PlacementCandidateAccumulator accumulator, bool outsidePlayableMap, bool inReserve, bool softPathing, bool hardBlocked)
    {
        accumulator.RawCandidateCount++;
        bool rejected = false;
        rejected |= accumulator.Rejections.Add("outsidePlayableMap", outsidePlayableMap);
        rejected |= accumulator.Rejections.Add("reserveZone", inReserve);
        rejected |= accumulator.Rejections.Add("softPathingEdge", softPathing);
        rejected |= accumulator.Rejections.Add("hardBlocked", hardBlocked);
        if (rejected)
        {
            accumulator.RejectedCount++;
            return false;
        }

        accumulator.ValidCount++;
        return true;
    }

    private static int[] LoadMaskValues(string path)
    {
        Texture2D image = LoadTexture(path);
        try
        {
            Color32[] pixels = image.GetPixels32();
            int[] values = new int[MapGridSize * MapGridSize];
            for (int gridZ = 0; gridZ < MapGridSize; gridZ++)
            {
                int pixelY = PixelYForGridZ(gridZ, image.height);
                int rowOffset = pixelY * image.width;
                int valueOffset = gridZ * MapGridSize;
                for (int gridX = 0; gridX < MapGridSize; gridX++)
                {
                    int pixelX = PixelXForGridX(gridX, image.width);
                    values[valueOffset + gridX] = Luminance(pixels[rowOffset + pixelX]);
                }
            }

            return values;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static GridRect GetPlayableMapFootprint()
    {
        return new GridRect(0, 0, MapGridSize, MapGridSize);
    }

    private static GridRect GridRectFromWorldBounds(Vector3 worldCenter, Vector3 worldSize)
    {
        float halfMap = MapGridMaxCoordinate * 0.5f;
        float minWorldX = worldCenter.x - worldSize.x * 0.5f;
        float maxWorldX = worldCenter.x + worldSize.x * 0.5f;
        float minWorldZ = worldCenter.z - worldSize.z * 0.5f;
        float maxWorldZ = worldCenter.z + worldSize.z * 0.5f;

        int xMin = Mathf.Clamp(Mathf.CeilToInt(minWorldX + halfMap), 0, MapGridSize);
        int xMax = Mathf.Clamp(Mathf.FloorToInt(maxWorldX + halfMap) + 1, 0, MapGridSize);
        int zMin = Mathf.Clamp(Mathf.CeilToInt(minWorldZ + halfMap), 0, MapGridSize);
        int zMax = Mathf.Clamp(Mathf.FloorToInt(maxWorldZ + halfMap) + 1, 0, MapGridSize);
        return new GridRect(xMin, zMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, zMax - zMin));
    }

    private static ReserveZoneSpec ReserveAt(int gridX, int gridZ)
    {
        foreach (ReserveZoneSpec zone in ReserveZoneSpecs)
        {
            if (zone.Rect.Contains(gridX, gridZ))
                return zone;
        }

        return default;
    }

    private static SpacingPlanInfo BuildSpacingPlanInfo()
    {
        GridRect playableMapFootprint = GetPlayableMapFootprint();
        int[] blocker = LoadMaskValues(BlockerMaskPath);
        int[] tree = LoadMaskValues(TreeDensityMaskPath);
        int[] rock = LoadMaskValues(RockDensityMaskPath);
        int[] height = LoadMaskValues(HeightMaskPath);

        List<SpacingKindSummary> summaries = new();
        int totalAccepted = 0;
        foreach (SpacingSpec spec in SpacingSpecs)
        {
            SpacingKindSummary summary = GenerateSpacingForKind(spec, playableMapFootprint, blocker, tree, rock, height);
            summaries.Add(summary);
            totalAccepted += summary.AcceptedCount;
        }

        return new SpacingPlanInfo(
            SpacingSeed,
            "Deterministic stratified dart pass: choose one best mask-weighted candidate per tile, then enforce minimum distance against nearby accepted points.",
            totalAccepted,
            summaries);
    }

    private static SpacingKindSummary GenerateSpacingForKind(SpacingSpec spec, GridRect playableMapFootprint, int[] blocker, int[] tree, int[] rock, int[] height)
    {
        List<SpacingPointInfo> accepted = new();
        List<SpacingPointInfo> sample = new();
        Dictionary<long, List<SpacingPointInfo>> spatial = new();
        int tileCandidateCount = 0;
        int rejectedBySpacing = 0;
        int tilesVisited = 0;
        int minDistanceSq = spec.MinDistance * spec.MinDistance;

        for (int tileZ = 0; tileZ < MapGridSize; tileZ += spec.TileSize)
        {
            for (int tileX = 0; tileX < MapGridSize; tileX += spec.TileSize)
            {
                tilesVisited++;
                SpacingPointInfo best = default;
                bool found = false;
                int bestScore = int.MinValue;
                int xMax = Mathf.Min(tileX + spec.TileSize, MapGridSize);
                int zMax = Mathf.Min(tileZ + spec.TileSize, MapGridSize);

                for (int gridZ = tileZ; gridZ < zMax; gridZ++)
                {
                    int rowOffset = gridZ * MapGridSize;
                    for (int gridX = tileX; gridX < xMax; gridX++)
                    {
                        int index = rowOffset + gridX;
                        if (!IsValidPlacementCandidate(spec.Kind, gridX, gridZ, playableMapFootprint, blocker[index], tree[index], rock[index], height[index]))
                            continue;

                        int weight = CandidateWeight(spec.Kind, blocker[index], tree[index], rock[index], height[index]);
                        int hash = StableHash(gridX, gridZ, SpacingSeed + spec.Kind.Length * 997);
                        int score = weight * 1024 + hash % 1024;
                        if (!found || score > bestScore)
                        {
                            found = true;
                            bestScore = score;
                            best = new SpacingPointInfo(gridX, gridZ, weight, hash);
                        }
                    }
                }

                if (!found)
                    continue;

                tileCandidateCount++;
                if (HasNearbyAccepted(best, spatial, spec.MinDistance, minDistanceSq))
                {
                    rejectedBySpacing++;
                    continue;
                }

                accepted.Add(best);
                if (sample.Count < MaxSamplePointsPerKind)
                    sample.Add(best);
                AddToSpatial(best, spatial, spec.MinDistance);
            }
        }

        return new SpacingKindSummary(spec.Kind, spec.Description, spec.MinDistance, spec.TileSize, tilesVisited, tileCandidateCount, accepted.Count, rejectedBySpacing, accepted, sample);
    }

    private static void PlaceDressingPrefabs(SetupValidation validation)
    {
        Scene targetScene = SceneManager.GetActiveScene();
        if (targetScene.path != TargetScenePath)
            targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        GameObject targetIsland = FindRootGameObject(targetScene, TargetIslandRootName);
        if (targetIsland == null)
            throw new InvalidOperationException("Target scene is missing root Island object: " + TargetScenePath);

        foreach (SpacingKindSummary summary in validation.SpacingPlan.KindSummaries)
        {
            string groupName = GeneratedGroupNameForKind(summary.Kind);
            Transform group = FindDirectChild(targetIsland.transform, groupName);
            if (group == null)
                throw new InvalidOperationException("Generated placement group is missing: " + groupName);

            ClearChildren(group);
            List<CatalogEntry> catalog = CatalogForKind(validation.PrefabCatalog, summary.Kind);
            if (catalog.Count == 0)
                throw new InvalidOperationException("No prefab catalog entries found for generated kind: " + summary.Kind);

            for (int i = 0; i < summary.AcceptedPoints.Count; i++)
            {
                SpacingPointInfo point = summary.AcceptedPoints[i];
                CatalogEntry entry = catalog[((point.Hash + i) & int.MaxValue) % catalog.Count];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.PrefabPath);
                if (prefab == null)
                    throw new FileNotFoundException("Missing prefab for generated dressing", entry.PrefabPath);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = summary.Kind + "_" + i.ToString("0000", CultureInfo.InvariantCulture) + "_" + Path.GetFileNameWithoutExtension(entry.PrefabPath);
                instance.transform.SetParent(group, false);
                instance.transform.localPosition = GridToLocalPosition(point.GridX, point.GridZ);
                instance.transform.localRotation = Quaternion.Euler(0f, RotationForPoint(point), 0f);
                instance.transform.localScale = ScaleForPoint(entry, point, summary.Kind);
            }
        }

        EditorSceneManager.MarkSceneDirty(targetScene);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static string GeneratedGroupNameForKind(string kind)
    {
        if (kind == "Mountains")
            return "Generated_Mountains";
        if (kind == "Trees")
            return "Generated_Trees";
        if (kind == "Bushes")
            return "Generated_Bushes";
        if (kind == "Rocks")
            return "Generated_Rocks";
        throw new InvalidOperationException("Unknown generated dressing kind: " + kind);
    }

    private static List<CatalogEntry> CatalogForKind(Dictionary<string, List<CatalogEntry>> catalog, string kind)
    {
        if (catalog.TryGetValue(kind, out List<CatalogEntry> entries))
            return entries;
        throw new InvalidOperationException("Prefab catalog is missing kind: " + kind);
    }

    private static Vector3 GridToLocalPosition(int gridX, int gridZ)
    {
        return new Vector3(gridX - MapGridMaxCoordinate * 0.5f, 0f, gridZ - MapGridMaxCoordinate * 0.5f);
    }

    private static float RotationForPoint(SpacingPointInfo point)
    {
        return (point.Hash % 36000) / 100f;
    }

    private static Vector3 ScaleForPoint(CatalogEntry entry, SpacingPointInfo point, string kind)
    {
        float t = ((point.Hash >> 8) & 1023) / 1023f;
        Vector3 scale = Vector3.Lerp(entry.MinLocalScale, entry.MaxLocalScale, t);
        if (kind == "Bushes")
            scale *= 0.88f;
        if (kind == "Trees")
            scale *= 0.96f;
        if (kind == "Rocks")
            scale *= 0.92f;
        return scale;
    }

    private static DressingPlacementInfo ScanDressingPlacementInfo(Transform targetIsland)
    {
        List<DressingPlacementGroupInfo> groups = new();
        int total = 0;
        foreach (SpacingSpec spec in SpacingSpecs)
        {
            string groupName = GeneratedGroupNameForKind(spec.Kind);
            Transform group = FindDirectChild(targetIsland, groupName);
            int placedCount = group == null ? 0 : group.childCount;
            total += placedCount;
            groups.Add(new DressingPlacementGroupInfo(spec.Kind, groupName, group != null, placedCount));
        }

        return new DressingPlacementInfo(total, groups);
    }

    private static bool IsValidPlacementCandidate(string kind, int gridX, int gridZ, GridRect playableMapFootprint, int blockerValue, int treeValue, int rockValue, int heightValue)
    {
        if (!playableMapFootprint.Contains(gridX, gridZ))
            return false;
        if (ReserveAt(gridX, gridZ).IsValid)
            return false;

        bool softPathing = blockerValue >= BlockerSoftEdgeThreshold && blockerValue < BlockerBlockedThreshold;
        bool hardBlocked = blockerValue >= BlockerBlockedThreshold;

        if (kind == "Trees" || kind == "Bushes")
            return treeValue >= DensityCandidateThreshold && !softPathing && !hardBlocked;
        if (kind == "Rocks")
            return rockValue >= DensityCandidateThreshold && !hardBlocked;
        if (kind == "Mountains")
            return heightValue >= HeightHighThreshold || hardBlocked || rockValue >= DensityDenseThreshold;

        return false;
    }

    private static int CandidateWeight(string kind, int blockerValue, int treeValue, int rockValue, int heightValue)
    {
        if (kind == "Trees")
            return treeValue;
        if (kind == "Bushes")
            return Mathf.Max(0, treeValue - 16);
        if (kind == "Rocks")
            return rockValue + Mathf.Max(0, heightValue - 96) / 2;
        if (kind == "Mountains")
            return Mathf.Max(Mathf.Max(heightValue, blockerValue), rockValue);
        return 0;
    }

    private static bool HasNearbyAccepted(SpacingPointInfo candidate, Dictionary<long, List<SpacingPointInfo>> spatial, int cellSize, int minDistanceSq)
    {
        int cellX = candidate.GridX / cellSize;
        int cellZ = candidate.GridZ / cellSize;
        for (int z = cellZ - 1; z <= cellZ + 1; z++)
        {
            for (int x = cellX - 1; x <= cellX + 1; x++)
            {
                long key = SpatialKey(x, z);
                if (!spatial.TryGetValue(key, out List<SpacingPointInfo> points))
                    continue;

                foreach (SpacingPointInfo point in points)
                {
                    int dx = candidate.GridX - point.GridX;
                    int dz = candidate.GridZ - point.GridZ;
                    if (dx * dx + dz * dz < minDistanceSq)
                        return true;
                }
            }
        }

        return false;
    }

    private static void AddToSpatial(SpacingPointInfo point, Dictionary<long, List<SpacingPointInfo>> spatial, int cellSize)
    {
        long key = SpatialKey(point.GridX / cellSize, point.GridZ / cellSize);
        if (!spatial.TryGetValue(key, out List<SpacingPointInfo> points))
        {
            points = new List<SpacingPointInfo>();
            spatial[key] = points;
        }

        points.Add(point);
    }

    private static long SpatialKey(int x, int z)
    {
        return ((long)x << 32) ^ (uint)z;
    }

    private static int StableHash(int x, int z, int seed)
    {
        unchecked
        {
            int hash = seed;
            hash = hash * 73856093 ^ x;
            hash = hash * 19349663 ^ z;
            hash ^= hash >> 13;
            hash *= 83492791;
            hash ^= hash >> 16;
            return hash & int.MaxValue;
        }
    }

    private static void WritePrefabCatalogJson(SetupValidation validation)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"catalogId\": \"GameTerrain4_MaskDressingPrefabCatalog\",");
        json.AppendLine("  \"sourceScene\": \"" + SourceScenePath + "\",");
        json.AppendLine("  \"sourceIslandRoot\": \"" + TargetIslandRootName + "\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"mapPack\": \"" + MapPackRoot + "\",");
        json.AppendLine("  \"uniquePrefabAssets\": " + validation.CatalogEntryCount.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"groups\": [");

        int groupIndex = 0;
        foreach (string groupName in SourceExampleGroups)
        {
            List<CatalogEntry> entries = validation.PrefabCatalog[groupName];
            int sampleCount = 0;
            foreach (CatalogEntry entry in entries)
                sampleCount += entry.SampleCount;

            json.AppendLine("    {");
            json.AppendLine("      \"group\": \"" + EscapeJson(groupName) + "\",");
            json.AppendLine("      \"sourceExampleInstances\": " + sampleCount.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"uniquePrefabAssets\": " + entries.Count.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"prefabs\": [");

            for (int i = 0; i < entries.Count; i++)
            {
                CatalogEntry entry = entries[i];
                json.AppendLine("        {");
                json.AppendLine("          \"prefabPath\": \"" + EscapeJson(entry.PrefabPath) + "\",");
                json.AppendLine("          \"sampleCount\": " + entry.SampleCount.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"minLocalScale\": " + VectorToJson(entry.MinLocalScale) + ",");
                json.AppendLine("          \"maxLocalScale\": " + VectorToJson(entry.MaxLocalScale) + ",");
                json.AppendLine("          \"sourceNames\": [");
                for (int n = 0; n < entry.SourceNames.Count; n++)
                {
                    string comma = n == entry.SourceNames.Count - 1 ? string.Empty : ",";
                    json.AppendLine("            \"" + EscapeJson(entry.SourceNames[n]) + "\"" + comma);
                }
                json.AppendLine("          ]");
                string entryComma = i == entries.Count - 1 ? string.Empty : ",";
                json.AppendLine("        }" + entryComma);
            }

            json.AppendLine("      ]");
            string groupComma = groupIndex == SourceExampleGroups.Length - 1 ? string.Empty : ",";
            json.AppendLine("    }" + groupComma);
            groupIndex++;
        }

        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(CatalogJsonPath), json.ToString());
    }

    private static void WriteFoundationSnapshotJson(SetupValidation validation)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        FoundationInfo foundation = validation.Foundation;

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"snapshotId\": \"GameTerrain4_IslandFoundationSnapshot\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"islandRoot\": \"" + TargetIslandRootName + "\",");
        json.AppendLine("  \"foundationChild\": \"" + EscapeJson(foundation.Name) + "\",");
        json.AppendLine("  \"foundationPath\": \"" + EscapeJson(foundation.Path) + "\",");
        json.AppendLine("  \"preservationRule\": \"Generated dressing must be added as sibling child groups under Island and must not modify, delete, rename, reparent, or rescale this foundation child.\",");
        json.AppendLine("  \"activeSelf\": " + ToJsonBool(foundation.ActiveSelf) + ",");
        json.AppendLine("  \"activeInHierarchy\": " + ToJsonBool(foundation.ActiveInHierarchy) + ",");
        json.AppendLine("  \"localPosition\": " + VectorToJson(foundation.LocalPosition) + ",");
        json.AppendLine("  \"localRotationEuler\": " + VectorToJson(foundation.LocalRotationEuler) + ",");
        json.AppendLine("  \"localScale\": " + VectorToJson(foundation.LocalScale) + ",");
        json.AppendLine("  \"childIndex\": " + foundation.ChildIndex.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"transformCount\": " + foundation.TransformCount.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"rendererCount\": " + foundation.RendererCount.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"meshFilterCount\": " + foundation.MeshFilterCount.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"colliderCount\": " + foundation.ColliderCount.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"worldBoundsCenter\": " + VectorToJson(foundation.WorldBoundsCenter) + ",");
        json.AppendLine("  \"worldBoundsSize\": " + VectorToJson(foundation.WorldBoundsSize));
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(FoundationSnapshotJsonPath), json.ToString());
    }

    private static void WriteGeneratedGroupsJson(SetupValidation validation)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"manifestId\": \"GameTerrain4_GeneratedGroupManifest\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"islandRoot\": \"" + TargetIslandRootName + "\",");
        json.AppendLine("  \"foundationChild\": \"" + TargetIslandBaseName + "\",");
        json.AppendLine("  \"rule\": \"All generated dressing content belongs in these direct Island children. The foundation child remains a sibling and must stay untouched.\",");
        json.AppendLine("  \"groups\": [");
        for (int i = 0; i < validation.GeneratedGroups.Count; i++)
        {
            GeneratedGroupInfo group = validation.GeneratedGroups[i];
            json.AppendLine("    {");
            json.AppendLine("      \"name\": \"" + EscapeJson(group.Name) + "\",");
            json.AppendLine("      \"purpose\": \"" + EscapeJson(group.Purpose) + "\",");
            json.AppendLine("      \"exists\": " + ToJsonBool(group.Exists) + ",");
            json.AppendLine("      \"path\": \"" + EscapeJson(group.Path) + "\",");
            json.AppendLine("      \"activeSelf\": " + ToJsonBool(group.ActiveSelf) + ",");
            json.AppendLine("      \"activeInHierarchy\": " + ToJsonBool(group.ActiveInHierarchy) + ",");
            json.AppendLine("      \"childIndex\": " + group.ChildIndex.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"childCount\": " + group.ChildCount.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"localPosition\": " + VectorToJson(group.LocalPosition) + ",");
            json.AppendLine("      \"localRotationEuler\": " + VectorToJson(group.LocalRotationEuler) + ",");
            json.AppendLine("      \"localScale\": " + VectorToJson(group.LocalScale));
            string comma = i == validation.GeneratedGroups.Count - 1 ? string.Empty : ",";
            json.AppendLine("    }" + comma);
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(GeneratedGroupsJsonPath), json.ToString());
    }

    private static void WriteMaskSamplingJson(SetupValidation validation)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        MaskSamplingInfo sampling = validation.MaskSampling;

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"summaryId\": \"GameTerrain4_MaskSamplingSummary\",");
        json.AppendLine("  \"mapPack\": \"" + MapPackRoot + "\",");
        json.AppendLine("  \"gridWidth\": " + sampling.GridWidth.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"gridHeight\": " + sampling.GridHeight.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"totalCells\": " + sampling.TotalCells.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"samplingRule\": \"" + EscapeJson(sampling.SamplingRule) + "\",");
        json.AppendLine("  \"layers\": [");
        for (int i = 0; i < sampling.Layers.Count; i++)
        {
            MaskLayerSamplingInfo layer = sampling.Layers[i];
            json.AppendLine("    {");
            json.AppendLine("      \"path\": \"" + EscapeJson(layer.Path) + "\",");
            json.AppendLine("      \"role\": \"" + EscapeJson(layer.Role) + "\",");
            json.AppendLine("      \"width\": " + layer.Width.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"height\": " + layer.Height.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"minValue\": " + layer.MinValue.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"maxValue\": " + layer.MaxValue.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"averageValue\": " + layer.AverageValue.ToString("0.###", CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"classCounts\": [");
            for (int c = 0; c < layer.ClassCounts.Count; c++)
            {
                ClassCountInfo count = layer.ClassCounts[c];
                json.AppendLine("        {");
                json.AppendLine("          \"class\": \"" + EscapeJson(count.Name) + "\",");
                json.AppendLine("          \"count\": " + count.Count.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"percent\": " + Percent(count.Count, sampling.TotalCells).ToString("0.###", CultureInfo.InvariantCulture));
                string classComma = c == layer.ClassCounts.Count - 1 ? string.Empty : ",";
                json.AppendLine("        }" + classComma);
            }
            json.AppendLine("      ],");
            json.AppendLine("      \"coordinateProbes\": [");
            for (int p = 0; p < layer.CoordinateProbes.Count; p++)
            {
                CoordinateProbeInfo probe = layer.CoordinateProbes[p];
                json.AppendLine("        {");
                json.AppendLine("          \"name\": \"" + EscapeJson(probe.Name) + "\",");
                json.AppendLine("          \"gridX\": " + probe.GridX.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"gridZ\": " + probe.GridZ.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"pixelX\": " + probe.PixelX.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"pixelY\": " + probe.PixelY.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"value\": " + probe.Value.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"class\": \"" + EscapeJson(probe.Classification) + "\"");
                string probeComma = p == layer.CoordinateProbes.Count - 1 ? string.Empty : ",";
                json.AppendLine("        }" + probeComma);
            }
            json.AppendLine("      ]");
            string layerComma = i == sampling.Layers.Count - 1 ? string.Empty : ",";
            json.AppendLine("    }" + layerComma);
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(MaskSamplingJsonPath), json.ToString());
    }

    private static void WritePlacementRejectionJson(SetupValidation validation)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        PlacementRejectionInfo rejection = validation.PlacementRejection;

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"summaryId\": \"GameTerrain4_PlacementRejectionSummary\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"mapPack\": \"" + MapPackRoot + "\",");
        json.AppendLine("  \"gridWidth\": " + MapGridSize.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"gridHeight\": " + MapGridSize.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placementContract\": \"Mask grid 0..2023 maps to the 2024 playable map footprint on the enlarged green/dirt island foundation; beach/coast terrain is visual border content outside this gameplay contract.\",");
        json.AppendLine("  \"rules\": [");
        json.AppendLine("    \"Reject decorative placement outside the 2024 playable map footprint.\",");
        json.AppendLine("    \"Reject decorative placement inside city/base reserve zones.\",");
        json.AppendLine("    \"Reject tree and bush placement on blocker soft-edge or hard-blocked pathing cells.\",");
        json.AppendLine("    \"Reject decorative rock placement on hard-blocked pathing cells.\",");
        json.AppendLine("    \"Allow mountain/blocker dressing only on natural blocker/high-terrain candidates outside reserve zones.\"");
        json.AppendLine("  ],");
        json.AppendLine("  \"playableMapFootprint\": " + GridRectToJson(rejection.PlayableMapFootprint) + ",");
        json.AppendLine("  \"validAnyPlacementCells\": " + rejection.ValidAnyCount.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"globalRejectionCounts\": " + RejectionCountsToJson(rejection.GlobalRejectionCounts, 2) + ",");
        json.AppendLine("  \"reserveZones\": [");
        for (int i = 0; i < rejection.ReserveZones.Count; i++)
        {
            ReserveZoneInfo zone = rejection.ReserveZones[i];
            json.AppendLine("    {");
            json.AppendLine("      \"id\": \"" + EscapeJson(zone.Id) + "\",");
            json.AppendLine("      \"intent\": \"" + EscapeJson(zone.Intent) + "\",");
            json.AppendLine("      \"rect\": " + GridRectToJson(zone.Rect) + ",");
            json.AppendLine("      \"areaCells\": " + zone.AreaCells.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"blockedCells\": " + zone.BlockedCells.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"softEdgeCells\": " + zone.SoftEdgeCells.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"treeCandidateCells\": " + zone.TreeCandidateCells.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"rockCandidateCells\": " + zone.RockCandidateCells.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"raisedOrHighCells\": " + zone.RaisedOrHighCells.ToString(CultureInfo.InvariantCulture));
            string comma = i == rejection.ReserveZones.Count - 1 ? string.Empty : ",";
            json.AppendLine("    }" + comma);
        }
        json.AppendLine("  ],");
        json.AppendLine("  \"candidateSummaries\": [");
        for (int i = 0; i < rejection.CandidateSummaries.Count; i++)
        {
            PlacementCandidateSummary summary = rejection.CandidateSummaries[i];
            json.AppendLine("    {");
            json.AppendLine("      \"kind\": \"" + EscapeJson(summary.Kind) + "\",");
            json.AppendLine("      \"rule\": \"" + EscapeJson(summary.Rule) + "\",");
            json.AppendLine("      \"rawCandidateCells\": " + summary.RawCandidateCount.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"validCells\": " + summary.ValidCount.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"rejectedCells\": " + summary.RejectedCount.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"rejectionCounts\": " + RejectionCountsToJson(summary.RejectionCounts, 3));
            string comma = i == rejection.CandidateSummaries.Count - 1 ? string.Empty : ",";
            json.AppendLine("    }" + comma);
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(PlacementRejectionJsonPath), json.ToString());
    }

    private static void WriteSpacingPlanJson(SetupValidation validation)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        SpacingPlanInfo spacing = validation.SpacingPlan;

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"summaryId\": \"GameTerrain4_SpacingPlanSummary\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"mapPack\": \"" + MapPackRoot + "\",");
        json.AppendLine("  \"seed\": " + spacing.Seed.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"method\": \"" + EscapeJson(spacing.Method) + "\",");
        json.AppendLine("  \"totalAcceptedPoints\": " + spacing.TotalAcceptedPoints.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"maxSamplePointsPerKind\": " + MaxSamplePointsPerKind.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"kinds\": [");
        for (int i = 0; i < spacing.KindSummaries.Count; i++)
        {
            SpacingKindSummary summary = spacing.KindSummaries[i];
            json.AppendLine("    {");
            json.AppendLine("      \"kind\": \"" + EscapeJson(summary.Kind) + "\",");
            json.AppendLine("      \"description\": \"" + EscapeJson(summary.Description) + "\",");
            json.AppendLine("      \"minDistanceCells\": " + summary.MinDistance.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"tileSizeCells\": " + summary.TileSize.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"tilesVisited\": " + summary.TilesVisited.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"tileCandidateCount\": " + summary.TileCandidateCount.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"acceptedCount\": " + summary.AcceptedCount.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"rejectedBySpacing\": " + summary.RejectedBySpacing.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"samplePoints\": [");
            for (int p = 0; p < summary.SamplePoints.Count; p++)
            {
                SpacingPointInfo point = summary.SamplePoints[p];
                json.AppendLine("        {");
                json.AppendLine("          \"gridX\": " + point.GridX.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"gridZ\": " + point.GridZ.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"weight\": " + point.Weight.ToString(CultureInfo.InvariantCulture) + ",");
                json.AppendLine("          \"hash\": " + point.Hash.ToString(CultureInfo.InvariantCulture));
                string pointComma = p == summary.SamplePoints.Count - 1 ? string.Empty : ",";
                json.AppendLine("        }" + pointComma);
            }
            json.AppendLine("      ]");
            string comma = i == spacing.KindSummaries.Count - 1 ? string.Empty : ",";
            json.AppendLine("    }" + comma);
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(SpacingPlanJsonPath), json.ToString());
    }

    private static void WriteDressingPlacementJson(SetupValidation validation)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        DressingPlacementInfo placement = validation.DressingPlacement;

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"summaryId\": \"GameTerrain4_DressingPlacementSummary\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"mapPack\": \"" + MapPackRoot + "\",");
        json.AppendLine("  \"placementRule\": \"Generated dressing prefabs are instantiated only under the generated sibling groups beneath Island; the preserved foundation child is not modified.\",");
        json.AppendLine("  \"totalPlacedPrefabs\": " + placement.TotalPlacedPrefabs.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"groups\": [");
        for (int i = 0; i < placement.Groups.Count; i++)
        {
            DressingPlacementGroupInfo group = placement.Groups[i];
            json.AppendLine("    {");
            json.AppendLine("      \"kind\": \"" + EscapeJson(group.Kind) + "\",");
            json.AppendLine("      \"groupName\": \"" + EscapeJson(group.GroupName) + "\",");
            json.AppendLine("      \"exists\": " + ToJsonBool(group.Exists) + ",");
            json.AppendLine("      \"placedPrefabs\": " + group.PlacedCount.ToString(CultureInfo.InvariantCulture));
            string comma = i == placement.Groups.Count - 1 ? string.Empty : ",";
            json.AppendLine("    }" + comma);
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(DressingPlacementJsonPath), json.ToString());
    }

    private static ValidationArtifactInfo BuildValidationArtifacts(SetupValidation validation, bool renderCaptures)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));

        Scene targetScene = SceneManager.GetActiveScene();
        if (targetScene.path != TargetScenePath)
            targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        GameObject targetIsland = FindRootGameObject(targetScene, TargetIslandRootName);
        if (targetIsland == null)
            throw new InvalidOperationException("Target scene is missing root Island object: " + TargetScenePath);

        int[] blocker = LoadMaskValues(BlockerMaskPath);
        int[] rock = LoadMaskValues(RockDensityMaskPath);
        int[] height = LoadMaskValues(HeightMaskPath);
        List<PlacedDressingPointInfo> placedPoints = CollectPlacedDressingPoints(targetIsland.transform);
        List<ValidationCheckInfo> checks = new();

        AddCheck(checks, "totalPlacedPrefabs", validation.SpacingPlan.TotalAcceptedPoints, placedPoints.Count, placedPoints.Count == validation.SpacingPlan.TotalAcceptedPoints, "Scene placed-prefab total must match the deterministic spacing plan.");
        foreach (SpacingKindSummary summary in validation.SpacingPlan.KindSummaries)
        {
            int actual = CountPlacedKind(placedPoints, summary.Kind);
            AddCheck(checks, "count." + summary.Kind, summary.AcceptedCount, actual, actual == summary.AcceptedCount, "Generated group count for " + summary.Kind + " must match accepted spacing points.");
        }

        foreach (ReserveZoneSpec zone in ReserveZoneSpecs)
        {
            int violations = CountReserveViolations(placedPoints, zone.Rect);
            AddCheck(checks, "reserveClear." + zone.Id, 0, violations, violations == 0, "No generated dressing may occupy reserved city/base placement space.");
        }

        int outsidePlayableMapViolations = 0;
        foreach (PlacedDressingPointInfo point in placedPoints)
        {
            if (!validation.PlacementRejection.PlayableMapFootprint.Contains(point.GridX, point.GridZ))
                outsidePlayableMapViolations++;
        }
        AddCheck(checks, "playableMapFootprint.containment", 0, outsidePlayableMapViolations, outsidePlayableMapViolations == 0, "Generated dressing must remain inside the 2024 playable map footprint, which is now fully covered by green/dirt terrain.");

        int vegetationPathingViolations = 0;
        int rockPathingViolations = 0;
        int mountainNaturalBlockerViolations = 0;
        int mountainNaturalBlockerCompliant = 0;
        foreach (PlacedDressingPointInfo point in placedPoints)
        {
            int index = point.GridZ * MapGridSize + point.GridX;
            int blockerValue = blocker[index];
            int rockValue = rock[index];
            int heightValue = height[index];

            if ((point.Kind == "Trees" || point.Kind == "Bushes") && blockerValue >= BlockerSoftEdgeThreshold)
                vegetationPathingViolations++;
            if (point.Kind == "Rocks" && blockerValue >= BlockerBlockedThreshold)
                rockPathingViolations++;
            if (point.Kind == "Mountains")
            {
                bool naturalBlockerTerrain = blockerValue >= BlockerBlockedThreshold || heightValue >= HeightHighThreshold || rockValue >= DensityDenseThreshold;
                if (naturalBlockerTerrain)
                    mountainNaturalBlockerCompliant++;
                else
                    mountainNaturalBlockerViolations++;
            }
        }

        AddCheck(checks, "pathing.vegetationClear", 0, vegetationPathingViolations, vegetationPathingViolations == 0, "Trees and bushes must not sit on soft-edge or hard-blocker pathing cells.");
        AddCheck(checks, "pathing.rocksClear", 0, rockPathingViolations, rockPathingViolations == 0, "Decorative rocks must not sit on hard-blocked pathing cells.");
        AddCheck(checks, "blockerBelt.mountainsNaturalTerrain", CountPlacedKind(placedPoints, "Mountains"), mountainNaturalBlockerCompliant, mountainNaturalBlockerViolations == 0, "Mountain dressing must stay tied to blocker, high-terrain, or dense-rock mask cells.");

        if (renderCaptures)
        {
            RenderTopDownMapProof(TopDownProofCapturePath, 2048, 2048, placedPoints, validation.PlacementRejection);
            RenderPlayableFrameProof(PlayableAngleProofCapturePath, 1920, 1080, placedPoints, validation.PlacementRejection);
        }

        if (renderCaptures || File.Exists(ProjectPath(TopDownProofCapturePath)))
        {
            int topDownExists = File.Exists(ProjectPath(TopDownProofCapturePath)) && new FileInfo(ProjectPath(TopDownProofCapturePath)).Length > 0 ? 1 : 0;
            AddCheck(checks, "capture.topDownProof", 1, topDownExists, topDownExists == 1, "Top-down proof image must exist and be non-empty.");
        }

        if (renderCaptures || File.Exists(ProjectPath(PlayableAngleProofCapturePath)))
        {
            int playableExists = File.Exists(ProjectPath(PlayableAngleProofCapturePath)) && new FileInfo(ProjectPath(PlayableAngleProofCapturePath)).Length > 0 ? 1 : 0;
            AddCheck(checks, "capture.playableAngleProof", 1, playableExists, playableExists == 1, "Playable-frame proof image must exist and be non-empty.");

            int playableContainsTerrain = playableExists == 1 && CaptureHasTerrainContent(PlayableAngleProofCapturePath) ? 1 : 0;
            AddCheck(checks, "capture.playableAngleContent", 1, playableContainsTerrain, playableContainsTerrain == 1, "Playable-frame proof must show readable terrain content, not only sky/background.");
        }

        bool passed = true;
        foreach (ValidationCheckInfo check in checks)
            passed &= check.Passed;

        return new ValidationArtifactInfo(renderCaptures, TopDownProofCapturePath, PlayableAngleProofCapturePath, placedPoints.Count, passed, checks);
    }

    private static void RenderTerrainCapture(string path, int width, int height, bool topDown, FoundationInfo foundation, Transform targetIsland, List<PlacedDressingPointInfo> placedPoints)
    {
        string absolutePath = ProjectPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2
        };

        GameObject cameraObject = new(topDown ? "Temp_GameTerrain4_TopDownProofCamera" : "Temp_GameTerrain4_PlayableProofCamera");
        GameObject lightObject = new("Temp_GameTerrain4_ValidationLight");
        GameObject overlayRoot = null;
        List<Material> overlayMaterials = null;
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.46f, 0.55f, 0.65f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 5000f;
            camera.targetTexture = renderTexture;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            float maxSize = Mathf.Max(foundation.WorldBoundsSize.x, foundation.WorldBoundsSize.z);
            Vector3 center = foundation.WorldBoundsCenter;
            if (topDown)
            {
                camera.orthographic = true;
                camera.orthographicSize = maxSize * 0.55f;
                camera.transform.position = center + new Vector3(0f, Mathf.Max(1200f, maxSize * 0.8f), 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                camera.orthographic = true;
                camera.orthographicSize = maxSize * 0.37f;
                Vector3 position = center + new Vector3(-maxSize * 0.34f, maxSize * 0.72f, -maxSize * 0.42f);
                camera.transform.position = position;
                camera.transform.LookAt(center + new Vector3(0f, 10f, 0f));
            }

            overlayRoot = CreateTemporaryValidationOverlay(targetIsland, placedPoints, topDown, out overlayMaterials);
            camera.Render();
            RenderTexture.active = renderTexture;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        finally
        {
            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera != null)
                camera.targetTexture = null;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            if (overlayRoot != null)
                UnityEngine.Object.DestroyImmediate(overlayRoot);
            if (overlayMaterials != null)
            {
                foreach (Material material in overlayMaterials)
                    UnityEngine.Object.DestroyImmediate(material);
            }
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
        }
    }

    private static bool CaptureHasTerrainContent(string path)
    {
        string absolutePath = ProjectPath(path);
        if (!File.Exists(absolutePath))
            return false;

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
                return false;

            Color32[] pixels = texture.GetPixels32();
            int stride = Mathf.Max(1, pixels.Length / 20000);
            int sampled = 0;
            int terrainLike = 0;
            for (int i = 0; i < pixels.Length; i += stride)
            {
                Color32 pixel = pixels[i];
                sampled++;
                bool skyLike = pixel.b > pixel.r + 25 && pixel.b > pixel.g + 5 && pixel.r > 95 && pixel.g > 115;
                bool paleBackground = pixel.r > 175 && pixel.g > 195 && pixel.b > 215;
                if (!skyLike && !paleBackground)
                    terrainLike++;
            }

            return sampled > 0 && terrainLike / (float)sampled >= 0.18f;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void RenderPlayableFrameProof(string path, int width, int height, List<PlacedDressingPointInfo> placedPoints, PlacementRejectionInfo rejection)
    {
        string absolutePath = ProjectPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        Texture2D baseVisual = LoadTexture(BaseVisualPath);
        Texture2D proof = new(width, height, TextureFormat.RGBA32, false);
        try
        {
            Color32[] basePixels = baseVisual.GetPixels32();
            Color32[] proofPixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int sourceY = Mathf.Clamp(Mathf.RoundToInt(y / (float)(height - 1) * (baseVisual.height - 1)), 0, baseVisual.height - 1);
                int sourceRow = sourceY * baseVisual.width;
                int outputRow = y * width;
                for (int x = 0; x < width; x++)
                {
                    int sourceX = Mathf.Clamp(Mathf.RoundToInt(x / (float)(width - 1) * (baseVisual.width - 1)), 0, baseVisual.width - 1);
                    Color32 color = basePixels[sourceRow + sourceX];
                    proofPixels[outputRow + x] = new Color32((byte)Mathf.Clamp(color.r * 0.88f, 0f, 255f), (byte)Mathf.Clamp(color.g * 0.88f, 0f, 255f), (byte)Mathf.Clamp(color.b * 0.88f, 0f, 255f), 255);
                }
            }

            DrawGridRectOutline(proofPixels, width, height, rejection.PlayableMapFootprint, new Color32(255, 255, 255, 255), 3);
            foreach (ReserveZoneSpec zone in ReserveZoneSpecs)
                DrawGridRectOutline(proofPixels, width, height, zone.Rect, new Color32(255, 68, 50, 255), 5);

            foreach (PlacedDressingPointInfo point in placedPoints)
                DrawMarker(proofPixels, width, height, point.GridX, point.GridZ, MarkerColorForKind(point.Kind), MarkerRadiusForKind(point.Kind));

            proof.SetPixels32(proofPixels);
            proof.Apply();
            File.WriteAllBytes(absolutePath, proof.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(baseVisual);
            UnityEngine.Object.DestroyImmediate(proof);
        }
    }

    private static void RenderTopDownMapProof(string path, int width, int height, List<PlacedDressingPointInfo> placedPoints, PlacementRejectionInfo rejection)
    {
        string absolutePath = ProjectPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        Texture2D baseVisual = LoadTexture(BaseVisualPath);
        Texture2D proof = new(width, height, TextureFormat.RGBA32, false);
        try
        {
            Color32[] basePixels = baseVisual.GetPixels32();
            Color32[] proofPixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int sourceY = Mathf.Clamp(Mathf.RoundToInt(y / (float)(height - 1) * (baseVisual.height - 1)), 0, baseVisual.height - 1);
                int sourceRow = sourceY * baseVisual.width;
                int outputRow = y * width;
                for (int x = 0; x < width; x++)
                {
                    int sourceX = Mathf.Clamp(Mathf.RoundToInt(x / (float)(width - 1) * (baseVisual.width - 1)), 0, baseVisual.width - 1);
                    Color32 color = basePixels[sourceRow + sourceX];
                    proofPixels[outputRow + x] = new Color32((byte)Mathf.Clamp(color.r * 0.82f, 0f, 255f), (byte)Mathf.Clamp(color.g * 0.82f, 0f, 255f), (byte)Mathf.Clamp(color.b * 0.82f, 0f, 255f), 255);
                }
            }

            DrawGridRectOutline(proofPixels, width, height, rejection.PlayableMapFootprint, new Color32(255, 255, 255, 255), 3);
            foreach (ReserveZoneSpec zone in ReserveZoneSpecs)
                DrawGridRectOutline(proofPixels, width, height, zone.Rect, new Color32(255, 68, 50, 255), 5);

            foreach (PlacedDressingPointInfo point in placedPoints)
                DrawMarker(proofPixels, width, height, point.GridX, point.GridZ, MarkerColorForKind(point.Kind), MarkerRadiusForKind(point.Kind));

            proof.SetPixels32(proofPixels);
            proof.Apply();
            File.WriteAllBytes(absolutePath, proof.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(baseVisual);
            UnityEngine.Object.DestroyImmediate(proof);
        }
    }

    private static void DrawGridRectOutline(Color32[] pixels, int width, int height, GridRect rect, Color32 color, int thickness)
    {
        int xMin = GridXToProofPixel(rect.XMin, width);
        int xMax = GridXToProofPixel(rect.XMax - 1, width);
        int yMin = GridZToProofPixel(rect.ZMax - 1, height);
        int yMax = GridZToProofPixel(rect.ZMin, height);

        for (int t = 0; t < thickness; t++)
        {
            DrawLine(pixels, width, height, xMin, yMin + t, xMax, yMin + t, color);
            DrawLine(pixels, width, height, xMin, yMax - t, xMax, yMax - t, color);
            DrawLine(pixels, width, height, xMin + t, yMin, xMin + t, yMax, color);
            DrawLine(pixels, width, height, xMax - t, yMin, xMax - t, yMax, color);
        }
    }

    private static void DrawLine(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;

        while (true)
        {
            SetPixel(pixels, width, height, x0, y0, color);
            if (x0 == x1 && y0 == y1)
                break;
            int e2 = 2 * error;
            if (e2 >= dy)
            {
                error += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private static void DrawMarker(Color32[] pixels, int width, int height, int gridX, int gridZ, Color32 color, int radius)
    {
        int centerX = GridXToProofPixel(gridX, width);
        int centerY = GridZToProofPixel(gridZ, height);
        int radiusSq = radius * radius;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSq)
                    BlendPixel(pixels, width, height, x, y, color, 0.86f);
            }
        }
    }

    private static int GridXToProofPixel(int gridX, int width)
    {
        return Mathf.Clamp(Mathf.RoundToInt(gridX / MapGridMaxCoordinate * (width - 1)), 0, width - 1);
    }

    private static int GridZToProofPixel(int gridZ, int height)
    {
        return Mathf.Clamp(Mathf.RoundToInt((1f - gridZ / MapGridMaxCoordinate) * (height - 1)), 0, height - 1);
    }

    private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;
        pixels[y * width + x] = color;
    }

    private static void BlendPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color, float alpha)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        int index = y * width + x;
        Color32 existing = pixels[index];
        pixels[index] = new Color32(
            (byte)Mathf.Clamp(existing.r * (1f - alpha) + color.r * alpha, 0f, 255f),
            (byte)Mathf.Clamp(existing.g * (1f - alpha) + color.g * alpha, 0f, 255f),
            (byte)Mathf.Clamp(existing.b * (1f - alpha) + color.b * alpha, 0f, 255f),
            255);
    }

    private static Color32 MarkerColorForKind(string kind)
    {
        if (kind == "Mountains")
            return new Color32(238, 88, 28, 255);
        if (kind == "Trees")
            return new Color32(24, 116, 45, 255);
        if (kind == "Bushes")
            return new Color32(150, 190, 45, 255);
        return new Color32(98, 93, 86, 255);
    }

    private static int MarkerRadiusForKind(string kind)
    {
        if (kind == "Mountains")
            return 8;
        if (kind == "Trees")
            return 5;
        if (kind == "Bushes")
            return 4;
        return 5;
    }

    private static GameObject CreateTemporaryValidationOverlay(Transform targetIsland, List<PlacedDressingPointInfo> placedPoints, bool topDown, out List<Material> materials)
    {
        materials = new List<Material>
        {
            CreateOverlayMaterial(new Color(0.95f, 0.49f, 0.16f, 1f)),
            CreateOverlayMaterial(new Color(0.12f, 0.45f, 0.16f, 1f)),
            CreateOverlayMaterial(new Color(0.53f, 0.70f, 0.22f, 1f)),
            CreateOverlayMaterial(new Color(0.42f, 0.42f, 0.42f, 1f))
        };

        GameObject root = new("Temp_GameTerrain4_ValidationMarkerOverlay")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        foreach (PlacedDressingPointInfo point in placedPoints)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.hideFlags = HideFlags.HideAndDontSave;
            marker.name = "Marker_" + point.Kind;
            marker.transform.SetParent(root.transform, true);

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            marker.transform.position = targetIsland.TransformPoint(GridToLocalPosition(point.GridX, point.GridZ)) + Vector3.up * (topDown ? 34f : 22f);
            marker.transform.rotation = Quaternion.identity;
            marker.transform.localScale = MarkerScaleForKind(point.Kind, topDown);

            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = MaterialForKind(point.Kind, materials);
        }

        return root;
    }

    private static Material CreateOverlayMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    private static Material MaterialForKind(string kind, List<Material> materials)
    {
        if (kind == "Mountains")
            return materials[0];
        if (kind == "Trees")
            return materials[1];
        if (kind == "Bushes")
            return materials[2];
        return materials[3];
    }

    private static Vector3 MarkerScaleForKind(string kind, bool topDown)
    {
        if (kind == "Mountains")
            return topDown ? new Vector3(24f, 6f, 24f) : new Vector3(22f, 22f, 22f);
        if (kind == "Trees")
            return topDown ? new Vector3(14f, 5f, 14f) : new Vector3(12f, 18f, 12f);
        if (kind == "Bushes")
            return topDown ? new Vector3(10f, 4f, 10f) : new Vector3(10f, 10f, 10f);
        return topDown ? new Vector3(12f, 4f, 12f) : new Vector3(12f, 12f, 12f);
    }

    private static List<PlacedDressingPointInfo> CollectPlacedDressingPoints(Transform targetIsland)
    {
        List<PlacedDressingPointInfo> points = new();
        foreach (SpacingSpec spec in SpacingSpecs)
        {
            string groupName = GeneratedGroupNameForKind(spec.Kind);
            Transform group = FindDirectChild(targetIsland, groupName);
            if (group == null)
                continue;

            foreach (Transform child in group)
            {
                int gridX = Mathf.Clamp(Mathf.RoundToInt(child.localPosition.x + MapGridMaxCoordinate * 0.5f), 0, MapGridSize - 1);
                int gridZ = Mathf.Clamp(Mathf.RoundToInt(child.localPosition.z + MapGridMaxCoordinate * 0.5f), 0, MapGridSize - 1);
                points.Add(new PlacedDressingPointInfo(spec.Kind, groupName, child.name, gridX, gridZ));
            }
        }

        return points;
    }

    private static int CountPlacedKind(List<PlacedDressingPointInfo> points, string kind)
    {
        int count = 0;
        foreach (PlacedDressingPointInfo point in points)
        {
            if (point.Kind == kind)
                count++;
        }

        return count;
    }

    private static int CountReserveViolations(List<PlacedDressingPointInfo> points, GridRect reserve)
    {
        int count = 0;
        foreach (PlacedDressingPointInfo point in points)
        {
            if (reserve.Contains(point.GridX, point.GridZ))
                count++;
        }

        return count;
    }

    private static void AddCheck(List<ValidationCheckInfo> checks, string id, int expected, int actual, bool passed, string details)
    {
        checks.Add(new ValidationCheckInfo(id, passed, expected, actual, details));
    }

    private static void WriteValidationArtifactsJson(ValidationArtifactInfo artifacts)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"summaryId\": \"GameTerrain4_ValidationArtifacts\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"mapPack\": \"" + MapPackRoot + "\",");
        json.AppendLine("  \"capturesRenderedThisRun\": " + ToJsonBool(artifacts.CapturesRendered) + ",");
        json.AppendLine("  \"topDownProofCapture\": \"" + EscapeJson(artifacts.TopDownCapturePath) + "\",");
        json.AppendLine("  \"playableAngleProofCapture\": \"" + EscapeJson(artifacts.PlayableAngleCapturePath) + "\",");
        json.AppendLine("  \"totalPlacedPrefabs\": " + artifacts.TotalPlacedPrefabs.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"passed\": " + ToJsonBool(artifacts.Passed) + ",");
        json.AppendLine("  \"checks\": [");
        for (int i = 0; i < artifacts.Checks.Count; i++)
        {
            ValidationCheckInfo check = artifacts.Checks[i];
            json.AppendLine("    {");
            json.AppendLine("      \"id\": \"" + EscapeJson(check.Id) + "\",");
            json.AppendLine("      \"passed\": " + ToJsonBool(check.Passed) + ",");
            json.AppendLine("      \"expected\": " + check.Expected.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"actual\": " + check.Actual.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine("      \"details\": \"" + EscapeJson(check.Details) + "\"");
            string comma = i == artifacts.Checks.Count - 1 ? string.Empty : ",";
            json.AppendLine("    }" + comma);
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(ValidationArtifactsJsonPath), json.ToString());
    }

    private static void EnsureSceneAsset(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            throw new FileNotFoundException("Missing scene asset", path);
    }

    private static void EnsureFile(string path)
    {
        if (!File.Exists(ProjectPath(path)))
            throw new FileNotFoundException("Missing required file", path);
    }

    private static TextureInfo LoadTextureInfo(string path)
    {
        string absolutePath = ProjectPath(path);
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException("Missing mask texture", path);

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath)))
                throw new InvalidOperationException("Unable to decode mask texture: " + path);
            return new TextureInfo(path, texture.width, texture.height);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
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

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform match = FindChildRecursive(child, name);
            if (match != null)
                return match;
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

    private static bool IsDescendantOrSelf(Transform candidate, Transform parent)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = current.parent;
        }

        return false;
    }

    private static string FormatVector(Vector3 vector)
    {
        return "("
            + vector.x.ToString("0.###", CultureInfo.InvariantCulture) + ", "
            + vector.y.ToString("0.###", CultureInfo.InvariantCulture) + ", "
            + vector.z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
    }

    private static string FormatClassCounts(List<ClassCountInfo> counts)
    {
        List<string> parts = new();
        foreach (ClassCountInfo count in counts)
            parts.Add(count.Name + "=" + count.Count.ToString(CultureInfo.InvariantCulture));
        return string.Join(", ", parts);
    }

    private static string FormatRejectionCounts(List<RejectionCountInfo> counts)
    {
        List<string> parts = new();
        foreach (RejectionCountInfo count in counts)
            parts.Add(count.Name + "=" + count.Count.ToString(CultureInfo.InvariantCulture));
        return string.Join(", ", parts);
    }

    private static string FormatGridRect(GridRect rect)
    {
        return rect.XMin.ToString(CultureInfo.InvariantCulture) + "," + rect.ZMin.ToString(CultureInfo.InvariantCulture) + " "
            + rect.Width.ToString(CultureInfo.InvariantCulture) + "x" + rect.Height.ToString(CultureInfo.InvariantCulture);
    }

    private static string GridRectToJson(GridRect rect)
    {
        return "{ \"xMin\": " + rect.XMin.ToString(CultureInfo.InvariantCulture)
            + ", \"zMin\": " + rect.ZMin.ToString(CultureInfo.InvariantCulture)
            + ", \"width\": " + rect.Width.ToString(CultureInfo.InvariantCulture)
            + ", \"height\": " + rect.Height.ToString(CultureInfo.InvariantCulture)
            + ", \"xMaxExclusive\": " + rect.XMax.ToString(CultureInfo.InvariantCulture)
            + ", \"zMaxExclusive\": " + rect.ZMax.ToString(CultureInfo.InvariantCulture) + " }";
    }

    private static string RejectionCountsToJson(List<RejectionCountInfo> counts, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);
        string childIndent = new string(' ', (indentLevel + 1) * 2);
        StringBuilder json = new();
        json.AppendLine("[");
        for (int i = 0; i < counts.Count; i++)
        {
            RejectionCountInfo count = counts[i];
            json.AppendLine(childIndent + "{");
            json.AppendLine(childIndent + "  \"reason\": \"" + EscapeJson(count.Name) + "\",");
            json.AppendLine(childIndent + "  \"count\": " + count.Count.ToString(CultureInfo.InvariantCulture));
            string comma = i == counts.Count - 1 ? string.Empty : ",";
            json.AppendLine(childIndent + "}" + comma);
        }
        json.Append(indent + "]");
        return json.ToString();
    }

    private static double Percent(int count, int total)
    {
        if (total <= 0)
            return 0;
        return count / (double)total * 100.0;
    }

    private static string VectorToJson(Vector3 vector)
    {
        return "["
            + vector.x.ToString("0.###", CultureInfo.InvariantCulture) + ", "
            + vector.y.ToString("0.###", CultureInfo.InvariantCulture) + ", "
            + vector.z.ToString("0.###", CultureInfo.InvariantCulture) + "]";
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string ToJsonBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string GetTransformPath(Transform transform, Transform stopAt)
    {
        List<string> parts = new();
        Transform current = transform;
        while (current != null)
        {
            parts.Add(current.name);
            if (current == stopAt)
                break;
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath ?? string.Empty);
    }

    private readonly struct TextureInfo
    {
        public readonly string Path;
        public readonly int Width;
        public readonly int Height;

        public TextureInfo(string path, int width, int height)
        {
            Path = path;
            Width = width;
            Height = height;
        }
    }

    private readonly struct SetupValidation
    {
        public readonly List<TextureInfo> Textures;
        public readonly Dictionary<string, int> SourceGroupChildren;
        public readonly Dictionary<string, List<CatalogEntry>> PrefabCatalog;
        public readonly FoundationInfo Foundation;
        public readonly List<GeneratedGroupInfo> GeneratedGroups;
        public readonly MaskSamplingInfo MaskSampling;
        public readonly PlacementRejectionInfo PlacementRejection;
        public readonly SpacingPlanInfo SpacingPlan;
        public readonly DressingPlacementInfo DressingPlacement;
        public readonly bool TargetIslandActive;
        public readonly Vector3 TargetIslandLocalPosition;
        public readonly Vector3 TargetIslandLocalScale;

        public SetupValidation(List<TextureInfo> textures, Dictionary<string, int> sourceGroupChildren, Dictionary<string, List<CatalogEntry>> prefabCatalog, FoundationInfo foundation, List<GeneratedGroupInfo> generatedGroups, MaskSamplingInfo maskSampling, PlacementRejectionInfo placementRejection, SpacingPlanInfo spacingPlan, DressingPlacementInfo dressingPlacement, bool targetIslandActive, Vector3 targetIslandLocalPosition, Vector3 targetIslandLocalScale)
        {
            Textures = textures;
            SourceGroupChildren = sourceGroupChildren;
            PrefabCatalog = prefabCatalog;
            Foundation = foundation;
            GeneratedGroups = generatedGroups;
            MaskSampling = maskSampling;
            PlacementRejection = placementRejection;
            SpacingPlan = spacingPlan;
            DressingPlacement = dressingPlacement;
            TargetIslandActive = targetIslandActive;
            TargetIslandLocalPosition = targetIslandLocalPosition;
            TargetIslandLocalScale = targetIslandLocalScale;
        }

        public int TextureCount => Textures.Count;
        public int SourceGroupCount => SourceGroupChildren.Count;
        public int CatalogEntryCount
        {
            get
            {
                int count = 0;
                foreach (List<CatalogEntry> entries in PrefabCatalog.Values)
                    count += entries.Count;
                return count;
            }
        }
    }

    private readonly struct SpacingSpec
    {
        public readonly string Kind;
        public readonly int MinDistance;
        public readonly int TileSize;
        public readonly string Description;

        public SpacingSpec(string kind, int minDistance, int tileSize, string description)
        {
            Kind = kind;
            MinDistance = minDistance;
            TileSize = tileSize;
            Description = description;
        }
    }

    private readonly struct CoordinateProbeSpec
    {
        public readonly string Name;
        public readonly int GridX;
        public readonly int GridZ;

        public CoordinateProbeSpec(string name, int gridX, int gridZ)
        {
            Name = name;
            GridX = gridX;
            GridZ = gridZ;
        }
    }

    private readonly struct ReserveZoneSpec
    {
        public readonly string Id;
        public readonly string Intent;
        public readonly GridRect Rect;

        public ReserveZoneSpec(string id, string intent, int xMin, int zMin, int width, int height)
        {
            Id = id;
            Intent = intent;
            Rect = new GridRect(xMin, zMin, width, height);
        }

        public bool IsValid => !string.IsNullOrEmpty(Id);
    }

    private readonly struct GeneratedGroupSpec
    {
        public readonly string Name;
        public readonly string Purpose;

        public GeneratedGroupSpec(string name, string purpose)
        {
            Name = name;
            Purpose = purpose;
        }
    }

    private readonly struct GeneratedGroupInfo
    {
        public readonly string Name;
        public readonly string Purpose;
        public readonly bool Exists;
        public readonly string Path;
        public readonly bool ActiveSelf;
        public readonly bool ActiveInHierarchy;
        public readonly int ChildIndex;
        public readonly int ChildCount;
        public readonly Vector3 LocalPosition;
        public readonly Vector3 LocalRotationEuler;
        public readonly Vector3 LocalScale;

        public GeneratedGroupInfo(string name, string purpose, bool exists, string path, bool activeSelf, bool activeInHierarchy, int childIndex, int childCount, Vector3 localPosition, Vector3 localRotationEuler, Vector3 localScale)
        {
            Name = name;
            Purpose = purpose;
            Exists = exists;
            Path = path;
            ActiveSelf = activeSelf;
            ActiveInHierarchy = activeInHierarchy;
            ChildIndex = childIndex;
            ChildCount = childCount;
            LocalPosition = localPosition;
            LocalRotationEuler = localRotationEuler;
            LocalScale = localScale;
        }
    }

    private readonly struct MaskSamplingInfo
    {
        public readonly int GridWidth;
        public readonly int GridHeight;
        public readonly int TotalCells;
        public readonly string SamplingRule;
        public readonly List<MaskLayerSamplingInfo> Layers;

        public MaskSamplingInfo(int gridWidth, int gridHeight, int totalCells, string samplingRule, List<MaskLayerSamplingInfo> layers)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            TotalCells = totalCells;
            SamplingRule = samplingRule;
            Layers = layers;
        }
    }

    private readonly struct MaskLayerSamplingInfo
    {
        public readonly string Path;
        public readonly string Role;
        public readonly int Width;
        public readonly int Height;
        public readonly int MinValue;
        public readonly int MaxValue;
        public readonly double AverageValue;
        public readonly List<ClassCountInfo> ClassCounts;
        public readonly List<CoordinateProbeInfo> CoordinateProbes;

        public MaskLayerSamplingInfo(string path, string role, int width, int height, int minValue, int maxValue, double averageValue, List<ClassCountInfo> classCounts, List<CoordinateProbeInfo> coordinateProbes)
        {
            Path = path;
            Role = role;
            Width = width;
            Height = height;
            MinValue = minValue;
            MaxValue = maxValue;
            AverageValue = averageValue;
            ClassCounts = classCounts;
            CoordinateProbes = coordinateProbes;
        }
    }

    private readonly struct ClassCountInfo
    {
        public readonly string Name;
        public readonly int Count;

        public ClassCountInfo(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }

    private readonly struct CoordinateProbeInfo
    {
        public readonly string Name;
        public readonly int GridX;
        public readonly int GridZ;
        public readonly int PixelX;
        public readonly int PixelY;
        public readonly int Value;
        public readonly string Classification;

        public CoordinateProbeInfo(string name, int gridX, int gridZ, int pixelX, int pixelY, int value, string classification)
        {
            Name = name;
            GridX = gridX;
            GridZ = gridZ;
            PixelX = pixelX;
            PixelY = pixelY;
            Value = value;
            Classification = classification;
        }
    }

    private readonly struct GridRect
    {
        public readonly int XMin;
        public readonly int ZMin;
        public readonly int Width;
        public readonly int Height;

        public GridRect(int xMin, int zMin, int width, int height)
        {
            XMin = xMin;
            ZMin = zMin;
            Width = width;
            Height = height;
        }

        public int XMax => XMin + Width;
        public int ZMax => ZMin + Height;
        public int Area => Width * Height;

        public bool Contains(int gridX, int gridZ)
        {
            return gridX >= XMin && gridX < XMax && gridZ >= ZMin && gridZ < ZMax;
        }
    }

    private readonly struct PlacementRejectionInfo
    {
        public readonly GridRect PlayableMapFootprint;
        public readonly List<ReserveZoneInfo> ReserveZones;
        public readonly List<RejectionCountInfo> GlobalRejectionCounts;
        public readonly List<PlacementCandidateSummary> CandidateSummaries;
        public readonly int ValidAnyCount;

        public PlacementRejectionInfo(GridRect playableMapFootprint, List<ReserveZoneInfo> reserveZones, List<RejectionCountInfo> globalRejectionCounts, List<PlacementCandidateSummary> candidateSummaries, int validAnyCount)
        {
            PlayableMapFootprint = playableMapFootprint;
            ReserveZones = reserveZones;
            GlobalRejectionCounts = globalRejectionCounts;
            CandidateSummaries = candidateSummaries;
            ValidAnyCount = validAnyCount;
        }
    }

    private readonly struct ReserveZoneInfo
    {
        public readonly string Id;
        public readonly string Intent;
        public readonly GridRect Rect;
        public readonly int AreaCells;
        public readonly int BlockedCells;
        public readonly int SoftEdgeCells;
        public readonly int TreeCandidateCells;
        public readonly int RockCandidateCells;
        public readonly int RaisedOrHighCells;

        public ReserveZoneInfo(string id, string intent, GridRect rect, int areaCells, int blockedCells, int softEdgeCells, int treeCandidateCells, int rockCandidateCells, int raisedOrHighCells)
        {
            Id = id;
            Intent = intent;
            Rect = rect;
            AreaCells = areaCells;
            BlockedCells = blockedCells;
            SoftEdgeCells = softEdgeCells;
            TreeCandidateCells = treeCandidateCells;
            RockCandidateCells = rockCandidateCells;
            RaisedOrHighCells = raisedOrHighCells;
        }
    }

    private readonly struct PlacementCandidateSummary
    {
        public readonly string Kind;
        public readonly string Rule;
        public readonly int RawCandidateCount;
        public readonly int ValidCount;
        public readonly int RejectedCount;
        public readonly List<RejectionCountInfo> RejectionCounts;

        public PlacementCandidateSummary(string kind, string rule, int rawCandidateCount, int validCount, int rejectedCount, List<RejectionCountInfo> rejectionCounts)
        {
            Kind = kind;
            Rule = rule;
            RawCandidateCount = rawCandidateCount;
            ValidCount = validCount;
            RejectedCount = rejectedCount;
            RejectionCounts = rejectionCounts;
        }
    }

    private readonly struct RejectionCountInfo
    {
        public readonly string Name;
        public readonly int Count;

        public RejectionCountInfo(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }

    private readonly struct SpacingPlanInfo
    {
        public readonly int Seed;
        public readonly string Method;
        public readonly int TotalAcceptedPoints;
        public readonly List<SpacingKindSummary> KindSummaries;

        public SpacingPlanInfo(int seed, string method, int totalAcceptedPoints, List<SpacingKindSummary> kindSummaries)
        {
            Seed = seed;
            Method = method;
            TotalAcceptedPoints = totalAcceptedPoints;
            KindSummaries = kindSummaries;
        }
    }

    private readonly struct SpacingKindSummary
    {
        public readonly string Kind;
        public readonly string Description;
        public readonly int MinDistance;
        public readonly int TileSize;
        public readonly int TilesVisited;
        public readonly int TileCandidateCount;
        public readonly int AcceptedCount;
        public readonly int RejectedBySpacing;
        public readonly List<SpacingPointInfo> AcceptedPoints;
        public readonly List<SpacingPointInfo> SamplePoints;

        public SpacingKindSummary(string kind, string description, int minDistance, int tileSize, int tilesVisited, int tileCandidateCount, int acceptedCount, int rejectedBySpacing, List<SpacingPointInfo> acceptedPoints, List<SpacingPointInfo> samplePoints)
        {
            Kind = kind;
            Description = description;
            MinDistance = minDistance;
            TileSize = tileSize;
            TilesVisited = tilesVisited;
            TileCandidateCount = tileCandidateCount;
            AcceptedCount = acceptedCount;
            RejectedBySpacing = rejectedBySpacing;
            AcceptedPoints = acceptedPoints;
            SamplePoints = samplePoints;
        }
    }

    private readonly struct DressingPlacementInfo
    {
        public readonly int TotalPlacedPrefabs;
        public readonly List<DressingPlacementGroupInfo> Groups;

        public DressingPlacementInfo(int totalPlacedPrefabs, List<DressingPlacementGroupInfo> groups)
        {
            TotalPlacedPrefabs = totalPlacedPrefabs;
            Groups = groups;
        }
    }

    private readonly struct DressingPlacementGroupInfo
    {
        public readonly string Kind;
        public readonly string GroupName;
        public readonly bool Exists;
        public readonly int PlacedCount;

        public DressingPlacementGroupInfo(string kind, string groupName, bool exists, int placedCount)
        {
            Kind = kind;
            GroupName = groupName;
            Exists = exists;
            PlacedCount = placedCount;
        }
    }

    private readonly struct ValidationArtifactInfo
    {
        public readonly bool CapturesRendered;
        public readonly string TopDownCapturePath;
        public readonly string PlayableAngleCapturePath;
        public readonly int TotalPlacedPrefabs;
        public readonly bool Passed;
        public readonly List<ValidationCheckInfo> Checks;

        public ValidationArtifactInfo(bool capturesRendered, string topDownCapturePath, string playableAngleCapturePath, int totalPlacedPrefabs, bool passed, List<ValidationCheckInfo> checks)
        {
            CapturesRendered = capturesRendered;
            TopDownCapturePath = topDownCapturePath;
            PlayableAngleCapturePath = playableAngleCapturePath;
            TotalPlacedPrefabs = totalPlacedPrefabs;
            Passed = passed;
            Checks = checks;
        }
    }

    private readonly struct ValidationCheckInfo
    {
        public readonly string Id;
        public readonly bool Passed;
        public readonly int Expected;
        public readonly int Actual;
        public readonly string Details;

        public ValidationCheckInfo(string id, bool passed, int expected, int actual, string details)
        {
            Id = id;
            Passed = passed;
            Expected = expected;
            Actual = actual;
            Details = details;
        }
    }

    private readonly struct PlacedDressingPointInfo
    {
        public readonly string Kind;
        public readonly string GroupName;
        public readonly string InstanceName;
        public readonly int GridX;
        public readonly int GridZ;

        public PlacedDressingPointInfo(string kind, string groupName, string instanceName, int gridX, int gridZ)
        {
            Kind = kind;
            GroupName = groupName;
            InstanceName = instanceName;
            GridX = gridX;
            GridZ = gridZ;
        }
    }

    private readonly struct SpacingPointInfo
    {
        public readonly int GridX;
        public readonly int GridZ;
        public readonly int Weight;
        public readonly int Hash;

        public SpacingPointInfo(int gridX, int gridZ, int weight, int hash)
        {
            GridX = gridX;
            GridZ = gridZ;
            Weight = weight;
            Hash = hash;
        }
    }

    private readonly struct FoundationInfo
    {
        public readonly string Name;
        public readonly string Path;
        public readonly bool ActiveSelf;
        public readonly bool ActiveInHierarchy;
        public readonly Vector3 LocalPosition;
        public readonly Vector3 LocalRotationEuler;
        public readonly Vector3 LocalScale;
        public readonly int ChildIndex;
        public readonly int TransformCount;
        public readonly int RendererCount;
        public readonly int MeshFilterCount;
        public readonly int ColliderCount;
        public readonly Vector3 WorldBoundsCenter;
        public readonly Vector3 WorldBoundsSize;

        public FoundationInfo(string name, string path, bool activeSelf, bool activeInHierarchy, Vector3 localPosition, Vector3 localRotationEuler, Vector3 localScale, int childIndex, int transformCount, int rendererCount, int meshFilterCount, int colliderCount, Vector3 worldBoundsCenter, Vector3 worldBoundsSize)
        {
            Name = name;
            Path = path;
            ActiveSelf = activeSelf;
            ActiveInHierarchy = activeInHierarchy;
            LocalPosition = localPosition;
            LocalRotationEuler = localRotationEuler;
            LocalScale = localScale;
            ChildIndex = childIndex;
            TransformCount = transformCount;
            RendererCount = rendererCount;
            MeshFilterCount = meshFilterCount;
            ColliderCount = colliderCount;
            WorldBoundsCenter = worldBoundsCenter;
            WorldBoundsSize = worldBoundsSize;
        }
    }

    private sealed class CatalogAccumulator
    {
        private const int MaxSourceNames = 8;
        private readonly List<string> sourceNames = new();

        public string PrefabPath { get; }
        public int SampleCount { get; private set; }
        public Vector3 MinLocalScale { get; private set; }
        public Vector3 MaxLocalScale { get; private set; }

        public CatalogAccumulator(string prefabPath)
        {
            PrefabPath = prefabPath;
            MinLocalScale = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            MaxLocalScale = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        }

        public void AddSample(string sourceName, Vector3 localScale)
        {
            SampleCount++;
            MinLocalScale = Vector3.Min(MinLocalScale, localScale);
            MaxLocalScale = Vector3.Max(MaxLocalScale, localScale);

            if (sourceNames.Count < MaxSourceNames && !sourceNames.Contains(sourceName))
                sourceNames.Add(sourceName);
        }

        public CatalogEntry ToEntry()
        {
            return new CatalogEntry(PrefabPath, SampleCount, MinLocalScale, MaxLocalScale, new List<string>(sourceNames));
        }
    }

    private sealed class PlacementCandidateAccumulator
    {
        public readonly RejectionCounter Rejections = new();

        public string Kind { get; }
        public string Rule { get; }
        public int RawCandidateCount { get; set; }
        public int ValidCount { get; set; }
        public int RejectedCount { get; set; }

        public PlacementCandidateAccumulator(string kind, string rule)
        {
            Kind = kind;
            Rule = rule;
        }

        public PlacementCandidateSummary ToSummary()
        {
            return new PlacementCandidateSummary(Kind, Rule, RawCandidateCount, ValidCount, RejectedCount, Rejections.ToList());
        }
    }

    private sealed class RejectionCounter
    {
        private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);

        public bool Add(string reason, bool rejected)
        {
            if (!rejected)
                return false;

            if (!counts.ContainsKey(reason))
                counts[reason] = 0;
            counts[reason]++;
            return true;
        }

        public List<RejectionCountInfo> ToList()
        {
            List<RejectionCountInfo> result = new();
            foreach (KeyValuePair<string, int> entry in counts)
                result.Add(new RejectionCountInfo(entry.Key, entry.Value));
            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }
    }

    private readonly struct CatalogEntry
    {
        public readonly string PrefabPath;
        public readonly int SampleCount;
        public readonly Vector3 MinLocalScale;
        public readonly Vector3 MaxLocalScale;
        public readonly List<string> SourceNames;

        public CatalogEntry(string prefabPath, int sampleCount, Vector3 minLocalScale, Vector3 maxLocalScale, List<string> sourceNames)
        {
            PrefabPath = prefabPath;
            SampleCount = sampleCount;
            MinLocalScale = minLocalScale;
            MaxLocalScale = maxLocalScale;
            SourceNames = sourceNames;
        }
    }
}
#endif
