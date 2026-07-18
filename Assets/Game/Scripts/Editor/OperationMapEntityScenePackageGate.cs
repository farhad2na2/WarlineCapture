using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;

namespace Game.Editor
{
    public static class OperationMapEntityScenePackageGate
    {
        public static void Validate(string packagePath)
        {
            if (!File.Exists(packagePath))
                throw new InvalidOperationException($"Android package not found: {packagePath}");

            string expectedGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.SourceSubScenePath);
            using var archive = ZipFile.OpenRead(packagePath);
            var entries = new List<string>(archive.Entries.Count);
            foreach (ZipArchiveEntry entry in archive.Entries)
                entries.Add(entry.FullName);

            string error = GetValidationError(entries, expectedGuid);
            if (error != null)
                throw new InvalidOperationException(error);
        }

        public static string GetValidationError(
            IReadOnlyList<string> entries,
            string expectedGuid)
        {
            if (entries == null)
                return "Android package entries are required.";
            if (string.IsNullOrEmpty(expectedGuid))
                return "The operation-map source subscene GUID is required.";

            bool hasHeader = false;
            bool hasSection = false;
            bool hasSceneInfo = false;
            string headerSuffix = $"/EntityScenes/{expectedGuid}.entityheader";
            string sectionPrefix = $"/EntityScenes/{expectedGuid}.";

            for (int i = 0; i < entries.Count; i++)
            {
                string path = "/" + entries[i].Replace('\\', '/').TrimStart('/');
                hasHeader |= path.EndsWith(headerSuffix, StringComparison.Ordinal);
                hasSection |= path.IndexOf(sectionPrefix, StringComparison.Ordinal) >= 0 &&
                              path.EndsWith(".entities", StringComparison.Ordinal);
                hasSceneInfo |= path.EndsWith(
                    "/EntityScenes/scene_info.bin",
                    StringComparison.Ordinal);
            }

            if (!hasHeader)
                return $"Android package is missing EntityScenes/{expectedGuid}.entityheader.";
            if (!hasSection)
                return $"Android package is missing an EntityScenes/{expectedGuid}.*.entities section.";
            if (!hasSceneInfo)
                return "Android package is missing EntityScenes/scene_info.bin.";
            return null;
        }
    }
}
