using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    // Explicit maintenance entry: normal builds continue to reject duplicate
    // dependencies. This uses Addressables' own isolation repair, preserving all
    // existing public addresses, labels and group ownership.
    public static class CampaignReadinessAddressablesBuild
    {
        public static void RepairDependenciesAndBuildAndroid()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("Addressables settings missing.");
            var existing = Snapshot(settings);
            var groups = settings.groups.Where(group => group != null).ToHashSet();
            var rule = new CheckBundleDupeDependencies();
            var analysis = rule.RefreshAnalysis(settings);
            if (analysis.Any(row => row.resultName.IndexOf("Analyze build failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    row.resultName.IndexOf("unsaved scenes", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("Dependency analysis failed; no repair applied.");

            rule.FixIssues(settings);
            var created = settings.groups.Where(group => group != null && !groups.Contains(group)).ToArray();
            foreach (var group in created)
            {
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;
                schema.UseAssetBundleCrc = true;
                schema.UseAssetBundleCrcForCachedBundles = true;
                EditorUtility.SetDirty(schema);
                EditorUtility.SetDirty(group);
            }

            var after = Snapshot(settings);
            foreach (var entry in existing)
                if (!after.TryGetValue(entry.Key, out var state) || state != entry.Value)
                    throw new InvalidOperationException("Dependency repair changed existing address ownership: " + entry.Key);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            if (!OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(true, out var error))
                throw new InvalidOperationException(error);

            string report = "Design/AgentReports/readiness_addressables_dependency_isolation.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            File.WriteAllLines(report, created.SelectMany(group => group.entries)
                .OrderBy(entry => entry.AssetPath, StringComparer.Ordinal)
                .Select(entry => entry.guid + " " + entry.AssetPath));
            Debug.Log("[ReadinessAddressablesRepair] existingAddressesPreserved=" + existing.Count +
                      " isolatedDependencies=" + created.Sum(group => group.entries.Count) +
                      " groups=" + created.Length + " report=" + report);
            // The existing build still validates the resulting bundle layout,
            // package contents and release report. Nothing is allowlisted here.
            BuildScript.BuildAndroid();
        }

        private static Dictionary<string, string> Snapshot(AddressableAssetSettings settings)
        {
            return settings.groups.Where(group => group != null).SelectMany(group => group.entries)
                .ToDictionary(entry => entry.guid, entry => entry.parentGroup.Guid + "\n" +
                    entry.address + "\n" + string.Join("\n", entry.labels.OrderBy(label => label, StringComparer.Ordinal)));
        }
    }
}
