using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Game.Composition;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Newtonsoft.Json.Linq;

internal static class S073LegacyContextControls
{
    static SkirmishBattleCatalogConfig Catalog(string map = "IB", int index = 3, string definition = "", int version = 0)
    {
        var catalog = (SkirmishBattleCatalogConfig)FormatterServices.GetUninitializedObject(typeof(SkirmishBattleCatalogConfig));
        catalog.Configure(new[]
        {
            new SkirmishBattleCatalogEntry
            {
                ScenarioId = "S073", MapId = map, ObjectiveId = "BA", ArmyProfile = "G", StartProfile = "F",
                Status = SkirmishBattleCatalogStatus.Playable, PlayableScenarioIndex = index,
                DefinitionId = definition, ContentVersion = version
            }
        });
        return catalog;
    }

    static SkirmishMatchState Match(int index = 3, SkirmishOutcome outcome = SkirmishOutcome.None) => new()
    {
        SessionId = new FixedString64Bytes("S073-source-control"),
        ScenarioIndex = index,
        Phase = SkirmishPhase.Playing,
        StartupFailure = SkirmishStartupFailureCode.None,
        Outcome = outcome,
        Reason = SkirmishEndReason.None
    };

    static void Reject(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static BlobAssetReference<MapSurfaceBlob> PlacementSurface(int secondLayer)
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.Dimensions = new int2(2, 1);
        root.GridOrigin = float3.zero;
        root.CellSize = 1f;
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;
        var cells = builder.Allocate(ref root.Cells, 2);
        var samples = builder.Allocate(ref root.Samples, 2);
        builder.Allocate(ref root.Connections, 0);
        builder.Allocate(ref root.CompactSamples, 0);
        for (int i = 0; i < 2; i++)
        {
            cells[i] = new MapSurfaceCell { FirstSurfaceIndex = i, SurfaceCount = 1 };
            samples[i] = new MapSurfaceSample
            {
                Cell = new int2(i, 0), SurfaceId = 100 + i, LayerId = i == 0 ? 0 : secondLayer,
                Height = 1f, SlopeDegrees = 0f
            };
        }
        return builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
    }

    static void PlacementSurfaceIdentity()
    {
        var helper = new BuildingSurfacePlacementUtilitySystemHelper();
        var sameLayer = PlacementSurface(0);
        try
        {
            var surface = new MapSurfaceComponent
            {
                SurfaceBlob = sameLayer, Dimensions = new int2(2, 1), CellSize = 1f, HasSurfaceData = 1
            };
            Reject(helper.TryEvaluateFootprint(surface, new Vector2Int(0, 0), new Vector2Int(2, 1),
                0f, 0f, out var result) && result.IsValid && result.SampleCount == 2 &&
                result.SurfaceId == 100 && result.LayerId == 0,
                "Different per-cell SurfaceIds on one flat layer must allow building placement");
        }
        finally { sameLayer.Dispose(); }

        var differentLayer = PlacementSurface(1);
        try
        {
            var surface = new MapSurfaceComponent
            {
                SurfaceBlob = differentLayer, Dimensions = new int2(2, 1), CellSize = 1f, HasSurfaceData = 1
            };
            Reject(helper.TryEvaluateFootprint(surface, new Vector2Int(0, 0), new Vector2Int(2, 1),
                0f, 0f, out var result) && !result.IsValid && result.SampleCount == 1,
                "Footprints crossing stacked surface layers must remain rejected");
        }
        finally { differentLayer.Dispose(); }
        Console.WriteLine("[S073PlacementSurfaceIdentity] result=Passed distinctSampleIdsAllowed=1 mixedLayersRejected=1");
    }

