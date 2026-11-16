#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Game.Components;
using Game.Rendering.Contracts;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Opt-in capture of the original cached water mask at the first S073
    // Preparing-phase placement predicate. This composes with existing observers.
    [InitializeOnLoad]
    internal static class S073NativeStartupWaterMaskSnapshot
    {
        const string OutputKey = "WARLINE_S073_NATIVE_STARTUP_WATER_MASK_OUTPUT";
        const string ExpectedIdentity = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        const string ExpectedContent = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        const string SourceScene = "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity";

        static readonly string[] SourcePins =
        {
            "Assets/Game/Scripts/Editor/Skirmish/S073NativeStartupWaterMaskSnapshot.cs",
            "Assets/Game/Scripts/Systems/S002PhysicalStartupObservation.cs",
            "Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.RoadValidation.cs",
            "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
            "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
            "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredBuildingCache.cs",
            "Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs",
            "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs",
            "Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
            "Assets/Game/Scenes/Match.unity",
            SourceScene
        };

        static bool done;
        static readonly Action<S002PhysicalStartupObservation.OriginalPlacementFacts> OriginalPlacementHandler = OnOriginalPlacement;

        static S073NativeStartupWaterMaskSnapshot()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputKey))) return;
            EnsureSubscription();
            EditorApplication.update += Pump;
        }

        // OriginalPlacement is a mutable Action field rather than an event.
        // Append only and re-append after later owner assignments.
        static void EnsureSubscription()
        {
            if (done) return;
            var chain = S002PhysicalStartupObservation.OriginalPlacement;
            if (chain == null || !chain.GetInvocationList().Contains(OriginalPlacementHandler))
                S002PhysicalStartupObservation.OriginalPlacement += OriginalPlacementHandler;
        }

        static void Pump()
        {
            if (done)
            {
                EditorApplication.update -= Pump;
                return;
            }
            EnsureSubscription();
            if (!EditorApplication.isPlaying) return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated ||
                    !TryReadS073Identity(world.EntityManager, out SkirmishMatchState match, out JObject identity) ||
                    match.Phase != SkirmishPhase.Playing) return;
                WriteNotObservedDiagnostic(world, world.EntityManager, identity);
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        static void OnOriginalPlacement(S002PhysicalStartupObservation.OriginalPlacementFacts facts)
        {
            if (done || facts == null || facts.Roads == null) return;
            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                EntityManager em = world.EntityManager;
                if (!TryReadS073Identity(em, out SkirmishMatchState match, out JObject identity) ||
                    match.Phase != SkirmishPhase.Preparing) return;

                GridConfig grid = facts.Grid;
                if (!IsExpectedGrid(grid) || facts.Buildings == null || !facts.Buildings.IsReady)
                    throw new InvalidOperationException("Complete original S073 pre-spawn grid/cache facts required");

                long entityOrderBefore = em.EntityOrderVersion;
                bool[] mask = facts.Roads.GetWater();
                if (mask == null || mask.Length != checked(grid.Width * grid.Height))
                    throw new InvalidOperationException("Original S073 cached water mask is unavailable or incomplete");

                IRenderBoundsPresentation provider = RenderBoundsGateway.Provider;
                if (provider == null || provider.GetType().FullName != "Game.Rendering.RenderBoundsPresentation+Reader")
                    throw new InvalidOperationException("Original S073 RenderBounds water provider required");
                JObject pins = HashPins();

                byte[] bytes = new byte[mask.Length];
                int waterCells = 0;
                for (int i = 0; i < mask.Length; i++)
                {
                    bytes[i] = mask[i] ? (byte)1 : (byte)0;
                    if (mask[i]) waterCells++;
                }

                if (em.EntityOrderVersion != entityOrderBefore || !ReferenceEquals(provider, RenderBoundsGateway.Provider) ||
                    !JToken.DeepEquals(pins, HashPins()))
                    throw new InvalidOperationException("S073 startup water observation changed ECS structure, provider, or pinned sources");

                string output = OutputPath();
                string maskPath = Path.ChangeExtension(output, ".water.bin");
                if (File.Exists(maskPath)) throw new InvalidOperationException("Fresh create-new S073 startup water mask output required");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using (var maskFile = new FileStream(maskPath, FileMode.CreateNew, FileAccess.Write))
                    maskFile.Write(bytes, 0, bytes.Length);

                var result = new JObject
                {
                    ["kind"] = "S073NativeStartupWaterMaskObservation",
                    ["schema_version"] = 1,
                    ["scope"] = "OriginalPreparingPhasePlacementCallback_CachedRoadOwnerWaterMask_NotReadinessOrMissionAcceptance",
                    ["authority"] = "S002PhysicalStartupObservation.OriginalPlacementFacts.Roads.GetWater",
                    ["unity_version"] = Application.unityVersion,
                    ["world_name"] = world.Name,
                    ["world_sequence"] = world.SequenceNumber.ToString(),
                    ["session_id"] = match.SessionId.ToString(),
                    ["scenario_index"] = 3,
                    ["seed"] = 104729,
                    ["phase"] = match.Phase.ToString(),
                    ["operation_map_id"] = "opmap.skirmish.industrial_basin",
                    ["source_identity_hash"] = ExpectedIdentity,
                    ["source_content_hash"] = ExpectedContent,
                    ["physical_source_validated"] = true,
                    ["request_id"] = null,
                    ["request_binding_available"] = false,
                    ["request_binding_reason"] = "OriginalPlacementFacts does not expose BuildingRuntimeSpawnRequest identity",
                    ["frame"] = Time.frameCount,
                    ["placement_origin"] = new JArray(facts.Origin.x, facts.Origin.y),
                    ["placement_footprint"] = new JArray(facts.Footprint.x, facts.Footprint.y),
                    ["placement_effective_rect"] = new JArray(facts.EffectiveRect.x, facts.EffectiveRect.y, facts.EffectiveRect.width, facts.EffectiveRect.height),
                    ["cache_owner_runtime_identity"] = RuntimeHelpers.GetHashCode(facts.Roads),
                    ["provider_type"] = provider.GetType().AssemblyQualifiedName,
                    ["provider_assembly_path"] = provider.GetType().Assembly.Location,
                    ["provider_assembly_sha256"] = HashFile(provider.GetType().Assembly.Location),
                    ["grid"] = new JObject
                    {
                        ["width"] = grid.Width,
                        ["height"] = grid.Height,
                        ["cell_size"] = grid.CellSize,
                        ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z)
                    },
                    ["mask_path"] = maskPath,
                    ["mask_encoding"] = "uint8-bool01-row-major-xfast",
                    ["mask_cells"] = bytes.Length,
                    ["water_cells"] = waterCells,
                    ["mask_sha256"] = HashBytes(bytes),
                    ["entity_order_version_before"] = entityOrderBefore,
                    ["entity_order_version_after"] = em.EntityOrderVersion,
                    ["source_pins_sha256"] = pins,
                    ["campaign_gameplay_state_copied"] = false,
                    ["mission_ready"] = false,
                    ["source_fit_certified"] = false,
                    ["normal_input_playthrough"] = false,
                    ["player_readiness"] = false
                };
                WriteOutput(output, result);
                done = true;
                Debug.Log("[S073NativeStartupWaterMask] result=Recorded frame=" + Time.frameCount +
                    " cells=" + bytes.Length + " waterCells=" + waterCells + " path=" + output);
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        static bool TryReadS073Identity(EntityManager em, out SkirmishMatchState match, out JObject identity)
        {
            match = default;
            identity = null;
            World world = em.World;
            if (world == null || !world.IsCreated || !ReferenceEquals(world, World.DefaultGameObjectInjectionWorld)) return false;
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matches.CalculateEntityCount() != 1) return false;
            match = matches.GetSingleton<SkirmishMatchState>();
            if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.StartupFailure != SkirmishStartupFailureCode.None)
                return false;

            using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            if (maps.CalculateEntityCount() != 1) return false;
            Entity mapEntity = maps.GetSingletonEntity();
            ActiveOperationMapComponent active = em.GetComponentData<ActiveOperationMapComponent>(mapEntity);
            OperationMapMetadataComponent metadata = em.GetComponentData<OperationMapMetadataComponent>(mapEntity);
            if (active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                !metadata.Blob.IsCreated || metadata.PhysicalSourceValidated == 0 ||
                metadata.Blob.Value.SourceIdentityHash.ToString() != ExpectedIdentity ||
                metadata.Blob.Value.SourceContentHash.ToString() != ExpectedContent) return false;

            identity = new JObject { ["session_id"] = match.SessionId.ToString(), ["phase"] = match.Phase.ToString() };
            return !string.IsNullOrEmpty(match.SessionId.ToString());
        }

        static void WriteNotObservedDiagnostic(World world, EntityManager em, JObject identity)
        {
            string output = OutputPath();
            var result = new JObject
            {
                ["kind"] = "S073NativeStartupWaterMaskDiagnostic",
                ["schema_version"] = 1,
                ["status"] = "NotObserved",
                ["diagnostic_reason"] = "NoPreparingPhaseOriginalPlacementCallbackBeforePlaying",
                ["scope"] = "OptInStartupWaterObserverDiagnostic_NotCaptureOrReadiness",
                ["unity_version"] = Application.unityVersion,
                ["world_name"] = world.Name,
                ["world_sequence"] = world.SequenceNumber.ToString(),
                ["session_id"] = identity["session_id"],
                ["scenario_index"] = 3,
                ["seed"] = 104729,
                ["phase"] = SkirmishPhase.Playing.ToString(),
                ["operation_map_id"] = "opmap.skirmish.industrial_basin",
                ["source_identity_hash"] = ExpectedIdentity,
                ["source_content_hash"] = ExpectedContent,
                ["physical_source_validated"] = true,
                ["source_pins_sha256"] = HashPins(),
                ["campaign_gameplay_state_copied"] = false,
                ["source_fit_certified"] = false,
                ["mission_ready"] = false,
                ["normal_input_playthrough"] = false,
                ["player_readiness"] = false
            };
            WriteOutput(output, result);
            done = true;
            Debug.Log("[S073NativeStartupWaterMask] result=NotObserved reason=" +
                (string)result["diagnostic_reason"] + " path=" + output);
        }

        static string OutputPath()
        {
            string output = Path.GetFullPath(Environment.GetEnvironmentVariable(OutputKey));
            if (!output.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(output))
                throw new InvalidOperationException("Fresh /private/tmp create-new S073 startup water output required");
            return output;
        }

        static void WriteOutput(string output, JObject result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
        }

        static bool IsExpectedGrid(GridConfig grid) => grid.Width == 2048 && grid.Height == 1024 &&
            grid.CellSize == 1f && grid.Origin.x == 0f && grid.Origin.y == 0f && grid.Origin.z == 0f;

        static JObject HashPins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var hashes = new JObject();
            foreach (string relative in SourcePins)
            {
                string path = Path.Combine(root, relative);
                if (!File.Exists(path)) throw new FileNotFoundException("Required S073 startup water source pin missing", relative);
                hashes[relative] = HashFile(path);
            }
            return hashes;
        }

        static void Fail(Exception error)
        {
            done = true;
            EditorApplication.update -= Pump;
            Debug.LogException(error);
            Debug.LogError("[S073NativeStartupWaterMask] result=Failed " + error.Message);
        }

        static string HashFile(string path) => HashBytes(File.ReadAllBytes(path));

        static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
