#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Game.Editor
{
    /// <summary>Identifies source/configuration bytes, including uncommitted files, independently of the setup seed.</summary>
    public static class SkirmishCandidateSourceHash
    {
        public static string Compute(string root)
        {
            root = Path.GetFullPath(root);
            var files = new List<string>();
            foreach (string directory in new[] { "Assets", "Packages", "ProjectSettings" })
            {
                string path = Path.Combine(root, directory);
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException(path);
                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    if (IsSource(Path.GetExtension(file))) files.Add(file);
            }
            if (files.Count == 0) throw new InvalidOperationException("Candidate has no source/configuration files.");
            files.Sort(StringComparer.Ordinal);
            using var manifest = new MemoryStream();
            using (var writer = new BinaryWriter(manifest, Encoding.UTF8, true))
            using (var sha = SHA256.Create())
                foreach (string file in files)
                {
                    writer.Write(file.Substring(root.Length + 1).Replace('\\', '/'));
                    using var stream = File.OpenRead(file);
                    byte[] digest = sha.ComputeHash(stream);
                    writer.Write(digest.Length);
                    writer.Write(digest);
                }
            manifest.Position = 0;
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(manifest)).Replace("-", "").ToLowerInvariant();
        }

        private static bool IsSource(string extension) => extension.ToLowerInvariant() is
            ".cs" or ".asmdef" or ".asmref" or ".rsp" or ".json" or ".asset" or
            ".prefab" or ".unity" or ".shader" or ".hlsl" or ".cginc" or ".compute" or ".meta";
    }
}
#endif
