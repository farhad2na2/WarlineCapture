#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEngine;
namespace Game.Editor
{
    internal sealed class S002InitialWorldBoundaryAdmissionException : InvalidOperationException
    {
        internal readonly Dictionary<string, object> Authority;
        internal S002InitialWorldBoundaryAdmissionException(string owner, Dictionary<string, object> authority)
            : base("Initial-world command domain is not quiescent: " + owner + " " + authority["reason"]) { Authority = authority; }
    }
    // An explicit failed-boundary diagnostic; never an accepted World snapshot.
    internal static class S002InitialWorldBoundaryDiagnostic
    {
        internal const int MaximumCharacters = 1024 * 1024;
        internal static void Write(World world, int before, int after, Dictionary<string, object> authority)
        {
            string path = Environment.GetEnvironmentVariable("WARLINE_S002_INITIAL_WORLD_REJECTION_OUTPUT");
            if (string.IsNullOrEmpty(path)) return;
            if (!S002InitialWorldBundleCapture.Enabled || before == after)
                throw new InvalidOperationException("Rejected-boundary diagnostic requires the actual synchronized roster callback.");
            var witness = S002InitialWorldBoundaryWitness.Capture(world);
            if (!(bool)witness["roster_callback_active"] || !(bool)witness["roster_structural_sync_proven"] ||
                (int)witness["roster_order_before"] != before || (int)witness["roster_order_after"] != after)
                throw new InvalidOperationException("Rejected-boundary diagnostic callback identity mismatch.");
            var report = new Dictionary<string, object>
            {
                ["schema_version"] = 1, ["kind"] = "InitialWorldBoundaryRejectionDiagnostic",
                ["phase"] = "Initialization.AfterOriginalRosterStructuralSync.BeforeSimulation",
                ["world_sequence"] = world.SequenceNumber.ToString(), ["engine_frame"] = Time.frameCount,
                ["roster_entity_order_before"] = before, ["roster_entity_order_after"] = after,
                ["source_authority_expected_sha256"] = Environment.GetEnvironmentVariable("WARLINE_S002_INITIAL_WORLD_SOURCE_AUTHORITY_SHA256"),
                ["source_compiler_join_verified"] = false,
                ["rejected_ecb_owner"] = authority,
                ["complete_queue_codec"] = false, ["data_capture_gate"] = false,
                ["initial_world_bundle"] = false, ["autonomous_qualified"] = false,
                ["future_native_inputs"] = false
            };
            WriteFresh(path, report);
            Debug.Log("[InitialWorldBoundaryDiagnostic] result=Rejected dataCaptureGate=0 autonomousQualified=0");
        }
        internal static void WriteFresh(string path, object report)
        {
            if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path)) throw new InvalidOperationException("Fresh absolute diagnostic path required.");
            path = Path.GetFullPath(path); string parent = Path.GetDirectoryName(path);
            if (!Directory.Exists(parent)) throw new InvalidOperationException("Owned diagnostic parent must already exist.");
            for (var directory = new DirectoryInfo(parent); directory != null; directory = directory.Parent)
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Diagnostic parent symlink/reparse point refused.");
            if (File.Exists(path) || Directory.Exists(path)) throw new InvalidOperationException("Existing diagnostic path refused.");
            string json;
            using (var bounded = new BoundedWriter())
            {
                using (var writer = new JsonTextWriter(bounded) { Formatting = Formatting.Indented, CloseOutput = false })
                    new JsonSerializer().Serialize(writer, report);
                bounded.Write('\n'); json = bounded.ToString();
            }
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);
        }
        sealed class BoundedWriter : TextWriter
        {
            readonly StringBuilder text = new StringBuilder();
            public override Encoding Encoding => Encoding.UTF8;
            void Admit(int count) { if (count < 0 || count > MaximumCharacters - text.Length) throw new InvalidOperationException("Rejected-boundary diagnostic character budget exceeded."); }
            public override void Write(char value) { Admit(1); text.Append(value); }
            public override void Write(string value) { if (value != null) { Admit(value.Length); text.Append(value); } }
            public override void Write(char[] value, int index, int count) { Admit(count); text.Append(value, index, count); }
            public override string ToString() => text.ToString();
        }
    }
}
#endif
