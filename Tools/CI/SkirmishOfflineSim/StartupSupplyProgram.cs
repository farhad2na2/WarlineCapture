using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Components;
using Game.Configs;
using Unity.Entities;

// Calls the unchanged native grant methods. Preflight stores are explicit inputs;
// earlier production ticks and later spending are not reconstructed here.
static class StartupSupplyProgram
{
    internal static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    static int Main(string[] args)
    {
        try
        {
            var file = new System.IO.FileInfo(args[0]);
            Require(file.Exists && (file.Attributes & (System.IO.FileAttributes.ReparsePoint | System.IO.FileAttributes.Directory)) == 0 && file.Length is > 0 and <= 134217728, "Bounded regular supply payload required");
            using var doc = JsonDocument.Parse(System.IO.File.ReadAllBytes(args[0]));
            var root = doc.RootElement;
            var journal = root.GetProperty("journal");
            var stages = journal.GetProperty("supply_stages").EnumerateArray().ToArray();
            Require(stages.Length == 2 && stages[0].GetProperty("stage").GetString() == "before-preflight" && stages[1].GetProperty("stage").GetString() == "after-grant", "Exactly preflight and after-grant stages required");
            var input = stages[0].GetProperty("all_enabled_stores_native_query_order").EnumerateArray().ToArray();
            var expected = stages[1].GetProperty("all_enabled_stores_native_query_order").EnumerateArray().ToArray();
            Require(input.Length is > 0 and <= 65536 && expected.Length == input.Length, "Bounded complete storage census required");
            string sessionId = journal.GetProperty("session_id").GetString();
            Require(!string.IsNullOrEmpty(sessionId), "Session identity required");
            var em = new EntityManager();
            var session = new Entity { Index = -1, Version = 1 };
            em.Add(session, new SkirmishExpandedSessionComponent { SessionId = sessionId });
            em.Add(session, new Game.Runtime.SkirmishSharedBuildingsReady());
            var rowEntities = new List<Entity>();
            var ownedStable = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in input)
            {
                var e = EntityOf(row.GetProperty("entity"));
                Require(!rowEntities.Contains(e), "Duplicate storage entity");
                rowEntities.Add(e);
                var s = row.GetProperty("storage").Deserialize<BuildingResourceStorageComponent>(Options);
                AssertStorage(s, row.GetProperty("storage"));
                foreach (var f in typeof(BuildingResourceStorageComponent).GetFields())
                    if (f.FieldType == typeof(float)) Require(float.IsFinite((float)f.GetValue(s)), "Finite storage values required");
                Require(s.OilStorageCapacity >= 0 && s.FuelStorageCapacity >= 0 && s.OwnerFactionId <= 2, "Supported storage capacities and factions required");
                em.Add(e, s);
                if (row.TryGetProperty("attempt", out var attempt))
                {
                    string stable = row.GetProperty("stable_id").GetString();
                    Require(!string.IsNullOrEmpty(stable) && ownedStable.Add(stable), "Unique owned stable identity required");
                    em.Add(e, new SkirmishAttemptOwnedComponent { SessionId = attempt.GetString(), StableObjectId = stable });
                }
                if (row.GetProperty("fabrication").ValueKind != JsonValueKind.Null) em.Add(e, new MaterialFabricationComponent());
            }
            Require(em.OwnedStores.Length == 8, "Actual S002 eight owned stores required");
            var setup = root.GetProperty("setup").Deserialize<SkirmishResolvedSetup>(Options);
            Require(setup.OilEach >= 0 && setup.UsableFuelEach >= 0, "Nonnegative authored supply required");
            bool initialized = Game.Runtime.SkirmishStartingSupplyService.Initialize(em, session, setup);
            Require(initialized && em.HasComponent<Game.Runtime.SkirmishSharedSupplyInitialized>(session), "Native grant preflight rejected inputs");
            var derivedOrder = em.LastGrantArray ?? throw new InvalidOperationException("Native grant order missing");
            foreach (var stage in stages)
            {
                var order = stage.GetProperty("native_grant_order_assertion").EnumerateArray().Select(EntityOf).ToArray();
                Require(order.SequenceEqual(derivedOrder), "Derived grant order differs from native assertion");
            }
            var output = new List<object>();
            for (int i = 0; i < input.Length; i++)
            {
                var e = rowEntities[i];
                Require(EntityOf(expected[i].GetProperty("entity")).Equals(e), "Storage census identity/order changed");
                var actual = em.GetComponentData<BuildingResourceStorageComponent>(e);
                AssertStorage(actual, expected[i].GetProperty("storage"));
                foreach (var key in new[] { "fabrication", "production_profile", "attempt", "stable_id" })
                {
                    bool a = input[i].TryGetProperty(key, out var before), b = expected[i].TryGetProperty(key, out var after);
                    Require(a == b && (!a || JsonNode.DeepEquals(JsonNode.Parse(before.GetRawText()), JsonNode.Parse(after.GetRawText()))), "Grant changed non-storage input " + key);
                }
                output.Add(new { entity = e, storage = actual });
            }
            // A repeated call must not grant again or advance a version.
            var once = rowEntities.Select(e => em.GetComponentData<BuildingResourceStorageComponent>(e)).ToArray();
            Require(Game.Runtime.SkirmishStartingSupplyService.Initialize(em, session, setup), "Repeated initialized grant failed");
            for (int i = 0; i < once.Length; i++) AssertStorage(em.GetComponentData<BuildingResourceStorageComponent>(rowEntities[i]), JsonSerializer.SerializeToElement(once[i], Options));
            Console.WriteLine(JsonSerializer.Serialize(new { result = "Passed", scope = "SourceLinkedStartingGrantTransaction_FromRecordedPreflightInputs", storage_assertions = input.Length, owned_stores = em.OwnedStores.Length, grant_order = derivedOrder, stores = output, earlier_production_derived = false, physical_startup_parity = false, full_mission_initialized = false, terminal_parity = false, player_ready = false }, Options));
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { result = "Failed", error = e.Message, physical_startup_parity = false, player_ready = false }));
            return 1;
        }
    }
    static Entity EntityOf(JsonElement v) => new() { Index = v.GetProperty("index").GetInt32(), Version = v.GetProperty("version").GetInt32() };
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static void AssertStorage(BuildingResourceStorageComponent value, JsonElement expected)
    {
        Require(expected.EnumerateObject().Count() == typeof(BuildingResourceStorageComponent).GetFields().Length, "Exact storage field set required");
        foreach (var field in typeof(BuildingResourceStorageComponent).GetFields())
        {
            var v = expected.GetProperty(field.Name);
            bool same = field.FieldType == typeof(float) ? BitConverter.SingleToInt32Bits((float)field.GetValue(value)) == BitConverter.SingleToInt32Bits(v.GetSingle())
                : field.FieldType == typeof(uint) ? (uint)field.GetValue(value) == v.GetUInt32()
                : Convert.ToInt64(field.GetValue(value)) == v.GetInt64();
            Require(same, "Derived storage differs: " + field.Name);
        }
    }
}

