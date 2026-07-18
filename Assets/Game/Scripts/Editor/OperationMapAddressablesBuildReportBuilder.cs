using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Editor
{
    internal static class OperationMapAddressablesBuildReportBuilder
    {
        internal const int SchemaVersion = 1;
        internal const string MapId = "opmap.skirmish.desert_base_01";
        internal const string OutputPath =
            "Design/AgentReports/operation_map_addressables_build_report.json";

        [MenuItem("Game/Operation Maps/Build Local Addressables And Publish Report")]
        public static void Run()
        {
            if (!OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(true, out string error))
                throw new InvalidOperationException(error);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("Addressables settings are required.");

            bool previousGenerateBuildLayout = ProjectConfigData.GenerateBuildLayout;
            try
            {
                ProjectConfigData.GenerateBuildLayout = true;
                CheckBundleDupeDependencies duplicateRule = new();
                List<AnalyzeRule.AnalyzeResult> analyzeResults = duplicateRule.RefreshAnalysis(settings);
                int analyzeIssueCount = analyzeResults.Count(result =>
                    result != null && result.severity != MessageType.None);
                Debug.Log(
                    $"[OperationMapAddressablesAnalyze] result=Passed " +
                    $"rule=duplicate-bundle-dependencies issues={analyzeIssueCount}");

                DateTime buildStartedUtc = DateTime.UtcNow;
                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
                if (result == null || !string.IsNullOrEmpty(result.Error))
                    throw new InvalidOperationException(result?.Error ?? "Addressables content build returned no result.");

                ValidateContentBuildOutput(result.OutputPath);
                string layoutPath = FindLatestBuildLayoutPath(buildStartedUtc);
                BuildLayout layout = BuildLayout.Open(layoutPath, true, true);
                if (layout == null)
                    throw new InvalidOperationException($"Addressables Build Layout could not be read: {layoutPath}");
                try
                {
                    OperationMapAddressablesBuildReport report = Create(layout);
                    if (!TryValidateDuplicateDependencies(report.DuplicateDependencies, out string duplicateError))
                        throw new InvalidOperationException(duplicateError);
                    bool wroteReport = Publish(OutputPath, Serialize(report));
                    Debug.Log(
                        $"[OperationMapAddressablesBuildReport] result=Passed " +
                        $"wrote={(wroteReport ? 1 : 0)} " +
                        $"maps={report.Maps.Length} bytes={report.AggregateBundleBytes} " +
                        $"partitions={report.Partitions.Length} addresses={report.RequiredAddresses.Length} " +
                        $"entitiesArtifacts={report.EntitiesArtifacts.Length} " +
                        $"duplicates={report.DuplicateDependencies.Length}");
                }
                finally
                {
                    layout.Close();
                }
            }
            finally
            {
                ProjectConfigData.GenerateBuildLayout = previousGenerateBuildLayout;
            }
        }

        internal static OperationMapAddressablesBuildReport Create(BuildLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (!string.IsNullOrEmpty(layout.BuildError))
                throw new InvalidOperationException($"Addressables Build Layout contains an error: {layout.BuildError}");
            if (string.IsNullOrEmpty(layout.BuildResultHash))
                throw new InvalidOperationException("Addressables Build Layout has no result hash.");

            List<BuildLayout.ExplicitAsset> mapAssets = EnumerateAssets(layout)
                .Where(HasMapPackLabel)
                .OrderBy(asset => asset.AddressableName, StringComparer.Ordinal)
                .ToList();
            if (mapAssets.Count == 0)
                throw new InvalidOperationException("Addressables Build Layout contains no operation-map pack assets.");

            string[] requiredAddresses = mapAssets
                .Select(asset => asset.AddressableName)
                .Where(address => !string.IsNullOrEmpty(address))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(address => address, StringComparer.Ordinal)
                .ToArray();
            if (requiredAddresses.Length != mapAssets.Count ||
                requiredAddresses.Any(address => !address.StartsWith("operation-map/", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Every operation-map pack asset must have one unique stable operation-map address.");
            }

            HashSet<BuildLayout.Bundle> mapBundles = CollectBundleClosure(mapAssets);
            ulong mapBytes = SumBundleBytes(mapBundles);
            OperationMapAddressablesPartitionReport[] partitions = BuildPartitions(mapAssets);
            OperationMapAddressablesEntitiesArtifactReport[] entitiesArtifacts =
                BuildEntitiesArtifacts(mapBundles);
            OperationMapAddressablesDuplicateDependencyReport[] duplicates =
                BuildDuplicateDependencies(layout, mapBundles);

            return new OperationMapAddressablesBuildReport(
                SchemaVersion,
                layout.BuildResultHash,
                layout.BuildTarget.ToString(),
                mapBytes,
                new[] { new OperationMapAddressablesBuildMapReport(MapId, mapBundles.Count, mapBytes) },
                partitions,
                requiredAddresses,
                entitiesArtifacts,
                duplicates);
        }

        internal static bool TryValidateDuplicateDependencies(
            OperationMapAddressablesDuplicateDependencyReport[] duplicates,
            out string error)
        {
            if (duplicates == null)
            {
                error = "Addressables duplicate-dependency evidence is missing.";
                return false;
            }

            for (int index = 0; index < duplicates.Length; index++)
            {
                OperationMapAddressablesDuplicateDependencyReport duplicate = duplicates[index];
                if (duplicate.AssetPath.StartsWith("Packages/", StringComparison.Ordinal))
                    continue;

                error =
                    $"Unapproved duplicated operation-map dependency: " +
                    $"guid={duplicate.AssetGuid} path={duplicate.AssetPath} " +
                    $"bundles={duplicate.BundleCount} bytes={duplicate.DuplicateBytes}.";
                return false;
            }

            error = null;
            return true;
        }

        internal static string Serialize(OperationMapAddressablesBuildReport report)
        {
            StringBuilder json = new(4096);
            json.Append("{\n  \"schemaVersion\": ").Append(report.SchemaVersion)
                .Append(",\n  \"buildResultHash\": ");
            AppendString(json, report.BuildResultHash);
            json.Append(",\n  \"buildTarget\": ");
            AppendString(json, report.BuildTarget);
            json.Append(",\n  \"aggregateBundleBytes\": ")
                .Append(report.AggregateBundleBytes.ToString(CultureInfo.InvariantCulture));

            json.Append(",\n  \"maps\": [");
            for (int index = 0; index < report.Maps.Length; index++)
            {
                OperationMapAddressablesBuildMapReport map = report.Maps[index];
                AppendSeparator(json, index);
                json.Append("    { \"mapId\": ");
                AppendString(json, map.MapId);
                json.Append(", \"bundleCount\": ").Append(map.BundleCount)
                    .Append(", \"bundleBytes\": ")
                    .Append(map.BundleBytes.ToString(CultureInfo.InvariantCulture)).Append(" }");
            }
            AppendArrayEnd(json, report.Maps.Length);

            json.Append(",\n  \"partitions\": [");
            for (int index = 0; index < report.Partitions.Length; index++)
            {
                OperationMapAddressablesPartitionReport partition = report.Partitions[index];
                AppendSeparator(json, index);
                json.Append("    { \"label\": ");
                AppendString(json, partition.Label);
                json.Append(", \"entryCount\": ").Append(partition.EntryCount)
                    .Append(", \"bundleCount\": ").Append(partition.BundleCount).Append(" }");
            }
            AppendArrayEnd(json, report.Partitions.Length);

            json.Append(",\n  \"requiredAddresses\": [");
            for (int index = 0; index < report.RequiredAddresses.Length; index++)
            {
                AppendSeparator(json, index);
                json.Append("    ");
                AppendString(json, report.RequiredAddresses[index]);
            }
            AppendArrayEnd(json, report.RequiredAddresses.Length);

            json.Append(",\n  \"entitiesArtifacts\": [");
            for (int index = 0; index < report.EntitiesArtifacts.Length; index++)
            {
                OperationMapAddressablesEntitiesArtifactReport artifact = report.EntitiesArtifacts[index];
                AppendSeparator(json, index);
                json.Append("    { \"identity\": ");
                AppendString(json, artifact.Identity);
                json.Append(", \"bytes\": ")
                    .Append(artifact.Bytes.ToString(CultureInfo.InvariantCulture)).Append(" }");
            }
            AppendArrayEnd(json, report.EntitiesArtifacts.Length);

            json.Append(",\n  \"duplicateDependencies\": [");
            for (int index = 0; index < report.DuplicateDependencies.Length; index++)
            {
                OperationMapAddressablesDuplicateDependencyReport duplicate =
                    report.DuplicateDependencies[index];
                AppendSeparator(json, index);
                json.Append("    { \"assetGuid\": ");
                AppendString(json, duplicate.AssetGuid);
                json.Append(", \"assetPath\": ");
                AppendString(json, duplicate.AssetPath);
                json.Append(", \"bundleCount\": ").Append(duplicate.BundleCount)
                    .Append(", \"duplicateBytes\": ")
                    .Append(duplicate.DuplicateBytes.ToString(CultureInfo.InvariantCulture)).Append(" }");
            }
            AppendArrayEnd(json, report.DuplicateDependencies.Length);
            json.Append("\n}\n");
            return json.ToString();
        }

        private static List<BuildLayout.ExplicitAsset> EnumerateAssets(BuildLayout layout)
        {
            List<BuildLayout.ExplicitAsset> assets = new();
            foreach (BuildLayout.Group group in layout.Groups ?? new List<BuildLayout.Group>())
            foreach (BuildLayout.Bundle bundle in group.Bundles ?? new List<BuildLayout.Bundle>())
            foreach (BuildLayout.File file in bundle.Files ?? new List<BuildLayout.File>())
                assets.AddRange(file.Assets ?? new List<BuildLayout.ExplicitAsset>());
            return assets;
        }

        private static bool HasMapPackLabel(BuildLayout.ExplicitAsset asset)
        {
            return asset?.Labels != null && asset.Labels.Contains(
                OperationMapAddressablesLayoutBuilder.PackLabel,
                StringComparer.Ordinal);
        }

        private static HashSet<BuildLayout.Bundle> CollectBundleClosure(
            IEnumerable<BuildLayout.ExplicitAsset> assets)
        {
            HashSet<BuildLayout.Bundle> bundles = new();
            Stack<BuildLayout.Bundle> pending = new(
                assets.Where(asset => asset.Bundle != null).Select(asset => asset.Bundle));
            while (pending.Count > 0)
            {
                BuildLayout.Bundle bundle = pending.Pop();
                if (bundle == null || !bundles.Add(bundle))
                    continue;
                foreach (BuildLayout.Bundle dependency in bundle.Dependencies ?? new List<BuildLayout.Bundle>())
                    pending.Push(dependency);
            }
            return bundles;
        }

        private static ulong SumBundleBytes(IEnumerable<BuildLayout.Bundle> bundles)
        {
            ulong total = 0;
            foreach (BuildLayout.Bundle bundle in bundles)
                total += bundle.FileSize;
            return total;
        }

        private static OperationMapAddressablesPartitionReport[] BuildPartitions(
            IEnumerable<BuildLayout.ExplicitAsset> assets)
        {
            Dictionary<string, List<BuildLayout.ExplicitAsset>> byLabel =
                new(StringComparer.Ordinal);
            foreach (BuildLayout.ExplicitAsset asset in assets)
            foreach (string label in asset.Labels ?? Array.Empty<string>())
            {
                if (!label.StartsWith("operation-map-partition-", StringComparison.Ordinal))
                    continue;
                if (!byLabel.TryGetValue(label, out List<BuildLayout.ExplicitAsset> entries))
                {
                    entries = new List<BuildLayout.ExplicitAsset>();
                    byLabel.Add(label, entries);
                }
                entries.Add(asset);
            }

            return byLabel
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new OperationMapAddressablesPartitionReport(
                    pair.Key,
                    pair.Value.Count,
                    pair.Value.Select(asset => asset.Bundle).Where(bundle => bundle != null).Distinct().Count()))
                .ToArray();
        }

        private static OperationMapAddressablesEntitiesArtifactReport[] BuildEntitiesArtifacts(
            IEnumerable<BuildLayout.Bundle> bundles)
        {
            List<OperationMapAddressablesEntitiesArtifactReport> artifacts = new();
            foreach (BuildLayout.Bundle bundle in bundles.OrderBy(item => item.Name, StringComparer.Ordinal))
            foreach (BuildLayout.File file in bundle.Files ?? new List<BuildLayout.File>())
            foreach (BuildLayout.SubFile subFile in file.SubFiles ?? new List<BuildLayout.SubFile>())
            {
                if (!IsEntitiesArtifact(subFile.Name))
                    continue;
                artifacts.Add(new OperationMapAddressablesEntitiesArtifactReport(
                    $"{bundle.Name}/{subFile.Name}",
                    subFile.Size));
            }
            return artifacts.OrderBy(item => item.Identity, StringComparer.Ordinal).ToArray();
        }

        private static bool IsEntitiesArtifact(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return name.IndexOf("entities", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("entityheader", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.EndsWith(".blob", StringComparison.OrdinalIgnoreCase);
        }

        private static OperationMapAddressablesDuplicateDependencyReport[] BuildDuplicateDependencies(
            BuildLayout layout,
            HashSet<BuildLayout.Bundle> mapBundles)
        {
            List<OperationMapAddressablesDuplicateDependencyReport> rows = new();
            foreach (BuildLayout.AssetDuplicationData duplicate in layout.DuplicatedAssets ??
                     new List<BuildLayout.AssetDuplicationData>())
            {
                HashSet<BuildLayout.Bundle> duplicateBundles = new();
                foreach (BuildLayout.ObjectDuplicationData duplicatedObject in duplicate.DuplicatedObjects ??
                         new List<BuildLayout.ObjectDuplicationData>())
                foreach (BuildLayout.File file in duplicatedObject.IncludedInBundleFiles ??
                         new List<BuildLayout.File>())
                {
                    if (file?.Bundle != null && mapBundles.Contains(file.Bundle))
                        duplicateBundles.Add(file.Bundle);
                }
                if (duplicateBundles.Count < 2)
                    continue;

                List<BuildLayout.DataFromOtherAsset> instances = mapBundles
                    .SelectMany(bundle => bundle.Files ?? new List<BuildLayout.File>())
                    .SelectMany(file => file.OtherAssets ?? new List<BuildLayout.DataFromOtherAsset>())
                    .Where(asset => string.Equals(asset.AssetGuid, duplicate.AssetGuid, StringComparison.Ordinal))
                    .ToList();
                ulong totalBytes = instances.Aggregate(
                    0ul,
                    (sum, asset) => sum + asset.SerializedSize + asset.StreamedSize);
                ulong retainedBytes = instances.Count == 0
                    ? 0
                    : instances.Min(asset => asset.SerializedSize + asset.StreamedSize);
                string assetPath = instances
                    .Select(asset => asset.AssetPath)
                    .FirstOrDefault(path => !string.IsNullOrEmpty(path)) ?? string.Empty;
                rows.Add(new OperationMapAddressablesDuplicateDependencyReport(
                    duplicate.AssetGuid,
                    assetPath,
                    duplicateBundles.Count,
                    totalBytes >= retainedBytes ? totalBytes - retainedBytes : 0));
            }
            return rows.OrderBy(row => row.AssetGuid, StringComparer.Ordinal).ToArray();
        }

        private static string FindLatestBuildLayoutPath(DateTime buildStartedUtc)
        {
            string directory = Addressables.BuildReportPath;
            string path = Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ThenBy(item => item, StringComparer.Ordinal)
                    .FirstOrDefault()
                : null;
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Addressables content build did not produce a Build Layout report.");
            if (File.GetLastWriteTimeUtc(path) < buildStartedUtc.AddSeconds(-2))
                throw new InvalidOperationException(
                    "Addressables content build did not produce a fresh Build Layout report.");
            return path;
        }

        private static void ValidateContentBuildOutput(string settingsPath)
        {
            if (string.IsNullOrEmpty(settingsPath) || !File.Exists(settingsPath))
                throw new InvalidOperationException("Addressables content build did not produce settings.json.");

            if (!TryValidateRuntimeSettings(File.ReadAllText(settingsPath), out string error))
                throw new InvalidOperationException(error);

            string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(settingsPath));
            string catalogPath = Path.Combine(outputDirectory, "catalog.bin");
            string hashPath = Path.Combine(outputDirectory, "catalog.hash");
            if (!File.Exists(catalogPath) || !File.Exists(hashPath))
                throw new InvalidOperationException(
                    "Addressables content build did not produce the local catalog and hash.");
        }

        internal static bool TryValidateRuntimeSettings(string json, out string error)
        {
            if (string.IsNullOrWhiteSpace(json))
                return FailRuntimeSettings("Addressables runtime settings are empty.", out error);

            OperationMapAddressablesRuntimeSettingsRecord settings;
            try
            {
                settings = JsonUtility.FromJson<OperationMapAddressablesRuntimeSettingsRecord>(json);
            }
            catch (Exception exception)
            {
                return FailRuntimeSettings(
                    $"Addressables runtime settings are invalid: {exception.Message}",
                    out error);
            }

            if (settings == null || !settings.m_DisableCatalogUpdateOnStart)
            {
                return FailRuntimeSettings(
                    "Local operation-map content must disable startup catalog updates.",
                    out error);
            }

            if (settings.m_CatalogLocations == null || settings.m_CatalogLocations.Length != 1)
            {
                return FailRuntimeSettings(
                    "Local operation-map content requires exactly one built-in catalog location.",
                    out error);
            }

            string internalId = settings.m_CatalogLocations[0]?.m_InternalId;
            if (string.IsNullOrEmpty(internalId) ||
                !internalId.StartsWith(
                    "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/",
                    StringComparison.Ordinal) ||
                Uri.TryCreate(internalId, UriKind.Absolute, out Uri remoteUri) &&
                (remoteUri.Scheme == Uri.UriSchemeHttp || remoteUri.Scheme == Uri.UriSchemeHttps))
            {
                return FailRuntimeSettings(
                    "Operation-map catalog location must resolve from Addressables.RuntimePath.",
                    out error);
            }

            error = null;
            return true;
        }

        private static bool FailRuntimeSettings(string message, out string error)
        {
            error = message;
            return false;
        }

        internal static bool Publish(string path, string content)
        {
            string absolutePath = Path.GetFullPath(path);
            if (File.Exists(absolutePath) &&
                string.Equals(File.ReadAllText(absolutePath), content, StringComparison.Ordinal))
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            string temporaryPath = absolutePath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));
                if (File.Exists(absolutePath))
                    File.Replace(temporaryPath, absolutePath, null);
                else
                    File.Move(temporaryPath, absolutePath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            return true;
        }

        private static void AppendSeparator(StringBuilder json, int index)
        {
            json.Append(index == 0 ? "\n" : ",\n");
        }

        [Serializable]
        private sealed class OperationMapAddressablesRuntimeSettingsRecord
        {
            public bool m_DisableCatalogUpdateOnStart;
            public OperationMapAddressablesCatalogLocationRecord[] m_CatalogLocations;
        }

        [Serializable]
        private sealed class OperationMapAddressablesCatalogLocationRecord
        {
            public string m_InternalId;
        }

        private static void AppendArrayEnd(StringBuilder json, int count)
        {
            if (count > 0)
                json.Append('\n');
            json.Append("  ]");
        }

        private static void AppendString(StringBuilder json, string value)
        {
            json.Append('"');
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\r': json.Append("\\r"); break;
                    case '\t': json.Append("\\t"); break;
                    default:
                        if (character < 32)
                            json.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            json.Append(character);
                        break;
                }
            }
            json.Append('"');
        }
    }
}
