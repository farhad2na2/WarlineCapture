#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    public static class S002InitialWorldAuthorityTests
    {
        static int checks;
        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++; Debug.Log("[InitialWorldAuthority] PASS " + message);
        }
        public static void RunFocusedValidation()
        {
            checks = 0; var previous = World.DefaultGameObjectInjectionWorld;
            using (var world = new World("InitialWorldAuthorityFocusedValidation"))
            {
                var ecb = world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
                Check(!(bool)S002InitialWorldEcbAuthority.Capture(ecb, false)["supported"], "unproven synchronization refuses before native ECB content reads");
                var fresh = S002InitialWorldEcbAuthority.Capture(ecb, true);
                Check((bool)fresh["supported"] && (int)fresh["pending_count"] == 0 && (bool)fresh["producer_handle_is_default"], "actual empty initialized ECB owner observed");
                ecb.CreateCommandBuffer();
                Check((bool)S002InitialWorldEcbAuthority.Capture(ecb, true)["supported"], "empty pending buffer remains explicitly represented");
                ecb.CreateCommandBuffer().CreateEntity();
                var pending = S002InitialWorldEcbAuthority.Capture(ecb, true);
                Check(!(bool)pending["supported"] && (int)pending["pending_count"] == 2, "nonempty structural commands refuse without playback or invented results");
                Check(UnitPathfindingSystem.HasPendingInitialWorldWork(world), "absent path owner is not invented as empty");
                using (var query = world.EntityManager.CreateEntityQuery(typeof(Game.Components.UnitGrid)))
                {
                    Check(S002InitialWorldQueryFilterAuthority.BelongsToWorld(query, world), "actual query owner matches captured World");
                    using (var foreign = new World("InitialWorldForeignQueryOwner"))
                        Check(!S002InitialWorldQueryFilterAuthority.BelongsToWorld(query, foreign), "foreign World cannot normalize or copy this query");
                    query.SetChangedVersionFilter(ComponentType.ReadOnly<Game.Components.UnitGrid>());
                    var setter = typeof(EntityQuery).GetMethod("SetChangedFilterRequiredVersion", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (setter == null) throw new InvalidOperationException("Installed filter setter metadata missing.");
                    setter.Invoke(query, new object[] { 123u });
                    var filter = S002InitialWorldQueryFilterAuthority.Capture(query);
                    Check((bool)filter["supported"] && (uint)filter["required_change_version"] == 123u && (bool)filter["has_filter"],
                        "actual mutable query change version captured independently of system LastSystemVersion");
                }
                bool refused = false;
                try { S002InitialWorldBundleCapture.RequireSafeBoundary(world, false); }
                catch (InvalidOperationException) { refused = true; }
                Check(refused, "read admission rejects before any global census");
                refused = false;
                try { S002InitialWorldManagedAuthority.Capture(world); }
                catch (InvalidOperationException) { refused = true; }
                Check(refused, "missing actual managed registry is not reconstructed from defaults");
            }
            string directory = Path.Combine(Path.GetTempPath(), "initial-world-hash-controls-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var method = typeof(S002InitialWorldBundleCapture).GetMethod("HashRegularFile", BindingFlags.Static | BindingFlags.NonPublic);
                string path = Path.Combine(directory, "source.cs"); File.WriteAllBytes(path, new byte[] { 97, 98, 99 });
                Check((string)method.Invoke(null, new object[] { path, 3L }) == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", "bounded regular source bytes hash exactly");
                bool refused = false;
                try { method.Invoke(null, new object[] { path, 2L }); }
                catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException) { refused = true; }
                Check(refused, "oversize source rejected before reading");
                File.WriteAllBytes(path, Array.Empty<byte>()); refused = false;
                try { method.Invoke(null, new object[] { path, 3L }); }
                catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException) { refused = true; }
                Check(refused, "zero-length source rejected before open");
            }
            finally { Directory.Delete(directory, true); }
            Check(ReferenceEquals(previous, World.DefaultGameObjectInjectionWorld), "default World remains unchanged");
            Debug.Log("[InitialWorldAuthority] result=Passed checks=" + checks + " nativeMissionQualified=0");
        }
        // Sole launch owner remains the checked existing harness. Focused test Worlds
        // are disposed first; no scene, bank, outcome or troop order is fabricated.
        public static void RunNativeStartupCaptureAndExit()
        {
            if (!S002InitialWorldBundleCapture.Enabled) throw new InvalidOperationException("Explicit initial-world capture mode required.");
            S002InitialWorldBoundaryWitness.ConfigureFromEnvironment();
            S002InitialWorldEntityExportTests.RunFocusedValidation();
            RunFocusedValidation();
            SkirmishS002AriaRunHarness.RunFocusedAriaAndExit();
        }
    }
}
#endif