namespace Unity.Collections { public enum Allocator { Temp } }
namespace Unity.Mathematics
{
    public static class math { public static float max(float a, float b) => a > b ? a : b; public static float min(float a,float b) => a < b ? a : b; }
}
namespace Unity.Entities
{
    public interface IComponentData { }
    public struct Entity : IEquatable<Entity>
    {
        public int Index, Version;
        public bool Equals(Entity e) => Index == e.Index && Version == e.Version;
        public override bool Equals(object o) => o is Entity e && Equals(e);
        public override int GetHashCode() => HashCode.Combine(Index,Version);
    }
    public sealed class EntityArray : IDisposable, System.Collections.Generic.IEnumerable<Entity>
    {
        readonly Entity[] values; readonly EntityManager em; public EntityArray(Entity[] v, EntityManager owner) { values=v; em=owner; }
        public Entity[] ToArray() => em.LastGrantArray = (Entity[])values.Clone();
        public System.Collections.Generic.IEnumerator<Entity> GetEnumerator() => ((System.Collections.Generic.IEnumerable<Entity>)values).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => values.GetEnumerator(); public void Dispose() { }
    }
    public sealed class EntityQuery : IDisposable
    {
        readonly EntityManager em; public EntityQuery(EntityManager e) { em=e; }
        public EntityArray ToEntityArray(Unity.Collections.Allocator allocator) => new(em.OwnedStores, em);
        public void Dispose() { }
    }
    public sealed class EntityManager
    {
        readonly Dictionary<Entity, Dictionary<Type,object>> values = new();
        public readonly List<Entity> QueryOrder = new();
        public Entity[] LastGrantArray;
        public Entity[] OwnedStores => QueryOrder.Where(e=>HasComponent<BuildingResourceStorageComponent>(e)&&HasComponent<SkirmishAttemptOwnedComponent>(e)).ToArray();
        public void Add<T>(Entity e,T value)
        {
            if(!values.TryGetValue(e,out var row)) { values.Add(e,row=new()); QueryOrder.Add(e); }
            row.Add(typeof(T),value);
        }
        public bool HasComponent<T>(Entity e) => values.TryGetValue(e,out var row)&&row.ContainsKey(typeof(T));
        public T GetComponentData<T>(Entity e) => (T)values[e][typeof(T)];
        public void SetComponentData<T>(Entity e,T value) { if(!HasComponent<T>(e)) throw new InvalidOperationException("Missing component"); values[e][typeof(T)]=value; }
        public void AddComponent<T>(Entity e) where T:new() => Add(e,new T());
        public EntityQuery CreateEntityQuery(params Type[] types)
        {
            if(types.Length!=2||types[0]!=typeof(BuildingResourceStorageComponent)||types[1]!=typeof(SkirmishAttemptOwnedComponent)) throw new InvalidOperationException("Unsupported query");
            return new(this);
        }
    }
}
namespace Game.Components
{
    public struct SkirmishAttemptOwnedComponent { public string SessionId,StableObjectId; }
    public struct SkirmishExpandedSessionComponent { public string SessionId; }
    public struct MaterialFabricationComponent { }
}
namespace Game.Configs { public struct SkirmishResolvedSetup { public int OilEach,UsableFuelEach; } }
namespace Game.Runtime { public struct SkirmishSharedBuildingsReady { } public struct SkirmishSharedSupplyInitialized { } }