    static void VerifySelectedSurfacePlacement(OperationMapDefinition selected)
    {
        string graphPath = Environment.GetEnvironmentVariable("WARLINE_S073_SURFACE_GRAPH_PATH");
        string graphSha = Environment.GetEnvironmentVariable("WARLINE_S073_SURFACE_GRAPH_SHA256");
        Reject(!string.IsNullOrEmpty(graphPath) && !string.IsNullOrEmpty(graphSha),
            "Pinned selected S073 surface source graph required");
        byte[] graphBytes = File.ReadAllBytes(graphPath);
        using (var sha = SHA256.Create())
            Reject(BitConverter.ToString(sha.ComputeHash(graphBytes)).Replace("-", "").ToLowerInvariant() == graphSha,
                "Selected S073 surface source graph changed");
        var graph = JObject.Parse(System.Text.Encoding.UTF8.GetString(graphBytes));
        var materializer = new SourceConfigMaterializer(graph);
        var gridAsset = materializer.ByPath<GridAuthoringSceneConfigAsset>(
            "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset");
        var surfaceAsset = materializer.ByPath<MapSurfaceDataAsset>(
            "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
        var metadata = selected.GridMetadata;
        var origin = metadata.Origin;
        Console.WriteLine("[S073SurfaceAssetPins] gridGuid=" + metadata.AssetGuid + " gridHash=" + metadata.ContentHash +
            " gridFrame=" + gridAsset.Width + "x" + gridAsset.Height + "/" + gridAsset.CellSize +
            "@" + gridAsset.Origin + " metadataFrame=" + metadata.Dimensions + "/" + metadata.CellSize + "@" + origin +
            " surfaceGuid=" + selected.SurfaceMetadata.AssetGuid + " surfaceHash=" + selected.SurfaceMetadata.ContentHash);
        bool gridIdentity = metadata.AssetGuid == "b201000000000000000000000000000b" &&
                            metadata.ContentHash == "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb";
        bool gridFrame = gridAsset.Width == metadata.Dimensions.x && gridAsset.Height == metadata.Dimensions.y &&
                         gridAsset.CellSize == metadata.CellSize && gridAsset.Origin.x == origin.x &&
                         gridAsset.Origin.y == origin.y && gridAsset.Origin.z == origin.z;
        bool surfaceIdentity = selected.SurfaceMetadata.AssetGuid == "12f517deb32ab49698acbfdaf7c3eac7" &&
                               selected.SurfaceMetadata.ContentHash == "5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d";
        Console.WriteLine("[S073SurfaceAssetPinChecks] grid_identity=" + gridIdentity + " grid_frame=" + gridFrame + " surface_identity=" + surfaceIdentity);
        Reject(gridIdentity && gridFrame && surfaceIdentity,
            "Selected S073 source does not match the pinned Campaign grid/surface assets");
        Console.WriteLine("[S073SurfaceAssetPayload] has=" + surfaceAsset.HasCompactPayload + " bytes=" +
            surfaceAsset.CompressedPayloadBytes + " frame=" + surfaceAsset.Dimensions + "/" + surfaceAsset.CellSize +
            " version=" + surfaceAsset.PayloadVersion + " encoding=" + surfaceAsset.PayloadEncoding +
            " declared_samples=" + surfaceAsset.SurfaceCount);
        Reject(surfaceAsset.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var blob),
            "Original selected Campaign surface decoder rejected the S073 source");
        try
        {
            ref var value = ref blob.Value;
            Reject(value.Dimensions.x == metadata.Dimensions.x && value.Dimensions.y == metadata.Dimensions.y &&
                   value.CellSize == metadata.CellSize && value.GridOrigin.x == origin.x &&
                   value.GridOrigin.y == origin.y && value.GridOrigin.z == origin.z &&
                   MapSurfaceBlobAccess.SurfaceCount(ref value) == selected.SurfaceMetadata.SurfaceCount,
                "Decoded S073 surface frame or sample count differs from its selected Campaign source");
            var firstCell = new int2(950, 438);
            MapSurfaceSample first = default, second = default;
            bool firstFound = MapSurfaceBlobAccess.TryGetPrimarySurface(ref value, firstCell, out first);
            bool secondFound = MapSurfaceBlobAccess.TryGetPrimarySurface(ref value, firstCell + new int2(1, 0), out second);
            Reject(firstFound && secondFound &&
                   first.SurfaceId != second.SurfaceId && first.LayerId == second.LayerId,
                "S073 Barracks witness no longer has distinct sample IDs on one surface layer");
            var runtime = new MapSurfaceComponent
            {
                SurfaceBlob = blob, GridOrigin = new float3(origin.x, origin.y, origin.z),
                CellSize = metadata.CellSize, Dimensions = new int2(metadata.Dimensions.x, metadata.Dimensions.y),
                HasSurfaceData = 1
            };
            var helper = new BuildingSurfacePlacementUtilitySystemHelper();
            Reject(helper.TryEvaluateFootprint(runtime, new Vector2Int(950, 438), new Vector2Int(28, 15),
                    0.5f, 45f, out var result) && result.IsValid && result.SampleCount == 420 &&
                   result.LayerId == first.LayerId,
                "Original S073 Barracks footprint still fails the source surface owner");
            Console.WriteLine("[S073SourceSurfaceReplay] result=Passed origin=950,438 footprint=28x15 sample_count=" +
                result.SampleCount + " first_surface_ids=" + first.SurfaceId + "," + second.SurfaceId +
                " layer=" + result.LayerId + " height_delta=" + result.MaxFootprintHeightDelta.ToString("R") +
                " max_slope=" + result.MaxFootprintSlopeDegrees.ToString("R"));
        }
        finally { blob.Dispose(); }
    }

