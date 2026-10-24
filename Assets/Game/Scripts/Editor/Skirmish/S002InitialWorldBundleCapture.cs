#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    // One observer on the existing startup barrier. Never completes pending work,
    // constructs missing gameplay state, or substitutes a later snapshot.
    internal static class S002InitialWorldBundleCapture
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("WARLINE_S002_INITIAL_WORLD_BUNDLE") == "1";


        internal static void RequireSafeBoundary(World world, bool originalStructuralSyncObserved)
        {
            if (!Enabled || !originalStructuralSyncObserved || UnitPathfindingSystem.HasPendingInitialWorldWork(world))
                throw new InvalidOperationException("Initial-world unsafe readers require an original synchronized roster tail and no pending path work.");
            foreach (var managed in world.Systems)
                if (managed is EntityCommandBufferSystem owner)
                {
                    var ecb = S002InitialWorldEcbAuthority.Capture(owner, originalStructuralSyncObserved);
                    if (!(ecb["supported"] is bool supported && supported))
                        throw new S002InitialWorldBoundaryAdmissionException(owner.GetType().FullName, ecb);
                }
        }

        internal static void Write(EntityManager em, Entity session, SkirmishExpandedSessionComponent value, JObject startup, string directory, int rosterOrderBefore, int rosterOrderAfter)
        {
            if (!Enabled) return;
            bool originalStructuralSyncObserved = rosterOrderBefore != rosterOrderAfter;
            RequireSafeBoundary(em.World, originalStructuralSyncObserved);
            // Capture actual Unity clock epochs once at the admitted initial barrier;
            // world elapsed time is a distinct authority and is never substituted.
            var engineClock = EncodeEngineClocks(Time.time, Time.timeAsDouble, Time.unscaledTime,
                Time.unscaledTimeAsDouble, Time.deltaTime, Time.unscaledDeltaTime, Time.timeScale,
                Time.realtimeSinceStartupAsDouble);
            var missing = new JArray();
            var consumerRequirements = new JArray("compiled_read_domain", "logical_identity_policy", "scheduler_backend");
            var nativeRequirements = new JArray("same_run_terminal_admission", "owning_compiler_source_loaded_assembly_join");
            var nativeExactness = new JArray();
            if (value.CatalogId.ToString() != "S002" || value.SpawnComplete == 0 ||
                value.Phase != Game.Skirmish.Contracts.SkirmishSessionPhase.Playing)
                throw new InvalidOperationException("Initial-world export requires the successful S002 Playing frontier.");
            Dictionary<string, object> pathState, occupancyState, managedState;
            using (S002InitialWorldBoundaryWitness.BeginPrivateCaptureBudget())
            {
                pathState = UnitPathfindingSystem.CaptureInitialWorldAuthority(em.World);
                occupancyState = DynamicOccupancyRebuildSystem.CaptureInitialWorldAuthority(em.World);
                managedState = S002InitialWorldManagedAuthority.Capture(em.World);
            }
            var witness = S002InitialWorldBoundaryWitness.Capture(em.World);
            var artifacts = (JObject)startup["artifacts"].DeepClone();
            var entry = em.World.GetExistingSystemManaged<S002InitialWorldSimulationEntryWitnessSystem>();
            bool boundaryValid = witness.TryGetValue("boundary_valid", out var valid) && valid is bool flag && flag &&
                entry != null && entry.Entries(value.SessionId.ToString()) == 0 &&
                witness.TryGetValue("spawn_frame", out var spawnFrame) && spawnFrame is int frame && frame == Time.frameCount;
            if (!boundaryValid) missing.Add("ActualSpawnRosterPreFirstSimulationBoundaryNotProven");
            if (!pathState.TryGetValue("supported", out var pathSupported) || !(pathSupported is bool pathOk && pathOk))
                missing.Add("PathfindingPrivateStateUnsupported:" + (pathState.TryGetValue("reason", out var pr) ? pr : "unknown"));
            if (!occupancyState.TryGetValue("supported", out var occSupported) || !(occSupported is bool occOk && occOk))
                missing.Add("OccupancyPrivateStateUnsupported:" + (occupancyState.TryGetValue("reason", out var or) ? or : "unknown"));
            if (!boundaryValid || !(pathSupported is bool ps && ps) || !(occSupported is bool os && os))
                throw new InvalidOperationException("Initial-world private/boundary admission failed before global component or native content copy.");
            var boundary = S002InitialWorldEntityExport.CaptureValue(witness, "boundary.witness", missing, directory, artifacts);
            // Private exporters reject pending worker state; do not complete jobs to
            // manufacture a synchronized initial frontier.
            var systems = new JObject
            {
                ["UnitPathfindingSystem"] = S002InitialWorldEntityExport.CaptureValue(pathState, "systems.UnitPathfindingSystem", missing, directory, artifacts),
                ["DynamicOccupancyRebuildSystem"] = S002InitialWorldEntityExport.CaptureValue(occupancyState, "systems.DynamicOccupancyRebuildSystem", missing, directory, artifacts)
            };
            systems["ManagedBuildingGameplay"] = S002InitialWorldEntityExport.CaptureValue(
                managedState, "systems.ManagedBuildingGameplay", missing, directory, artifacts);
            var world = S002InitialWorldEntityExport.Capture(em, directory, artifacts);
            if (world["missing"] is JArray worldMissing)
                foreach (var reason in worldMissing)
                {
                    if ((string)reason["path"] == "world/entity_allocator" &&
                        (string)reason["reason"] == "future_allocation_free_list_versions_not_exported")
                        nativeExactness.Add(reason.DeepClone());
                    else missing.Add(reason.DeepClone());
                }
            var scheduler = Scheduler(em.World, missing, directory, artifacts, originalStructuralSyncObserved);
            var authority = SourceAuthority(missing);
            var startupPath = Path.Combine(directory, "startup-observation.json");
            if (!File.Exists(startupPath)) throw new InvalidOperationException("Same-boundary startup member must already exist.");
            var report = new JObject
            {
                ["schema_version"] = 1,
                ["kind"] = "InitialGroundWorldBundle",
                ["source_authority"] = authority,
                ["boundary"] = new JObject
                {
                    ["phase"] = "InitializationSystemGroup.AfterSkirmishRosterProjection.BeforeSimulation",
                    ["roster_entity_order_before"] = rosterOrderBefore, ["roster_entity_order_after"] = rosterOrderAfter,
                    ["original_structural_sync_observed"] = originalStructuralSyncObserved,
                    ["engine_clock"] = engineClock,
                    ["world_sequence"] = em.World.SequenceNumber.ToString(),
                    ["engine_frame"] = Time.frameCount,
                    ["session_entity"] = session.Index + ":" + session.Version,
                    ["session_id"] = value.SessionId.ToString(),
                    ["world_elapsed_seconds_bits"] = BitConverter.DoubleToInt64Bits(em.World.Time.ElapsedTime).ToString("x16"),
                    ["world_delta_seconds_bits"] = BitConverter.ToInt32(BitConverter.GetBytes(em.World.Time.DeltaTime), 0).ToString("x8"),
                    ["valid"] = boundaryValid, ["simulation_entries_after_spawn"] = entry != null ? (JToken)entry.Entries(value.SessionId.ToString()) : JValue.CreateNull(),
                    ["witness"] = boundary
                },
                ["world"] = world,
                ["systems"] = systems,
                ["scheduler"] = scheduler,
                ["startup_observation"] = new JObject
                {
                    ["path"] = "startup-observation.json", ["bytes"] = new FileInfo(startupPath).Length,
                    ["sha256"] = SkirmishOfflineDataExporter.HashFile(startupPath),
                    ["engine_frame"] = startup["engine_frame"].DeepClone(),
                    ["world_elapsed_seconds"] = startup["world_elapsed_seconds"].DeepClone(),
                    ["session_id"] = startup["session_id"].DeepClone()
                },
                ["artifacts"] = artifacts,
                ["closure"] = new JObject
                {
                    ["complete"] = false,
                    ["capture_complete"] = missing.Count == 0 && (bool)world["census_complete"],
                    ["capture_missing"] = missing,
                    ["consumer_requirements"] = consumerRequirements,
                    ["native_requirements"] = nativeRequirements,
                    ["native_exactness_limitations"] = nativeExactness,
                    ["missing"] = missing.DeepClone(),
                    ["autonomous_qualified"] = false,
                    ["future_native_inputs"] = false
                }
            };
            // A global census does not establish the compiled consumer model. Its
            // independent admission must bind this immutable bundle and worker build;
            // source/terminal admission and native allocator exactness stay separate.
            File.WriteAllText(Path.Combine(directory, "initial-world-bundle.capture.json"), report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[S002InitialGroundWorldBundle] result=Captured closureComplete=0 missing=" + missing.Count + " autonomousQualified=0");
        }

        // BEGIN PURE INITIAL ENGINE CLOCK CODEC
        internal static JObject EncodeEngineClocks(float time, double timeAsDouble, float unscaledTime,
            double unscaledTimeAsDouble, float deltaTime, float unscaledDeltaTime, float timeScale,
            double realtimeSinceStartupAsDouble)
        {
            return new JObject
            {
                ["source"] = "UnityEngine.Time",
                ["time_f32_bits"] = ClockFloatBits(time),
                ["time_as_double_f64_bits"] = ClockDoubleBits(timeAsDouble),
                ["unscaled_time_f32_bits"] = ClockFloatBits(unscaledTime),
                ["unscaled_time_as_double_f64_bits"] = ClockDoubleBits(unscaledTimeAsDouble),
                ["delta_time_f32_bits"] = ClockFloatBits(deltaTime),
                ["unscaled_delta_time_f32_bits"] = ClockFloatBits(unscaledDeltaTime),
                ["time_scale_f32_bits"] = ClockFloatBits(timeScale),
                ["realtime_since_startup_as_double_f64_bits"] = ClockDoubleBits(realtimeSinceStartupAsDouble),
                ["realtime_role"] = "initial_performance_clock_epoch",
                ["future_native_clock_rows"] = false
            };
        }
        static string ClockFloatBits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value), 0).ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
        static string ClockDoubleBits(double value) => BitConverter.ToUInt64(BitConverter.GetBytes(value), 0).ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
        // END PURE INITIAL ENGINE CLOCK CODEC

        static JObject SourceAuthority(JArray missing)
        {
            string path = Environment.GetEnvironmentVariable("WARLINE_S002_INITIAL_WORLD_SOURCE_AUTHORITY");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new InvalidOperationException("Frozen current-source authority manifest is required before capture.");
            string expected = Environment.GetEnvironmentVariable("WARLINE_S002_INITIAL_WORLD_SOURCE_AUTHORITY_SHA256");
            var info = new FileInfo(path);
            if (!Path.IsPathRooted(path) || !info.Exists || (info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0 ||
                info.Length <= 0 || info.Length > 32L * 1024 * 1024 || expected == null || expected.Length != 64 ||
                expected.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
                throw new InvalidOperationException("Positive bounded non-link source manifest and exact SHA-256 are required.");
            long length = info.Length, modified = info.LastWriteTimeUtc.Ticks; var bytes = new byte[checked((int)length)];
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!stream.CanSeek || stream.Length != length) throw new InvalidOperationException("Source manifest changed before read.");
                int offset = 0; while (offset < bytes.Length)
                { int read = stream.Read(bytes, offset, bytes.Length - offset); if (read == 0) throw new EndOfStreamException("Source manifest truncated."); offset += read; }
                if (stream.ReadByte() != -1 || stream.Length != length) throw new InvalidOperationException("Source manifest grew during read.");
            }
            info.Refresh();
            string actual; using (var sha = SHA256.Create()) actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            if (!info.Exists || info.Length != length || info.LastWriteTimeUtc.Ticks != modified || actual != expected)
                throw new InvalidOperationException("Source manifest identity changed or SHA-256 mismatch.");
            var authority = JObject.Parse(new UTF8Encoding(false, true).GetString(bytes));
            if (authority["source_pins"] is not JObject pins || !pins.Properties().Any())
                throw new InvalidOperationException("Source authority must pin the current source closure.");
            string project = Path.GetFullPath(Directory.GetCurrentDirectory()) + Path.DirectorySeparatorChar;
            foreach (var pin in pins.Properties())
            {
                string physical = Path.GetFullPath(Path.Combine(project, pin.Name));
                if (Path.IsPathRooted(pin.Name) || !physical.StartsWith(project, StringComparison.Ordinal) ||
                    pin.Value.Type != JTokenType.String || ((string)pin.Value).Length != 64 ||
                    !File.Exists(physical) || HashRegularFile(physical, 32L * 1024 * 1024) != (string)pin.Value)
                    throw new InvalidOperationException("Source changed across initial-world capture: " + pin.Name);
            }
            var loaded = new JObject();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.GetName().Name, StringComparer.Ordinal))
            {
                var name = assembly.GetName().Name;
                if (!(name.StartsWith("Game.", StringComparison.Ordinal) || name == "Unity.Entities" ||
                      name == "Unity.Collections" || name == "Unity.Mathematics" || name == "Unity.Transforms")) continue;
                if (string.IsNullOrEmpty(assembly.Location) || !File.Exists(assembly.Location))
                    throw new InvalidOperationException("Owning assembly image missing: " + name);
                loaded[name] = new JObject { ["sha256"] = HashRegularFile(assembly.Location, 256L * 1024 * 1024),
                    ["mvid"] = assembly.ManifestModule.ModuleVersionId.ToString("D") };
            }
            authority["loaded_assemblies"] = loaded;
            authority["manifest_sha256"] = actual;
            authority["native_admission"] = new JObject { ["status"] = "PendingSameRunTerminalReceipt" };
            authority["compiler_consumption_proven"] = false;
            return authority;
        }


        static string HashRegularFile(string path, long maximum)
        {
            var info = new FileInfo(path);
            if (!info.Exists || (info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0 || info.Length <= 0 || info.Length > maximum)
                throw new InvalidOperationException("Positive bounded regular source/assembly file required: " + path);
            long length = info.Length, modified = info.LastWriteTimeUtc.Ticks;
            byte[] buffer = new byte[65536]; byte[] digest;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var hash = SHA256.Create())
            {
                if (!stream.CanSeek || stream.Length != length) throw new InvalidOperationException("Source/assembly length changed before hashing.");
                long remaining = length;
                while (remaining > 0)
                {
                    int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read == 0) throw new EndOfStreamException("Source/assembly truncated.");
                    hash.TransformBlock(buffer, 0, read, buffer, 0); remaining -= read;
                }
                if (stream.ReadByte() != -1 || stream.Length != length) throw new InvalidOperationException("Source/assembly grew during hashing.");
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0); digest = hash.Hash;
            }
            info.Refresh();
            if (!info.Exists || info.Length != length || info.LastWriteTimeUtc.Ticks != modified)
                throw new InvalidOperationException("Source/assembly metadata changed across hashing.");
            return BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();
        }

        static JObject Scheduler(World world, JArray missing, string directory, JObject artifacts, bool originalStructuralSyncObserved)
        {
            var groups = new JArray(); var ecbs = new JArray();
            foreach (var managed in world.Systems)
                if (managed is EntityCommandBufferSystem owner)
                {
                    var ecb = S002InitialWorldEcbAuthority.Capture(owner, originalStructuralSyncObserved);
                    if (!(ecb["supported"] is bool supported && supported)) missing.Add("PendingECBUnsupported:" + owner.GetType().FullName);
                    ecbs.Add(S002InitialWorldEntityExport.CaptureValue(ecb, "scheduler.ecb." + owner.GetType().FullName, missing, directory, artifacts));
                }
            foreach (var managed in world.Systems)
            {
                if (managed is not ComponentSystemGroup group) continue;
                using var handles = group.GetAllSystems(Allocator.Temp);
                var order = new JArray();
                for (int i = 0; i < handles.Length; i++)
                {
                    var typeIndex = world.Unmanaged.GetSystemTypeIndex(handles[i]);
                    order.Add(new JObject { ["ordinal"] = i, ["source_type"] = TypeManager.GetSystemName(typeIndex).ToString() });
                }
                groups.Add(new JObject { ["source_type"] = group.GetType().FullName,
                    ["enabled"] = group.Enabled, ["ordered_systems"] = order });
            }
            return new JObject { ["groups"] = groups, ["entity_command_buffer_owners"] = ecbs, ["future_completion_policy_derived"] = false };
        }
    }
}
#endif
