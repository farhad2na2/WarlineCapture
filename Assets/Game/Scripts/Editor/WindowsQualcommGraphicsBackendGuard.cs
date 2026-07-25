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
            if (!RequiresD3D11(
                    SystemInfo.operatingSystemFamily,
                    SystemInfo.graphicsDeviceType,
                    SystemInfo.graphicsDeviceName,
                    Application.isBatchMode))
            {
                return;
            }

            EditorApplication.delayCall += ReopenWithD3D11;
            Debug.LogWarning(
                "[GraphicsBackendGuard] Qualcomm Adreno D3D12 was detected. " +
                "Reopening the project with D3D11 to avoid the native Match-scene heap-corruption crash.");
        }

        internal static bool RequiresD3D11(
            OperatingSystemFamily operatingSystemFamily,
            GraphicsDeviceType graphicsDeviceType,
            string graphicsDeviceName,
            bool isBatchMode)
        {
            if (isBatchMode ||
                operatingSystemFamily != OperatingSystemFamily.Windows ||
                graphicsDeviceType != GraphicsDeviceType.Direct3D12 ||
                string.IsNullOrWhiteSpace(graphicsDeviceName))
            {
                return false;
            }

            return graphicsDeviceName.IndexOf("Qualcomm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                graphicsDeviceName.IndexOf("Adreno", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ReopenWithD3D11()
        {
            EditorApplication.delayCall -= ReopenWithD3D11;
            if (restartQueued || EditorApplication.isPlayingOrWillChangePlaymode)
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
    }
}
