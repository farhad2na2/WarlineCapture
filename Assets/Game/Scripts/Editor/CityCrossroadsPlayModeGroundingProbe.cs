#if UNITY_EDITOR
namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Game.Components;
    using Game.Composition;
    using Game.Configs;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Runs City Crossroads in Play Mode and proves, with live entities, that the E0.1 correction holds:
    /// units stand on the corrected ground rather than floating above it, and the in-match view of the
    /// northern base / civic area matches the owner's original complaint framing.
    ///
    /// <para>Everything E0.1 has measured so far came from baked data and Editor cameras. This closes that
    /// loop against a running match. It reuses the launch sequence already proven by the E0.3 stress probe
    /// (SessionState across the domain reload, an EditorApplication.update stage machine, then
    /// <see cref="SkirmishLaunchProjection"/> to queue, enter and begin the match).</para>
    /// </summary>
    [InitializeOnLoad]
    public static class CityCrossroadsPlayModeGroundingProbe
    {
        private const string Key = "Warline.CityCrossroadsPlayProbe";
        private const string EvidenceDirectory = CityCrossroadsShelfGeometryAudit.EvidenceDirectory;
        private const int MapSeed = 104729;

        /// <summary>A unit further than this above the surface beneath it is standing on air.</summary>
        private const float GroundingTolerance = 0.60f;

        private static int stage;
        private static double next;
        private static double deadline;

        private readonly struct PlayCapture
        {
            public PlayCapture(string name, Vector3 position, Vector3 target)
            {
                Name = name;
                Position = position;
                Target = target;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
        }

        /// <summary>
        /// Gameplay-pitch framings of the areas in the owner's original in-match screenshot: the northern
        /// base join with the statue and market stall, the civic square, and the east shelf.
        /// </summary>
        private static readonly PlayCapture[] Captures =
        {
            new("north-join", new Vector3(986f, 34f, 648f), new Vector3(986f, 2f, 700f)),
            new("north-statue", new Vector3(1012f, 16f, 676f), new Vector3(997f, 4f, 702f)),
            new("civic-square", new Vector3(1033f, 34f, 458f), new Vector3(1033f, 2f, 510f)),
            new("east-shelf", new Vector3(1072f, 34f, 570f), new Vector3(1072f, 2f, 621f))
        };

        static CityCrossroadsPlayModeGroundingProbe()
        {
            if (SessionState.GetBool(Key, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Warline/Skirmish/Launch E0.1 City Crossroads Grounding Probe")]
        public static void Launch()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching the E0.1 grounding probe.");

            SessionState.SetBool(Key, true);
            stage = 0;
            next = 0;
            deadline = 0;
            Directory.CreateDirectory(EvidenceDirectory);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0d)
                deadline = now + 420d;
            if (now > deadline)
            {
                Finish($"[CityCrossroadsPlayProbe] result=Failed reason=timeout stage={stage}");
                return;
            }

            if (now < next)
                return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return;
                EntityManager em = world.EntityManager;

                if (stage == 0)
                {
                    GameLocalization.SetLocale("en", false);
                    if (!SkirmishLaunchProjection.IsShellReadyToEnterMatch(em))
                    {
                        next = now + 0.5d;
                        return;
                    }

                    if (!TryQueueCityCrossroads(em))
                        return;
                    stage = 1;
                    next = now + 2d;
                    return;
                }

                if (!SkirmishLaunchProjection.TryGet(em, out _, out SkirmishMatchState match))
                    return;

                if (stage == 1)
                {
                    SkirmishLaunchProjection.TryEnterMatch(em);
                    SkirmishLaunchProjection.TryBeginGameplayIfLoaded();
                    SkirmishLaunchProjection.TryRequestPlay(em);
                    int units = CountUnits(em);

                    // Before the map streams in, a flat-equivalent surface stands in for the real bake, and
                    // every cell reads 0. Measuring against that would prove nothing about the rebake, so
                    // wait for the baked blob to replace it.
                    bool baked = HasBakedSurface(em);
                    if (!baked || units == 0)
                    {
                        if (now > deadline - 120d)
                        {
                            Debug.LogWarning(
                                $"[CityCrossroadsPlayProbe] stillWaiting phase={match.Phase} units={units} " +
                                $"bakedSurface={baked}");
                        }

                        next = now + 0.5d;
                        return;
                    }

                    Debug.Log(
                        $"[CityCrossroadsPlayProbe] reachedMatch scenario={match.ScenarioIndex} " +
                        $"phase={match.Phase} seed={match.Seed} units={units} bakedSurface=1");
                    stage = 2;
                    next = now + 5d;
                    return;
                }

                if (stage == 2)
                {
                    CaptureAll();
                    stage = 3;
                    next = now + 3d;
                    return;
                }

                if (stage == 3)
                {
                    string marker = MeasureGrounding(em, match);
                    string ledge = ProbeResidualLedge(em);
                    Debug.Log(ledge);
                    Finish(marker);
                    return;
                }
            }
            catch (Exception exception)
            {
                Finish($"[CityCrossroadsPlayProbe] result=Failed reason=exception {exception}");
            }
        }

        private static bool TryQueueCityCrossroads(EntityManager em)
        {
            QuickGameConfig config = QuickGameConfig.Defaults;
            config.MapSeed = MapSeed;
            config.ScenarioIndex = SkirmishPresetConfig.CityCrossroadsScenarioIndex;

            if (!SkirmishLaunchProjection.TryGet(em, out _, out SkirmishMatchState match) ||
                match.ScenarioIndex != config.ScenarioIndex)
            {
                if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                    return false;
                if (!SkirmishLaunchProjection.TryQueue(em, config))
                    return false;
                if (!SkirmishLaunchProjection.TryGet(em, out _, out match))
                    return false;
            }

            if (!SkirmishLaunchProjection.TryEnterMatch(em))
                return false;

            Debug.Log(
                $"[CityCrossroadsPlayProbe] queued scenario={match.ScenarioIndex} seed={match.Seed} " +
                $"phase={match.Phase}");
            return true;
        }

        /// <summary>True once the streamed bake has replaced the flat-equivalent stand-in surface.</summary>
        private static bool HasBakedSurface(EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;
            Entity entity = query.GetSingletonEntity();
            if (em.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity))
                return false;
            if (!em.HasComponent<MapSurfaceRuntimeBakedBlobTag>(entity))
                return false;
            MapSurfaceComponent surface = em.GetComponentData<MapSurfaceComponent>(entity);
            return surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated;
        }

        private static string DescribeSurfaceProvenance(EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            if (query.CalculateEntityCount() != 1)
                return "absent";
            Entity entity = query.GetSingletonEntity();
            bool flat = em.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity);
            bool baked = em.HasComponent<MapSurfaceRuntimeBakedBlobTag>(entity);
            return flat ? "flat-equivalent" : baked ? "baked" : "untagged";
        }

        private static int CountUnits(EntityManager em)
        {
            using var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<LocalTransform>());
            return query.CalculateEntityCount();
        }

        private static void CaptureAll()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[CityCrossroadsPlayProbe] no main camera; skipping captures.");
                return;
            }

            // Synchronous render-texture readback rather than ScreenCapture.CaptureScreenshot: the latter
            // writes at end of frame, by which point the gameplay camera rig has already reset the transform
            // we set, so the saved frame would not show the area we aimed at.
            for (int i = 0; i < Captures.Length; i++)
            {
                PlayCapture definition = Captures[i];
                camera.transform.position = definition.Position;
                camera.transform.rotation = Quaternion.LookRotation(
                    (definition.Target - definition.Position).normalized,
                    Vector3.up);

                string path = Path.Combine(EvidenceDirectory, $"play-{definition.Name}.png");
                long size = CaptureCameraNow(camera, path);
                Debug.Log($"[CityCrossroadsPlayProbe] capture={path} bytes={size}");
            }
        }

        private static long CaptureCameraNow(Camera camera, string path)
        {
            const int width = 1600;
            const int height = 900;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                name = "CityCrossroadsPlayCapture"
            };
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                for (int i = 0; i < 3; i++)
                    camera.Render();
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }

            return new FileInfo(path).Length;
        }

        /// <summary>
        /// Compares every live unit's world Y against the runtime surface directly beneath it. This is the
        /// live counterpart to the baked-data agreement check: entities must rest on the corrected ground.
        /// </summary>
        private static string MeasureGrounding(EntityManager em, SkirmishMatchState match)
        {
            using var surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            if (surfaceQuery.CalculateEntityCount() != 1)
                return "[CityCrossroadsPlayProbe] result=Failed reason=noSurfaceSingleton";

            MapSurfaceComponent surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
                return "[CityCrossroadsPlayProbe] result=Failed reason=noSurfaceBlob";

            using var unitQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<LocalTransform>());
            using var entities = unitQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            var report = new StringBuilder();
            report.AppendLine("# City Crossroads live grounding (Play Mode)");
            report.AppendLine();
            report.AppendLine($"- Scenario index: {match.ScenarioIndex} (City Crossroads)");
            report.AppendLine($"- Seed: {match.Seed}, phase: {match.Phase}");
            report.AppendLine($"- Live units measured: {entities.Length}");
            report.AppendLine();
            report.AppendLine("| unit | world position | cell | surface height | unitY - surface |");
            report.AppendLine("|---:|---|---|---:|---:|");

            float worst = 0f;
            string worstDetail = "none";
            int measured = 0;
            int floating = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                // Aircraft legitimately hold altitude; only ground units prove the surface correction.
                if (em.HasComponent<UnitAirComponent>(entities[i]))
                    continue;

                LocalTransform transform = em.GetComponentData<LocalTransform>(entities[i]);
                float3 position = transform.Position;
                var cell = new int2(
                    (int)math.floor((position.x - surface.GridOrigin.x) / surface.CellSize),
                    (int)math.floor((position.z - surface.GridOrigin.z) / surface.CellSize));
                if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out MapSurfaceSample sample))
                    continue;

                measured++;
                float delta = position.y - sample.Height;
                if (Mathf.Abs(delta) > Mathf.Abs(worst))
                {
                    worst = delta;
                    worstDetail = $"unit at ({position.x:0.0},{position.y:0.00},{position.z:0.0}) " +
                                  $"cell ({cell.x},{cell.y}) surface {sample.Height:0.00}";
                }

                if (delta > GroundingTolerance)
                    floating++;

                if (measured <= 40)
                {
                    report.AppendLine(
                        $"| {i} | ({position.x:0.0}, {position.y:0.00}, {position.z:0.0}) | " +
                        $"({cell.x},{cell.y}) | {sample.Height:0.000} | {delta:0.000} |");
                }
            }

            report.AppendLine();
            report.AppendLine($"- Worst unit-to-surface offset: {worst:0.000} m ({worstDetail})");
            report.AppendLine($"- Units floating above tolerance ({GroundingTolerance:0.00} m): {floating}");
            report.AppendLine();
            report.AppendLine("## Corrected cells, sampled live");
            report.AppendLine();
            report.AppendLine("| area | cell | runtime surface height | movement mask |");
            report.AppendLine("|---|---|---:|---|");
            (string Name, int X, int Z)[] corrected =
            {
                ("civic", 1033, 509),
                ("civic-statue", 1027, 523),
                ("northern-join", 986, 698),
                ("east-shelf", 1072, 620)
            };
            for (int i = 0; i < corrected.Length; i++)
            {
                (string name, int x, int z) = corrected[i];
                string height = "none";
                string mask = "n/a";
                if (MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, new int2(x, z), out MapSurfaceSample sample))
                {
                    height = sample.Height.ToString("0.000");
                    mask = sample.MovementMask.ToString();
                }

                report.AppendLine($"| {name} | ({x},{z}) | {height} | {mask} |");
            }

            File.WriteAllText(Path.Combine(EvidenceDirectory, "cc-play-grounding.md"), report.ToString());

            string provenance = DescribeSurfaceProvenance(em);
            bool passed = measured > 0 && floating == 0 && provenance == "baked";
            return
                $"[CityCrossroadsPlayProbe] result={(passed ? "Passed" : "Failed")} scenario={match.ScenarioIndex} " +
                $"phase={match.Phase} surface={provenance} unitsMeasured={measured} floatingUnits={floating} " +
                $"worstUnitOffset={worst:0.000} tolerance={GroundingTolerance:0.00}";
        }

        /// <summary>
        /// Characterises the residual 2.37 m ledge near (1008,686): how large the raised patch is, and
        /// whether traversable ground surrounds it. A small patch ringed by walkable grade is something
        /// units path around; a long barrier sealing a route would be a genuine blocker.
        /// </summary>
        private static string ProbeResidualLedge(EntityManager em)
        {
            using var surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            if (surfaceQuery.CalculateEntityCount() != 1)
                return "[CityCrossroadsLedgeProbe] result=Skipped reason=noSurfaceSingleton";

            MapSurfaceComponent surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;

            var seed = new int2(1008, 686);
            if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, seed, out MapSurfaceSample seedSample))
                return "[CityCrossroadsLedgeProbe] result=Skipped reason=noSeedCell";

            // The ledge only exists in the baked heights; on the flat stand-in every cell is 0 and a
            // flood fill would swallow the whole map, so refuse to report a meaningless verdict.
            if (seedSample.Height < 1.0f)
            {
                return
                    $"[CityCrossroadsLedgeProbe] result=Skipped reason=ledgeAbsentAtRuntime " +
                    $"seedHeight={seedSample.Height:0.00} surface={DescribeSurfaceProvenance(em)}";
            }

            // Flood-fill the raised patch the seed belongs to.
            float raisedFloor = seedSample.Height - 0.40f;
            var visited = new HashSet<long>();
            var frontier = new Queue<int2>();
            frontier.Enqueue(seed);
            visited.Add(KeyOf(seed));

            int minX = seed.x, maxX = seed.x, minZ = seed.y, maxZ = seed.y;
            int patchCells = 0;
            int perimeterWalkable = 0;

            while (frontier.Count > 0 && patchCells < 40000)
            {
                int2 cell = frontier.Dequeue();
                patchCells++;
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minZ = Mathf.Min(minZ, cell.y);
                maxZ = Mathf.Max(maxZ, cell.y);

                int2[] neighbours =
                {
                    new(cell.x + 1, cell.y), new(cell.x - 1, cell.y),
                    new(cell.x, cell.y + 1), new(cell.x, cell.y - 1)
                };

                for (int i = 0; i < neighbours.Length; i++)
                {
                    int2 neighbour = neighbours[i];
                    if (visited.Contains(KeyOf(neighbour)))
                        continue;
                    if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, neighbour, out MapSurfaceSample sample))
                        continue;

                    if (sample.Height >= raisedFloor)
                    {
                        visited.Add(KeyOf(neighbour));
                        frontier.Enqueue(neighbour);
                        continue;
                    }

                    // Lower ground adjoining the patch: traversable bypass if units may stand there.
                    if ((sample.MovementMask & MapSurfaceMovementMask.AllGroundUnits) != 0)
                        perimeterWalkable++;
                }
            }

            bool bypassed = perimeterWalkable > 0;
            return
                $"[CityCrossroadsLedgeProbe] result={(bypassed ? "BypassAvailable" : "Blocking")} " +
                $"seed=(1008,686) seedHeight={seedSample.Height:0.00} patchCells={patchCells} " +
                $"patchBounds=[x {minX}..{maxX}, z {minZ}..{maxZ}] walkablePerimeterCells={perimeterWalkable}";
        }

        private static long KeyOf(int2 cell) => ((long)cell.x << 32) ^ (uint)cell.y;

        private static void Finish(string message)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            Debug.Log(message);
            // The probe owns the Editor lifetime: it is launched without -quit so play mode can run, so it
            // must quit itself. A non-zero code surfaces the failure as a normal validation failure.
            EditorApplication.Exit(message.Contains("result=Passed", StringComparison.Ordinal) ? 0 : 1);
        }
    }
}
#endif
