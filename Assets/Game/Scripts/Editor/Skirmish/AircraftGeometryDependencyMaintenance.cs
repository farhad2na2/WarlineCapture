using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Configs;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    // Explicit Editor maintenance only. Never runs from a Play/build guard.
    public static class AircraftGeometryDependencyMaintenance
    {
        public static void RefreshUnchangedGeometryDependencies()
        {
            RequireIdle();
            string manifestPath = Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_MAINTENANCE_MANIFEST");
            if (string.IsNullOrWhiteSpace(manifestPath))
                throw new InvalidOperationException("Explicit reviewed maintenance manifest required");
            var manifest = JObject.Parse(File.ReadAllText(manifestPath));
            string diagnosisPath = (string)manifest["diagnosisPath"];
            if (Hash(diagnosisPath) != (string)manifest["diagnosisSha256"])
                throw new InvalidOperationException("Pinned read-only diagnosis changed");
            var diagnosis = JObject.Parse(File.ReadAllText(diagnosisPath));
            if ((string)diagnosis["scope"] != "ReadOnlyAircraftGeometryDependencyDiagnosis" ||
                !(bool)diagnosis["geometryAndOtherSerializedFactsUnchanged"])
                throw new InvalidOperationException("Dependency-only diagnosis required; changed geometry requires separate preparation");
            string[] paths = { SkirmishRigidAircraftGeometryAuthoring.Output,
                SkirmishCanonicalAircraftGeometryAuthoring.Output, SkirmishAircraftRotorGeometryAuthoring.Output };
            var pins = manifest["resources"] as JArray;
            if (pins == null || pins.Count != paths.Length || diagnosis["resources"]?.Count() != paths.Length)
                throw new InvalidOperationException("Exactly three pinned aircraft resources required");
            var installed = paths.Select(p => AssetDatabase.LoadAssetAtPath<ScriptableObject>(p)).ToArray();
            for (int i = 0; i < paths.Length; i++)
            {
                if ((string)pins[i]["asset"] != paths[i] || Hash(paths[i]) != (string)pins[i]["sha256"] ||
                    Hash(paths[i] + ".meta") != (string)pins[i]["metaSha256"] || installed[i] == null || EditorUtility.IsDirty(installed[i]))
                    throw new InvalidOperationException("Pinned clean resource/GUID bytes changed: " + paths[i]);
            }
            RequireUnfrozen(installed);
            var rotor = (SkirmishAircraftRotorGeometryConfig)installed[2];
            ScriptableObject[] measured = { SkirmishRigidAircraftGeometryAuthoring.Measure(),
                SkirmishCanonicalAircraftGeometryAuthoring.Measure(),
                SkirmishAircraftRotorGeometryAuthoring.Measure(rotor.NativeTemplateProvenanceSha256) };
            try
            {
                var comparisons = paths.Select((p, i) => Compare(installed[i], measured[i], p)).ToArray();
                // Preflight every resource before changing any object. The diagnosis
                // also pins the freshly measured hashes, so a concurrent source edit
                // requires another diagnosis instead of silently accepting new input.
                for (int i = 0; i < paths.Length; i++)
                    if (!(bool)comparisons[i]["equalExceptDependencyHashes"] ||
                        (string)diagnosis["resources"][i]["asset"] != paths[i] ||
                        !JToken.DeepEquals(comparisons[i]["measuredPayload"], diagnosis["resources"][i]["measuredPayload"]))
                        throw new InvalidOperationException("Diagnosed source/geometry changed: " + paths[i]);
                string output = Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_MAINTENANCE_OUTPUT");
                if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("Explicit backup output directory required");
                string backup = Path.Combine(output, "before-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(backup);
                for (int i = 0; i < paths.Length; i++)
                {
                    File.Copy(paths[i], Path.Combine(backup, Path.GetFileName(paths[i])));
                    File.Copy(paths[i] + ".meta", Path.Combine(backup, Path.GetFileName(paths[i]) + ".meta"));
                }
                var transport = (SkirmishRigidAircraftGeometryConfig)installed[0];
                var freshTransport = (SkirmishRigidAircraftGeometryConfig)measured[0];
                for (int i = 0; i < transport.Sources.Length; i++) transport.Sources[i].DependencyHash = freshTransport.Sources[i].DependencyHash;
                var canonical = (SkirmishCanonicalAircraftGeometryConfig)installed[1];
                var freshCanonical = (SkirmishCanonicalAircraftGeometryConfig)measured[1];
                for (int i = 0; i < canonical.Roles.Length; i++)
                    for (int j = 0; j < canonical.Roles[i].Sources.Length; j++)
                        canonical.Roles[i].Sources[j].DependencyHash = freshCanonical.Roles[i].Sources[j].DependencyHash;
                var freshRotor = (SkirmishAircraftRotorGeometryConfig)measured[2];
                for (int i = 0; i < rotor.Bindings.Length; i++) rotor.Bindings[i].DependencyHash = freshRotor.Bindings[i].DependencyHash;
                for (int i = 0; i < paths.Length; i++)
                    if (!JToken.DeepEquals(PersistentPayload(installed[i]), comparisons[i]["expectedStampOnlyPayload"]))
                        throw new InvalidOperationException("Unexpected metadata mutation before save: " + paths[i]);
                for (int i = 0; i < paths.Length; i++)
                {
                    EditorUtility.SetDirty(installed[i]);
                    AssetDatabase.SaveAssetIfDirty(installed[i]);
                    if (Hash(paths[i] + ".meta") != (string)pins[i]["metaSha256"])
                        throw new InvalidOperationException("Resource GUID/meta changed: " + paths[i]);
                }
                // The ordinary guards remain the final authority; no relaxed
                // comparison is added to Play or build entry points.
                SkirmishRigidAircraftGeometryAuthoring.VerifyInstalledTransportMetadata();
                SkirmishAircraftRotorGeometryAuthoring.ValidateInstalled();
                File.WriteAllText(Path.Combine(output, "refresh-receipt.json"), new JObject
                {
                    ["scope"] = "ExplicitDependencyStampsOnly",
                    ["backup"] = backup,
                    ["resources"] = new JArray(comparisons),
                    ["geometryChanged"] = false,
                    ["nativeTemplateProvenanceChanged"] = false,
                    ["freshNativeActorOrDeviceAcceptance"] = false
                }.ToString());
                Debug.Log("[AircraftGeometryDependencyRefresh] result=Passed resources=3 geometryChanged=0 guidPreserved=1 originalGuardsPassed=1 nativeActorAcceptance=Pending");
            }
            finally { foreach (var value in measured) Object.DestroyImmediate(value); }
        }

        public static void Diagnose()
        {
            RequireIdle();
            string output = Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_MAINTENANCE_OUTPUT");
            if (string.IsNullOrWhiteSpace(output))
                throw new InvalidOperationException("Explicit maintenance output directory required");
            Directory.CreateDirectory(output);
            var transport = AssetDatabase.LoadAssetAtPath<SkirmishRigidAircraftGeometryConfig>(SkirmishRigidAircraftGeometryAuthoring.Output);
            var canonical = AssetDatabase.LoadAssetAtPath<SkirmishCanonicalAircraftGeometryConfig>(SkirmishCanonicalAircraftGeometryAuthoring.Output);
            var rotor = AssetDatabase.LoadAssetAtPath<SkirmishAircraftRotorGeometryConfig>(SkirmishAircraftRotorGeometryAuthoring.Output);
            if (transport == null || canonical == null || rotor == null ||
                EditorUtility.IsDirty(transport) || EditorUtility.IsDirty(canonical) || EditorUtility.IsDirty(rotor))
                throw new InvalidOperationException("Three clean installed geometry resources required");
            var measuredTransport = SkirmishRigidAircraftGeometryAuthoring.Measure();
            var measuredCanonical = SkirmishCanonicalAircraftGeometryAuthoring.Measure();
            var measuredRotor = SkirmishAircraftRotorGeometryAuthoring.Measure(rotor.NativeTemplateProvenanceSha256);
            try
            {
                // This collector records source/importer facts only when no native
                // World is supplied; it explicitly reports bakedRegistryAvailable=false.
                string provenance = SkirmishAircraftRootProvenance.CollectExistingWorld(null);
                var reports = new JArray(
                    Compare(transport, measuredTransport, SkirmishRigidAircraftGeometryAuthoring.Output),
                    Compare(canonical, measuredCanonical, SkirmishCanonicalAircraftGeometryAuthoring.Output),
                    Compare(rotor, measuredRotor, SkirmishAircraftRotorGeometryAuthoring.Output));
                var report = new JObject
                {
                    ["scope"] = "ReadOnlyAircraftGeometryDependencyDiagnosis",
                    ["unityVersion"] = Application.unityVersion,
                    ["sourceImporterReport"] = provenance,
                    ["nativeWorldCreated"] = false,
                    ["resources"] = reports,
                    ["geometryAndOtherSerializedFactsUnchanged"] = reports.All(r => (bool)r["equalExceptDependencyHashes"])
                };
                string diagnosisPath = Path.GetFullPath(Path.Combine(output, "diagnosis.json"));
                File.WriteAllText(diagnosisPath, report.ToString());
                File.WriteAllText(Path.Combine(output, "maintenance-manifest.json"), new JObject
                {
                    ["diagnosisPath"] = diagnosisPath,
                    ["diagnosisSha256"] = Hash(diagnosisPath),
                    ["resources"] = new JArray(reports.Select(r =>
                    {
                        string path = (string)r["asset"];
                        return new JObject { ["asset"] = path, ["sha256"] = Hash(path), ["metaSha256"] = Hash(path + ".meta") };
                    }))
                }.ToString());
                Debug.Log("[AircraftGeometryDependencyDiagnosis] result=Collected resources=3 factsUnchanged=" +
                    ((bool)report["geometryAndOtherSerializedFactsUnchanged"] ? 1 : 0) + " output=" + output);
            }
            finally
            {
                Object.DestroyImmediate(measuredTransport);
                Object.DestroyImmediate(measuredCanonical);
                Object.DestroyImmediate(measuredRotor);
            }
        }

        public static JObject Compare(Object installed, Object measured, string path)
        {
            var original = PersistentPayload(installed);
            var current = PersistentPayload(measured);
            var expected = (JObject)original.DeepClone();
            var differences = new JArray();
            foreach (var stamp in expected.SelectTokens("$..DependencyHash").ToArray())
            {
                var fresh = current.SelectToken(stamp.Path);
                if (stamp.Type != JTokenType.String || fresh == null || fresh.Type != JTokenType.String)
                    throw new InvalidOperationException("Dependency stamp schema changed: " + path + " " + stamp.Path);
                if (!JToken.DeepEquals(stamp, fresh))
                    differences.Add(new JObject { ["field"] = stamp.Path, ["before"] = stamp.DeepClone(), ["after"] = fresh.DeepClone() });
                stamp.Replace(fresh.DeepClone());
            }
            return new JObject
            {
                ["asset"] = path,
                ["dependencyChanges"] = differences,
                ["equalExceptDependencyHashes"] = JToken.DeepEquals(expected, current),
                ["expectedStampOnlyPayload"] = expected,
                ["measuredPayload"] = current
            };
        }

        // JsonUtility uses process-local instance IDs for Unity object references.
        // A diagnosis must survive Editor restart, so retain the actual persistent
        // GUID/local-file-ID pair instead; never compare host instance numbers.
        static JObject PersistentPayload(Object value)
        {
            var payload = JObject.Parse(JsonUtility.ToJson(value));
            foreach (var reference in payload.Descendants().OfType<JObject>()
                .Where(o => o.Property("instanceID") != null).ToArray())
            {
                if (reference.Properties().Count() != 1)
                    throw new InvalidOperationException("Unexpected Unity reference serialization schema");
                ulong id = (ulong)reference["instanceID"];
                if (id == 0) continue;
                var target = EditorUtility.EntityIdToObject(EntityId.FromULong(id));
                if (target == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string guid, out long localId) ||
                    string.IsNullOrEmpty(guid))
                    throw new InvalidOperationException("Persistent source reference required for dependency maintenance");
                reference.Replace(new JObject { ["guid"] = guid, ["localFileId"] = localId });
            }
            return payload;
        }

        static void RequireIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Idle checked Editor required for explicit aircraft metadata maintenance");
        }

        static void RequireUnfrozen(ScriptableObject[] installed)
        {
            if (HasFrozenField(installed[0], "index") || HasFrozenField(installed[2], "frozen") ||
                ((SkirmishCanonicalAircraftGeometryConfig)installed[1]).Roles.Any(r =>
                    HasFrozenField(r, "index")))
                throw new InvalidOperationException("Framing lookup already frozen; use a fresh checked maintenance Editor");
        }

        static bool HasFrozenField(object value, string name)
        {
            var field = value.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new InvalidOperationException("Framing lookup schema changed: " + name);
            return field.GetValue(value) != null;
        }

        static string Hash(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