    static void VerifyNativeAuthoredMaskIfRequested(SourcePlacementDomain domain, OperationMapDefinition source, string sceneGuid)
    {
        string path = Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_AUTHORED_MASK_PATH");
        if (string.IsNullOrEmpty(path)) return;
        string expectedSha = Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_AUTHORED_MASK_SHA256");
        bool[] cache = SourceS073LegacySpawnRequestAdapter.LoadOriginalAuthoredBuildingCache(source, path, expectedSha, sceneGuid);
        Reject(cache.Length == 2048 * 1024 && cache.Count(value => value) == 376518,
            "Pinned S073 original authored-cache mask cardinality changed");
        int clear = 0;
        for (int y = 438; y < 438 + 15; y++)
            for (int x = 950; x < 950 + 28; x++) if (!cache[y * 2048 + x]) clear++;
        Reject(clear == 28 * 15, "Original Barracks placement witness is not clear in the captured authored-cache domain");
        var prior = domain.Build(2048, 1024, 1f, 0f, 0f, 0f);
        Reject(prior.AuthoredBuildingCells == 352592 && !domain.UsesS073NativeAuthoredBuildingMask,
            "S002 comparison baseline must remain unchanged before explicit S073 owner binding");
        string priorReference = domain.ReferenceSha256;
        string priorWaterReference = domain.WaterReferenceSha256;
        var bound = domain.BindValidatedS073NativeAuthoredBuildingMask(source.OperationMapId, source.SourceIdentityHash, source.ContentHash, cache);
        var rebound = bound.Build(2048, 1024, 1f, 0f, 0f, 0f);
        Reject(bound.UsesS073NativeAuthoredBuildingMask && bound.S073NativeAuthoredBuildingMaskSha256 == expectedSha &&
               bound.S073NativeAuthoredBuildingMaskOccupiedCells == 376518 && rebound.AuthoredBuildingCells == 376518 &&
               rebound.AuthoredBuildings.SequenceEqual(cache) && domain.Build(2048, 1024, 1f, 0f, 0f, 0f).AuthoredBuildingCells == 352592 &&
               bound.ReferenceSha256 == priorReference && bound.WaterReferenceSha256 == priorWaterReference &&
               bound.WaterFootprintCount == domain.WaterFootprintCount && bound.CopyOriginalOwnerRoadMask().SequenceEqual(domain.CopyOriginalOwnerRoadMask()),
            "S073 source domain must bind exact native occupancy while preserving the separate S002 diagnostics");
        var wrongMask = (bool[])cache.Clone(); wrongMask[0] = !wrongMask[0];
        bool wrongMaskRejected = false;
        try { domain.BindValidatedS073NativeAuthoredBuildingMask(source.OperationMapId, source.SourceIdentityHash, source.ContentHash, wrongMask); }
        catch (InvalidDataException) { wrongMaskRejected = true; }
        Reject(wrongMaskRejected, "Modified native occupancy bytes must fail the retained exact hash");
        bool wrongSourceRejected = false;
        var alias = SourceMap(map: "opmap.skirmish.desert_base_01");
        try { domain.BindValidatedS073NativeAuthoredBuildingMask(alias.OperationMapId, alias.SourceIdentityHash, alias.ContentHash, cache); }
        catch (NotSupportedException) { wrongSourceRejected = true; }
        Reject(wrongSourceRejected, "S073 native occupancy binding must reject a source-map alias");
        Console.WriteLine("[S073NativeAuthoredMaskBindingParity] result=Passed native_occupied=" + cache.Count(value => value) +
            " bound_occupied=" + rebound.AuthoredBuildingCells + " mismatch_cells=0 prior_s002_occupied=" + prior.AuthoredBuildingCells +
            " source_hash=" + bound.S073NativeAuthoredBuildingMaskSha256 + " diagnostic_masks_preserved=1 source_fit=0");
    }

