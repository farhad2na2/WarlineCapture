#if UNITY_EDITOR
using System;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace Game.Editor
{
    // Read-only AssetDatabase inventory for the exact aircraft prefab sources
    // recorded by the pinned S073 geometry-drift report.
    internal static class S073AircraftDependencyClosureDiagnostic
    {
        private const string OutputKey = "WARLINE_S073_AIRCRAFT_DEPENDENCY_CLOSURE_OUTPUT";
        private const string ReportRelativePath = "Logs/SkirmishProduction/s073-aircraft-geometry-drift-20261102-r1/report.json";
        private const string ReportSha256 = "801eebd8adb5b36229afb9be89bf83b7fef2e213a92cb64adb5a229f2b57b6e2";

        public static void RunS073AircraftDependencyClosureDiagnostic()
        {
            string outputPath = null;
            JObject result = new JObject
            {
                ["kind"] = "S073AircraftDependencyClosureDiagnostic",
                ["schema_version"] = 1,
                ["scope"] = "PinnedDriftReportAircraftPrefabSources_AssetDatabaseDependenciesAndRawFileHashes_ReadOnly_NoPlayOrGameplayState",
                ["mission"] = "S073",
                ["status"] = "Failed",
                ["mission_world_loaded"] = false,
                ["native_readiness"] = false
            };

            try
            {
                outputPath = RequireFreshPrivateTemporaryOutput();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("S073 dependency diagnostic requires Edit mode and never enters Play mode.");

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string reportPath = Path.Combine(projectRoot, ReportRelativePath);
                string observedReportSha256 = Sha256(reportPath);
                result["input_report"] = ReportRelativePath;
                result["input_report_sha256"] = observedReportSha256;
                result["expected_input_report_sha256"] = ReportSha256;
                if (!string.Equals(observedReportSha256, ReportSha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Pinned S073 aircraft drift report SHA-256 mismatch.");

                JObject report;
                using (var text = File.OpenText(reportPath))
                using (var reader = new JsonTextReader(text) { MaxDepth = 64, DateParseHandling = DateParseHandling.None })
                    report = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if ((int?)report["schema_version"] != 1 ||
                    (string)report["kind"] != "S073AircraftGeometryDependencyDiagnostic")
                    throw new InvalidDataException("Pinned S073 aircraft report schema/kind mismatch.");

                string[] sources = ReadAircraftSourcePaths(report);
                result["unity_version"] = Application.unityVersion;
                result["asset_database_closure_recursive"] = true;
                result["source_count"] = sources.Length;
                JArray sourceResults = new JArray(sources.Select(source => ReadSource(projectRoot, source)));
                result["sources"] = sourceResults;
                bool allQueriesAvailable = sourceResults.All(source =>
                    (string)source["closure_status"] == "Available" &&
                    (string)source["asset_database_dependency_hash"]?["status"] == "Available");
                result["status"] = allQueriesAvailable ? "Passed" : "Failed";
                if (!allQueriesAvailable) result["failure"] = "One or more AssetDatabase closure/hash queries were unavailable; per-source statuses are recorded.";
            }
            catch (Exception error)
            {
                result["status"] = "Failed";
                result["failure"] = error.GetType().Name + ": " + error.Message;
            }

            try
            {
                if (string.IsNullOrEmpty(outputPath)) outputPath = RequireFreshPrivateTemporaryOutput();
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                using (var file = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(file))
                    writer.Write(result.ToString(Formatting.Indented) + "\n");
                string marker = "[S073AircraftDependencyClosure] result=" + (string)result["status"] + " output=" + outputPath;
                if ((string)result["status"] == "Passed")
                {
                    Debug.Log(marker + " sources=" + (int)result["source_count"]);
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError(marker + " failure=" + (string)result["failure"]);
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S073AircraftDependencyClosure] result=Failed output=" + (outputPath ?? "Unavailable") + " failure=" + error.Message);
                EditorApplication.Exit(1);
            }
        }

        private static string[] ReadAircraftSourcePaths(JObject report)
        {
            var paths = new System.Collections.Generic.SortedSet<string>(StringComparer.Ordinal);
            JArray rotorRows = report["rotor_geometry"]?["rows"] as JArray;
            JArray roles = report["canonical_geometry"]?["roles"] as JArray;
            if (rotorRows == null || roles == null)
                throw new InvalidDataException("Pinned report lacks rotor rows or canonical aircraft roles.");

            foreach (JToken row in rotorRows)
            {
                AddSourcePath(paths, (string)row["installed"]?["source_path"]);
                AddSourcePath(paths, (string)row["measured"]?["source_path"]);
            }
            foreach (JToken role in roles)
            {
                JArray comparisons = role["source_comparisons"] as JArray;
                if (comparisons == null) throw new InvalidDataException("Pinned report has a role without source comparisons.");
                foreach (JToken comparison in comparisons)
                {
                    AddSourcePath(paths, (string)comparison["installed"]?["path"]);
                    AddSourcePath(paths, (string)comparison["measured"]?["path"]);
                }
            }
            if (paths.Count == 0) throw new InvalidDataException("Pinned report has no aircraft prefab source paths.");
            return paths.ToArray();
        }

        private static void AddSourcePath(System.Collections.Generic.ISet<string> paths, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new InvalidDataException("Pinned aircraft source path is missing.");
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".prefab", StringComparison.Ordinal) ||
                path.IndexOf("..", StringComparison.Ordinal) >= 0 || path.IndexOf('\\') >= 0)
                throw new InvalidDataException("Pinned aircraft source path is outside the expected Assets prefab domain: " + path);
            paths.Add(path);
        }

        private static JObject ReadSource(string projectRoot, string sourcePath)
        {
            var source = new JObject { ["source_path"] = sourcePath };
            var hashTimer = Stopwatch.StartNew();
            try
            {
                string hash = AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
                source["asset_database_dependency_hash"] = new JObject
                {
                    ["status"] = string.IsNullOrEmpty(hash) ? "Unavailable_EmptyHash" : "Available",
                    ["value"] = string.IsNullOrEmpty(hash) ? null : hash,
                    ["elapsed_ms"] = hashTimer.Elapsed.TotalMilliseconds
                };
            }
            catch (Exception error)
            {
                source["asset_database_dependency_hash"] = new JObject
                {
                    ["status"] = "Unavailable_QueryFailed",
                    ["error"] = error.GetType().Name + ": " + error.Message,
                    ["elapsed_ms"] = hashTimer.Elapsed.TotalMilliseconds
                };
            }

            var closureTimer = Stopwatch.StartNew();
            try
            {
                string[] dependencies = AssetDatabase.GetDependencies(sourcePath, recursive: true);
                source["closure_status"] = dependencies == null ? "Unavailable_NullResult" : "Available";
                source["closure_elapsed_ms"] = closureTimer.Elapsed.TotalMilliseconds;
                source["dependencies"] = dependencies == null
                    ? new JArray()
                    : new JArray(dependencies.Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal)
                        .Select(path => ReadDependencyFiles(projectRoot, path)));
            }
            catch (Exception error)
            {
                source["closure_status"] = "Unavailable_QueryFailed";
                source["closure_elapsed_ms"] = closureTimer.Elapsed.TotalMilliseconds;
                source["closure_error"] = error.GetType().Name + ": " + error.Message;
                source["dependencies"] = new JArray();
            }
            return source;
        }

        private static JObject ReadDependencyFiles(string projectRoot, string assetPath)
        {
            string physicalPath = ResolvePhysicalAssetPath(projectRoot, assetPath);
            return new JObject
            {
                ["asset_path"] = assetPath,
                ["raw_bytes"] = ReadFileHash(physicalPath),
                ["meta_bytes"] = ReadFileHash(physicalPath == null ? null : physicalPath + ".meta")
            };
        }

        private static string ResolvePhysicalAssetPath(string projectRoot, string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!assetPath.StartsWith("Packages/", StringComparison.Ordinal)) return null;

            PackageInfo package = PackageInfo.FindForAssetPath(assetPath);
            if (package == null || string.IsNullOrEmpty(package.assetPath) || string.IsNullOrEmpty(package.resolvedPath)) return null;
            string prefix = package.assetPath.TrimEnd('/') + "/";
            if (!assetPath.StartsWith(prefix, StringComparison.Ordinal)) return null;
            return Path.GetFullPath(Path.Combine(package.resolvedPath,
                assetPath.Substring(prefix.Length).Replace('/', Path.DirectorySeparatorChar)));
        }

        private static JObject ReadFileHash(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return new JObject { ["status"] = "Unavailable_PhysicalPathUnresolved" };
                if (!File.Exists(path))
                    return new JObject { ["status"] = "Unavailable_FileMissing" };
                return new JObject { ["status"] = "Available", ["sha256"] = Sha256(path) };
            }
            catch (Exception error)
            {
                return new JObject
                {
                    ["status"] = "Unavailable_ReadFailed",
                    ["error"] = error.GetType().Name + ": " + error.Message
                };
            }
        }

        private static string RequireFreshPrivateTemporaryOutput()
        {
            string rawPath = Environment.GetEnvironmentVariable(OutputKey);
            if (string.IsNullOrWhiteSpace(rawPath))
                throw new InvalidOperationException("Required fresh output environment variable missing: " + OutputKey);
            string path = Path.GetFullPath(rawPath);
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal))
                throw new InvalidOperationException("S073 dependency diagnostic output must be under /private/tmp: " + OutputKey);
            if (File.Exists(path)) throw new InvalidOperationException("Fresh create-new output path required: " + path);
            return path;
        }

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
