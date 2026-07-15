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
        private static readonly HashSet<string> KnownBinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".3ds", ".7z", ".aac", ".aif", ".aiff", ".apk", ".avi", ".blend", ".bmp",
            ".bundle", ".bytes", ".dll", ".dylib", ".exr", ".fbx", ".flac", ".gif", ".gz",
            ".ico", ".jar", ".jpeg", ".jpg", ".m4a", ".mdb", ".mov", ".mp3", ".mp4",
            ".ogg", ".otf", ".pdf", ".pdb", ".png", ".psd", ".so", ".tga", ".tif",
            ".tiff", ".ttf", ".unitypackage", ".wav", ".webm", ".webp", ".zip"
        };

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
            byte[] hash;
            if (IsTextFile(path))
            {
                using FileStream stream = File.OpenRead(path);
                byte[] input = new byte[8192];
                byte[] normalized = new byte[input.Length + 1];
                bool pendingCarriageReturn = false;
                int read;
                while ((read = stream.Read(input, 0, input.Length)) > 0)
                {
                    int normalizedLength = 0;
                    for (int index = 0; index < read; index++)
                    {
                        byte value = input[index];
                        if (value == '\r')
                        {
                            if (pendingCarriageReturn)
                                normalized[normalizedLength++] = (byte)'\n';
                            pendingCarriageReturn = true;
                            continue;
                        }

                        if (pendingCarriageReturn)
                        {
                            normalized[normalizedLength++] = (byte)'\n';
                            pendingCarriageReturn = false;
                            if (value == '\n')
                                continue;
                        }

                        normalized[normalizedLength++] = value;
                    }

                    if (normalizedLength > 0)
                    {
                        algorithm.TransformBlock(
                            normalized,
                            0,
                            normalizedLength,
                            normalized,
                            0);
                    }
                }

                if (pendingCarriageReturn)
                {
                    normalized[0] = (byte)'\n';
                    algorithm.TransformBlock(normalized, 0, 1, normalized, 0);
                }
                algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                hash = algorithm.Hash;
            }
            else
            {
                using FileStream stream = File.OpenRead(path);
                hash = algorithm.ComputeHash(stream);
            }

            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
        }

        private static bool IsTextFile(string path)
        {
            if (KnownBinaryExtensions.Contains(Path.GetExtension(path)))
                return false;

            using FileStream stream = File.OpenRead(path);
            Decoder decoder = new UTF8Encoding(false, true).GetDecoder();
            byte[] bytes = new byte[8192];
            char[] characters = new char[bytes.Length];
            try
            {
                int read;
                while ((read = stream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    int byteIndex = 0;
                    while (byteIndex < read)
                    {
                        decoder.Convert(
                            bytes,
                            byteIndex,
                            read - byteIndex,
                            characters,
                            0,
                            characters.Length,
                            false,
                            out int bytesUsed,
                            out int charactersUsed,
                            out _);
                        if (!ContainsOnlyTextCharacters(characters, charactersUsed))
                            return false;
                        byteIndex += bytesUsed;
                    }
                }

                decoder.Convert(
                    Array.Empty<byte>(),
                    0,
                    0,
                    characters,
                    0,
                    characters.Length,
                    true,
                    out _,
                    out int finalCharacters,
                    out _);
                return ContainsOnlyTextCharacters(characters, finalCharacters);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static bool ContainsOnlyTextCharacters(char[] characters, int length)
        {
            for (int index = 0; index < length; index++)
            {
                char value = characters[index];
                if (char.IsControl(value) && value != '\t' && value != '\n' && value != '\r')
                    return false;
            }
            return true;
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
