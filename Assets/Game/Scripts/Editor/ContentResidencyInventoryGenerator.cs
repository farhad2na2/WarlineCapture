#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Game.Configs;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Profiling;
    using Object = UnityEngine.Object;

    public static class ContentResidencyInventoryGenerator
    {
        public const string JsonReportPath =
            "Design/AgentReports/architecture_performance_content_residency_baseline.json";

        public const string MarkdownReportPath =
            "Design/AgentReports/architecture_performance_content_residency_baseline.md";

        public const string BaselineCommit = "7084805d771142706f340e9f2e52a68570bcb72b";

        private const string StreamingAssetRootKind = "StreamingAssets";
        private const string AudioAssetRoot = "Assets/Game/Audio/";
        private const string ScopeDescription =
            "Enabled build scenes, Assets Resources content, PlayerSettings preloaded assets, " +
            "and StreamingAssets, including transitive AssetDatabase dependencies.";

        private static readonly MethodInfo AudioCompressedSizeMethod = typeof(AudioImporter).Assembly
            .GetType("UnityEditor.AudioUtil")?
            .GetMethod(
                "GetSoundSize",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(AudioClip) },
                null);

        private static readonly HashSet<string> ExcludedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".asmdef",
            ".asmref",
            ".cs",
            ".meta",
            ".rsp"
        };

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        [MenuItem("Game/Tools/Performance/Generate Content Residency Baseline")]
        public static void GenerateFromMenu()
        {
            GenerateAndWriteReports();
        }

        public static void Run()
        {
            try
            {
                ContentResidencyReport report = GenerateAndWriteReports();
                Debug.Log(
                    $"[ContentResidencyInventory] result=Passed assets={report.Summary.AssetCount} " +
                    $"roots={report.Summary.DependencyRootCount} " +
                    $"catalogAudioClips={report.Summary.CatalogAudioClipCount} " +
                    $"importedSizeAvailable={report.Summary.ImportedSizeAvailableAssetCount} " +
                    $"animationTexturePayloadBytes={report.Summary.AnimationTexturePayloadBytes} " +
                    $"json={JsonReportPath} markdown={MarkdownReportPath}");
                ExitBatchMode(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[ContentResidencyInventory] result=Failed");
                ExitBatchMode(1);
                throw;
            }
        }

        public static ContentResidencyReport GenerateAndWriteReports()
        {
            ContentResidencyReport report = GenerateReport();
            EnsureReportDirectory();
            File.WriteAllText(JsonReportPath, SerializeReport(report) + Environment.NewLine, new UTF8Encoding(false));
            File.WriteAllText(MarkdownReportPath, BuildMarkdown(report), new UTF8Encoding(false));
            return report;
        }

        public static string SerializeReport(ContentResidencyReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            return JsonConvert.SerializeObject(report, JsonSettings);
        }

        public static string BuildMarkdown(ContentResidencyReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            var builder = new StringBuilder(16384);
            builder.AppendLine("# Architecture Performance Content Residency Baseline");
            builder.AppendLine();
            builder.AppendLine($"- Task: `{report.TaskId}`");
            builder.AppendLine($"- Audio residency extension: `{report.AudioResidencyTaskId}`");
            builder.AppendLine($"- Status: `{report.Status}`");
            builder.AppendLine($"- Baseline commit: `{report.BaselineCommit}`");
            builder.AppendLine($"- Generated UTC: `{report.GeneratedUtc ?? "Unavailable"}`");
            builder.AppendLine($"- Unity: `{report.UnityVersion ?? "Unavailable"}`");
            builder.AppendLine($"- Active build target: `{report.ActiveBuildTarget ?? "Unavailable"}`");
            builder.AppendLine($"- Scope: {report.Scope}");
            builder.AppendLine();

            if (!string.Equals(report.Status, "complete", StringComparison.Ordinal))
            {
                builder.AppendLine("## Pending Unity Step");
                builder.AppendLine();
                builder.AppendLine(
                    "This artifact is a schema-valid preflight only. No asset row or imported-size measurement " +
                    "has been generated without the exclusive Unity lease.");
                builder.AppendLine();
            }

            AppendSummary(builder, report.Summary);
            AppendRootTable(builder, report.Roots);
            AppendAssetTable(
                builder,
                "Largest Source Assets",
                report.Assets.Where(asset => asset.SourceSizeBytes.HasValue)
                    .OrderByDescending(asset => asset.SourceSizeBytes)
                    .ThenBy(asset => asset.AssetPath, StringComparer.Ordinal)
                    .Take(25));
            AppendAssetTable(
                builder,
                "Largest Measured Imported Assets",
                report.Assets.Where(asset => asset.ImportedSizeBytes.HasValue)
                    .OrderByDescending(asset => asset.ImportedSizeBytes)
                    .ThenBy(asset => asset.AssetPath, StringComparer.Ordinal)
                    .Take(25));
            AppendCategorySummaries(builder, report.Assets);
            AppendCatalogAudioResidency(builder, report.CatalogAudioClips, report.AudioCatalogAssetPaths);
            AppendAnimationTextureTable(builder, report.Assets);

            builder.AppendLine("## Measurement Boundaries");
            builder.AppendLine();
            for (int i = 0; i < report.Limitations.Count; i++)
                builder.AppendLine($"- {report.Limitations[i]}");

            if (report.Warnings.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("## Warnings");
                builder.AppendLine();
                for (int i = 0; i < report.Warnings.Count; i++)
                    builder.AppendLine($"- {EscapeMarkdown(report.Warnings[i])}");
            }

            return builder.ToString();
        }

        public static bool IsPotentialPlayerContentPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) &&
                !normalized.StartsWith("Packages/", StringComparison.Ordinal))
            {
                return false;
            }

            if (normalized.StartsWith("Assets/Editor/", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("Packages/Editor/", StringComparison.OrdinalIgnoreCase) ||
                normalized.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return !ExcludedExtensions.Contains(Path.GetExtension(normalized));
        }

        public static bool IsGeneratedAnimationTexturePath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            string normalized = assetPath.Replace('\\', '/');
            string fileName = Path.GetFileNameWithoutExtension(normalized);
            return string.Equals(Path.GetExtension(normalized), ".asset", StringComparison.OrdinalIgnoreCase) &&
                   normalized.IndexOf("/ModelResources/", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   fileName.StartsWith("AnimationTexture", StringComparison.Ordinal);
        }

        public static string GetAudioCategory(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith(AudioAssetRoot, StringComparison.Ordinal))
                return null;

            string relative = normalized.Substring(AudioAssetRoot.Length);
            int separatorIndex = relative.IndexOf('/');
            return separatorIndex > 0 ? relative.Substring(0, separatorIndex) : null;
        }

        public static long EstimateDecodedAudioSizeBytes(long sampleFrames, int channels)
        {
            if (sampleFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(sampleFrames));
            if (channels <= 0)
                throw new ArgumentOutOfRangeException(nameof(channels));

            return checked(sampleFrames * channels * sizeof(float));
        }

        private static ContentResidencyReport GenerateReport()
        {
            string activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            string importerPlatform = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString();
            var report = new ContentResidencyReport
            {
                Status = "complete",
                GeneratedUtc = DateTime.UtcNow.ToString("O"),
                UnityVersion = Application.unityVersion,
                ActiveBuildTarget = activeBuildTarget,
                ImporterPlatform = importerPlatform,
                Scope = ScopeDescription
            };
            report.Limitations.Add(
                "Build inclusion is a deterministic dependency-root inventory, not a BuildReport. " +
                "Exact APK/AAB contribution remains APH-500 work.");
            report.Limitations.Add(
                "Imported size is reported only for loaded Texture, AudioClip, and Mesh objects through " +
                "Profiler.GetRuntimeMemorySizeLong; unsupported assets remain JSON null.");
            report.Limitations.Add(
                "Dependency inclusion does not prove simultaneous runtime residency or unload lifetime.");
            report.Limitations.Add(
                "Source size is the project/package file length. Native built-in resources without a project path are excluded.");
            report.Limitations.Add(
                "Catalog audio compressed size is Unity's imported storage-memory measurement from " +
                "AudioUtil.GetSoundSize, not source WAV size or final APK/AAB contribution.");
            report.Limitations.Add(
                "Catalog audio decoded size is estimated as sample frames x channels x 4-byte PCM float samples; " +
                "it excludes engine/object overhead and does not claim simultaneous residency.");

            List<DependencyRootRecord> roots = DiscoverDependencyRoots(report.Warnings);
            report.Roots.AddRange(roots);

            Dictionary<string, ContentResidencyAssetRecord> records = CollectDependencyRecords(roots, report.Warnings);
            foreach (ContentResidencyAssetRecord record in records.Values.OrderBy(asset => asset.AssetPath, StringComparer.Ordinal))
                report.Assets.Add(record);

            report.CatalogAudioClips.AddRange(CollectCatalogAudioResidency(
                report.AudioCatalogAssetPaths,
                report.Warnings));
            report.Summary = BuildSummary(report.Roots, report.Assets, report.CatalogAudioClips);
            return report;
        }

        private static List<CatalogAudioResidencyRecord> CollectCatalogAudioResidency(
            List<string> catalogAssetPaths,
            List<string> warnings)
        {
            var records = new Dictionary<string, CatalogAudioResidencyRecord>(StringComparer.Ordinal);
            string[] catalogPaths = AssetDatabase.FindAssets($"t:{nameof(AudioEventCatalogConfig)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            for (int catalogIndex = 0; catalogIndex < catalogPaths.Length; catalogIndex++)
            {
                string catalogPath = catalogPaths[catalogIndex];
                AudioEventCatalogConfig catalog = AssetDatabase.LoadAssetAtPath<AudioEventCatalogConfig>(catalogPath);
                if (catalog == null)
                {
                    warnings.Add($"Audio event catalog could not be loaded: '{catalogPath}'.");
                    continue;
                }

                catalogAssetPaths.Add(catalogPath);
                IReadOnlyList<AudioEventCatalogEntry> events = catalog.Events;
                for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
                {
                    AudioEventCatalogEntry audioEvent = events[eventIndex];
                    if (audioEvent == null)
                    {
                        warnings.Add($"Audio event catalog '{catalogPath}' contains a null event at index {eventIndex}.");
                        continue;
                    }

                    CollectCatalogAudioClipSet(
                        audioEvent,
                        audioEvent.Clips,
                        catalogPath,
                        localeCode: null,
                        records,
                        warnings);
                    IReadOnlyList<LocalizedAudioClipSet> localizedSets = audioEvent.LocalizedClips;
                    for (int setIndex = 0; setIndex < localizedSets.Count; setIndex++)
                    {
                        LocalizedAudioClipSet localizedSet = localizedSets[setIndex];
                        if (localizedSet == null)
                            continue;
                        CollectCatalogAudioClipSet(
                            audioEvent,
                            localizedSet.Clips,
                            catalogPath,
                            localizedSet.LocaleCode,
                            records,
                            warnings);
                    }
                }
            }

            if (catalogAssetPaths.Count == 0)
                warnings.Add("No AudioEventCatalogConfig assets were found for catalog audio residency reporting.");

            return records.Values
                .OrderBy(record => string.Join(",", record.BusIds), StringComparer.Ordinal)
                .ThenBy(record => record.Category, StringComparer.Ordinal)
                .ThenBy(record => record.AssetPath, StringComparer.Ordinal)
                .ToList();
        }

        private static void CollectCatalogAudioClipSet(
            AudioEventCatalogEntry audioEvent,
            IReadOnlyList<AudioClipWeightEntry> clips,
            string catalogPath,
            string localeCode,
            Dictionary<string, CatalogAudioResidencyRecord> records,
            List<string> warnings)
        {
            for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            {
                AudioClip clip = clips[clipIndex]?.Clip;
                string localeLabel = string.IsNullOrWhiteSpace(localeCode) ? "default" : localeCode;
                if (clip == null)
                {
                    warnings.Add(
                        $"Audio event '{audioEvent.EventId}' in '{catalogPath}' contains a null {localeLabel} clip " +
                        $"at index {clipIndex}.");
                    continue;
                }

                string clipPath = AssetDatabase.GetAssetPath(clip)?.Replace('\\', '/');
                if (string.IsNullOrWhiteSpace(clipPath))
                {
                    warnings.Add(
                        $"Audio event '{audioEvent.EventId}' has a {localeLabel} clip without an AssetDatabase path.");
                    continue;
                }

                if (!records.TryGetValue(clipPath, out CatalogAudioResidencyRecord record))
                {
                    record = InspectCatalogAudioClip(clipPath, clip, warnings);
                    records.Add(clipPath, record);
                }

                AddUniqueSorted(record.EventIds, audioEvent.EventId);
                AddUniqueSorted(record.BusIds, audioEvent.BusId);
            }
        }

        private static CatalogAudioResidencyRecord InspectCatalogAudioClip(
            string assetPath,
            AudioClip clip,
            List<string> warnings)
        {
            string category = GetAudioCategory(assetPath);
            if (category == null)
            {
                category = "Uncategorized";
                warnings.Add($"Catalog audio clip is outside the profiled audio category root: '{assetPath}'.");
            }

            var record = new CatalogAudioResidencyRecord
            {
                AssetPath = assetPath,
                Category = category,
                DurationSeconds = clip.frequency > 0 ? (double)clip.samples / clip.frequency : clip.length,
                SampleFrames = clip.samples,
                Channels = clip.channels,
                FrequencyHz = clip.frequency,
                EstimatedDecodedSizeBytes = EstimateDecodedAudioSizeBytes(clip.samples, clip.channels)
            };

            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                warnings.Add($"Catalog audio clip does not have an AudioImporter: '{assetPath}'.");
            }
            else
            {
                ResolveAudioLoadType(importer, out string loadType, out string loadTypeSource);
                record.ImportLoadType = loadType;
                record.ImportLoadTypeSource = loadTypeSource;
            }

            record.CompressedSizeBytes = TryMeasureAudioCompressedSize(clip, assetPath, warnings);
            if (record.CompressedSizeBytes.HasValue)
                record.CompressedSizeMeasurement = "UnityEditor.AudioUtil.GetSoundSize";

            return record;
        }

        private static List<DependencyRootRecord> DiscoverDependencyRoots(List<string> warnings)
        {
            var roots = new List<DependencyRootRecord>();
            var rootKeys = new HashSet<string>(StringComparer.Ordinal);

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < buildScenes.Length; i++)
            {
                EditorBuildSettingsScene scene = buildScenes[i];
                if (scene.enabled)
                    AddRoot(roots, rootKeys, "BuildScene", scene.path);
            }

            Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            for (int i = 0; i < preloadedAssets.Length; i++)
            {
                string path = AssetDatabase.GetAssetPath(preloadedAssets[i]);
                AddRoot(roots, rootKeys, "PreloadedAsset", path);
            }

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (IsResourcesAssetPath(path) && !AssetDatabase.IsValidFolder(path))
                    AddRoot(roots, rootKeys, "ResourcesAsset", path);
            }

            AddStreamingAssetRoots(roots, rootKeys, warnings);
            roots.Sort(CompareRoots);
            return roots;
        }

        private static Dictionary<string, ContentResidencyAssetRecord> CollectDependencyRecords(
            IReadOnlyList<DependencyRootRecord> roots,
            List<string> warnings)
        {
            var records = new Dictionary<string, ContentResidencyAssetRecord>(StringComparer.Ordinal);
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                DependencyRootRecord root = roots[rootIndex];
                string[] dependencies;
                try
                {
                    dependencies = string.Equals(root.Kind, StreamingAssetRootKind, StringComparison.Ordinal)
                        ? new[] { root.AssetPath }
                        : AssetDatabase.GetDependencies(root.AssetPath, true);
                }
                catch (Exception exception)
                {
                    warnings.Add($"Dependency scan failed for {root.Kind} root '{root.AssetPath}': {exception.Message}");
                    dependencies = new[] { root.AssetPath };
                }

                for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
                {
                    string dependencyPath = dependencies[dependencyIndex].Replace('\\', '/');
                    if (!IsPotentialPlayerContentPath(dependencyPath) || AssetDatabase.IsValidFolder(dependencyPath))
                        continue;

                    if (!records.TryGetValue(dependencyPath, out ContentResidencyAssetRecord record))
                    {
                        record = InspectAsset(dependencyPath, warnings);
                        records.Add(dependencyPath, record);
                    }

                    AddDependencyRoot(record, root);
                }
            }

            return records;
        }

        private static ContentResidencyAssetRecord InspectAsset(string assetPath, List<string> warnings)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            Type mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            var record = new ContentResidencyAssetRecord
            {
                AssetPath = assetPath,
                AssetType = mainType?.Name ?? importer?.GetType().Name ?? "RawFile",
                SourceSizeBytes = TryGetSourceSize(assetPath)
            };

            Object[] objects = Array.Empty<Object>();
            if (ShouldLoadContentObjects(mainType, importer))
            {
                try
                {
                    objects = AssetDatabase.LoadAllAssetsAtPath(assetPath) ?? Array.Empty<Object>();
                }
                catch (Exception exception)
                {
                    warnings.Add($"Asset inspection failed for '{assetPath}': {exception.Message}");
                }
            }

            record.ImportedSizeBytes = MeasureImportedSize(objects);
            if (record.ImportedSizeBytes.HasValue)
            {
                record.ImportedSizeMeasurement =
                    "Profiler.GetRuntimeMemorySizeLong total for Texture, AudioClip, and Mesh objects at this asset path";
            }

            if (importer is AudioImporter audioImporter)
                ApplyAudioMetadata(record, audioImporter);

            Texture texture = objects.OfType<Texture>().FirstOrDefault();
            if (texture != null)
                ApplyTextureMetadata(record, texture, importer as TextureImporter, warnings);

            ApplyMeshMetadata(record, objects.OfType<Mesh>().ToArray(), importer as ModelImporter);
            return record;
        }

        private static bool ShouldLoadContentObjects(Type mainType, AssetImporter importer)
        {
            if (importer is TextureImporter || importer is AudioImporter || importer is ModelImporter)
                return true;

            return mainType != null &&
                   (typeof(Texture).IsAssignableFrom(mainType) ||
                    typeof(AudioClip).IsAssignableFrom(mainType) ||
                    typeof(Mesh).IsAssignableFrom(mainType));
        }

        private static void ApplyAudioMetadata(ContentResidencyAssetRecord record, AudioImporter importer)
        {
            ResolveAudioLoadType(importer, out string loadType, out string loadTypeSource);
            record.AudioLoadType = loadType;
            record.AudioLoadTypeSource = loadTypeSource;
        }

        private static void ResolveAudioLoadType(
            AudioImporter importer,
            out string loadType,
            out string loadTypeSource)
        {
            string platform = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString();
            bool usesOverride = !string.IsNullOrEmpty(platform) && importer.ContainsSampleSettingsOverride(platform);
            AudioImporterSampleSettings settings = usesOverride
                ? importer.GetOverrideSampleSettings(platform)
                : importer.defaultSampleSettings;
            loadType = settings.loadType.ToString();
            loadTypeSource = usesOverride ? $"{platform} override" : "default importer settings";
        }

        private static long? TryMeasureAudioCompressedSize(
            AudioClip clip,
            string assetPath,
            List<string> warnings)
        {
            if (AudioCompressedSizeMethod == null)
            {
                AddWarningOnce(
                    warnings,
                    "UnityEditor.AudioUtil.GetSoundSize is unavailable; catalog compressed sizes are null.");
                return null;
            }

            try
            {
                object value = AudioCompressedSizeMethod.Invoke(null, new object[] { clip });
                long bytes = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return bytes >= 0 ? bytes : null;
            }
            catch (Exception exception)
            {
                warnings.Add($"Compressed-size measurement failed for '{assetPath}': {exception.GetBaseException().Message}");
                return null;
            }
        }

        private static void ApplyTextureMetadata(
            ContentResidencyAssetRecord record,
            Texture texture,
            TextureImporter importer,
            List<string> warnings)
        {
            record.TextureWidth = texture.width;
            record.TextureHeight = texture.height;
            record.TextureFormat = texture.graphicsFormat.ToString();
            record.TextureMipmapsEnabled = importer != null ? importer.mipmapEnabled : texture.mipmapCount > 1;

            if (importer != null)
            {
                record.TextureStreamingEnabled = importer.streamingMipmaps;
            }
            else
            {
                SerializedObject serializedTexture = new(texture);
                SerializedProperty streamingProperty = serializedTexture.FindProperty("m_StreamingMipmaps");
                record.TextureStreamingEnabled = streamingProperty?.boolValue;
            }

            if (!IsGeneratedAnimationTexturePath(record.AssetPath))
                return;

            record.AnimationTexturePayloadBytes = TryGetAnimationTexturePayload(texture as Texture2D);
            if (!record.AnimationTexturePayloadBytes.HasValue)
                warnings.Add($"Animation texture payload was unavailable for '{record.AssetPath}'.");
        }

        private static void ApplyMeshMetadata(
            ContentResidencyAssetRecord record,
            IReadOnlyList<Mesh> meshes,
            ModelImporter importer)
        {
            if (importer != null)
            {
                record.MeshReadWriteEnabled = importer.isReadable;
                record.MeshReadWriteState = importer.isReadable ? "enabled" : "disabled";
                return;
            }

            if (meshes.Count == 0)
                return;

            bool anyReadable = false;
            bool anyNotReadable = false;
            for (int i = 0; i < meshes.Count; i++)
            {
                if (meshes[i].isReadable)
                    anyReadable = true;
                else
                    anyNotReadable = true;
            }

            if (anyReadable && anyNotReadable)
            {
                record.MeshReadWriteEnabled = null;
                record.MeshReadWriteState = "mixed";
            }
            else
            {
                record.MeshReadWriteEnabled = anyReadable;
                record.MeshReadWriteState = anyReadable ? "enabled" : "disabled";
            }
        }

        private static long? MeasureImportedSize(IReadOnlyList<Object> objects)
        {
            long total = 0;
            bool available = false;
            for (int i = 0; i < objects.Count; i++)
            {
                Object asset = objects[i];
                if (asset is not Texture && asset is not AudioClip && asset is not Mesh)
                    continue;

                long bytes = Profiler.GetRuntimeMemorySizeLong(asset);
                if (bytes < 0)
                    continue;

                total += bytes;
                available = true;
            }

            return available ? total : null;
        }

        private static long? TryGetAnimationTexturePayload(Texture2D texture)
        {
            if (texture == null)
                return null;

            SerializedObject serializedTexture = new(texture);
            SerializedProperty completeImageSize = serializedTexture.FindProperty("m_CompleteImageSize");
            if (completeImageSize != null)
                return completeImageSize.longValue;

            if (!texture.isReadable)
                return null;

            try
            {
                return texture.GetRawTextureData<byte>().Length;
            }
            catch (UnityException)
            {
                return null;
            }
        }

        internal static ContentResidencySummary BuildSummary(
            IReadOnlyCollection<DependencyRootRecord> roots,
            IReadOnlyCollection<ContentResidencyAssetRecord> assets,
            IReadOnlyCollection<CatalogAudioResidencyRecord> catalogAudioClips)
        {
            IReadOnlyList<ContentResidencyAssetRecord> textureRows =
                BuildDeterministicTexture2DRows(assets);
            return new ContentResidencySummary
            {
                DependencyRootCount = roots.Count,
                AssetCount = assets.Count,
                SourceSizeAvailableAssetCount = assets.Count(asset => asset.SourceSizeBytes.HasValue),
                SourceSizeBytes = assets.Where(asset => asset.SourceSizeBytes.HasValue)
                    .Sum(asset => asset.SourceSizeBytes.GetValueOrDefault()),
                ImportedSizeAvailableAssetCount = assets.Count(asset => asset.ImportedSizeBytes.HasValue),
                ImportedSizeBytes = assets.Where(asset => asset.ImportedSizeBytes.HasValue)
                    .Sum(asset => asset.ImportedSizeBytes.GetValueOrDefault()),
                AudioAssetCount = assets.Count(asset => asset.AudioLoadType != null),
                TextureAssetCount = textureRows.Count,
                TextureStreamingEnabledCount = textureRows.Count(asset => asset.TextureStreamingEnabled == true),
                MeshAssetCount = assets.Count(asset => asset.MeshReadWriteState != null),
                MeshReadWriteEnabledCount = assets.Count(asset => asset.MeshReadWriteEnabled == true),
                AnimationTextureCount = assets.Count(asset => asset.AnimationTexturePayloadBytes.HasValue),
                AnimationTexturePayloadBytes = assets.Where(asset => asset.AnimationTexturePayloadBytes.HasValue)
                    .Sum(asset => asset.AnimationTexturePayloadBytes.GetValueOrDefault()),
                CatalogAudioClipCount = catalogAudioClips.Count,
                CatalogAudioDurationSeconds = catalogAudioClips.Sum(clip => clip.DurationSeconds),
                CatalogAudioCompressedSizeAvailableClipCount =
                    catalogAudioClips.Count(clip => clip.CompressedSizeBytes.HasValue),
                CatalogAudioCompressedSizeBytes = catalogAudioClips
                    .Where(clip => clip.CompressedSizeBytes.HasValue)
                    .Sum(clip => clip.CompressedSizeBytes.GetValueOrDefault()),
                CatalogAudioEstimatedDecodedSizeBytes = catalogAudioClips
                    .Sum(clip => clip.EstimatedDecodedSizeBytes)
            };
        }

        internal static IReadOnlyList<ContentResidencyAssetRecord> BuildDeterministicTexture2DRows(
            IEnumerable<ContentResidencyAssetRecord> assets)
        {
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));

            return assets
                .Where(asset => asset != null &&
                                string.Equals(asset.AssetType, nameof(Texture2D), StringComparison.Ordinal) &&
                                !string.IsNullOrWhiteSpace(asset.AssetPath))
                .GroupBy(asset => asset.AssetPath, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(asset => asset.AssetPath, StringComparer.Ordinal)
                .ToArray();
        }

        private static void AppendSummary(StringBuilder builder, ContentResidencySummary summary)
        {
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine("| Metric | Value |");
            builder.AppendLine("|---|---:|");
            builder.AppendLine($"| Dependency roots | {summary.DependencyRootCount:N0} |");
            builder.AppendLine($"| Included asset paths | {summary.AssetCount:N0} |");
            builder.AppendLine($"| Assets with source size | {summary.SourceSizeAvailableAssetCount:N0} |");
            builder.AppendLine($"| Known source bytes | {FormatBytes(summary.SourceSizeBytes)} |");
            builder.AppendLine($"| Assets with measured imported size | {summary.ImportedSizeAvailableAssetCount:N0} |");
            builder.AppendLine($"| Known imported bytes | {FormatBytes(summary.ImportedSizeBytes)} |");
            builder.AppendLine($"| Audio assets | {summary.AudioAssetCount:N0} |");
            builder.AppendLine($"| Catalog-referenced audio clips | {summary.CatalogAudioClipCount:N0} |");
            builder.AppendLine($"| Catalog audio duration | {FormatDuration(summary.CatalogAudioDurationSeconds)} |");
            builder.AppendLine(
                $"| Catalog clips with compressed size | " +
                $"{summary.CatalogAudioCompressedSizeAvailableClipCount:N0} / {summary.CatalogAudioClipCount:N0} |");
            builder.AppendLine($"| Known catalog compressed bytes | {FormatBytes(summary.CatalogAudioCompressedSizeBytes)} |");
            builder.AppendLine(
                $"| Estimated catalog decoded bytes | {FormatBytes(summary.CatalogAudioEstimatedDecodedSizeBytes)} |");
            builder.AppendLine($"| Texture2D inventory rows | {summary.TextureAssetCount:N0} |");
            builder.AppendLine($"| Streaming-enabled Texture2D rows | {summary.TextureStreamingEnabledCount:N0} |");
            builder.AppendLine($"| Mesh assets | {summary.MeshAssetCount:N0} |");
            builder.AppendLine($"| Read/write-enabled mesh assets | {summary.MeshReadWriteEnabledCount:N0} |");
            builder.AppendLine($"| Animation texture assets | {summary.AnimationTextureCount:N0} |");
            builder.AppendLine($"| Animation texture payload | {FormatBytes(summary.AnimationTexturePayloadBytes)} |");
            builder.AppendLine();
        }

        private static void AppendRootTable(StringBuilder builder, IReadOnlyList<DependencyRootRecord> roots)
        {
            builder.AppendLine("## Dependency Roots");
            builder.AppendLine();
            if (roots.Count == 0)
            {
                builder.AppendLine("Unavailable until Unity generation.");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("| Kind | Asset path |");
            builder.AppendLine("|---|---|");
            for (int i = 0; i < roots.Count; i++)
                builder.AppendLine($"| {EscapeMarkdown(roots[i].Kind)} | `{EscapeMarkdown(roots[i].AssetPath)}` |");
            builder.AppendLine();
        }

        private static void AppendAssetTable(
            StringBuilder builder,
            string heading,
            IEnumerable<ContentResidencyAssetRecord> assets)
        {
            ContentResidencyAssetRecord[] rows = assets.ToArray();
            builder.AppendLine($"## {heading}");
            builder.AppendLine();
            if (rows.Length == 0)
            {
                builder.AppendLine("Unavailable until Unity generation.");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("| Asset | Type | Source | Imported | Dependency roots |");
            builder.AppendLine("|---|---|---:|---:|---|");
            for (int i = 0; i < rows.Length; i++)
            {
                ContentResidencyAssetRecord row = rows[i];
                builder.AppendLine(
                    $"| `{EscapeMarkdown(row.AssetPath)}` | {EscapeMarkdown(row.AssetType)} | " +
                    $"{FormatBytes(row.SourceSizeBytes)} | {FormatBytes(row.ImportedSizeBytes)} | " +
                    $"{FormatRoots(row.DependencyRoots)} |");
            }
            builder.AppendLine();
        }

        private static void AppendCategorySummaries(
            StringBuilder builder,
            IReadOnlyCollection<ContentResidencyAssetRecord> assets)
        {
            builder.AppendLine("## Import States");
            builder.AppendLine();
            builder.AppendLine("### Build-Included Audio Load Types");
            builder.AppendLine();
            AppendGroupedCounts(builder, assets.Where(asset => asset.AudioLoadType != null), asset => asset.AudioLoadType);
            builder.AppendLine("### Texture Mipmap and Streaming");
            builder.AppendLine();
            AppendGroupedCounts(
                builder,
                assets.Where(asset => asset.TextureWidth.HasValue),
                asset => $"mipmaps={FormatNullableBool(asset.TextureMipmapsEnabled)}, " +
                         $"streaming={FormatNullableBool(asset.TextureStreamingEnabled)}");
            builder.AppendLine("### Mesh Read/Write");
            builder.AppendLine();
            AppendGroupedCounts(
                builder,
                assets.Where(asset => asset.MeshReadWriteState != null),
                asset => asset.MeshReadWriteState);
        }

        private static void AppendGroupedCounts(
            StringBuilder builder,
            IEnumerable<ContentResidencyAssetRecord> assets,
            Func<ContentResidencyAssetRecord, string> selector)
        {
            IGrouping<string, ContentResidencyAssetRecord>[] groups = assets
                .GroupBy(selector, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToArray();
            if (groups.Length == 0)
            {
                builder.AppendLine("Unavailable until Unity generation.");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("| State | Asset count |");
            builder.AppendLine("|---|---:|");
            for (int i = 0; i < groups.Length; i++)
                builder.AppendLine($"| {EscapeMarkdown(groups[i].Key)} | {groups[i].Count():N0} |");
            builder.AppendLine();
        }

        private static void AppendCatalogAudioResidency(
            StringBuilder builder,
            IReadOnlyCollection<CatalogAudioResidencyRecord> catalogAudioClips,
            IReadOnlyList<string> catalogAssetPaths)
        {
            CatalogAudioResidencyRecord[] rows = catalogAudioClips
                .OrderBy(record => FormatValues(record.BusIds), StringComparer.Ordinal)
                .ThenBy(record => record.Category, StringComparer.Ordinal)
                .ThenBy(record => record.AssetPath, StringComparer.Ordinal)
                .ToArray();

            builder.AppendLine("## Catalog-Referenced Audio Residency");
            builder.AppendLine();
            builder.AppendLine(
                "This inventory includes only clips directly referenced by serialized `AudioEventCatalogConfig` assets. " +
                "Unreferenced project audio is excluded.");
            builder.AppendLine();

            if (catalogAssetPaths.Count > 0)
            {
                builder.AppendLine("Catalog assets:");
                for (int i = 0; i < catalogAssetPaths.Count; i++)
                    builder.AppendLine($"- `{EscapeMarkdown(catalogAssetPaths[i])}`");
                builder.AppendLine();
            }

            if (rows.Length == 0)
            {
                builder.AppendLine("Unavailable until Unity generation.");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("### Bus and Category Totals");
            builder.AppendLine();
            builder.AppendLine("| Bus | Category | Clips | Duration | Compressed | Estimated decoded |");
            builder.AppendLine("|---|---|---:|---:|---:|---:|");
            var groups = rows
                .GroupBy(record => new { Bus = FormatValues(record.BusIds), record.Category })
                .OrderBy(group => group.Key.Bus, StringComparer.Ordinal)
                .ThenBy(group => group.Key.Category, StringComparer.Ordinal);
            foreach (var group in groups)
            {
                long? compressedBytes = group.All(record => record.CompressedSizeBytes.HasValue)
                    ? group.Sum(record => record.CompressedSizeBytes.GetValueOrDefault())
                    : null;
                builder.AppendLine(
                    $"| {EscapeMarkdown(group.Key.Bus)} | {EscapeMarkdown(group.Key.Category)} | " +
                    $"{group.Count():N0} | {FormatDuration(group.Sum(record => record.DurationSeconds))} | " +
                    $"{FormatBytes(compressedBytes)} | " +
                    $"{FormatBytes(group.Sum(record => record.EstimatedDecodedSizeBytes))} |");
            }
            builder.AppendLine();

            builder.AppendLine("### Catalog Clip Detail");
            builder.AppendLine();
            builder.AppendLine(
                "| Bus | Category | Clip | Event ID(s) | Duration | Channels | Frequency | Import load type | Compressed | Estimated decoded |");
            builder.AppendLine("|---|---|---|---|---:|---:|---:|---|---:|---:|");
            for (int i = 0; i < rows.Length; i++)
            {
                CatalogAudioResidencyRecord row = rows[i];
                builder.AppendLine(
                    $"| {EscapeMarkdown(FormatValues(row.BusIds))} | {EscapeMarkdown(row.Category)} | " +
                    $"`{EscapeMarkdown(row.AssetPath)}` | {EscapeMarkdown(FormatValues(row.EventIds))} | " +
                    $"{FormatDuration(row.DurationSeconds)} | {row.Channels.ToString("N0", CultureInfo.InvariantCulture)} | " +
                    $"{row.FrequencyHz.ToString("N0", CultureInfo.InvariantCulture)} Hz | " +
                    $"{EscapeMarkdown(row.ImportLoadType ?? "Unavailable")} | {FormatBytes(row.CompressedSizeBytes)} | " +
                    $"{FormatBytes(row.EstimatedDecodedSizeBytes)} |");
            }
            builder.AppendLine();
        }

        private static void AppendAnimationTextureTable(
            StringBuilder builder,
            IReadOnlyCollection<ContentResidencyAssetRecord> assets)
        {
            ContentResidencyAssetRecord[] rows = assets
                .Where(asset => IsGeneratedAnimationTexturePath(asset.AssetPath))
                .OrderBy(asset => asset.AssetPath, StringComparer.Ordinal)
                .ToArray();
            builder.AppendLine("## Animation Texture Payload");
            builder.AppendLine();
            if (rows.Length == 0)
            {
                builder.AppendLine("Unavailable until Unity generation, or no generated animation texture is build-included.");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("| Asset | Dimensions | Format | Payload | Imported | Dependency roots |");
            builder.AppendLine("|---|---:|---|---:|---:|---|");
            for (int i = 0; i < rows.Length; i++)
            {
                ContentResidencyAssetRecord row = rows[i];
                string dimensions = row.TextureWidth.HasValue && row.TextureHeight.HasValue
                    ? $"{row.TextureWidth} x {row.TextureHeight}"
                    : "Unavailable";
                builder.AppendLine(
                    $"| `{EscapeMarkdown(row.AssetPath)}` | {dimensions} | " +
                    $"{EscapeMarkdown(row.TextureFormat ?? "Unavailable")} | " +
                    $"{FormatBytes(row.AnimationTexturePayloadBytes)} | {FormatBytes(row.ImportedSizeBytes)} | " +
                    $"{FormatRoots(row.DependencyRoots)} |");
            }
            builder.AppendLine();
        }

        private static void AddStreamingAssetRoots(
            List<DependencyRootRecord> roots,
            HashSet<string> rootKeys,
            List<string> warnings)
        {
            string directory = Application.streamingAssetsPath;
            if (!Directory.Exists(directory))
                return;

            try
            {
                string normalizedDirectory = Path.GetFullPath(directory).Replace('\\', '/').TrimEnd('/');
                foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                {
                    if (string.Equals(Path.GetExtension(file), ".meta", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string normalizedFile = Path.GetFullPath(file).Replace('\\', '/');
                    string relative = normalizedFile.Substring(normalizedDirectory.Length).TrimStart('/');
                    AddRoot(roots, rootKeys, StreamingAssetRootKind, $"Assets/StreamingAssets/{relative}");
                }
            }
            catch (Exception exception)
            {
                warnings.Add($"StreamingAssets discovery failed: {exception.Message}");
            }
        }

        private static bool IsResourcesAssetPath(string assetPath)
        {
            if (!IsPotentialPlayerContentPath(assetPath))
                return false;

            string normalized = assetPath.Replace('\\', '/');
            return normalized.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddRoot(
            List<DependencyRootRecord> roots,
            HashSet<string> rootKeys,
            string kind,
            string assetPath)
        {
            if (!IsPotentialPlayerContentPath(assetPath))
                return;

            string normalized = assetPath.Replace('\\', '/');
            string key = $"{kind}\n{normalized}";
            if (rootKeys.Add(key))
                roots.Add(new DependencyRootRecord { Kind = kind, AssetPath = normalized });
        }

        private static void AddDependencyRoot(
            ContentResidencyAssetRecord record,
            DependencyRootRecord root)
        {
            for (int i = 0; i < record.DependencyRoots.Count; i++)
            {
                DependencyRootRecord existing = record.DependencyRoots[i];
                if (string.Equals(existing.Kind, root.Kind, StringComparison.Ordinal) &&
                    string.Equals(existing.AssetPath, root.AssetPath, StringComparison.Ordinal))
                {
                    return;
                }
            }

            record.DependencyRoots.Add(new DependencyRootRecord
            {
                Kind = root.Kind,
                AssetPath = root.AssetPath
            });
            record.DependencyRoots.Sort(CompareRoots);
        }

        private static void AddUniqueSorted(List<string> values, string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "Unassigned" : value.Trim();
            if (values.Contains(normalized, StringComparer.Ordinal))
                return;

            values.Add(normalized);
            values.Sort(StringComparer.Ordinal);
        }

        private static void AddWarningOnce(List<string> warnings, string warning)
        {
            if (!warnings.Contains(warning, StringComparer.Ordinal))
                warnings.Add(warning);
        }

        private static int CompareRoots(DependencyRootRecord left, DependencyRootRecord right)
        {
            int pathComparison = string.Compare(left.AssetPath, right.AssetPath, StringComparison.Ordinal);
            return pathComparison != 0
                ? pathComparison
                : string.Compare(left.Kind, right.Kind, StringComparison.Ordinal);
        }

        private static long? TryGetSourceSize(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return null;

            string platformPath = assetPath.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(projectRoot, platformPath);
            return File.Exists(fullPath) ? new FileInfo(fullPath).Length : null;
        }

        private static string FormatRoots(IReadOnlyList<DependencyRootRecord> roots)
        {
            if (roots.Count == 0)
                return "Unavailable";

            return string.Join(
                "<br>",
                roots.Select(root => $"{EscapeMarkdown(root.Kind)}: `{EscapeMarkdown(root.AssetPath)}`"));
        }

        private static string FormatNullableBool(bool? value)
        {
            return value.HasValue ? value.Value.ToString().ToLowerInvariant() : "unavailable";
        }

        private static string FormatValues(IReadOnlyList<string> values)
        {
            return values.Count > 0 ? string.Join("<br>", values) : "Unassigned";
        }

        private static string FormatDuration(double seconds)
        {
            return $"{seconds.ToString("0.000", CultureInfo.InvariantCulture)} s";
        }

        private static string FormatBytes(long? bytes)
        {
            return bytes.HasValue ? FormatBytes(bytes.Value) : "Unavailable";
        }

        private static string FormatBytes(long bytes)
        {
            return $"`{bytes:N0}` ({bytes / (1024d * 1024d):N2} MiB)";
        }

        private static string EscapeMarkdown(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        private static void EnsureReportDirectory()
        {
            string directory = Path.GetDirectoryName(JsonReportPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }

        private static void ExitBatchMode(int exitCode)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }
    }

    public sealed class ContentResidencyReport
    {
        public int SchemaVersion { get; set; } = 2;
        public string TaskId { get; set; } = "APH-008";
        public string AudioResidencyTaskId { get; set; } = "APH-400";
        public string Status { get; set; } = "pending-unity-generation";
        public string BaselineCommit { get; set; } = ContentResidencyInventoryGenerator.BaselineCommit;
        public string GeneratedUtc { get; set; }
        public string UnityVersion { get; set; }
        public string ActiveBuildTarget { get; set; }
        public string ImporterPlatform { get; set; }
        public string Scope { get; set; } = string.Empty;
        public ContentResidencySummary Summary { get; set; } = new();
        public List<DependencyRootRecord> Roots { get; } = new();
        public List<ContentResidencyAssetRecord> Assets { get; } = new();
        public List<string> AudioCatalogAssetPaths { get; } = new();
        public List<CatalogAudioResidencyRecord> CatalogAudioClips { get; } = new();
        public List<string> Limitations { get; } = new();
        public List<string> Warnings { get; } = new();
    }

    public sealed class ContentResidencySummary
    {
        public int DependencyRootCount { get; set; }
        public int AssetCount { get; set; }
        public int SourceSizeAvailableAssetCount { get; set; }
        public long SourceSizeBytes { get; set; }
        public int ImportedSizeAvailableAssetCount { get; set; }
        public long ImportedSizeBytes { get; set; }
        public int AudioAssetCount { get; set; }
        public int CatalogAudioClipCount { get; set; }
        public double CatalogAudioDurationSeconds { get; set; }
        public int CatalogAudioCompressedSizeAvailableClipCount { get; set; }
        public long CatalogAudioCompressedSizeBytes { get; set; }
        public long CatalogAudioEstimatedDecodedSizeBytes { get; set; }
        public int TextureAssetCount { get; set; }
        public int TextureStreamingEnabledCount { get; set; }
        public int MeshAssetCount { get; set; }
        public int MeshReadWriteEnabledCount { get; set; }
        public int AnimationTextureCount { get; set; }
        public long AnimationTexturePayloadBytes { get; set; }
    }

    public sealed class DependencyRootRecord
    {
        public string Kind { get; set; } = string.Empty;
        public string AssetPath { get; set; } = string.Empty;
    }

    public sealed class ContentResidencyAssetRecord
    {
        public string AssetPath { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public long? SourceSizeBytes { get; set; }
        public long? ImportedSizeBytes { get; set; }
        public string ImportedSizeMeasurement { get; set; }
        public List<DependencyRootRecord> DependencyRoots { get; } = new();
        public string AudioLoadType { get; set; }
        public string AudioLoadTypeSource { get; set; }
        public int? TextureWidth { get; set; }
        public int? TextureHeight { get; set; }
        public string TextureFormat { get; set; }
        public bool? TextureMipmapsEnabled { get; set; }
        public bool? TextureStreamingEnabled { get; set; }
        public bool? MeshReadWriteEnabled { get; set; }
        public string MeshReadWriteState { get; set; }
        public long? AnimationTexturePayloadBytes { get; set; }
    }

    public sealed class CatalogAudioResidencyRecord
    {
        public string AssetPath { get; set; } = string.Empty;
        public List<string> EventIds { get; } = new();
        public List<string> BusIds { get; } = new();
        public string Category { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public int SampleFrames { get; set; }
        public int Channels { get; set; }
        public int FrequencyHz { get; set; }
        public string ImportLoadType { get; set; }
        public string ImportLoadTypeSource { get; set; }
        public long? CompressedSizeBytes { get; set; }
        public string CompressedSizeMeasurement { get; set; }
        public long EstimatedDecodedSizeBytes { get; set; }
    }
}

#endif
