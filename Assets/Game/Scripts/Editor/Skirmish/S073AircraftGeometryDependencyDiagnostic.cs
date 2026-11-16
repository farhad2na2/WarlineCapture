#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Configs;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Read-only comparison of installed aircraft metadata against current source
    // measurements. The only write is the requested fresh private-tmp report.
    internal static class S073AircraftGeometryDependencyDiagnostic
    {
        private const string OutputKey = "WARLINE_AIRCRAFT_GEOMETRY_DIAGNOSTIC_OUTPUT";

        public static void RunInstalledVsMeasuredDiagnostic()
        {
            string outputPath = null;
            int exitCode = 1;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Editor must be in Edit Mode for read-only source measurement.");
                outputPath = RequireFreshOutput();
                var result = new JObject
                {
                    ["kind"] = "S073AircraftGeometryDependencyDiagnostic",
                    ["schema_version"] = 1,
                    ["scope"] = "ReadOnlyInstalledVersusCurrentSourceMeasurement_NoAssetWrites_NoReadinessClaim",
                    ["unity_version"] = Application.unityVersion,
                    ["rotor_geometry"] = CompareRotorGeometry(),
                    ["canonical_geometry"] = CompareCanonicalGeometry(),
                    ["mission_ready"] = false,
                    ["player_readiness"] = false
                };
                int mismatchCount = (int)result["rotor_geometry"]["mismatch_count"] +
                                    (int)result["canonical_geometry"]["mismatch_count"];
                result["overall_status"] = mismatchCount == 0 ? "NoMismatchDetected" : "MismatchDetected";
                result["mismatch_count"] = mismatchCount;
                WriteFresh(outputPath, result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
                Debug.Log("[S073AircraftGeometryDiagnostic] result=Completed mismatch_count=" + mismatchCount +
                          " output=" + outputPath);
                exitCode = 0;
            }
            catch (Exception error)
            {
                string message = error.GetType().Name + ":" + error.Message.Replace('\n', ' ').Replace('\r', ' ');
                try
                {
                    if (outputPath != null && !File.Exists(outputPath))
                        WriteFresh(outputPath, new JObject
                        {
                            ["kind"] = "S073AircraftGeometryDependencyDiagnostic",
                            ["schema_version"] = 1,
                            ["result"] = "Failed",
                            ["error"] = message,
                            ["mission_ready"] = false,
                            ["player_readiness"] = false
                        }.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
                }
                catch (Exception writeError)
                {
                    message += " report_write=" + writeError.GetType().Name + ":" +
                               writeError.Message.Replace('\n', ' ').Replace('\r', ' ');
                }
                Debug.LogError("[S073AircraftGeometryDiagnostic] result=Failed error=" + message);
            }
            finally
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            }
        }

        private static JObject CompareRotorGeometry()
        {
            var installed = AssetDatabase.LoadAssetAtPath<SkirmishAircraftRotorGeometryConfig>(
                SkirmishAircraftRotorGeometryAuthoring.Output);
            if (installed == null) throw new FileNotFoundException("Installed rotor metadata is missing.",
                SkirmishAircraftRotorGeometryAuthoring.Output);

            SkirmishAircraftRotorGeometryConfig measured = null;
            try
            {
                measured = SkirmishAircraftRotorGeometryAuthoring.Measure(installed.NativeTemplateProvenanceSha256);
                var installedRows = installed.Bindings ?? Array.Empty<SkirmishAircraftRotorGeometryConfig.Binding>();
                var measuredRows = measured.Bindings ?? Array.Empty<SkirmishAircraftRotorGeometryConfig.Binding>();
                var rows = new JArray();
                int mismatchCount = 0;
                int count = Math.Max(installedRows.Length, measuredRows.Length);
                for (int i = 0; i < count; i++)
                {
                    var expected = i < installedRows.Length ? installedRows[i] : null;
                    var actual = i < measuredRows.Length ? measuredRows[i] : null;
                    var differences = new List<string>();
                    CompareRotorRow(expected, actual, differences);
                    if (differences.Count > 0) mismatchCount++;
                    rows.Add(new JObject
                    {
                        ["row_index"] = i,
                        ["role"] = expected != null ? expected.Role.ToString() : actual != null ? actual.Role.ToString() : null,
                        ["visual_kind"] = expected != null ? expected.VisualKind : actual != null ? actual.VisualKind : (byte?)null,
                        ["source_path_mismatch"] = expected != null && actual != null && expected.SourcePath != actual.SourcePath,
                        ["dependency_hash_mismatch"] = expected != null && actual != null && expected.DependencyHash != actual.DependencyHash,
                        ["other_geometry_fields_differ"] = differences.Exists(field =>
                            field != "SourcePath" && field != "DependencyHash"),
                        ["installed"] = RotorRow(expected),
                        ["measured"] = RotorRow(actual),
                        ["differences"] = new JArray(differences)
                    });
                }
                return new JObject
                {
                    ["installed_asset_path"] = SkirmishAircraftRotorGeometryAuthoring.Output,
                    ["installed_dependency_provenance"] = installed.NativeTemplateProvenanceSha256,
                    ["measured_dependency_provenance"] = measured.NativeTemplateProvenanceSha256,
                    ["installed_row_count"] = installedRows.Length,
                    ["measured_row_count"] = measuredRows.Length,
                    ["mismatch_count"] = mismatchCount,
                    ["rows"] = rows
                };
            }
            finally
            {
                if (measured != null) UnityEngine.Object.DestroyImmediate(measured);
            }
        }

        private static void CompareRotorRow(SkirmishAircraftRotorGeometryConfig.Binding expected,
            SkirmishAircraftRotorGeometryConfig.Binding actual, List<string> differences)
        {
            if (expected == null || actual == null)
            {
                differences.Add(expected == null ? "InstalledRowMissing" : "MeasuredRowMissing");
                return;
            }
            AddIf(expected.Role != actual.Role, differences, "Role");
            AddIf(expected.VisualKind != actual.VisualKind, differences, "VisualKind");
            AddIf(expected.Axis != actual.Axis, differences, "Axis");
            AddIf(expected.Mesh != actual.Mesh, differences, "MeshReference");
            AddIf(expected.SubMesh != actual.SubMesh, differences, "SubMesh");
            AddIf(expected.GeometrySha256 != actual.GeometrySha256, differences, "GeometrySha256");
            AddIf(expected.SourcePath != actual.SourcePath, differences, "SourcePath");
            AddIf(expected.DependencyHash != actual.DependencyHash, differences, "DependencyHash");
            AddIf(expected.RendererPath != actual.RendererPath, differences, "RendererPath");
            AddIf(expected.BladePath != actual.BladePath, differences, "BladePath");
            AddIf(expected.MeshToBlade != actual.MeshToBlade, differences, "MeshToBlade");
            AddIf(expected.BladeToVisualRoot != actual.BladeToVisualRoot, differences, "BladeToVisualRoot");
            AddIf(expected.MinimumAxial != actual.MinimumAxial, differences, "MinimumAxial");
            AddIf(expected.MaximumAxial != actual.MaximumAxial, differences, "MaximumAxial");
            AddIf(expected.MaximumRadius != actual.MaximumRadius, differences, "MaximumRadius");
        }

        private static JObject RotorRow(SkirmishAircraftRotorGeometryConfig.Binding row)
        {
            if (row == null) return null;
            return new JObject
            {
                ["role"] = row.Role.ToString(),
                ["visual_kind"] = row.VisualKind,
                ["axis"] = row.Axis,
                ["mesh"] = ObjectIdentity(row.Mesh),
                ["submesh"] = row.SubMesh,
                ["geometry_sha256"] = row.GeometrySha256,
                ["source_path"] = row.SourcePath,
                ["dependency_hash"] = row.DependencyHash,
                ["renderer_path"] = row.RendererPath,
                ["blade_path"] = row.BladePath,
                ["mesh_to_blade"] = Matrix(row.MeshToBlade),
                ["blade_to_visual_root"] = Matrix(row.BladeToVisualRoot),
                ["minimum_axial"] = row.MinimumAxial,
                ["maximum_axial"] = row.MaximumAxial,
                ["maximum_radius"] = row.MaximumRadius
            };
        }

        private static JObject CompareCanonicalGeometry()
        {
            var installed = AssetDatabase.LoadAssetAtPath<SkirmishCanonicalAircraftGeometryConfig>(
                SkirmishCanonicalAircraftGeometryAuthoring.Output);
            if (installed == null) throw new FileNotFoundException("Installed canonical metadata is missing.",
                SkirmishCanonicalAircraftGeometryAuthoring.Output);

            SkirmishCanonicalAircraftGeometryConfig measured = null;
            try
            {
                measured = SkirmishCanonicalAircraftGeometryAuthoring.Measure();
                var installedRoles = installed.Roles ?? Array.Empty<SkirmishCanonicalAircraftGeometryConfig.Entry>();
                var measuredRoles = measured.Roles ?? Array.Empty<SkirmishCanonicalAircraftGeometryConfig.Entry>();
                var roles = new JArray();
                int mismatchCount = 0;
                int count = Math.Max(installedRoles.Length, measuredRoles.Length);
                for (int i = 0; i < count; i++)
                {
                    var expected = i < installedRoles.Length ? installedRoles[i] : null;
                    var actual = i < measuredRoles.Length ? measuredRoles[i] : null;
                    var differences = new List<string>();
                    var sources = CompareCanonicalSources(expected, actual, differences, ref mismatchCount);
                    var draws = CompareCanonicalDraws(expected, actual, differences, ref mismatchCount);
                    if (expected == null || actual == null || expected.Role != actual.Role ||
                        expected.PrefabKey != actual.PrefabKey) mismatchCount++;
                    if (expected != null && actual != null && expected.Role != actual.Role)
                        differences.Add("Role");
                    if (expected != null && actual != null && expected.PrefabKey != actual.PrefabKey)
                        differences.Add("PrefabKey");
                    roles.Add(new JObject
                    {
                        ["role_index"] = i,
                        ["role"] = expected != null ? expected.Role.ToString() : actual != null ? actual.Role.ToString() : null,
                        ["prefab_key_installed"] = expected != null ? expected.PrefabKey : null,
                        ["prefab_key_measured"] = actual != null ? actual.PrefabKey : null,
                        ["installed_source_count"] = expected != null && expected.Sources != null ? expected.Sources.Length : 0,
                        ["measured_source_count"] = actual != null && actual.Sources != null ? actual.Sources.Length : 0,
                        ["installed_draw_count"] = expected != null && expected.Draws != null ? expected.Draws.Length : 0,
                        ["measured_draw_count"] = actual != null && actual.Draws != null ? actual.Draws.Length : 0,
                        ["differences"] = new JArray(differences),
                        ["source_comparisons"] = sources,
                        ["draw_comparisons"] = draws
                    });
                }
                if (installedRoles.Length != measuredRoles.Length) mismatchCount++;
                return new JObject
                {
                    ["installed_asset_path"] = SkirmishCanonicalAircraftGeometryAuthoring.Output,
                    ["installed_schema_version"] = installed.SchemaVersion,
                    ["measured_schema_version"] = measured.SchemaVersion,
                    ["installed_role_count"] = installedRoles.Length,
                    ["measured_role_count"] = measuredRoles.Length,
                    ["mismatch_count"] = mismatchCount,
                    ["roles"] = roles
                };
            }
            finally
            {
                if (measured != null) UnityEngine.Object.DestroyImmediate(measured);
            }
        }

        private static JArray CompareCanonicalSources(SkirmishCanonicalAircraftGeometryConfig.Entry expected,
            SkirmishCanonicalAircraftGeometryConfig.Entry actual, List<string> roleDifferences, ref int mismatchCount)
        {
            var rows = new JArray();
            var installed = expected != null && expected.Sources != null
                ? expected.Sources : Array.Empty<SkirmishCanonicalAircraftGeometryConfig.Source>();
            var measured = actual != null && actual.Sources != null
                ? actual.Sources : Array.Empty<SkirmishCanonicalAircraftGeometryConfig.Source>();
            int count = Math.Max(installed.Length, measured.Length);
            for (int i = 0; i < count; i++)
            {
                var left = i < installed.Length ? installed[i] : null;
                var right = i < measured.Length ? measured[i] : null;
                var differences = new List<string>();
                if (left == null || right == null) differences.Add(left == null ? "InstalledSourceMissing" : "MeasuredSourceMissing");
                else
                {
                    AddIf(left.VisualKind != right.VisualKind, differences, "VisualKind");
                    AddIf(left.Prefab != right.Prefab, differences, "PrefabReference");
                    AddIf(left.Path != right.Path, differences, "Path");
                    AddIf(left.DependencyHash != right.DependencyHash, differences, "DependencyHash");
                }
                if (differences.Count > 0)
                {
                    mismatchCount++;
                    roleDifferences.Add("Source[" + i + "]");
                }
                rows.Add(new JObject
                {
                    ["source_index"] = i,
                    ["visual_kind_installed"] = left != null ? left.VisualKind : (byte?)null,
                    ["visual_kind_measured"] = right != null ? right.VisualKind : (byte?)null,
                    ["path_mismatch"] = left != null && right != null && left.Path != right.Path,
                    ["dependency_hash_mismatch"] = left != null && right != null && left.DependencyHash != right.DependencyHash,
                    ["other_source_identity_fields_differ"] = differences.Exists(field =>
                        field != "Path" && field != "DependencyHash"),
                    ["installed"] = CanonicalSource(left),
                    ["measured"] = CanonicalSource(right),
                    ["differences"] = new JArray(differences)
                });
            }
            return rows;
        }

        private static JObject CanonicalSource(SkirmishCanonicalAircraftGeometryConfig.Source source)
        {
            if (source == null) return null;
            return new JObject
            {
                ["visual_kind"] = source.VisualKind,
                ["prefab"] = ObjectIdentity(source.Prefab),
                ["path"] = source.Path,
                ["dependency_hash"] = source.DependencyHash
            };
        }

        private static JArray CompareCanonicalDraws(SkirmishCanonicalAircraftGeometryConfig.Entry expected,
            SkirmishCanonicalAircraftGeometryConfig.Entry actual, List<string> roleDifferences, ref int mismatchCount)
        {
            var rows = new JArray();
            var installed = expected != null && expected.Draws != null
                ? expected.Draws : Array.Empty<SkirmishRigidAircraftGeometryConfig.Draw>();
            var measured = actual != null && actual.Draws != null
                ? actual.Draws : Array.Empty<SkirmishRigidAircraftGeometryConfig.Draw>();
            var used = new HashSet<int>();
            for (int i = 0; i < measured.Length; i++)
            {
                var right = measured[i];
                int match = -1;
                for (int j = 0; j < installed.Length; j++)
                {
                    if (used.Contains(j) || installed[j] == null || right == null) continue;
                    if (DrawKey(installed[j]) == DrawKey(right)) { match = j; break; }
                }
                var left = match >= 0 ? installed[match] : null;
                if (match >= 0) used.Add(match);
                var differences = new List<string>();
                if (left == null) differences.Add("InstalledDrawMissingForMeasuredMeshSubmesh");
                else
                {
                    AddIf(left.VertexCount != right.VertexCount, differences, "VertexCount");
                    AddIf(left.DrawnIndexCount != right.DrawnIndexCount, differences, "DrawnIndexCount");
                    AddIf(left.LocalBounds.center != right.LocalBounds.center, differences, "LocalBoundsCenter");
                    AddIf(left.LocalBounds.size != right.LocalBounds.size, differences, "LocalBoundsSize");
                    AddIf(left.GeometrySha256 != right.GeometrySha256, differences, "GeometrySha256");
                }
                if (differences.Count > 0)
                {
                    mismatchCount++;
                    roleDifferences.Add("Draw[" + i + "]");
                }
                rows.Add(new JObject
                {
                    ["mesh_submesh_key"] = right != null ? DrawKey(right) : null,
                    ["installed"] = CanonicalDraw(left),
                    ["measured"] = CanonicalDraw(right),
                    ["differences"] = new JArray(differences)
                });
            }
            for (int i = 0; i < installed.Length; i++)
            {
                if (used.Contains(i)) continue;
                mismatchCount++;
                roleDifferences.Add("InstalledDraw[" + i + "]MissingFromMeasurement");
                rows.Add(new JObject
                {
                    ["mesh_submesh_key"] = DrawKey(installed[i]),
                    ["installed"] = CanonicalDraw(installed[i]),
                    ["measured"] = null,
                    ["differences"] = new JArray("MeasuredDrawMissing")
                });
            }
            return rows;
        }

        private static string DrawKey(SkirmishRigidAircraftGeometryConfig.Draw draw)
        {
            if (draw == null) return "<null>";
            if (draw.Mesh == null) return "<null-mesh>:" + draw.SubMesh;
            JObject identity = ObjectIdentity(draw.Mesh);
            return (string)identity["guid"] + ":" + (string)identity["local_file_id"] + ":" + draw.SubMesh;
        }

        private static JObject CanonicalDraw(SkirmishRigidAircraftGeometryConfig.Draw draw)
        {
            if (draw == null) return null;
            return new JObject
            {
                ["mesh"] = ObjectIdentity(draw.Mesh),
                ["submesh"] = draw.SubMesh,
                ["vertex_count"] = draw.VertexCount,
                ["drawn_index_count"] = draw.DrawnIndexCount,
                ["local_bounds_center"] = Vector(draw.LocalBounds.center),
                ["local_bounds_size"] = Vector(draw.LocalBounds.size),
                ["geometry_sha256"] = draw.GeometrySha256
            };
        }

        private static JObject ObjectIdentity(UnityEngine.Object value)
        {
            if (value == null) return null;
            string guid = null;
            long localId = 0;
            bool persistentIdentity = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out guid, out localId);
            return new JObject
            {
                ["name"] = value.name,
                ["asset_path"] = AssetDatabase.GetAssetPath(value),
                ["guid"] = persistentIdentity ? guid : null,
                ["local_file_id"] = persistentIdentity ? localId : (long?)null,
                ["persistent_identity_available"] = persistentIdentity
            };
        }

        private static JArray Matrix(Matrix4x4 matrix)
        {
            var values = new JArray();
            for (int i = 0; i < 16; i++) values.Add(matrix[i]);
            return values;
        }

        private static JArray Vector(Vector3 vector) => new JArray(vector.x, vector.y, vector.z);

        private static void AddIf(bool mismatch, List<string> fields, string field)
        {
            if (mismatch) fields.Add(field);
        }

        private static string RequireFreshOutput()
        {
            string configured = Environment.GetEnvironmentVariable(OutputKey);
            if (string.IsNullOrEmpty(configured) || !Path.IsPathRooted(configured))
                throw new InvalidOperationException(OutputKey + " must be an absolute path.");
            string path = Path.GetFullPath(configured);
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) ||
                Path.GetDirectoryName(path) != "/private/tmp" || File.Exists(path) || Directory.Exists(path))
                throw new InvalidOperationException("Fresh direct-child /private/tmp output required: " + path);
            return path;
        }

        private static void WriteFresh(string path, string contents)
        {
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(file, new UTF8Encoding(false)))
                writer.Write(contents);
        }
    }
}
#endif
