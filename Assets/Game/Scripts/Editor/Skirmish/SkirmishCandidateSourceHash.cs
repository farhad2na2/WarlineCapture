#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

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
            // Reuse one hashing buffer across the entire candidate. Per-file
            // megabyte FileStream buffers caused gigabytes of temporary managed
            // allocations when validating the shared Campaign asset library.
            // The buffered1MiB read was verified against the independent full
            // manifest. Keep normal OS buffering for the repeated Editor census;
            // no source paths, bytes or hash/ordering rules are skipped.
            var buffer = new byte[1024 * 1024];
            using var manifest = new MemoryStream();
            using (var writer = new BinaryWriter(manifest, Encoding.UTF8, true))
            using (var sha = new StreamingFileDigest())
                foreach (string file in files)
                {
                    writer.Write(file.Substring(root.Length + 1).Replace('\\', '/'));
                    using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read,
                        65536, FileOptions.None);
                    byte[] digest = sha.Compute(stream, buffer);
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

        // Mono's managed SHA implementation makes a complete Campaign asset
        // census take minutes. CommonCrypto hashes the same stream bytes and
        // leaves the manifest, ordering and source-extension contract unchanged.
        private sealed class StreamingFileDigest : IDisposable
        {
#if UNITY_EDITOR_OSX
            // CC_SHA256_CTX: count[2], hash[8], wbuf[16], all CC_LONG (uint32).
            // Layout verified against the macOS SDK CommonDigest.h.
            private readonly IntPtr context = Marshal.AllocHGlobal(26 * sizeof(uint));
            private const string Library = "/usr/lib/system/libcommonCrypto.dylib";
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            private static extern int CC_SHA256_Init(IntPtr context);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            private static extern int CC_SHA256_Update(IntPtr context, byte[] data, uint length);
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            private static extern int CC_SHA256_Final(byte[] digest, IntPtr context);

            public byte[] Compute(Stream stream, byte[] buffer)
            {
                if (CC_SHA256_Init(context) != 1) throw new CryptographicException("SHA-256 initialization failed.");
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
                    if (CC_SHA256_Update(context, buffer, (uint)read) != 1)
                        throw new CryptographicException("SHA-256 stream update failed.");
                var digest = new byte[32];
                if (CC_SHA256_Final(digest, context) != 1) throw new CryptographicException("SHA-256 finalization failed.");
                return digest;
            }
            public void Dispose() => Marshal.FreeHGlobal(context);
#else
            private readonly SHA256 hash = SHA256.Create();
            public byte[] Compute(Stream stream, byte[] buffer)
            {
                hash.Initialize();
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
                    hash.TransformBlock(buffer, 0, read, buffer, 0);
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return hash.Hash;
            }
            public void Dispose() => hash.Dispose();
#endif
        }
    }
}
#endif
