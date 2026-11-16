using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;

namespace Game.Runtime
{
    // Explicit diagnostic use of historical captures. The pin comparison is
    // not a qualification of current native behavior or scene state.
    internal sealed class SourceS004HistoricalStartingGrant
    {
        readonly Dictionary<string, string> pins;
        readonly GeneratedBuildingDefinitions definitions;
        readonly JObject readModel;
        readonly Dictionary<string, Entity> prefabs;
        List<SourceUnitPrefab> publishedOrder;
        Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry> publishedEntries;
        internal GeneratedStartingBuildingPipeline CompletedPipeline { get; private set; }
        internal GeneratedBuildingDefinitions CompletedDefinitions => CompletedPipeline != null ? definitions : null;
        private SourceS004HistoricalStartingGrant(Dictionary<string, string> pins, GeneratedBuildingDefinitions definitions, JObject readModel,
            Dictionary<string, Entity> prefabs)
        { this.pins = pins; this.definitions = definitions; this.readModel = readModel; this.prefabs = prefabs; }

        static JObject Descriptor(JToken token)
        {
            var value = token as JObject;
            if (value == null || value.Count != 2 || value["path"]?.Type != JTokenType.String || value["sha256"]?.Type != JTokenType.String)
                throw new InvalidDataException("Exact historical factory input descriptor required");
            return value;
        }
        static JObject Read(JObject desc, int limit)
        {
            var bytes = AutonomousWorld.PinnedFile((string)desc["path"], (string)desc["sha256"], limit);
            return JObject.Parse(new UTF8Encoding(false, true).GetString(bytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }
        static string HashFile(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length > 128L * 1024 * 1024)
                throw new InvalidDataException("Bounded regular historical factory source required");
            using var stream = File.OpenRead(path); using var hash = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        internal static SourceS004HistoricalStartingGrant Load(JToken token, string root, EntityManager em, SkirmishResolvedSetup setup)
        {
            var spec = token as JObject;
            string[] fields = { "kind", "setup", "prefab_shared", "prefab_s004", "factory", "readmodel", "pin_drift" };
            if (spec == null || spec.Count != fields.Length || spec.Properties().Any(p => !fields.Contains(p.Name)) ||
                (string)spec["kind"] != "HistoricalS004StartingGrantInputs" || spec["pin_drift"] is not JArray declared ||
                setup == null || setup.CatalogId != "S004" || !Path.IsPathRooted(root) || em == null)
                throw new InvalidDataException("Exact explicitly historical S004 starting inputs required");
            var descriptors = fields.Skip(1).Take(5).ToDictionary(k => k, k => Descriptor(spec[k]), StringComparer.Ordinal);
            SourceS004CompiledSetupInput.RequireExact(setup, (string)descriptors["setup"]["path"], (string)descriptors["setup"]["sha256"]);
            var captures = new Dictionary<string, JObject>();
            foreach (string key in new[] { "prefab_shared", "prefab_s004", "factory", "readmodel" }) captures.Add(key, Read(descriptors[key], 16 * 1024 * 1024));
            var pins = new Dictionary<string, string>(StringComparer.Ordinal); var old = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var capture in captures.Values)
            {
                var sourcePins = capture["source_pins"] as JObject ?? throw new InvalidDataException("Historical factory source pins required");
                if (sourcePins.Count < 1 || sourcePins.Count > 30000) throw new InvalidDataException("Bounded historical factory source pins required");
                foreach (var pin in sourcePins.Properties())
                {
                    string path = Path.GetFullPath(Path.IsPathRooted(pin.Name) ? pin.Name : Path.Combine(root, pin.Name));
                    string recorded = (string)pin.Value;
                    if (old.TryGetValue(path, out string prior) && prior != recorded) throw new InvalidDataException("Historical factory captures use conflicting source epochs");
                    old[path] = recorded;
                    if (!pins.ContainsKey(path)) pins.Add(path, HashFile(path));
                }
            }
            var drift = new JArray();
            foreach (var pin in old.OrderBy(p => p.Key, StringComparer.Ordinal))
                if (pins[pin.Key] != pin.Value) drift.Add(new JObject { ["path"] = pin.Key, ["captured_sha256"] = pin.Value, ["current_sha256"] = pins[pin.Key] });
            if (!JToken.DeepEquals(drift, declared)) throw new InvalidDataException("Historical factory source drift must be declared exactly");
            foreach (var descriptor in descriptors.Values)
            {
                string path = Path.GetFullPath((string)descriptor["path"]), hash = (string)descriptor["sha256"];
                if (pins.TryGetValue(path, out string prior) && prior != hash) throw new InvalidDataException("Conflicting historical factory evidence pin");
                pins[path] = hash;
            }
            var model = captures["readmodel"];
            if ((string)model["kind"] != "S004OriginalConfiguredSpawnableReadModel" ||
                (string)model["authority"] != "OriginalS004ProductionOverlayAndPublishConfiguredSpawnablesReadModel" ||
                (string)model["catalog_id"] != setup.CatalogId || (uint?)model["setup_hash"] != setup.SetupHash ||
                (int?)model["seed"] != setup.Seed || (bool?)model["scene_loaded"] != false)
                throw new InvalidDataException("Historical configured building capture identity differs");
            var prefabs = new Dictionary<string, Entity>(StringComparer.Ordinal);
            foreach (string key in new[] { "prefab_shared", "prefab_s004" })
            {
                var source = captures[key]; var keys = (source["requested_prefab_keys"] as JArray)?.Values<string>().ToArray();
                if (keys == null || keys.Length == 0) throw new InvalidDataException("Historical prefab keys required");
                var imported = GeneratedReferenceTemplateImporter.Import(em, source, Path.GetDirectoryName((string)descriptors[key]["path"]), keys);
                foreach (var pair in imported.Prefabs) if (!prefabs.TryAdd(pair.Key, pair.Value)) throw new InvalidDataException("Duplicate historical prefab key");
            }
            var definitions = GeneratedBuildingDefinitions.LoadS004Source((string)descriptors["factory"]["path"], (string)descriptors["factory"]["sha256"],
                prefabs, setup, (string)descriptors["setup"]["path"], (string)descriptors["setup"]["sha256"]);
            return new SourceS004HistoricalStartingGrant(pins, definitions, model, prefabs);
        }
        internal void InstallSourcePrefabRegistry(EntityManager em, SkirmishResolvedSetup setup)
        {
            if (em == null || setup?.Forces == null || prefabs == null || prefabs.Count == 0)
                throw new InvalidOperationException("Historical source prefab registry prerequisites required");
            using (var existing = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry)))
                if (!existing.IsEmptyIgnoreFilter) throw new InvalidOperationException("Fresh source prefab registry required");
            foreach (var force in setup.Forces)
                if (String.IsNullOrEmpty(force.RuntimePrefabKey) || !prefabs.TryGetValue(force.RuntimePrefabKey, out var entity) || !em.Exists(entity))
                    throw new InvalidDataException("Historical source prefab is missing for an actual starting force");
            var registry = em.CreateEntity(); var entries = em.AddBuffer<UnitPrefabRegistryEntry>(registry);
            foreach (var pair in prefabs.OrderBy(p => p.Key, StringComparer.Ordinal))
                entries.Add(new UnitPrefabRegistryEntry { Prefab = pair.Value });
        }
        internal void PublishAndSubmit(EntityManager em, Entity session, Entity boundary, SkirmishResolvedSetup setup)
        {
            var source = new GeneratedConfiguredDefinitionSource(); source.BindGeneratedDefinitions(definitions.Definitions, true);
            var order = new List<SourceUnitPrefab>(); var entries = new Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry>();
            var rows = readModel["entries"] as JArray;
            if (rows == null || rows.Count != definitions.Definitions.Count) throw new InvalidDataException("Historical configured row count differs");
            foreach (var token in rows)
            {
                var row = token as JObject; string key = (string)row?["visual_key"];
                var matches = definitions.Definitions.Where(d => d.Prefab.name == key).ToArray();
                var native = row?["source_definition"] as JObject;
                if (matches.Length != 1 || native == null || !JToken.DeepEquals(native["prefab"], definitions.AssetIdentities[key]) ||
                    (string)native["display_name"] != matches[0].DisplayName || native["can_request"]?.Type != JTokenType.Boolean)
                    throw new InvalidDataException("Historical configured source identity differs");
                var definition = matches[0]; order.Add(definition.Prefab);
                entries.Add(definition.Prefab, new BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry(definition.DisplayName,
                    definition.Description, definition.Prefab, (bool)native["can_request"], Math.Max(0, definition.CreditsCost)));
            }
            source.BindRequestMetadata(order, entries);
            publishedOrder = order; publishedEntries = entries;
            GeneratedRuntimeSpawnProcessor.PublishConfiguredOnly(source, em, boundary);
            var actual = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
            if (actual.Length != rows.Count) throw new InvalidDataException("Original publisher row count differs");
            for (int i = 0; i < actual.Length; i++)
            {
                var expected = rows[i]["read_model"]; var value = actual[i];
                if (value.BuildingId.ToString() != (string)expected["BuildingId"] || value.DisplayName.ToString() != (string)expected["DisplayName"] ||
                    value.Price != (int)expected["Price"] || value.MaterialsCost != (int)expected["MaterialsCost"] ||
                    value.FootprintCells.x != (int)expected["FootprintCells"]["x"] || value.FootprintCells.y != (int)expected["FootprintCells"]["y"] ||
                    value.CanRequest != (byte)expected["CanRequest"]) throw new InvalidDataException("Original publisher differs from historical capture");
            }
            bool complete = SkirmishStartingBuildingsService.Step(em, session, setup, out var reason);
            if (complete || reason != SkirmishReasonCode.None) throw new InvalidOperationException("Original starting grant submission rejected: " + reason);
            if (em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length != setup.Structures.Length ||
                em.GetBuffer<SkirmishStartingBuildingRequest>(session).Length != setup.Structures.Length)
                throw new InvalidOperationException("Original starting grant request count differs");
        }
        internal JObject ProcessDiagnostic(EntityManager em, Entity session, Entity boundary, Entity gridEntity,
            Entity surfaceEntity, SkirmishResolvedSetup setup, Dictionary<int, RuntimeBuildingEntity> registry,
            float plane, float fixedDeltaTime, bool[] historicalBuildings = null, bool[] historicalRoads = null, bool[] historicalSidewalks = null)
        {
            if (publishedOrder == null || publishedEntries == null || registry == null || registry.Count != 0 ||
                float.IsNaN(plane) || float.IsInfinity(plane) || !(fixedDeltaTime > 0))
                throw new InvalidOperationException("Fresh same-world historical placement prerequisites required");
            var grid = em.GetComponentData<GridConfig>(gridEntity); int size = checked(grid.Width * grid.Height);
            var roads = new bool[size]; BuildingPlacementRoadSurfaceMask.Append(em, surfaceEntity, grid, roads);
            // These three masks are absent source domains, explicitly ignored only
            // in this opt-in diagnostic. No physical/source-fit claim follows.
            var ignored = new bool[size];
            bool useHistoricalMasks = historicalBuildings != null || historicalRoads != null || historicalSidewalks != null;
            if (useHistoricalMasks && (historicalBuildings?.Length != size || historicalRoads?.Length != size || historicalSidewalks?.Length != size))
                throw new InvalidDataException("Complete same-frame historical placement masks required");
            if (useHistoricalMasks) for (int i=0;i<size;i++) roads[i] |= historicalRoads[i];
            using (var all = em.CreateEntityQuery(new EntityQueryDesc { Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities }))
                RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(em, true, true,
                    all.ToEntityArray(Unity.Collections.Allocator.Temp).ToArray(), e => em.HasComponentNamed(e, "Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord"));
            var pipeline = new GeneratedStartingBuildingPipeline(em, gridEntity, boundary, definitions.Definitions, true, registry,
                useHistoricalMasks ? historicalBuildings : ignored, roads,
                useHistoricalMasks ? historicalSidewalks : ignored, ignored, plane, (x, z, w, h) => {
                    var bits = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
                    for (int row = z; row < z + h; row++) for (int col = x; col < x + w; col++)
                        if (bits.IsSet(row * grid.Width + col)) return true;
                    return false;
                });
            pipeline.BindConfiguredRequestMetadata(publishedOrder, publishedEntries, false);
            var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            int frames = 0;
            while (frames < 64 && requests.ToArray().Any(r => r.Status == BuildingRuntimeSpawnRequest.Pending))
            {
                frames++; pipeline.ProcessRequests(frames * fixedDeltaTime, true);
            }
            bool bound = pipeline.SubmitOrBind(session, setup, out var reason);
            if (bound && reason == SkirmishReasonCode.None && registry.Count == setup.Structures.Length &&
                requests.ToArray().All(row => row.Status == BuildingRuntimeSpawnRequest.Succeeded))
                CompletedPipeline = pipeline;
            var observations = new JArray();
            for (int i = 0; i < requests.Length; i++)
            {
                var row = requests[i];
                bool found = registry.TryGetValue(row.BuildingRuntimeId, out var registered);
                bool pathBlocker = found && registered.BlockerEntity != Entity.Null && em.Exists(registered.BlockerEntity);
                observations.Add(new JObject { ["request_id"] = row.RequestId,
                    ["building"] = row.BuildingId.ToString(), ["status"] = row.Status, ["result_code"] = (int)row.ResultCode,
                    ["actual_x"] = row.ActualOrigin.x, ["actual_y"] = row.ActualOrigin.y, ["runtime_id"] = row.BuildingRuntimeId,
                    ["registered"] = found, ["has_path_blocker_entity"] = pathBlocker });
            }
            return new JObject { ["kind"] = "S004HistoricalSourcePlacementDiagnostic", ["frames"] = frames,
                ["requests"] = observations, ["registry_count"] = registry.Count, ["original_bind_complete"] = bound,
                ["bind_reason"] = reason.ToString(), ["water_mask"] = "IgnoredUnknown", ["sidewalk_mask"] = useHistoricalMasks ? "HistoricalCapture" : "IgnoredUnknown",
                ["authored_building_mask"] = useHistoricalMasks ? "HistoricalCapture" : "IgnoredUnknown", ["static_blockers"] = "HistoricalCapture",
                ["source_surface_roads"] = true, ["native_parity"] = false, ["source_fit_certified"] = false,
                ["mission_ready"] = false };
        }
        internal void VerifyInputs() { foreach (var pin in pins) if (HashFile(pin.Key) != pin.Value) throw new InvalidDataException("Historical factory source changed during run: " + pin.Key); }
    }
}
