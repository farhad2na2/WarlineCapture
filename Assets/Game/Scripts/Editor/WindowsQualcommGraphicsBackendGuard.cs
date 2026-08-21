using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    /// <summary>
    /// Avoids a reproducible native heap-corruption crash in the x64 Unity Editor
    /// running through Windows ARM64 emulation on Qualcomm Adreno D3D12 drivers.
    /// </summary>
    [InitializeOnLoad]
    internal static class WindowsQualcommGraphicsBackendGuard
    {
        private const string ForceD3D11Argument = "-force-d3d11";
        private static bool restartQueued;

        static WindowsQualcommGraphicsBackendGuard()
        {
            if (Application.isBatchMode ||
                !IsQualcommAdrenoWindowsDevice(
                    SystemInfo.operatingSystemFamily,
                    SystemInfo.graphicsDeviceName))
            {
                return;
            }

            EditorApplication.delayCall += ConfigureAndReopenWithD3D11;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
            {
                Debug.LogWarning(
                    "[GraphicsBackendGuard] Qualcomm Adreno D3D12 was detected. " +
                    "Persisting D3D11 for Windows before reopening the project to avoid the native " +
                    "Match-scene heap-corruption crash.");
            }
        }

        internal static bool RequiresD3D11(
            OperatingSystemFamily operatingSystemFamily,
            GraphicsDeviceType graphicsDeviceType,
            string graphicsDeviceName,
            bool isBatchMode)
        {
            if (isBatchMode || graphicsDeviceType != GraphicsDeviceType.Direct3D12)
            {
                return false;
            }

            return IsQualcommAdrenoWindowsDevice(operatingSystemFamily, graphicsDeviceName);
        }

        internal static bool IsQualcommAdrenoWindowsDevice(
            OperatingSystemFamily operatingSystemFamily,
            string graphicsDeviceName)
        {
            if (operatingSystemFamily != OperatingSystemFamily.Windows ||
                string.IsNullOrWhiteSpace(graphicsDeviceName))
            {
                return false;
            }

            return graphicsDeviceName.IndexOf("Qualcomm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                graphicsDeviceName.IndexOf("Adreno", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool RequiresPersistentD3D11Preference(
            bool useDefaultGraphicsApis,
            GraphicsDeviceType[] graphicsApis)
        {
            return useDefaultGraphicsApis ||
                graphicsApis == null ||
                graphicsApis.Length == 0 ||
                graphicsApis[0] != GraphicsDeviceType.Direct3D11;
        }

        private static void ConfigureAndReopenWithD3D11()
        {
            EditorApplication.delayCall -= ConfigureAndReopenWithD3D11;
            if (restartQueued || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            PersistD3D11Preference();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12)
                return;

            restartQueued = true;
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            MethodInfo openProject = typeof(EditorApplication).GetMethod(
                "OpenProject",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(string[]) },
                modifiers: null);

            if (openProject == null)
            {
                restartQueued = false;
                Debug.LogError(
                    $"[GraphicsBackendGuard] Unity cannot automatically reopen this project. " +
                    $"Close the Editor and launch it with {ForceD3D11Argument}.");
                return;
            }

            openProject.Invoke(null, new object[] { projectPath, new[] { ForceD3D11Argument } });
        }

        private static void PersistD3D11Preference()
        {
            const BuildTarget target = BuildTarget.StandaloneWindows64;
            bool useDefaultGraphicsApis = PlayerSettings.GetUseDefaultGraphicsAPIs(target);
            GraphicsDeviceType[] graphicsApis = PlayerSettings.GetGraphicsAPIs(target);
            if (!RequiresPersistentD3D11Preference(useDefaultGraphicsApis, graphicsApis))
                return;

            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.Direct3D11 });
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[GraphicsBackendGuard] Windows graphics API preference set to D3D11 for Qualcomm Adreno. " +
                "Future Hub launches will no longer need the D3D12 close/reopen cycle.");
        }
    }
}
