#if UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishSagaPlayEntryAudit
    {
        public static void Run()
        {
            var report = new JObject();
            bool exact = true;
            Audit("transport", SkirmishRigidAircraftGeometryAuthoring.Output, SkirmishRigidAircraftGeometryAuthoring.Measure(), report, ref exact);
            Audit("canonical", SkirmishCanonicalAircraftGeometryAuthoring.Output, SkirmishCanonicalAircraftGeometryAuthoring.Measure(), report, ref exact);
            var rotor = AssetDatabase.LoadAssetAtPath<SkirmishAircraftRotorGeometryConfig>(SkirmishAircraftRotorGeometryAuthoring.Output);
            Audit("rotor", SkirmishAircraftRotorGeometryAuthoring.Output, SkirmishAircraftRotorGeometryAuthoring.Measure(rotor.NativeTemplateProvenanceSha256), report, ref exact);
            report["readOnly"] = true;
            report["allFieldsExceptDependencyHashesEqual"] = exact;
            string path = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/normal-input/aircraft-cache-audit.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, report.ToString());
            Debug.Log("[SagaPlayEntryAudit] readOnly=1 geometryAndBindingsEqual=" + exact + " report=" + path);
            EditorApplication.Exit(exact ? 0 : 1);
        }

        // Explicit dependency-only preparation for these exact audited installed bytes.
        // Geometry, source bindings, provenance and every other serialized field must match.
        public static void RefreshAuditedDependencyStamps()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Fresh idle Editor required");
            string[] paths =
            {
                SkirmishRigidAircraftGeometryAuthoring.Output,
                SkirmishCanonicalAircraftGeometryAuthoring.Output,
                SkirmishAircraftRotorGeometryAuthoring.Output
            };
            string[] pins =
            {
                "186c72a0ed0b6c3dbeba06015463b9d9e9c761b6e68b861a8d48daa69a614f4b",
                "f8a00c494aec924cee9fcc9de6154f0393ef777bd3cabc5b0e0f7d7e0db78353",
                "3c2a3f9b6b3c6e15b4164038f56aaa26c7a4b1329acbff859d0ad337ae9ae0d9"
            };
            var assets = new ScriptableObject[]
            {
                AssetDatabase.LoadAssetAtPath<SkirmishRigidAircraftGeometryConfig>(paths[0]),
                AssetDatabase.LoadAssetAtPath<SkirmishCanonicalAircraftGeometryConfig>(paths[1]),
                AssetDatabase.LoadAssetAtPath<SkirmishAircraftRotorGeometryConfig>(paths[2])
            };
            for (int i = 0; i < paths.Length; i++)
                if (Hash(paths[i]) != pins[i] || assets[i] == null || EditorUtility.IsDirty(assets[i]))
                    throw new InvalidOperationException("Exact clean audited cache bytes required: " + paths[i]);
            RequireUnindexed(assets[0], "index");
            RequireUnindexed(assets[2], "frozen");
            foreach (var role in ((SkirmishCanonicalAircraftGeometryConfig)assets[1]).Roles)
                RequireUnindexed(role, "index");
            var rotor = (SkirmishAircraftRotorGeometryConfig)assets[2];
            var measured = new ScriptableObject[]
            {
                SkirmishRigidAircraftGeometryAuthoring.Measure(),
                SkirmishCanonicalAircraftGeometryAuthoring.Measure(),
                SkirmishAircraftRotorGeometryAuthoring.Measure(rotor.NativeTemplateProvenanceSha256)
            };
            try
            {
                var expected = new JObject[3];
                var guids = new string[3];
                var metas = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    var before = JObject.Parse(JsonUtility.ToJson(assets[i]));
                    expected[i] = JObject.Parse(JsonUtility.ToJson(measured[i]));
                    var geometry = (JObject)expected[i].DeepClone();
                    RemoveStamps(before);
                    RemoveStamps(geometry);
                    if (!JToken.DeepEquals(before, geometry))
                        throw new InvalidOperationException("Dependency-only preparation refused geometry/binding drift: " + paths[i]);
                    guids[i] = AssetDatabase.AssetPathToGUID(paths[i]);
                    metas[i] = Hash(paths[i] + ".meta");
                }

                string backup = Path.GetFullPath("Logs/SkirmishProduction/Offline/S074SagaAuditedDependencyStampRefreshRootExecution_20261021/cache-backups-and-receipt");
                Directory.CreateDirectory(backup);
                for (int i = 0; i < 3; i++)
                {
                    File.Copy(paths[i], Path.Combine(backup, Path.GetFileName(paths[i]) + ".before"), false);
                    File.Copy(paths[i] + ".meta", Path.Combine(backup, Path.GetFileName(paths[i]) + ".meta.before"), false);
                }

                var transport = (SkirmishRigidAircraftGeometryConfig)assets[0];
                var transportFresh = (SkirmishRigidAircraftGeometryConfig)measured[0];
                for (int i = 0; i < transport.Sources.Length; i++)
                    transport.Sources[i].DependencyHash = transportFresh.Sources[i].DependencyHash;
                var canonical = (SkirmishCanonicalAircraftGeometryConfig)assets[1];
                var canonicalFresh = (SkirmishCanonicalAircraftGeometryConfig)measured[1];
                for (int i = 0; i < canonical.Roles.Length; i++)
                    for (int j = 0; j < canonical.Roles[i].Sources.Length; j++)
                        canonical.Roles[i].Sources[j].DependencyHash = canonicalFresh.Roles[i].Sources[j].DependencyHash;
                var rotorFresh = (SkirmishAircraftRotorGeometryConfig)measured[2];
                for (int i = 0; i < rotor.Bindings.Length; i++)
                    rotor.Bindings[i].DependencyHash = rotorFresh.Bindings[i].DependencyHash;
                for (int i = 0; i < 3; i++)
                {
                    if (!JToken.DeepEquals(JObject.Parse(JsonUtility.ToJson(assets[i])), expected[i]))
                        throw new InvalidOperationException("Unexpected non-stamp mutation");
                    EditorUtility.SetDirty(assets[i]);
                    AssetDatabase.SaveAssetIfDirty(assets[i]);
                    if (AssetDatabase.AssetPathToGUID(paths[i]) != guids[i] || Hash(paths[i] + ".meta") != metas[i])
                        throw new InvalidOperationException("GUID/meta changed");
                }

                SkirmishRigidAircraftGeometryAuthoring.VerifyInstalledTransportMetadata();
                SkirmishCanonicalAircraftGeometryAuthoring.VerifyInstalled();
                SkirmishAircraftRotorGeometryAuthoring.ValidateInstalled();
                File.WriteAllText(Path.Combine(backup, "receipt.json"), new JObject { ["scope"] = "DependencyStampsOnly", ["geometryAndBindingsChanged"] = false, ["guidsAndMetasPreserved"] = true, ["originalStrictVerifiersPassed"] = true, ["nativeMissionAcceptance"] = "Pending" }.ToString());
                Debug.Log("[SagaAircraftDependencyPreparation] result=Passed caches=3 geometryChanged=0 guidsPreserved=1 originalStrictGuards=Passed");
                EditorApplication.Exit(0);
            }
            finally
            {
                foreach (var item in measured)
                    UnityEngine.Object.DestroyImmediate(item);
            }
        }

        private static void RequireUnindexed(object value, string field)
        {
            var member = value.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (member == null || member.GetValue(value) != null)
                throw new InvalidOperationException("Fresh unindexed lookup required: " + value.GetType().Name);
        }

        private static string Hash(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void Audit(string name, string path, ScriptableObject measured, JObject report, ref bool exact)
        {
            try
            {
                var installed = AssetDatabase.LoadMainAssetAtPath(path);
                if (installed == null || EditorUtility.IsDirty(installed))
                    throw new InvalidOperationException("Clean installed cache required: " + path);
                var old = JObject.Parse(JsonUtility.ToJson(installed));
                var fresh = JObject.Parse(JsonUtility.ToJson(measured));
                bool same = JToken.DeepEquals(old, fresh);
                var oldGeometry = (JObject)old.DeepClone();
                var freshGeometry = (JObject)fresh.DeepClone();
                RemoveStamps(oldGeometry);
                RemoveStamps(freshGeometry);
                bool geometry = JToken.DeepEquals(oldGeometry, freshGeometry);
                report[name] = new JObject
                {
                    ["asset"] = path,
                    ["allFieldsEqual"] = same,
                    ["allFieldsExceptDependencyHashesEqual"] = geometry,
                    ["installed"] = old,
                    ["measured"] = fresh
                };
                exact &= geometry;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(measured);
            }
        }

        private static void RemoveStamps(JToken value)
        {
            if (value is JObject obj)
            {
                obj.Remove("DependencyHash");
                foreach (var property in obj.Properties())
                    RemoveStamps(property.Value);
            }
            else if (value is JArray array)
                foreach (var child in array)
                    RemoveStamps(child);
        }
    }
}
#endif

