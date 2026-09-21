#if UNITY_EDITOR
namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Deterministic Edit-mode geometry audit, capture harness and corrective pass for the City Crossroads
    /// (E0.1) floating-shelf defect.
    ///
    /// The dense-city grading pass archived the interior hills but left the sand tiles that used to cap them
    /// stranded at the old hill height, together with every prop standing on them. The audit measures that
    /// condition on the authored presentation SubScene (the source of truth the runtime SubScene streams),
    /// and <see cref="ApplyAuthoredCorrection"/> reseats the stranded assemblies in that same authored scene.
    /// Nothing here runs at play time.
    /// </summary>
    public static class CityCrossroadsShelfGeometryAudit
    {
        public const string PresentationScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";

        /// <summary>City Crossroads playable window recorded on OperationMap_CityCrossroads.</summary>
        public static readonly Vector2 PlayableMin = new(900f, 260f);
        public static readonly Vector2 PlayableMax = new(1300f, 875f);

        /// <summary>Authored city floor grade for the retained dense-city ground.</summary>
        public const float AuthoredGrade = 0.01f;

        /// <summary>A ground panel may sit this far above abutting terrain before it needs visible support.</summary>
        public const float MaxUnsupportedRise = 1.50f;

        internal const float SupportContactTolerance = 0.75f;
        internal const float SupportCoverageFraction = 0.10f;
        internal const float GroundContactTolerance = 1.00f;
        internal const float SupportBucketSize = 24f;
        internal const float AbuttingMargin = 12f;
        internal const float RiderEmbedTolerance = 1.50f;
        internal const float RiderSeatTolerance = 0.75f;

        public const string EvidenceDirectory = "/private/tmp/warline-e01-cc-shelf";
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;

        internal readonly struct GeometryRecord
        {
            public GeometryRecord(Transform owner, string path, Bounds bounds)
            {
                Owner = owner;
                Path = path;
                Bounds = bounds;
            }

            public Transform Owner { get; }
            public string Path { get; }
            public Bounds Bounds { get; }
            public float Thickness => Bounds.size.y;
            public float FootprintArea => Bounds.size.x * Bounds.size.z;
        }

        private readonly struct CaptureDefinition
        {
            public CaptureDefinition(string name, Vector3 position, Vector3 target, float fieldOfView)
            {
                Name = name;
                Position = position;
                Target = target;
                FieldOfView = fieldOfView;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
            public float FieldOfView { get; }
        }

        /// <summary>
        /// Civic square and northern base join, each from a low pitch, a high pitch and both route sides,
        /// per the MAPS.md geometry acceptance rule.
        /// </summary>
        private static readonly CaptureDefinition[] Captures =
        {
            // The civic slab is covered by civic-east and civic-west at low pitch plus civic-high above it.
            // A dedicated south-facing low camera is deliberately absent: every position tried there sits
            // inside the surrounding block and renders interior wall, which would be misleading evidence.
            new("civic-high", new Vector3(1033f, 52f, 448f), new Vector3(1033f, 3f, 512f), 55f),
            new("civic-east", new Vector3(1092f, 14f, 509f), new Vector3(1033f, 5f, 509f), 55f),
            new("civic-west", new Vector3(996f, 12f, 496f), new Vector3(1033f, 4f, 512f), 55f),
            new("north-low", new Vector3(986f, 11f, 648f), new Vector3(986f, 5f, 698f), 55f),
            new("north-high", new Vector3(986f, 52f, 634f), new Vector3(986f, 3f, 698f), 55f),
            new("north-east", new Vector3(1045f, 14f, 698f), new Vector3(986f, 5f, 698f), 55f),
            new("north-west", new Vector3(927f, 14f, 698f), new Vector3(986f, 5f, 698f), 55f),
            new("east-low", new Vector3(1104f, 9f, 596f), new Vector3(1074f, 4f, 618f), 55f),
            new("east-high", new Vector3(1072f, 46f, 566f), new Vector3(1072f, 3f, 622f), 55f),
            new("east-side", new Vector3(1120f, 13f, 620f), new Vector3(1072f, 5f, 620f), 55f)
        };

        // ---------------------------------------------------------------------------------------------
        // Audit
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Fails when any authored ground panel inside the City Crossroads playable window hovers more than
        /// <see cref="MaxUnsupportedRise"/> above abutting terrain without supporting terrain beneath it, or
        /// when scenery riding on such a panel is left hovering or buried.
        /// </summary>
        public static void RunFocusedValidation()
        {
            Scene scene = OpenPresentationScene();
            List<GeometryRecord> records = CollectRecordsInPlayableWindow(scene);
            List<GeometryRecord> panels = CollectGroundPanels(records);
            bool[] grounded = ResolveGroundPanelGrounding(panels, out string[] supportDetails);

            var findings = new List<string>();
            int cases = 0;
            int unsupported = 0;
            int unseatedPanels = 0;

            for (int i = 0; i < panels.Count; i++)
            {
                GeometryRecord panel = panels[i];
                cases++;

                // Case 1: the panel must not hover above abutting terrain without visible support.
                float grade = ComputeAbuttingGrade(panels, panel);
                float rise = panel.Bounds.min.y - grade;
                if (rise > MaxUnsupportedRise && !grounded[i])
                {
                    unsupported++;
                    findings.Add(
                        $"unsupported-ground-panel name=`{panel.Owner.name}` underside={panel.Bounds.min.y:0.00} " +
                        $"top={panel.Bounds.max.y:0.00} abuttingGrade={grade:0.00} rise={rise:0.00} " +
                        $"footprint={panel.FootprintArea:0.0} support={supportDetails[i]} path={panel.Path}");
                }

                // Case 2: scenery carried by the panel must stay seated on it. A correction that lowers the
                // slab but strands the statue, stall or rugs in mid-air fails here.
                if (!TryGetLowestRider(records, panel, out GeometryRecord lowest))
                    continue;
                cases++;
                float gap = lowest.Bounds.min.y - panel.Bounds.max.y;
                if (gap >= -RiderEmbedTolerance && gap <= RiderSeatTolerance)
                    continue;

                unseatedPanels++;
                findings.Add(
                    $"unseated-scenery panel=`{panel.Owner.name}` panelTop={panel.Bounds.max.y:0.00} " +
                    $"lowestRider=`{lowest.Owner.name}` riderBase={lowest.Bounds.min.y:0.00} seatGap={gap:0.00}");
            }

            // Case 3: zoning. No industrial building may stand inside the City Crossroads window; MAPS.md
            // reserves industrial identity for the separate Industrial Basin battlefield.
            cases++;
            List<Game.Authoring.OperationMapBuildingAuthoring> industrial = CollectIndustrialInWindow(scene);
            int industrialCount = industrial.Count;
            for (int i = 0; i < industrial.Count && i < 40; i++)
            {
                findings.Add(
                    $"industrial-building-in-window name=`{industrial[i].name}` " +
                    $"originCell=({industrial[i].OriginCell.x},{industrial[i].OriginCell.y}) " +
                    $"definition=`{(industrial[i].Definition != null ? industrial[i].Definition.name : "none")}`");
            }

            // Case 4: reported baseline only. Gameplay buildings whose footprint sits on authored sidewalk
            // is a pre-existing Desert Base defect as well, so hard-failing here would block unrelated work.
            // The threshold is pinned at the measured count so it can only be driven down.
            int sidewalkBuildings = MeasureSidewalkOverlaps(scene, out int sidewalkPairs, out int buildingsConsidered);

            Directory.CreateDirectory(EvidenceDirectory);
            string findingsPath = Path.Combine(EvidenceDirectory, "cc-shelf-audit-findings.txt");
            File.WriteAllLines(findingsPath, findings);

            for (int i = 0; i < findings.Count && i < 40; i++)
                Debug.LogWarning($"[CityCrossroadsShelfFinding] {findings[i]}");

            int violations = unsupported + unseatedPanels + industrialCount;
            bool sidewalkWithinBaseline = sidewalkBuildings <= SidewalkOverlapBaseline;
            if (!sidewalkWithinBaseline)
                violations++;

            string marker =
                $"[CityCrossroadsShelfGeometryAudit] result={(violations == 0 ? "Passed" : "Failed")} " +
                $"cases={cases} panels={panels.Count} unsupportedPanels={unsupported} " +
                $"unseatedScenery={unseatedPanels} industrialInWindow={industrialCount} " +
                $"sidewalkOverlapBuildings={sidewalkBuildings}/{buildingsConsidered} " +
                $"sidewalkOverlapPairs={sidewalkPairs} sidewalkBaseline={SidewalkOverlapBaseline} " +
                $"sidewalkWithinBaseline={sidewalkWithinBaseline} " +
                $"violations={violations} findings={findingsPath}";

            if (violations != 0)
            {
                Debug.LogError(marker);
                throw new InvalidOperationException(marker);
            }

            Debug.Log(marker);
        }

        // ---------------------------------------------------------------------------------------------
        // Corrective pass on the authored scene
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Reseats every stranded ground panel inside the playable window onto the abutting graded terrain,
        /// moving each panel together with the scenery riding on it as one rigid assembly so relative seating
        /// (statue on pedestal, stall clutter on tables, rugs on sand) is preserved exactly. The authored
        /// scene is saved, so the runtime SubScene bakes the corrected geometry.
        /// </summary>
        public static void ApplyAuthoredCorrection()
        {
            Scene scene = OpenPresentationScene();
            var log = new List<string>();
            int totalPanels = 0;
            int totalObjects = 0;
            int pass = 0;

            // The stranded tiles form a stack: sand slabs at ~5.85 m abut an intermediate terrace at ~1.89 m
            // that is itself stranded above the city floor. Lowering settles the stack one tier per pass, so
            // iterate to a fixpoint instead of assuming a single tier.
            for (; pass < MaxCorrectionPasses; pass++)
            {
                List<GeometryRecord> records = CollectRecordsInPlayableWindow(scene);
                List<GeometryRecord> panels = CollectGroundPanels(records);
                bool[] grounded = ResolveGroundPanelGrounding(panels, out _);

                var movedRoots = new HashSet<Transform>();
                int passPanels = 0;
                int passObjects = 0;

                for (int i = 0; i < panels.Count; i++)
                {
                    GeometryRecord panel = panels[i];
                    float grade = ComputeAbuttingGrade(panels, panel);
                    float rise = panel.Bounds.min.y - grade;
                    if (rise <= MaxUnsupportedRise || grounded[i])
                        continue;

                    // Land the panel's walking surface flush with the abutting terrain it was stranded above.
                    float delta = grade - panel.Bounds.max.y;
                    if (Mathf.Abs(delta) < 0.001f)
                        continue;

                    var assembly = new List<Transform>();
                    if (!AddMover(assembly, movedRoots, panel.Owner))
                        continue;
                    List<GeometryRecord> carried = CollectCarriedAssembly(records, panel);
                    for (int k = 0; k < carried.Count; k++)
                        AddMover(assembly, movedRoots, carried[k].Owner);

                    for (int k = 0; k < assembly.Count; k++)
                    {
                        Transform mover = assembly[k];
                        Undo.RecordObject(mover, "Reseat City Crossroads shelf");
                        mover.position += new Vector3(0f, delta, 0f);
                        EditorUtility.SetDirty(mover);
                        passObjects++;
                    }

                    passPanels++;
                    log.Add(
                        $"pass={pass} panel=`{panel.Owner.name}` underside={panel.Bounds.min.y:0.00} " +
                        $"top={panel.Bounds.max.y:0.00} abuttingGrade={grade:0.00} rise={rise:0.00} " +
                        $"delta={delta:0.00} movedObjects={assembly.Count}");
                }

                totalPanels += passPanels;
                totalObjects += passObjects;
                Debug.Log(
                    $"[CityCrossroadsShelfCorrection] pass={pass} correctedPanels={passPanels} " +
                    $"movedObjects={passObjects}");
                if (passPanels == 0)
                    break;
            }

            Directory.CreateDirectory(EvidenceDirectory);
            File.WriteAllLines(Path.Combine(EvidenceDirectory, "cc-shelf-correction.txt"), log);

            if (totalPanels > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Failed to save the corrected presentation scene.");
            }

            Debug.Log(
                $"[CityCrossroadsShelfCorrection] result=Applied passes={pass} correctedPanels={totalPanels} " +
                $"movedObjects={totalObjects} scene={PresentationScenePath}");
        }

        internal const int MaxCorrectionPasses = 10;

        /// <summary>
        /// Adds the scene-level object owning <paramref name="source"/> to the move set, skipping anything
        /// already scheduled (directly or through an ancestor) so no object is displaced twice.
        /// </summary>
        private static bool AddMover(List<Transform> assembly, HashSet<Transform> movedRoots, Transform source)
        {
            Transform mover = GetContainerChild(source);
            if (mover == null || mover.parent == null)
                return false;
            for (Transform cursor = mover; cursor != null; cursor = cursor.parent)
            {
                if (movedRoots.Contains(cursor))
                    return false;
            }

            movedRoots.Add(mover);
            assembly.Add(mover);
            return true;
        }

        /// <summary>
        /// The top-level authored object directly under a `__SourceTransform__*` container, or null when the
        /// object does not live under one. Returning null matters: falling back to the outermost ancestor
        /// would hand back the map root, and moving that would displace the entire map rather than one shelf.
        /// </summary>
        internal static Transform GetContainerChild(Transform transform)
        {
            for (Transform current = transform; current.parent != null; current = current.parent)
            {
                if (current.parent.name.StartsWith("__SourceTransform__", StringComparison.Ordinal))
                    return current;
            }

            return null;
        }

        // ---------------------------------------------------------------------------------------------
        // Capture
        // ---------------------------------------------------------------------------------------------

        public static void CaptureBefore() => CaptureSet("before");

        public static void CaptureAfter() => CaptureSet("after");

        /// <summary>Writes the pre-fix captures, then runs the audit so its failure is recorded in the same log.</summary>
        public static void CaptureBeforeAndAudit()
        {
            CaptureSet("before");
            RunFocusedValidation();
        }

        /// <summary>Writes the post-fix captures, then runs the audit so its pass is recorded in the same log.</summary>
        public static void CaptureAfterAndAudit()
        {
            CaptureSet("after");
            RunFocusedValidation();
        }

        private static void CaptureSet(string prefix)
        {
            OpenPresentationScene();
            Directory.CreateDirectory(EvidenceDirectory);

            var cameraObject = new GameObject("CC_ShelfAuditCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 4000f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            EnableUrpPostProcessing(cameraObject);

            // The presentation SubScene carries no active lighting on its own, so captures would render as
            // flat silhouettes. Add a deterministic sun and ambient fill for the shot only, and restore the
            // scene's render settings afterwards. The scene is never saved from a capture run.
            var sunObject = new GameObject("CC_ShelfAuditSun");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(48f, 138f, 0f);

            AmbientMode previousAmbientMode = RenderSettings.ambientMode;
            Color previousAmbientLight = RenderSettings.ambientLight;
            Color previousAmbientSky = RenderSettings.ambientSkyColor;
            float previousAmbientIntensity = RenderSettings.ambientIntensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.49f);
            RenderSettings.ambientIntensity = 1f;

            var written = new List<string>();
            try
            {
                for (int i = 0; i < Captures.Length; i++)
                {
                    CaptureDefinition definition = Captures[i];
                    camera.fieldOfView = definition.FieldOfView;
                    camera.transform.position = definition.Position;
                    camera.transform.rotation = Quaternion.LookRotation(
                        (definition.Target - definition.Position).normalized,
                        Vector3.up);

                    string path = Path.Combine(EvidenceDirectory, $"{prefix}-{definition.Name}.png");
                    long size = CaptureCamera(camera, path);
                    if (size <= 0L)
                        throw new InvalidOperationException($"Empty capture written for {path}.");
                    written.Add($"{Path.GetFileName(path)} bytes={size}");
                    Debug.Log($"[CityCrossroadsShelfCapture] file={path} bytes={size}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sunObject);
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.ambientSkyColor = previousAmbientSky;
                RenderSettings.ambientIntensity = previousAmbientIntensity;
            }

            Debug.Log(
                $"[CityCrossroadsShelfCapture] result=Completed set={prefix} captures={written.Count} " +
                $"directory={EvidenceDirectory}");
        }

        /// <summary>Synchronous render-texture readback, so no end-of-frame wait is needed.</summary>
        private static long CaptureCamera(Camera camera, string path)
        {
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                name = "CityCrossroadsShelfCapture"
            };
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                // Prime URP state so the first camera renders like the rest.
                for (int i = 0; i < 3; i++)
                    camera.Render();

                texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
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

        private static void EnableUrpPostProcessing(GameObject cameraObject)
        {
            Type dataType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (dataType == null)
                return;

            Component data = cameraObject.GetComponent(dataType) ?? cameraObject.AddComponent(dataType);
            PropertyInfo renderPostProcessing =
                dataType.GetProperty("renderPostProcessing", BindingFlags.Instance | BindingFlags.Public);
            renderPostProcessing?.SetValue(data, true);
        }

        // ---------------------------------------------------------------------------------------------
        // Shared geometry helpers
        // ---------------------------------------------------------------------------------------------

        internal static Scene OpenPresentationScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && string.Equals(active.path, PresentationScenePath, StringComparison.Ordinal))
                return active;
            return EditorSceneManager.OpenScene(PresentationScenePath, OpenSceneMode.Single);
        }

        internal static List<GeometryRecord> CollectRecordsInPlayableWindow(Scene scene)
        {
            var records = new List<GeometryRecord>(4096);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Renderer[] renderers = roots[rootIndex].GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer is ParticleSystemRenderer)
                        continue;
                    Bounds bounds = renderer.bounds;
                    if (bounds.size.sqrMagnitude <= 0f)
                        continue;
                    if (bounds.max.x < PlayableMin.x || bounds.min.x > PlayableMax.x)
                        continue;
                    if (bounds.max.z < PlayableMin.y || bounds.min.z > PlayableMax.y)
                        continue;
                    Transform owner = renderer.transform;
                    records.Add(new GeometryRecord(owner, GetScenePath(owner), bounds));
                }
            }

            return records;
        }

        /// <summary>
        /// Regression ceiling for buildings whose footprint overlaps authored sidewalk. Pinned at the count
        /// measured when the check was introduced so it can be driven down but never silently grow. Reported
        /// rather than hard-failed, because the same defect exists on Desert Base content E0.1 may not change.
        ///
        /// <para>Measured here as 342 of 1097 in-window placements (1010 overlap pairs). The method is a
        /// deterministic axis-aligned overlap between a placement's footprint cells and the world bounds of
        /// sidewalk-classified renderers, so it is comparable run to run but coarser than the runtime cache's
        /// per-cell rasterisation and will read higher than a per-cell count.</para>
        /// </summary>
        public const int SidewalkOverlapBaseline = 342;

        internal static List<Game.Authoring.OperationMapBuildingAuthoring> CollectIndustrialInWindow(Scene scene)
        {
            var found = new List<Game.Authoring.OperationMapBuildingAuthoring>();
            RectInt window = CityCrossroadsIndustrialPlacementCleanup.PlayableWindow;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                found.AddRange(
                    CityCrossroadsIndustrialPlacementCleanup.CollectIndustrialPlacementsInWindow(
                        roots[i].transform,
                        window));
            }

            return found;
        }

        /// <summary>
        /// Counts gameplay-building footprints inside the window that overlap authored sidewalk meshes.
        /// Sidewalk classification goes through <c>RoadGridProjectionSystem.TryGetFootprintKind</c>, the same
        /// entry point <c>BuildingPlacementAuthoredRoadCache</c> uses for the runtime build cursor — that mask
        /// already existed but had never been run against shipped map content, which is how this shipped.
        /// </summary>
        internal static int MeasureSidewalkOverlaps(Scene scene, out int overlapPairs, out int buildingsConsidered)
        {
            RectInt window = CityCrossroadsIndustrialPlacementCleanup.PlayableWindow;

            var sidewalks = new List<Rect>();
            var buildings = new List<Game.Authoring.OperationMapBuildingAuthoring>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                MeshRenderer[] renderers = roots[i].GetComponentsInChildren<MeshRenderer>(true);
                for (int k = 0; k < renderers.Length; k++)
                {
                    if (!Game.Runtime.RoadGridProjectionSystem.TryGetFootprintKind(
                            renderers[k].transform,
                            false,
                            out Game.Runtime.RoadGridProjectionSystem.RoadFootprintKind kind) ||
                        kind != Game.Runtime.RoadGridProjectionSystem.RoadFootprintKind.Sidewalk)
                    {
                        continue;
                    }

                    Bounds bounds = renderers[k].bounds;
                    if (bounds.max.x < window.xMin || bounds.min.x > window.xMax ||
                        bounds.max.z < window.yMin || bounds.min.z > window.yMax)
                    {
                        continue;
                    }

                    sidewalks.Add(new Rect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z));
                }

                Game.Authoring.OperationMapBuildingAuthoring[] placements =
                    roots[i].GetComponentsInChildren<Game.Authoring.OperationMapBuildingAuthoring>(true);
                for (int k = 0; k < placements.Length; k++)
                {
                    if (CityCrossroadsIndustrialPlacementCleanup.IsInsideWindow(placements[k], window))
                        buildings.Add(placements[k]);
                }
            }

            overlapPairs = 0;
            buildingsConsidered = buildings.Count;
            int overlapping = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                Game.Authoring.OperationMapBuildingAuthoring placement = buildings[i];
                Vector2Int origin = placement.OriginCell;
                Vector2Int size = placement.FootprintCells;
                if (size.x <= 0 || size.y <= 0)
                    continue;

                var footprint = new Rect(origin.x, origin.y, size.x, size.y);
                bool hit = false;
                for (int k = 0; k < sidewalks.Count; k++)
                {
                    if (!footprint.Overlaps(sidewalks[k]))
                        continue;
                    overlapPairs++;
                    hit = true;
                }

                if (hit)
                    overlapping++;
            }

            return overlapping;
        }

        /// <summary>
        /// Authored ground/terrain panels. Deliberately excludes the skydome, rocks, debris and buildings:
        /// MAPS.md requires a raised ground panel to stand on visible supporting *terrain*, and an arbitrary
        /// prop whose axis-aligned bounds happen to span the gap is not terrain support.
        /// </summary>
        internal static bool IsGroundPanel(string name)
        {
            return name.IndexOf("Env_Ground", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static List<GeometryRecord> CollectGroundPanels(List<GeometryRecord> records)
        {
            var panels = new List<GeometryRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                if (IsGroundPanel(records[i].Owner.name))
                    panels.Add(records[i]);
            }

            return panels;
        }

        /// <summary>
        /// Resolves transitive grounding across ground panels only. Panels are visited in ascending underside
        /// order, so a panel can only be supported by terrain already proven to reach the city floor; two
        /// stranded panels at the same height cannot certify each other.
        /// </summary>
        internal static bool[] ResolveGroundPanelGrounding(
            List<GeometryRecord> panels,
            out string[] supportDetails)
        {
            int count = panels.Count;
            var grounded = new bool[count];
            supportDetails = new string[count];

            var order = new int[count];
            for (int i = 0; i < count; i++)
                order[i] = i;
            Array.Sort(order, (a, b) => panels[a].Bounds.min.y.CompareTo(panels[b].Bounds.min.y));

            var buckets = new Dictionary<long, List<int>>(count * 2);
            for (int i = 0; i < count; i++)
            {
                foreach (long key in BucketKeys(panels[i].Bounds))
                {
                    if (!buckets.TryGetValue(key, out List<int> list))
                    {
                        list = new List<int>(8);
                        buckets[key] = list;
                    }

                    list.Add(i);
                }
            }

            for (int orderIndex = 0; orderIndex < count; orderIndex++)
            {
                int index = order[orderIndex];
                GeometryRecord panel = panels[index];
                if (panel.Bounds.min.y <= AuthoredGrade + GroundContactTolerance)
                {
                    grounded[index] = true;
                    supportDetails[index] = "rests on the authored city floor";
                    continue;
                }

                float footprint = panel.FootprintArea;
                var seen = new HashSet<int>();
                foreach (long key in BucketKeys(panel.Bounds))
                {
                    if (!buckets.TryGetValue(key, out List<int> list))
                        continue;
                    for (int k = 0; k < list.Count; k++)
                    {
                        int candidateIndex = list[k];
                        if (candidateIndex == index || !seen.Add(candidateIndex) || !grounded[candidateIndex])
                            continue;

                        GeometryRecord candidate = panels[candidateIndex];
                        if (ReferenceEquals(candidate.Owner, panel.Owner))
                            continue;

                        Bounds bounds = candidate.Bounds;
                        if (bounds.min.y >= panel.Bounds.min.y - 0.05f)
                            continue;
                        if (bounds.max.y < panel.Bounds.min.y - SupportContactTolerance)
                            continue;

                        float overlapX = Mathf.Min(bounds.max.x, panel.Bounds.max.x) -
                                         Mathf.Max(bounds.min.x, panel.Bounds.min.x);
                        float overlapZ = Mathf.Min(bounds.max.z, panel.Bounds.max.z) -
                                         Mathf.Max(bounds.min.z, panel.Bounds.min.z);
                        if (overlapX <= 0f || overlapZ <= 0f)
                            continue;
                        if (footprint > 0f && (overlapX * overlapZ) / footprint < SupportCoverageFraction)
                            continue;

                        grounded[index] = true;
                        supportDetails[index] =
                            $"`{candidate.Owner.name}` top={bounds.max.y:0.00}";
                        break;
                    }

                    if (grounded[index])
                        break;
                }

                if (!grounded[index])
                    supportDetails[index] = "no grounded terrain rises to the underside";
            }

            return grounded;
        }

        /// <summary>Highest ground-panel top that abuts this panel's footprint and sits below its underside.</summary>
        internal static float ComputeAbuttingGrade(List<GeometryRecord> panels, GeometryRecord panel)
        {
            float best = float.NaN;
            Bounds expanded = panel.Bounds;
            expanded.Expand(new Vector3(AbuttingMargin * 2f, 0f, AbuttingMargin * 2f));
            for (int i = 0; i < panels.Count; i++)
            {
                GeometryRecord candidate = panels[i];
                if (ReferenceEquals(candidate.Owner, panel.Owner))
                    continue;
                if (candidate.Bounds.max.y >= panel.Bounds.min.y)
                    continue;
                if (candidate.Bounds.max.x < expanded.min.x || candidate.Bounds.min.x > expanded.max.x)
                    continue;
                if (candidate.Bounds.max.z < expanded.min.z || candidate.Bounds.min.z > expanded.max.z)
                    continue;
                if (float.IsNaN(best) || candidate.Bounds.max.y > best)
                    best = candidate.Bounds.max.y;
            }

            return float.IsNaN(best) ? AuthoredGrade : best;
        }

        /// <summary>
        /// Scenery carried by the panel (statues, pedestals, stall furniture, rugs, clutter).
        /// Other ground panels are never riders: they are terrain in their own right and are corrected
        /// independently, so a slab three metres higher must not be dragged down as though it were a prop.
        /// <paramref name="seatedOnly"/> narrows the set to objects actually resting on the panel top, which
        /// is what the seating assertion checks; the wider set is what moves with the panel.
        /// </summary>
        internal static List<GeometryRecord> CollectRiders(List<GeometryRecord> records, GeometryRecord panel)
        {
            float top = panel.Bounds.max.y;
            float ceiling = top + RiderSeatTolerance;
            var riders = new List<GeometryRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                GeometryRecord candidate = records[i];
                if (ReferenceEquals(candidate.Owner, panel.Owner))
                    continue;
                if (IsGroundPanel(candidate.Owner.name))
                    continue;
                Vector3 centre = candidate.Bounds.center;
                if (centre.x < panel.Bounds.min.x || centre.x > panel.Bounds.max.x)
                    continue;
                if (centre.z < panel.Bounds.min.z || centre.z > panel.Bounds.max.z)
                    continue;
                float baseY = candidate.Bounds.min.y;
                if (baseY < top - RiderEmbedTolerance || baseY > ceiling)
                    continue;
                riders.Add(candidate);
            }

            return riders;
        }

        /// <summary>Vertical band in which one object counts as resting on another.</summary>
        internal const float StackContactTolerance = 0.75f;

        /// <summary>
        /// Everything the panel actually carries, resolved as a support chain: objects seated on the panel,
        /// then objects seated on those, and so on. A dome resting on a building that stands at grade inside
        /// the panel's footprint is therefore *not* carried, while a statue on a pedestal on the panel is.
        /// Moving only this set keeps the shelf's own scenery together without dragging down neighbours.
        /// </summary>
        internal static List<GeometryRecord> CollectCarriedAssembly(
            List<GeometryRecord> records,
            GeometryRecord panel)
        {
            // Restrict to objects standing over the panel footprint; the chain cannot leave it.
            var candidates = new List<GeometryRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                GeometryRecord candidate = records[i];
                if (ReferenceEquals(candidate.Owner, panel.Owner) || IsGroundPanel(candidate.Owner.name))
                    continue;
                Vector3 centre = candidate.Bounds.center;
                if (centre.x < panel.Bounds.min.x || centre.x > panel.Bounds.max.x)
                    continue;
                if (centre.z < panel.Bounds.min.z || centre.z > panel.Bounds.max.z)
                    continue;
                if (candidate.Bounds.min.y < panel.Bounds.max.y - RiderEmbedTolerance)
                    continue;
                candidates.Add(candidate);
            }

            var carried = new List<GeometryRecord>();
            var taken = new bool[candidates.Count];
            var supports = new List<Bounds> { panel.Bounds };

            for (bool changed = true; changed;)
            {
                changed = false;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (taken[i])
                        continue;
                    GeometryRecord candidate = candidates[i];
                    for (int s = 0; s < supports.Count; s++)
                    {
                        Bounds support = supports[s];
                        Vector3 centre = candidate.Bounds.center;
                        if (centre.x < support.min.x || centre.x > support.max.x)
                            continue;
                        if (centre.z < support.min.z || centre.z > support.max.z)
                            continue;
                        float gap = candidate.Bounds.min.y - support.max.y;
                        if (gap < -RiderEmbedTolerance || gap > StackContactTolerance)
                            continue;

                        taken[i] = true;
                        carried.Add(candidate);
                        supports.Add(candidate.Bounds);
                        changed = true;
                        break;
                    }
                }
            }

            return carried;
        }

        /// <summary>The rider that actually rests on the panel, i.e. the one with the lowest base.</summary>
        internal static bool TryGetLowestRider(
            List<GeometryRecord> records,
            GeometryRecord panel,
            out GeometryRecord lowest)
        {
            lowest = default;
            bool found = false;
            List<GeometryRecord> riders = CollectRiders(records, panel);
            for (int i = 0; i < riders.Count; i++)
            {
                if (found && riders[i].Bounds.min.y >= lowest.Bounds.min.y)
                    continue;
                lowest = riders[i];
                found = true;
            }

            return found;
        }

        private static IEnumerable<long> BucketKeys(Bounds bounds)
        {
            int minX = Mathf.FloorToInt(bounds.min.x / SupportBucketSize);
            int maxX = Mathf.FloorToInt(bounds.max.x / SupportBucketSize);
            int minZ = Mathf.FloorToInt(bounds.min.z / SupportBucketSize);
            int maxZ = Mathf.FloorToInt(bounds.max.z / SupportBucketSize);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                    yield return ((long)x << 32) ^ (uint)z;
            }
        }

        internal static string GetScenePath(Transform transform)
        {
            var builder = new StringBuilder(transform.name);
            Transform current = transform.parent;
            while (current != null)
            {
                builder.Insert(0, current.name + "/");
                current = current.parent;
            }

            return builder.ToString();
        }

        internal static string Fmt(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.00}, {1:0.00}, {2:0.00})",
                value.x,
                value.y,
                value.z);
        }
    }
}
#endif
