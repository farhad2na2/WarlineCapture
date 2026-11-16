#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishValidationPlayerBuilder
    {
        const string Define = "WARLINE_VALIDATION_PLAYER";
        public static void Build()
        {
            string outputRoot = Argument("-warlineValidationBuildRoot");
            if (!Path.IsPathRooted(outputRoot)) throw new ArgumentException("Absolute unique build root required");
            outputRoot = Path.GetFullPath(outputRoot);
            if (Directory.Exists(outputRoot) || File.Exists(outputRoot)) throw new IOException("Build root already exists: " + outputRoot);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
                throw new InvalidOperationException("Start the owned Editor with StandaloneOSX active; shipping settings are not switched by this entry");
            string[] scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            string[] expected = { "Assets/Game/Scenes/Menu.unity", "Assets/Game/Scenes/Match.unity" };
            if (!scenes.SequenceEqual(expected)) throw new InvalidOperationException("Enabled scene set changed; review before building");
            foreach (string scene in scenes)
                if (!File.Exists(scene) || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scene)))
                    throw new FileNotFoundException("Enabled source scene missing", scene);
            Directory.CreateDirectory(outputRoot);
            string app = Path.Combine(outputRoot, "WarlineValidation.app");
            string receipt = Path.Combine(outputRoot, "build-receipt.json");
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            JObject before = SourceInputs(project, scenes);
            AtomicJson(Path.Combine(outputRoot, "build-inputs.json"), before);
            try
            {
                // Same normal source additions/gates; no isolated candidate override or scene filtering.
                var additions = new OperationMapEntitySceneBuildAdditions().RegisterAdditionalEntityScenesToBuild();
                var additionalScenes = new JArray(additions.Select(x => x.ToString()).OrderBy(x => x).Select(x => new JObject {
                    ["guid"] = x, ["path"] = AssetDatabase.GUIDToAssetPath(x) }));
                var options = new BuildPlayerOptions {
                    scenes = scenes,
                    target = BuildTarget.StandaloneOSX,
                    targetGroup = BuildTargetGroup.Standalone,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    extraScriptingDefines = new[] { Define },
                    locationPathName = app,
                    options = BuildOptions.DetailedBuildReport
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report == null || report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Player build failed: " + (report == null ? "no report" : report.summary.result.ToString()));
                JObject after = SourceInputs(project, scenes);
                if ((string)before["fingerprint"] != (string)after["fingerprint"])
                    throw new InvalidOperationException("Source/settings/scene inputs changed during build; no Passed receipt emitted");
                string executable = PlayerExecutable(app);
                var package = new JArray();
                foreach (string file in Directory.GetFiles(app, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
                    package.Add(new JObject { ["path"] = Path.GetFullPath(file), ["relative_path"] = file.Substring(app.Length + 1),
                        ["size"] = new FileInfo(file).Length, ["sha256"] = Sha(file) });
                var result = new JObject {
                    ["schema_version"] = 1, ["kind"] = "SkirmishValidationPlayerBuild", ["status"] = "Passed",
                    ["unity_version"] = Application.unityVersion, ["validation_define"] = Define,
                    ["extra_scripting_defines"] = new JArray(Define), ["build_guid"] = report.summary.guid.ToString(),
                    ["build_target"] = BuildTarget.StandaloneOSX.ToString(), ["subtarget"] = "Player",
                    ["build_root"] = app, ["output_root"] = outputRoot, ["player_executable"] = executable,
                    ["player_sha256"] = Sha(executable), ["player_size"] = new FileInfo(executable).Length,
                    ["scenes"] = new JArray(scenes), ["additional_entity_scenes_preflight"] = additionalScenes,
                    ["backend"] = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone).ToString(),
                    ["build_report_result"] = report.summary.result.ToString(), ["build_report_errors"] = report.summary.totalErrors,
                    ["build_report_warnings"] = report.summary.totalWarnings, ["build_report_total_size"] = report.summary.totalSize,
                    ["build_seconds"] = report.summary.totalTime.TotalSeconds, ["editor_log"] = Application.consoleLogPath,
                    ["source_fingerprint"] = before["fingerprint"], ["source_fingerprint_scope"] = before["scope"],
                    ["package_files"] = package, ["shipping_settings_changed_by_entry"] = false,
                    ["runtime_validation"] = "Pending; build success is not S002 gameplay or ARIA acceptance"
                };
                AtomicJson(receipt, result);
                Debug.Log("[SkirmishValidationPlayerBuild] result=Passed receipt=" + receipt + " executable=" + executable);
            }
            catch (Exception exception)
            {
                AtomicJson(Path.Combine(outputRoot, "build-failure.json"), new JObject {
                    ["schema_version"] = 1, ["kind"] = "SkirmishValidationPlayerBuild", ["status"] = "Failed",
                    ["message"] = exception.ToString(), ["unity_version"] = Application.unityVersion,
                    ["editor_log"] = Application.consoleLogPath, ["validation_define"] = Define });
                Debug.LogError("[SkirmishValidationPlayerBuild] result=Failed " + exception);
                throw;
            }
        }
        static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int[] indices = Enumerable.Range(0, args.Length).Where(i => args[i] == key).ToArray();
            if (indices.Length != 1 || indices[0] + 1 >= args.Length || string.IsNullOrWhiteSpace(args[indices[0] + 1]))
                throw new ArgumentException("Exactly one " + key + " <unique absolute directory> required");
            return args[indices[0] + 1];
        }
        static JObject SourceInputs(string project, string[] scenes)
        {
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(Path.Combine(project, "Assets"), "*", SearchOption.AllDirectories))
                if (file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".asmdef", StringComparison.Ordinal) || file.EndsWith(".asmref", StringComparison.Ordinal))
                { paths.Add(file); if (File.Exists(file + ".meta")) paths.Add(file + ".meta"); }
            foreach (string file in Directory.GetFiles(Path.Combine(project, "ProjectSettings"), "*", SearchOption.TopDirectoryOnly)) paths.Add(file);
            foreach (string name in new[] { "manifest.json", "packages-lock.json" })
                paths.Add(Path.Combine(project, "Packages", name));
            var hashes = new JObject();
            foreach (string file in paths) hashes[file.Substring(project.Length + 1)] = Sha(file);
            var dependencies = new JObject();
            foreach (string scene in scenes) dependencies[scene] = AssetDatabase.GetAssetDependencyHash(scene).ToString();
            string serialized = hashes.ToString(Newtonsoft.Json.Formatting.None) + "\n" + dependencies.ToString(Newtonsoft.Json.Formatting.None);
            using var sha = SHA256.Create();
            return new JObject { ["scope"] = "Assets C#/asmdef/asmref and their metas; ProjectSettings; package manifests; enabled-scene dependency hashes (not a full all-assets census)",
                ["fingerprint"] = Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(serialized))), ["files"] = hashes, ["scene_dependencies"] = dependencies };
        }
        static string PlayerExecutable(string app)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
            var xml = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(Path.Combine(app, "Contents", "Info.plist"), settings)) xml.Load(reader);
            var value = xml.SelectSingleNode("/plist/dict/key[text()='CFBundleExecutable']/following-sibling::*[1]");
            string name = value?.InnerText;
            if (value?.Name != "string" || string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || name == "." || name == "..")
                throw new InvalidDataException("Actual built CFBundleExecutable is missing or invalid");
            string executable = Path.GetFullPath(Path.Combine(app, "Contents", "MacOS", name));
            if (!File.Exists(executable) || new FileInfo(executable).Length == 0) throw new FileNotFoundException("Built executable missing", executable);
            return executable;
        }
        static string Sha(string path) { using var sha = SHA256.Create(); using var stream = File.OpenRead(path); return Hex(sha.ComputeHash(stream)); }
        static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        static void AtomicJson(string path, JObject value)
        {
            string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temp, value.ToString(Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
            File.Move(temp, path);
        }
    }
}
#endif
