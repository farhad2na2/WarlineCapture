using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using System.IO.Compression;

public class BuildScript
{
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
        var outputPath = $"{outputDirectory}/WarlineCapture.{extension}";
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            target = BuildTarget.Android,
            options = BuildOptions.None,
            locationPathName = outputPath
        };

        ConfigureAndroidBuild(buildType == "AAB");
        
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

    private static void ZipBuild(string path)
    {
        var zipPath = $"{path}.zip";
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }
        ZipFile.CreateFromDirectory(path, zipPath);
    }

    private static void ExecuteBuild(BuildPlayerOptions buildPlayerOptions)
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
    }

    private static void ConfigureAndroidBuild(bool buildAppBundle)
    {
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        //PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        UnityEngine.Debug.Log("[BuildScript] Android build configured: architectures=ARM64");
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
