extern alias ActualCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Newtonsoft.Json.Linq;
using Bounds = ActualCore::UnityEngine.Bounds;

// Disposable source-only fixture. Its ECS entities are test-created and do not
// represent a loaded S002 world, native prefab, player HUD, or successful spawn.
internal static class SourceS002RifleAriaPreflightFixture
{
    const string ScenarioPath = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset";
    const string CatalogPath = "Assets/Game/Resources/SkirmishExpansionCatalog.asset";
    const string MapPath = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";
    const string RifleKey = "Unit_Chr_Soldier_Male_02_Alt_04";
    const string BarrackKey = "Building_Barrack";
    static readonly string RifleConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset";

    sealed class Inputs
    {
        internal string Root;
        internal SourceUnitPrefab Rifle, Transport, Barrack;
        internal BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata RifleProduction, TransportProduction;
        internal SourceUnitMoveAuthoringSource Movement;
        internal int RecipeQuantity;
        internal int RawMaterialsCost, CreditsCost, CanRequest;
        internal int GameplayMaterialsCost;
        internal string BarrackDisplayName, BarrackDescription;
        internal JObject Descriptor;
        internal SkirmishResolvedSetup Setup;
        internal SkirmishRoleOverlay RifleRole;
        internal string RiflePrefabPath, RiflePrefabGuid;
        internal long RiflePrefabLocalId;
    }

    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    static string MovementSystemState(OriginalGroundOwners owners, EntityManager em, Entity actor)
    {
        object boxed = typeof(OriginalGroundOwners).GetField("movementState", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(owners) ?? throw new InvalidDataException("Original movement SystemState field unavailable");
        bool enabled = (bool)typeof(SystemState).GetField("Enabled", BindingFlags.Instance | BindingFlags.Public).GetValue(boxed);
        var requiredField = typeof(SystemState).GetField("required", BindingFlags.Instance | BindingFlags.NonPublic);
        var required = requiredField?.GetValue(boxed) as IEnumerable;
        var gates = new List<string>();
        if (required != null)
            foreach (object query in required)
            {
                var entityQuery = (EntityQuery)query;
                var description = entityQuery.Description;
                string all = description.HasValue ? string.Join("+", description.Value.All.Select(type => type.Type.Name)) : "no-description";
                int count = entityQuery.CalculateEntityCount();
                string none = description.HasValue && description.Value.None != null
                    ? string.Join("+", description.Value.None.Select(type => type.Type.Name + ":excluded=" + type.Excluded)) : "<null>";
                string any = description.HasValue && description.Value.Any != null
                    ? string.Join("+", description.Value.Any.Select(type => type.Type.Name + ":excluded=" + type.Excluded)) : "<null>";
                string allModes = description.HasValue ? string.Join("+", description.Value.All.Select(type =>
                    type.Type.Name + ":excluded=" + type.Excluded)) : "no-description";
                int exactDescriptorCount = description.HasValue ? em.CreateEntityQuery(description.Value).CalculateEntityCount() : -1;
                string sameTypes = description.HasValue ? string.Join("+", description.Value.All.Select(type =>
                    type.Type == typeof(UnitPathFollow) ? "follow-type-same" : type.Type == typeof(UnitPathRange) ? "range-type-same" : "other-type")) : "";
                gates.Add(all + "=" + (entityQuery.IsEmptyIgnoreFilter ? "0" : "1") + "(count=" + count +
                    ",current=" + entityQuery.Current.Length + ",exact_descriptor_count=" + exactDescriptorCount +
                    ",manager_same=" + ReferenceEquals(entityQuery.Manager, em) + ",options=" +
                    (description.HasValue ? description.Value.Options.ToString() : "none") + ",all=" + allModes + ",none=" + none +
                    ",any=" + any + ",actor_has_follow=" + em.HasComponent<UnitPathFollow>(actor) +
                    ",actor_has_range=" + em.HasComponent<UnitPathRange>(actor) + ",chunks_null=" + (entityQuery.Chunks == null) + "," + sameTypes + ")");
            }
        return "state_enabled=" + enabled + ",required_gates=" + string.Join("", gates);
    }

    static void ExpectException<T>(Action action, string exactMessage, string failureMessage) where T : Exception
    {
        try { action(); }
        catch (T error) when (error.Message == exactMessage) { return; }
        throw new InvalidDataException(failureMessage);
    }

    static string Sha(byte[] bytes)
    {
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    static string ShaFile(string path) => Sha(File.ReadAllBytes(path));

    static byte[] Pinned(string path, string expected, int maximum)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !Regex.IsMatch(expected ?? "", "^[0-9a-f]{64}$"))
            throw new InvalidDataException("Absolute source path and lowercase SHA-256 required");
        path = Path.GetFullPath(path);
        var info = new FileInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length <= 0 || info.Length > maximum)
            throw new InvalidDataException("Pinned regular bounded source file required: " + path);
        byte[] bytes = File.ReadAllBytes(path);
        if (Sha(bytes) != expected) throw new InvalidDataException("Pinned source hash changed: " + path);
        return bytes;
    }

    static string RootFrom(string graphPath)
    {
        for (DirectoryInfo d = new FileInfo(graphPath).Directory; d != null; d = d.Parent)
            if (Directory.Exists(Path.Combine(d.FullName, "Assets", "Game", "Scripts")) &&
                Directory.Exists(Path.Combine(d.FullName, "Tools", "CI", "SkirmishSourceSimulation"))) return d.FullName;
        throw new DirectoryNotFoundException("Repository root not found from source graph");
    }

    static string Scalar(string text, string field)
    {
        var matches = Regex.Matches(text, "(?m)^  " + Regex.Escape(field) + ":\\s*(.*?)\\s*$");
        Need(matches.Count == 1, "Expected exactly one current authored config field: " + field);
        return matches[0].Groups[1].Value;
    }

    static int Int(string text, string field)
    {
        string value = Scalar(text, field);
        Need(Regex.IsMatch(value, "^-?[0-9]+$"), "Exact serialized integer required: " + field);
        return int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    static bool Bool(string text, string field)
    {
        string value = Scalar(text, field);
        Need(value == "0" || value == "1", "Exact serialized boolean required: " + field);
        return value == "1";
    }

    static float Float(string text, string field)
    {
        string value = Scalar(text, field);
        Need(Regex.IsMatch(value, "^-?(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)(?:[eE][+-]?[0-9]+)?$"),
            "Exact serialized float required: " + field);
        return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    static string Override(string text, string field, string targetFileId)
    {
        var matches = Regex.Matches(text,
            "(?m)^\\s*- target: \\{fileID: " + Regex.Escape(targetFileId) +
            ", guid: 5680e7910ed3741beba6f07116b6b03c, type: 3\\}\\s*\\n\\s+propertyPath: " + Regex.Escape(field) +
            "\\s*\\n\\s+value: ([^\\r\\n]*)\\s*\\n\\s+objectReference: \\{fileID: 0\\}\\s*$");
        Need(matches.Count == 1, "Expected exactly one pinned Rifle root override: " + field);
        return matches[0].Groups[1].Value.Trim();
    }

    sealed class RifleActorPlan
    {
        internal string Name, DisplayName, Description;
        internal string RootPrefabText;
        internal float3 Position;
        internal quaternion Rotation;
        internal float Scale, GroundOffset;
        internal UnitAnimationSettings Animation;
    }

    static string TypeBody(string source, string typeName)
    {
        string declaration = "public struct " + typeName;
        var starts = new List<int>();
        for (int cursor = 0; ;)
        {
            int candidate = source.IndexOf(declaration, cursor, StringComparison.Ordinal);
            if (candidate < 0) break;
            int after = candidate + declaration.Length;
            if (after == source.Length || !(char.IsLetterOrDigit(source[after]) || source[after] == '_'))
                starts.Add(candidate);
            cursor = after;
        }
        Need(starts.Count == 1,
            "Expected unique current source component declaration: " + typeName);
        int start = starts[0];
        int open = source.IndexOf('{', start + declaration.Length);
        Need(open >= 0, "Current source component body missing: " + typeName);
        int depth = 0;
        for (int i = open; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0) return source.Substring(open + 1, i - open - 1);
        }
        throw new InvalidDataException("Unbalanced current source component body: " + typeName);
    }

    static void RequireFields(string file, string typeName, params string[] fields)
    {
        string body = TypeBody(file, typeName);
        foreach (string field in fields)
            Need(Regex.IsMatch(body, "(?m)^\\s*public\\s+" + Regex.Escape(field.Split(' ')[0]) +
                 "\\s+" + Regex.Escape(field.Split(' ')[1]) + "\\s*;"),
                "Current source component field changed: " + typeName + "." + field);
    }

    static string ReadDescriptorPin(string root, JObject pins, string relative)
    {
        JToken pin = pins[relative];
        Need(pin != null && pin.Type == JTokenType.String,
            "Exact consumed current source pin missing: " + relative);
        return Encoding.UTF8.GetString(Pinned(
            Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)),
            (string)pin, 8 * 1024 * 1024));
    }

    static void RequireExactSource(string source, string exact, string label)
    {
        Need(source.Contains(exact), "Current source behavior changed: " + label);
    }

    static void ValidatePartialMaterializerSourceShapes(string root, JObject pins)
    {
        string groundAuthoring = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Authorings/UnitGridAuthoring.GroundContact.cs");
        string surfaceProjection = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Components/UnitSurfaceAuthoringProjection.cs");
        string surfaceComponents = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Components/MapSurfaceComponents.cs");
        string visuals = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Components/UnitVisualComponents.cs");
        string grid = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Components/GridComponents.cs");
        string combat = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Components/UnitCombatComponents.cs");
        string factions = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Components/FactionAIComponents.cs");
        string initializer = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Systems/SharedProductionActorInitializer.cs");
        string baker = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs");
        string projection = ReadDescriptorPin(root, pins, "Assets/Game/Scripts/Components/UnitMoveAuthoringProjection.cs");

        RequireExactSource(Regex.Replace(groundAuthoring, "\\s+", ""),
            "AddComponent(entity,newUnitGroundOffsetComponent{Value=authoring.ConfiguredGroundOffset});", "ground offset Baker presence/value");
        RequireExactSource(Regex.Replace(surfaceProjection, "\\s+", ""),
            "SurfaceId=-1;LayerId=0;LastSampledHeight=authoredY;LastSampledNormal=newfloat3(0f,1f,0f);HasSurface=0;IsGrounded=0;",
            "initial surface projection");
        RequireFields(surfaceComponents, "UnitSurfaceComponent", "int SurfaceId", "int LayerId", "float LastSampledHeight", "float3 LastSampledNormal", "byte HasSurface", "byte IsGrounded");
        RequireFields(surfaceComponents, "UnitGroundOffsetComponent", "float Value");
        RequireFields(visuals, "UnitIdleWanderComponent", "uint RandomState", "float RetrySeconds", "float CurrentIdleDelaySeconds");
        RequireFields(visuals, "UnitAnimationSettings", "float IdleDelayMinSeconds", "float IdleDelayMaxSeconds", "float IdleWanderDistanceMin", "float IdleWanderDistanceMax", "float AttackAnimationSeconds", "float DeathAnimationSeconds");
        RequireFields(visuals, "UnitPrevWorldPos", "float3 Value");
        RequireFields(visuals, "UnitMoveVisualComponent", "byte IsMoving", "float StillSeconds");
        RequireFields(grid, "UnitGrid", "int2 Cell");
        RequireFields(grid, "UnitMove", "float Speed", "float WalkSpeed", "float RoadSpeedMultiplier", "float ArriveDistance");
        RequireFields(grid, "UnitFootprint", "int2 Size");
        RequireFields(grid, "UnitMovementBehavior", "byte AllowIdleWander", "byte UsesVehicleMotion");
        RequireFields(grid, "UnitVehicleMovement", "float TurnSpeedDegrees", "float Acceleration", "float Braking", "float RearPivotOffset");
        RequireFields(grid, "UnitVehicleKinematics", "float CurrentSpeed", "float StallSeconds");
        RequireFields(grid, "UnitAirMovement", "float CruiseHeight", "float RunwayTaxiSpeed");
        RequireFields(grid, "UnitPathRequest", "int2 Goal");
        RequireFields(grid, "UnitPathFollow", "int PathIndex");
        RequireFields(grid, "UnitPathRange", "int Start", "int Length");
        RequireFields(combat, "UnitCombat", "int AggroRangeCells", "float ChaseBreakDistance", "byte CanAttack", "byte AutoEngage");
        RequireFields(combat, "UnitHealth", "int Current", "int Max");
        RequireFields(combat, "UnitAttack", "float Range", "float CooldownSeconds", "int Damage", "float4 TraceColor", "float TraceWidth", "float TraceScrollSpeed", "float TraceDashDensity", "float TraceVisibleSeconds", "int TracerEveryNthShot");
        RequireFields(combat, "UnitAttackCooldownComponent", "float CooldownRemaining");
        RequireFields(combat, "UnitAttackTraceComponent", "float TimeRemaining", "float Phase", "int ShotCounter");
        RequireFields(factions, "Faction", "byte Id");
        RequireExactSource(Regex.Replace(initializer, "\\s+", ""),
            "if(em.HasComponent<Faction>(instance))em.SetComponentData(instance,newFaction{Id=ResolveProducedUnitFaction(building)});",
            "production initializer faction overwrite");
        RequireExactSource(Regex.Replace(initializer, "\\s+", ""),
            "if(em.HasComponent<UnitIdleWanderComponent>(instance)){randomState=math.max(1u,randomState+1u);",
            "production initializer idle-wander RNG advancement");
        RequireExactSource(Regex.Replace(baker, "\\s+", ""),
            "AddComponent(entity,UnitIdleWanderAuthoringProjection.FromAuthoredIdentity(footprint.x,footprint.y,authoring.gameObject.name));",
            "Baker shared idle-wander initializer");
        RequireExactSource(Regex.Replace(baker, "\\s+", ""),
            "AddComponent(entity,UnitAnimationAuthoringProjection.FromConfiguredValues(",
            "Baker shared animation settings projection");
        RequireExactSource(Regex.Replace(baker, "\\s+", ""),
            "AddGroundContact(entity,authoring);",
            "Baker ground-offset component path");
        RequireExactSource(Regex.Replace(baker, "\\s+", ""),
            "UnitSurfaceAuthoringProjection.FromAuthoredHeight(authoring.transform.position.y)",
            "Baker authored surface initialization");
        RequireExactSource(Regex.Replace(baker, "\\s+", ""),
            "AddComponent(entity,newUnitGrid{Cell=int2.zero});",
            "Baker unit grid initial cell");
        RequireExactSource(Regex.Replace(baker, "\\s+", ""),
            "AddComponent(entity,newFaction{Id=bakedFactionId});",
            "Baker faction component presence");
        RequireExactSource(Regex.Replace(projection, "\\s+", ""),
            "math.hash(newint4(footprintX,footprintY,authoredName.GetHashCode(),0x45d9f3b))",
            "shared authored idle random hash expression");
        RequireExactSource(Regex.Replace(projection, "\\s+", ""),
            "IdleDelayMaxSeconds=math.max(idleDelayMinSeconds,idleDelayMaxSeconds)",
            "shared animation timing clamp");

        // Tamper controls exercise the exact validators with no EntityManager access.
        string badGround = groundAuthoring.Replace("UnitGroundOffsetComponent", "OtherGroundOffsetComponent");
        ExpectException<InvalidDataException>(() => RequireExactSource(Regex.Replace(badGround, "\\s+", ""),
                "AddComponent(entity,newUnitGroundOffsetComponent{Value=authoring.ConfiguredGroundOffset});", "ground offset negative control"),
            "Current source behavior changed: ground offset negative control",
            "Shape control accepted a changed ground offset Baker component");
        string badInitializer = initializer.Replace("ResolveProducedUnitFaction(building)", "FactionIdentity.NeutralFactionId");
        ExpectException<InvalidDataException>(() => RequireExactSource(Regex.Replace(badInitializer, "\\s+", ""),
                "if(em.HasComponent<Faction>(instance))em.SetComponentData(instance,newFaction{Id=ResolveProducedUnitFaction(building)});",
                "Faction initializer negative control"),
            "Current source behavior changed: Faction initializer negative control",
            "Shape control accepted a changed pre-consumer faction overwrite");
        string badVisualShape = visuals.Replace("public uint RandomState;", "public int RandomState;");
        ExpectException<InvalidDataException>(() => RequireFields(badVisualShape, "UnitIdleWanderComponent",
                "uint RandomState", "float RetrySeconds", "float CurrentIdleDelaySeconds"),
            "Current source component field changed: UnitIdleWanderComponent.uint RandomState",
            "Shape control accepted a changed idle-wander RNG component field");
    }

    // This plan is intentionally narrower than UnitGridBaker. It carries only
    // the current source values used by production spawn initialization and the
    // manual path/movement owners. It does not claim a full Baker prefab.
    static RifleActorPlan PrepareRifleActorPlan(Inputs input)
    {
        ValidatePartialMaterializerSourceShapes(input.Root, (JObject)input.Descriptor["source_pins"]);
        string rootPrefabPath = input.RiflePrefabPath;
        string sourcePrefabPath = "Assets/Game/Prefabs/Characters/Unit.prefab";
        JObject pins = (JObject)input.Descriptor["source_pins"];
        Need(rootPrefabPath == "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab" &&
             input.RiflePrefabGuid == "ff73c8b7cbf844778916da330e9cd3e3" &&
             input.RiflePrefabLocalId == 3282182154612916099L,
            "Exact current Rifle path/GUID/fileID identity required before partial materialization");
        string rootPrefab = Encoding.UTF8.GetString(Pinned(
            Path.Combine(input.Root, rootPrefabPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[rootPrefabPath], 2 * 1024 * 1024));
        string sourcePrefab = Encoding.UTF8.GetString(Pinned(
            Path.Combine(input.Root, sourcePrefabPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[sourcePrefabPath], 2 * 1024 * 1024));
        string sourceMetaPath = sourcePrefabPath + ".meta";
        string sourceMeta = Encoding.UTF8.GetString(Pinned(
            Path.Combine(input.Root, sourceMetaPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[sourceMetaPath], 128 * 1024));
        string sourceGuid = Regex.Match(sourceMeta, "(?m)^guid: ([0-9a-f]{32})$").Groups[1].Value;
        Need(sourceGuid.Length == 32 && Regex.Matches(sourceMeta, "(?m)^guid: [0-9a-f]{32}$").Count == 1,
            "Exact UnitGridAuthoring source prefab GUID required");
        var rootInstances = Regex.Matches(rootPrefab,
            "(?ms)^--- !u!1001 &[^\\r\\n]+\\r?\\n(.*?)(?=^--- !u!|\\z)")
            .Cast<Match>()
            .Where(match => Regex.IsMatch(match.Groups[1].Value, "(?m)^    m_TransformParent: \\{fileID: 0\\}$") &&
                            Regex.IsMatch(match.Groups[1].Value, "(?m)^  m_SourcePrefab: \\{fileID: 100100000, guid: " + sourceGuid + ", type: 3\\}$"))
            .ToArray();
        Need(rootInstances.Length == 1,
            "Rifle must have exactly one pinned root instance of the pinned Unit source prefab");
        string modelPath = "Assets/Game/Prefabs/Generated/CharactersBaked/Prefab_SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned_31.prefab";
        string model = Encoding.UTF8.GetString(Pinned(
            Path.Combine(input.Root, modelPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[modelPath], 8 * 1024 * 1024));
        string operationIdentityScript = "Assets/Game/Scripts/Authorings/OperationMapEntityPresentationIdentityAuthoring.cs";
        string operationIdentityMeta = Encoding.UTF8.GetString(Pinned(
            Path.Combine(input.Root, (operationIdentityScript + ".meta").Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[operationIdentityScript + ".meta"], 128 * 1024));
        string operationIdentityGuid = Regex.Match(operationIdentityMeta, "(?m)^guid: ([0-9a-f]{32})$").Groups[1].Value;
        Need(operationIdentityGuid.Length == 32 && Regex.Matches(operationIdentityMeta, "(?m)^guid: [0-9a-f]{32}$").Count == 1 &&
             Regex.Matches(rootPrefab, "(?m)^    m_AddedComponents: \\[\\]$").Count == 2 &&
             !rootPrefab.Contains(operationIdentityGuid) && !model.Contains(operationIdentityGuid),
            "Pinned Rifle root/model must not contain an OperationMap identity authoring component");
        string modelMetaPath = modelPath + ".meta";
        string modelMeta = Encoding.UTF8.GetString(Pinned(
            Path.Combine(input.Root, modelMetaPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[modelMetaPath], 128 * 1024));
        string modelGuid = Regex.Match(modelMeta, "(?m)^guid: ([0-9a-f]{32})$").Groups[1].Value;
        var modelSourceRefs = Regex.Matches(rootPrefab,
            "(?m)^  m_SourcePrefab: \\{fileID: 100100000, guid: ([0-9a-f]{32}), type: 3\\}$");
        Need(modelGuid.Length == 32 && Regex.Matches(modelMeta, "(?m)^guid: [0-9a-f]{32}$").Count == 1 &&
             modelSourceRefs.Count == 2 && modelSourceRefs[1].Groups[1].Value == modelGuid,
            "Pinned current Rifle model prefab GUID/reference mismatch");
        Match transformBlock = Regex.Match(sourcePrefab,
            "(?ms)^--- !u!4 &23759086217692639\\nTransform:\\n(.*?)(?=^--- !u!|\\z)");
        Need(transformBlock.Success && Regex.Matches(sourcePrefab, "(?m)^--- !u!4 &23759086217692639$").Count == 1 &&
             Regex.IsMatch(sourcePrefab, "(?m)^  m_Name: Unit$") &&
             Regex.IsMatch(transformBlock.Groups[1].Value, "(?m)^  m_GameObject: \\{fileID: 2254344950327853479\\}$") &&
             Regex.IsMatch(transformBlock.Groups[1].Value, "(?m)^  m_Father: \\{fileID: 0\\}$") &&
             Regex.IsMatch(transformBlock.Groups[1].Value, "(?m)^  m_LocalPosition: \\{x: 0, y: 0, z: 0\\}$") &&
             Regex.IsMatch(transformBlock.Groups[1].Value, "(?m)^  m_LocalRotation: \\{x: 0, y: 0, z: 0, w: 1\\}$") &&
             Regex.IsMatch(transformBlock.Groups[1].Value, "(?m)^  m_LocalScale: \\{x: 1, y: 1, z: 1\\}$"),
            "Pinned Unit source root transform must have exact unit scale and identity pose");

        string name = Override(rootPrefab, "m_Name", "2254344950327853479");
        RequireRifleRootName(name);
        float px = ParseRootOverride(rootPrefab, "m_LocalPosition.x", "23759086217692639", 0f);
        float py = ParseRootOverride(rootPrefab, "m_LocalPosition.y", "23759086217692639", 0f);
        float pz = ParseRootOverride(rootPrefab, "m_LocalPosition.z", "23759086217692639", 0f);
        float qx = ParseRootOverride(rootPrefab, "m_LocalRotation.x", "23759086217692639", 0f);
        float qy = ParseRootOverride(rootPrefab, "m_LocalRotation.y", "23759086217692639", 0f);
        float qz = ParseRootOverride(rootPrefab, "m_LocalRotation.z", "23759086217692639", 0f);
        float qw = ParseRootOverride(rootPrefab, "m_LocalRotation.w", "23759086217692639", 1f);
        string configPath = RifleConfigPath;
        string config = Encoding.UTF8.GetString(Pinned(
            Path.Combine(input.Root, configPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[configPath], 1024 * 1024));
        return new RifleActorPlan {
            RootPrefabText = rootPrefab,
            Name = name,
            DisplayName = TextBlock(config, "displayName"),
            Description = TextBlock(config, "description"),
            Position = new float3(px, py, pz),
            Rotation = new quaternion(qx, qy, qz, qw),
            Scale = 1f,
            GroundOffset = Float(config, "groundOffset"),
            Animation = UnitAnimationAuthoringProjection.FromConfiguredValues(
                Float(config, "idleDelayMinSeconds"), Float(config, "idleDelayMaxSeconds"),
                Float(config, "idleWanderDistanceMin"), Float(config, "idleWanderDistanceMax"),
                Float(config, "attackAnimationSeconds"), Float(config, "deathAnimationSeconds"))
        };
    }

    static void RequireRifleRootName(string name)
    {
        Need(name == RifleKey, "Pinned Rifle root name does not match the authored source key");
    }

    static float ParseRootOverride(string text, string field, string targetFileId, float expected)
    {
        string value = Override(text, field, targetFileId);
        Need(Regex.IsMatch(value, "^-?(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)$"),
            "Exact serialized root transform float required: " + field);
        float parsed = float.Parse(value, CultureInfo.InvariantCulture);
        Need(parsed == expected, "Current Rifle root transform changed: " + field);
        return parsed;
    }

    static Entity MaterializeRifleMovementPrefab(EntityManager em, Inputs input, RifleActorPlan plan)
    {
        UnitMoveAuthoringProjection movement = input.Movement.Projection;
        Need(movement.HasMovementSlice && movement.Footprint.Size.x == 1 && movement.Footprint.Size.y == 1 &&
             movement.MovementBehavior.AllowIdleWander == 1 && movement.MovementBehavior.UsesVehicleMotion == 0,
            "Exact current Rifle movement projection required before materialization");
        var entity = em.CreateEntity();
        em.AddComponent<Prefab>(entity);
        em.AddComponentData(entity, LocalTransform.FromPositionRotationScale(plan.Position, plan.Rotation, plan.Scale));
        em.AddComponentData(entity, new UnitGrid { Cell = int2.zero });
        em.AddComponentData(entity, new UnitMove { Speed = movement.Speed, WalkSpeed = movement.WalkSpeed,
            RoadSpeedMultiplier = movement.RoadSpeedMultiplier, ArriveDistance = movement.ArriveDistance });
        em.AddComponentData(entity, movement.Footprint);
        em.AddComponentData(entity, movement.MovementBehavior);
        em.AddComponentData(entity, movement.VehicleMovement);
        em.AddComponentData(entity, movement.VehicleKinematics);
        UnitSurfaceAuthoringProjection surface = UnitSurfaceAuthoringProjection.FromAuthoredHeight(plan.Position.y);
        em.AddComponentData(entity, new UnitSurfaceComponent { SurfaceId = surface.SurfaceId, LayerId = surface.LayerId,
            LastSampledHeight = surface.LastSampledHeight, LastSampledNormal = surface.LastSampledNormal,
            HasSurface = surface.HasSurface, IsGrounded = surface.IsGrounded });
        em.AddComponentData(entity, new UnitGroundOffsetComponent { Value = plan.GroundOffset });
        // Baker unconditionally supplies Faction and the production initializer
        // overwrites it from the producer owner before downstream consumers.
        // The value here is only a fixture carrier and is not native-prefab evidence.
        em.AddComponentData(entity, new Faction { Id = FactionIdentity.NeutralFactionId });
        em.AddComponentData(entity, input.Movement.Combat);
        em.AddComponentData(entity, input.Movement.Health);
        em.AddComponentData(entity, input.Movement.Attack);
        em.AddComponentData(entity, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
        em.AddComponentData(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
        em.AddComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
        em.AddComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(plan.Name) });
        em.AddComponentData(entity, new UnitPrevWorldPos { Value = plan.Position });
        em.AddComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.AddComponentData(entity, plan.Animation);
        em.AddComponentData(entity, UnitIdleWanderAuthoringProjection.FromAuthoredIdentity(
            movement.Footprint.Size.x, movement.Footprint.Size.y, plan.Name));
        return entity;
    }

    static string TextBlock(string text, string field)
    {
        string[] lines = text.Replace("\r", "").Split('\n');
        string prefix = "  " + field + ":";
        int found = -1;
        for (int i = 0; i < lines.Length; i++) if (lines[i].StartsWith(prefix, StringComparison.Ordinal))
        { Need(found < 0, "Duplicate current authored text field: " + field); found = i; }
        Need(found >= 0, "Missing current authored text field: " + field);
        string first = lines[found].Substring(prefix.Length).TrimStart();
        var parts = new List<string> { first };
        for (int i = found + 1; i < lines.Length && lines[i].StartsWith("    ", StringComparison.Ordinal); i++)
            parts.Add(lines[i].Trim());
        return string.Join(" ", parts);
    }

    static Inputs LoadInputs(string[] args)
    {
        // Arguments: descriptor path/hash, current movement input path/hash,
        // source graph path/hash, setup matrix path/hash.
        Need(args != null && (args.Length == 8 || args.Length == 9), "Eight explicit pinned fixture inputs plus optional connected-entry mode required");
        string metadataPath = Path.GetFullPath(args[0]);
        JObject metadata = JObject.Parse(Encoding.UTF8.GetString(Pinned(metadataPath, args[1], 2 * 1024 * 1024)));
        Need((int?)metadata["schema_version"] == 1 && (string)metadata["kind"] == "BarrackRifleCurrentAuthoredSourceFacts" &&
             (string)metadata["status"] == "PassedSourceOnlyDescriptor" && (bool?)metadata["scope"]?["native_factory_invoked"] == false &&
             (bool?)metadata["scope"]?["runtime_admission"] == false && (bool?)metadata["scope"]?["mission_ready"] == false &&
             (bool?)metadata["scope"]?["generic_witness_establishes_s002_s003_membership"] == false &&
             (bool?)metadata["scope"]?["unity_asset_resolution"] == false,
             "Exact current source-only Rifle descriptor required");
        JObject pins = metadata["source_pins"] as JObject ?? throw new InvalidDataException("Rifle descriptor pins missing");
        string root = RootFrom(Path.GetFullPath(args[4]));
        foreach (JProperty pin in pins.Properties())
            Pinned(Path.Combine(root, pin.Name.Replace('/', Path.DirectorySeparatorChar)), (string)pin.Value, 8 * 1024 * 1024);
        JObject unit = (JObject)metadata["facts"]["unit"];
        JObject transport = (JObject)metadata["facts"]["transport_unit"];
        JObject producerFacts = (JObject)metadata["facts"]["producer"];
        JObject witness = (JObject)metadata["generic_authored_identity_witness"];
        JObject barrackPair = (JObject)witness?["barrack_pair"];
        Need((string)unit["key"] == RifleKey && (string)producerFacts["key"] == BarrackKey &&
             (int?)((JArray)producerFacts["recipes"])[0]["quantity"] == 4 &&
             (string)witness?["kind"] == "GenericSkirmishAuthoredBarrackReferenceWitness" &&
             (string)witness?["scope"] == "generic SkirmishBaseAssault template only" &&
             (string)barrackPair?["guid"] == (string)producerFacts["prefab"]?["guid"] &&
             (long?)barrackPair?["local_id"] == 6603657636351658259L &&
             (string)barrackPair?["basis"] == "exact authored YAML reference pair; no Unity importer resolution claimed",
             "Current authored Rifle/Barrack recipe identity changed");

        string configPath = Path.Combine(root, RifleConfigPath.Replace('/', Path.DirectorySeparatorChar));
        string configSha = (string)pins[RifleConfigPath];
        string config = Encoding.UTF8.GetString(Pinned(configPath, configSha, 1024 * 1024));
        string barrackConfigPath = (string)producerFacts["config"]["path"];
        string barrackConfig = Encoding.UTF8.GetString(Pinned(
            Path.Combine(root, barrackConfigPath.Replace('/', Path.DirectorySeparatorChar)),
            (string)pins[barrackConfigPath], 1024 * 1024));
        string unitSourcePath = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs";
        Need(pins[unitSourcePath] != null, "Current UnitGridAuthoring getter source pin missing");

        string riflePrefabPath = (string)unit["prefab"]["path"];
        string riflePrefabGuid = (string)unit["prefab"]["guid"];
        long riflePrefabLocalId = (long)unit["prefab"]["local_id"];
        var riflePrefab = SourceUnitPrefab.FromAssetIdentity(RifleKey, riflePrefabPath, riflePrefabGuid, riflePrefabLocalId);
        string barrackPrefabPath = (string)producerFacts["prefab"]["path"];
        string barrackPrefabGuid = (string)barrackPair["guid"];
        long barrackPrefabLocalId = (long)barrackPair["local_id"];
        var barrackPrefab = SourceUnitPrefab.FromAssetIdentity(BarrackKey, barrackPrefabPath, barrackPrefabGuid, barrackPrefabLocalId);
        var transportPrefab = SourceUnitPrefab.FromAssetIdentity((string)transport["key"],
            (string)transport["prefab"]["path"], (string)transport["prefab"]["guid"], (long)transport["prefab"]["local_id"]);

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
        Need((string)unit["transport"]["path"] == transportPrefab.SourceAssetPath &&
             (string)unit["transport"]["guid"] == transportPrefab.SourceAssetGuid &&
             (long)unit["transport"]["local_id"] == transportPrefab.SourceAssetLocalId &&
             transportPrefab.SourceAssetGuid == (string)transport["prefab"]["guid"] &&
             transportPrefab.SourceAssetLocalId == (long)transport["prefab"]["local_id"],
             "Rifle transport reference must bind the exact transport prefab GUID/fileID");
        Need((float)unit["production_duration_seconds"] == 5f &&
             (float)unit["transport_arrival_seconds"] == 5f &&
             (float)unit["transport_hold_for_next_ready_seconds"] == 4f &&
             (int)unit["transport_max_concurrent"] == 1 &&
             !(bool)unit["transport_requires_airport_runway"] && !(bool)unit["transport_uses_runway_landing"] &&
             (int)unit["footprint_cells"][0] == 1 && (int)unit["footprint_cells"][1] == 1 &&
             (float)transport["production_duration_seconds"] == 5f &&
             (float)transport["production_transport_arrival_seconds"] == 5f &&
             (float)transport["production_transport_hold_for_next_ready_seconds"] == 4f &&
             (int)transport["production_transport_max_concurrent"] == 2 &&
             !(bool)transport["production_transport_requires_airport_runway"] &&
             !(bool)transport["production_transport_uses_runway_landing"] &&
             (int)transport["footprint_cells"][0] == 1 && (int)transport["footprint_cells"][1] == 1,
             "Current authored Rifle/transport arrival, hold, concurrency, flags, footprint, and duration facts changed");

        var movementDescriptor = new JObject { ["path"] = Path.GetFullPath(args[2]), ["sha256"] = args[3] };
        var movement = SourceUnitMoveAuthoringSource.LoadCurrentProjection(movementDescriptor, RifleKey, root,
            (path, sha, maximum) => Pinned(path, sha, maximum));
        Need(movement.Projection.HasMovementSlice && movement.ConfigGuid == (string)unit["config"]["guid"],
             "Current no-capture Rifle projection/config binding mismatch");

        int credits = (int)unit["credits_cost"];
        int rawMaterials = Int(config, "materialsCost");
        int configPrice = Int(config, "price");
        Need(rawMaterials == (int)unit["materials_cost"] && configPrice == credits && configPrice > 0,
            "Pinned positive config Price and materials branch must match the original UnitGridAuthoringConfig getter path");
        int materialsGetter = rawMaterials > 0 ? rawMaterials : UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(configPrice);
        Need(UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(-1) == 1 &&
             UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(0) == 1 &&
             UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(499) == 1 &&
             UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(500) == 1 &&
             UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(501) == 2 &&
             UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(10000) == 20,
             "Shared original legacy materials-cost boundary behavior changed");
        int canRequest = Bool(config, "canRequest") ? 1 : 0;

        JObject graph = JObject.Parse(Encoding.UTF8.GetString(Pinned(Path.GetFullPath(args[4]), args[5], 16 * 1024 * 1024)));
        string matrixPath = Path.GetFullPath(args[6]);
        byte[] matrixBytes = Pinned(matrixPath, args[7], 4 * 1024 * 1024);
        JObject matrixDescriptor = graph["setup_matrix"] as JObject ?? throw new InvalidDataException("Current graph setup matrix descriptor missing");
        Need(Path.GetFullPath((string)matrixDescriptor["path"]) == matrixPath && (string)matrixDescriptor["sha256"] == args[7],
             "S002 matrix must be the exact graph-owned setup matrix");
        Need((string)graph["definition_path"] == ScenarioPath && (string)graph["catalog_path"] == CatalogPath &&
             (bool?)graph["compiled_setup"] == false && (bool?)graph["world_initialized"] == false,
             "Current authored source graph, before world construction, required");
        foreach (JProperty pin in ((JObject)graph["source_pins"]).Properties()) Pinned((string)pin.Name, (string)pin.Value, 4 * 1024 * 1024);
        var materializer = new SourceConfigMaterializer(graph);
        var definition = materializer.ByPath<SkirmishScenarioDefinitionConfig>(ScenarioPath);
        var catalog = materializer.ByPath<SkirmishExpansionCatalogConfig>(CatalogPath);
        UnityEngine.Resources.BindSourceResources(new Dictionary<string, object> {
            { SkirmishExpansionCatalogConfig.ResourceName, catalog },
            { SkirmishSetupMatrixTable.PackagedResourceName, new UnityEngine.TextAsset(Encoding.UTF8.GetString(matrixBytes)) }
        });
        Need(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError), "Current S002 setup matrix rejected: " + matrixError);
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
        Need(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard,
            104731, manifest, matrix, out var setup, out var reasons), "Original S002 setup compiler rejected: " + string.Join(";", reasons.Select(x => x.Code + ":" + x.Field)));
        Need(setup.SetupHash == 3657809016u && setup.Forces.Length == 16 && setup.Structures.Length == 18,
             "Actual in-process setup differs from current T4 compiler result");
        var role = setup.RoleOverlays.SingleOrDefault(x => x.RoleKind == SkirmishRoleKind.Rifle);
        Need(role.RoleKind == SkirmishRoleKind.Rifle && role.Producer == SkirmishProducerKind.Barracks && role.SquadMembers == 4,
             "Compiled S002 setup does not bind the authored Barrack Rifle recipe");
        Need(canRequest == 1, "Current authored Rifle is not requestable");

        return new Inputs {
            Root = root, Rifle = riflePrefab, Transport = transportPrefab, Barrack = barrackPrefab,
            RiflePrefabPath = riflePrefab.SourceAssetPath, RiflePrefabGuid = riflePrefab.SourceAssetGuid,
            RiflePrefabLocalId = riflePrefab.SourceAssetLocalId,
            RifleProduction = rifleMetadata, TransportProduction = transportMetadata, Movement = movement,
            RecipeQuantity = (int)((JArray)metadata["facts"]["producer"]["recipes"])[0]["quantity"],
            RawMaterialsCost = rawMaterials, CreditsCost = credits, CanRequest = canRequest,
            GameplayMaterialsCost = materialsGetter,
            BarrackDisplayName = TextBlock(barrackConfig, "displayName"), BarrackDescription = TextBlock(barrackConfig, "description"),
            Descriptor = metadata, Setup = setup, RifleRole = role
        };
    }

    static void Run(string[] args)
    {
        Inputs input = LoadInputs(args);
        string connectedEntry = args.Length > 8 ? args[8] : "aria-success";
        Need(connectedEntry == "aria-success" || connectedEntry == "hud-success",
            "Explicit fixture connected request entry must be aria-success or hud-success");
        int next = 1;
        var em = new EntityManager { LogicalAllocatorAdmitted = true, AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 } };
        RifleActorPlan rifleActorPlan = PrepareRifleActorPlan(input);
        int preallocationIdentity = next;
        ExpectException<InvalidDataException>(() => Pinned(
                Path.Combine(input.Root, input.RiflePrefabPath.Replace('/', Path.DirectorySeparatorChar)),
                new string('0', 64), 2 * 1024 * 1024),
            "Pinned source hash changed: " + Path.Combine(input.Root, input.RiflePrefabPath.Replace('/', Path.DirectorySeparatorChar)),
            "Partial Rifle materializer accepted a mismatched current prefab pin");
        ExpectException<InvalidDataException>(() => RequireRifleRootName("Unit_Chr_Soldier_Male_02_Alt_03"),
            "Pinned Rifle root name does not match the authored source key",
            "Partial Rifle materializer accepted a different prefab root name");
        ExpectException<InvalidDataException>(() => ParseRootOverride(
                rifleActorPlan.RootPrefabText, "m_LocalPosition.x", "23759086217692639", 1f),
            "Current Rifle root transform changed: m_LocalPosition.x",
            "Partial Rifle materializer accepted a changed root transform");
        Need(next == preallocationIdentity,
            "Negative partial-prefab source controls allocated an ECS identity before validation completed");
        Entity partialRiflePrefab = MaterializeRifleMovementPrefab(em, input, rifleActorPlan);
        Need(em.Exists(partialRiflePrefab) && next == preallocationIdentity + 1 &&
             em.HasComponent<Prefab>(partialRiflePrefab) &&
             em.HasComponent<LocalTransform>(partialRiflePrefab) &&
             em.HasComponent<UnitGrid>(partialRiflePrefab) &&
             em.HasComponent<UnitMove>(partialRiflePrefab) &&
             em.HasComponent<UnitFootprint>(partialRiflePrefab) &&
             em.HasComponent<UnitMovementBehavior>(partialRiflePrefab) &&
             em.HasComponent<UnitVehicleMovement>(partialRiflePrefab) &&
             em.HasComponent<UnitVehicleKinematics>(partialRiflePrefab) &&
             em.HasComponent<UnitSurfaceComponent>(partialRiflePrefab) &&
             em.HasComponent<UnitGroundOffsetComponent>(partialRiflePrefab) &&
             em.HasComponent<Faction>(partialRiflePrefab) &&
             em.HasComponent<UnitPrevWorldPos>(partialRiflePrefab) &&
             em.HasComponent<UnitMoveVisualComponent>(partialRiflePrefab) &&
             em.HasComponent<UnitIdleWanderComponent>(partialRiflePrefab) &&
             !em.HasComponent<UnitAirMovement>(partialRiflePrefab) &&
             !em.HasComponent<UnitPathFollow>(partialRiflePrefab) &&
             !em.HasComponent<UnitPathRange>(partialRiflePrefab) &&
             !em.HasComponent<UnitPathRequest>(partialRiflePrefab),
            "Fixture-only partial Rifle materializer did not add the exact owner-consumed component set");
        UnitIdleWanderComponent actualWander = em.GetComponentData<UnitIdleWanderComponent>(partialRiflePrefab);
        UnitIdleWanderComponent expectedWander = UnitIdleWanderAuthoringProjection.FromAuthoredIdentity(1, 1, RifleKey);
        UnitMove actualMove = em.GetComponentData<UnitMove>(partialRiflePrefab);
        LocalTransform actualTransform = em.GetComponentData<LocalTransform>(partialRiflePrefab);
        Need(actualWander.RandomState == expectedWander.RandomState && actualWander.RetrySeconds == 0f &&
             actualWander.CurrentIdleDelaySeconds == 0f &&
             actualMove.Speed == input.Movement.Projection.Speed &&
             actualMove.WalkSpeed == input.Movement.Projection.WalkSpeed &&
             actualMove.RoadSpeedMultiplier == input.Movement.Projection.RoadSpeedMultiplier &&
             actualMove.ArriveDistance == input.Movement.Projection.ArriveDistance &&
             math.all(actualTransform.Position == rifleActorPlan.Position) &&
             math.all(actualTransform.Rotation.value == rifleActorPlan.Rotation.value) &&
             actualTransform.Scale == rifleActorPlan.Scale &&
             em.GetComponentData<UnitGroundOffsetComponent>(partialRiflePrefab).Value == rifleActorPlan.GroundOffset &&
             em.GetComponentData<UnitSourcePrefabKey>(partialRiflePrefab).Value.ToString() == RifleKey,
            "Fixture partial Rifle materializer values diverged from pinned source projections");
        // Barrack identity comes from T12's generic authored reference witness.
        // It carries no template, Unity importer resolution, or S002 membership.
        // This separate fixture token binds the source identity to the exact
        // partial entity built above. It is not promoted to a native prefab.
        var rifle = new SourceUnitPrefab(RifleKey, partialRiflePrefab,
            input.Rifle.SourceAssetPath, input.Rifle.SourceAssetGuid, input.Rifle.SourceAssetLocalId);
        var transport = SourceUnitPrefab.FromAssetIdentity(input.Transport.name,
            input.Transport.SourceAssetPath, input.Transport.SourceAssetGuid, input.Transport.SourceAssetLocalId);
        var barrack = SourceUnitPrefab.FromAssetIdentity(BarrackKey,
            input.Barrack.SourceAssetPath, input.Barrack.SourceAssetGuid, input.Barrack.SourceAssetLocalId);
        Need(barrack.IsAssetIdentityOnly && barrack.Template == Entity.Null,
            "Generic authored Barrack witness must remain an identity-only token");
        var rifleProduction = input.RifleProduction;
        var transportProduction = input.TransportProduction;
        string rifleIdentity = rifle.SourceAssetGuid + ":" + rifle.SourceAssetLocalId.ToString(CultureInfo.InvariantCulture);
        var map = new SourceCampGameplayIdentityMap(new[] { rifle },
            new[] { new SourceGameplayPrefabBinding(rifle, rifleIdentity) });

        // The setup object above is compiled by the original source compiler from current
        // graph+matrix. The ECS session/bank/producer below are explicitly fixture-created.
        Entity session = em.CreateEntity();
        string sessionId = "source-rifle-aria-fixture";
        // Playing and completion bits below are explicitly fixture-injected so
        // the original semantic entry has its required context. They are not S002
        // launch, startup, or mission-initialization evidence.
        em.AddComponentData(session, new SkirmishExpandedSessionComponent {
            SessionId = new FixedString64Bytes(sessionId), CatalogId = new FixedString64Bytes(input.Setup.CatalogId),
            DefinitionId = new FixedString64Bytes(input.Setup.DefinitionId), ContentVersion = input.Setup.ContentVersion,
            SetupHash = input.Setup.SetupHash, Seed = input.Setup.Seed, SizeId = input.Setup.SizeId,
            DifficultyId = input.Setup.DifficultyId, ObjectiveKind = input.Setup.ObjectiveKind,
            Phase = SkirmishSessionPhase.Playing, InitializationComplete = 1, SpawnComplete = 1
        });
        em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = input.Setup });
        var roleMap = input.Setup.RoleOverlays.ToDictionary(
            x => SkirmishRoleCatalogConfig.RuntimePrefabKey(x.RoleKind), x => x, StringComparer.Ordinal);
        var catalog = new SkirmishProductionCatalog(input.Setup, roleMap, true);
        em.AddComponentObject(session, new SkirmishProductionCatalogRecord { Catalog = catalog });
        em.AddComponentData(session, new SkirmishCapacityComponent {
            FactionId = FactionIdentity.PlayerFactionId, InfantryCap = input.Setup.InfantryCapEach,
            GroundCap = input.Setup.GroundCapEach, AirCap = input.Setup.TacticalAirCapEach, SupplyCap = input.Setup.SupplyCapEach,
            NextReservationId = 1
        });
        // Match the original session initializer's legacy stock carrier using
        // compiled setup values. The semantic request still charges the actual
        // faction bank and source receipt owner; this is not payment injection.
        em.AddComponentData(session, new SkirmishEconomyStockComponent {
            FactionId = FactionIdentity.PlayerFactionId, Materials = input.Setup.MaterialsEach,
            Oil = input.Setup.OilEach, Fuel = input.Setup.UsableFuelEach,
            MaterialsCapacity = input.Setup.MaterialsCapacityEach,
            OilCapacity = input.Setup.OilCapacityEach, FuelCapacity = input.Setup.FuelCapacityEach
        });
        em.AddBuffer<SkirmishProductionReservation>(session);
        SkirmishMaterialsService.Initialize(em, session, input.Setup);
        Entity bank = FindPlayerBank(em);

        Entity producerEntity = em.CreateEntity();
        em.AddComponentData(producerEntity, new SkirmishAttemptOwnedComponent {
            SessionId = new FixedString64Bytes(sessionId), StableObjectId = new FixedString64Bytes("fixture.barrack"),
            FactionId = FactionIdentity.PlayerFactionId, IsStructure = 1
        });
        em.AddComponentData(producerEntity, new SkirmishStructureIdentityComponent {
            StructureId = new FixedString64Bytes(SkirmishStructureIds.Barracks), Producer = SkirmishProducerKind.Barracks
        });
        JObject producerFacts = (JObject)input.Descriptor["facts"]["producer"];
        int producerAuthoredMaxHealth = (int)producerFacts["max_health"];
        em.AddComponentData(producerEntity, new UnitHealth { Current = producerAuthoredMaxHealth, Max = producerAuthoredMaxHealth });
        JArray producerFootprint = (JArray)producerFacts["footprint_cells"];
        var producerDefinition = new RuntimeDefinition {
            SourceMetadataComplete = false, Prefab = barrack, DisplayName = input.BarrackDisplayName,
            Description = input.BarrackDescription, MaxHealth = (int)producerFacts["max_health"],
            CreditsCost = (int)producerFacts["credits_cost"], MaterialsCost = (int)producerFacts["materials_cost"],
            FootprintCells = new ActualCore::UnityEngine.Vector2Int((int)producerFootprint[0], (int)producerFootprint[1]),
            ProductionDurationSeconds = (float)producerFacts["construction_duration_seconds"], IsWall = false,
            ProductionSlots = new List<RuntimeDefinition.ProductionSlotDefinition> {
                new RuntimeDefinition.ProductionSlotDefinition {
                    SpawnUnitPrefab = rifle, SpawnUnitSourceKey = new FixedString64Bytes(input.Rifle.name),
                    Quantity = input.RecipeQuantity
                }
            }
        };
        int producerId = 1001;
        string descriptorPath = Path.GetFullPath(args[0]);
        SourceProducerRecipeAuthority authority = SourceProducerRecipeAuthority.ReadExplicitS002RifleFixture(
            descriptorPath, args[1], input.Root, producerDefinition, rifle, input.Setup, input.Transport);
        producerDefinition.SourceRecipeAuthority = authority;
        string transportVariantText = File.ReadAllText(Path.Combine(input.Root, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab"));
        string transportBaseText = File.ReadAllText(Path.Combine(input.Root, "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab"));
        string unitRootText = File.ReadAllText(Path.Combine(input.Root, "Assets/Game/Prefabs/Characters/Unit.prefab"));
        const string transportBaseGuid = "9ce6998c034284d98a89b5badc6e2f24";
        const string unitSourceGuid = "5680e7910ed3741beba6f07116b6b03c";
        Need(SourceProducerRecipeAuthority.MatchesPrefabSource(transportVariantText, transportBaseGuid) &&
             SourceProducerRecipeAuthority.MatchesRootNameOverride(transportVariantText, transportBaseGuid,
                 1972179586304415830L, "Unit_Veh_Helicopter_Transport") &&
             SourceProducerRecipeAuthority.MatchesPrefabSource(transportBaseText, unitSourceGuid) &&
             SourceProducerRecipeAuthority.MatchesRootNameOverride(transportBaseText, unitSourceGuid,
                 2254344950327853479L, "Unit_Veh") &&
             SourceProducerRecipeAuthority.MatchesUnitRoot(unitRootText, 2254344950327853479L),
            "Pinned transport prefab variant chain must resolve its exact inherited root names");
        Need(!SourceProducerRecipeAuthority.MatchesRootNameOverride(transportVariantText, transportBaseGuid,
                 1972179586304415831L, "Unit_Veh_Helicopter_Transport") &&
             !SourceProducerRecipeAuthority.MatchesRootNameOverride(transportVariantText, transportBaseGuid,
                 1972179586304415830L, "Unit_Veh_Helicopter_Attack") &&
             !SourceProducerRecipeAuthority.MatchesPrefabSource(transportVariantText,
                 "00000000000000000000000000000000") &&
             !SourceProducerRecipeAuthority.MatchesRootNameOverride(transportBaseText, unitSourceGuid,
                 2254344950327853479L, "Unit_Veh_Helicopter_Transport") &&
             !SourceProducerRecipeAuthority.MatchesUnitRoot(unitRootText.Replace("m_Name: Unit\n", "m_Name: WrongRoot\n"),
                 2254344950327853479L),
            "Transport variant lineage must reject wrong source GUID, target fileID, override name, and base root name");
        string variantWithDuplicateWrongSource = transportVariantText +
            "\n  m_SourcePrefab: {fileID: 100100000, guid: 00000000000000000000000000000000, type: 3}\n";
        string variantWithDuplicateParent = transportVariantText +
            "\n    m_TransformParent: {fileID: 42}\n";
        string conflictingRootNameOverride =
            "    - target: {fileID: 1972179586304415830, guid: " + transportBaseGuid + ", type: 3}\n" +
            "      propertyPath: m_Name\n      value: Unit_Veh_Helicopter_Attack\n" +
            "      objectReference: {fileID: 0}\n";
        string variantWithConflictingRootName = transportVariantText + "\n" + conflictingRootNameOverride;
        string variantWithDuplicateRootName = transportVariantText + "\n" +
            "    - target: {fileID: 1972179586304415830, guid: " + transportBaseGuid + ", type: 3}\n" +
            "      propertyPath: m_Name\n      value: Unit_Veh_Helicopter_Transport\n" +
            "      objectReference: {fileID: 0}\n";
        Need(!SourceProducerRecipeAuthority.MatchesPrefabSource(variantWithDuplicateWrongSource, transportBaseGuid) &&
             !SourceProducerRecipeAuthority.MatchesPrefabSource(variantWithDuplicateParent, transportBaseGuid) &&
             !SourceProducerRecipeAuthority.MatchesRootNameOverride(variantWithConflictingRootName,
                 transportBaseGuid, 1972179586304415830L, "Unit_Veh_Helicopter_Transport") &&
             !SourceProducerRecipeAuthority.MatchesRootNameOverride(variantWithDuplicateRootName,
                 transportBaseGuid, 1972179586304415830L, "Unit_Veh_Helicopter_Transport"),
            "Transport lineage must reject duplicate source/parent fields and duplicate or conflicting target-name overrides");
        var fixtureScope = new SourceCampSingleProducerFixtureScope(producerId, authority);
        var knownNullTransportScope = new SourceCampKnownNullSingleTransportFixtureScope(
            fixtureScope, input.Transport, null, null);
        Need(knownNullTransportScope.MaxConcurrent == 2 &&
             ReferenceEquals(knownNullTransportScope.TransportPrefab, input.Transport),
            "Known-null transport fixture must bind descriptor-authored concurrency and prefab identity");
        ExpectException<NotSupportedException>(() => new SourceCampKnownNullSingleTransportFixtureScope(
                fixtureScope, input.Transport, null, new object()),
            "Known-null camera and render-bounds provider required",
            "Presentation-free fixture accepted a present render-bounds provider");
        var wrongTransport = SourceUnitPrefab.FromAssetIdentity(input.Transport.name,
            input.Transport.SourceAssetPath, input.Transport.SourceAssetGuid, input.Transport.SourceAssetLocalId + 1);
        ExpectException<NotSupportedException>(() => new SourceCampKnownNullSingleTransportFixtureScope(
                fixtureScope, wrongTransport, null, null),
            "Pinned identity-only transport and exact source-authorized maxConcurrent required for the scoped Rifle fixture",
            "Presentation-free fixture accepted a transport with a different authored fileID");
        Need(!producerDefinition.SourceMetadataComplete && authority.MatchesDefinition(producerDefinition),
            "Fixture authority must bind the exact partial source recipe without promoting normal metadata completeness");
        ExpectException<InvalidOperationException>(() => SourceProducerRecipeAuthority.ReadExplicitS002RifleFixture(
            descriptorPath, new string('0', 64), input.Root, producerDefinition, rifle, input.Setup),
            "Source input hash changed: " + descriptorPath,
            "Fixture authority accepted a descriptor whose explicit SHA-256 was changed");
        Need(!authority.Matches(producerDefinition, 0, rifle, input.RecipeQuantity + 1),
            "Fixture authority accepted a changed recipe quantity");
        var building = new RuntimeBuildingEntity {
            Id = producerId, CombatEntity = producerEntity, HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId, InitialProducerCollectionsKnown = true,
            PendingProductions = new List<SourcePendingProduction>(), Definition = producerDefinition,
            ProducedUnits = new List<Entity>()
        };
        var buildings = new Dictionary<int, RuntimeBuildingEntity> { [producerId] = building };
        var definitions = new[] { producerDefinition };
        var definitionsByPrefab = definitions.ToDictionary(x => x.Prefab);
        ExpectException<NotSupportedException>(() => fixtureScope.RequireRegistry(
            new Dictionary<int, RuntimeBuildingEntity> { [producerId] = building, [producerId + 1] = building },
            definitions, definitionsByPrefab),
            "Single-producer fixture scope does not match the complete current producer registry",
            "Fixture scope accepted an added producer registry row");
        // This one-row configured identity scope is deliberate. Transport is
        // metadata-only here because the original delivery preflight rejects
        // its missing helicopter owner before any spawn/template access.
        var units = new[] { rifle };
        var byKey = units.ToDictionary(x => x.name.ToLowerInvariant(), x => x, StringComparer.Ordinal);
        Need(fixtureScope.HasExactConfiguredUnitIndex(units, byKey),
            "Scoped source authority rejected the exact normalized one-row configured-unit index");
        Need(!fixtureScope.HasExactConfiguredUnitIndex(units,
                new Dictionary<string, SourceUnitPrefab> { [rifle.name] = rifle }),
            "Scoped source authority accepted an original-case configured-unit index key");
        var sameIdentityWrongReference = SourceUnitPrefab.FromAssetIdentity(RifleKey,
            rifle.SourceAssetPath, rifle.SourceAssetGuid, rifle.SourceAssetLocalId);
        Need(!ReferenceEquals(sameIdentityWrongReference, rifle) &&
             !fixtureScope.HasExactConfiguredUnitIndex(units,
                new Dictionary<string, SourceUnitPrefab> { [RifleKey.ToLowerInvariant()] = sameIdentityWrongReference }),
            "Scoped source authority accepted a different prefab reference with matching source identity");
        var recipes = new Dictionary<SourceUnitPrefab, BuildingProductionRecipe> {
            [rifle] = new BuildingProductionRecipe {
                ProducerPrefab = barrack, UnitPrefab = rifle, Quantity = input.RifleRole.SquadMembers,
                MaterialsCost = input.RifleRole.MaterialsCost, CreditsCost = 0, UseProducerGroundExit = false
            }
        };
        var gameplayRequestMetadata = new SourceUnitGameplayRequestMetadataStore(
            new[] { rifle }, new[] { rifle }, new[] { new SourceUnitGameplayRequestMetadata(
            input.CanRequest == 1, input.GameplayMaterialsCost, input.CreditsCost) });
        var production = new BuildingProductionQueueCompositionSystemHelper();
        production.ConfigureUnitProductionMetadataResolver((SourceUnitPrefab prefab, out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata metadata) => {
            if (ReferenceEquals(prefab, rifle)) { metadata = rifleProduction; return true; }
            if (ReferenceEquals(prefab, transport)) { metadata = transportProduction; return true; }
            metadata = default; return false;
        });
        production.ConfigureProductionRecipes(recipes.Values.ToArray());
        BuildingProductionQueueCompositionSystemHelper.TryGetPrefabLocalBoundsDelegate bounds =
            (SourceUnitPrefab prefab, out Bounds value) => { value = default; throw new NotSupportedException("Fixture does not admit prefab bounds; explicit T11 Rifle transport path must not consume bounds"); };
        var productionContext = new BuildingProductionQueueCompositionSystemHelper.QueueContext(
            units, byKey, new BuildingProductionSlotUtilitySystemHelper(), bounds, null,
            (EntityManager manager, out Entity entity) => { entity = Entity.Null; return false; });

        Entity grid = em.CreateEntity();
        // This is an explicit disposable coordinate frame, not the selected
        // S002 operation map. The known-null delivery owner falls back to the
        // origin when this fixture producer has no runtime Instance; this
        // offset maps its lane-adjusted preferred drop outside the authored
        // 28x15 Barrack footprint plus the owner's five-cell reservation.
        const int fixtureGridWidth = 64, fixtureGridHeight = 64;
        int fixtureGridCells = checked(fixtureGridWidth * fixtureGridHeight);
        em.AddComponentData(grid, new GridConfig {
            Width = fixtureGridWidth, Height = fixtureGridHeight, CellSize = 1f,
            Origin = new float3(-40f, 0f, -40f)
        });
        var walkable = em.AddBuffer<GridWalkable>(grid);
        var roads = em.AddBuffer<GridRoad>(grid);
        var sidewalks = em.AddBuffer<GridRoadSidewalk>(grid);
        var dirtRoads = em.AddBuffer<GridRoadDirt>(grid);
        for (int i = 0; i < fixtureGridCells; i++) {
            walkable.Add(new GridWalkable { Value = 1 });
            roads.Add(default); sidewalks.Add(default); dirtRoads.Add(default);
        }
        em.AddComponentData(grid, new DynamicBlockerComponent {
            GridSize = fixtureGridCells, Counts = new NativeArray<int>(fixtureGridCells, Allocator.Persistent),
            Blocked = new NativeBitArray(new bool[fixtureGridCells]),
            FriendlyPassFactionIds = new NativeArray<byte>(fixtureGridCells, Allocator.Persistent),
            StorageEpoch = 1, AppliedStorageEpoch = 1, AppliedRevision = 1,
            AppliedGrid = new GridConfig { Width = fixtureGridWidth, Height = fixtureGridHeight, CellSize = 1f,
                Origin = new float3(-40f, 0f, -40f) }
        });
        em.AddComponentData(grid, new DynamicOccupancyComponent {
            GridSize = fixtureGridCells, Occupied = new NativeBitArray(new bool[fixtureGridCells])
        });
        em.AddComponentData(grid, new PathPoolComponent { Cells = new NativeList<int2>(64, Allocator.Persistent) });
        var fixtureBlockers = em.GetComponentData<DynamicBlockerComponent>(grid);
        var fixtureOccupancy = em.GetComponentData<DynamicOccupancyComponent>(grid);
        Need(walkable.Length == fixtureGridCells &&
             roads.Length == fixtureGridCells && sidewalks.Length == fixtureGridCells && dirtRoads.Length == fixtureGridCells &&
             fixtureBlockers.GridSize == fixtureGridCells && fixtureBlockers.Blocked.IsCreated &&
             fixtureBlockers.Blocked.Length == fixtureGridCells && fixtureBlockers.Counts.IsCreated &&
             fixtureBlockers.Counts.Length == fixtureGridCells && fixtureBlockers.FriendlyPassFactionIds.IsCreated &&
             fixtureBlockers.FriendlyPassFactionIds.Length == fixtureGridCells &&
             fixtureOccupancy.GridSize == fixtureGridCells && fixtureOccupancy.Occupied.IsCreated &&
             fixtureOccupancy.Occupied.Length == fixtureGridCells &&
             em.GetComponentData<PathPoolComponent>(grid).Cells.IsCreated,
            "Synthetic fixture grid navigation/occupancy arrays must exactly match its declared dimensions");
        // Exact current inventory of this disposable source-host manager. This
        // is a fixture-local no-record profile, not a native-world certificate.
        Entity[] fixtureInventory = em.CaptureSourceHostLiveEntityInventory();
        RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(em, true, true, fixtureInventory,
            entity => em.HasComponentNamed(entity, "Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord"));
        var roots = new Dictionary<int, SourceProducerRootFrame> {
            [producerId] = new SourceProducerRootFrame(float3.zero, quaternion.identity, 1f)
        };
        var deliveryWorld = new SourceProductionGroundWorld(em, grid, Entity.Null, buildings, roots);
        int globalQueueLimit = Math.Max(0, input.Setup.InfantryQueuesEach);
        Need(globalQueueLimit > 0, "Compiled S002 infantry queue capacity must be source-positive");
        var delivery = new SourceProductionDeliveryOwner(deliveryWorld, production, productionContext,
            new Dictionary<int, SourceProductionDeliveryRecipe[]> {
                [producerId] = new[] { new SourceProductionDeliveryRecipe(0, rifle, input.RifleRole.SquadMembers, rifleProduction) }
            }, globalQueueLimit, unchecked((uint)input.Setup.Seed), fixtureScope);

        var availability = new SourceCampAvailability(em, session, bank, null, null, map, gameplayRequestMetadata,
            buildings, definitions, definitionsByPrefab, units, byKey, recipes, globalQueueLimit,
            production, productionContext, bounds, null, false, false, fixtureScope);
        Need(!producerDefinition.SourceMetadataComplete,
            "Fixture scope must leave normal RuntimeDefinition completeness false");
        var aria = new SourceAriaPaidRecruitment(em, session, availability, delivery);
        var offer = aria.Read(SkirmishRoleKind.Rifle);
        Need(offer.Present, "Actual source ARIA adapter did not resolve the current S002 Rifle role offer: " + offer.Reason);

        // Exercise the original HUD-facing command helper entry without a Unity
        // view/GameObject. Its fixture context binds the callback to the same
        // SourceCampAvailability semantic owner used by ARIA.
        var hudCommands = new BuildingUiCommandSystemHelper();
        var hudContext = new BuildingUiCommandSystemHelper.Context(
            () => em.GetComponentData<FactionEconomy>(bank).Money,
            () => globalQueueLimit,
            () => 0,
            (int index, out BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry entry) => { entry = default; return false; },
            () => units.Length,
            (int index, out BuildingUiCommandSystemHelper.ConfiguredUnitEntry entry) => {
                if (index != 0) { entry = default; return false; }
                entry = new BuildingUiCommandSystemHelper.ConfiguredUnitEntry(
                    RifleKey, RifleKey, rifle, false, offer.CanRequest, input.GameplayMaterialsCost);
                return true;
            },
            prefab => ReferenceEquals(prefab, rifle),
            null,
            (SourceUnitPrefab prefab, int price, out string required, bool focusProducerOnSuccess) => {
                required = string.Empty;
                var result = availability.RequestUnitSemantic(prefab, price, delivery.TryQueue, 0f);
                if (result.Accepted) return BuildingUiCommandSystemHelper.CampRequestFailure.None;
                return Enum.TryParse(result.SourceReason, out BuildingUiCommandSystemHelper.CampRequestFailure failure)
                    ? failure : BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection;
            },
            () => false, () => false, () => string.Empty, () => 0, () => 0f,
            () => false, () => { }, (int buildingId, int pendingIndex) => false);
        var hudEconomyBefore = em.GetComponentData<FactionEconomy>(bank);
        var hudMaterialsBefore = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
        int hudPendingBefore = building.PendingProductions?.Count ?? 0;
        int hudReservationsBefore = em.GetBuffer<SkirmishProductionReservation>(session).Length;
        if (offer.CanRequest)
        {
            ExpectException<NotSupportedException>(() => hudCommands.TryRequestCampItem(
                    hudContext, rifle, input.GameplayMaterialsCost, out _, false),
                "Source transport mode/geometry not admitted; no payment performed",
                "Original HUD command helper did not reach the shared semantic/delivery owner");
        }
        else
        {
            var hudFailure = hudCommands.TryRequestCampItem(
                hudContext, rifle, input.GameplayMaterialsCost, out _, false);
            Need(hudFailure != BuildingUiCommandSystemHelper.CampRequestFailure.None,
                "Original HUD command helper accepted a currently unavailable source offer");
        }
        Need(em.GetComponentData<FactionEconomy>(bank).Money == hudEconomyBefore.Money &&
             em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current == hudMaterialsBefore.Current &&
             (building.PendingProductions?.Count ?? 0) == hudPendingBefore &&
             em.GetBuffer<SkirmishProductionReservation>(session).Length == hudReservationsBefore,
            "Original HUD command helper changed payment, pending queue, or source reservation state on rejection");

        var economyBefore = em.GetComponentData<FactionEconomy>(bank);
        var materialsBefore = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
        var pendingBefore = building.PendingProductions?.Count ?? 0;
        var reservationsBefore = em.GetBuffer<SkirmishProductionReservation>(session).Length;
        string requestOutcome;
        if (offer.CanRequest)
        {
            ExpectException<NotSupportedException>(() => aria.Request(SkirmishRoleKind.Rifle, 0f),
                "Source transport mode/geometry not admitted; no payment performed",
                "Actual source ARIA route did not reach the source delivery owner's missing-helicopter preflight rejection");
            requestOutcome = "delivery_rejected_missing_helicopter";
        }
        else
        {
            SourceAriaRecruitmentReceipt receipt = aria.Request(SkirmishRoleKind.Rifle, 0f);
            Need(!receipt.Accepted && receipt.ReceiptId == null,
                "Source ARIA semantic owner did not preserve the currently unavailable Rifle request");
            requestOutcome = "source_semantic_rejection:" + receipt.SourceReason;
        }
        var economyAfter = em.GetComponentData<FactionEconomy>(bank);
        var materialsAfter = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
        Need(economyAfter.Money == economyBefore.Money && materialsAfter.Current == materialsBefore.Current &&
             (building.PendingProductions?.Count ?? 0) == pendingBefore &&
             em.GetBuffer<SkirmishProductionReservation>(session).Length == reservationsBefore,
            "Actual source rejection changed payment, pending queue, or source reservation state");

        // Keep the no-helicopter/no-payment rejection above as its own control.
        // This second path binds the explicit presentation-free scope and then
        // lets the actual semantic/payment/delivery/spawn owners decide.
        var knownNullDelivery = new SourceHelicopterDelivery(delivery, knownNullTransportScope);
        delivery.Helicopters = knownNullDelivery;
        float connectedEconomyBefore = em.GetComponentData<FactionEconomy>(bank).Money;
        int connectedReservationsBefore = em.GetBuffer<SkirmishProductionReservation>(session).Length;
        long connectedReceiptId;
        if (connectedEntry == "aria-success")
        {
            SourceAriaRecruitmentReceipt connectedReceipt = aria.Request(SkirmishRoleKind.Rifle, 0f);
            Need(connectedReceipt.Accepted && connectedReceipt.ReceiptId.HasValue,
                "Actual semantic request/delivery owner rejected the source-bound connected fixture request: " + connectedReceipt.SourceReason);
            connectedReceiptId = connectedReceipt.ReceiptId.Value;
        }
        else
        {
            BuildingUiCommandSystemHelper.CampRequestFailure hudConnectedFailure = hudCommands.TryRequestCampItem(
                hudContext, rifle, input.GameplayMaterialsCost, out _, false);
            Need(hudConnectedFailure == BuildingUiCommandSystemHelper.CampRequestFailure.None,
                "Original HUD helper did not accept the source-bound connected fixture request: " + hudConnectedFailure);
            Need(building.PendingProductions != null && building.PendingProductions.Count == 1 &&
                 building.PendingProductions[0].ReceiptId > 0,
                "HUD callback did not produce exactly one actual source receipt/pending item");
            connectedReceiptId = building.PendingProductions[0].ReceiptId;
        }
        Need(connectedReceiptId > 0,
            "Connected source request did not expose a positive actual receipt ID");
        Need(em.GetComponentData<FactionEconomy>(bank).Money <= connectedEconomyBefore &&
             em.GetBuffer<SkirmishProductionReservation>(session).Length == connectedReservationsBefore + 1 &&
             building.PendingProductions != null && building.PendingProductions.Count == 1,
            "Actual source request did not produce its paid reservation and pending item");

        bool connectedSpawned = false;
        Entity connectedActor = Entity.Null;
        float fixtureNow = 0f;
        const float fixtureDelta = 0.25f;
        // Bounded disposable clock advancement; all queue and phase changes are
        // performed by the actual owner. No pending, receipt, payment, or actor
        // state is injected by the fixture.
        for (int tick = 0; tick < 1200 && !connectedSpawned; tick++)
        {
            fixtureNow += fixtureDelta;
            delivery.Advance(fixtureNow, fixtureDelta);
            if (building.ProducedUnits != null)
                foreach (Entity candidate in building.ProducedUnits)
                    if (candidate != Entity.Null && em.Exists(candidate))
                    { connectedActor = candidate; connectedSpawned = true; break; }
        }
        string connectedEvents = string.Join(",", delivery.DeliveryEvents.Select(row => (string)row["kind"]));
        SourcePendingProduction remainingPending = building.PendingProductions?.FirstOrDefault();
        string pendingDiagnostic = remainingPending == null ? "none" :
            "receipt=" + remainingPending.ReceiptId + ",remaining=" + remainingPending.RemainingQuantity +
            ",started=" + remainingPending.StartedAt.ToString(CultureInfo.InvariantCulture) +
            ",ready=" + remainingPending.ReadyAt.ToString(CultureInfo.InvariantCulture) +
            ",transport=" + (remainingPending.TransportPrefab?.name ?? "null") +
            ",mode=" + remainingPending.TransportMode +
            ",arrival=" + remainingPending.TransportArrivalSeconds.ToString(CultureInfo.InvariantCulture) +
            ",hold=" + remainingPending.TransportHoldForNextReadySeconds.ToString(CultureInfo.InvariantCulture) +
            ",concurrency=" + remainingPending.TransportMaxConcurrent +
            ",searchRadius=" + remainingPending.TransportClearDropSearchStartRadius;
        Need(connectedSpawned && connectedActor != Entity.Null && em.Exists(connectedActor),
            "Actual source delivery/spawn owner did not produce a live fixture actor within the bounded source-clock window; events=" +
            connectedEvents + "; active_transport=" + knownNullDelivery.AnyActive +
            "; delivered_members=" + delivery.DeliveredMembers + "; pending=" + pendingDiagnostic +
            "; poisoned=" + delivery.Poisoned);
        Need(em.HasComponent<UnitGrid>(connectedActor) && em.HasComponent<LocalTransform>(connectedActor) &&
             em.HasComponent<UnitSourcePrefabKey>(connectedActor) && em.HasComponent<Faction>(connectedActor),
            "Actual source-spawned actor lacks required current owner output components");

        // Explicitly activate only this disposable manager and invoke the actual
        // manual-order, pathfinding, occupancy, and grid-movement owners. The goal
        // is chosen from current fixture occupancy and source owner inputs; no
        // path/result, grid position, or actor transform is written by the fixture.
        em.AddComponentData(em.CreateEntity(), new RuntimeGameplayStateComponent { PlayRequested = 1, SimulationActive = 1 });
        var endSimulation = new OwnedEndSimulationBuffers();
        em.AddComponentData(em.CreateEntity(), new EndSimulationEntityCommandBufferSystem.Singleton { Owner = endSimulation });
        em.WorldUnmanaged.Time = new TimeData { ElapsedTime = 0d, DeltaTime = 1f / 60f };
        using (var movementOwners = new OriginalGroundOwners(em, generatedFresh: true))
        {
            EntityQuery followQueryBeforeActualPathResult = em.CreateEntityQuery(typeof(UnitPathFollow));
            EntityQuery rangeQueryBeforeActualPathResult = em.CreateEntityQuery(typeof(UnitPathRange));
            int followOrderVersionBeforePath = em.GetComponentOrderVersion<UnitPathFollow>();
            int rangeOrderVersionBeforePath = em.GetComponentOrderVersion<UnitPathRange>();
            long membershipHitsBeforePath = em.MembershipHits;
            long membershipMissesBeforePath = em.MembershipMisses;
            Need(movementOwners.OccupancyPhase(), "Original occupancy owner did not run for the fixture actor");
            UnitGrid movementStartCell = em.GetComponentData<UnitGrid>(connectedActor);
            LocalTransform movementStartTransform = em.GetComponentData<LocalTransform>(connectedActor);
            UnitFootprint movementFootprint = em.GetComponentData<UnitFootprint>(connectedActor);
            var currentOccupancy = em.GetComponentData<DynamicOccupancyComponent>(grid);
            int2 moveGoal = default;
            bool hasMoveGoal = false;
            const int fixtureMoveDistanceCells = 6;
            foreach (int2 direction in new[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) })
            {
                int2 candidate = movementStartCell.Cell + direction * fixtureMoveDistanceCells;
                if (candidate.x < 0 || candidate.y < 0 || candidate.x + movementFootprint.Size.x > fixtureGridWidth ||
                    candidate.y + movementFootprint.Size.y > fixtureGridHeight) continue;
                bool free = true;
                for (int step = 1; step <= fixtureMoveDistanceCells && free; step++)
                for (int y = movementStartCell.Cell.y + direction.y * step;
                     y < movementStartCell.Cell.y + direction.y * step + movementFootprint.Size.y && free; y++)
                for (int x = movementStartCell.Cell.x + direction.x * step;
                     x < movementStartCell.Cell.x + direction.x * step + movementFootprint.Size.x; x++)
                {
                    int index = y * fixtureGridWidth + x;
                    if (walkable[index].Value == 0 || fixtureBlockers.Counts[index] != 0 ||
                        fixtureBlockers.Blocked.IsSet(index) || currentOccupancy.Occupied.IsSet(index))
                    { free = false; break; }
                }
                if (free) { moveGoal = candidate; hasMoveGoal = true; break; }
            }
            Need(hasMoveGoal, "No adjacent fixture cell was clear under the live occupancy and blocker owners");
            var moveResult = new UnitMoveOrderSystem().IssueGroupedManualMoveOrder(
                em, connectedActor, moveGoal, true, false, 0, 0);
            Need(moveResult.Issued && moveResult.PathRequests == 1 && em.HasComponent<UnitPathRequest>(connectedActor),
                "Original manual move-order owner did not issue one ground path request");

            bool appliedPath = false;
            for (int tick = 0; tick < 12 && !appliedPath; tick++)
            {
                em.WorldUnmanaged.Time = new TimeData { ElapsedTime = tick * (1d / 60d), DeltaTime = 1f / 60f };
                Need(movementOwners.PathPhase(), "Original pathfinding owner did not run in explicitly active fixture context");
                appliedPath = em.HasComponent<UnitPathFollow>(connectedActor) && em.HasComponent<UnitPathRange>(connectedActor);
            }
            Need(appliedPath, "Original pathfinding owner did not apply a path for the accepted fixture request");
            int followOrderVersionAfterPath = em.GetComponentOrderVersion<UnitPathFollow>();
            int rangeOrderVersionAfterPath = em.GetComponentOrderVersion<UnitPathRange>();
            long membershipHitsAfterPath = em.MembershipHits;
            long membershipMissesAfterPath = em.MembershipMisses;
            string pathMembershipEvidence = "follow_version=" + followOrderVersionBeforePath + ">" + followOrderVersionAfterPath +
                ",range_version=" + rangeOrderVersionBeforePath + ">" + rangeOrderVersionAfterPath +
                ",precreated_follow_query_count=" + followQueryBeforeActualPathResult.CalculateEntityCount() +
                ",precreated_range_query_count=" + rangeQueryBeforeActualPathResult.CalculateEntityCount() +
                ",membership_hits=" + membershipHitsBeforePath + ">" + membershipHitsAfterPath +
                ",membership_misses=" + membershipMissesBeforePath + ">" + membershipMissesAfterPath;
            UnitPathFollow appliedFollow = em.GetComponentData<UnitPathFollow>(connectedActor);
            UnitPathRange appliedRange = em.GetComponentData<UnitPathRange>(connectedActor);
            var appliedPool = em.GetComponentData<PathPoolComponent>(grid).Cells;
            Need(appliedRange.Length > 0 && appliedRange.Start >= 0 &&
                 (long)appliedRange.Start + appliedRange.Length <= appliedPool.Length,
                "Original path owner applied a range outside its actual path pool");
            int2[] observedPath = new int2[appliedRange.Length];
            for (int i = 0; i < observedPath.Length; i++) observedPath[i] = appliedPool[appliedRange.Start + i];
            int movementTicks = 0;
            const int maximumMovementTicks = 600;
            int movementGridQueryCount = em.CreateEntityQuery(typeof(GridConfig)).CalculateEntityCount();
            int movementEndSingletonCount = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<EndSimulationEntityCommandBufferSystem.Singleton>() },
                Options = EntityQueryOptions.IncludeSystems
            }).CalculateEntityCount();
            int followBeforeMovement = em.CreateEntityQuery(typeof(UnitPathFollow)).CalculateEntityCount();
            int rangeBeforeMovement = em.CreateEntityQuery(typeof(UnitPathRange)).CalculateEntityCount();
            Need(em.HasComponent<UnitPathFollow>(connectedActor) && em.HasComponent<UnitPathRange>(connectedActor),
                "Actual path was not attached to the connected actor before the first grid-movement owner call");
            bool movementOwnerInvoked = false;
            bool movementObserved = false;
            bool movementOrderCompleted = false;
            string movementStopReason = "bounded_update_limit";
            var movementObservations = new JArray();
            for (int tick = 0; tick < maximumMovementTicks; tick++)
            {
                em.WorldUnmanaged.Time = new TimeData { ElapsedTime = (12 + tick) * (1d / 60d), DeltaTime = 1f / 60f };
                bool occupancyRan = movementOwners.OccupancyPhase();
                Need(occupancyRan, "Original occupancy owner did not run in explicitly active fixture context");
                UnitGrid cellBeforeOwner = em.GetComponentData<UnitGrid>(connectedActor);
                LocalTransform transformBeforeOwner = em.GetComponentData<LocalTransform>(connectedActor);
                bool ran = movementOwners.MovementPhase();
                Need(ran, "Original grid-movement owner did not run in explicitly active fixture context: grid=" + movementGridQueryCount +
                    ",follow_before_movement=" + followBeforeMovement + ",range_before_movement=" + rangeBeforeMovement +
                    ",pool=" + em.CreateEntityQuery(typeof(PathPoolComponent)).CalculateEntityCount() +
                    ",occupancy=" + em.CreateEntityQuery(typeof(DynamicOccupancyComponent)).CalculateEntityCount() +
                    ",roads=" + em.CreateEntityQuery(typeof(GridRoad)).CalculateEntityCount() +
                    ",sidewalks=" + em.CreateEntityQuery(typeof(GridRoadSidewalk)).CalculateEntityCount() +
                    ",dirt=" + em.CreateEntityQuery(typeof(GridRoadDirt)).CalculateEntityCount() +
                    ",end_singleton_include_systems=" + movementEndSingletonCount + "," + MovementSystemState(movementOwners, em, connectedActor) + "," + pathMembershipEvidence);
                movementOwnerInvoked |= ran;
                movementOwners.EndSimulationPhase();
                movementTicks++;
                UnitGrid cellAfterOwner = em.GetComponentData<UnitGrid>(connectedActor);
                LocalTransform transformAfterOwner = em.GetComponentData<LocalTransform>(connectedActor);
                bool followAfterOwner = em.HasComponent<UnitPathFollow>(connectedActor);
                bool rangeAfterOwner = em.HasComponent<UnitPathRange>(connectedActor);
                bool targetAfterOwner = em.HasComponent<UnitTarget>(connectedActor);
                bool manualOrderAfterOwner = em.HasComponent<ManualMoveOrderTag>(connectedActor);
                movementObserved |= !cellAfterOwner.Cell.Equals(movementStartCell.Cell) ||
                    !transformAfterOwner.Position.Equals(movementStartTransform.Position);
                movementObservations.Add(new JObject {
                    ["tick"] = tick, ["positive_delta_seconds"] = 1f / 60f, ["movement_owner_ran"] = ran,
                    ["occupancy_owner_ran"] = occupancyRan,
                    ["cell_before"] = new JArray(cellBeforeOwner.Cell.x, cellBeforeOwner.Cell.y),
                    ["cell_after"] = new JArray(cellAfterOwner.Cell.x, cellAfterOwner.Cell.y),
                    ["position_before"] = new JArray(transformBeforeOwner.Position.x, transformBeforeOwner.Position.y, transformBeforeOwner.Position.z),
                    ["position_after"] = new JArray(transformAfterOwner.Position.x, transformAfterOwner.Position.y, transformAfterOwner.Position.z),
                    ["path_follow_after"] = followAfterOwner, ["path_range_after"] = rangeAfterOwner,
                    ["unit_target_after"] = targetAfterOwner, ["manual_order_after"] = manualOrderAfterOwner,
                    ["end_simulation_playback_invoked"] = true
                });
                if (!followAfterOwner && !rangeAfterOwner && !targetAfterOwner && !manualOrderAfterOwner)
                {
                    movementOrderCompleted = true;
                    movementStopReason = "original_path_and_manual_order_cleanup";
                    break;
                }
                if (!followAfterOwner || !rangeAfterOwner)
                {
                    movementStopReason = "path_cleared_before_order_termination";
                    break;
                }
            }
            UnitGrid movementEndCell = em.GetComponentData<UnitGrid>(connectedActor);
            LocalTransform movementEndTransform = em.GetComponentData<LocalTransform>(connectedActor);
            string pathCells = string.Join("|", observedPath.Select(cell => cell.x.ToString(CultureInfo.InvariantCulture) + ":" + cell.y.ToString(CultureInfo.InvariantCulture)));
            Need(movementOwnerInvoked && movementObserved,
                "Original grid-movement owner produced no observed actor displacement within its bounded fixture steps: owner_updates=" + movementTicks +
                ",start_cell=" + movementStartCell.Cell + ",goal=" + moveGoal + ",end_cell=" + movementEndCell.Cell +
                ",start_position=" + movementStartTransform.Position + ",end_position=" + movementEndTransform.Position +
                ",path_cells=" + pathCells + ",path_index=" + (em.HasComponent<UnitPathFollow>(connectedActor) ? em.GetComponentData<UnitPathFollow>(connectedActor).PathIndex.ToString(CultureInfo.InvariantCulture) : "absent") +
                ",range_start=" + (em.HasComponent<UnitPathRange>(connectedActor) ? em.GetComponentData<UnitPathRange>(connectedActor).Start.ToString(CultureInfo.InvariantCulture) : "absent") +
                ",range_length=" + (em.HasComponent<UnitPathRange>(connectedActor) ? em.GetComponentData<UnitPathRange>(connectedActor).Length.ToString(CultureInfo.InvariantCulture) : "absent") +
                ",current_speed=" + em.GetComponentData<UnitVehicleKinematics>(connectedActor).CurrentSpeed +
                ",first_owner_observation=" + (movementObservations.Count == 0 ? "none" : movementObservations[0].ToString(Newtonsoft.Json.Formatting.None)) +
                ",last_owner_observation=" + (movementObservations.Count == 0 ? "none" : movementObservations[movementObservations.Count - 1].ToString(Newtonsoft.Json.Formatting.None)) +
                ",order_completed=" + movementOrderCompleted + ",stop_reason=" + movementStopReason);
            Need(em.GetComponentData<RuntimeGameplayStateComponent>(em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)).GetSingletonEntity()).SimulationActive == 1,
                "Fixture-only scheduler activation changed during owner execution");
            string startPosition = string.Join(",", new[] { movementStartTransform.Position.x, movementStartTransform.Position.y, movementStartTransform.Position.z }.Select(x => x.ToString("R", CultureInfo.InvariantCulture)));
            string endPosition = string.Join(",", new[] { movementEndTransform.Position.x, movementEndTransform.Position.y, movementEndTransform.Position.z }.Select(x => x.ToString("R", CultureInfo.InvariantCulture)));
            Console.WriteLine("[SourceS002RifleAriaPreflightFixture] result=Passed scope=DisposableS002OneProducerFixture hud_entry=BuildingUiCommandSystemHelper.TryRequestCampItem aria_offer=present aria_can_request=" + offer.CanRequest + " request_outcome=" + requestOutcome + " connected_entry=" + connectedEntry + " connected_receipt=" + connectedReceiptId + " connected_outcome=actual_paid_receipt_delivery_and_spawn connected_actor=" + connectedActor + " movement_outcome=actual_manual_order_pathfinding_and_grid_movement movement_start=" + movementStartCell.Cell + " movement_start_position=" + startPosition + " movement_goal=" + moveGoal + " movement_end=" + movementEndCell.Cell + " movement_end_position=" + endPosition + " movement_ticks=" + movementTicks + " observed_path_cells=" + observedPath.Length + " path_cells=" + pathCells + " path_follow_index_after_apply=" + appliedFollow.PathIndex + " path_follow_after_owner=" + em.HasComponent<UnitPathFollow>(connectedActor) + " path_range_after_owner=" + em.HasComponent<UnitPathRange>(connectedActor) + " movement_order_completed=" + movementOrderCompleted + " movement_stop_reason=" + movementStopReason + " owner_tick_observations=" + movementObservations.Count + " owner_tick_observations_json=" + movementObservations.ToString(Newtonsoft.Json.Formatting.None) + " shared_semantic_owner=SourceCampAvailability.RequestUnitSemantic payment_owner=actual_source_queue source_metadata_complete=false fixture_producers=1 compiled_setup_structures=" + input.Setup.Structures.Length + " producer_health=" + producerAuthoredMaxHealth + " producer_factory_footprint=unverified partial_prefab_entity_materialized=true no_record_scope=exact_disposable_manager_inventory native_baker_prefab=false omitted=UnitSelectionHitbox,UnitDisplayInfo,UnitAnimationOrder,UnitAnimationGroundOffset,visual/model/VFX references faction_preinit_value_unspecified=true idle_hash_runtime_parity_unclaimed=true native_ui=false native_admission=false movement_fixture=true");
        }
    }

    static Entity FindPlayerBank(EntityManager em)
    {
        using (var q = em.CreateEntityQuery(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent)))
            foreach (Entity entity in q.ToEntityArray(Allocator.Temp))
                if (em.GetComponentData<FactionEconomy>(entity).FactionId == FactionIdentity.PlayerFactionId) return entity;
        throw new InvalidDataException("Original SkirmishMaterialsService did not create player bank");
    }

    static void Main(string[] args)
    {
        try { Run(args); }
        catch (Exception error)
        {
            Console.Error.WriteLine("[SourceS002RifleAriaPreflightFixture] result=Failed " + error);
            throw;
        }
    }
}
