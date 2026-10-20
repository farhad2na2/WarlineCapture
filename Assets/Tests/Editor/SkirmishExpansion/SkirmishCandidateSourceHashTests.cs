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
            string unicode = Path.Combine(root, "Assets", "Ø²Ù…ÛŒÙ†-" + new string('x', 130) + ".meta");
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

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(65535)]
    [TestCase(65536)]
    [TestCase(65537)]
    [TestCase(200003)]
    public void FileDigestSessionMatchesIndependentManagedSha(int length)
    {
        WithFileDigestRoot(root => {
            var bytes = new byte[length];
            new System.Random(155926).NextBytes(bytes);
            string path = Path.Combine(root, "source.asset");
            File.WriteAllBytes(path, bytes);
            using var managed = SHA256.Create();
            string expected = BitConverter.ToString(managed.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            using var session = new SkirmishCandidateSourceHash.FileDigestSession();
            Assert.AreEqual(expected, session.ComputeFile(path));
            Assert.AreEqual(expected, SkirmishProductionCanaryRequest.FileHash(path));
        });
    }

    private static void WithFileDigestRoot(Action<string> action)
    {
        string root = Path.Combine(Path.GetTempPath(), "warline-file-digest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { action(root); }
        finally
        {
            string full = Path.GetFullPath(root);
            string temporary = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.IsTrue(full.StartsWith(temporary, StringComparison.OrdinalIgnoreCase), "Owned fixture stays under system Temp.");
            Assert.IsTrue(Path.GetFileName(full).StartsWith("warline-file-digest-", StringComparison.Ordinal));
            Assert.AreEqual(0, (int)(File.GetAttributes(full) & FileAttributes.ReparsePoint));
            Directory.Delete(full, true);
        }
    }

    [Test]
    public void FileDigestSessionResetsAcrossFilesAndDetectsLastByteMutation()
    {
        WithFileDigestRoot(root => {
            string first = Path.Combine(root, "first.asset"), second = Path.Combine(root, "second.asset");
            var bytes = new byte[131073]; new System.Random(155926).NextBytes(bytes);
            File.WriteAllBytes(first, bytes); File.WriteAllText(second, "different file");
            using var session = new SkirmishCandidateSourceHash.FileDigestSession();
            string expected = session.ComputeFile(first);
            Assert.AreNotEqual(expected, session.ComputeFile(second));
            Assert.AreEqual(expected, session.ComputeFile(first));
            bytes[bytes.Length - 1] ^= 1; File.WriteAllBytes(first, bytes);
            Assert.AreNotEqual(expected, session.ComputeFile(first));
        });
    }

    [Test]
    public void FileDigestSessionPreservesMissingFileFailureAndRecovers()
    {
        WithFileDigestRoot(root => {
            string path = Path.Combine(root, "missing.asset");
            using var session = new SkirmishCandidateSourceHash.FileDigestSession();
            Assert.Throws<FileNotFoundException>(() => session.ComputeFile(path));
            Assert.Throws<FileNotFoundException>(() => SkirmishProductionCanaryRequest.FileHash(path));
            File.WriteAllText(path, "created after refusal");
            Assert.AreEqual(SkirmishProductionCanaryRequest.FileHash(path), session.ComputeFile(path));
        });
    }

    [Test]
    public void FileDigestSessionPreservesDirectoryFailure()
    {
        WithFileDigestRoot(root => {
            using var session = new SkirmishCandidateSourceHash.FileDigestSession();
            Assert.Throws<UnauthorizedAccessException>(() => session.ComputeFile(root));
            Assert.Throws<UnauthorizedAccessException>(() => SkirmishProductionCanaryRequest.FileHash(root));
        });
    }

    [Test]
    public void FileDigestSessionPreservesExclusiveShareFailure()
    {
        WithFileDigestRoot(root => {
            string path = Path.Combine(root, "locked.asset"); File.WriteAllText(path, "locked");
            using var session = new SkirmishCandidateSourceHash.FileDigestSession();
            using (var held = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.Throws<IOException>(() => session.ComputeFile(path));
                Assert.Throws<IOException>(() => SkirmishProductionCanaryRequest.FileHash(path));
            }
            Assert.AreEqual(SkirmishProductionCanaryRequest.FileHash(path), session.ComputeFile(path));
        });
    }

    [Test]
    public void FileDigestSessionDisposeIsIdempotentAndRejectsReuse()
    {
        WithFileDigestRoot(root => {
            string path = Path.Combine(root, "source.asset"); File.WriteAllText(path, "source");
            var session = new SkirmishCandidateSourceHash.FileDigestSession();
            try { session.ComputeFile(path); }
            finally { session.Dispose(); }
            session.Dispose();
            Assert.Throws<ObjectDisposedException>(() => session.ComputeFile(path));
        });
    }

    public static void RunFileDigestSessionValidation()
    {
        var tests = new SkirmishCandidateSourceHashTests();
        foreach (int length in new[] { 0, 1, 65535, 65536, 65537, 200003 })
            tests.FileDigestSessionMatchesIndependentManagedSha(length);
        tests.FileDigestSessionResetsAcrossFilesAndDetectsLastByteMutation();
        tests.FileDigestSessionPreservesMissingFileFailureAndRecovers();
        tests.FileDigestSessionPreservesDirectoryFailure();
        tests.FileDigestSessionPreservesExclusiveShareFailure();
        tests.FileDigestSessionDisposeIsIdempotentAndRejectsReuse();
        tests.StreamingFingerprintPreservesManifestAndDetectsSourceMutation();
        Debug.Log("[SkirmishFileDigestSessionControls] result=Passed cases=12 hashCompatibility=1 sourceMutation=1 pathPolicyChanged=0 nativeThroughputMeasured=0 missionReady=0");
    }

    public static void RunFocusedValidation()
    {
        new SkirmishCandidateSourceHashTests().StreamingFingerprintPreservesManifestAndDetectsSourceMutation();
        Debug.Log("[SkirmishCandidateSourceHashTests] result=Passed manifestCompatible=1 sourceMutationDetected=1");
    }
}
