#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    internal static class S002StartupAllocatorCapture
    {
        internal static readonly JArray Events = new JArray();
        internal static readonly JObject Artifacts = new JObject();
        static readonly Dictionary<Entity, JObject> Pending = new Dictionary<Entity, JObject>();
        static string path, sessionId;
        internal static bool Completed;

        static S002StartupAllocatorCapture()
        {
            SkirmishScenarioSpawnSystem.ObserveStartupAllocator = Observe;
            SkirmishScenarioSpawnSystem.ObserveStartupPlacementResult = ObserveResult;
        }

        internal static string OutputPath(string requested)
        {
            if (path != null)
            {
                if (!string.Equals(path, Path.GetFullPath(requested), StringComparison.Ordinal))
                    throw new InvalidOperationException("Startup capture destination changed");
                return path;
            }
            path = SkirmishOfflineDataExporter.ValidateOutputDirectory(requested);
            Directory.CreateDirectory(path);
            return path;
        }

        static void Observe(SkirmishScenarioSpawnSystem.StartupAllocatorObservation value)
        {
            string requested = Environment.GetEnvironmentVariable("WARLINE_S002_OFFLINE_STARTUP_CAPTURE");
            if (Completed || string.IsNullOrEmpty(requested)) return;
            var em = value.EntityManager;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (sessions.CalculateEntityCount() != 1) throw new InvalidOperationException("Ambiguous observed startup session");
            var session = sessions.GetSingleton<SkirmishExpandedSessionComponent>();
            if (session.CatalogId.ToString() != "S002") return;
            string currentSession = session.SessionId.ToString();
            if (sessionId != null && sessionId != currentSession) throw new InvalidOperationException("Mixed startup sessions");
            sessionId = currentSession;
            OutputPath(requested);
            int cells = checked(value.Grid.Width * value.Grid.Height);
            if (cells <= 0 || cells > SkirmishOfflineDataExporter.MaximumCells ||
                !value.Blocked.IsCreated || !value.Occupied.IsCreated || !value.Reserved.IsCreated ||
                value.Blocked.Length != cells || value.Occupied.Length != cells || value.Reserved.Length != cells)
                throw new InvalidOperationException("Incomplete actual allocator masks");
            var census = new JArray();
            using var units = em.CreateEntityQuery(typeof(UnitGrid), typeof(UnitFootprint));
            using var existing = units.ToEntityArray(Allocator.Temp);
            foreach (Entity actor in existing)
            {
                if (actor == value.Actor) continue;
                var cell = em.GetComponentData<UnitGrid>(actor).Cell;
                var size = em.GetComponentData<UnitFootprint>(actor).Size;
                census.Add(new JObject
                {
                    ["entity_index"] = actor.Index, ["entity_version"] = actor.Version,
                    ["cell"] = Pair(cell), ["raw_footprint"] = Pair(size),
                    ["stable_id"] = em.HasComponent<SkirmishAttemptOwnedComponent>(actor)
                        ? em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).StableObjectId.ToString() : null
                });
            }
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(value.Actor);
            var record = new JObject
            {
                ["ordinal"] = Events.Count, ["session_id"] = currentSession,
                ["stable_id"] = owner.StableObjectId.ToString(), ["seed"] = value.Seed,
                ["grid"] = JObject.Parse(JsonUtility.ToJson(value.Grid)),
                ["requested_cell"] = Pair(value.RequestedCell),
                ["requested_world"] = new JArray(value.RequestedWorld.x, value.RequestedWorld.y, value.RequestedWorld.z),
                ["footprint"] = Pair(value.Footprint), ["air"] = value.Air, ["typed"] = value.Typed,
                ["random_state_before_find"] = value.RandomState,
                ["initial_search_radius"] = 12, ["surface_correction_radius"] = 48,
                ["blocked_artifact"] = WriteMask(value.Blocked), ["effective_occupied_artifact"] = WriteMask(value.Occupied),
                ["reservation_census_native_query_order"] = census,
                ["reserved_u8_sha256"] = Hash(Bytes(value.Reserved)),
                ["pre_placement_actor"] = SkirmishBakedActorSnapshotExporter.CaptureActor(em, value.Actor),
                ["placement_result"] = JValue.CreateNull()
            };
            if (Pending.ContainsKey(value.Actor)) throw new InvalidOperationException("Duplicate allocator observation");
            Pending.Add(value.Actor, record);
            Events.Add(record);
        }

        static void ObserveResult(EntityManager em, Entity actor, bool success)
        {
            if (!Pending.TryGetValue(actor, out JObject record)) return;
            record["placement_result"] = new JObject
            {
                ["success"] = success,
                ["scope"] = "AfterNativePlacement_BeforeBirthStampAndRosterOverlay",
                ["actor"] = SkirmishBakedActorSnapshotExporter.CaptureActor(em, actor)
            };
            Pending.Remove(actor);
        }
        internal static void RequireComplete(int expected)
        {
            if (Events.Count != expected || Pending.Count != 0)
                throw new InvalidOperationException("Incomplete startup allocator transaction: " + Events.Count + "/" + expected + " pending=" + Pending.Count);
        }
        static JArray Pair(int2 value) => new JArray(value.x, value.y);
        static byte[] Bytes(NativeBitArray bits)
        {
            var bytes = new byte[bits.Length];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = bits.IsSet(i) ? (byte)1 : (byte)0;
            return bytes;
        }
        static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static string WriteMask(NativeBitArray bits)
        {
            byte[] bytes = Bytes(bits); string hash = Hash(bytes), name = "allocator-" + hash + ".u8";
            if (Artifacts[name] == null)
            {
                File.WriteAllBytes(Path.Combine(path, name), bytes);
                Artifacts[name] = new JObject { ["bytes"] = bytes.Length, ["encoding"] = "u8", ["sha256"] = hash };
            }
            return name;
        }
    }
}
#endif
