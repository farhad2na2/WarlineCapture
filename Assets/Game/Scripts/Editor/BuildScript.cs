using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Reflection;

namespace Game.Editor
{
    public class BuildScript
    {
        internal const BuildOptions ReleaseAndroidBuildOptions =
            BuildOptions.DetailedBuildReport;

        internal const BuildOptions CleanReleaseAndroidBuildOptions =
            ReleaseAndroidBuildOptions |
            BuildOptions.CleanBuildCache;

        public static void BuildWindows()
        {
            SwitchBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            var path = "Build/Windows";
            CreateDirectory(path);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                locationPathName = $"{path}/Build.exe"
            };
            ExecuteBuild(buildPlayerOptions);
            ZipBuild(path);
        }

        public static void BuildWebGL()
        {
            SwitchBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            var path = "Build/WebGL";
            CreateDirectory(path);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
                locationPathName = $"{path}"
            };
            ExecuteBuild(buildPlayerOptions);
            ZipBuild(path);
        }

        public static void BuildIOS()
        {
            SwitchBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            var path = "Build/iOS";
            CreateDirectory(path);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                target = BuildTarget.iOS,
                options = BuildOptions.None,
                locationPathName = $"{path}"
            };
            ExecuteBuild(buildPlayerOptions);
            ZipBuild(path);
        }

        public static void BuildAndroid()
        {
            var arg = Environment.GetCommandLineArgs();
            var buildType = GetArgument(arg, "-buildType");
            AndroidBuildReportProvenance buildProvenance =
                AndroidBuildReportGenerator.CaptureGitProvenance();
            bool cleanBuildCache = arg.Any(value =>
                string.Equals(value, "-cleanBuild", StringComparison.OrdinalIgnoreCase));
            SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigureGradleUserHome();

            var outputDirectory = buildType switch
            {
                "APK" => "Build/AndroidAPK",
                "AAB" => "Build/AndroidAAB",
                _ => "Unknown"
            };

            if (outputDirectory == "Unknown")
            {
                throw new ArgumentException("BuildAndroid requires -buildType APK or -buildType AAB.");
            }

            CreateDirectory(outputDirectory);
            var extension = buildType == "AAB" ? "aab" : "apk";
            var outputPath = $"{outputDirectory}/{ResolveBuildOutputName()}.{extension}";
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            ConfigureAndroidBuild(buildType == "AAB");
            OperationMapAddressablesBuildReportBuilder.Run();

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject(GetEnabledScenes()),
                target = BuildTarget.Android,
                options = cleanBuildCache
                    ? CleanReleaseAndroidBuildOptions
                    : ReleaseAndroidBuildOptions,
                locationPathName = outputPath
            };

            UnityEngine.Debug.Log(
                $"[BuildScript] Android cache mode: {(cleanBuildCache ? "clean" : "incremental")}");

            BuildReport report = ExecuteBuild(buildPlayerOptions);
            OperationMapEntityScenePackageGate.Validate(outputPath);
            AndroidBuildReportGenerator.GenerateAndWriteReports(
                report,
                buildType,
                buildProvenance);
        }

        public static void BuildDenseCityCandidateAndroidApk()
        {
            var arg = Environment.GetCommandLineArgs();
            bool cleanBuildCache = arg.Any(value =>
                string.Equals(value, "-cleanBuild", StringComparison.OrdinalIgnoreCase));
            AndroidBuildReportProvenance buildProvenance =
                AndroidBuildReportGenerator.CaptureGitProvenance();

            SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigureGradleUserHome();
            ConfigureAndroidBuild(buildAppBundle: false);
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .BuildDenseCityCandidateEmbeddedAndroidContent();

            const string outputDirectory = "Build/AndroidDenseCandidate";
            CreateDirectory(outputDirectory);
            string outputPath =
                $"{outputDirectory}/{ResolveBuildOutputName()}-DenseCityCandidate.apk";
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            string projectRoot = Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath,
                ".."));
            string[] scenes =
                OperationMapDenseCityCandidateAndroidPackageDeployment.ResolvePlayerScenes(
                    GetEnabledScenes(),
                    path => File.Exists(Path.GetFullPath(Path.Combine(projectRoot, path))) &&
                            !string.IsNullOrWhiteSpace(
                                AssetDatabase.AssetPathToGUID(path)));
            using var deployment =
                OperationMapDenseCityCandidateAndroidPackageDeployment.Begin(projectRoot);
            using var entitySceneOverride =
                OperationMapEntitySceneBuildAdditions.UseCurrentProcessSceneOverride(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.Android,
                options = cleanBuildCache
                    ? CleanReleaseAndroidBuildOptions
                    : ReleaseAndroidBuildOptions,
                locationPathName = outputPath
            };

            UnityEngine.Debug.Log(
                "[BuildScript] Dense candidate Android package: " +
                $"cacheMode={(cleanBuildCache ? "clean" : "incremental")} " +
                $"revision={buildProvenance.ExactCommit} dirty={buildProvenance.Dirty}");
            BuildReport report = ExecuteBuild(buildPlayerOptions);

            string denseGuid = AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            string productionGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);
            OperationMapDenseCityCandidateAndroidPackageDeployment.ValidatePackage(
                outputPath,
                denseGuid,
                productionGuid);
            UnityEngine.Debug.Log(
                "[DenseCityCandidateAndroidPackage] result=Passed " +
                $"output={outputPath} bytes={report.summary.totalSize} " +
                $"entitySceneGuid={denseGuid} productionEntitySceneIncluded=0");
        }

        public static void ValidateDenseCityCandidateAndroidApk()
        {
            const string packagePath =
                "Build/AndroidDenseCandidate/WarlineCapture-DenseCityCandidate.apk";
            string denseGuid = AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            string productionGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);
            OperationMapDenseCityCandidateAndroidPackageDeployment.ValidatePackage(
                packagePath,
                denseGuid,
                productionGuid);
            var package = new FileInfo(packagePath);
            UnityEngine.Debug.Log(
                "[DenseCityCandidateAndroidPackageGate] result=Passed " +
                $"output={package.FullName} bytes={package.Length} " +
                $"entitySceneGuid={denseGuid} productionEntitySceneIncluded=0");
        }

        public static void BuildAndroidProfilerApk()
        {
            BuildAndroidProfilerApk(disableBurstAot: false);
        }

        public static void BuildAndroidProfilerNoBurstApk()
        {
            BuildAndroidProfilerApk(disableBurstAot: true);
        }

        private static void BuildAndroidProfilerApk(bool disableBurstAot)
        {
            SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigureGradleUserHome();
            ConfigureAndroidBuild(false);
            OperationMapAddressablesBuildReportBuilder.Run();
            using var burstAotScope = disableBurstAot
                ? TryDisableBurstAotForAndroidBuild()
                : null;

            const string outputDirectory = "Build/AndroidProfiler";
            CreateDirectory(outputDirectory);
            var outputSuffix = disableBurstAot ? "Profiler-NoBurst" : "Profiler";
            var outputPath = $"{outputDirectory}/{ResolveBuildOutputName()}-{outputSuffix}.apk";
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject(GetEnabledScenes()),
                target = BuildTarget.Android,
                options =
                    BuildOptions.Development |
                    BuildOptions.ConnectWithProfiler,
                locationPathName = outputPath
            };

            UnityEngine.Debug.Log($"[BuildScript] Android profiler APK configured: development=1 autoconnectProfiler=1 scriptDebugging=0 deepProfiling=0 burstAot={(disableBurstAot ? 0 : 1)}");
            ExecuteBuild(buildPlayerOptions);
        }

        public static void CreateDirectory(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(x => x.enabled)
                .Select(x => x.path)
                .ToArray();
        }

        private static string ResolveBuildOutputName()
        {
            string productName = PlayerSettings.productName;
            if (string.IsNullOrWhiteSpace(productName))
                return "Game";

            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            string sanitized = new(productName
                .Select(character => invalidFileNameChars.Contains(character) ? '_' : character)
                .ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "Game" : sanitized;
        }

        private static void ZipBuild(string path)
        {
            var zipPath = $"{path}.zip";
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            ZipFile.CreateFromDirectory(path, zipPath);
        }

        private static BuildReport ExecuteBuild(BuildPlayerOptions buildPlayerOptions)
        {
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Build failed. target={buildPlayerOptions.target} result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings}");
            }

            UnityEngine.Debug.Log(
                $"Build succeeded. target={buildPlayerOptions.target} output={summary.outputPath} size={summary.totalSize} warnings={summary.totalWarnings}");
            return report;
        }

        private static void ConfigureAndroidBuild(bool buildAppBundle)
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.enableFrameTimingStats = true;
            //PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            UnityEngine.Debug.Log(
                "[BuildScript] Android build configured: architectures=ARM64 frameTimingStats=1");
        }

        private static void ConfigureGradleUserHome()
        {
            const string variableName = "GRADLE_USER_HOME";
            var configuredPath = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                UnityEngine.Debug.Log($"[BuildScript] Using existing {variableName}: {configuredPath}");
                return;
            }

            var projectGradleHome = Path.GetFullPath(Path.Combine("Library", "GradleCache"));
            CreateDirectory(projectGradleHome);
            Environment.SetEnvironmentVariable(variableName, projectGradleHome);
            UnityEngine.Debug.Log($"[BuildScript] Redirected {variableName} to {projectGradleHome}");
        }

        private static IDisposable TryDisableBurstAotForAndroidBuild()
        {
            Type settingsType = Type.GetType("Unity.Burst.Editor.BurstPlatformAotSettings, Unity.Burst.Editor");
            if (settingsType == null)
            {
                UnityEngine.Debug.LogWarning("[BuildScript] Burst AOT settings type not found; continuing without disabling Burst AOT.");
                return null;
            }

            MethodInfo getOrCreateSettings = settingsType.GetMethod(
                "GetOrCreateSettings",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo save = settingsType.GetMethod(
                "Save",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo enableBurstCompilation = settingsType.GetField(
                "EnableBurstCompilation",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (getOrCreateSettings == null || save == null || enableBurstCompilation == null)
            {
                UnityEngine.Debug.LogWarning("[BuildScript] Burst AOT settings members not found; continuing without disabling Burst AOT.");
                return null;
            }

            object target = (BuildTarget?)BuildTarget.Android;
            object settings = getOrCreateSettings.Invoke(null, new[] { target });
            bool original = (bool)enableBurstCompilation.GetValue(settings);
            if (!original)
            {
                UnityEngine.Debug.Log("[BuildScript] Android Burst AOT was already disabled.");
                return null;
            }

            enableBurstCompilation.SetValue(settings, false);
            save.Invoke(settings, new[] { target });
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[BuildScript] Android Burst AOT temporarily disabled for profiler APK build.");
            return new RestoreBurstAotSettings(settings, save, enableBurstCompilation, target, original);
        }

        private sealed class RestoreBurstAotSettings : IDisposable
        {
            private readonly object settings;
            private readonly MethodInfo save;
            private readonly FieldInfo enableBurstCompilation;
            private readonly object target;
            private readonly bool original;
            private bool restored;

            public RestoreBurstAotSettings(
                object settings,
                MethodInfo save,
                FieldInfo enableBurstCompilation,
                object target,
                bool original)
            {
                this.settings = settings;
                this.save = save;
                this.enableBurstCompilation = enableBurstCompilation;
                this.target = target;
                this.original = original;
            }

            public void Dispose()
            {
                if (restored)
                    return;

                restored = true;
                enableBurstCompilation.SetValue(settings, original);
                save.Invoke(settings, new[] { target });
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("[BuildScript] Android Burst AOT setting restored after profiler APK build.");
            }
        }

        private static void SwitchBuildTarget(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            if (EditorUserBuildSettings.activeBuildTarget != buildTarget &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget))
            {
                throw new InvalidOperationException(
                    $"Failed to switch active build target. targetGroup={buildTargetGroup} target={buildTarget}");
            }

            UnityEngine.Debug.Log($"[BuildScript] Active build target: {EditorUserBuildSettings.activeBuildTarget}");
        }

        private static string GetArgument(string[] args, string argumentName)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == argumentName && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