    static void SetField(object value, string name, object fieldValue) =>
        value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, fieldValue);

    static OperationMapDefinition SourceMap(string map = "opmap.skirmish.industrial_basin", string sourceId = "opmap.skirmish.desert_base_01",
        string identity = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900",
        string sourceContent = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c",
        int width = 2048, string gridGuid = "b201000000000000000000000000000b",
        string surfaceGuid = "12f517deb32ab49698acbfdaf7c3eac7",
        string authoredScene = "c00140f2e94a04c3084c8dcb0c18cbd0")
    {
        var source = (OperationMapDefinition)FormatterServices.GetUninitializedObject(typeof(OperationMapDefinition));
        SetField(source, "operationMapId", map);
        SetField(source, "sourceIdentityHash", identity);
        SetField(source, "sourceBinding", new OperationMapSourceBindingConfig(sourceId, identity, sourceContent));
        SetField(source, "gridMetadata", new OperationMapGridMetadataConfig(gridGuid,
            "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb", Vector3.zero, new Vector2Int(width, 1024), 1, 0));
        SetField(source, "surfaceMetadata", new OperationMapSurfaceMetadataConfig(surfaceGuid,
            "5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d", "8810a336a366016217a73bf104b45a3a", 2048 * 1024, 3, 1, -5.31082f, 17.199179f));
        SetField(source, "navigationMetadata", new OperationMapNavigationMetadataConfig(authoredScene, 146043441, 0, true, true, true));
        SetField(source, "mapSurfaceDataReference", new AssetReference(surfaceGuid));
        return source;
    }

    static void SharedPlacementSourceBinding()
    {
        string mapPath = Environment.GetEnvironmentVariable("WARLINE_S073_OPERATION_MAP_PATH");
        string expectedMapSha = Environment.GetEnvironmentVariable("WARLINE_S073_OPERATION_MAP_SHA256");
        Reject(!string.IsNullOrEmpty(mapPath) && !string.IsNullOrEmpty(expectedMapSha), "Pinned current S073 source map required");
        byte[] bytes = File.ReadAllBytes(mapPath);
        using (var sha = SHA256.Create())
            Reject(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() == expectedMapSha,
                "Current S073 operation-map source changed");
        string text = System.Text.Encoding.UTF8.GetString(bytes);
        string Root(string key) => Regex.Match(text, "(?m)^  " + Regex.Escape(key) + @":\s*(.+)$").Groups[1].Value.Trim();
        string Section(string name) => Regex.Match(text, "(?ms)^  " + Regex.Escape(name) + @":\r?\n(?<body>.*?)(?=^  [A-Za-z]|\z)").Groups["body"].Value;
        string Nested(string section, string key) => Regex.Match(Section(section), "(?m)^    " + Regex.Escape(key) + @":\s*(.+)$").Groups[1].Value.Trim();
        Vector3 ParseOrigin(string value) { var m=Regex.Match(value, @"\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}");return new Vector3(float.Parse(m.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture),float.Parse(m.Groups[2].Value,System.Globalization.CultureInfo.InvariantCulture),float.Parse(m.Groups[3].Value,System.Globalization.CultureInfo.InvariantCulture)); }
        Vector2Int ParseDimensions(string value) { var m=Regex.Match(value, @"\{x:\s*(\d+),\s*y:\s*(\d+)\}");return new Vector2Int(int.Parse(m.Groups[1].Value),int.Parse(m.Groups[2].Value)); }
        string surfaceGuid = Regex.Match(Section("mapSurfaceDataReference"), "(?m)^    m_AssetGUID:\\s*(.+)$").Groups[1].Value.Trim();
        var actual=(OperationMapDefinition)FormatterServices.GetUninitializedObject(typeof(OperationMapDefinition));
        SetField(actual,"operationMapId",Root("operationMapId"));SetField(actual,"sourceIdentityHash",Root("sourceIdentityHash"));SetField(actual,"contentHash",Root("contentHash"));
        SetField(actual,"sourceBinding",new OperationMapSourceBindingConfig(Nested("sourceBinding","sourceOperationMapId"),Nested("sourceBinding","sourceIdentityHash"),Nested("sourceBinding","sourceContentHash")));
        SetField(actual,"gridMetadata",new OperationMapGridMetadataConfig(Nested("gridMetadata","assetGuid"),Nested("gridMetadata","contentHash"),ParseOrigin(Nested("gridMetadata","origin")),ParseDimensions(Nested("gridMetadata","dimensions")),float.Parse(Nested("gridMetadata","cellSize"),System.Globalization.CultureInfo.InvariantCulture),0));
        SetField(actual,"surfaceMetadata",new OperationMapSurfaceMetadataConfig(Nested("surfaceMetadata","assetGuid"),Nested("surfaceMetadata","contentHash"),Nested("surfaceMetadata","runtimeBlobHash"),int.Parse(Nested("surfaceMetadata","surfaceCount")),int.Parse(Nested("surfaceMetadata","payloadVersion")),byte.Parse(Nested("surfaceMetadata","payloadEncoding")),float.Parse(Nested("surfaceMetadata","minimumHeight"),System.Globalization.CultureInfo.InvariantCulture),float.Parse(Nested("surfaceMetadata","maximumHeight"),System.Globalization.CultureInfo.InvariantCulture)));
        SetField(actual,"navigationMetadata",new OperationMapNavigationMetadataConfig(Nested("navigationMetadata","authoredSubSceneGuid"),long.Parse(Nested("navigationMetadata","gridAuthoringLocalId")),int.Parse(Nested("navigationMetadata","staticGridBlockerCount")),Nested("navigationMetadata","usesSurfaceMovementMetadata")=="1",Nested("navigationMetadata","supportsDynamicBlockers")=="1",Nested("navigationMetadata","supportsDynamicOccupancy")=="1"));
        SetField(actual,"mapSurfaceDataReference",new AssetReference(surfaceGuid));
        SourceS073LegacySpawnRequestAdapter.RequireSharedPlacementSource(actual);
        VerifySelectedSurfacePlacement(actual);
        var domain = SourceS073LegacySpawnRequestAdapter.LoadSharedPlacementSource(actual,
            Environment.GetEnvironmentVariable("WARLINE_S073_PHYSICAL_PATH"), Environment.GetEnvironmentVariable("WARLINE_S073_PHYSICAL_SHA256"),
            Environment.GetEnvironmentVariable("WARLINE_S073_WATER_PATH"), Environment.GetEnvironmentVariable("WARLINE_S073_WATER_SHA256"),
            Environment.GetEnvironmentVariable("WARLINE_S073_STARTUP_PATH"), Environment.GetEnvironmentVariable("WARLINE_S073_STARTUP_SHA256"),
            Environment.GetEnvironmentVariable("WARLINE_S073_SOURCE_ROOT"));
        Reject(domain.UsesOriginalOwnerMaskCapture && domain.WaterDomainKnown && domain.WaterFootprintCount == 194,
            "S073 selected source must retain the existing complete placement and active-water captures");
        string sourceSceneGuid = Regex.Match(Section("sourceSceneReference"), "(?m)^    m_AssetGUID:\\s*(.+)$").Groups[1].Value.Trim();
        VerifyNativeAuthoredMaskIfRequested(domain, actual, sourceSceneGuid);
        SourceS073LegacySpawnRequestAdapter.RequireSharedPlacementSource(SourceMap());
        foreach (var source in new[]
        {
            SourceMap(map: "opmap.skirmish.desert_base_01"),
            SourceMap(identity: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            SourceMap(sourceContent: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            SourceMap(width: 2047),
            SourceMap(gridGuid: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            SourceMap(surfaceGuid: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            SourceMap(authoredScene: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
        })
        {
            bool rejected = false;
            try { SourceS073LegacySpawnRequestAdapter.RequireSharedPlacementSource(source); }
            catch (NotSupportedException) { rejected = true; }
            Reject(rejected, "S073 source-binding alias accepted mismatched map/grid/surface/subscene facts");
        }
    }

    static void RequestProjection()
    {
        var em = new EntityManager();
        int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var plan = em.CreateEntity();
        em.AddComponentData(plan, new InitialUnitsSpawnConfig());
        var factionRows = em.AddBuffer<InitialUnitsFactionSpawnEntry>(plan);
        factionRows.Add(new InitialUnitsFactionSpawnEntry { FactionId = 1, SpawnCell = new int2(800, 600) });
        var buildingRows = em.AddBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan);
        buildingRows.Add(new InitialUnitsFactionBuildingSpawnEntry
        {
            FactionId = 1,
            Prefab = Entity.Null,
            PrefabLookupKey = new FixedString128Bytes("building_barrack"),
            OriginOffset = new int2(0, -20)
        });
        var match = em.CreateEntity();
        em.AddComponentData(match, new SkirmishMatchState());
        var boundary = em.CreateEntity();
        var requests = em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        Reject(SourceS073LegacySpawnRequestAdapter.Queue(em, plan, boundary, true) == 1 && requests.Length == 1,
            "Source host did not queue one authored legacy building request");
        int2 origin = new(800, 580);
        var request = requests[0];
        Reject(request.RequestKind == BuildingRuntimeSpawnRequest.KindBuilding &&
               request.Status == BuildingRuntimeSpawnRequest.Pending && request.HasOwnerFaction == 1 &&
               request.RequirePreferredOrigin == 1 && request.PlanEntity == plan && request.PreferredOrigin.Equals(origin),
            "Original exact-site request projection changed");
        Reject(request.BuildingId.ToString() == BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("building_barrack"),
            "Original configured building key normalization changed");
        Reject(GeneratedRuntimeSpawnProcessor.IsLegacyInitialRequest(em, request),
            "Original request was not admitted by the existing runtime spawn processor");

        Reject(SourceS073LegacySpawnRequestAdapter.Queue(em, plan, boundary, false) == 1 && requests.Length == 2,
            "Source host did not queue the non-exact control request");
        var nonExact = requests[1];
        Reject(nonExact.RequirePreferredOrigin == 0 && !GeneratedRuntimeSpawnProcessor.IsLegacyInitialRequest(em, nonExact),
            "Ordinary request incorrectly crossed the exact legacy initial-spawn boundary");
    }

    static void LegacyArmySelection(in GroundMissionContext legacyContext)
    {
        var em = new EntityManager();
        int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var match = em.CreateEntity();
        em.AddComponentData(match, Match());

        Entity Member(byte faction, byte slot)
        {
            var unit = em.CreateEntity();
            em.AddComponentData(unit, new Faction { Id = faction });
            em.AddComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
            em.AddComponentData(unit, new UnitGrid { Cell = new int2(slot, faction) });
            em.AddComponentData(unit, new UnitMove { Speed = 1, WalkSpeed = 1 });
            em.AddComponentData(unit, LocalTransform.FromPosition(new float3(slot, 0, faction)));
            em.AddComponentData(unit, new SkirmishSquadMember { Slot = slot });
            return unit;
        }

        Entity playerSlot1 = Member(FactionIdentity.PlayerFactionId, 1);
        Entity playerSlot2 = Member(FactionIdentity.PlayerFactionId, 2);
        Entity enemySlot1 = Member(FactionIdentity.EnemyFactionId, 1);
        var publicMarkers = new SourcePublicPlayerContactOwner(em, match, legacyContext);
        publicMarkers.UpdateAfterFog(1, 0f);
        var publicSnapshot = publicMarkers.ReadSnapshot();
        Reject(publicSnapshot.Identity > 0 && publicSnapshot.Generation == 1 && publicSnapshot.Players.Length == 2 &&
               publicSnapshot.Players.Any(player => player.X == 1 && player.Z == FactionIdentity.PlayerFactionId) &&
               publicSnapshot.Players.Any(player => player.X == 2 && player.Z == FactionIdentity.PlayerFactionId) &&
               !publicSnapshot.Players.Any(player => player.Z == FactionIdentity.EnemyFactionId),
            "S073 legacy ARIA public marker owner must expose actual player units and omit hostile transforms");
        em.AddComponent<SelectedUnitTag>(enemySlot1);
        Reject(SkirmishArmySelectionService.TrySelectLegacySquad(em, match, 1, false, out var select1) &&
               select1.Accepted && select1.IssuedCount == 1 &&
               em.HasComponent<SelectedUnitTag>(playerSlot1) && !em.HasComponent<SelectedUnitTag>(playerSlot2) &&
               !em.HasComponent<SelectedUnitTag>(enemySlot1),
            "Legacy slot selection must select only living player members and clear the prior selection");

        Reject(SkirmishArmySelectionService.TrySelectLegacySquad(em, match, 2, true, out var add) &&
               add.Accepted && add.IssuedCount == 1 &&
               em.HasComponent<SelectedUnitTag>(playerSlot1) && em.HasComponent<SelectedUnitTag>(playerSlot2),
            "Legacy additive selection must retain existing selection");
        Reject(!SkirmishArmySelectionService.TrySelectLegacySquad(em, match, 4, false, out _) &&
               em.HasComponent<SelectedUnitTag>(playerSlot1) && em.HasComponent<SelectedUnitTag>(playerSlot2),
            "Rejected empty legacy slot must not clear current selection");

        var wrongMission = em.GetComponentData<SkirmishMatchState>(match);
        wrongMission.ScenarioIndex = 4;
        em.SetComponentData(match, wrongMission);
        Reject(!SkirmishArmySelectionService.TrySelectLegacySquad(em, match, 1, false, out _),
            "Legacy squad selection must remain bound to the original S073 dispatch index");
    }

    // Bounded pure policy fixture: it verifies the extracted production outcome
    // owner and its phase gate; it does not represent a mission-world outcome.
    static void LegacyOutcomePolicy()
    {
        var queued = new SkirmishMatchState
        {
            SessionId = new FixedString64Bytes("S073-queued-policy-control"),
            ScenarioIndex = 3,
            Phase = SkirmishPhase.Queued
        };
        Reject(!SkirmishOutcomeRules.Evaluate(ref queued, false, false, 10f, true, true) &&
               queued.Phase == SkirmishPhase.Queued && queued.ElapsedSeconds == 0f &&
               queued.Outcome == SkirmishOutcome.None && queued.Reason == SkirmishEndReason.None,
            "Queued legacy state must not advance or fabricate an outcome");

        var destroyedBase = Match();
        Reject(SkirmishOutcomeRules.Evaluate(ref destroyedBase, true, false, 1f, true) &&
               destroyedBase.Phase == SkirmishPhase.Finished &&
               destroyedBase.Outcome == SkirmishOutcome.Victory &&
               destroyedBase.Reason == SkirmishEndReason.MainBaseDestroyed &&
               destroyedBase.ElapsedSeconds == 1f,
            "Original legacy base-destruction outcome policy changed");

        var surrender = Match();
        Reject(SkirmishOutcomeRules.Evaluate(ref surrender, true, true, 3f, false, true) &&
               surrender.Phase == SkirmishPhase.Finished && surrender.Outcome == SkirmishOutcome.Defeat &&
               surrender.Reason == SkirmishEndReason.Surrender && surrender.ElapsedSeconds == 0f,
            "Original legacy surrender outcome policy changed");
    }

    static EntityManager NewLegacyQueueManager()
    {
        var em = new EntityManager();
        int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        return em;
    }

    static QuickGameConfig S073LaunchConfig(int seed = 104729)
    {
        var config = QuickGameConfig.Defaults;
        config.ScenarioIndex = SkirmishPresetConfig.IndustrialBasinScenarioIndex;
        config.MapSeed = seed;
        return config;
    }

    static int SessionCount(EntityManager em)
    {
        using var sessions = em.CreateEntityQuery(typeof(SkirmishMatchState));
        return sessions.CalculateEntityCount();
    }

    static void LegacyQueueOwner()
    {
        var valid = NewLegacyQueueManager();
        var retry = valid.CreateEntity();
        valid.AddComponentData(retry, new MatchStartQueueComponent { LastRequestId = 17 });
        var retryRequests = valid.AddBuffer<MatchStartRequestElement>(retry);
        retryRequests.Add(new MatchStartRequestElement { RequestId = 17 });
        var retryResults = valid.AddBuffer<MatchStartResultElement>(retry);
        retryResults.Add(new MatchStartResultElement { RequestId = 17, Status = MatchStartStatusKind.Failed });
        valid.AddComponentData(retry, new MatchStartProgressComponent { Progress01 = 0.5f });
        Reject(SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(valid, S073LaunchConfig()),
            "Original legacy queue owner rejected the admitted source S073 fixture");
        using (var sessions = valid.CreateEntityQuery(typeof(SkirmishMatchState)))
        {
            Reject(sessions.CalculateEntityCount() == 1, "Legacy queue owner did not create exactly one session");
            var session = sessions.GetSingletonEntity();
            var match = valid.GetComponentData<SkirmishMatchState>(session);
            Reject(match.ScenarioIndex == 3 && match.Seed == 104729 && match.Phase == SkirmishPhase.Queued &&
                   match.SessionId.Length == 32 && valid.HasBuffer<SkirmishActionRequest>(session) &&
                   valid.HasBuffer<SkirmishTrackedUnit>(session) &&
                   valid.HasComponent<SkirmishLaunchSnapshot>(session) &&
                   !valid.HasComponent<SkirmishExpandedSessionComponent>(session),
                "Legacy queue session identity, buffers, snapshot, or phase changed");
            var snapshot = valid.GetComponentObject<SkirmishLaunchSnapshot>(session).Configuration;
            Reject(snapshot.ScenarioIndex == 3 && snapshot.MapSeed == 104729,
                "Legacy launch snapshot lost normalized source scenario or seed");
        }
        Reject(valid.GetComponentData<MatchStartQueueComponent>(retry).LastRequestId == 17 &&
               retryRequests.Length == 0 && retryResults.Length == 0 &&
               valid.HasComponent<MatchStartProgressComponent>(retry) &&
               valid.GetComponentData<MatchStartProgressComponent>(retry).Progress01 == 0f,
            "Legacy retry cleanup changed its request id or retained prior request/result/progress");

        var campaign = NewLegacyQueueManager();
        var campaignRoot = campaign.CreateEntity();
        campaign.AddBuffer<CampaignMissionLaunchRequestElement>(campaignRoot)
            .Add(new CampaignMissionLaunchRequestElement { SchemaVersion = 1, MissionId = new FixedString64Bytes("campaign.pending") });
        Reject(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(campaign, S073LaunchConfig()) && SessionCount(campaign) == 0,
            "Pending Campaign launch request must reject legacy S073 queue");

        var existing = NewLegacyQueueManager();
        var existingSession = existing.CreateEntity();
        existing.AddComponentData(existingSession, new SkirmishMatchState { Phase = SkirmishPhase.Queued });
        Reject(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(existing, S073LaunchConfig()) && SessionCount(existing) == 1,
            "Existing match session must reject a second legacy queue");

        var pendingStart = NewLegacyQueueManager();
        var startBoundary = pendingStart.CreateEntity();
        pendingStart.AddComponentData(startBoundary, new MatchStartQueueComponent { IsStartPending = 1, LastRequestId = 8 });
        Reject(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(pendingStart, S073LaunchConfig()) && SessionCount(pendingStart) == 0 &&
               pendingStart.GetComponentData<MatchStartQueueComponent>(startBoundary).LastRequestId == 8,
            "Pending match-start request must reject queue without mutating the boundary");

        var wrongScenario = NewLegacyQueueManager();
        var otherConfig = QuickGameConfig.Defaults;
        otherConfig.ScenarioIndex = 4;
        otherConfig.MapSeed = 104729;
        Reject(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(wrongScenario,
                   otherConfig) && SessionCount(wrongScenario) == 0,
            "S073 owner must reject non-S073 scenario identity");

        var enforcement = typeof(ContentAccessRuntime).GetField("enforceOwnership", BindingFlags.Static | BindingFlags.NonPublic);
        Reject(enforcement != null, "Content-access control owner changed shape");
        bool previous = (bool)enforcement.GetValue(null);
        try
        {
            enforcement.SetValue(null, true);
            var denied = NewLegacyQueueManager();
            Reject(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(denied, S073LaunchConfig()) && SessionCount(denied) == 0,
                "Denied original ContentAccessRuntime admission must not queue a legacy session");
        }
        finally { enforcement.SetValue(null, previous); }
    }

    static void Main()
    {
        PlacementSurfaceIdentity();
        SharedPlacementSourceBinding();
        Reject(GroundMissionContextFactory.TryCreateS073LegacyLogicalPublicData(Catalog(), Match(), out var legacy, out var error),
            "Actual S073 legacy identity rejected: " + error);
        Reject(legacy.LegacySourceBound && legacy.CatalogId == "S073" && legacy.DefinitionId == "" &&
               legacy.ContentVersion == 0 && legacy.SetupHash == 0 && legacy.SourcePresetLoadIndex == 3,
            "Legacy context fabricated expanded identity");

        var observation = new AriaS002Observation
        {
            Scope = AriaS002Scope.LogicalPublicData,
            SourceScenarioIndex = 3
        };
        legacy.RequireCompatibleObservation(observation);
        var plan = default(AriaS002PlanState);
        var contact = default(AriaS002ContactState);
        AriaGroundBaseAssaultPolicy.Step(legacy, observation, ref plan, ref contact, out _);

        // The shared pure column policy can issue a real legacy slot-selection
        // decision while both expanded-session claims remain false. The
        // adapter must still source this one-page roster from live legacy ECS.
        var legacyRosterView = new AriaS002Observation
        {
            Scope = AriaS002Scope.LogicalPublicData,
            SourceScenarioIndex = 3,
            Active = true,
            Frame = 1,
            Time = 1f,
            EnemyDesignatedAlive = true,
            ExpandedPageCount = 1,
            ExpandedTotalGroups = 1,
            ExpandedAssaultMask = 1,
            ExpandedStructureMask = 1,
            Squad0 = new AriaS002ActionTarget { Action = AriaS002Action.SelectSquad, Id = 90100, Slot = 0, Admitted = true }
        };
        var legacyPlan = default(AriaS002PlanState);
        var legacyContact = new AriaS002ContactState { Phase = AriaS002ContactPhase.Observing };
        AriaGroundBaseAssaultPolicy.Step(legacy, legacyRosterView, ref legacyPlan, ref legacyContact, out var legacyDecision);
        Reject(!legacyRosterView.ExpandedSession && !legacyRosterView.ExpandedBaseAssault &&
               legacyDecision.Kind == AriaS002DecisionKind.Control &&
               legacyDecision.Action == AriaS002Action.SelectSquad && legacyDecision.Slot == 0 &&
               legacyDecision.TargetId == 90100,
            "Shared ARIA policy must select an admitted source legacy slot without claiming an expanded session");

        Reject(!GroundMissionContextFactory.TryCreateS073LegacyLogicalPublicData(Catalog(map: "DB"), Match(), out _, out _),
            "Wrong source map admitted");
        Reject(!GroundMissionContextFactory.TryCreateS073LegacyLogicalPublicData(Catalog(index: 4), Match(), out _, out _),
            "Wrong catalog dispatch admitted");
        Reject(!GroundMissionContextFactory.TryCreateS073LegacyLogicalPublicData(Catalog(definition: "skirmish.s073", version: 1), Match(), out _, out _),
            "Invented expanded identity admitted");
        Reject(!GroundMissionContextFactory.TryCreateS073LegacyLogicalPublicData(Catalog(), Match(outcome: SkirmishOutcome.Victory), out _, out _),
            "Terminal match reused as active context");

        bool rejected = false;
        try { legacy.RequireCompatibleObservation(new AriaS002Observation { Scope = AriaS002Scope.LogicalPublicData, SourceScenarioIndex = 4 }); }
        catch (NotSupportedException) { rejected = true; }
        Reject(rejected, "Wrong observation source index admitted");
        rejected = false;
        try { legacy.RequireCompatibleObservation(new AriaS002Observation { Scope = AriaS002Scope.LogicalPublicData, SourceScenarioIndex = 3, ExpandedSession = true }); }
        catch (NotSupportedException) { rejected = true; }
        Reject(rejected, "Expanded observation crossed legacy boundary");

        var expanded = new GroundMissionContext("S002", "skirmish.s002", 3, 1, "expanded-session",
            4, SkirmishObjectiveKind.BaseAssault, SkirmishArmyProfileId.GroundManeuver,
            SkirmishStartPackageId.EstablishedBase, SkirmishReadinessStage.Established,
            AriaGroundApproachKind.Ordinary, "canonical:DB", "DB", 4, 0,
            SkirmishPresetConfig.ResourceName, AriaS002Scope.LogicalPublicData);
        var expandedObservation = new AriaS002Observation
        {
            Scope = AriaS002Scope.LogicalPublicData, SourceScenarioIndex = 4,
            ExpandedSession = true, ExpandedBaseAssault = true
        };
        expanded.RequireCompatibleObservation(expandedObservation);

        RequestProjection();
        LegacyArmySelection(legacy);
        LegacyOutcomePolicy();
        LegacyQueueOwner();

        Console.WriteLine("[S073LegacyContextControls] result=Passed positive=1 sourceBindingRejects=7 rejected=6 expandedRegression=Passed legacyRequests=1 legacyArmySelection=Passed legacyQueueOwner=Passed accessDenied=Passed campaignExistingPendingRejected=Passed snapshotSeed=Passed legacyOutcomePolicy=Passed queuedNoOutcome=Passed boundedFixture=1 legacyAriaPublicMarkers=Passed legacyAriaPolicySlotDecision=Passed placementSurfaceIds=Passed stackedLayersRejected=1 nonExactRejected=1");
    }
}
