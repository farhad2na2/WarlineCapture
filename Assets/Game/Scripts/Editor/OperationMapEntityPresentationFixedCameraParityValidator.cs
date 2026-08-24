#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Authoring;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.SceneManagement;

    internal static class OperationMapEntityPresentationFixedCameraParityValidator
    {
        internal const string ReportPath =
            "Design/AgentReports/2026-07-22_operation_map_fixed_camera_parity.json";
        internal const string CaptureDirectory =
            "Design/AgentReports/Captures/2026-07-22_operation_map_fixed_camera_parity";
        internal const string DenseEditorReportPath =
            "Design/AgentReports/2026-07-24_dense_city_editor_fixed_camera_baseline.json";
        internal const string DenseEditorCaptureDirectory =
            "Design/AgentReports/Captures/2026-07-24_dense_city_editor_fixed_camera_baseline";
        internal const int Width = 1280;
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        internal const int Height = 720;
        internal const float MaximumMeanChannelDelta = 0.0025f;
        internal const float MaximumChangedPixelRatio = 0.01f;
        private const byte ChangedChannelThreshold = 3;
        private const int ExpectedDenseIdentityCount = 36424;
        private const int ExpectedDenseRuntimeRenderRowCount = 62136;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/EntityScene Migration/Capture Fixed Camera Parity")]
        public static void CaptureCurrentCandidate() => CaptureCurrentCandidateBatch();

        [MenuItem(
            "Game/Operation Maps/EntityScene Migration/Capture Dense City Editor Baseline")]
        public static void CaptureDenseCityEditorBaseline() =>
            CaptureDenseCityEditorBaselineBatch();

        public static void CaptureDenseCityEditorBaselineBatch()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            string candidatePath =
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            RequireAsset(candidatePath);

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene workspace = default;
            var rendererStates = new List<RendererState>();
            var lightStates = new List<LightState>();
            AmbientMode previousAmbientMode = RenderSettings.ambientMode;
            Color previousAmbientLight = RenderSettings.ambientLight;
            float previousAmbientIntensity = RenderSettings.ambientIntensity;
            try
            {
                workspace = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Scene candidate =
                    EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(workspace);

                List<Renderer> renderers = BuildDenseEditorRenderers(
                    candidate,
                    out int legacyIdentityCount,
                    out int denseIdentityCount);
                if (renderers.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Dense fixed-camera baseline has no active finite renderers.");
                }
                rendererStates.AddRange(renderers.Select(renderer => new RendererState(renderer)));
                foreach (Light light in candidate.GetRootGameObjects()
                             .SelectMany(root => root.GetComponentsInChildren<Light>(true)))
                {
                    lightStates.Add(new LightState(light));
                }
                for (int i = 0; i < lightStates.Count; i++)
                    lightStates[i].light.enabled = false;

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.72f, 1f);
                RenderSettings.ambientIntensity = 1f;
                SetVisible(renderers, true);
                ApplyInitialDenseVisualState(candidate);
                ApplyPackedBaseColorPreview(renderers);

                Bounds bounds = CalculateBounds(renderers);
                Camera camera = CreateCamera(workspace);
                Light captureLight = CreateLight(workspace);
                IReadOnlyList<ViewSpec> views = BuildViews(bounds, renderers);
                string captureRoot = Path.Combine(projectRoot, DenseEditorCaptureDirectory);
                Directory.CreateDirectory(captureRoot);

                var rows = new List<DenseEditorCaptureRow>(views.Count);
                for (int i = 0; i < views.Count; i++)
                {
                    ViewSpec view = views[i];
                    ConfigureCamera(camera, view);
                    WarmUp(camera);
                    Texture2D texture = Capture(camera);
                    try
                    {
                        string relativePath =
                            $"{DenseEditorCaptureDirectory}/{view.name}_editor.png";
                        byte[] png = texture.EncodeToPNG();
                        File.WriteAllBytes(Path.Combine(projectRoot, relativePath), png);
                        PixelComparison nonBlank = Compare(
                            texture.GetPixels32(),
                            texture.GetPixels32(),
                            ChangedChannelThreshold);
                        if (nonBlank.sourceLumaVariance <= 0.0001f)
                        {
                            throw new InvalidOperationException(
                                $"Dense fixed-camera baseline view is blank: {view.name}");
                        }
                        rows.Add(new DenseEditorCaptureRow
                        {
                            view = view.name,
                            editorPath = relativePath,
                            editorSha256 = Sha256(png),
                            editorLumaVariance = nonBlank.sourceLumaVariance,
                            cameraPosition = ToArray(view.position),
                            cameraRotation = ToArray(view.rotation.eulerAngles),
                            orthographic = view.orthographic ? 1 : 0,
                            fieldOfView = view.fieldOfView,
                            orthographicSize = view.orthographicSize
                        });
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }

                var report = new DenseEditorCaptureReport
                {
                    schema = "warline.operation-map.dense-city-editor-fixed-camera-baseline",
                    schemaVersion = 1,
                    operationMapId =
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                    result = "DenseCityEditorFixedCameraBaselineCaptured",
                    candidateSubScenePath = candidatePath,
                    candidateSubSceneSha256 =
                        Sha256File(Path.Combine(projectRoot, candidatePath)),
                    width = Width,
                    height = Height,
                    rendererCount = renderers.Count,
                    legacyIdentityCount = legacyIdentityCount,
                    denseIdentityCount = denseIdentityCount,
                    expectedRuntimeRenderRowCount = ExpectedDenseRuntimeRenderRowCount,
                    viewCount = rows.Count,
                    maximumMeanChannelDelta = MaximumMeanChannelDelta,
                    maximumChangedPixelRatio = MaximumChangedPixelRatio,
                    productionCutover = 1,
                    rows = rows
                };
                string reportPath = Path.Combine(projectRoot, DenseEditorReportPath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(reportPath) ?? projectRoot);
                File.WriteAllText(
                    reportPath,
                    JsonUtility.ToJson(report, true) + "\n",
                    Utf8WithoutBom);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log(
                    $"[DenseCityEditorFixedCameraBaseline] result={report.result} " +
                    $"views={rows.Count} renderers={renderers.Count} " +
                    $"report={DenseEditorReportPath}");
                UnityEngine.Object.DestroyImmediate(captureLight.gameObject);
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
            finally
            {
                for (int i = 0; i < rendererStates.Count; i++)
                    rendererStates[i].Restore();
                for (int i = 0; i < lightStates.Count; i++)
                    lightStates[i].Restore();
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.ambientIntensity = previousAmbientIntensity;
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        public static void CaptureCurrentCandidateBatch()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            string sourceSubScenePath = OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath;
            string sourceOperationMapPath = OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath;
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            RequireAsset(sourceSubScenePath);
            RequireAsset(sourceOperationMapPath);
            RequireAsset(candidatePath);

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene workspace = default;
            var rendererStates = new List<RendererState>();
            var lightStates = new List<LightState>();
            AmbientMode previousAmbientMode = RenderSettings.ambientMode;
            Color previousAmbientLight = RenderSettings.ambientLight;
            float previousAmbientIntensity = RenderSettings.ambientIntensity;
            try
            {
                workspace = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Scene sourceSubScene = EditorSceneManager.OpenScene(sourceSubScenePath, OpenSceneMode.Additive);
                Scene sourceOperationMap = EditorSceneManager.OpenScene(sourceOperationMapPath, OpenSceneMode.Additive);
                Scene candidate = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(workspace);

                Scene[] sourceScenes = { sourceSubScene, sourceOperationMap };
                CaptureSets sets = BuildCaptureSets(sourceScenes, candidate);
                rendererStates.AddRange(sets.sourceRenderers.Select(renderer => new RendererState(renderer)));
                rendererStates.AddRange(sets.candidateRenderers.Select(renderer => new RendererState(renderer)));
                for (int i = 0; i < sourceScenes.Length; i++)
                {
                    foreach (Light light in sourceScenes[i].GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Light>(true)))
                        lightStates.Add(new LightState(light));
                }
                foreach (Light light in candidate.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Light>(true)))
                    lightStates.Add(new LightState(light));
                for (int i = 0; i < lightStates.Count; i++)
                    lightStates[i].light.enabled = false;

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.72f, 1f);
                RenderSettings.ambientIntensity = 1f;

                Bounds bounds = CalculateBounds(sets.sourceRenderers);
                Camera camera = CreateCamera(workspace);
                Light captureLight = CreateLight(workspace);
                IReadOnlyList<ViewSpec> views = BuildViews(bounds, sets.sourceRenderers);
                string captureRoot = Path.Combine(projectRoot, CaptureDirectory);
                Directory.CreateDirectory(captureRoot);

                var rows = new List<CaptureRow>(views.Count);
                for (int i = 0; i < views.Count; i++)
                {
                    ViewSpec view = views[i];
                    ConfigureCamera(camera, view);
                    SetVisible(sets.sourceRenderers, true);
                    SetVisible(sets.candidateRenderers, false);
                    camera.Render();
                    Texture2D sourceTexture = Capture(camera);
                    SetVisible(sets.sourceRenderers, false);
                    SetVisible(sets.candidateRenderers, true);
                    camera.Render();
                    Texture2D candidateTexture = Capture(camera);
                    try
                    {
                        string sourceRelative = $"{CaptureDirectory}/{view.name}_source.png";
                        string candidateRelative = $"{CaptureDirectory}/{view.name}_candidate.png";
                        byte[] sourcePng = sourceTexture.EncodeToPNG();
                        byte[] candidatePng = candidateTexture.EncodeToPNG();
                        File.WriteAllBytes(Path.Combine(projectRoot, sourceRelative), sourcePng);
                        File.WriteAllBytes(Path.Combine(projectRoot, candidateRelative), candidatePng);
                        PixelComparison comparison = Compare(
                            sourceTexture.GetPixels32(),
                            candidateTexture.GetPixels32(),
                            ChangedChannelThreshold);
                        bool passed = comparison.meanChannelDelta <= MaximumMeanChannelDelta &&
                                      comparison.changedPixelRatio <= MaximumChangedPixelRatio &&
                                      comparison.sourceLumaVariance > 0.0001f;
                        rows.Add(new CaptureRow
                        {
                            view = view.name,
                            result = passed ? "Passed" : "Rejected",
                            sourcePath = sourceRelative,
                            candidatePath = candidateRelative,
                            sourceSha256 = Sha256(sourcePng),
                            candidateSha256 = Sha256(candidatePng),
                            meanChannelDelta = comparison.meanChannelDelta,
                            maximumChannelDelta = comparison.maximumChannelDelta,
                            changedPixelRatio = comparison.changedPixelRatio,
                            sourceLumaVariance = comparison.sourceLumaVariance,
                            cameraPosition = ToArray(view.position),
                            cameraRotation = ToArray(view.rotation.eulerAngles),
                            orthographic = view.orthographic ? 1 : 0,
                            fieldOfView = view.fieldOfView,
                            orthographicSize = view.orthographicSize
                        });
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(sourceTexture);
                        UnityEngine.Object.DestroyImmediate(candidateTexture);
                    }
                }

                int rejected = rows.Count(row => !string.Equals(row.result, "Passed", StringComparison.Ordinal));
                var report = new CaptureReport
                {
                    schema = "warline.operation-map.fixed-camera-parity",
                    schemaVersion = 1,
                    operationMapId = OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                    contentScope = "ExistingAcceptedMapOnly",
                    generatedCityState = "NotGenerated",
                    combinedGeneratedParityAccepted = 0,
                    result = rejected == 0 ? "FixedCameraParityPassed" : "FixedCameraParityRejected",
                    width = Width,
                    height = Height,
                    sourceRendererCount = sets.sourceRenderers.Count,
                    candidateRendererCount = sets.candidateRenderers.Count,
                    viewCount = rows.Count,
                    rejectedViewCount = rejected,
                    maximumMeanChannelDelta = MaximumMeanChannelDelta,
                    maximumChangedPixelRatio = MaximumChangedPixelRatio,
                    rows = rows
                };
                WriteReport(projectRoot, report);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (rejected != 0)
                    throw new InvalidOperationException($"Fixed-camera parity rejected {rejected}/{rows.Count} views. Report: {ReportPath}");
                Debug.Log(
                    $"[OperationMapFixedCameraParity] result={report.result} views={rows.Count} " +
                    $"sourceRenderers={report.sourceRendererCount} candidateRenderers={report.candidateRendererCount} " +
                    $"report={ReportPath}");
                UnityEngine.Object.DestroyImmediate(captureLight.gameObject);
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
            finally
            {
                for (int i = 0; i < rendererStates.Count; i++)
                    rendererStates[i].Restore();
                for (int i = 0; i < lightStates.Count; i++)
                    lightStates[i].Restore();
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.ambientIntensity = previousAmbientIntensity;
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] previousSetup)
        {
            if (previousSetup != null && previousSetup.Any(entry => entry.isLoaded && entry.isActive))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        internal static PixelComparison Compare(Color32[] source, Color32[] candidate, byte changedThreshold)
        {
            if (source == null || candidate == null || source.Length == 0 || source.Length != candidate.Length)
                throw new InvalidOperationException("Pixel buffers must be non-empty and equal in length.");
            long totalDelta = 0;
            int maximumDelta = 0;
            int changed = 0;
            double lumaSum = 0d;
            double lumaSquaredSum = 0d;
            for (int i = 0; i < source.Length; i++)
            {
                int red = Math.Abs(source[i].r - candidate[i].r);
                int green = Math.Abs(source[i].g - candidate[i].g);
                int blue = Math.Abs(source[i].b - candidate[i].b);
                int alpha = Math.Abs(source[i].a - candidate[i].a);
                int pixelMaximum = Math.Max(Math.Max(red, green), Math.Max(blue, alpha));
                totalDelta += red + green + blue + alpha;
                maximumDelta = Math.Max(maximumDelta, pixelMaximum);
                if (pixelMaximum > changedThreshold)
                    changed++;
                double luma = (0.2126d * source[i].r + 0.7152d * source[i].g + 0.0722d * source[i].b) / 255d;
                lumaSum += luma;
                lumaSquaredSum += luma * luma;
            }
            double meanLuma = lumaSum / source.Length;
            return new PixelComparison(
                (float)(totalDelta / (source.Length * 4d * 255d)),
                maximumDelta / 255f,
                changed / (float)source.Length,
                (float)Math.Max(0d, lumaSquaredSum / source.Length - meanLuma * meanLuma));
        }

        private static CaptureSets BuildCaptureSets(IReadOnlyList<Scene> sources, Scene candidate)
        {
            OperationMapEntityPresentationIdentityAuthoring[] identities = candidate.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true))
                .OrderBy(identity => identity.SourceGlobalObjectId, StringComparer.Ordinal)
                .ToArray();
            var sourceRenderers = new HashSet<Renderer>();
            var candidateRenderers = new HashSet<Renderer>();
            foreach (OperationMapEntityPresentationIdentityAuthoring identity in identities)
            {
                if (!GlobalObjectId.TryParse(identity.SourceGlobalObjectId, out GlobalObjectId sourceId) ||
                    GlobalObjectId.GlobalObjectIdentifierToObjectSlow(sourceId) is not GameObject sourceOwner ||
                    !sources.Any(scene => scene == sourceOwner.scene))
                    throw new InvalidOperationException("Fixed-camera source identity did not resolve exactly once: " + identity.SourceGlobalObjectId);
                foreach (Renderer renderer in sourceOwner.GetComponentsInChildren<Renderer>(true))
                    sourceRenderers.Add(renderer);
                foreach (Renderer renderer in identity.GetComponentsInChildren<Renderer>(true))
                    candidateRenderers.Add(renderer);
            }
            if (identities.Length != OperationMapEntityPresentationIdentityBackfillEditor.ExpectedIdentityCount ||
                sourceRenderers.Count == 0 || candidateRenderers.Count == 0)
                throw new InvalidOperationException("Fixed-camera capture set is incomplete.");
            return new CaptureSets(
                sourceRenderers.OrderBy(GetPath, StringComparer.Ordinal).ToList(),
                candidateRenderers.OrderBy(GetPath, StringComparer.Ordinal).ToList());
        }

        private static List<Renderer> BuildDenseEditorRenderers(
            Scene candidate,
            out int legacyIdentityCount,
            out int denseIdentityCount)
        {
            OperationMapEntityPresentationIdentityAuthoring[] legacy =
                candidate.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        OperationMapEntityPresentationIdentityAuthoring>(true))
                    .ToArray();
            DenseCityPresentationIdentityAuthoring[] dense =
                candidate.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        DenseCityPresentationIdentityAuthoring>(true))
                    .ToArray();
            legacyIdentityCount = legacy.Length;
            denseIdentityCount = dense.Length;
            if (legacyIdentityCount !=
                    OperationMapEntityPresentationIdentityBackfillEditor.ExpectedIdentityCount ||
                denseIdentityCount != ExpectedDenseIdentityCount)
            {
                throw new InvalidOperationException(
                    $"Dense fixed-camera identity counts differ: legacy={legacyIdentityCount}, " +
                    $"dense={denseIdentityCount}.");
            }

            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < legacy.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(legacy[i].SourceGlobalObjectId) ||
                    !identities.Add("legacy:" + legacy[i].SourceGlobalObjectId))
                {
                    throw new InvalidOperationException(
                        "Dense fixed-camera legacy identity is empty or duplicated.");
                }
            }
            for (int i = 0; i < dense.Length; i++)
            {
                if (!dense[i].TryValidate(out string error) ||
                    !identities.Add("dense:" + dense[i].StableId))
                {
                    throw new InvalidOperationException(
                        "Dense fixed-camera generated identity is invalid or duplicated: " +
                        error);
                }
            }

            var owned = new HashSet<Renderer>();
            for (int i = 0; i < legacy.Length; i++)
            {
                foreach (Renderer renderer in
                         legacy[i].GetComponentsInChildren<Renderer>(true))
                    owned.Add(renderer);
            }
            for (int i = 0; i < dense.Length; i++)
            {
                foreach (Renderer renderer in dense[i].GetComponentsInChildren<Renderer>(true))
                    owned.Add(renderer);
            }
            OperationMapBuildingAuthoring[] buildings = candidate.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    OperationMapBuildingAuthoring>(true))
                .ToArray();
            for (int i = 0; i < buildings.Length; i++)
            {
                foreach (Renderer renderer in
                         buildings[i].GetComponentsInChildren<Renderer>(true))
                    owned.Add(renderer);
            }
            UnitGridAuthoring[] vehicles = candidate.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<UnitGridAuthoring>(true))
                .ToArray();
            for (int i = 0; i < vehicles.Length; i++)
            {
                foreach (Renderer renderer in
                         vehicles[i].GetComponentsInChildren<Renderer>(true))
                    owned.Add(renderer);
            }

            List<Renderer> active = candidate.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer =>
                    renderer.enabled &&
                    renderer.gameObject.activeInHierarchy &&
                    IsEntitiesGraphicsRenderer(renderer) &&
                    IsFinite(renderer.bounds.center) &&
                    IsFinite(renderer.bounds.extents))
                .OrderBy(GetPath, StringComparer.Ordinal)
                .ToList();
            for (int i = 0; i < active.Count; i++)
            {
                if (!owned.Contains(active[i]))
                {
                    throw new InvalidOperationException(
                        "Dense fixed-camera active renderer has no accepted identity owner: " +
                        GetPath(active[i]));
                }
            }
            return active;
        }

        private static void ApplyInitialDenseVisualState(Scene candidate)
        {
            OperationMapBuildingAuthoring[] buildings = candidate.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    OperationMapBuildingAuthoring>(true))
                .ToArray();
            for (int i = 0; i < buildings.Length; i++)
            {
                SetHierarchyVisible(buildings[i].IntactVisualRoot, true);
                SetHierarchyVisible(buildings[i].DestroyedVisualRoot, false);
            }

            UnitGridAuthoring[] vehicles = candidate.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<UnitGridAuthoring>(true))
                .ToArray();
            for (int i = 0; i < vehicles.Length; i++)
            {
                Transform destroyed = vehicles[i].transform.Find("Destroyed");
                if (destroyed != null)
                    SetHierarchyVisible(destroyed.gameObject, false);
            }
        }

        private static void SetHierarchyVisible(GameObject root, bool visible)
        {
            if (root == null)
                return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.forceRenderingOff = !visible;
        }

        private static bool IsEntitiesGraphicsRenderer(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                return false;
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh != null;
            return renderer is MeshRenderer &&
                   renderer.GetComponent<MeshFilter>()?.sharedMesh != null;
        }

        private static Bounds CalculateBounds(IReadOnlyList<Renderer> renderers)
        {
            var activeBounds = new List<Bounds>(renderers.Count);
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.gameObject.activeInHierarchy || !renderer.enabled)
                    continue;
                Bounds rendererBounds = renderer.bounds;
                if (IsFinite(rendererBounds.center) && IsFinite(rendererBounds.extents))
                    activeBounds.Add(rendererBounds);
            }
            if (activeBounds.Count == 0)
                throw new InvalidOperationException("Fixed-camera source bounds are unavailable or invalid.");

            float[] centersX = activeBounds.Select(bounds => bounds.center.x).OrderBy(value => value).ToArray();
            float[] centersZ = activeBounds.Select(bounds => bounds.center.z).OrderBy(value => value).ToArray();
            float minX = Percentile(centersX, 0.01f);
            float maxX = Percentile(centersX, 0.99f);
            float minZ = Percentile(centersZ, 0.01f);
            float maxZ = Percentile(centersZ, 0.99f);
            float spanX = Mathf.Max(1f, maxX - minX);
            float spanZ = Mathf.Max(1f, maxZ - minZ);
            float minimumY = float.PositiveInfinity;
            float maximumY = float.NegativeInfinity;
            for (int i = 0; i < activeBounds.Count; i++)
            {
                Bounds candidate = activeBounds[i];
                if (candidate.center.x < minX || candidate.center.x > maxX ||
                    candidate.center.z < minZ || candidate.center.z > maxZ ||
                    candidate.size.x > spanX * 0.5f || candidate.size.z > spanZ * 0.5f)
                    continue;
                minimumY = Mathf.Min(minimumY, candidate.min.y);
                maximumY = Mathf.Max(maximumY, candidate.max.y);
            }
            if (!float.IsFinite(minimumY) || !float.IsFinite(maximumY))
                throw new InvalidOperationException("Fixed-camera focus bounds contain no finite detailed renderers.");
            return new Bounds(
                new Vector3((minX + maxX) * 0.5f, (minimumY + maximumY) * 0.5f, (minZ + maxZ) * 0.5f),
                new Vector3(spanX, Mathf.Max(1f, maximumY - minimumY), spanZ));
        }

        internal static float Percentile(float[] sortedValues, float percentile)
        {
            if (sortedValues == null || sortedValues.Length == 0 || percentile < 0f || percentile > 1f)
                throw new InvalidOperationException("A sorted non-empty value set and normalized percentile are required.");
            if (sortedValues.Length == 1)
                return sortedValues[0];
            float index = percentile * (sortedValues.Length - 1);
            int lower = Mathf.FloorToInt(index);
            int upper = Mathf.CeilToInt(index);
            return Mathf.Lerp(sortedValues[lower], sortedValues[upper], index - lower);
        }

        private static IReadOnlyList<ViewSpec> BuildViews(Bounds bounds, IReadOnlyList<Renderer> renderers)
        {
            Vector3 center = bounds.center;
            float horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float height = Mathf.Max(bounds.extents.y, horizontal * 0.15f);
            IReadOnlyList<Vector3> detailTargets = FindDetailTargets(bounds, renderers, 3);
            return new[]
            {
                Perspective("overview", center, new Vector3(-1f, 0.72f, -1f), horizontal * 1.12f + height),
                Orthographic(
                    "minimap",
                    center + Vector3.up * (horizontal * 2f + height),
                    Mathf.Max(bounds.extents.z, bounds.extents.x / (Width / (float)Height)) * 1.08f),
                Perspective("detail_01", detailTargets[0], new Vector3(-0.8f, 0.9f, -0.65f), horizontal * 0.2f + height),
                Perspective("detail_02", detailTargets[1], new Vector3(0.75f, 0.95f, -0.8f), horizontal * 0.2f + height),
                Perspective("detail_03", detailTargets[2], new Vector3(0.8f, 0.9f, 0.65f), horizontal * 0.2f + height)
            };
        }

        private static IReadOnlyList<Vector3> FindDetailTargets(
            Bounds focusBounds,
            IReadOnlyList<Renderer> renderers,
            int count)
        {
            float horizontal = Mathf.Max(focusBounds.size.x, focusBounds.size.z);
            float cellSize = Mathf.Max(10f, horizontal / 12f);
            var cells = new Dictionary<Vector2Int, DetailCell>();
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.gameObject.activeInHierarchy || !renderer.enabled)
                    continue;
                Bounds bounds = renderer.bounds;
                if (bounds.size.x > horizontal * 0.18f || bounds.size.z > horizontal * 0.18f ||
                    bounds.center.x < focusBounds.min.x || bounds.center.x > focusBounds.max.x ||
                    bounds.center.z < focusBounds.min.z || bounds.center.z > focusBounds.max.z)
                    continue;
                var key = new Vector2Int(
                    Mathf.FloorToInt(bounds.center.x / cellSize),
                    Mathf.FloorToInt(bounds.center.z / cellSize));
                cells.TryGetValue(key, out DetailCell cell);
                cell.count++;
                cell.positionSum += bounds.center;
                cells[key] = cell;
            }

            List<Vector3> targets = cells
                .OrderByDescending(pair => pair.Value.count)
                .ThenBy(pair => pair.Key.x)
                .ThenBy(pair => pair.Key.y)
                .Select(pair => pair.Value.positionSum / pair.Value.count)
                .ToList();
            var accepted = new List<Vector3>(count);
            for (int i = 0; i < targets.Count && accepted.Count < count; i++)
            {
                Vector3 target = targets[i];
                if (accepted.All(existing => Vector2.Distance(
                        new Vector2(existing.x, existing.z),
                        new Vector2(target.x, target.z)) >= cellSize * 1.5f))
                    accepted.Add(target);
            }
            while (accepted.Count < count)
                accepted.Add(focusBounds.center);
            return accepted;
        }

        private static ViewSpec Perspective(string name, Vector3 target, Vector3 direction, float distance)
        {
            Vector3 position = target + direction.normalized * distance;
            return new ViewSpec(name, position, Quaternion.LookRotation(target - position, Vector3.up), false, 42f, 0f);
        }

        private static ViewSpec Orthographic(string name, Vector3 position, float size) =>
            new(name, position, Quaternion.LookRotation(Vector3.down, Vector3.forward), true, 60f, size);

        private static Camera CreateCamera(Scene scene)
        {
            var gameObject = new GameObject("OperationMapFixedCameraParityCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            Camera camera = gameObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20000f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            return camera;
        }

        private static Light CreateLight(Scene scene)
        {
            var gameObject = new GameObject("OperationMapFixedCameraParityLight", typeof(Light));
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            gameObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = gameObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f, 1f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            return light;
        }

        private static void ConfigureCamera(Camera camera, ViewSpec view)
        {
            camera.transform.SetPositionAndRotation(view.position, view.rotation);
            camera.orthographic = view.orthographic;
            camera.fieldOfView = view.fieldOfView;
            camera.orthographicSize = view.orthographicSize;
            camera.aspect = Width / (float)Height;
        }

        private static Texture2D Capture(Camera camera)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                name = "OperationMapFixedCameraParityTarget"
            };
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                var request = new RenderPipeline.StandardRequest
                {
                    destination = target
                };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                {
                    throw new InvalidOperationException(
                        "The active render pipeline does not support fixed-camera StandardRequest capture.");
                }

                RenderTexture.active = target;
                RenderPipeline.SubmitRenderRequest(camera, request);
                var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
                texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void WarmUp(Camera camera)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                name = "OperationMapFixedCameraParityWarmup"
            };
            try
            {
                var request = new RenderPipeline.StandardRequest
                {
                    destination = target
                };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                {
                    throw new InvalidOperationException(
                        "The active render pipeline does not support fixed-camera warmup.");
                }

                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            finally
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void ApplyPackedBaseColorPreview(IReadOnlyList<Renderer> renderers)
        {
            for (int rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                renderer.SetPropertyBlock(null);
                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null ||
                        material.shader == null ||
                        !material.shader.name.StartsWith(
                            "Universal Render Pipeline/",
                            StringComparison.Ordinal) ||
                        !material.HasProperty(BaseColorPropertyId))
                    {
                        renderer.SetPropertyBlock(null, materialIndex);
                        continue;
                    }

                    Color color = material.GetColor(BaseColorPropertyId).linear;
                    var block = new MaterialPropertyBlock();
                    block.SetVector(
                        BaseColorPropertyId,
                        new Vector4(color.r, color.g, color.b, color.a));
                    renderer.SetPropertyBlock(block, materialIndex);
                }
            }
        }

        private static void SetVisible(IReadOnlyList<Renderer> renderers, bool visible)
        {
            for (int i = 0; i < renderers.Count; i++)
                renderers[i].forceRenderingOff = !visible;
        }

        private static void WriteReport(string projectRoot, CaptureReport report)
        {
            string path = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? projectRoot);
            File.WriteAllText(path, JsonUtility.ToJson(report, true) + "\n", Utf8WithoutBom);
        }

        private static void RequireAsset(string path)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                throw new InvalidOperationException("Required fixed-camera parity asset is missing: " + path);
        }

        private static string Sha256(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(bytes);
            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }

        private static string Sha256File(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(stream);
            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }

        private static string GetPath(Renderer renderer)
        {
            var names = new Stack<string>();
            Transform current = renderer.transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return renderer.gameObject.scene.path + "::" + string.Join("/", names);
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static float[] ToArray(Vector3 value) => new[] { value.x, value.y, value.z };

        internal readonly struct PixelComparison
        {
            internal PixelComparison(float meanChannelDelta, float maximumChannelDelta, float changedPixelRatio, float sourceLumaVariance)
            {
                this.meanChannelDelta = meanChannelDelta;
                this.maximumChannelDelta = maximumChannelDelta;
                this.changedPixelRatio = changedPixelRatio;
                this.sourceLumaVariance = sourceLumaVariance;
            }

            internal readonly float meanChannelDelta;
            internal readonly float maximumChannelDelta;
            internal readonly float changedPixelRatio;
            internal readonly float sourceLumaVariance;
        }

        private readonly struct CaptureSets
        {
            internal CaptureSets(List<Renderer> sourceRenderers, List<Renderer> candidateRenderers)
            {
                this.sourceRenderers = sourceRenderers;
                this.candidateRenderers = candidateRenderers;
            }

            internal readonly List<Renderer> sourceRenderers;
            internal readonly List<Renderer> candidateRenderers;
        }

        private readonly struct RendererState
        {
            internal RendererState(Renderer renderer)
            {
                this.renderer = renderer;
                forceRenderingOff = renderer.forceRenderingOff;
                globalPropertyBlock = CapturePropertyBlock(renderer);
                Material[] materials = renderer.sharedMaterials;
                materialPropertyBlocks = new MaterialPropertyBlock[materials.Length];
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materialPropertyBlocks[materialIndex] =
                        CapturePropertyBlock(renderer, materialIndex);
                }
            }

            internal readonly Renderer renderer;
            private readonly bool forceRenderingOff;
            private readonly MaterialPropertyBlock globalPropertyBlock;
            private readonly MaterialPropertyBlock[] materialPropertyBlocks;

            internal void Restore()
            {
                if (renderer == null)
                    return;

                renderer.forceRenderingOff = forceRenderingOff;
                renderer.SetPropertyBlock(globalPropertyBlock);
                for (int materialIndex = 0;
                     materialIndex < materialPropertyBlocks.Length;
                     materialIndex++)
                {
                    renderer.SetPropertyBlock(
                        materialPropertyBlocks[materialIndex],
                        materialIndex);
                }
            }

            private static MaterialPropertyBlock CapturePropertyBlock(Renderer renderer)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                return block.isEmpty ? null : block;
            }

            private static MaterialPropertyBlock CapturePropertyBlock(
                Renderer renderer,
                int materialIndex)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, materialIndex);
                return block.isEmpty ? null : block;
            }
        }

        private readonly struct LightState
        {
            internal LightState(Light light)
            {
                this.light = light;
                enabled = light.enabled;
            }

            internal readonly Light light;
            private readonly bool enabled;
            internal void Restore()
            {
                if (light != null)
                    light.enabled = enabled;
            }
        }

        private readonly struct ViewSpec
        {
            internal ViewSpec(string name, Vector3 position, Quaternion rotation, bool orthographic, float fieldOfView, float orthographicSize)
            {
                this.name = name;
                this.position = position;
                this.rotation = rotation;
                this.orthographic = orthographic;
                this.fieldOfView = fieldOfView;
                this.orthographicSize = orthographicSize;
            }

            internal readonly string name;
            internal readonly Vector3 position;
            internal readonly Quaternion rotation;
            internal readonly bool orthographic;
            internal readonly float fieldOfView;
            internal readonly float orthographicSize;
        }

        private struct DetailCell
        {
            internal int count;
            internal Vector3 positionSum;
        }

        [Serializable]
        private sealed class CaptureReport
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string contentScope;
            public string generatedCityState;
            public int combinedGeneratedParityAccepted;
            public string result;
            public int width;
            public int height;
            public int sourceRendererCount;
            public int candidateRendererCount;
            public int viewCount;
            public int rejectedViewCount;
            public float maximumMeanChannelDelta;
            public float maximumChangedPixelRatio;
            public List<CaptureRow> rows;
        }

        [Serializable]
        private sealed class CaptureRow
        {
            public string view;
            public string result;
            public string sourcePath;
            public string candidatePath;
            public string sourceSha256;
            public string candidateSha256;
            public float meanChannelDelta;
            public float maximumChannelDelta;
            public float changedPixelRatio;
            public float sourceLumaVariance;
            public float[] cameraPosition;
            public float[] cameraRotation;
            public int orthographic;
            public float fieldOfView;
            public float orthographicSize;
        }

        [Serializable]
        private sealed class DenseEditorCaptureReport
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string result;
            public string candidateSubScenePath;
            public string candidateSubSceneSha256;
            public int width;
            public int height;
            public int rendererCount;
            public int legacyIdentityCount;
            public int denseIdentityCount;
            public int expectedRuntimeRenderRowCount;
            public int viewCount;
            public float maximumMeanChannelDelta;
            public float maximumChangedPixelRatio;
            public int productionCutover;
            public List<DenseEditorCaptureRow> rows;
        }

        [Serializable]
        private sealed class DenseEditorCaptureRow
        {
            public string view;
            public string editorPath;
            public string editorSha256;
            public float editorLumaVariance;
            public float[] cameraPosition;
            public float[] cameraRotation;
            public int orthographic;
            public float fieldOfView;
            public float orthographicSize;
        }
    }
}

#endif
