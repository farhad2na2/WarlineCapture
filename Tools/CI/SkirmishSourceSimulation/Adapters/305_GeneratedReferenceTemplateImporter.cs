using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Game.Components;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    internal static partial class GeneratedReferenceTemplateImporter
    {
        internal sealed class Result
        {
            internal EntityManager Manager;
            internal readonly Dictionary<string,Entity> Prefabs = new Dictionary<string,Entity>(StringComparer.Ordinal);
            internal readonly Dictionary<Entity,Entity> SourceToLogical = new Dictionary<Entity,Entity>();
            internal int ImportedEntities, ImportedComponents, OmittedPresentationComponents;
        }
        private sealed class Row { internal Entity Source; internal bool Enabled; internal readonly List<Cell> Cells = new List<Cell>(); }
        private sealed class Cell { internal Type Type;internal bool Buffer;internal bool? Enabled;internal object Value;internal object[] Items; }
        private sealed class PhysicalBlockerInput
        {
            internal Entity Source;
            internal UnitGrid Grid;
            internal GridBlockerSize Size;
            internal bool HasOrigin, HasFriendly, HasOpening;
            internal RuntimeBuildingCombatInfo Origin;
            internal FriendlyPassGridBlocker Friendly;
            internal StaticGridBlockerSurfaceOpeningComponent Opening;
            internal OperationMapBuildingComponent Building;
            internal OperationMapBuildingIdentity Identity;
            internal UnitFootprint Footprint;
            internal UnitHealth Health;
            internal UnitSourcePrefabKey SourceKey;
            internal UnitDisplayInfo Display;
            internal UnitSelectionHitbox Hitbox;
            internal LocalTransform Transform;
            internal LocalToWorld LocalToWorld;
        }
        internal sealed class FixedMapPhysicalBlocker
        {
            internal string SourceId;
            internal Entity Entity;
            internal int2 Min, Max;
        }
        private static readonly HashSet<string> PresentationOnly = new HashSet<string>(StringComparer.Ordinal) {
            "Game.Components.UnitAttackImpactVfxReference", "Game.Components.UnitMuzzleFlashVfxReference", "Game.Components.GroundMissileLauncherVfxReferenceComponent", "Game.Components.AirMissileLauncherVfxReferenceComponent" };
        internal const string RuntimeActorBirthRecordName = "Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord";
        internal static bool BirthRecordIsInPresentationOmissionSet => PresentationOnly.Contains(RuntimeActorBirthRecordName);
        private static void Need(bool value,string reason) { if(!value)throw new InvalidOperationException(reason); }
        // Decode the isolated streamed source graph without allocating an ECS
        // identity. This is a codec diagnostic, never an import or readiness gate.
        internal static JObject PreflightStreamedSource(JObject source, string binaryDirectory)
        {
            DecodeStreamedSource(source, binaryDirectory, out var rows, out var roots, out int omitted, out int components);
            return StreamedReceipt(source, rows.Count, roots.Count, omitted, components);
        }
        private static JObject StreamedReceipt(JObject source, int entities, int roots, int omitted, int components)
        {
            return new JObject { ["status"] = "Decoded_NotImportAdmission", ["roots"] = roots,
                ["entities"] = entities, ["decoded_components"] = components,
                ["omitted_vfx_components"] = omitted, ["unsupported_vfx_values"] = ((JArray)source["unsupported_values"]).Count,
                ["world_allocated"] = false, ["entity_identities_allocated"] = 0,
                ["source_authority_closure_complete"] = false, ["import_admitted"] = false, ["mission_ready"] = false };
        }
        private static void DecodeStreamedSource(JObject source, string binaryDirectory,
            out Dictionary<Entity,Row> bySource, out HashSet<Entity> roots, out int omitted, out int components)
        {
            Need((int?)source["schema_version"] == 1 &&
                (string)source["kind"] == "S073IsolatedStreamedSourceWorldCapture" &&
                (string)source["scene_guid"] == "c00140f2e94a04c3084c8dcb0c18cbd0" &&
                (bool?)source["closure_complete"] == false && (bool?)source["mission_ready"] == false &&
                (bool?)source["live_s073_session_observed"] == false &&
                (bool?)source["physical_source_validated"] == false &&
                (bool?)source["scheduler_admitted"] == false && FocusedUnsupportedValuesAreUnconsumed(source),
                "Exact unqualified streamed source capture required");
            var inputRows = source["entities"] as JArray;
            var rootIds = source["root_entity_ids"] as JArray;
            Need(inputRows != null && inputRows.Count > 0 && inputRows.Count <= 32768 &&
                rootIds != null && rootIds.Count > 0 && rootIds.Count <= inputRows.Count &&
                (int?)source["building_faction_query_count"] == rootIds.Count,
                "Bounded complete streamed source row/root lists required");
            var codec = new ExactInitialValueCodec(binaryDirectory, new JObject());
            var assembly = typeof(GeneratedReferenceTemplateImporter).Assembly;
            bySource = new Dictionary<Entity,Row>(); omitted = 0; components = 0;
            foreach (JObject input in inputRows)
            {
                Entity id = ExactInitialValueCodec.EntityId((string)input["id"]);
                Need(id != Entity.Null && !bySource.ContainsKey(id) && input["entity_enabled"]?.Type == JTokenType.Boolean,
                    "Unique streamed entity identity and enablement required");
                var row = new Row { Source = id, Enabled = (bool)input["entity_enabled"] };
                var cells = input["components"] as JObject;
                Need(cells != null && cells.Count <= 256, "Bounded streamed component records required");
                foreach (var property in cells.Properties())
                {
                    var record = property.Value as JObject;
                    Need(record != null && (bool?)record["present"] == true &&
                        record["buffer"]?.Type == JTokenType.Boolean && record["value"] != null,
                        "Exact streamed component record required");
                    if (PresentationOnly.Contains(property.Name)) { omitted++; continue; }
                    Type type = ResolveCompiledComponentType(assembly, property.Name);
                    Need(type != null && type.IsValueType && (type.Namespace == "Game.Components" ||
                        type.Namespace == "Unity.Transforms" || type == typeof(Prefab) ||
                        type == typeof(Disabled) || type == typeof(LinkedEntityGroup) || type.FullName == "Unity.Entities.Simulate"),
                        "Unsupported compiled streamed type: " + property.Name);
                    bool buffer = (bool)record["buffer"];
                    Need(buffer ? typeof(IBufferElementData).IsAssignableFrom(type) || typeof(ICleanupBufferElementData).IsAssignableFrom(type)
                        : typeof(IComponentData).IsAssignableFrom(type) || typeof(ICleanupComponentData).IsAssignableFrom(type),
                        "Streamed component/buffer source kind mismatch: " + property.Name);
                    bool? enabled = record["enabled"]?.Type == JTokenType.Null ? null : (bool?)record["enabled"];
                    Need(enabled.HasValue == typeof(IEnableableComponent).IsAssignableFrom(type),
                        "Exact streamed enableable component facts required");
                    var cell = new Cell { Type = type, Buffer = buffer, Enabled = enabled };
                    if (buffer)
                    {
                        var envelope = record["value"] as JObject; var items = envelope?["items"] as JArray;
                        Need((string)envelope?["codec"] == "template-buffer-values-v1" &&
                            (string)envelope?["element_type"] == type.FullName && items != null && items.Count <= 65536,
                            "Exact bounded streamed buffer values required");
                        cell.Items = items.Select((item, i) => codec.Decode(item, type, id + "/" + property.Name + "/" + i)).ToArray();
                    }
                    else cell.Value = codec.Decode(record["value"], type, id + "/" + property.Name);
                    row.Cells.Add(cell); components++;
                }
                bySource.Add(id, row);
            }
            var graph = bySource;
            foreach (var row in graph.Values) foreach (var cell in row.Cells)
                foreach (var item in cell.Buffer ? cell.Items : new[] { cell.Value })
                    VisitEntities(item, cell.Type, entity => Need(entity == Entity.Null || graph.ContainsKey(entity),
                        "Unbound streamed source Entity: " + entity));
            roots = new HashSet<Entity>();
            foreach (var root in rootIds)
            {
                Entity id = ExactInitialValueCodec.EntityId((string)root);
                Need(roots.Add(id) && bySource.TryGetValue(id, out var row) &&
                    row.Cells.Any(cell => cell.Type == typeof(OperationMapBuildingComponent)) &&
                    row.Cells.Any(cell => cell.Type == typeof(Faction)), "Unique actual source building roots required");
            }
        }
        // Staged collision-only projection. The caller owns the fresh entities;
        // no captured Entity reference or Campaign gameplay component is copied.
        private static Dictionary<Entity,Entity> CreatePhysicalBlockers(EntityManager target,
            Dictionary<Entity,Row> rows, HashSet<Entity> roots)
        {
            var staged = new List<PhysicalBlockerInput>();
            foreach (var root in roots)
            {
                var row = rows[root];
                Need(row.Enabled && !row.Cells.Any(c => c.Type == typeof(Disabled) || c.Type == typeof(Prefab)),
                    "Active source geometry root required");
                Cell Required(Type t) { var c = row.Cells.SingleOrDefault(x => x.Type == t); Need(c != null && !c.Buffer && !c.Enabled.HasValue, "Missing physical blocker input: " + t.FullName); return c; }
                Cell Optional(Type t) { var c = row.Cells.SingleOrDefault(x => x.Type == t); Need(c == null || (!c.Buffer && !c.Enabled.HasValue), "Invalid physical blocker input: " + t.FullName); return c; }
                Required(typeof(StaticGridBlocker)); Required(typeof(RuntimeBuildingCombatTag)); Required(typeof(Faction));
                var origin = Required(typeof(RuntimeBuildingCombatInfo));
                var friendly = Optional(typeof(FriendlyPassGridBlocker));
                var opening = Optional(typeof(StaticGridBlockerSurfaceOpeningComponent));
                var health = (UnitHealth)Required(typeof(UnitHealth)).Value;
                var destroyed = row.Cells.SingleOrDefault(x => x.Type == typeof(OperationMapBuildingDestroyedComponent));
                Need(health.Max > 0 && health.Current == health.Max && destroyed != null && !destroyed.Buffer && destroyed.Enabled == false,
                    "Fresh authoring health and disabled destruction state required");
                var originalInfo = (RuntimeBuildingCombatInfo)origin.Value;
                staged.Add(new PhysicalBlockerInput { Source = root,
                    Grid = (UnitGrid)Required(typeof(UnitGrid)).Value,
                    Size = (GridBlockerSize)Required(typeof(GridBlockerSize)).Value,
                    HasOrigin = true, Origin = new RuntimeBuildingCombatInfo { RuntimeBuildingId = originalInfo.RuntimeBuildingId,
                        OwnerFactionId = 0, OriginCell = originalInfo.OriginCell, FootprintCells = originalInfo.FootprintCells,
                        IsWall = originalInfo.IsWall, IsGate = originalInfo.IsGate },
                    HasFriendly = friendly != null, Friendly = friendly == null ? default : (FriendlyPassGridBlocker)friendly.Value,
                    HasOpening = opening != null, Opening = opening == null ? default : (StaticGridBlockerSurfaceOpeningComponent)opening.Value,
                    Building = (OperationMapBuildingComponent)Required(typeof(OperationMapBuildingComponent)).Value,
                    Identity = (OperationMapBuildingIdentity)Required(typeof(OperationMapBuildingIdentity)).Value,
                    Footprint = (UnitFootprint)Required(typeof(UnitFootprint)).Value,
                    Health = new UnitHealth { Current = health.Max, Max = health.Max },
                    SourceKey = (UnitSourcePrefabKey)Required(typeof(UnitSourcePrefabKey)).Value,
                    Display = (UnitDisplayInfo)Required(typeof(UnitDisplayInfo)).Value,
                    Hitbox = (UnitSelectionHitbox)Required(typeof(UnitSelectionHitbox)).Value,
                    Transform = (LocalTransform)Required(typeof(LocalTransform)).Value,
                    LocalToWorld = (LocalToWorld)Required(typeof(LocalToWorld)).Value });
            }
            var created = new Dictionary<Entity,Entity>();
            foreach (var item in staged)
            {
                Entity e = target.CreateEntity(); created.Add(item.Source, e);
                target.AddComponentData(e, new StaticGridBlocker());
                target.AddComponentData(e, item.Grid); target.AddComponentData(e, item.Size);
                target.AddComponentData(e, item.Building); target.AddComponentData(e, item.Identity);
                target.AddComponentData(e, item.Footprint); target.AddComponentData(e, item.Health);
                target.AddComponentData(e, new Faction { Id = 0 }); target.AddComponentData(e, new RuntimeBuildingCombatTag());
                target.AddComponentData(e, item.SourceKey); target.AddComponentData(e, item.Display);
                target.AddComponentData(e, item.Hitbox); target.AddComponentData(e, item.Transform); target.AddComponentData(e, item.LocalToWorld);
                target.AddComponentData(e, new UnitGridInitialized());
                target.AddComponentData(e, new OperationMapBuildingDestroyedComponent());
                target.SetComponentEnabled<OperationMapBuildingDestroyedComponent>(e, false);
                if (item.HasOrigin) target.AddComponentData(e, item.Origin);
                if (item.HasFriendly) target.AddComponentData(e, item.Friendly);
                if (item.HasOpening) target.AddComponentData(e, item.Opening);
            }
            return created;
        }
        internal static int ProjectStreamedPhysicalBlockers(EntityManager target, JObject source, string binaryDirectory)
        {
            DecodeStreamedSource(source, binaryDirectory, out var rows, out var roots, out _, out _);
            Need(roots.Count == 4931, "Exact captured S073 source building count required");
            return CreatePhysicalBlockers(target, rows, roots).Count;
        }
        // Import only the fixed-map readback's non-building static geometry. Source
        // entities are never reused and no Campaign/gameplay component is copied.
        internal static List<FixedMapPhysicalBlocker> ProjectFixedMapNonBuildingBlockers(
            EntityManager target, JObject source, string binaryDirectory, string expectedOperationMapId)
        {
            Need(target != null && source != null &&
                (string)source["kind"] == "PinnedCampaignPhysicalInputExport" &&
                (bool?)source["missionReady"] == false && (bool?)source["missionInitialized"] == false &&
                (bool?)source["sourceFitCertified"] == false && (string)source["sourceSha256"] == "8a03d36fd1d4d59a180c65d82f1c708fd70e7c291f49d480c80629a80ec728c0",
                "Pinned non-ready Campaign physical source required");
            var domain = source["fixedMapReadDomain"] as JObject;
            Need(domain != null && (string)domain["kind"] == "OriginalCampaignFixedMapReadDomain" &&
                (string)domain["stage"] == "Collected" && (string)domain["operationMapId"] == expectedOperationMapId &&
                (string)domain["sourceDefinitionSha256"] == (string)source["sourceSha256"] &&
                (bool?)domain["actorOrMissionStateExported"] == false &&
                domain["structuralVersionBefore"]?.ToString() == domain["structuralVersionAfter"]?.ToString() &&
                (bool?)domain["gatewayReferenceUnchanged"] == true,
                "Original same-source fixed-map readback authority changed");
            var raw = domain["authoredNavigationBlockers"] as JArray;
            Need(raw != null && raw.Count == 4954, "Exact complete fixed-map navigation readback required");
            var codec = new ExactInitialValueCodec(binaryDirectory, new JObject());
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var nonBuildings = new List<Tuple<string, bool, JObject>>();
            int buildings = 0;
            foreach (JObject row in raw)
            {
                string id = (string)row["id"];
                ExactInitialValueCodec.EntityId(id);
                Need(ids.Add(id) && row["entity_enabled"]?.Type == JTokenType.Boolean,
                    "Unique enabled fixed-map source blocker identity required");
                var components = row["components"] as JObject;
                Need(components != null, "Fixed-map source blocker component map required");
                if (components[typeof(OperationMapBuildingComponent).FullName] != null)
                {
                    buildings++;
                    continue;
                }
                Need(components[typeof(RuntimeBuildingCombatInfo).FullName] == null,
                    "Non-building physical blocker cannot carry building origin state");
                nonBuildings.Add(Tuple.Create(id, (bool)row["entity_enabled"], components));
            }
            Need(buildings == 4931 && nonBuildings.Count == 23,
                "Fixed-map navigation census must contain 4,931 buildings and 23 physical-only blockers");
            var result = new List<FixedMapPhysicalBlocker>(23);
            foreach (var row in nonBuildings)
            {
                Need(row.Item2, "Enabled non-building static geometry row required");
                var components = row.Item3;
                object Decode(Type type)
                {
                    var record = components[type.FullName] as JObject;
                    Need(record != null && (bool?)record["present"] == true &&
                        (bool?)record["buffer"] == false && record["enabled"]?.Type == JTokenType.Null,
                        "Required non-building physical component record required: " + type.FullName);
                    return codec.Decode(record["value"], type, row.Item1 + "/" + type.FullName);
                }
                var grid = (UnitGrid)Decode(typeof(UnitGrid));
                var size = (GridBlockerSize)Decode(typeof(GridBlockerSize));
                Decode(typeof(StaticGridBlocker));
                Need(size.Size.x > 0 && size.Size.y > 0,
                    "Enabled non-building static geometry row required");
                Entity entity = target.CreateEntity();
                target.AddComponentData(entity, grid);
                target.AddComponentData(entity, size);
                target.AddComponentData(entity, new StaticGridBlocker());
                target.SetName(entity, "campaign-physical-only:" + row.Item1);
                result.Add(new FixedMapPhysicalBlocker { SourceId = row.Item1, Entity = entity,
                    Min = grid.Cell, Max = grid.Cell + size.Size });
            }
            return result;
        }
        // A disposable diagnostic manager only: no imported Entity, manager, or
        // payload escapes to the mission host. This checks serialization/remapping,
        // not live scene ownership, gameplay authority, or complete source closure.

#if WARLINE_SOURCE_S073
        internal static JObject ExerciseStreamedSourceRoundtrip(JObject source, string binaryDirectory,
            Game.Configs.GridAuthoringSceneConfigAsset staticOwnerGrid = null)
        {
            DecodeStreamedSource(source, binaryDirectory, out var rows, out var roots, out int omitted, out int components);
            foreach (var row in rows.Values)
                Need(row.Enabled != row.Cells.Any(cell => cell.Type == typeof(Disabled)),
                    "Captured entity enablement and Disabled tag disagree");
            var em = new EntityManager { LogicalAllocatorAdmitted = true };
            int next = checked(rows.Keys.Max(e => e.Index) + 1);
            em.AllocateLogicalIdentity = () => new Entity { Index = checked(next++), Version = 1 };
            var map = new Dictionary<Entity,Entity>();
            foreach (var row in rows.Values) map.Add(row.Source, em.CreateEntity());
            Need(map.Values.All(entity => !rows.ContainsKey(entity)), "Diagnostic identities overlap source namespace");
            var addBuffer = typeof(EntityManager).GetMethods().Single(m => m.Name == "AddBuffer" && m.IsGenericMethodDefinition);
            var getBuffer = typeof(EntityManager).GetMethods().Single(m => m.Name == "GetBuffer" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
            var setEnabled = typeof(EntityManager).GetMethod("SetComponentEnabled");
            var isEnabled = typeof(EntityManager).GetMethod("IsComponentEnabled");
            foreach (var row in rows.Values)
            {
                Entity target = map[row.Source];
                foreach (var cell in row.Cells)
                {
                    if (cell.Buffer)
                    {
                        object buffer = addBuffer.MakeGenericMethod(cell.Type).Invoke(em, new object[] { target });
                        var add = buffer.GetType().GetMethod("Add");
                        foreach (var item in cell.Items) add.Invoke(buffer, new[] { Remap(item, cell.Type, map) });
                    }
                    else em.AddComponent(target, cell.Type, Remap(cell.Value, cell.Type, map));
                    if (cell.Enabled.HasValue)
                        setEnabled.MakeGenericMethod(cell.Type).Invoke(em, new object[] { target, cell.Enabled.Value });
                }
            }
            int verified = 0;
            foreach (var row in rows.Values)
            {
                Entity target = map[row.Source];
                Need(em.Exists(target) && row.Enabled != em.HasComponent<Disabled>(target), "Diagnostic entity enablement changed");
                foreach (var cell in row.Cells)
                {
                    Need(em.HasComponent(target, cell.Type), "Diagnostic component missing: " + cell.Type.FullName);
                    if (cell.Buffer)
                    {
                        object buffer = getBuffer.MakeGenericMethod(cell.Type).Invoke(em, new object[] { target });
                        int length = (int)buffer.GetType().GetProperty("Length").GetValue(buffer);
                        Need(length == cell.Items.Length, "Diagnostic buffer length changed: " + cell.Type.FullName);
                        var indexer = buffer.GetType().GetProperty("Item");
                        for (int i = 0; i < length; i++)
                            Need(Equals(indexer.GetValue(buffer, new object[] { i }), Remap(cell.Items[i], cell.Type, map)),
                                "Diagnostic buffer value changed: " + cell.Type.FullName);
                    }
                    else Need(Equals(em.GetComponent(target, cell.Type), Remap(cell.Value, cell.Type, map)),
                        "Diagnostic component value changed: " + cell.Type.FullName);
                    if (cell.Enabled.HasValue)
                        Need((bool)isEnabled.MakeGenericMethod(cell.Type).Invoke(em, new object[] { target }) == cell.Enabled.Value,
                            "Diagnostic component enablement changed: " + cell.Type.FullName);
                    verified++;
                }
            }
            Need(verified == components && roots.All(root => em.Exists(map[root])), "Diagnostic graph accounting changed");
            // Exercise the unmodified gameplay owner on the isolated graph. The
            // result is comparison evidence, not permission to transfer this state
            // into an actual Skirmish session.
            SkirmishWorldSetup.NormalizeScenery(em);
            var normalized = new JArray();
            var normalizedByRoot = new Dictionary<Entity,JObject>();
            foreach (var root in roots)
            {
                Entity target = map[root];
                Need(!em.HasComponent<Prefab>(target) && !em.HasComponent<Disabled>(target) &&
                    em.HasComponent<SkirmishSceneryNormalized>(target) && em.GetComponentData<Faction>(target).Id == 0,
                    "Original scenery owner did not normalize an active source building");
                var building = em.GetComponentData<OperationMapBuildingComponent>(target);
                var normalizedRow = new JObject {
                    ["stable_id"] = building.StableId.ToString(),
                    ["source_global_object_id"] = building.SourceGlobalObjectId.ToString(),
                    ["placement_index"] = building.PlacementIndex,
                    ["faction"] = em.GetComponentData<Faction>(target).Id,
                    ["ai"] = em.HasComponent<AIControlledTag>(target),
                    ["combat_owner"] = em.HasComponent<RuntimeBuildingCombatInfo>(target)
                        ? (int?)em.GetComponentData<RuntimeBuildingCombatInfo>(target).OwnerFactionId : null,
                    ["storage_owner"] = em.HasComponent<BuildingResourceStorageComponent>(target)
                        ? (int?)em.GetComponentData<BuildingResourceStorageComponent>(target).OwnerFactionId : null,
                    ["health_current"] = em.HasComponent<UnitHealth>(target) ? (int?)em.GetComponentData<UnitHealth>(target).Current : null
                };
                normalized.Add(normalizedRow);
                normalizedByRoot.Add(root, normalizedRow);
            }
            JObject blockerEvidence = null;
            if (staticOwnerGrid != null)
            {
                Need(staticOwnerGrid.Width == 2048 && staticOwnerGrid.Height == 1024 && staticOwnerGrid.CellSize == 1f &&
                    staticOwnerGrid.Origin.x == 0 && staticOwnerGrid.Origin.y == 0 && staticOwnerGrid.Origin.z == 0,
                    "Exact S073 source grid frame required");
                Entity grid = em.CreateEntity();
                GeneratedGridSourceBaking.Bake(em, grid, staticOwnerGrid);
                GeneratedFreshGridStorage.Initialize(em, grid);
                var state = new SystemState { EntityManager = em, Enabled = true };
                var owner = new StaticGridBlockerUpdateSystem();
                state.Begin(); owner.OnCreate(ref state); state.End();
                state.Begin();
                try
                {
                    Need(state.Enabled && state.RequiredQueriesPresent, "Original static blocker prerequisites missing");
                    owner.OnUpdate(ref state);
                }
                finally { state.End(); }
                var blockers = em.GetComponentData<DynamicBlockerComponent>(grid);
                var bytes = new byte[checked(staticOwnerGrid.Width * staticOwnerGrid.Height)];
                long total = 0; int occupied = 0;
                for (int i = 0; i < bytes.Length; i++)
                {
                    int count = blockers.Counts[i];
                    Need(count >= 0 && blockers.Blocked.IsSet(i) == (count > 0), "Static blocker count/bit mismatch");
                    total += count;
                    if (count > 0) { bytes[i] = 1; occupied++; }
                }
                int withHistory = roots.Count(root => em.HasComponent<StaticBlockerPrevBounds>(map[root]));
                Need(withHistory == roots.Count && blockers.WriteInProgress == 0, "Static blocker owner did not commit all source roots");
                foreach (var root in roots)
                {
                    var history = em.GetComponentData<StaticBlockerPrevBounds>(map[root]);
                    normalizedByRoot[root]["static_history"] = new JObject {
                        ["min"] = new JArray(history.Min.x, history.Min.y), ["max"] = new JArray(history.Max.x, history.Max.y),
                        ["friendly_pass_faction"] = history.FriendlyPassFactionId,
                        ["bound_to_grid_epoch"] = history.AppliedGrid == grid && history.StorageEpoch == blockers.StorageEpoch
                    };
                }
                string maskSha;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                    maskSha = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                // Recreate only the physical inputs consumed by the original
                // blocker owner in a separate manager. No source entity, faction,
                // health, storage, production, or session component is copied.
                var geometry = new EntityManager { LogicalAllocatorAdmitted = true };
                int geometryNext = 1;
                geometry.AllocateLogicalIdentity = () => new Entity { Index = geometryNext++, Version = 1 };
                Entity geometryGrid = geometry.CreateEntity();
                GeneratedGridSourceBaking.Bake(geometry, geometryGrid, staticOwnerGrid);
                GeneratedFreshGridStorage.Initialize(geometry, geometryGrid);
                var geometryRoots = CreatePhysicalBlockers(geometry, rows, roots);
                var geometryState = new SystemState { EntityManager = geometry, Enabled = true };
                var geometryOwner = new StaticGridBlockerUpdateSystem();
                geometryState.Begin(); geometryOwner.OnCreate(ref geometryState); geometryState.End();
                geometryState.Begin();
                try { Need(geometryState.Enabled && geometryState.RequiredQueriesPresent, "Geometry blocker prerequisites missing"); geometryOwner.OnUpdate(ref geometryState); }
                finally { geometryState.End(); }
                var geometryBlockers = geometry.GetComponentData<DynamicBlockerComponent>(geometryGrid);
                int bitDifferences = 0, countDifferences = 0, friendlyDifferences = 0, historyDifferences = 0;
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (blockers.Blocked.IsSet(i) != geometryBlockers.Blocked.IsSet(i)) bitDifferences++;
                    if (blockers.Counts[i] != geometryBlockers.Counts[i]) countDifferences++;
                    if (blockers.FriendlyPassFactionIds[i] != geometryBlockers.FriendlyPassFactionIds[i]) friendlyDifferences++;
                }
                foreach (var root in roots)
                {
                    Entity target = geometryRoots[root];
                    Need(geometry.HasComponent<StaticBlockerPrevBounds>(target), "Geometry blocker history missing");
                    var a = em.GetComponentData<StaticBlockerPrevBounds>(map[root]);
                    var b = geometry.GetComponentData<StaticBlockerPrevBounds>(target);
                    if (a.Min.x != b.Min.x || a.Min.y != b.Min.y || a.Max.x != b.Max.x || a.Max.y != b.Max.y ||
                        a.FriendlyPassFactionId != b.FriendlyPassFactionId || b.AppliedGrid != geometryGrid ||
                        b.StorageEpoch != geometryBlockers.StorageEpoch) historyDifferences++;
                }
                blockerEvidence = new JObject { ["owner"] = "StaticGridBlockerUpdateSystem.OnUpdate",
                    ["source_roots_with_history"] = withHistory, ["grid_cells"] = bytes.Length,
                    ["blocked_cells"] = occupied, ["overlap_count_sum"] = total, ["blocked_mask_sha256"] = maskSha,
                    ["geometry_only_comparison"] = new JObject {
                        ["roots"] = geometryRoots.Count, ["blocked_bit_differences"] = bitDifferences,
                        ["count_differences"] = countDifferences, ["friendly_pass_differences"] = friendlyDifferences,
                        ["history_differences"] = historyDifferences,
                        ["campaign_gameplay_state_copied"] = false, ["mission_import_admitted"] = false },
                    ["scope"] = "Isolated source buildings and source grid; no live occupancy or source-fit claim" };
                RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(em, grid);
            }
            return new JObject { ["status"] = "IsolatedRoundtrip_NotMissionImport", ["roots"] = roots.Count,
                ["entities"] = rows.Count, ["components_verified"] = verified, ["omitted_vfx_components"] = omitted,
                ["normalization_owner"] = "SkirmishWorldSetup.NormalizeScenery", ["normalized_buildings"] = normalized,
                ["static_blocker_owner"] = blockerEvidence,
                ["source_authority_closure_complete"] = false, ["isolated_diagnostic_manager"] = true,
                ["source_ids_remapped"] = true, ["manager_exposed_to_mission"] = false,
                ["physical_source_validated"] = false, ["scheduler_admitted"] = false, ["mission_ready"] = false };
        }
#endif

        // Shares the exact importer decoder/type/closure checks; allocates no world identities.
        internal static JObject Preflight(JObject source,string binaryDirectory,IEnumerable<string> requiredSourceKeys){var rows=DecodeAndValidate(source,binaryDirectory,requiredSourceKeys,out var roots,out int omitted);return new JObject{["roots"]=roots.Count,["entities"]=rows.Count,["decoded_components"]=rows.Sum(r=>r.Cells.Count),["omitted_presentation_components"]=omitted,["unsupported_unconsumed_values"]=(source["unsupported_values"]as JArray)?.Count??0,["world_allocated"]=false,["entity_identities_allocated"]=0};}
        // S004's failed native capture is projected into a source-only artifact
        // with the reviewed GameObject references removed. This path validates
        // that artifact with the same compiled type/value/closure decoder used
        // by Import, but never creates an EntityManager or allocates identities.
        internal static JObject PreflightS004PresentationProjection(JObject projection,string binaryDirectory)
        {
            return PreflightS004ProjectionCore(projection,binaryDirectory,false);
        }
        // Separate entry for a passed scoped source/baker capture. The legacy
        // failed-capture entry above remains fail-closed and unchanged in scope.
        internal static JObject PreflightS004PassedScopedProjection(JObject projection,string binaryDirectory)
        {
            return PreflightS004ProjectionCore(projection,binaryDirectory,true);
        }
        private static JObject PreflightS004ProjectionCore(JObject projection,string binaryDirectory,bool passedScoped)
        {
            bool captureLineage=passedScoped
                ? (string)projection?["kind"]=="S004PassedScopedPrefabSourceOnlyProjection" &&
                  (string)projection?["capture"]?["capture_classification"]=="PassedScopedSourceCaptureOnly_NotHistoricalSourceEquality" &&
                  (bool?)projection?["historical_t11_provenance"]?["retained_separately"]==true
                : (string)projection?["kind"]=="S004FocusedPrefabSourceOnlyPresentationExcludedProjection" &&
                  (string)projection?["original_capture"]?["status"]=="Failed" &&
                  (bool?)projection?["original_capture"]?["raw_artifact_admitted"]==false;
            Need(projection!=null && (int?)projection["schema_version"]==1 &&
                captureLineage &&
                (string)projection["status"]=="PassedSourceOnlyProjection" &&
                (string)projection["source_authority"]=="ActualSourceAssetsAndNativeBakers_NotMissionState" &&
                Guid.TryParse((string)projection["template_namespace_id"],out var ns) && ns!=Guid.Empty &&
                (bool?)projection["geometry_exported"]==false && (bool?)projection["runtime_import_attempted"]==false &&
                (bool?)projection["runtime_replay_ready"]==false && (bool?)projection["mission_ready"]==false &&
                (bool?)projection["native_visual_readiness"]==false && (int?)projection["excluded_count"]==16 &&
                (projection["remaining_unsupported_values"] as JArray)?.Count==0,
                passedScoped?"Exact passed-scoped S004 source-only projection required":"Exact failed-capture S004 source-only projection required");
            var exclusions=projection["excluded_presentation_object_refs"] as JArray;
            Need(exclusions!=null&&exclusions.Count==16,"Complete reviewed S004 exclusion ledger required");
            var allowed=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal) {
                ["Game.Components.UnitAttackImpactVfxReference"]=new HashSet<string>(new[]{"Prefab"},StringComparer.Ordinal),
                ["Game.Components.UnitMuzzleFlashVfxReference"]=new HashSet<string>(new[]{"Prefab"},StringComparer.Ordinal),
                ["Game.Components.AirMissileLauncherVfxReferenceComponent"]=new HashSet<string>(new[]{"MissileVisualPrefab","LaunchFlashPrefab","LaunchSmokePrefab","MissileTrailPrefab","AirburstExplosionPrefab","AirTargetImpactPrefab","InterceptExplosionPrefab"},StringComparer.Ordinal)
            };
            const string reason="unity_object_ref_requires_bound_source_identity";
            const string objectRef="Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]";
            var counts=new Dictionary<string,int>(StringComparer.Ordinal);var paths=new HashSet<string>(StringComparer.Ordinal);
            var unsupported=new JArray();var entityRows=projection["entities"] as JArray;
            Need(entityRows!=null&&entityRows.Count>0,"Projected captured source entity closure required");
            var entities=entityRows.OfType<JObject>().ToDictionary(x=>{ExactInitialValueCodec.EntityId((string)x["id"]);return (string)x["id"];},StringComparer.Ordinal);
            foreach(JObject item in exclusions)
            {
                string path=(string)item["path"], component=(string)item["component"], field=(string)item["field"];
                Need(path!=null&&component!=null&&field!=null&&allowed.TryGetValue(component,out var fields)&&fields.Contains(field)&&
                    (string)item["reason"]==reason&&(string)item["type"]==objectRef&&paths.Add(path),
                    "Unknown or duplicate S004 presentation exclusion");
                var parts=path.Split('/');Need(parts.Length==3&&parts[1]==component&&parts[2]==field,"Exact S004 exclusion path required");
                string entityId=ExactInitialValueCodec.EntityId(parts[0]).ToString();
                Need(entities.TryGetValue(entityId,out var entity),"Excluded presentation reference must belong to a captured entity");
                var componentRecord=entity["components"]?[component] as JObject;
                Need(componentRecord!=null&&(bool?)componentRecord["present"]==true&&(bool?)componentRecord["buffer"]==false,
                    "Excluded reference must be a present scalar component");
                string fieldKey=component+"::"+field;
                Need(componentRecord["value"]?["fields"]?[fieldKey]==null,"Excluded ObjectRef field must not remain in the projection");
                counts[component+"/"+field]=counts.TryGetValue(component+"/"+field,out int count)?count+1:1;
                unsupported.Add(new JObject { ["path"]=path,["reason"]=reason,["type"]=objectRef });
            }
            Need(counts.TryGetValue("Game.Components.UnitAttackImpactVfxReference/Prefab",out int impact)&&impact==5&&
                counts.TryGetValue("Game.Components.UnitMuzzleFlashVfxReference/Prefab",out int muzzle)&&muzzle==4&&
                counts.TryGetValue("Game.Components.AirMissileLauncherVfxReferenceComponent/MissileVisualPrefab",out int missile)&&missile==1&&
                new[]{"LaunchFlashPrefab","LaunchSmokePrefab","MissileTrailPrefab","AirburstExplosionPrefab","AirTargetImpactPrefab","InterceptExplosionPrefab"}.All(f=>counts.TryGetValue("Game.Components.AirMissileLauncherVfxReferenceComponent/"+f,out int c)&&c==1),
                "S004 presentation exclusion field cardinalities changed");
            var source=(JObject)projection.DeepClone();
            source["kind"]="AuthoredSkirmishPrefabReferenceData";
            source["authority"]=(string)projection["source_authority"];
            source["unsupported_values"]=unsupported;
            var rows=DecodeAndValidate(source,binaryDirectory,((JArray)source["requested_prefab_keys"]).Values<string>(),out var roots,out int omitted);
            Need(omitted==10,"Compiled importer did not omit the exact reviewed S004 VFX components");
            return new JObject { ["status"]="Decoded_SourceOnly_NotImportAdmission",["roots"]=roots.Count,
                ["entities"]=rows.Count,["decoded_components"]=rows.Sum(r=>r.Cells.Count),
                ["excluded_vfx_references"]=16,["omitted_vfx_components"]=omitted,
                ["compiled_importer_type_and_value_preflight_passed"]=true,
                ["world_allocated"]=false,["entity_identities_allocated"]=0,["runtime_import_attempted"]=false,
                ["runtime_replay_ready"]=false,["mission_ready"]=false,["native_visual_readiness"]=false };
        }
        private static List<Row> DecodeAndValidate(JObject source,string binaryDirectory,IEnumerable<string> requiredSourceKeys,out JObject roots,out int omitted)
        {
            bool focused=(string)source["kind"]=="AuthoredSkirmishPrefabReferenceData";
            Need((int?)source["schema_version"]==1 && (focused||(string)source["kind"]=="AuthoredSkirmishReferenceData") &&
                (string)source["authority"]=="ActualSourceAssetsAndNativeBakers_NotMissionState","Actual source-asset bake profile required");
            var entities=source["entities"] as JArray;roots=source["opening_prefabs"] as JObject;var rootDomain=roots;
            Need(entities!=null&&entities.Count>0&&entities.Count<=4096&&roots!=null&&roots.Count>0&&roots.Count<=256,"Bounded complete template graph required");
            if(focused){var requested=source["requested_prefab_keys"]as JArray;Need(Guid.TryParse((string)source["template_namespace_id"],out var ns)&&ns!=Guid.Empty&&(bool?)source["geometry_exported"]==false&&FocusedUnsupportedValuesAreUnconsumed(source)&&requested!=null&&requested.Count==roots.Count&&requested.Values<string>().Distinct(StringComparer.Ordinal).Count()==requested.Count&&requested.Values<string>().All(k=>rootDomain[k]!=null),"Exact single captured prefab namespace and immutable root domain required");}
            var required=new HashSet<string>(requiredSourceKeys??throw new ArgumentNullException(nameof(requiredSourceKeys)),StringComparer.Ordinal);
            Need(required.Count>0&&required.All(k=>!string.IsNullOrEmpty(k)&&rootDomain[k]!=null),"All actual force template keys required");
            var codec=new ExactInitialValueCodec(binaryDirectory,new JObject());
            var assembly=typeof(GeneratedReferenceTemplateImporter).Assembly;
            var rows=new List<Row>();var bySource=new Dictionary<Entity,Row>();omitted=0;
            foreach(JObject input in entities)
            {
                Entity id=ExactInitialValueCodec.EntityId((string)input["id"]);
                Need(id!=Entity.Null&&!bySource.ContainsKey(id)&&input["entity_enabled"]?.Type==JTokenType.Boolean,"Unique complete asset entity identity/enablement required");
                var row=new Row {Source=id,Enabled=(bool)input["entity_enabled"]};
                var components=input["components"] as JObject;Need(components!=null&&components.Count<=256,"Complete bounded component map required");
                foreach(var property in components.Properties())
                {
                    var value=property.Value as JObject;Need(value!=null&&(bool?)value["present"]==true&&value["buffer"]?.Type==JTokenType.Boolean&&value["value"]!=null,"Exact present component record required");
                    if(PresentationOnly.Contains(property.Name)){omitted++;continue;}
                    Type type=ResolveCompiledComponentType(assembly,property.Name);
                    Need(type!=null&&type.IsValueType&&(type.Namespace=="Game.Components"||type.Namespace=="Unity.Transforms"||type==typeof(Prefab)||type==typeof(Disabled)||type==typeof(LinkedEntityGroup)||type.FullName=="Unity.Entities.Simulate"),"Unsupported compiled template type: "+property.Name);
                    bool buffer=(bool)value["buffer"];Need(buffer?typeof(IBufferElementData).IsAssignableFrom(type):typeof(IComponentData).IsAssignableFrom(type),"Component/buffer source kind mismatch: "+property.Name);
                    bool? enabled=value["enabled"]?.Type==JTokenType.Null?null:(bool?)value["enabled"];
                    Need(enabled.HasValue==typeof(IEnableableComponent).IsAssignableFrom(type),"Exact enableable component facts required");
                    var cell=new Cell {Type=type,Buffer=buffer,Enabled=enabled};
                    if(buffer)
                    {
                        var envelope=value["value"] as JObject;var items=envelope?["items"] as JArray;
                        Need((string)envelope?["codec"]=="template-buffer-values-v1"&&(string)envelope?["element_type"]==type.FullName&&items!=null&&items.Count<=4096,"Exact bounded ordered buffer values required");
                        cell.Items=items.Select((x,i)=>codec.Decode(x,type,id+"/"+property.Name+"/"+i)).ToArray();
                    }
                    else cell.Value=codec.Decode(value["value"],type,id+"/"+property.Name);
                    row.Cells.Add(cell);
                }
                Need(row.Cells.Any(c=>c.Type==typeof(Prefab)),"Every source template entity must be a real Prefab");
                Need(row.Enabled!=row.Cells.Any(c=>c.Type==typeof(Disabled)),"Captured entity enablement and Disabled tag disagree");rows.Add(row);bySource.Add(id,row);
            }
            // Closure and every decode/type/shape are checked BEFORE allocating any world identity.
            foreach(var row in rows)foreach(var cell in row.Cells)
                foreach(var item in cell.Buffer?cell.Items:new[]{cell.Value})VisitEntities(item,cell.Type,e=>Need(e==Entity.Null||bySource.ContainsKey(e),"Unbound external source template Entity: "+e));
            foreach(var root in roots.Properties())
            {
                Entity id=ExactInitialValueCodec.EntityId((string)root.Value);Need(bySource.ContainsKey(id),"Missing real opening prefab root");
                var keyCell=bySource[id].Cells.FirstOrDefault(c=>c.Type==typeof(UnitSourcePrefabKey));
                Need(keyCell!=null&&((UnitSourcePrefabKey)keyCell.Value).Value.ToString()==root.Name,"Actual source prefab key/root mismatch");
            }
            return rows;
        }
        internal static Result Import(EntityManager em,JObject source,string binaryDirectory,IEnumerable<string> requiredSourceKeys)
            => ImportCore(em, source, binaryDirectory, requiredSourceKeys, null, default, null, null);
        // Only these exact native UnityObjectRef GameObject fields are outside the logical gameplay read domain.
        private static bool FocusedUnsupportedValuesAreUnconsumed(JObject source){var records=source["unsupported_values"]as JArray;if(records==null)return false;var fields=new Dictionary<string,string[]>(StringComparer.Ordinal){["Game.Components.UnitAttackImpactVfxReference"]=new[]{"Prefab"},["Game.Components.UnitMuzzleFlashVfxReference"]=new[]{"Prefab"},["Game.Components.GroundMissileLauncherVfxReferenceComponent"]=new[]{"LauncherBackfirePrefab","RocketTrailPrefab","ImpactExplosionPrefab","ImpactSmokePrefab"},["Game.Components.AirMissileLauncherVfxReferenceComponent"]=new[]{"MissileVisualPrefab","LaunchFlashPrefab","LaunchSmokePrefab","MissileTrailPrefab","AirburstExplosionPrefab","AirTargetImpactPrefab","InterceptExplosionPrefab"}};foreach(JObject row in records){var path=((string)row["path"])?.Split('/');if(path==null||path.Length!=3||!fields.TryGetValue(path[1],out var allowed)||!allowed.Contains(path[2])||(string)row["reason"]!="unity_object_ref_requires_bound_source_identity"||!((string)row["type"]??"").StartsWith("Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,",StringComparison.Ordinal))return false;ExactInitialValueCodec.EntityId(path[0]);}return true;}
        private static Type ResolveCompiledComponentType(Assembly assembly,string fullName)
            => fullName==typeof(Disabled).FullName?typeof(Disabled):assembly.GetType(fullName,false,false);
        private static void VisitEntities(object value,Type type,Action<Entity> visit)
        {
            if(type==typeof(Entity)){visit((Entity)value);return;}
            if(!type.IsValueType||type.IsPrimitive||type.IsEnum)return;
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                if(!field.IsStatic&&(field.FieldType==typeof(Entity)||field.FieldType.IsValueType&&!field.FieldType.IsPrimitive&&!field.FieldType.IsEnum))VisitEntities(field.GetValue(value),field.FieldType,visit);
        }
        private static object Remap(object value,Type type,Dictionary<Entity,Entity> map)
        {
            if(type==typeof(Entity)){var e=(Entity)value;return e==Entity.Null?e:map[e];}
            if(!type.IsValueType||type.IsPrimitive||type.IsEnum)return value;
            // Reflection mutates a boxed struct in place. Copy the box so the
            // decoded source graph remains immutable across remapping passes.
            object copy=System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(value);
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                if(!field.IsStatic&&(field.FieldType==typeof(Entity)||field.FieldType.IsValueType&&!field.FieldType.IsPrimitive&&!field.FieldType.IsEnum))field.SetValue(copy,Remap(field.GetValue(copy),field.FieldType,map));
            return copy;
        }
    }
}
