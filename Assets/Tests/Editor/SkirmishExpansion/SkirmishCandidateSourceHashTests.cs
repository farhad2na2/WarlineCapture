using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class SkirmishCandidateSourceHashTests
{
    [Test]
    public void StreamingFingerprintPreservesManifestAndDetectsSourceMutation()
    {
        string root = Path.Combine(Path.GetTempPath(), "warline-fingerprint-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (string folder in new[] { "Assets", "Packages", "ProjectSettings" })
                Directory.CreateDirectory(Path.Combine(root, folder));
            File.WriteAllText(Path.Combine(root, "Assets", "empty.cs"), "");
            var bytes = new byte[200003];
            new System.Random(104734).NextBytes(bytes);
            string asset = Path.Combine(root, "Assets", "large.asset");
            File.WriteAllBytes(asset, bytes);
            File.WriteAllText(Path.Combine(root, "Packages", "manifest.json"), "{}");
            File.WriteAllText(Path.Combine(root, "Assets", "ignored.png"), "ignored");
            string unicode = Path.Combine(root, "Assets", "زمین-" + new string('x', 130) + ".meta");
            File.WriteAllText(unicode, "unicode path and multi-byte manifest length");
            string[] files = { Path.Combine(root, "Assets", "empty.cs"), asset,
                Path.Combine(root, "Packages", "manifest.json"), unicode };
            using var manifest = new MemoryStream();
            using (var writer = new BinaryWriter(manifest, Encoding.UTF8, true))
            using (var sha = SHA256.Create())
                foreach (string file in files.OrderBy(x => x, StringComparer.Ordinal))
                {
                    writer.Write(file.Substring(root.Length + 1).Replace('\\', '/'));
                    byte[] digest = sha.ComputeHash(File.ReadAllBytes(file));
                    writer.Write(digest.Length);
                    writer.Write(digest);
                }
            using var hash = SHA256.Create();
            string expected = BitConverter.ToString(hash.ComputeHash(manifest.ToArray())).Replace("-", "").ToLowerInvariant();
            Assert.AreEqual(expected, SkirmishCandidateSourceHash.Compute(root));
            bytes[65536] ^= 1;
            File.WriteAllBytes(asset, bytes);
            Assert.AreNotEqual(expected, SkirmishCandidateSourceHash.Compute(root));
        }
        finally { Directory.Delete(root, true); }
    }

    public static void RunFocusedValidation()
    {
        new SkirmishCandidateSourceHashTests().StreamingFingerprintPreservesManifestAndDetectsSourceMutation();
        Debug.Log("[SkirmishCandidateSourceHashTests] result=Passed manifestCompatible=1 sourceMutationDetected=1");
    }
}
