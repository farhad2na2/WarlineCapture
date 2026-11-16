extern alias ActualCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Runtime.TransportPose;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Separate source-only S003 semantic/payment/delivery fixture. It advances the
// original owners through one spawn using a source-derived partial template and
// explicit synthetic grid; it claims no factory closure or provider membership.
internal static class SourceS003RifleSemanticQueueFixture
{
    const string RifleKey = "Unit_Chr_Soldier_Male_02_Alt_04";
    const string BarrackKey = "Building_Barrack";
    const string T17Path = "T Logs/SkirmishProduction/barrack-rifle-source-descriptor-20261103-T17/descriptor.json";
    const string T17Sha = "5e025367f128dde6b862fab29e80ff8acb8e8245a451f97e1e066ef89a42a6af";

    static void Need(bool value, string message)
    {
        if (!value) throw new InvalidDataException(message);
    }

    static byte[] Pinned(string path, string expected, int maximum)
    {
        if (!Path.IsPathRooted(path) || !Regex.IsMatch(expected ?? "", "^[0-9a-f]{64}$"))
            throw new InvalidDataException("Absolute source path and lowercase SHA-256 required");
        path = Path.GetFullPath(path);
        var info = new FileInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length <= 0 || info.Length > maximum)
            throw new InvalidDataException("Pinned regular bounded source file required: " + path);
        byte[] bytes = File.ReadAllBytes(path);
        using (var sha = SHA256.Create())
            if (BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() != expected)
                throw new InvalidDataException("Pinned source hash changed: " + path);
        return bytes;
    }

    static void VerifyDescriptorPins(string root, JObject descriptor)
    {
        var pins = descriptor["source_pins"] as JObject ?? throw new InvalidDataException("T17 authored source pins required");
        foreach (JProperty pin in pins.Properties())
        {
            string relative = pin.Name;
            if (Path.IsPathRooted(relative) || relative.Contains('\\') ||
                Array.Exists(relative.Split('/'), part => part.Length == 0 || part == "." || part == ".."))
                throw new InvalidDataException("Safe T17 relative source pin required");
            string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidDataException("T17 source pin escaped repository root");
            Pinned(full, (string)pin.Value, 8 * 1024 * 1024);
        }
    }

    static string Scalar(string text, string field)
    {
        var matches = Regex.Matches(text, "(?m)^  " + Regex.Escape(field) + ":\\s*(.*?)\\s*$");
        Need(matches.Count == 1, "Expected exactly one current authored gameplay field: " + field);
        return matches[0].Groups[1].Value;
    }

    static int Int(string text, string field)
    {
        string raw = Scalar(text, field);
        Need(Regex.IsMatch(raw, "^-?[0-9]+$"), "Exact serialized integer required: " + field);
        return int.Parse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    static bool Bool(string text, string field)
    {
        string raw = Scalar(text, field);
        Need(raw == "0" || raw == "1", "Exact serialized boolean required: " + field);
        return raw == "1";
    }

    static float Float(string text, string field)
    {
        string raw = Scalar(text, field);
        Need(Regex.IsMatch(raw, "^-?(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)(?:[eE][+-]?[0-9]+)?$"),
            "Exact serialized float required: " + field);
        return float.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    static string PrefabOverride(string text, string field, string targetFileId)
    {
        var matches = Regex.Matches(text,
            "(?m)^    - target: \\{fileID: " + Regex.Escape(targetFileId) +
            ", guid: 5680e7910ed3741beba6f07116b6b03c, type: 3\\}\\r?\\n      propertyPath: " + Regex.Escape(field) +
            "\\r?\\n      value: ([^\\r\\n]*)\\r?\\n      objectReference: \\{fileID: 0\\}$");
        Need(matches.Count == 1, "Exact current Rifle prefab root override required: " + field);
        return matches[0].Groups[1].Value.Trim();
    }

    static float RootOverrideFloat(string text, string field, float expected)
    {
        string value = PrefabOverride(text, field, "23759086217692639");
        Need(Regex.IsMatch(value, "^-?(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)$"), "Serialized root transform float required: " + field);
        float parsed = float.Parse(value, CultureInfo.InvariantCulture);
        Need(parsed == expected, "Current Rifle root transform differs from pinned fixture source: " + field);
        return parsed;
    }

    static Entity FindPlayerBank(EntityManager em)
    {
        using (var query = em.CreateEntityQuery(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent)))
        using (var entities = query.ToEntityArray(Allocator.Temp))
            foreach (Entity entity in entities)
                if (em.GetComponentData<FactionEconomy>(entity).FactionId == FactionIdentity.PlayerFactionId)
                    return entity;
        throw new InvalidDataException("Original SkirmishMaterialsService did not create the player bank");
    }

    static (SourceUnitPrefab Rifle, SourceUnitPrefab Barrack, SourceUnitPrefab Transport,
        BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata RifleProduction,
        BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata TransportProduction,
        JObject Descriptor, SourceUnitMoveAuthoringSource Movement, int RawMaterials, int Price, int GetterMaterials)
        ReadCurrentInputs(string root, string currentProjectionPath, string currentProjectionSha)
    {
        string descriptorPath = Path.Combine(root, T17Path.Replace('/', Path.DirectorySeparatorChar));
        JObject descriptor = JObject.Parse(Encoding.UTF8.GetString(Pinned(Path.GetFullPath(descriptorPath), T17Sha, 4 * 1024 * 1024)),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        Need((string)descriptor["kind"] == "BarrackRifleCurrentAuthoredSourceFacts" &&
             (string)descriptor["status"] == "PassedSourceOnlyDescriptor" &&
             (bool?)descriptor["scope"]?["native_factory_invoked"] == false &&
             (bool?)descriptor["scope"]?["runtime_admission"] == false &&
             (bool?)descriptor["scope"]?["mission_ready"] == false &&
             (bool?)descriptor["scope"]?["generic_witness_establishes_s002_s003_membership"] == false &&
             (bool?)descriptor["scope"]?["unity_asset_resolution"] == false,
            "T17 current source-only facts with explicit non-membership scope required");
        VerifyDescriptorPins(root, descriptor);
        JObject facts = (JObject)descriptor["facts"];
        JObject unit = (JObject)facts["unit"];
        JObject producer = (JObject)facts["producer"];
        JObject transport = (JObject)facts["transport_unit"];
        JObject riflePrefabFacts = (JObject)unit["prefab"];
        JObject barrackPrefabFacts = (JObject)producer["prefab"];
        JObject barrackPair = (JObject)descriptor["generic_authored_identity_witness"]?["barrack_pair"];
        JObject transportPrefabFacts = (JObject)transport["prefab"];
        Need((string)unit["key"] == RifleKey && (string)producer["key"] == BarrackKey &&
             (string)barrackPair["guid"] == (string)barrackPrefabFacts["guid"] &&
             (long?)barrackPair["local_id"] == 6603657636351658259L &&
             (string)descriptor["generic_authored_identity_witness"]?["scope"] == "generic SkirmishBaseAssault template only",
            "Exact T17 Rifle/Barrack identities and generic-only Barrack witness required");

        string unitConfigPath = (string)unit["config"]["path"];
        string unitConfig = Encoding.UTF8.GetString(Pinned(
            Path.Combine(root, unitConfigPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)descriptor["source_pins"]?[unitConfigPath], 1024 * 1024));
        int rawMaterials = Int(unitConfig, "materialsCost");
        int price = Int(unitConfig, "price");
        bool canRequest = Bool(unitConfig, "canRequest");
        Need(canRequest && price == (int)unit["credits_cost"] && rawMaterials == (int)unit["materials_cost"] && price > 0,
            "Current authored request getter fields disagree with pinned Rifle facts");
        int getterMaterials = rawMaterials > 0 ? rawMaterials : UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(price);

        var rifle = SourceUnitPrefab.FromAssetIdentity(RifleKey,
            (string)riflePrefabFacts["path"], (string)riflePrefabFacts["guid"], (long)riflePrefabFacts["local_id"]);
        var barrack = SourceUnitPrefab.FromAssetIdentity(BarrackKey,
            (string)barrackPrefabFacts["path"], (string)barrackPair["guid"], (long)barrackPair["local_id"]);
        var transportPrefab = SourceUnitPrefab.FromAssetIdentity((string)transport["key"],
            (string)transportPrefabFacts["path"], (string)transportPrefabFacts["guid"], (long)transportPrefabFacts["local_id"]);
        var rifleMetadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
            (float)unit["production_duration_seconds"], transportPrefab, (bool)unit["is_air_unit"],
            (float)unit["transport_arrival_seconds"], (float)unit["transport_hold_for_next_ready_seconds"],
            (int)unit["transport_max_concurrent"], (bool)unit["transport_requires_airport_runway"],
            (bool)unit["transport_uses_runway_landing"], new ActualCore::UnityEngine.Vector2Int((int)unit["footprint_cells"][0], (int)unit["footprint_cells"][1]));
        var transportMetadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
            (float)transport["production_duration_seconds"], null, (bool)transport["is_air_unit"],
            (float)transport["production_transport_arrival_seconds"], (float)transport["production_transport_hold_for_next_ready_seconds"],
            (int)transport["production_transport_max_concurrent"], (bool)transport["production_transport_requires_airport_runway"],
            (bool)transport["production_transport_uses_runway_landing"], new ActualCore::UnityEngine.Vector2Int((int)transport["footprint_cells"][0], (int)transport["footprint_cells"][1]));
        var movement = SourceUnitMoveAuthoringSource.LoadCurrentProjection(
            new JObject { ["path"] = Path.GetFullPath(currentProjectionPath), ["sha256"] = currentProjectionSha },
            RifleKey, root, (path, sha, maximum) => Pinned(path, sha, maximum));
        Need(movement.ConfigGuid == (string)unit["config"]?["guid"] && movement.RootKey == RifleKey,
            "Current movement/combat/health/attack projection must bind the T17 Rifle config identity");
        return (rifle, barrack, transportPrefab, rifleMetadata, transportMetadata, descriptor, movement, rawMaterials, price, getterMaterials);
    }

    static SourceUnitPrefab CreatePartialRifleTemplate(EntityManager em, string root, JObject descriptor,
        SourceUnitMoveAuthoringSource movement)
    {
        JObject unit = (JObject)descriptor["facts"]?["unit"] ?? throw new InvalidDataException("T17 Rifle source facts required");
        JObject prefabFacts = (JObject)unit["prefab"] ?? throw new InvalidDataException("T17 Rifle prefab identity required");
        JObject pins = (JObject)descriptor["source_pins"] ?? throw new InvalidDataException("T17 source pins required");
        string prefabPath = (string)prefabFacts["path"];
        string prefabText = Encoding.UTF8.GetString(Pinned(Path.Combine(root, prefabPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[prefabPath], 2 * 1024 * 1024));
        string basePath = "Assets/Game/Prefabs/Characters/Unit.prefab";
        string baseText = Encoding.UTF8.GetString(Pinned(Path.Combine(root, basePath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[basePath], 2 * 1024 * 1024));
        string baseMetaPath = basePath + ".meta";
        string baseMeta = Encoding.UTF8.GetString(Pinned(Path.Combine(root, baseMetaPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[baseMetaPath], 128 * 1024));
        string baseGuid = Regex.Match(baseMeta, "(?m)^guid: ([0-9a-f]{32})$").Groups[1].Value;
        Need(baseGuid.Length == 32 && Regex.Matches(baseMeta, "(?m)^guid: [0-9a-f]{32}$").Count == 1 &&
             (string)prefabFacts["guid"] == "ff73c8b7cbf844778916da330e9cd3e3" &&
             (long?)prefabFacts["local_id"] == 3282182154612916099L,
            "Exact pinned Rifle prefab GUID/fileID and base prefab GUID required");
        var rootInstances = Regex.Matches(prefabText,
            "(?ms)^--- !u!1001 &[^\\r\\n]+\\r?\\n(.*?)(?=^--- !u!|\\z)")
            .Cast<Match>()
            .Where(row => Regex.IsMatch(row.Groups[1].Value, "(?m)^    m_TransformParent: \\{fileID: 0\\}$") &&
                          Regex.IsMatch(row.Groups[1].Value, "(?m)^  m_SourcePrefab: \\{fileID: 100100000, guid: " + baseGuid + ", type: 3\\}$"))
            .ToArray();
        Match baseRootObject = Regex.Match(baseText,
            "(?ms)^--- !u!1 &2254344950327853479\\r?\\n(.*?)(?=^--- !u!|\\z)");
        Match baseRootTransform = Regex.Match(baseText,
            "(?ms)^--- !u!4 &23759086217692639\\r?\\n(.*?)(?=^--- !u!|\\z)");
        Need(rootInstances.Length == 1 && baseRootObject.Success && baseRootTransform.Success &&
             Regex.IsMatch(baseRootObject.Groups[1].Value, "(?m)^  m_Name: Unit$") &&
             Regex.IsMatch(baseRootObject.Groups[1].Value, "(?m)^  - component: \\{fileID: 23759086217692639\\}$") &&
             Regex.IsMatch(baseRootTransform.Groups[1].Value, "(?m)^  m_GameObject: \\{fileID: 2254344950327853479\\}$") &&
             Regex.IsMatch(baseRootTransform.Groups[1].Value, "(?m)^  m_LocalScale: \\{x: 1, y: 1, z: 1\\}$"),
            "Pinned Rifle variant must retain the exact Unit root and scale");
        string name = PrefabOverride(prefabText, "m_Name", "2254344950327853479");
        Need(name == RifleKey, "Current Rifle root name must match its pinned source key");
        float px = RootOverrideFloat(prefabText, "m_LocalPosition.x", 0f);
        float py = RootOverrideFloat(prefabText, "m_LocalPosition.y", 0f);
        float pz = RootOverrideFloat(prefabText, "m_LocalPosition.z", 0f);
        float qx = RootOverrideFloat(prefabText, "m_LocalRotation.x", 0f);
        float qy = RootOverrideFloat(prefabText, "m_LocalRotation.y", 0f);
        float qz = RootOverrideFloat(prefabText, "m_LocalRotation.z", 0f);
        float qw = RootOverrideFloat(prefabText, "m_LocalRotation.w", 1f);
        string configPath = (string)unit["config"]?["path"];
        string config = Encoding.UTF8.GetString(Pinned(Path.Combine(root, configPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[configPath], 1024 * 1024));
        UnitMoveAuthoringProjection p = movement.Projection;
        Need(p.HasMovementSlice && p.Footprint.Size.x == 1 && p.Footprint.Size.y == 1 &&
             p.MovementBehavior.AllowIdleWander == 1 && p.MovementBehavior.UsesVehicleMotion == 0,
            "Current pinned Rifle movement projection required for partial template");
        float3 position = new float3(px, py, pz);
        Entity entity = em.CreateEntity();
        em.AddComponent<Prefab>(entity);
        em.AddComponentData(entity, LocalTransform.FromPositionRotationScale(position, new quaternion(qx, qy, qz, qw), 1f));
        em.AddComponentData(entity, new UnitGrid { Cell = int2.zero });
        em.AddComponentData(entity, new UnitMove { Speed = p.Speed, WalkSpeed = p.WalkSpeed,
            RoadSpeedMultiplier = p.RoadSpeedMultiplier, ArriveDistance = p.ArriveDistance });
        em.AddComponentData(entity, p.Footprint);
        em.AddComponentData(entity, p.MovementBehavior);
        em.AddComponentData(entity, p.VehicleMovement);
        em.AddComponentData(entity, p.VehicleKinematics);
        var surface = UnitSurfaceAuthoringProjection.FromAuthoredHeight(py);
        em.AddComponentData(entity, new UnitSurfaceComponent { SurfaceId = surface.SurfaceId, LayerId = surface.LayerId,
            LastSampledHeight = surface.LastSampledHeight, LastSampledNormal = surface.LastSampledNormal,
            HasSurface = surface.HasSurface, IsGrounded = surface.IsGrounded });
        em.AddComponentData(entity, new UnitGroundOffsetComponent { Value = Float(config, "groundOffset") });
        // The production initializer overwrites this fixture carrier from its actual producer owner.
        em.AddComponentData(entity, new Faction { Id = FactionIdentity.NeutralFactionId });
        em.AddComponentData(entity, movement.Combat);
        em.AddComponentData(entity, movement.Health);
        em.AddComponentData(entity, movement.Attack);
        em.AddComponentData(entity, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
        em.AddComponentData(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
        em.AddComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
        em.AddComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(name) });
        em.AddComponentData(entity, new UnitPrevWorldPos { Value = position });
        em.AddComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.AddComponentData(entity, UnitAnimationAuthoringProjection.FromConfiguredValues(
            Float(config, "idleDelayMinSeconds"), Float(config, "idleDelayMaxSeconds"),
            Float(config, "idleWanderDistanceMin"), Float(config, "idleWanderDistanceMax"),
            Float(config, "attackAnimationSeconds"), Float(config, "deathAnimationSeconds")));
        em.AddComponentData(entity, UnitIdleWanderAuthoringProjection.FromAuthoredIdentity(
            p.Footprint.Size.x, p.Footprint.Size.y, name));
        return new SourceUnitPrefab(RifleKey, entity, prefabPath, (string)prefabFacts["guid"], (long)prefabFacts["local_id"]);
    }

    static (Entity Entity, int Cells) CreateFixtureDeliveryGrid(EntityManager em)
    {
        Entity grid = em.CreateEntity();
        const int width = 64, height = 64, count = width * height;
        var config = new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = new float3(-40f, 0f, -40f) };
        em.AddComponentData(grid, config);
        var walkable = em.AddBuffer<GridWalkable>(grid);
        var roads = em.AddBuffer<GridRoad>(grid);
        var sidewalks = em.AddBuffer<GridRoadSidewalk>(grid);
        var dirt = em.AddBuffer<GridRoadDirt>(grid);
        for (int i = 0; i < count; i++) { walkable.Add(new GridWalkable { Value = 1 }); roads.Add(default); sidewalks.Add(default); dirt.Add(default); }
        em.AddComponentData(grid, new DynamicBlockerComponent { GridSize = count,
            Counts = new NativeArray<int>(count, Allocator.Persistent), Blocked = new NativeBitArray(new bool[count]),
            FriendlyPassFactionIds = new NativeArray<byte>(count, Allocator.Persistent), StorageEpoch = 1,
            AppliedStorageEpoch = 1, AppliedRevision = 1, AppliedGrid = config });
        em.AddComponentData(grid, new DynamicOccupancyComponent { GridSize = count, Occupied = new NativeBitArray(new bool[count]) });
        em.AddComponentData(grid, new PathPoolComponent { Cells = new NativeList<int2>(64, Allocator.Persistent) });
        return (grid, count);
    }

    static JObject ExerciseOriginalMovementOwners(EntityManager em, Entity session, Entity gridEntity, Entity actor,
        uint receiptId, int gridWidth, int gridHeight)
    {
        Need(em.HasComponent<SkirmishAttemptOwnedComponent>(actor) && em.HasComponent<SkirmishUnitRoleComponent>(actor) &&
             em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor) && em.HasComponent<UnitMove>(actor) &&
             em.HasComponent<UnitFootprint>(actor) && em.HasComponent<LocalTransform>(actor),
            "Actual delivered S003 Rifle actor lacks original ownership, role, group, or movement components");
        var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
        var role = em.GetComponentData<SkirmishUnitRoleComponent>(actor);
        var membership = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor);
        Need(owned.ReservationId == receiptId && owned.IsStructure == 0 && owned.FactionId == FactionIdentity.PlayerFactionId &&
             owned.SessionId.Equals(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId) &&
             role.Role == SkirmishRoleKind.Rifle && membership.GroupId != 0,
            "Original production owners must attach the spawned actor to this paid Rifle receipt and group");
        Need(SkirmishArmyGroupSystem.TryGet(em, session, membership.GroupId, out var group) &&
             group.FactionId == FactionIdentity.PlayerFactionId && group.Role == SkirmishRoleKind.Rifle && group.AliveCount == 1,
            "Original group owner must publish exactly this live Rifle group before command");
        Need(SkirmishExpandedPresentedOrders.TryPresentedGroup(em, session, membership.GroupId) &&
             em.HasComponent<SelectedUnitTag>(actor),
            "Original presented-group owner did not select the produced Rifle group");

        Entity runtimeState = em.CreateEntity();
        em.AddComponentData(runtimeState, new RuntimeGameplayStateComponent { PlayRequested = 1, SimulationActive = 1 });
        var deferred = new OwnedEndSimulationBuffers();
        em.AddComponentData(em.CreateEntity(), new EndSimulationEntityCommandBufferSystem.Singleton { Owner = deferred });
        UnitMoveOrderRequestSystem.EnsureQueueEntity(em);
        em.WorldUnmanaged.Time = new TimeData { ElapsedTime = 0d, DeltaTime = 1f / 60f };
        using (var owners = new OriginalGroundOwners(em, generatedFresh: true))
        {
            Need(owners.OccupancyPhase(), "Original occupancy owner did not run in explicit fixture activation");
            UnitGrid startCell = em.GetComponentData<UnitGrid>(actor);
            LocalTransform startTransform = em.GetComponentData<LocalTransform>(actor);
            UnitFootprint footprint = em.GetComponentData<UnitFootprint>(actor);
            DynamicBlockerComponent blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
            int2 goal = default;
            bool hasGoal = false;
            const int fixtureMoveDistanceCells = 6;
            foreach (int2 direction in new[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) })
            {
                int2 candidate = startCell.Cell + direction * fixtureMoveDistanceCells;
                if (candidate.x < 0 || candidate.y < 0 || candidate.x + footprint.Size.x > gridWidth ||
                    candidate.y + footprint.Size.y > gridHeight) continue;
                bool free = true;
                for (int step = 1; step <= fixtureMoveDistanceCells && free; step++)
                for (int y = startCell.Cell.y + direction.y * step; y < startCell.Cell.y + direction.y * step + footprint.Size.y && free; y++)
                for (int x = startCell.Cell.x + direction.x * step; x < startCell.Cell.x + direction.x * step + footprint.Size.x; x++)
                {
                    int index = y * gridWidth + x;
                    if (walkable[index].Value == 0 || blockers.Counts[index] != 0 || blockers.Blocked.IsSet(index) || occupancy.Occupied.IsSet(index))
                    { free = false; break; }
                }
                if (free) { goal = candidate; hasGoal = true; break; }
            }
            Need(hasGoal, "Original occupancy/blocker owners found no six-cell clear move goal in the synthetic grid");
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            float3 destination = GridUtils.CellToWorldCenter(grid, goal);
            bool moveAccepted = SkirmishArmyCommandService.TryMove(em, session, destination, out SkirmishCommandDecision decision);
            Need(moveAccepted && decision.Accepted && decision.IssuedCount == 1 &&
                 em.HasComponent<SkirmishMoveIntentComponent>(actor) && em.GetComponentData<SkirmishMoveIntentComponent>(actor).Active != 0 &&
                 em.HasComponent<UnitPathRequest>(actor),
                "Original grouped Move command did not issue one active receipt-owned path request: " + JObject.FromObject(decision) +
                ",active_intent=" + (em.HasComponent<SkirmishMoveIntentComponent>(actor) && em.GetComponentData<SkirmishMoveIntentComponent>(actor).Active != 0) +
                ",path_request=" + em.HasComponent<UnitPathRequest>(actor));

            bool pathApplied = false;
            for (int tick = 0; tick < 12 && !pathApplied; tick++)
            {
                em.WorldUnmanaged.Time = new TimeData { ElapsedTime = tick * (1d / 60d), DeltaTime = 1f / 60f };
                Need(owners.PathPhase(), "Original UnitPathfindingSystem did not run in explicit fixture activation");
                pathApplied = em.HasComponent<UnitPathFollow>(actor) && em.HasComponent<UnitPathRange>(actor);
            }
            Need(pathApplied, "Original UnitPathfindingSystem did not apply a path to the accepted move request");
            UnitPathFollow appliedFollow = em.GetComponentData<UnitPathFollow>(actor);
            UnitPathRange pathRange = em.GetComponentData<UnitPathRange>(actor);
            NativeList<int2> pathPool = em.GetComponentData<PathPoolComponent>(gridEntity).Cells;
            Need(pathRange.Length > 0 && pathRange.Start >= 0 && (long)pathRange.Start + pathRange.Length <= pathPool.Length,
                "Original path owner published an invalid range into the current grid path pool");
            var pathCells = new JArray();
            for (int i = 0; i < pathRange.Length; i++)
            {
                int2 cell = pathPool[pathRange.Start + i];
                pathCells.Add(new JArray(cell.x, cell.y));
            }

            bool moved = false;
            bool movementOwnerRan = false;
            bool orderCompleted = false;
            int movementTicks = 0;
            string stopReason = "bounded_update_limit";
            var observations = new JArray();
            const int maximumMovementTicks = 600;
            for (int tick = 0; tick < maximumMovementTicks; tick++)
            {
                em.WorldUnmanaged.Time = new TimeData { ElapsedTime = (12 + tick) * (1d / 60d), DeltaTime = 1f / 60f };
                bool occupancyRan = owners.OccupancyPhase();
                Need(occupancyRan, "Original occupancy owner did not run during movement");
                UnitGrid beforeCell = em.GetComponentData<UnitGrid>(actor);
                LocalTransform before = em.GetComponentData<LocalTransform>(actor);
                bool ran = owners.MovementPhase();
                Need(ran, "Original UnitGridMovementSystem did not run in explicit fixture activation");
                movementOwnerRan |= ran;
                owners.EndSimulationPhase();
                movementTicks++;
                UnitGrid afterCell = em.GetComponentData<UnitGrid>(actor);
                LocalTransform after = em.GetComponentData<LocalTransform>(actor);
                bool follow = em.HasComponent<UnitPathFollow>(actor), range = em.HasComponent<UnitPathRange>(actor);
                bool target = em.HasComponent<UnitTarget>(actor), manual = em.HasComponent<ManualMoveOrderTag>(actor);
                moved |= !afterCell.Cell.Equals(startCell.Cell) || !after.Position.Equals(startTransform.Position);
                observations.Add(new JObject {
                    ["tick"] = tick, ["occupancy_owner_ran"] = occupancyRan, ["movement_owner_ran"] = ran,
                    ["cell_before"] = new JArray(beforeCell.Cell.x, beforeCell.Cell.y),
                    ["cell_after"] = new JArray(afterCell.Cell.x, afterCell.Cell.y),
                    ["position_before"] = new JArray(before.Position.x, before.Position.y, before.Position.z),
                    ["position_after"] = new JArray(after.Position.x, after.Position.y, after.Position.z),
                    ["path_follow_after"] = follow, ["path_range_after"] = range,
                    ["unit_target_after"] = target, ["manual_order_after"] = manual,
                    ["end_simulation_playback_invoked"] = true
                });
                if (!follow && !range && !target && !manual)
                { orderCompleted = true; stopReason = "original_path_and_manual_order_cleanup"; break; }
                if (!follow || !range) { stopReason = "path_cleared_before_order_termination"; break; }
            }
            UnitGrid endCell = em.GetComponentData<UnitGrid>(actor);
            LocalTransform endTransform = em.GetComponentData<LocalTransform>(actor);
            Need(movementOwnerRan && moved && orderCompleted &&
                 stopReason == "original_path_and_manual_order_cleanup" && endCell.Cell.Equals(goal),
                "Original UnitGridMovementSystem did not complete the spawned Rifle move at its goal within bounded updates: start=" + startCell.Cell +
                ",goal=" + goal + ",end=" + endCell.Cell + ",order_completed=" + orderCompleted +
                ",stop_reason=" + stopReason + ",path=" + pathCells.ToString(Newtonsoft.Json.Formatting.None));
            return new JObject {
                ["kind"] = "OriginalSourceGroupedMoveAndMovementOwners",
                ["group_id"] = membership.GroupId,
                ["receipt_id"] = receiptId,
                ["selected_by_original_presented_group_owner"] = true,
                ["move_api"] = "SkirmishArmyCommandService.TryMove",
                ["move_accepted"] = decision.Accepted,
                ["issued_count"] = decision.IssuedCount,
                ["occupancy_derived_goal"] = new JArray(goal.x, goal.y),
                ["start_cell"] = new JArray(startCell.Cell.x, startCell.Cell.y),
                ["end_cell"] = new JArray(endCell.Cell.x, endCell.Cell.y),
                ["start_position"] = new JArray(startTransform.Position.x, startTransform.Position.y, startTransform.Position.z),
                ["end_position"] = new JArray(endTransform.Position.x, endTransform.Position.y, endTransform.Position.z),
                ["path_cells"] = pathCells,
                ["path_follow_index_after_apply"] = appliedFollow.PathIndex,
                ["movement_owner"] = "OriginalGroundOwners.PathPhase/OccupancyPhase/MovementPhase",
                ["movement_owner_ran"] = movementOwnerRan,
                ["movement_observed"] = moved,
                ["movement_ticks"] = movementTicks,
                ["order_completed_by_original_cleanup"] = orderCompleted,
                ["stop_reason"] = stopReason,
                ["owner_observations"] = observations,
                ["fixture_activation"] = true,
                ["factory_closure"] = false, ["world_admission"] = false, ["mission_readiness"] = false
            };
        }
    }

    internal static JObject Run(string root, string output, string currentProjectionPath, string currentProjectionSha)
    {
        root = Path.GetFullPath(root);
        Directory.CreateDirectory(output);
        var setup = S003SingleRifleAuthorityControl.CompileCurrentStandardSetup(root);
        var input = ReadCurrentInputs(root, currentProjectionPath, currentProjectionSha);
        var setupRoles = setup.RoleOverlays.Where(row => row.RoleId == "role.rifle").ToArray();
        Need(setup.CatalogId == "S003" && setup.Seed == 104732 && setup.SetupHash == 2394754492u &&
             setupRoles.Length == 1 && setupRoles[0].RoleKind == SkirmishRoleKind.Rifle &&
             setupRoles[0].Producer == SkirmishProducerKind.Barracks && setupRoles[0].SquadMembers == 4 &&
             setupRoles[0].MaterialsCost == 80,
            "Actual original compiler S003 setup and Rifle overlay must match the frozen T6 facts");

        int nextEntity = 1;
        var em = new EntityManager { LogicalAllocatorAdmitted = true,
            AllocateLogicalIdentity = () => new Entity { Index = nextEntity++, Version = 1 } };
        var rifle = CreatePartialRifleTemplate(em, root, input.Descriptor, input.Movement);
        var barrack = input.Barrack;
        var transport = input.Transport;
        var map = new SourceCampGameplayIdentityMap(new[] { rifle }, new[] {
            new SourceGameplayPrefabBinding(rifle, rifle.SourceAssetGuid + ":" + rifle.SourceAssetLocalId.ToString(CultureInfo.InvariantCulture))
        });

        const int producerId = 3201;
        string sessionText = "s003-rifle-queue-fixture";
        Entity session = em.CreateEntity();
        em.AddComponentData(session, new SkirmishExpandedSessionComponent {
            SessionId = new FixedString64Bytes(sessionText), CatalogId = new FixedString64Bytes(setup.CatalogId),
            DefinitionId = new FixedString64Bytes(setup.DefinitionId), ContentVersion = setup.ContentVersion,
            SetupHash = setup.SetupHash, Seed = setup.Seed, SizeId = setup.SizeId, DifficultyId = setup.DifficultyId,
            ObjectiveKind = setup.ObjectiveKind, Phase = SkirmishSessionPhase.Playing,
            InitializationComplete = 1, SpawnComplete = 1
        });
        em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
        var roleMap = setup.RoleOverlays.ToDictionary(row => SkirmishRoleCatalogConfig.RuntimePrefabKey(row.RoleKind),
            row => row, StringComparer.Ordinal);
        em.AddComponentObject(session, new SkirmishProductionCatalogRecord {
            Catalog = new SkirmishProductionCatalog(setup, roleMap, true)
        });
        em.AddComponentData(session, new SkirmishCapacityComponent {
            FactionId = FactionIdentity.PlayerFactionId, InfantryCap = setup.InfantryCapEach,
            GroundCap = setup.GroundCapEach, AirCap = setup.TacticalAirCapEach, SupplyCap = setup.SupplyCapEach,
            NextReservationId = 1
        });
        em.AddComponentData(session, new SkirmishEconomyStockComponent {
            FactionId = FactionIdentity.PlayerFactionId, Materials = setup.MaterialsEach,
            Oil = setup.OilEach, Fuel = setup.UsableFuelEach, MaterialsCapacity = setup.MaterialsCapacityEach,
            OilCapacity = setup.OilCapacityEach, FuelCapacity = setup.FuelCapacityEach
        });
        em.AddBuffer<SkirmishProductionReservation>(session);
        SkirmishMaterialsService.Initialize(em, session, setup);
        Entity bank = FindPlayerBank(em);

        Entity producerEntity = em.CreateEntity();
        em.AddComponentData(producerEntity, new SkirmishAttemptOwnedComponent {
            SessionId = new FixedString64Bytes(sessionText), StableObjectId = new FixedString64Bytes("fixture.s003.barrack"),
            FactionId = FactionIdentity.PlayerFactionId, IsStructure = 1
        });
        em.AddComponentData(producerEntity, new SkirmishStructureIdentityComponent {
            StructureId = new FixedString64Bytes(SkirmishStructureIds.Barracks), Producer = SkirmishProducerKind.Barracks
        });
        JObject producerFacts = (JObject)input.Descriptor["facts"]["producer"];
        int producerHealth = (int)producerFacts["max_health"];
        em.AddComponentData(producerEntity, new UnitHealth { Current = producerHealth, Max = producerHealth });
        var producerDefinition = new RuntimeDefinition {
            SourceMetadataComplete = false, Prefab = barrack,
            ProductionSlots = new List<RuntimeDefinition.ProductionSlotDefinition> {
                new RuntimeDefinition.ProductionSlotDefinition {
                    SpawnUnitPrefab = rifle, SpawnUnitSourceKey = new FixedString64Bytes(RifleKey), Quantity = 4
                }
            }
        };
        var authority = SourceProducerRecipeAuthority.ReadExplicitS003SingleRifleFixture(
            root, producerDefinition, barrack, rifle, transport, setup);
        producerDefinition.SourceRecipeAuthority = authority;
        Need(authority.IsS003Fixture && authority.MatchesSetup(setup) && !producerDefinition.SourceMetadataComplete &&
             barrack.IsAssetIdentityOnly && !rifle.IsAssetIdentityOnly && em.Exists(rifle.Template) && transport.IsAssetIdentityOnly,
            "Exact S003 fixture capability, current source-derived Rifle template, and identity-only Barrack/transport tokens required");

        var building = new RuntimeBuildingEntity {
            Id = producerId, CombatEntity = producerEntity, HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId, InitialProducerCollectionsKnown = true,
            PendingProductions = new List<SourcePendingProduction>(), ProducedUnits = new List<Entity>(),
            Definition = producerDefinition
        };
        var buildings = new Dictionary<int, RuntimeBuildingEntity> { [producerId] = building };
        RuntimeDefinition[] definitions = { producerDefinition };
        var definitionsByPrefab = new Dictionary<SourceUnitPrefab, RuntimeDefinition> { [barrack] = producerDefinition };
        SourceUnitPrefab[] units = { rifle };
        var byKey = new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal) { [RifleKey.ToLowerInvariant()] = rifle };
        var recipe = new BuildingProductionRecipe {
            ProducerPrefab = barrack, UnitPrefab = rifle, Quantity = setupRoles[0].SquadMembers,
            MaterialsCost = setupRoles[0].MaterialsCost, CreditsCost = 0, UseProducerGroundExit = false
        };
        var recipes = new Dictionary<SourceUnitPrefab, BuildingProductionRecipe> { [rifle] = recipe };
        var gameplayMetadata = new SourceUnitGameplayRequestMetadataStore(units, units, new[] {
            new SourceUnitGameplayRequestMetadata(true, input.GetterMaterials, input.Price)
        });

        var production = new BuildingProductionQueueCompositionSystemHelper();
        production.ConfigureUnitProductionMetadataResolver((SourceUnitPrefab prefab,
            out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata metadata) => {
            if (ReferenceEquals(prefab, rifle)) { metadata = input.RifleProduction; return true; }
            if (ReferenceEquals(prefab, transport)) { metadata = input.TransportProduction; return true; }
            metadata = default; return false;
        });
        production.ConfigureProductionRecipes(recipes.Values.ToArray());
        BuildingProductionQueueCompositionSystemHelper.TryGetPrefabLocalBoundsDelegate bounds =
            (SourceUnitPrefab prefab, out ActualCore::UnityEngine.Bounds value) => {
                value = default;
                throw new NotSupportedException("Queue fixture has no local-bounds provider; nonvisual Rifle transport queue must not consume it");
            };
        var queueContext = new BuildingProductionQueueCompositionSystemHelper.QueueContext(
            units, byKey, new BuildingProductionSlotUtilitySystemHelper(), bounds, null,
            (EntityManager manager, out Entity entity) => { entity = Entity.Null; return false; });

        var fixtureGrid = CreateFixtureDeliveryGrid(em);
        // Synthetic disposable coordinate frame only, matching the S002
        // source-only fixture. It is not an S003 map or producer placement.
        Entity[] fixtureInventory = em.CaptureSourceHostLiveEntityInventory();
        RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(em, true, true, fixtureInventory,
            entity => em.HasComponentNamed(entity, "Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord"));
        var rootFrames = new Dictionary<int, SourceProducerRootFrame> {
            [producerId] = new SourceProducerRootFrame(float3.zero, quaternion.identity, 1f)
        };
        var world = new SourceProductionGroundWorld(em, fixtureGrid.Entity, Entity.Null, buildings, rootFrames);
        int globalQueueLimit = setup.InfantryQueuesEach;
        Need(globalQueueLimit > 0, "Original S003 Standard setup must provide a positive infantry queue limit");
        var producerScope = new SourceCampSingleProducerFixtureScope(producerId, authority);
        var delivery = new SourceProductionDeliveryOwner(world, production, queueContext,
            new Dictionary<int, SourceProductionDeliveryRecipe[]> {
                [producerId] = new[] { new SourceProductionDeliveryRecipe(0, rifle, 4, input.RifleProduction) }
            }, globalQueueLimit, unchecked((uint)setup.Seed), producerScope);
        var availability = new SourceCampAvailability(em, session, bank, null, null, map, gameplayMetadata,
            buildings, definitions, definitionsByPrefab, units, byKey, recipes, globalQueueLimit,
            production, queueContext, bounds, null, false, false, producerScope);
        bool missingFixtureAuthorityRejected = false;
        try { using var unscoped = new SourceCampReadScope(em, null, null, map); }
        catch (NotSupportedException) { missingFixtureAuthorityRejected = true; }
        Need(missingFixtureAuthorityRejected, "S003 setup must remain rejected by the ordinary scope without its explicit fixture authority");
        bool mismatchedSetupRejected = false;
        SkirmishObjectiveKind originalObjective = setup.ObjectiveKind;
        try {
            setup.ObjectiveKind = SkirmishObjectiveKind.None;
            SourceCampReadScope.RequireS003FixtureSetup(authority, setup);
        }
        catch (NotSupportedException error) when (error.Message == "Exact current S003 fixture authority/setup required")
        { mismatchedSetupRejected = true; }
        finally { setup.ObjectiveKind = originalObjective; }
        Need(mismatchedSetupRejected, "S003 fixture authority must reject a non-null current setup with mismatched objective identity");
        var aria = new SourceAriaPaidRecruitment(em, session, availability, delivery);
        var offer = aria.Read(SkirmishRoleKind.Rifle);
        Need(offer.Present && offer.CanRequest, "S003 source role must be present and currently requestable: " + offer.Reason);

        int materialsBeforeRejectedRequest = em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current;
        int moneyBeforeRejectedRequest = em.GetComponentData<FactionEconomy>(bank).Money;
        int reservationsBeforeRejectedRequest = em.GetBuffer<SkirmishProductionReservation>(session).Length;
        bool missingTransportOwnerRejected = false;
        try { aria.Request(SkirmishRoleKind.Rifle, 0f); }
        catch (NotSupportedException error) when (error.Message == "Source transport mode/geometry not admitted; no payment performed")
        { missingTransportOwnerRejected = true; }
        Need(missingTransportOwnerRejected && em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current == materialsBeforeRejectedRequest &&
             em.GetComponentData<FactionEconomy>(bank).Money == moneyBeforeRejectedRequest &&
             em.GetBuffer<SkirmishProductionReservation>(session).Length == reservationsBeforeRejectedRequest &&
             building.PendingProductions.Count == 0,
            "Original missing transport-owner gate must reject before payment, reservation, or pending mutation");

        var knownNullScope = new SourceCampKnownNullSingleTransportFixtureScope(producerScope, transport, null, null);
        delivery.Helicopters = new SourceHelicopterDelivery(delivery, knownNullScope);
        int materialsBefore = em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current;
        int moneyBefore = em.GetComponentData<FactionEconomy>(bank).Money;
        int reservationCountBefore = em.GetBuffer<SkirmishProductionReservation>(session).Length;
        SourceAriaRecruitmentReceipt receipt = aria.Request(SkirmishRoleKind.Rifle, 0f);
        Need(receipt.Accepted && receipt.ReceiptId.HasValue && receipt.ReceiptId.Value > 0,
            "Actual S003 shared semantic request/original paid queue rejected: " + receipt.SourceReason);
        Need(building.PendingProductions.Count == 1 && building.PendingProductions[0].ReceiptId == receipt.ReceiptId.Value &&
             building.PendingProductions[0].ReceiptOwner == session && building.PendingProductions[0].RemainingQuantity == 4,
            "Accepted S003 request must expose its actual pending receipt and four-member quantity");
        var pending = building.PendingProductions[0];
        Need(ReferenceEquals(pending.Prefab, rifle) && ReferenceEquals(pending.TransportPrefab, transport) &&
             pending.TransportMode == BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Helicopter &&
             pending.TransportArrivalSeconds == 5f && pending.TransportHoldForNextReadySeconds == 4f &&
             pending.TransportMaxConcurrent == 2,
            "Original queue resolver must retain authored helicopter timing and maxConcurrent floor");
        var reservations = em.GetBuffer<SkirmishProductionReservation>(session);
        Need(reservations.Length == reservationCountBefore + 1,
            "Actual queue must append exactly one S003 reservation");
        SkirmishProductionReservation reservation = reservations[reservations.Length - 1];
        Need(reservation.ReservationId == receipt.ReceiptId.Value && reservation.Role == SkirmishRoleKind.Rifle &&
             reservation.Producer == SkirmishProducerKind.Barracks && reservation.ProducerRuntimeId == producerId &&
             reservation.MemberCount == 4 && reservation.RemainingMembers == 4 && reservation.MaterialsPaid == setupRoles[0].MaterialsCost &&
             reservation.FactionId == FactionIdentity.PlayerFactionId,
            "Actual S003 reservation row must preserve source overlay, producer, quantity, and faction");
        int materialsAfter = em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current;
        int moneyAfter = em.GetComponentData<FactionEconomy>(bank).Money;
        Need(materialsBefore - materialsAfter == setupRoles[0].MaterialsCost && moneyAfter == moneyBefore,
            "Original paid queue must debit exactly the current Rifle overlay materials cost and no credits");
        Need(ownerQueueAccepted(delivery, receipt.ReceiptId.Value) && !producerDefinition.SourceMetadataComplete &&
             !delivery.Helicopters.AnyActive,
            "Actual queue acceptance and incomplete producer-metadata scope required before delivery advances");
        bool spawned = false;
        Entity spawnedActor = Entity.Null;
        float sourceNow = 0f;
        const float sourceDelta = 0.25f;
        for (int tick = 0; tick < 1200 && !spawned; tick++)
        {
            sourceNow += sourceDelta;
            delivery.Advance(sourceNow, sourceDelta);
            if (building.ProducedUnits == null) continue;
            foreach (Entity candidate in building.ProducedUnits)
                if (candidate != Entity.Null && em.Exists(candidate))
                { spawnedActor = candidate; spawned = true; break; }
        }
        string actualEvents = string.Join(",", delivery.DeliveryEvents.Select(row => (string)row["kind"]));
        Need(spawned && spawnedActor != Entity.Null && em.Exists(spawnedActor) &&
             delivery.DeliveredMembers == 1 && em.HasComponent<UnitGrid>(spawnedActor) &&
             em.HasComponent<LocalTransform>(spawnedActor) && em.HasComponent<UnitSourcePrefabKey>(spawnedActor) &&
             em.HasComponent<Faction>(spawnedActor) && em.GetComponentData<UnitSourcePrefabKey>(spawnedActor).Value.ToString() == RifleKey,
            "Original delivery and spawn owners did not create one source-keyed Rifle actor: events=" + actualEvents +
            "; active=" + delivery.Helicopters.AnyActive + "; delivered=" + delivery.DeliveredMembers +
            "; pending=" + (building.PendingProductions.Count == 0 ? "none" : building.PendingProductions[0].RemainingQuantity.ToString(CultureInfo.InvariantCulture)) +
            "; poisoned=" + delivery.Poisoned);
        pending = building.PendingProductions.Single();
        Need(pending.ReceiptId == receipt.ReceiptId.Value && pending.ReceiptOwner == session && pending.RemainingQuantity == 3,
            "Original queue must consume exactly one member from the accepted S003 receipt");
        var spawnedCell = em.GetComponentData<UnitGrid>(spawnedActor).Cell;
        var spawnedTransform = em.GetComponentData<LocalTransform>(spawnedActor);
        JObject movement = ExerciseOriginalMovementOwners(em, session, fixtureGrid.Entity, spawnedActor,
            receipt.ReceiptId.Value, 64, 64);

        return new JObject {
            ["kind"] = "SourceS003RifleSemanticQueueFixture",
            ["status"] = "Passed_SourceOnlyS003MovementFixture",
            ["entry"] = "SourceAriaPaidRecruitment.Request",
            ["semantic_owner"] = "SourceCampAvailability.RequestUnitSemantic",
            ["queue_owner"] = "SourceProductionDeliveryOwner.TryQueue -> original TryQueuePlayerUnitFromBuildingShared",
            ["payment_owner"] = "SkirmishNativeProduction.TryReserve / SkirmishProductionService",
            ["setup_catalog_id"] = setup.CatalogId,
            ["setup_definition_id"] = setup.DefinitionId,
            ["setup_size_id"] = setup.SizeId.ToString(),
            ["setup_seed"] = setup.Seed,
            ["setup_hash"] = setup.SetupHash,
            ["role_id"] = setupRoles[0].RoleId,
            ["producer"] = setupRoles[0].Producer.ToString(),
            ["quantity"] = reservation.MemberCount,
            ["materials_cost"] = reservation.MaterialsPaid,
            ["credits_debited"] = moneyBefore - moneyAfter,
            ["materials_debited"] = materialsBefore - materialsAfter,
            ["receipt_id"] = receipt.ReceiptId.Value,
            ["receipt_owner_is_current_session"] = pending.ReceiptOwner == session,
            ["transport_identity"] = transport.SourceAssetGuid + ":" + transport.SourceAssetLocalId.ToString(CultureInfo.InvariantCulture),
            ["transport_arrival_seconds"] = pending.TransportArrivalSeconds,
            ["transport_hold_seconds"] = pending.TransportHoldForNextReadySeconds,
            ["transport_max_concurrent_resolved_by_queue"] = pending.TransportMaxConcurrent,
            ["delivered_member_count"] = delivery.DeliveredMembers,
            ["spawned_actor_index"] = spawnedActor.Index,
            ["spawned_actor_version"] = spawnedActor.Version,
            ["spawned_cell"] = new JArray(spawnedCell.x, spawnedCell.y),
            ["spawned_position"] = new JArray(spawnedTransform.Position.x, spawnedTransform.Position.y, spawnedTransform.Position.z),
            ["source_delivery_events"] = new JArray(delivery.DeliveryEvents.Select(row => (string)row["kind"])),
            ["movement"] = movement,
            ["missing_transport_gate_rejected_before_payment"] = missingTransportOwnerRejected,
            ["missing_fixture_authority_rejected"] = missingFixtureAuthorityRejected,
            ["mismatched_setup_rejected"] = mismatchedSetupRejected,
            ["known_null_camera_and_provider_scope"] = true,
            ["partial_rifle_template_source_derived"] = em.Exists(rifle.Template) && em.HasComponent<Prefab>(rifle.Template) &&
                em.HasComponent<UnitMove>(rifle.Template) && em.HasComponent<UnitCombat>(rifle.Template) && em.HasComponent<UnitHealth>(rifle.Template),
            ["transport_identity_only"] = transport.IsAssetIdentityOnly,
            ["barrack_identity_only"] = barrack.IsAssetIdentityOnly,
            ["source_metadata_complete"] = producerDefinition.SourceMetadataComplete,
            ["pending_count"] = building.PendingProductions.Count,
            ["delivered_members"] = delivery.DeliveredMembers,
            ["factory_closure"] = false,
            ["s003_provider_membership"] = false,
            ["world_admission"] = false,
            ["mission_readiness"] = false,
            ["scope"] = "Disposable S003 source-only fixture. The original T26 compiler setup and T6/T17 field-scoped authority bind one source-identity Barrack/Rifle recipe. Current pinned Rifle source movement/combat/health/attack projection and T17-pinned prefab/config fields create an explicitly partial ECS template; no native prefab bake or complete template is claimed. Original semantic, paid queue, known-null transport, shared phase timing, clear-cell, spawn, receipt-adoption, actor-initializer, presented-group, grouped move, original pathfinding, occupancy and grid-movement owners run through one actual spawned member. Synthetic 64x64 grid and origin root frame exist only inside this disposable fixture; they do not represent an S003 map or live producer placement. The original path is selected against current synthetic fixture occupancy; no route or movement result is injected. No factory closure, S003 provider membership, normal world admission or mission readiness is claimed."
        };
    }

    static bool ownerQueueAccepted(SourceProductionDeliveryOwner owner, uint receiptId) =>
        owner.DeliveryEvents.Any(row => (string)row["kind"] == "queue_accepted" && (uint?)row["receipt_id"] == receiptId);

    public static int Main(string[] args)
    {
        try
        {
            if (args == null || args.Length != 4) throw new ArgumentException("Repository root, output directory, movement projection path, and SHA-256 required");
            JObject report = Run(args[0], args[1], args[2], args[3]);
            File.WriteAllText(Path.Combine(args[1], "control.json"), report.ToString(Newtonsoft.Json.Formatting.Indented) + Environment.NewLine);
            JObject movement = (JObject)report["movement"];
            Console.WriteLine("[SourceS003RifleSemanticQueueFixture] result=Passed scope=DisposableS003Move receipt=" + (uint)report["receipt_id"] +
                " materials=" + (int)report["materials_debited"] + " delivered=1 spawned=1 move=1 path_cells=" + ((JArray)movement["path_cells"]).Count +
                " movement_ticks=" + (int)movement["movement_ticks"] + " factory=0 membership=0 admission=0 readiness=0");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("[SourceS003RifleSemanticQueueFixture] result=Failed " + error);
            return 1;
        }
    }
}
