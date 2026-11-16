using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Runtime;
using Game.Simulation;
using Game.Runtime.TransportPose;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static partial class SourceS073TacticalAIDisposableCopyFixture
{
    const string ProductionOwnerOptIn = "WARLINE_S073_PRODUCTION_OWNER_DIAGNOSTIC";
    const string SourceClearanceProjectionOptIn = "WARLINE_S073_SOURCE_CLEARANCE_PROJECTION_DIAGNOSTIC";
    const string SourceClearanceDifferentialOptIn = "WARLINE_S073_SOURCE_CLEARANCE_DIFFERENTIAL_CONTROL";
    const string ProductionSpawnpointBridgeOptIn = "WARLINE_S073_PRODUCTION_SPAWNPOINT_BRIDGE";
    const string ProductionRequestConsumerOptIn = "WARLINE_S073_PRODUCTION_REQUEST_CONSUMER_DIAGNOSTIC";
    const string CopyDeliveryAdvanceOptIn = "WARLINE_S073_COPY_DELIVERY_ADVANCE_DIAGNOSTIC";
    const string NativeSpawnpointEvidencePath = "Logs/SkirmishProduction/s073-native-spawnpoint-transform-20261101/r5-exit0/spawnpoint-transform.json";
    const string NativeSpawnpointEvidenceSha256 = "a4b9677a358fcbfc036c82c948b0369d387b117a2a7266dad9d53d0202309748";
    const string NativeUnitMetadataRelativePath = "Design/AgentReports/SkirmishProduction/S073/TransportMetadataReadback20261101/s073-unit-production-metadata.json";
    const string NativeUnitMetadataSha256 = "1372d72063491572eeaa11ffc78a8bbf790f10d1ef9f673fe5627af4447c3e8e";
    const string NativeRecipeReceiptRelativePath = "Design/AgentReports/SkirmishProduction/S073/NativeProductionRecipeObservation20261101/recipe-observation-receipt.json";
    const string NativeRecipeReceiptSha256 = "43e8e923810ae82d04dbdf0ca18a2e35d5b44122b4337b7b23fdaa53739fbffb";

    static string Sha256(byte[] bytes)
    {
        using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    static byte[] ReadPinnedFile(string path, string expectedSha, int maxBytes)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Require(bytes.Length > 0 && bytes.Length <= maxBytes && Sha256(bytes) == expectedSha,
            "Pinned S073 production input bytes changed: " + path);
        return bytes;
    }

    static float ReadF32Bits(JToken token, string label)
    {
        Require((string)token?["codec"] == "f32-bits-v1" && ((string)token["bits"])?.Length == 8,
            "Exact pinned f32 required: " + label);
        uint bits = uint.Parse((string)token["bits"], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        Require(!float.IsNaN(value) && !float.IsInfinity(value), "Finite pinned f32 required: " + label);
        return value;
    }

    static bool ReadBool(JToken token, string label)
    {
        Require((string)token?["codec"] == "bool-v1" && token["value"]?.Type == JTokenType.Boolean,
            "Exact pinned boolean required: " + label);
        return (bool)token["value"];
    }

    static int ReadInt(JToken token, string label)
    {
        Require((string)token?["codec"] == "integer-v1" && token["value"]?.Type == JTokenType.String,
            "Exact pinned integer required: " + label);
        if (!int.TryParse((string)token["value"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            throw new InvalidDataException("Exact pinned integer required: " + label);
        return value;
    }

    static bool SameSourcePrefabIdentity(SourceUnitPrefab left, SourceUnitPrefab right)
    {
        if (left == null || right == null || left.name != right.name) return false;
        if (left.SourceAssetPath != null || right.SourceAssetPath != null)
            return left.SourceAssetPath != null && right.SourceAssetPath != null &&
                left.SourceAssetPath == right.SourceAssetPath && left.SourceAssetGuid == right.SourceAssetGuid &&
                left.SourceAssetLocalId == right.SourceAssetLocalId;
        if (left.Template != Entity.Null && right.Template != Entity.Null) return left.Template == right.Template;
        return left.SourceAssetPath != null && right.SourceAssetPath != null && left.SourceAssetPath == right.SourceAssetPath &&
            left.SourceAssetGuid == right.SourceAssetGuid && left.SourceAssetLocalId == right.SourceAssetLocalId;
    }

    static Unity.Mathematics.float3 ReadVector3(JToken token, string label)
    {
        JObject fields = token? ["fields"] as JObject;
        Require((string)token?["codec"] == "struct-fields-v1" && (string)token["type"] == "UnityEngine.Vector3" && fields != null,
            "Exact pinned Unity Vector3 required: " + label);
        return new Unity.Mathematics.float3(ReadF32Bits(fields["UnityEngine.Vector3::x"], label + ".x"),
            ReadF32Bits(fields["UnityEngine.Vector3::y"], label + ".y"), ReadF32Bits(fields["UnityEngine.Vector3::z"], label + ".z"));
    }

    static Unity.Mathematics.quaternion ReadQuaternion(JToken token, string label)
    {
        JObject fields = token?["fields"] as JObject;
        Require((string)token?["codec"] == "struct-fields-v1" && (string)token["type"] == "UnityEngine.Quaternion" && fields != null,
            "Exact pinned Unity Quaternion required: " + label);
        var result = new Unity.Mathematics.quaternion(ReadF32Bits(fields["UnityEngine.Quaternion::x"], label + ".x"),
            ReadF32Bits(fields["UnityEngine.Quaternion::y"], label + ".y"), ReadF32Bits(fields["UnityEngine.Quaternion::z"], label + ".z"),
            ReadF32Bits(fields["UnityEngine.Quaternion::w"], label + ".w"));
        Require(Math.Abs(Unity.Mathematics.math.lengthsq(result.value) - 1f) <= 0.0001f, "Normalized pinned quaternion required: " + label);
        return result;
    }

    sealed class S073ProductionEvidence
    {
        internal readonly List<SourceUnitPrefab> Units = new List<SourceUnitPrefab>();
        internal readonly Dictionary<SourceUnitPrefab, BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata> Metadata =
            new Dictionary<SourceUnitPrefab, BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata>();
        internal readonly Dictionary<SourceUnitPrefab, SourceTransportTemplateData> TransportTemplates =
            new Dictionary<SourceUnitPrefab, SourceTransportTemplateData>();
        internal SourceUnitPrefab Helicopter;
        internal uint ConstructorRandomState;
        internal JObject Provenance;
    }

    static S073ProductionEvidence LoadS073ProductionEvidence(string repositoryRoot,
        S073NativeConfiguredUnits configuredUnits, Dictionary<int, RuntimeBuildingEntity> sourceBuildings, bool historicalInputDiagnostic)
    {
        string metadataPath = Path.GetFullPath(Path.Combine(repositoryRoot, NativeUnitMetadataRelativePath));
        string receiptPath = Path.GetFullPath(Path.Combine(repositoryRoot, NativeRecipeReceiptRelativePath));
        JObject metadata = JObject.Parse(System.Text.Encoding.UTF8.GetString(ReadPinnedFile(metadataPath, NativeUnitMetadataSha256, 8 * 1024 * 1024)));
        JObject receipt = JObject.Parse(System.Text.Encoding.UTF8.GetString(ReadPinnedFile(receiptPath, NativeRecipeReceiptSha256, 1024 * 1024)));
        Require((string)metadata["kind"] == "S073AuthoredUnitProductionMetadata" && (int?)metadata["schema_version"] == 1 &&
            (string)metadata["authority"] == "OriginalConfiguredAuthoringFactories_NotMissionState" &&
            (string)metadata["mission"] == "S073" && (int?)metadata["seed_context"] == 104729 &&
            (string)metadata["scope"] == "LegacyInitialForcesAndUnitRegistry_AuthoringOnly_NotRuntimeActivation" &&
            (bool?)metadata["full_mission_initialized"] == false && (bool?)metadata["native_visual_readiness"] == false &&
            (bool?)metadata["baked_actor_footprint_substituted"] == false && (metadata["unsupported_values"] as JArray)?.Count == 0,
            "Qualified source-only S073 authored unit metadata envelope required");
        Require((string)metadata["legacy_active_unit_registry"]?["path"] == "Assets/Game/Configs/Skirmish/UnitRegistry.asset" &&
            (string)metadata["building_placement_config"]?["path"] == "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset" &&
            (string)metadata["registry_selection"] == "Legacy S073 uses SkirmishPreset.buildingPlacement through MatchSceneView.BuildingPlacementConfig; scene serialized default is retained separately and is not treated as the active S073 registry." &&
            ((JArray)metadata["units"]).Count == 3 && configuredUnits != null && configuredUnits.Count == 3,
            "Exact legacy S073 registry selection and three-unit domain required");

        string[] requiredPins = {
            "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
            "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
            "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset",
            "Assets/Game/Configs/Skirmish/UnitRegistry.asset",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab"
        };
        JObject pins = metadata["source_pins"] as JObject ?? throw new InvalidDataException("S073 production metadata source pins missing");
        var historicalSourceDifferences = new JArray();
        foreach (string relative in requiredPins)
        {
            string pin = (string)pins[relative];
            Require(pin != null, "Required pinned S073 production source absent: " + relative);
            SourceS073LegacyProductionRuntime.RequireProductionSource(repositoryRoot, relative, pin, historicalInputDiagnostic,
                historicalSourceDifferences, "S073 production authoring source changed: ");
        }
        string placementConfig = File.ReadAllText(Path.Combine(repositoryRoot, "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"));
        Require(placementConfig.Split('\n').Count(line => line.Trim() == "maxQueuedUnitProductions: 4") == 1,
            "Pinned S073 legacy production queue limit changed");

        JObject observation = receipt["observation"] as JObject ?? throw new InvalidDataException("Native S073 production startup observation missing");
        Require((string)receipt["kind"] == "S073ProductionRecipeStartupObservationReceipt" &&
            (int?)receipt["schema_version"] == 1 &&
            (string)observation["mission"] == "S073" && (int?)observation["seed"] == 104729 &&
            (bool?)observation["legacy_session"] == true && (string)observation["building_placement_config"] == "Construction" &&
            (bool?)observation["recipe_input_is_null"] == true && (int?)observation["recipe_input_count"] == 0 &&
            (observation["recipe_input_rows"] as JArray)?.Count == 0 && (int?)observation["resulting_ground_exit_count"] == 0 &&
            (observation["resulting_ground_exit_prefabs"] as JArray)?.Count == 0 &&
            (uint?)observation["building_spawn_random_state"] == 0x12345678u &&
            (string)observation["random_state_owner"] == "BuildingGameplaySourceCompositionSystemHelper.BuildingSpawnRandomState",
            "Pinned native S073 null-recipe/empty-ground-exit/startup-RNG observation required");
        string wrapperStatusPath = Path.GetFullPath(Path.Combine(repositoryRoot,
            "Design/AgentReports/SkirmishProduction/S073/NativeProductionRecipeObservation20261101/native-wrapper-status.json"));
        Require((string)receipt["native_wrapper_status_path"] == wrapperStatusPath,
            "Native S073 production recipe receipt must identify its exact durable wrapper-status artifact");
        JObject wrapperStatus = JObject.Parse(System.Text.Encoding.UTF8.GetString(ReadPinnedFile(wrapperStatusPath,
            (string)receipt["native_wrapper_status_sha256"], 1024 * 1024)));
        Require((string)wrapperStatus["kind"] == "S073NativeRecipeProbeWrapperStatus" &&
            (int?)wrapperStatus["schema_version"] == 1 && (int?)wrapperStatus["retry_wrapper_exit_code"] == 0 &&
            (bool?)wrapperStatus["started_marker_observed"] == true &&
            (bool?)wrapperStatus["passed_marker_observed"] == true &&
            (string)wrapperStatus["native_loading_observer_result"] == "Observed" &&
            (int?)wrapperStatus["production_recipe_observation_count"] == 1,
            "Receipt-linked native S073 recipe wrapper must record its successful observed retry");
        JObject recipePins = receipt["observed_source_pins_sha256"] as JObject ?? throw new InvalidDataException("Native S073 recipe source pins absent");
        foreach (string relative in new[] { "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs", "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs" })
            SourceS073LegacyProductionRuntime.RequireProductionSource(repositoryRoot, relative, (string)recipePins[relative],
                historicalInputDiagnostic, historicalSourceDifferences, "Native S073 recipe-observation source changed: ");
        string configPin = (string)pins["Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"];
        Require(configPin == Sha256(File.ReadAllBytes(Path.Combine(repositoryRoot, "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"))),
            "S073 native construction config is not the captured source version");

        var evidence = new S073ProductionEvidence { ConstructorRandomState = (uint)observation["building_spawn_random_state"] };
        var configuredByKey = new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal);
        var definitionUnits = new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal);
        foreach (var pair in sourceBuildings)
        {
            var slots = pair.Value?.Definition?.ProductionSlots;
            if (slots == null) continue;
            foreach (var slot in slots)
            {
                if (slot?.SpawnUnitPrefab == null) continue;
                string key = slot.SpawnUnitPrefab.name;
                if (definitionUnits.TryGetValue(key, out var prior))
                {
                    Require(SameSourcePrefabIdentity(prior, slot.SpawnUnitPrefab),
                        "Conflicting source prefab identities in the same-run producer registry: " + key);
                }
                else definitionUnits.Add(key, slot.SpawnUnitPrefab);
            }
        }
        JArray unitRows = metadata["units"] as JArray;
        for (int i = 0; i < unitRows.Count; i++)
        {
            JObject row = (JObject)unitRows[i];
            string key = (string)row["key"];
            Require((int?)row["index"] == i && (bool?)row["has_production_metadata"] == true &&
                row["production_metadata"] != null && row["production_metadata"]["fields"] is JObject,
                "Contiguous actual S073 production metadata row required");
            SourceUnitPrefab definitionUnit = null;
            bool hasDefinitionUnit = definitionUnits.TryGetValue(key, out definitionUnit);
            Require(configuredUnits.TryRead(i, out SourceUnitPrefab configured, out _, out _, out _, out _) && configured != null &&
                configured.name == key && hasDefinitionUnit &&
                SameSourcePrefabIdentity(configured, definitionUnit),
                "Same-run configured registry and producer definitions must contain S073 unit " + key);
            JObject asset = row["prefab"] as JObject ?? throw new InvalidDataException("S073 unit asset identity missing: " + key);
            JObject registryRow = (JObject)((JArray)metadata["registered_unit_prefabs"])[i];
            Require((int?)registryRow["index"] == i && (string)registryRow["prefab"]?["key"] == key &&
                JToken.DeepEquals(registryRow["prefab"], asset) && (bool?)registryRow["also_in_legacy_initial_forces"] == true &&
                (string)asset["key"] == key,
                "Authored S073 metadata row differs from the pinned legacy registry order: " + key);
            configuredByKey.Add(key, definitionUnit);
            evidence.Units.Add(definitionUnit);
        }
        Require(evidence.Units.Count == 3 && evidence.Units.Select(unit => unit.name).Distinct(StringComparer.Ordinal).Count() == 3,
            "Unique source-compatible S073 configured units required");

        JObject transportRow = ((JArray)metadata["transports"]).SingleOrDefault() as JObject ??
            throw new InvalidDataException("Exactly one authored S073 helicopter transport graph required");
        JObject transportAsset = transportRow["prefab"] as JObject ?? throw new InvalidDataException("S073 transport asset identity missing");
        Require((string)transportAsset["key"] == "Unit_Veh_Helicopter_Transport" &&
            (string)transportAsset["path"] == "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab" &&
            (string)transportAsset["guid"] == "e540888d7cd8a489f9472b3456bf2406" &&
            (long?)transportAsset["local_id"] == 5616646512338707422L,
            "Exact S073 helicopter transport source identity required");
        evidence.Helicopter = SourceUnitPrefab.FromAssetIdentity((string)transportAsset["key"],
            (string)transportAsset["path"], (string)transportAsset["guid"], (long)transportAsset["local_id"]);
        var frames = new List<PoseFrame>();
        var meshes = new List<MeshBounds>();
        var rendererEnabled = new List<bool>();
        int doorNode = -1;
        JArray nodes = transportRow["nodes"] as JArray ?? throw new InvalidDataException("S073 transport nodes missing");
        Require(nodes.Count > 0 && nodes.Count <= 128, "Bounded actual S073 transport hierarchy required");
        Game.Runtime.TransportPose.PoseMath.AdmitInstalledMono();
        for (int index = 0; index < nodes.Count; index++)
        {
            JObject node = (JObject)nodes[index];
            Require((int?)node["index"] == index && (bool?)node["active_self"] == true,
                "Complete source-ordered active S073 transport hierarchy required");
            string name = (string)node["name"];
            frames.Add(new PoseFrame { Parent = (int)node["parent"], Name = name,
                Position = ReadVector3(node["local_position"], "transport[" + index + "].position"),
                Rotation = ReadQuaternion(node["local_rotation"], "transport[" + index + "].rotation"),
                Scale = ReadVector3(node["local_scale"], "transport[" + index + "].scale"), InitialEulerPresent = true,
                InitialEulerDegrees = ReadVector3(node["initial_local_euler_degrees"], "transport[" + index + "].euler") });
            if (doorNode < 0 && name == "Door_X") doorNode = index;
            foreach (JObject mesh in (JArray)node["meshes"])
            {
                Require((string)mesh["kind"] == "MeshFilter", "Unsupported S073 static transport mesh type");
                JObject bounds = mesh["local_bounds"]?["fields"] as JObject ?? throw new InvalidDataException("S073 transport mesh bounds missing");
                meshes.Add(new MeshBounds { Node = index, Present = (bool)mesh["renderer_present"],
                    Center = ReadVector3(bounds["UnityEngine.Bounds::m_Center"], "transport.mesh.center"),
                    Extents = ReadVector3(bounds["UnityEngine.Bounds::m_Extents"], "transport.mesh.extents") });
                rendererEnabled.Add((bool)mesh["renderer_enabled"]);
            }
            foreach (JObject renderer in (JArray)node["renderers"])
                Require((string)renderer["type"] == "UnityEngine.MeshRenderer", "Unsupported S073 transport renderer type");
        }
        PoseFrame[] frameCopy = frames.ToArray(); MeshBounds[] meshCopy = meshes.ToArray(); bool[] enabledCopy = rendererEnabled.ToArray();
        Func<PoseGraph> createPose = () => new PoseGraph((PoseFrame[])frameCopy.Clone(), (MeshBounds[])meshCopy.Clone(),
            ((JArray)transportRow["nodes"]).OfType<JObject>().Select((node, index) => new { node, index })
                .Where(item => ((string)item.node["name"]).EndsWith("_X", StringComparison.Ordinal) ||
                    ((string)item.node["name"]).EndsWith("_Y", StringComparison.Ordinal) ||
                    ((string)item.node["name"]).EndsWith("_Z", StringComparison.Ordinal))
                .Select(item => item.index).ToArray(), true, true, true, true);
        createPose();
        evidence.TransportTemplates.Add(evidence.Helicopter,
            new SourceTransportTemplateData(evidence.Helicopter, createPose, meshCopy, enabledCopy, true, doorNode));

        foreach (JObject row in unitRows)
        {
            string key = (string)row["key"];
            SourceUnitPrefab prefab = configuredByKey[key];
            JObject fields = row["production_metadata"]["fields"] as JObject;
            string refCodec = (string)fields["ProductionTransportPrefab"]?["codec"];
            SourceUnitPrefab transport = null;
            if (refCodec == "source-asset-reference-v1")
            {
                JObject reference = (JObject)fields["ProductionTransportPrefab"];
                Require(JToken.DeepEquals(reference, transportAsset), "S073 unit transport reference and graph identity differ: " + key);
                transport = evidence.Helicopter;
            }
            else Require(refCodec == "null-v1", "Unexpected S073 production transport identity codec: " + key);
            JObject footprint = fields["FootprintCells"]?["fields"] as JObject ?? throw new InvalidDataException("S073 unit footprint missing: " + key);
            var unitMeta = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                ReadF32Bits(fields["ProductionDurationSeconds"], key + ".duration"), transport,
                ReadBool(fields["IsAirUnit"], key + ".air"),
                ReadF32Bits(fields["ProductionTransportArrivalSeconds"], key + ".arrival"),
                ReadF32Bits(fields["ProductionTransportHoldForNextReadySeconds"], key + ".hold"),
                ReadInt(fields["ProductionTransportMaxConcurrent"], key + ".concurrent"),
                ReadBool(fields["ProductionTransportRequiresAirportRunway"], key + ".runway"),
                ReadBool(fields["ProductionTransportUsesRunwayLanding"], key + ".runway_landing"),
                new Vector2Int(ReadInt(footprint["UnityEngine.Vector2Int::m_X"], key + ".footprint.x"),
                    ReadInt(footprint["UnityEngine.Vector2Int::m_Y"], key + ".footprint.y")));
            evidence.Metadata.Add(prefab, unitMeta);
        }
        evidence.Provenance = new JObject
        {
            ["historical_production_input_diagnostic"] = historicalInputDiagnostic,
            ["historical_source_differences"] = historicalSourceDifferences,
            ["current_production_metadata_equivalence_proven"] = false,
            ["unit_metadata_path"] = metadataPath, ["unit_metadata_sha256"] = NativeUnitMetadataSha256,
            ["unit_metadata_scope"] = (string)metadata["scope"], ["legacy_registry_asset"] = (string)metadata["legacy_active_unit_registry"]["path"],
            ["configured_unit_count"] = evidence.Units.Count, ["transport_graph_key"] = evidence.Helicopter.name,
            ["transport_hierarchy_node_count"] = nodes.Count, ["recipe_receipt_path"] = receiptPath,
            ["recipe_receipt_sha256"] = NativeRecipeReceiptSha256, ["native_recipe_input_was_null"] = true,
            ["native_recipe_count"] = 0, ["native_ground_exit_count"] = 0,
            ["native_startup_random_state"] = evidence.ConstructorRandomState,
            ["native_random_state_session_id"] = (string)observation["session_id"],
            ["native_random_state_is_this_copy_session"] = false,
            ["no_catalog_overlay_or_ground_exit_injected"] = true
        };
        return evidence;
    }

    static RuntimeDefinition CloneDefinition(RuntimeDefinition source)
    {
        if (source == null) return null;
        var slots = source.ProductionSlots == null ? null : source.ProductionSlots.Select(slot => slot == null ? null :
            new RuntimeDefinition.ProductionSlotDefinition { SpawnUnitPrefab = slot.SpawnUnitPrefab,
                SpawnUnitSourceKey = slot.SpawnUnitSourceKey, Quantity = slot.Quantity }).ToList();
        return new RuntimeDefinition { SourceMetadataComplete = source.SourceMetadataComplete, DisplayName = source.DisplayName,
            Description = source.Description, MaxHealth = source.MaxHealth, CreditsCost = source.CreditsCost,
            MaterialsCost = source.MaterialsCost, FootprintCells = source.FootprintCells, ProductionSlots = slots,
            Prefab = source.Prefab, SpawnUnitPrefab = source.SpawnUnitPrefab, SecondarySpawnUnitPrefab = source.SecondarySpawnUnitPrefab,
            TertiarySpawnUnitPrefab = source.TertiarySpawnUnitPrefab, QuaternarySpawnUnitPrefab = source.QuaternarySpawnUnitPrefab,
            ProductionDurationSeconds = source.ProductionDurationSeconds, Role = source.Role, IsWall = source.IsWall,
            HasRunway = source.HasRunway, ProductionSpawnLocalPositions = source.ProductionSpawnLocalPositions == null ? null :
                (Vector3[])source.ProductionSpawnLocalPositions.Clone(), RunwayLocalPosition = source.RunwayLocalPosition,
            RunwayHalfExtents = source.RunwayHalfExtents };
    }

    static Dictionary<int, RuntimeBuildingEntity> CloneSameRunBuildingRegistry(EntityManager source, EntityManager copy,
        Dictionary<Entity, Entity> remap, Dictionary<int, RuntimeBuildingEntity> sourceBuildings,
        S073ProductionEvidence evidence, Entity boundary, out Dictionary<int, SourceProducerRootFrame> rootFrames,
        out JObject sourceQueueGuardBefore)
    {
        Require(sourceBuildings != null && sourceBuildings.Count > 0, "Same-run original S073 runtime building registry required");
        var result = new Dictionary<int, RuntimeBuildingEntity>(); rootFrames = new Dictionary<int, SourceProducerRootFrame>();
        var sourceRows = new JArray();
        foreach (var pair in sourceBuildings.OrderBy(pair => pair.Key))
        {
            RuntimeBuildingEntity original = pair.Value;
            Entity combat = Entity.Null;
            bool hasCombatRemap = original != null && original.CombatEntity != Entity.Null && remap.TryGetValue(original.CombatEntity, out combat);
            Require(original != null && original.Id == pair.Key && original.Definition != null && original.Definition.SourceMetadataComplete &&
                original.InitialProducerCollectionsKnown && original.CombatEntity != Entity.Null && hasCombatRemap &&
                copy.Exists(combat) && copy.HasComponent<Unity.Transforms.LocalTransform>(combat),
                "Same-run producer registry entry and exact copy combat transform required: " + pair.Key);
            Require((original.PendingProductions?.Count ?? 0) == 0 && (original.ProducedUnits?.Count ?? 0) == 0 &&
                (original.ProducedUnitPrefabs?.Count ?? 0) == 0 && (original.ProducedUnitSourceKeys?.Count ?? 0) == 0 &&
                (original.ProducedUnitSlots == null || original.ProducedUnitSlots.All(entity => entity == Entity.Null)),
                "Source production queue/slots must be unadvanced before copying and applying observed startup RNG: " + pair.Key);
            RuntimeBuildingEntity clone = new RuntimeBuildingEntity
            {
                Id = original.Id, Definition = CloneDefinition(original.Definition), Instance = null,
                OriginCell = original.OriginCell, CombatEntity = combat,
                IsDestroyed = original.IsDestroyed, IsCityGenerated = original.IsCityGenerated,
                HasOwnerFaction = original.HasOwnerFaction, OwnerFactionId = original.OwnerFactionId,
                InitialProducerCollectionsKnown = original.InitialProducerCollectionsKnown,
                ProductionSpawnLocalPositions = original.ProductionSpawnLocalPositions == null ? null :
                    (Vector3[])original.ProductionSpawnLocalPositions.Clone(),
                ProducedUnitSlots = original.ProducedUnitSlots == null ? null :
                    (Entity[])original.ProducedUnitSlots.Clone(),
                ProducedUnits = new List<Entity>(), PendingProductions = new List<SourcePendingProduction>(),
                Focus = original.Focus, FocusProven = original.FocusProven
            };
            if (original.OilMirrorKnown) clone.StoredOilBarrels = original.StoredOilBarrels;
            if (original.FuelMirrorKnown) clone.StoredFuelBarrels = original.StoredFuelBarrels;
            if (original.ProducedUnitPrefabs != null)
                clone.ProducedUnitPrefabs = new Dictionary<Entity, SourceUnitPrefab>(original.ProducedUnitPrefabs);
            if (original.ProducedUnitSourceKeys != null)
                clone.ProducedUnitSourceKeys = new Dictionary<Entity, Unity.Collections.FixedString64Bytes>(original.ProducedUnitSourceKeys);
            var transform = copy.GetComponentData<Unity.Transforms.LocalTransform>(combat);
            rootFrames.Add(pair.Key, new SourceProducerRootFrame(transform.Position, transform.Rotation, transform.Scale));
            result.Add(pair.Key, clone);
            sourceRows.Add(new JObject { ["building_id"] = pair.Key, ["combat_entity"] = original.CombatEntity.ToString(),
                ["definition_key"] = original.Definition.Prefab?.name, ["faction_id"] = original.HasOwnerFaction ? (int?)original.OwnerFactionId : null,
                ["spawnpoint_count"] = original.ProductionSpawnLocalPositions?.Length ?? 0,
                ["pending_count"] = original.PendingProductions?.Count ?? 0, ["produced_unit_count"] = original.ProducedUnits?.Count ?? 0 });
        }
        sourceQueueGuardBefore = new JObject { ["buildings"] = sourceRows, ["all_queues_empty"] = true,
            ["all_production_slots_empty"] = true, ["same_run_registry_count"] = sourceBuildings.Count };
        return result;
    }

    static JObject BindCopyProductionTemplates(EntityManager copy, S073ProductionEvidence evidence,
        Dictionary<int, RuntimeBuildingEntity> copyBuildings, string path, string sha256)
    {
        byte[] bytes = ReadPinnedFile(path, sha256, 128 * 1024 * 1024);
        JObject capture = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        Require((string)capture["kind"] == "AuthoredSkirmishPrefabReferenceData" &&
            (string)capture["authority"] == "ActualSourceAssetsAndNativeBakers_NotMissionState",
            "Actual source production prefab capture required for copy templates");
        string[] keys = evidence.Units.Select(unit => unit.name).ToArray();
        Require(keys.Length == 3 && keys.Distinct(StringComparer.Ordinal).Count() == 3 &&
            keys.All(key => capture["opening_prefabs"]?[key] != null), "Exact captured S073 configured roots required");
        var imported = GeneratedReferenceTemplateImporter.Import(copy, capture, Path.GetDirectoryName(path), keys);
        Require(ReferenceEquals(imported.Manager, copy) && !GeneratedReferenceTemplateImporter.BirthRecordIsInPresentationOmissionSet &&
            keys.All(key => imported.Prefabs.ContainsKey(key)), "All configured copy templates and observable birth records required");
        const EntityQueryOptions allOptions = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities |
            EntityQueryOptions.IncludeSystems | EntityQueryOptions.IncludeMetaChunks | EntityQueryOptions.IgnoreComponentEnabledState;
        using var censusQuery = copy.CreateEntityQuery(new EntityQueryDesc { All = Array.Empty<ComponentType>(), Options = allOptions });
        using var censusRows = censusQuery.ToEntityArray(Allocator.Temp);
        Entity[] census = censusRows.ToArray();
        var censusSet = new HashSet<Entity>(census);
        Entity[] independent = copy.CaptureSourceHostLiveEntityInventory();
        Require(census.Length > 0 && census.Length <= 65536 && censusSet.Count == census.Length &&
            independent.Length == census.Length && new HashSet<Entity>(independent).SetEquals(censusSet),
            "Complete independent disposable-copy entity inventory required");
        var importedEntities = new HashSet<Entity>(imported.SourceToLogical.Values);
        Require(importedEntities.Count == imported.ImportedEntities && importedEntities.Count == imported.SourceToLogical.Count &&
            importedEntities.All(entity => entity != Entity.Null && censusSet.Contains(entity) && copy.Exists(entity) && copy.HasComponent<Prefab>(entity)) &&
            imported.Prefabs.Values.All(entity => importedEntities.Contains(entity)), "Complete imported copy prefab closure required");
        const string birthRecordName = "Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord";
        Func<Entity, bool> liveHasBirthRecord = entity => !copy.Exists(entity) || copy.HasComponentNamed(entity, birthRecordName);
        Require(census.All(entity => !liveHasBirthRecord(entity)), "Active native birth record in disposable copy is unsupported");
        RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(copy, true, true, census, liveHasBirthRecord);
        var replacements = new Dictionary<SourceUnitPrefab, SourceUnitPrefab>();
        for (int i = 0; i < evidence.Units.Count; i++)
        {
            SourceUnitPrefab old = evidence.Units[i];
            Require(imported.Prefabs.TryGetValue(old.name, out var entity) && copy.Exists(entity) && copy.HasComponent<Prefab>(entity),
                "Imported copy prefab entity required: " + old.name);
            var replacement = new SourceUnitPrefab(old.name, entity, old.SourceAssetPath, old.SourceAssetGuid, old.SourceAssetLocalId);
            Require(evidence.Metadata.TryGetValue(old, out var metadata), "Exact source production metadata binding required");
            evidence.Metadata.Remove(old); evidence.Metadata.Add(replacement, metadata);
            replacements.Add(old, replacement); evidence.Units[i] = replacement;
        }
        SourceUnitPrefab Rebind(SourceUnitPrefab prefab) => prefab != null && replacements.TryGetValue(prefab, out var replacement) ? replacement : prefab;
        foreach (var building in copyBuildings.Values)
        {
            RuntimeDefinition definition = building.Definition;
            if (definition.ProductionSlots != null)
                foreach (var slot in definition.ProductionSlots) if (slot != null) slot.SpawnUnitPrefab = Rebind(slot.SpawnUnitPrefab);
            definition.SpawnUnitPrefab = Rebind(definition.SpawnUnitPrefab);
            definition.SecondarySpawnUnitPrefab = Rebind(definition.SecondarySpawnUnitPrefab);
            definition.TertiarySpawnUnitPrefab = Rebind(definition.TertiarySpawnUnitPrefab);
            definition.QuaternarySpawnUnitPrefab = Rebind(definition.QuaternarySpawnUnitPrefab);
        }
        return new JObject { ["status"] = "ImportedIntoDisposableCopy", ["capture_path"] = path,
            ["capture_sha256"] = sha256, ["configured_root_count"] = keys.Length, ["imported_root_count"] = imported.Prefabs.Count,
            ["imported_entity_count"] = imported.ImportedEntities, ["copy_live_entity_count"] = census.Length,
            ["copy_prefab_entity_count"] = census.Count(entity => copy.HasComponent<Prefab>(entity)),
            ["copy_no_native_birth_record_profile"] = "IndependentFullInventoryAndLiveRecordCheck",
            ["native_world_completeness_claimed"] = false,
            ["source_world_mutated"] = false, ["native_runtime_parity"] = false, ["mission_ready"] = false };
    }

    static JObject ProductionRequestConsumerPreflight(EntityManager source, EntityManager copy, Entity boundary,
        Dictionary<Entity, Entity> remap, SameRunBirthInputs sameRunBirths, Dictionary<int, RuntimeBuildingEntity> sourceBuildings,
        S073NativeConfiguredUnits configuredUnits, string repositoryRoot, bool historicalInputDiagnostic,
        SourceS073HostAbsentPresentationCameraCapability sourceCameraCapability, string productionPrefabPath, string productionPrefabSha256)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(ProductionRequestConsumerOptIn), "1", StringComparison.Ordinal))
            return new JObject { ["status"] = "NotRequested", ["consumer_invoked"] = false };

        var requestsBefore = ProductionRequestRows(copy, boundary);
        var beforePending = requestsBefore.OfType<JObject>().Where(row => (int?)row["status"] == (int)BuildingFactionUnitProductionRequest.Pending).ToArray();
        using var matchQuery = copy.CreateEntityQuery(typeof(SkirmishMatchState));
        using var expandedQuery = copy.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
        using var gridQuery = copy.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        Require(matchQuery.CalculateEntityCount() == 1 && expandedQuery.IsEmptyIgnoreFilter && gridQuery.CalculateEntityCount() == 1,
            "One original S073 legacy match, no expanded session, and a complete source-copy grid are required");
        JProperty requestOwnerProperty = beforePending.Length == 1 ? beforePending[0].Property("request_owner") : null;
        JToken requestOwnerToken = requestOwnerProperty?.Value;
        bool requestOwnerIsNull = requestOwnerProperty != null &&
            (requestOwnerToken == null || requestOwnerToken is JValue ownerValue && ownerValue.Value == null);
        bool expectedPending = beforePending.Length == 1 && (int?)beforePending[0]["faction_id"] == 2 &&
            (string)beforePending[0]["unit_id"] == "unit_chr_soldier_male_02_alt_04" && requestOwnerIsNull;
        if (!expectedPending)
            return new JObject
            {
                ["status"] = "BlockedPreflight",
                ["consumer_invoked"] = false,
                ["failure"] = "Exactly one actual original AIProductionSystem pending Rifleman request from the legacy owner required",
                ["observed_requests"] = requestsBefore,
                ["observed_pending_count"] = beforePending.Length,
                ["observed_faction_id"] = beforePending.Length == 1 ? beforePending[0]["faction_id"]?.DeepClone() : null,
                ["observed_unit_id"] = beforePending.Length == 1 ? beforePending[0]["unit_id"]?.DeepClone() : null,
                ["observed_request_owner_property_present"] = requestOwnerProperty != null,
                ["observed_request_owner_token_type"] = requestOwnerToken?.Type.ToString() ?? "NullReference",
                ["observed_request_owner_value_is_null"] = requestOwnerToken == null || requestOwnerToken is JValue observedOwner && observedOwner.Value == null,
                ["expected_pending_faction_id"] = 2,
                ["expected_pending_unit_id"] = "unit_chr_soldier_male_02_alt_04",
                ["expected_request_owner"] = null,
                ["source_world_mutated"] = false,
                ["production_completed"] = false,
                ["mission_ready"] = false
            };

        var native = LoadS073ProductionEvidence(repositoryRoot, configuredUnits, sourceBuildings, historicalInputDiagnostic);
        Entity grid = gridQuery.GetSingletonEntity();
        var sourceRegistryBefore = RegistryQueueState(sourceBuildings);
        var copyBuildings = CloneSameRunBuildingRegistry(source, copy, remap, sourceBuildings, native, boundary,
            out var roots, out var sourceQueueGuard);
        Require(JToken.DeepEquals(sourceRegistryBefore, RegistryQueueState(sourceBuildings)),
            "Copy registry construction changed source runtime producer state");
        bool advanceDelivery = string.Equals(Environment.GetEnvironmentVariable(CopyDeliveryAdvanceOptIn), "1", StringComparison.Ordinal);
        JObject copyTemplateEvidence = advanceDelivery
            ? BindCopyProductionTemplates(copy, native, copyBuildings, productionPrefabPath, productionPrefabSha256)
            : new JObject { ["status"] = "NotRequested" };

        var configured = new List<SourceUnitPrefab>(native.Units);
        var byKey = new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal);
        for (int i = 0; i < configured.Count; i++)
        {
            string normalized = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(configured[i].name);
            Require(!string.IsNullOrEmpty(normalized) && !byKey.ContainsKey(normalized), "Unique normalized S073 configured-unit key required");
            byKey.Add(normalized, configured[i]);
            Require(configuredUnits.TryRead(i, out var actual, out _, out _, out _, out _) && actual.name == configured[i].name,
                "Same-run native configured unit order changed");
        }
        Require(native.Metadata.Count == 3 && native.Metadata.Keys.All(prefab => configured.Any(row => ReferenceEquals(row, prefab))),
            "Exact source-backed metadata for every S073 configured prefab required");

        var production = new BuildingProductionQueueCompositionSystemHelper();
        production.ConfigureProductionRecipes(null);
        production.ConfigureUnitProductionMetadataResolver((SourceUnitPrefab prefab, out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata metadata) =>
            native.Metadata.TryGetValue(prefab, out metadata));
        var slotSystem = new BuildingProductionSlotUtilitySystemHelper();
        BuildingProductionQueueCompositionSystemHelper.TryGetRuntimeBoundaryEntityDelegate tryBoundary =
            (EntityManager manager, out Entity entity) => { entity = boundary; return ReferenceEquals(manager, copy) && manager.Exists(entity); };
        var queueContext = new BuildingProductionQueueCompositionSystemHelper.QueueContext(configured, byKey,
            slotSystem, null, null, tryBoundary);
        var requestRecipes = new Dictionary<int, SourceProductionDeliveryRecipe[]>();
        foreach (var pair in copyBuildings)
        {
            var recipes = new List<SourceProductionDeliveryRecipe>();
            var slots = pair.Value.Definition.ProductionSlots;
            if (slots != null)
                for (int index = 0; index < slots.Count; index++)
                {
                    var slot = slots[index];
                    if (slot == null || slot.SpawnUnitPrefab == null) continue;
                    if (!native.Metadata.TryGetValue(slot.SpawnUnitPrefab, out var metadata)) continue;
                    recipes.Add(new SourceProductionDeliveryRecipe(index, slot.SpawnUnitPrefab, Math.Max(1, slot.Quantity), metadata));
                }
            requestRecipes.Add(pair.Key, recipes.ToArray());
        }
        SourceProductionGroundWorld groundWorld = new SourceProductionGroundWorld(copy, grid, boundary, copyBuildings, roots);
        var deliveryOwner = new SourceProductionDeliveryOwner(groundWorld, production, queueContext,
            requestRecipes, 4, native.ConstructorRandomState);
        bool sourceClearanceProjectionRequested =
            string.Equals(Environment.GetEnvironmentVariable(SourceClearanceProjectionOptIn), "1", StringComparison.Ordinal);
        var sourceClearanceProjection = sourceClearanceProjectionRequested
            ? S073SourceClearanceProjectionAdapter.Load(repositoryRoot, groundWorld.Grid)
            : null;
        bool sourceClearanceDifferentialRequested =
            string.Equals(Environment.GetEnvironmentVariable(SourceClearanceDifferentialOptIn), "1", StringComparison.Ordinal);
        if (sourceClearanceDifferentialRequested && sourceClearanceProjection == null)
            throw new InvalidOperationException("S073 clearance differential requires the separately opted-in source projection adapter");
        JObject sourceClearanceDifferential = sourceClearanceDifferentialRequested
            ? sourceClearanceProjection.CompareAgainstS002(groundWorld, repositoryRoot)
            : new JObject { ["status"] = "NotRequested", ["compiled_differential_executed"] = false,
                ["delivery_invoked"] = false, ["mission_ready"] = false };
        SourceHelicopterDelivery.ReserveStaticSourceBounds noDeliveryBounds;
        if (sourceClearanceProjection != null)
            noDeliveryBounds = sourceClearanceProjection.ReserveCallback;
        else
            noDeliveryBounds = (SourceProductionGroundWorld _, Unity.Mathematics.int2 _, ref NativeBitArray _) =>
                throw new NotSupportedException("This bounded S073 diagnostic stops at the real queue append and does not advance or deliver");
        if (advanceDelivery && (!historicalInputDiagnostic || sourceClearanceProjection == null ||
            sourceCameraCapability == null || !sourceCameraCapability.IsValidForSourceHost))
            throw new NotSupportedException("Copy delivery advance requires explicit historical input, captured clearance, and source-host absent-camera authority");
        JObject capturedClearanceParity = advanceDelivery
            ? SourceS073CapturedClearanceAdmission.RequireSevenCaseCapturedProjectionParity(repositoryRoot) : null;
        deliveryOwner.Helicopters = new SourceHelicopterDelivery(deliveryOwner, native.TransportTemplates,
            true, false, Unity.Mathematics.float3.zero, Unity.Mathematics.float3.zero, noDeliveryBounds,
            useLiveS073ProducerDropPose: advanceDelivery);
        production.BindSourceFactionQueueOwner(deliveryOwner);

        var requestContext = new SourceCampAvailabilityKernel.Context
        {
            RuntimeBuildings = copyBuildings,
            UnitSpawnPrefabs = configured,
            UnitSpawnPrefabsByKey = byKey,
            MaxQueuedUnitProductions = 4,
            ProductionSystem = production,
            ProductionQueueContext = queueContext,
            GetProductionPrefab = SourceCampDefinitionReads.GetProductionPrefab,
            TryGetConfiguredUnitReadModel = (int index, out SourceUnitPrefab prefab, out string display,
                out int price, out bool canRequest, out bool isVehicle) =>
            {
                prefab = null; display = string.Empty; price = 0; canRequest = false; isVehicle = false;
                if (!configuredUnits.TryRead(index, out SourceUnitPrefab authored, out display, out price, out canRequest, out isVehicle)) return false;
                if (index < 0 || index >= configured.Count || configured[index].name != authored.name) return false;
                prefab = configured[index]; return true;
            },
            CountPendingProductionsForFaction = (byte faction, string unitId) => copyBuildings.Values.Sum(building =>
                (building.HasOwnerFaction && building.OwnerFactionId == faction ? building.PendingProductions?.Count(item =>
                    item.Prefab != null && BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(item.Prefab.name) == unitId) ?? 0 : 0)),
            CountRuntimeProducedUnitsForFaction = (byte faction, string unitId) => copyBuildings.Values.Sum(building =>
                building.HasOwnerFaction && building.OwnerFactionId == faction && building.ProducedUnits != null && building.ProducedUnitPrefabs != null
                    ? building.ProducedUnits.Count(entity => building.ProducedUnitPrefabs.TryGetValue(entity, out var prefab) &&
                        BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name) == unitId) : 0),
            TryGetEntityManager = (out EntityManager manager) => { manager = copy; return true; }
        };
        var requestSystem = new SourceCampAvailabilityKernel();
        if (copy == null || deliveryOwner == null || requestSystem == null || requestContext == null)
            throw new InvalidOperationException("Complete same-copy production dispatch inputs required");
        var dispatcher = new SourceFactionProductionDispatcher(copy, boundary, deliveryOwner, requestSystem, requestContext);
        double beforeClock = copy.WorldUnmanaged.Time.ElapsedTime;
        float now = (float)beforeClock;
        Require(!float.IsNaN(now) && !float.IsInfinity(now), "Original source-copy production clock must be finite");
        var queueBefore = CopyQueueRows(copyBuildings);
        Require(queueBefore.Count == 0, "No copy production queue may already be progressed before startup RNG is bound");
        var copyRequestBefore = ProductionRequestRows(copy, boundary);
        SourceS073LegacyCatalogScope.VerifyProjectionControls();
        using var legacyCatalogScope = new SourceS073LegacyCatalogScope(source, copy, configuredUnits, configured);
        JObject legacyCatalogEvidence = JObject.FromObject(legacyCatalogScope.Evidence);
        using var sourceMatches = source.CreateEntityQuery(typeof(SkirmishMatchState));
        using var sourceGameplay = source.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
        Require(sourceMatches.CalculateEntityCount() == 1 && sourceGameplay.CalculateEntityCount() == 1,
            "Unique source S073 match and runtime gameplay entities required for source-world guard");
        JObject sourceGuardBefore = CaptureSourceGuard(source, sourceMatches.GetSingletonEntity(), sourceGameplay.GetSingletonEntity(), sameRunBirths);
        dispatcher.ProcessS073Legacy(now);
        JObject sourceGuardAfter = CaptureSourceGuard(source, sourceMatches.GetSingletonEntity(), sourceGameplay.GetSingletonEntity(), sameRunBirths);
        Require(JToken.DeepEquals(sourceGuardBefore, sourceGuardAfter), "Direct original S073 request consumer mutated the source ECS world");
        Require(JToken.DeepEquals(sourceRegistryBefore, RegistryQueueState(sourceBuildings)),
            "Direct original S073 request consumer mutated the source runtime producer registry");
        var copyRequestAfter = ProductionRequestRows(copy, boundary);
        var queueAfter = CopyQueueRows(copyBuildings);
        bool succeeded = copyRequestAfter.Count == copyRequestBefore.Count &&
            (int?)copyRequestAfter[0]["status"] == (int)BuildingFactionUnitProductionRequest.Succeeded &&
            queueAfter.Count == 1 && (string)queueAfter[0]["unit_id"] == "Unit_Chr_Soldier_Male_02_Alt_04";
        Require(succeeded, "Original S073 request consumer must accept the actual pending request and append exactly one source-owned queue item");
        uint randomStateAfterAppend = deliveryOwner.RandomState;
        JObject deliveryAdvance = new JObject { ["status"] = "NotRequested", ["ticks"] = 0 };
        if (advanceDelivery)
        {
            const int deliveryTicks = 900;
            const float deliveryDelta = 1f / 60f;
            uint initialRandomState = deliveryOwner.RandomState;
            int attemptedTick = 0;
            try
            {
                for (int tick = 1; tick <= deliveryTicks; tick++)
                {
                    attemptedTick = tick;
                    double elapsed = beforeClock + tick / 60d;
                    copy.WorldUnmanaged.Time = new TimeData { ElapsedTime = elapsed, DeltaTime = deliveryDelta };
                    deliveryOwner.Advance((float)elapsed, deliveryDelta);
                }
            }
            catch (Exception error)
            {
                var failure = new JObject { ["scope"] = "DisposableCopy_HistoricalInput_NoNativeParity",
                    ["attempted_tick"] = attemptedTick, ["completed_ticks"] = Math.Max(0, attemptedTick - 1),
                    ["delta_seconds"] = deliveryDelta, ["elapsed_seconds"] = copy.WorldUnmanaged.Time.ElapsedTime,
                    ["exception_type"] = error.GetType().FullName, ["exception_message"] = error.Message,
                    ["events"] = new JArray(deliveryOwner.DeliveryEvents.Select(item => item.DeepClone())),
                    ["delivered_members"] = deliveryOwner.DeliveredMembers, ["queue_at_failure"] = CopyQueueRows(copyBuildings),
                    ["source_ecs_guard_unchanged"] = JToken.DeepEquals(sourceGuardBefore, CaptureSourceGuard(source,
                        sourceMatches.GetSingletonEntity(), sourceGameplay.GetSingletonEntity(), sameRunBirths)),
                    ["source_registry_unchanged"] = JToken.DeepEquals(sourceRegistryBefore, RegistryQueueState(sourceBuildings)),
                    ["mission_ready"] = false };
                Console.WriteLine("[S073CopyDeliveryFailure] " + failure.ToString(Newtonsoft.Json.Formatting.None));
                throw;
            }
            Require(JToken.DeepEquals(sourceGuardBefore, CaptureSourceGuard(source, sourceMatches.GetSingletonEntity(), sourceGameplay.GetSingletonEntity(), sameRunBirths)),
                "Copy delivery advance mutated the source ECS world");
            Require(JToken.DeepEquals(sourceRegistryBefore, RegistryQueueState(sourceBuildings)),
                "Copy delivery advance mutated the source runtime producer registry");
            deliveryAdvance = new JObject { ["status"] = "FixedTickOwnerAdvanceCompleted", ["ticks"] = deliveryTicks,
                ["delta_seconds"] = deliveryDelta, ["start_seconds"] = beforeClock,
                ["end_seconds"] = copy.WorldUnmanaged.Time.ElapsedTime,
                ["events"] = new JArray(deliveryOwner.DeliveryEvents.Select(item => item.DeepClone())),
                ["delivered_members"] = deliveryOwner.DeliveredMembers, ["queue_after_advance"] = CopyQueueRows(copyBuildings),
                ["initial_rng_state_from_historical_observation"] = initialRandomState, ["final_rng_state"] = deliveryOwner.RandomState,
                ["same_session_rng_parity_proven"] = false, ["native_delivery_parity_proven"] = false,
                ["source_camera_capability"] = sourceCameraCapability.Provenance(),
                ["captured_clearance_parity"] = capturedClearanceParity, ["source_world_mutated"] = false,
                ["full_scheduler_executed"] = false, ["mission_ready"] = false };
        }
        return new JObject
        {
            ["status"] = "OriginalLegacyRequestAcceptedIntoCopyQueue",
            ["consumer_invoked"] = true,
            ["source_method_path"] = "generated SourceFactionProductionDispatcher.ProcessProductionRequests -> generated SourceCampAvailabilityKernel.QueueFactionUnitProductionRequest -> original BuildingProductionQueueCompositionSystemHelper.TryQueuePlayerUnitFromBuilding",
            ["camp_availability_scope_invoked"] = false,
            ["camp_specific_preflight_claimed_as_s073_requirement"] = false,
            ["legacy_catalog_membership_projection"] = legacyCatalogEvidence,
            ["legacy_catalog_projection_controls_passed"] = true,
            ["native_source_recipe_observation"] = native.Provenance,
            ["same_run_source_registry"] = sourceQueueGuard,
            ["copy_registry_producer_count"] = copyBuildings.Count,
            ["configured_unit_order"] = new JArray(configured.Select(unit => unit.name)),
            ["copy_clock_elapsed_before_request_consumer"] = beforeClock,
            ["copy_clock_used_by_original_consumer"] = now,
            ["copy_request_before"] = copyRequestBefore,
            ["copy_request_after"] = copyRequestAfter,
            ["copy_queue_before"] = queueBefore,
            ["copy_queue_after"] = queueAfter,
            ["request_status_succeeded"] = true,
            ["request_source_id_and_cost_preserved"] = true,
            ["queue_entry_source_slot_and_metadata_validated"] = true,
            ["source_ecs_guard_unchanged"] = true,
            ["source_runtime_registry_unchanged"] = true,
            ["source_world_mutated"] = false,
            ["native_payment_or_receipt_created"] = false,
            ["random_state_seed_scope"] = "Native S073 startup observation at Queued; same copy-session continuity is not established",
            ["queue_append_random_state_dependency"] = false,
            ["native_queue_time_rng_state_observed"] = false,
            ["same_time_queue_rng_parity_claimed"] = false,
            ["copy_delivery_adapter_constructor_state_after_append"] = randomStateAfterAppend,
            ["copy_delivery_adapter_rng_unchanged_by_append"] = randomStateAfterAppend == native.ConstructorRandomState,
            ["queue_owner_advanced"] = advanceDelivery,
            ["copy_delivery_advance"] = deliveryAdvance,
            ["copy_template_import"] = copyTemplateEvidence,
            ["production_completed"] = advanceDelivery ? (bool?)null : false,
            ["source_clearance_projection"] = sourceClearanceProjection != null
                ? sourceClearanceProjection.Evidence()
                : new JObject { ["status"] = "NotRequested", ["adapter_bound"] = false,
                    ["callback_invoked"] = false, ["delivery_invoked"] = false,
                    ["mission_ready"] = false },
            ["source_clearance_differential_control"] = sourceClearanceDifferential,
            ["spawned_units"] = deliveryOwner.DeliveredMembers,
            ["mission_ready"] = false
        };
    }

    static JArray CopyQueueRows(IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings)
    {
        var rows = new JArray();
        foreach (var pair in buildings.OrderBy(pair => pair.Key))
        {
            var queue = pair.Value.PendingProductions;
            if (queue == null) continue;
            for (int i = 0; i < queue.Count; i++)
            {
                var pending = queue[i];
                rows.Add(new JObject { ["producer_id"] = pair.Key, ["queue_index"] = i,
                    ["production_index"] = pending.ProductionIndex, ["unit_id"] = pending.Prefab?.name,
                    ["started_at"] = pending.StartedAt, ["ready_at"] = pending.ReadyAt,
                    ["reserved_slot_index"] = pending.ReservedProductionSlotIndex,
                    ["transport_prefab"] = pending.TransportPrefab?.name,
                    ["transport_arrival_seconds"] = pending.TransportArrivalSeconds,
                    ["transport_hold_seconds"] = pending.TransportHoldForNextReadySeconds,
                    ["remaining_quantity"] = pending.RemainingQuantity,
                    ["receipt_owner_is_null"] = pending.ReceiptOwner == Entity.Null,
                    ["receipt_id"] = pending.ReceiptId });
            }
        }
        return rows;
    }

    static JObject RegistryQueueState(IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings)
    {
        var rows = new JArray();
        foreach (var pair in buildings.OrderBy(pair => pair.Key))
        {
            var building = pair.Value;
            rows.Add(new JObject { ["building_id"] = pair.Key, ["combat_entity"] = building.CombatEntity.ToString(),
                ["definition_key"] = building.Definition?.Prefab?.name,
                ["has_owner_faction"] = building.HasOwnerFaction,
                ["owner_faction_id"] = building.HasOwnerFaction ? (int?)building.OwnerFactionId : null,
                ["production_spawn_count"] = building.ProductionSpawnLocalPositions?.Length ?? 0,
                ["produced_slot_count"] = building.ProducedUnitSlots?.Length ?? 0,
                ["produced_slots_empty"] = building.ProducedUnitSlots == null || building.ProducedUnitSlots.All(entity => entity == Entity.Null),
                ["pending_count"] = building.PendingProductions?.Count ?? 0,
                ["produced_unit_count"] = building.ProducedUnits?.Count ?? 0,
                ["produced_prefab_count"] = building.ProducedUnitPrefabs?.Count ?? 0,
                ["produced_source_key_count"] = building.ProducedUnitSourceKeys?.Count ?? 0 });
        }
        return new JObject { ["buildings"] = rows, ["pending_queue"] = CopyQueueRows(buildings) };
    }
    const int ProductionOwnerIntervalSeconds = 15;
    const int ProductionOwnerTicksAt60Hz = ProductionOwnerIntervalSeconds * 60;

    static JArray ProductionPlanEntries(EntityManager manager, Entity entity)
    {
        var rows = new JArray();
        if (!manager.HasBuffer<AIProductionPlanEntry>(entity)) return rows;
        var entries = manager.GetBuffer<AIProductionPlanEntry>(entity);
        for (int i = 0; i < entries.Length; i++)
            rows.Add(new JObject { ["index"] = i, ["unit_id"] = entries[i].UnitId.ToString() });
        return rows;
    }

    static JArray ProductionConfiguredUnits(EntityManager manager, Entity entity)
    {
        var rows = new JArray();
        if (!manager.HasBuffer<BuildingConfiguredUnitReadModel>(entity)) return rows;
        var buffer = manager.GetBuffer<BuildingConfiguredUnitReadModel>(entity);
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i];
            rows.Add(new JObject { ["index"] = i, ["unit_id"] = value.UnitId.ToString(),
                ["display_name"] = value.DisplayName.ToString(), ["credits_cost"] = value.CreditsCost,
                ["materials_cost"] = value.Price, ["can_request"] = value.CanRequest, ["is_vehicle"] = value.IsVehicle });
        }
        return rows;
    }

    static JArray ProductionSummaryRows(EntityManager manager, Entity entity)
    {
        var rows = new JArray();
        if (!manager.HasBuffer<BuildingRuntimeUnitProductionSummary>(entity)) return rows;
        var buffer = manager.GetBuffer<BuildingRuntimeUnitProductionSummary>(entity);
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i];
            rows.Add(new JObject { ["index"] = i, ["faction_id"] = value.FactionId,
                ["unit_id"] = value.UnitId.ToString(), ["produced_count"] = value.ProducedCount,
                ["queued_count"] = value.QueuedCount });
        }
        return rows;
    }

    static JArray ProductionRequestRows(EntityManager manager, Entity entity, Dictionary<Entity, Entity> remap = null)
    {
        var rows = new JArray();
        if (!manager.HasBuffer<BuildingFactionUnitProductionRequest>(entity)) return rows;
        var buffer = manager.GetBuffer<BuildingFactionUnitProductionRequest>(entity);
        var reverse = remap == null ? null : remap.ToDictionary(pair => pair.Value, pair => pair.Key);
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i];
            string SourceIdentity(Entity current)
            {
                if (current == Entity.Null) return null;
                if (reverse != null && reverse.TryGetValue(current, out Entity original)) return original.ToString();
                return current.ToString();
            }
            rows.Add(new JObject { ["index"] = i, ["request_id"] = value.RequestId,
                ["faction_id"] = value.FactionId, ["unit_id"] = value.UnitId.ToString(),
                ["status"] = value.Status, ["cost"] = value.Cost, ["materials_cost"] = value.MaterialsCost,
                ["resources_reserved"] = value.ResourcesReserved, ["request_owner"] = SourceIdentity(value.RequestOwner) });
        }
        return rows;
    }

    static JArray ProductionFactionSummaries(EntityManager manager, Entity entity)
    {
        var rows = new JArray();
        if (!manager.HasBuffer<BuildingRuntimeFactionSummary>(entity)) return rows;
        var buffer = manager.GetBuffer<BuildingRuntimeFactionSummary>(entity);
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i];
            rows.Add(new JObject { ["index"] = i, ["faction_id"] = value.FactionId,
                ["stored_fuel_barrels"] = value.StoredFuelBarrels, ["fuel_per_day"] = value.FuelBarrelsPerDay });
        }
        return rows;
    }

    static JArray ProductionUsableFuel(EntityManager manager, Entity entity)
    {
        var rows = new JArray();
        if (!manager.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(entity)) return rows;
        var buffer = manager.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(entity);
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i];
            rows.Add(new JObject { ["index"] = i, ["faction_id"] = value.FactionId,
                ["current_fuel_barrels"] = value.CurrentFuelBarrels, ["fuel_produced_barrels"] = value.FuelProducedBarrels,
                ["fuel_delivered_barrels"] = value.FuelDeliveredBarrels, ["fuel_spent_barrels"] = value.FuelSpentBarrels });
        }
        return rows;
    }

    static JArray ProductionSlotRows(EntityManager manager, Entity entity)
    {
        var rows = new JArray();
        if (!manager.HasBuffer<BuildingProductionSlotReadModel>(entity)) return rows;
        var buffer = manager.GetBuffer<BuildingProductionSlotReadModel>(entity);
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i];
            rows.Add(new JObject { ["index"] = i, ["building_id"] = value.BuildingId.ToString(),
                ["slot_index"] = value.SlotIndex, ["unit_source_key"] = value.UnitSourceKey.ToString(),
                ["unit_id"] = value.UnitId.ToString() });
        }
        return rows;
    }

    static JArray ProductionSpawnPoints(EntityManager manager, Entity entity)
    {
        var rows = new JArray();
        if (!manager.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(entity)) return rows;
        var buffer = manager.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(entity);
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i];
            rows.Add(new JObject { ["index"] = i, ["faction_id"] = value.FactionId,
                ["building_id"] = value.BuildingId.ToString(), ["runtime_building_id"] = value.BuildingRuntimeId,
                ["slot_index"] = value.SlotIndex, ["cell"] = new JArray(value.Cell.x, value.Cell.y),
                ["world_position"] = new JArray(value.WorldPosition.x, value.WorldPosition.y, value.WorldPosition.z) });
        }
        return rows;
    }

    static JObject CaptureProductionInputs(EntityManager manager, Entity planEntity, Entity boundaryEntity,
        Entity economyEntity, IReadOnlyList<Entity> producerRecordEntities, Dictionary<Entity, Entity> remap = null)
    {
        var reverse = remap == null ? null : remap.ToDictionary(pair => pair.Value, pair => pair.Key);
        string SourceIdentity(Entity current)
        {
            if (reverse != null && reverse.TryGetValue(current, out Entity original)) return original.ToString();
            return current.ToString();
        }
        var plan = manager.GetComponentData<AIProductionPlan>(planEntity);
        var economy = manager.GetComponentData<FactionEconomy>(economyEntity);
        var materials = manager.HasComponent<FactionTacticalMaterialsComponent>(economyEntity)
            ? manager.GetComponentData<FactionTacticalMaterialsComponent>(economyEntity) : default;
        var producerRows = new JArray();
        foreach (Entity sourceEntity in producerRecordEntities)
        {
            producerRows.Add(new JObject { ["source_entity"] = SourceIdentity(sourceEntity),
                ["production_slots"] = ProductionSlotRows(manager, sourceEntity),
                ["faction_spawn_points"] = ProductionSpawnPoints(manager, sourceEntity) });
        }
        return new JObject
        {
            ["plan"] = new JObject { ["source_entity"] = SourceIdentity(planEntity), ["faction_id"] = plan.FactionId,
                ["enabled"] = plan.Enabled, ["next_unit_index"] = plan.NextUnitIndex,
                ["target_produced_units"] = plan.TargetProducedUnits, ["max_queued_units"] = plan.MaxQueuedUnits,
                ["interval_seconds"] = plan.UnitProductionIntervalSeconds, ["last_production_time"] = plan.LastProductionTime,
                ["entries"] = ProductionPlanEntries(manager, planEntity) },
            ["economy"] = new JObject { ["source_entity"] = SourceIdentity(economyEntity), ["faction_id"] = economy.FactionId,
                ["money"] = economy.Money, ["materials_only_construction"] = economy.MaterialsOnlyConstruction,
                ["materials"] = manager.HasComponent<FactionTacticalMaterialsComponent>(economyEntity)
                    ? new JObject { ["faction_id"] = materials.FactionId, ["current"] = materials.Current,
                        ["capacity"] = materials.Capacity, ["lifetime_spent"] = materials.LifetimeSpent,
                        ["lifetime_construction_spent"] = materials.LifetimeConstructionSpent, ["version"] = materials.Version } : null },
            ["building_runtime_boundary"] = new JObject { ["source_entity"] = SourceIdentity(boundaryEntity),
                ["has_runtime_tag"] = manager.HasComponent<BuildingRuntimeStateTag>(boundaryEntity),
                ["configured_units"] = ProductionConfiguredUnits(manager, boundaryEntity),
                ["unit_production_summaries"] = ProductionSummaryRows(manager, boundaryEntity),
                ["resource_summaries"] = ProductionFactionSummaries(manager, boundaryEntity),
                ["usable_fuel_summaries"] = ProductionUsableFuel(manager, boundaryEntity),
                ["requests"] = ProductionRequestRows(manager, boundaryEntity, remap) },
            ["physical_producer_records"] = producerRows
        };
    }

    static JObject RunSourceProductionOwnerOnCopy(EntityManager source, EntityManager copy,
        Dictionary<Entity, Entity> remap, SameRunBirthInputs sameRunBirths,
        HashSet<Entity> disposableEntities, string combatPresetPath, LegacyInitialSpawnData sourceLegacyConfig,
        int reinforcementInfantryTarget, int attackSquadSize,
        Dictionary<int, RuntimeBuildingEntity> sourceBuildings, S073NativeConfiguredUnits sourceConfiguredUnits,
        string repositoryRoot, bool historicalInputDiagnostic, SourceS073HostAbsentPresentationCameraCapability sourceCameraCapability,
        string productionPrefabPath, string productionPrefabSha256)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(ProductionOwnerOptIn), "1", StringComparison.Ordinal))
            return new JObject { ["status"] = "NotRequested", ["owner_invoked"] = false };

        bool spawnpointBridgeRequested = string.Equals(Environment.GetEnvironmentVariable(ProductionSpawnpointBridgeOptIn), "1", StringComparison.Ordinal);
        JObject spawnpointBridgeEvidence = null;

        using var planQuery = source.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>(),
            ComponentType.ReadOnly<AIProductionPlanEntry>());
        using var boundaryQuery = source.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
            ComponentType.ReadOnly<BuildingConfiguredUnitReadModel>(),
            ComponentType.ReadOnly<BuildingRuntimeUnitProductionSummary>(),
            ComponentType.ReadWrite<BuildingFactionUnitProductionRequest>());
        using var slotsQuery = source.CreateEntityQuery(typeof(BuildingProductionSlotReadModel));
        using var spawnPointsQuery = source.CreateEntityQuery(typeof(BuildingFactionProductionSpawnPointReadModel));
        using var plans = planQuery.ToEntityArray(Allocator.Temp);
        using var boundaries = boundaryQuery.ToEntityArray(Allocator.Temp);
        using var slots = slotsQuery.ToEntityArray(Allocator.Temp);
        using var spawnPoints = spawnPointsQuery.ToEntityArray(Allocator.Temp);

        var blocked = new JArray();
        if (plans.Length < 1) blocked.Add("No actual source AIProductionPlan with AIProductionPlanEntry query entity");
        if (boundaries.Length != 1) blocked.Add("Original owner requires exactly one source BuildingRuntimeState/configured-unit/summary/request boundary");
        Entity selectedPlan = Entity.Null;
        if (plans.Length > 0)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                AIProductionPlan candidate = source.GetComponentData<AIProductionPlan>(plans[i]);
                if (candidate.Enabled != 0 && FactionIdentity.IsAiControlledByDefault(candidate.FactionId))
                {
                    selectedPlan = plans[i];
                    break;
                }
            }
            if (selectedPlan == Entity.Null) blocked.Add("No enabled source AI faction plan");
        }
        Entity boundary = boundaries.Length == 1 ? boundaries[0] : Entity.Null;
        byte factionId = selectedPlan == Entity.Null ? byte.MaxValue : source.GetComponentData<AIProductionPlan>(selectedPlan).FactionId;
        bool economyPresent = sameRunBirths.EconomyBanks.TryGetValue(factionId, out Entity sourceEconomy);
        if (!economyPresent) blocked.Add("No same-run source-created economy bank for the selected plan faction");
        if (selectedPlan != Entity.Null && source.GetBuffer<AIProductionPlanEntry>(selectedPlan).Length == 0)
            blocked.Add("Actual source AI production plan has no entries");
        if (boundary != Entity.Null && source.GetBuffer<BuildingFactionUnitProductionRequest>(boundary).Length != 0)
            blocked.Add("Source production request buffer is not empty at the observed Preparing boundary");
        if (selectedPlan != Entity.Null && !remap.ContainsKey(selectedPlan)) blocked.Add("Actual source plan was not included in the disposable copy");
        if (boundary != Entity.Null && !remap.ContainsKey(boundary)) blocked.Add("Actual source production boundary was not included in the disposable copy");
        if (economyPresent && !remap.ContainsKey(sourceEconomy)) blocked.Add("Same-run source economy bank was not included in the disposable copy");

        if (blocked.Count > 0)
            return new JObject { ["status"] = "BlockedPreflight", ["owner_invoked"] = false,
                ["blocked_source_inputs"] = blocked, ["source_production_plans"] = plans.Length,
                ["source_production_boundaries"] = boundaries.Length,
                ["source_production_slot_record_entities"] = slots.Length,
                ["source_production_spawn_point_record_entities"] = spawnPoints.Length,
                ["source_world_mutated"] = false, ["production_completed"] = false };

        var producerEntities = slots.ToArray().Concat(spawnPoints.ToArray()).Distinct().OrderBy(x => x.Index).ToArray();
        Entity sourcePlan = selectedPlan;
        Entity copyPlan = remap[sourcePlan];
        Entity copyBoundary = remap[boundary];
        Entity copyEconomy = remap[sourceEconomy];
        Entity[] copyProducerEntities = producerEntities.Select(e => remap.TryGetValue(e, out Entity v) ? v : Entity.Null).ToArray();
        if (copyProducerEntities.Any(e => e == Entity.Null))
            return new JObject { ["status"] = "BlockedPreflight", ["owner_invoked"] = false,
                ["blocked_source_inputs"] = new JArray("A source physical producer record owner is missing from the disposable closure"),
                ["source_world_mutated"] = false, ["production_completed"] = false };

        var sourceGuardBefore = CaptureProductionInputs(source, sourcePlan, boundary, sourceEconomy, producerEntities);
        var copyBeforeRules = CaptureProductionInputs(copy, copyPlan, copyBoundary, copyEconomy, copyProducerEntities, remap);
        Require(JToken.DeepEquals(sourceGuardBefore, copyBeforeRules),
            "Source-derived AI production plan/economy/configured/request/producer inputs differ in the disposable copy");
        if (spawnpointBridgeRequested)
        {
            string evidencePath = Path.GetFullPath(NativeSpawnpointEvidencePath);
            if (!File.Exists(evidencePath))
                throw new FileNotFoundException("Pinned native same-world spawnpoint observation is required for the opt-in bridge", evidencePath);
            byte[] evidenceBytes = File.ReadAllBytes(evidencePath);
            string evidenceSha;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                evidenceSha = string.Concat(sha.ComputeHash(evidenceBytes).Select(value => value.ToString("x2")));
            Require(string.Equals(evidenceSha, NativeSpawnpointEvidenceSha256, StringComparison.Ordinal),
                "Pinned native S073 spawnpoint observation bytes changed");
            JObject nativeEvidence = JObject.Parse(System.Text.Encoding.UTF8.GetString(evidenceBytes));
            JArray nativeBuildings = nativeEvidence["buildings"] as JArray;
            Require((string)nativeEvidence["kind"] == "S073NativeSpawnpointTransformObservation" &&
                (string)nativeEvidence["scope"] == "ActualRegisteredS073BuildingGameObjectTransformPointAndAssociatedCombatEntityLocalTransform" &&
                (string)nativeEvidence["source_identity_hash"] == "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" &&
                nativeBuildings != null && nativeBuildings.Count > 0 &&
                nativeBuildings.All(building => (bool?)building["root_position_equal_ecs_bits"] == true &&
                    (building["production_spawnpoints"] as JArray)?.Count > 0 &&
                    ((JArray)building["production_spawnpoints"]).All(point => (bool?)point["gameobject_transform_point_equals_ecs_spawnpoint_bits"] == true)),
                "Pinned native same-world root and production-spawnpoint transform observations required");
            int sourceSpawnpointRows = producerEntities.Sum(entity => ProductionSpawnPoints(source, entity).Count);
            int copySpawnpointRows = copyProducerEntities.Sum(entity => ProductionSpawnPoints(copy, entity).Count);
            Require(sourceSpawnpointRows > 0 && sourceSpawnpointRows == copySpawnpointRows,
                "Actual published authored-array spawnpoint rows must be present in both source and disposable copy");
            spawnpointBridgeEvidence = new JObject
            {
                ["status"] = "SourceRowsCopiedAndEqual",
                ["publisher"] = "SourceS073LegacyHostHandoff.PublishOriginalLegacyProductionSpawnPoints",
                ["authored_input"] = "Registered RuntimeBuildingEntity.ProductionSpawnLocalPositions",
                ["transform_input"] = "Same-world CombatEntity LocalTransform",
                ["native_observation_path"] = evidencePath,
                ["native_observation_sha256"] = evidenceSha,
                ["native_observation_buildings"] = nativeBuildings.Count,
                ["native_observation_session_id"] = (string)nativeEvidence["session_id"],
                ["native_observation_is_this_source_session"] = false,
                ["source_spawnpoint_rows"] = sourceSpawnpointRows,
                ["copy_spawnpoint_rows"] = copySpawnpointRows,
                ["source_copy_rows_equal"] = true,
                ["production_request_or_completion_claimed"] = false,
                ["mission_ready"] = false
            };
        }
        var copyMatchQuery = copy.CreateEntityQuery(typeof(SkirmishMatchState));
        if (copyMatchQuery.CalculateEntityCount() != 1)
            return new JObject { ["status"] = "BlockedPreflight", ["owner_invoked"] = false,
                ["blocked_source_inputs"] = new JArray("Copied match session singleton required before original Rules owner"),
                ["source_world_mutated"] = false, ["production_completed"] = false };
        Entity copyMatchEntity = copyMatchQuery.GetSingletonEntity();
        if (!string.Equals(Environment.GetEnvironmentVariable(OwnerOptIn), "1", StringComparison.Ordinal) ||
            !string.Equals(Environment.GetEnvironmentVariable(RulesOptIn), "1", StringComparison.Ordinal))
            return new JObject { ["status"] = "BlockedPreflight", ["owner_invoked"] = false,
                ["blocked_source_inputs"] = new JArray("Opt-in original Preparing Rules owner is required before production"),
                ["source_production_inputs_before_rules"] = sourceGuardBefore,
                ["copy_production_inputs_before_rules"] = copyBeforeRules,
                ["source_world_mutated"] = false, ["production_completed"] = false };
        copy.WorldUnmanaged.Time = source.WorldUnmanaged.Time;
        JObject rulesOwner = RunOriginalOwnersOnCopy(copy, copyMatchEntity, disposableEntities,
            combatPresetPath, remap, true, false, sourceLegacyConfig,
            reinforcementInfantryTarget, attackSquadSize, preparingRulesOnly: true);
        if ((bool?)rulesOwner["owner_invoked"] != true)
            return new JObject { ["status"] = "BlockedPreflight", ["owner_invoked"] = false,
                ["blocked_source_inputs"] = new JArray("Original Preparing Rules owner did not complete its source-gated transition"),
                ["pre_production_rules_owner"] = rulesOwner,
                ["source_production_inputs_before_rules"] = sourceGuardBefore,
                ["copy_production_inputs_before_rules"] = copyBeforeRules,
                ["source_world_mutated"] = false, ["production_completed"] = false };
        var copyBefore = CaptureProductionInputs(copy, copyPlan, copyBoundary, copyEconomy, copyProducerEntities, remap);
        var sourceRequestsBefore = ProductionRequestRows(source, boundary);
        string candidateResourceEvaluation = "NoCandidate";
        var planBefore = copy.GetComponentData<AIProductionPlan>(copyPlan);
        var entriesBefore = copy.GetBuffer<AIProductionPlanEntry>(copyPlan);
        var unitsBefore = copy.GetBuffer<BuildingConfiguredUnitReadModel>(copyBoundary);
        var economyBefore = copy.GetComponentData<FactionEconomy>(copyEconomy);
        var materialsBefore = copy.GetComponentData<FactionTacticalMaterialsComponent>(copyEconomy);
        if (economyBefore.MaterialsOnlyConstruction != 1)
            return new JObject { ["status"] = "BlockedPreflight", ["owner_invoked"] = false,
                ["blocked_source_inputs"] = new JArray("Original Preparing Rules owner did not establish the observed materials-only construction policy"),
                ["pre_production_rules_owner"] = rulesOwner,
                ["source_production_inputs_before_rules"] = sourceGuardBefore,
                ["copy_production_inputs_before_rules"] = copyBeforeRules,
                ["copy_production_inputs_after_rules"] = copyBefore,
                ["copy_materials_only_construction_after_rules"] = economyBefore.MaterialsOnlyConstruction,
                ["source_world_mutated"] = false, ["production_completed"] = false };
        if (entriesBefore.Length > 0)
        {
            int candidateIndex = planBefore.NextUnitIndex % entriesBefore.Length;
            if (candidateIndex < 0) candidateIndex += entriesBefore.Length;
            string candidateId = entriesBefore[candidateIndex].UnitId.ToString();
            for (int i = 0; i < unitsBefore.Length; i++)
            {
                var candidate = unitsBefore[i];
                if (!candidate.UnitId.ToString().Equals(candidateId, StringComparison.Ordinal)) continue;
                candidateResourceEvaluation = FactionConstructionResourceUtilitySystemHelper.Evaluate(
                    economyBefore, materialsBefore, Math.Max(0, candidate.CreditsCost), candidate.Price).ToString();
                break;
            }
        }
        TimeData sourceClock = source.WorldUnmanaged.Time;
        copy.WorldUnmanaged.Time = sourceClock;
        double sourceElapsed = sourceClock.ElapsedTime;
        float sourceSessionElapsed = copy.GetComponentData<SkirmishMatchState>(copyMatchEntity).ElapsedSeconds;

        var state = new SystemState { EntityManager = copy, Enabled = true };
        var owner = new AIProductionSystem();
        var rulesState = new SystemState { EntityManager = copy, Enabled = true };
        var rulesOwnerSystem = new SkirmishRulesSystem();
        bool hadPriorRulesPreset = SourceS073LegacyCombatPresetProvider.TryGetBound(copy, out var priorRulesPreset);
        int rulesOwnerUpdates = 0;
        SourceS073LegacyCombatPresetProvider.Bind(copy, combatPresetPath);
        state.Begin();
        try
        {
            owner.OnCreate(ref state);
            rulesState.Begin();
            try
            {
                rulesOwnerSystem.OnCreate(ref rulesState);
                if (!rulesState.Enabled || !rulesState.RequiredQueriesPresent)
                    throw new InvalidDataException("Original Playing Rules owner required-query gate is closed on the disposable copy");
            }
            finally { rulesState.End(); }
            for (int tick = 1; tick <= ProductionOwnerTicksAt60Hz; tick++)
            {
                double tickSeconds = tick / 60d;
                var clock = copy.WorldUnmanaged;
                clock.Time = new TimeData { ElapsedTime = sourceElapsed + tickSeconds, DeltaTime = 1f / 60f };
                copy.WorldUnmanaged.Time = clock.Time;
                state.Begin();
                try { owner.OnUpdate(ref state); }
                finally { state.End(); }
                rulesState.Begin();
                try
                {
                    if (rulesState.Enabled && rulesState.RequiredQueriesPresent)
                    {
                        rulesOwnerSystem.OnUpdate(ref rulesState);
                        rulesOwnerUpdates++;
                    }
                }
                finally { rulesState.End(); }
            }
        }
        finally
        {
            state.End();
            SourceS073LegacyCombatPresetProvider.RestoreBinding(copy, hadPriorRulesPreset ? priorRulesPreset : null);
        }

        var sourceGuardAfter = CaptureProductionInputs(source, sourcePlan, boundary, sourceEconomy, producerEntities);
        var copyAfter = CaptureProductionInputs(copy, copyPlan, copyBoundary, copyEconomy, copyProducerEntities, remap);
        Require(JToken.DeepEquals(sourceGuardBefore, sourceGuardAfter),
            "Original AIProductionSystem mutated source plan/economy/readmodel/request/producer inputs");
        var copyPlanAfter = copy.GetComponentData<AIProductionPlan>(copyPlan);
        var copyEconomyAfter = copy.GetComponentData<FactionEconomy>(copyEconomy);
        var copyMaterialsAfter = copy.GetComponentData<FactionTacticalMaterialsComponent>(copyEconomy);
        var copyMatchAfter = copy.GetComponentData<SkirmishMatchState>(copyMatchEntity);
        var copyRequestsAfter = ProductionRequestRows(copy, copyBoundary);
        int newRequests = copyRequestsAfter.Count - sourceRequestsBefore.Count;
        string ownerResult = newRequests > 0 ? "RequestPending" : "NoRequest";
        return new JObject
        {
            ["status"] = "OwnerInvoked_" + ownerResult,
            ["owner"] = "OriginalAIProductionSystem.OnCreate+OnUpdate",
            ["owner_invoked"] = true, ["owner_update_count"] = ProductionOwnerTicksAt60Hz,
            ["fixed_tick_delta_seconds"] = 1d / 60d,
            ["owner_interval_seconds"] = ProductionOwnerIntervalSeconds,
            ["source_elapsed_seconds"] = sourceElapsed,
            ["source_session_elapsed_seconds"] = sourceSessionElapsed,
            ["copy_owner_tick_elapsed_seconds"] = sourceElapsed + ProductionOwnerIntervalSeconds,
            ["copy_session_elapsed_after_owner_seconds"] = copyMatchAfter.ElapsedSeconds,
            ["copy_match_elapsed_set_directly_by_fixture"] = false,
            ["original_playing_rules_owner"] = "SkirmishRulesSystem.OnUpdate.Playing",
            ["original_playing_rules_owner_updates"] = rulesOwnerUpdates,
            ["match_elapsed_advanced_by_original_rules_owner"] = rulesOwnerUpdates == ProductionOwnerTicksAt60Hz,
            ["copy_clock_started_from_source_world_time"] = true,
            ["source_production_inputs_before"] = sourceGuardBefore,
            ["source_production_inputs_after"] = sourceGuardAfter,
            ["copy_production_inputs_before_rules"] = copyBeforeRules,
            ["copy_production_inputs_after_rules_before_owner"] = copyBefore,
            ["pre_production_rules_owner"] = rulesOwner,
            ["source_inputs_unchanged"] = true,
            ["copy_production_inputs_before"] = copyBefore,
            ["copy_production_inputs_after"] = copyAfter,
            ["selected_source_plan_entity"] = sourcePlan.ToString(), ["selected_copy_plan_entity"] = copyPlan.ToString(),
            ["source_economy_entity"] = sourceEconomy.ToString(), ["copy_economy_entity"] = copyEconomy.ToString(),
            ["source_boundary_entity"] = boundary.ToString(), ["copy_boundary_entity"] = copyBoundary.ToString(),
            ["source_configured_unit_count"] = ProductionConfiguredUnits(source, boundary).Count,
            ["source_production_slot_record_rows"] = producerEntities.Sum(e => ProductionSlotRows(source, e).Count),
            ["source_production_spawn_point_record_rows"] = producerEntities.Sum(e => ProductionSpawnPoints(source, e).Count),
            ["preexisting_source_request_count"] = sourceRequestsBefore.Count,
            ["new_copy_request_count"] = newRequests,
            ["source_candidate_resource_evaluation_before_owner"] = candidateResourceEvaluation,
            ["copy_economy_money_after"] = copyEconomyAfter.Money,
            ["copy_tactical_materials_after"] = copyMaterialsAfter.Current,
            ["copy_plan_last_production_time_after"] = copyPlanAfter.LastProductionTime,
            ["copy_plan_next_unit_index_after"] = copyPlanAfter.NextUnitIndex,
            ["production_completed"] = false,
            ["production_spawnpoint_bridge"] = spawnpointBridgeEvidence ?? new JObject { ["status"] = "NotRequested" },
            ["downstream_request_consumer_invoked"] = false,
            ["downstream_request_consumer_diagnostic"] = ProductionRequestConsumerPreflight(source, copy, copyBoundary,
                remap, sameRunBirths, sourceBuildings, sourceConfiguredUnits, repositoryRoot, historicalInputDiagnostic, sourceCameraCapability,
                productionPrefabPath, productionPrefabSha256),
            ["source_world_mutated"] = false,
            ["mission_ready"] = false
        };
    }
}

namespace Game.Runtime
{
    // Opt-in source-geometry adapter for the disposable S073 copy. This binds a
    // captured shared-Campaign AABB projection; it does not qualify current
    // provider parity and its callback is not invoked by this queue-only owner.
    internal sealed partial class SourceFactionProductionDispatcher
    {
        // This calls the exact generated original ProcessProductionRequests body. The S002 camp
        // availability scope is not part of the native S073 legacy production owner.
        internal void ProcessS073Legacy(float now)
        {
            if (Poisoned || owner.Poisoned) throw new InvalidOperationException("S073 copy request dispatcher/queue is poisoned");
            if (float.IsNaN(now) || float.IsInfinity(now)) throw new ArgumentException("Finite same-copy source clock required");
            try { ProcessProductionRequests(requestSystem, context, new SourceFactionProductionReadModels(), context, em, boundary, now); }
            catch { Poisoned = true; owner.Poisoned = true; throw; }
        }
    }
}
