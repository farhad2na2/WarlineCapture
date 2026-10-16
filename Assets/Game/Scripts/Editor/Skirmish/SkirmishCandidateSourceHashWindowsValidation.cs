// DRAFT OUTSIDE ASSETS. Coordinator imports only after the current wrapper exits.
#if UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Editor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishCandidateSourceHashWindowsValidation
    {
        private static readonly string Empty = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        private static readonly string Abc = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        private static readonly string MillionA = "cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0";

        [Serializable] private sealed class Result
        {
            public string result, fixtureDigest, benchmarkDigest, artifactDirectory, platform, unityVersion;
            public int assertions;
            public long benchmarkBytes, nativeTicks, managedOracleTicks, stopwatchFrequency;
            public double nativeMilliseconds, managedOracleMilliseconds;
        }

        public static void RunFocusedValidation()
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            // Unity's Mono filesystem still enforces legacy Windows path limits.
            // Keep the 140-character filename fixture below that limit at this root.
            string artifact = Path.Combine(project, "Temp", "SkirmishHash", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(artifact);
            string fixture = Path.Combine(artifact, "fixture");
            string benchmark = Path.Combine(artifact, "benchmark");
            CreateTrees(fixture); CreateTrees(benchmark);
            int assertions = 0;
            var record = new Result { result = "Failed", artifactDirectory = artifact,
                platform = Application.platform.ToString(), unityVersion = Application.unityVersion,
                stopwatchFrequency = Stopwatch.Frequency };
            try
            {
                // Explicit native ordinal order is deliberately different from
                // normalized slash sorting: Assets\\AB.cs precedes Assets\\A\\Z.cs.
                Put(fixture, "Assets/AB.cs", Encoding.ASCII.GetBytes("abc"));
                Put(fixture, "Assets/A/Z.cs", new byte[0]);
                Put(fixture, "Assets/فارسی.cs", Encoding.ASCII.GetBytes("abc"));
                Put(fixture, "Packages/empty.json", new byte[0]);
                var million = new byte[1000000]; for (int i = 0; i < million.Length; i++) million[i] = (byte)'a';
                Put(fixture, "ProjectSettings/million.asset", million);
                string[] paths = { "Assets/AB.cs", "Assets/A/Z.cs", "Assets/فارسی.cs", "Packages/empty.json", "ProjectSettings/million.asset" };
                string[] hashes = { Abc, Empty, Abc, Empty, MillionA };
                string expected = KnownManifest(paths, hashes);
                string actual = SkirmishCandidateSourceHash.Compute(fixture);
                Equal(actual, expected, "Known vectors/native ordinal/Unicode manifest"); assertions++;
                Equal(ManagedOracle(fixture), expected, "Independent managed file oracle"); assertions++;
                Put(fixture, "Assets/excluded.txt", Encoding.ASCII.GetBytes("Excluded extension must not alter source identity"));
                Equal(SkirmishCandidateSourceHash.Compute(fixture), actual, "Existing extension exclusions"); assertions++;
                // Exercise >127-byte BinaryWriter UTF8 path-length encoding without
                // relying only on the implementation's BinaryWriter behavior.
                string longPath = "Assets/long/" + new string('x', 140) + ".asset";
                Put(fixture, longPath, new byte[0]);
                Equal(SkirmishCandidateSourceHash.Compute(fixture), ManagedOracle(fixture), "Long UTF8 path prefix"); assertions++;
                string beforeChange = SkirmishCandidateSourceHash.Compute(fixture);
                Put(fixture, "Assets/AB.cs", Encoding.ASCII.GetBytes("abd"));
                string changed = SkirmishCandidateSourceHash.Compute(fixture);
                if (changed == beforeChange) throw new InvalidOperationException("Changed-file identity was incorrectly accepted."); assertions++;
                Equal(changed, ManagedOracle(fixture), "Changed-file exact digest"); assertions++;

                // Small bounded census only. This does not time the real project.
                byte[] block = new byte[1024 * 1024]; for (int i = 0; i < block.Length; i++) block[i] = (byte)((i * 197 + i / 257) & 255);
                using (var stream = File.Create(Path.Combine(benchmark, "Assets", "sixteen.asset")))
                    for (int i = 0; i < 16; i++) stream.Write(block, 0, block.Length);
                record.benchmarkBytes = 16L * block.Length;
                // Warm up JIT/native provider separately from recorded samples.
                SkirmishCandidateSourceHash.Compute(fixture);
                var timer = Stopwatch.StartNew();
                string benchmarkNative = SkirmishCandidateSourceHash.Compute(benchmark);
                timer.Stop(); record.nativeTicks = timer.ElapsedTicks; record.nativeMilliseconds = timer.Elapsed.TotalMilliseconds;
                timer.Restart(); string benchmarkManaged = ManagedOracle(benchmark); timer.Stop();
                record.managedOracleTicks = timer.ElapsedTicks; record.managedOracleMilliseconds = timer.Elapsed.TotalMilliseconds;
                Equal(benchmarkNative, benchmarkManaged, "16MiB actual Compute/managed equivalence"); assertions++;
                record.fixtureDigest = changed; record.benchmarkDigest = benchmarkNative;
                record.assertions = assertions; record.result = "Passed";
                File.WriteAllText(Path.Combine(artifact, "result.json"), JsonUtility.ToJson(record, true), new UTF8Encoding(false));
                UnityEngine.Debug.Log("[SkirmishCandidateSourceHashWindowsValidation] result=Passed assertions=" + assertions +
                    " benchmarkBytes=" + record.benchmarkBytes + " nativeMilliseconds=" + record.nativeMilliseconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                    " managedOracleMilliseconds=" + record.managedOracleMilliseconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                    " artifact=" + artifact + " scope=DisposableFixtureOnly fullProjectEquivalence=Pending");
            }
            catch (Exception error)
            {
                record.assertions = assertions;
                File.WriteAllText(Path.Combine(artifact, "result.json"), JsonUtility.ToJson(record, true), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(artifact, "failure.txt"), error.ToString(), new UTF8Encoding(false));
                UnityEngine.Debug.LogError("[SkirmishCandidateSourceHashWindowsValidation] result=Failed artifact=" + artifact + " error=" + error);
                throw;
            }
            // Preserve tiny fixture + 16MiB benchmark inputs and result together in
            // ignored Temp for review. No source, assets or evidence are deleted.
        }

        private static void CreateTrees(string root)
        { foreach (string name in new[] { "Assets", "Packages", "ProjectSettings" }) Directory.CreateDirectory(Path.Combine(root, name)); }
        private static void Put(string root, string relative, byte[] bytes)
        { string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, bytes); }
        private static void Equal(string actual, string expected, string reason)
        { if (actual != expected) throw new InvalidOperationException(reason + " differs: actual=" + actual + " expected=" + expected); }
        private static byte[] Unhex(string value)
        { var output = new byte[value.Length / 2]; for (int i = 0; i < output.Length; i++) output[i] = Convert.ToByte(value.Substring(i * 2, 2), 16); return output; }
        private static string Digest(Stream stream)
        { using (var sha = new SHA256Managed()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }

        private static string KnownManifest(string[] paths, string[] knownFileDigests)
        {
            // Independently encode BinaryWriter string lengths (7-bit UTF8 byte
            // counts) and little-endian int32 digest length. No production helper.
            using (var manifest = new MemoryStream())
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    byte[] path = Encoding.UTF8.GetBytes(paths[i]); int length = path.Length;
                    while (length >= 128) { manifest.WriteByte((byte)((length & 127) | 128)); length >>= 7; }
                    manifest.WriteByte((byte)length); manifest.Write(path, 0, path.Length);
                    manifest.WriteByte(32); manifest.WriteByte(0); manifest.WriteByte(0); manifest.WriteByte(0);
                    byte[] hash = Unhex(knownFileDigests[i]); manifest.Write(hash, 0, hash.Length);
                }
                manifest.Position = 0; return Digest(manifest);
            }
        }

        private static string ManagedOracle(string root)
        {
            var files = new System.Collections.Generic.List<string>();
            string[] extensions = { ".cs", ".asmdef", ".asmref", ".rsp", ".json", ".asset", ".prefab", ".unity", ".shader", ".hlsl", ".cginc", ".compute", ".meta" };
            foreach (string directory in new[] { "Assets", "Packages", "ProjectSettings" })
                foreach (string path in Directory.GetFiles(Path.Combine(root, directory), "*", SearchOption.AllDirectories))
                    if (Array.IndexOf(extensions, Path.GetExtension(path).ToLowerInvariant()) >= 0) files.Add(path);
            files.Sort(StringComparer.Ordinal);
            var names = new string[files.Count]; var hashes = new string[files.Count];
            for (int i = 0; i < files.Count; i++)
            {
                names[i] = files[i].Substring(root.Length + 1).Replace('\\', '/');
                using (var stream = File.OpenRead(files[i])) hashes[i] = Digest(stream);
            }
            return KnownManifest(names, hashes);
        }
    }
}
#endif
