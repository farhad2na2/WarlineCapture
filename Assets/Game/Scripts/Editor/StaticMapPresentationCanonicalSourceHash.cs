using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal static class StaticMapPresentationCanonicalSourceHash
    {
        internal static string Compute(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                throw new ArgumentException("Canonical scene path is required.", nameof(scenePath));

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Unable to resolve the Unity project root for canonical hashing.");
            string scenePhysicalPath = ResolvePhysicalAssetPath(projectRoot, scenePath);
            if (string.IsNullOrWhiteSpace(scenePhysicalPath) || !File.Exists(scenePhysicalPath))
                throw new FileNotFoundException("Canonical scene file does not exist.", scenePhysicalPath ?? scenePath);

            string[] dependencies = TraverseSourceDependencyGraph(
                scenePath,
                path => AssetDatabase.GetDependencies(path, false));
            return ComputeDirectDependencySetHash(
                dependencies,
                path => ResolvePhysicalAssetPath(projectRoot, path),
                path => AssetDatabase.AssetPathToGUID(path));
        }

        internal static bool IsGeneratedOutputPath(string assetPath)
        {
            return !string.IsNullOrWhiteSpace(assetPath) &&
                   (string.Equals(assetPath, StaticMapPresentationBaker.OutputRoot, StringComparison.Ordinal) ||
                    assetPath.StartsWith(StaticMapPresentationBaker.OutputRoot + "/", StringComparison.Ordinal));
        }

        internal static string[] TraverseSourceDependencyGraph(
            string rootAssetPath,
            Func<string, IEnumerable<string>> getDirectDependencies)
        {
            if (string.IsNullOrWhiteSpace(rootAssetPath))
                throw new ArgumentException("Root asset path is required.", nameof(rootAssetPath));
            if (getDirectDependencies == null)
                throw new ArgumentNullException(nameof(getDirectDependencies));

            HashSet<string> visited = new(StringComparer.Ordinal);
            SortedSet<string> sourcePaths = new(StringComparer.Ordinal);
            Stack<string> pending = new();
            pending.Push(rootAssetPath);
            while (pending.Count > 0)
            {
                string currentPath = pending.Pop();
                if (string.IsNullOrWhiteSpace(currentPath) || !visited.Add(currentPath))
                    continue;

                if (IsGeneratedOutputPath(currentPath))
                    continue;

                sourcePaths.Add(currentPath);
                IEnumerable<string> directDependencies =
                    getDirectDependencies(currentPath) ?? Array.Empty<string>();
                string[] orderedDependencies = directDependencies
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.Ordinal)
                    .OrderByDescending(path => path, StringComparer.Ordinal)
                    .ToArray();
                for (int i = 0; i < orderedDependencies.Length; i++)
                    pending.Push(orderedDependencies[i]);
            }

            return sourcePaths.ToArray();
        }

        internal static string ComputeDirectDependencySetHash(
            IEnumerable<string> dependencyPaths,
            Func<string, string> physicalPathResolver,
            Func<string, string> stableFallbackResolver)
        {
            if (dependencyPaths == null)
                throw new ArgumentNullException(nameof(dependencyPaths));
            if (physicalPathResolver == null)
                throw new ArgumentNullException(nameof(physicalPathResolver));
            if (stableFallbackResolver == null)
                throw new ArgumentNullException(nameof(stableFallbackResolver));

            string[] dependencies = dependencyPaths
                .Where(path => !string.IsNullOrWhiteSpace(path) && !IsGeneratedOutputPath(path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            StringBuilder builder = new(dependencies.Length * 128);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string dependencyPath = dependencies[i];
                builder.Append(dependencyPath).Append('|');
                string physicalPath = physicalPathResolver(dependencyPath);
                if (!string.IsNullOrWhiteSpace(physicalPath) && File.Exists(physicalPath))
                {
                    AppendFileHash(builder, physicalPath);
                    builder.Append('|');
                    string metadataPath = physicalPath + ".meta";
                    if (File.Exists(metadataPath))
                        AppendFileHash(builder, metadataPath);
                    else
                        builder.Append("meta-missing");
                }
                else
                {
                    builder.Append("virtual:").Append(stableFallbackResolver(dependencyPath) ?? string.Empty);
                }

                builder.Append(';');
            }

            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static void AppendFileHash(StringBuilder builder, string path)
        {
            using SHA256 algorithm = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            byte[] hash = algorithm.ComputeHash(stream);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
        }

        private static string ResolvePhysicalAssetPath(string projectRoot, string assetPath)
        {
            string physicalPath = FileUtil.GetPhysicalPath(assetPath);
            if (!string.IsNullOrWhiteSpace(physicalPath) && File.Exists(physicalPath))
                return physicalPath;

            string projectRelativePath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            return File.Exists(projectRelativePath) ? projectRelativePath : physicalPath;
        }
    }
}
