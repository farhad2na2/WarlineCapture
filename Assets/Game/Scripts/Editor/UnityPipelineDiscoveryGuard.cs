using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Keeps the Unity Pipeline discovery descriptor available for the already-running Editor.
    /// The package server can remain healthy after an interrupted test removes its descriptor;
    /// without this file the Unity CLI cannot discover or authenticate that live server.
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityPipelineDiscoveryGuard
    {
        private const string StartupTypeName = "Unity.Pipeline.Editor.PipelineServerStartup";
        private const string TokenTypeName = "Unity.Pipeline.Security.SecurityTokenManager";
        private const double RefreshIntervalSeconds = 2d;
        private static double _nextRefreshAt;

        [Serializable]
        private sealed class DescriptorPayload
        {
            public int pid;
            public int port;
            public string projectPath;
            public string projectName;
            public string unityVersion;
            public string mode;
            public string startedAt;
            public string lastHeartbeat;
            public string evalToken;
        }

        static UnityPipelineDiscoveryGuard()
        {
            EditorApplication.update -= Refresh;
            EditorApplication.update += Refresh;
            EditorApplication.delayCall += Refresh;
        }

        private static void Refresh()
        {
            if (EditorApplication.timeSinceStartup < _nextRefreshAt)
                return;
            _nextRefreshAt = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;

            try
            {
                Type startupType = FindLoadedType(StartupTypeName);
                Type tokenType = FindLoadedType(TokenTypeName);
                if (startupType == null || tokenType == null)
                    return;

                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                object server = startupType.GetProperty("Server", flags)?.GetValue(null);
                if (server == null)
                {
                    startupType.GetMethod("EnsureServerStarted", flags)?.Invoke(null, null);
                    server = startupType.GetProperty("Server", flags)?.GetValue(null);
                }
                if (server == null)
                    return;

                Type serverType = server.GetType();
                bool isRunning = (bool)(serverType.GetProperty("IsRunning")?.GetValue(server) ?? false);
                int port = (int)(serverType.GetProperty("Port")?.GetValue(server) ?? 0);
                if (!isRunning || port <= 0)
                    return;

                string token = tokenType.GetMethod("GetOrCreateToken", flags)?.Invoke(null, null) as string;
                if (string.IsNullOrEmpty(token))
                    return;

                string projectPath = Path.GetDirectoryName(Application.dataPath);
                if (string.IsNullOrEmpty(projectPath))
                    return;

                string now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                DescriptorPayload payload = new()
                {
                    pid = Process.GetCurrentProcess().Id,
                    port = port,
                    projectPath = projectPath,
                    projectName = Path.GetFileName(projectPath),
                    unityVersion = Application.unityVersion,
                    mode = Application.isBatchMode ? "batchmode" : "editor",
                    startedAt = now,
                    lastHeartbeat = now,
                    evalToken = token
                };

                string directory = Path.Combine(projectPath, "Library", "Pipeline");
                string descriptorPath = Path.Combine(directory, ".unity-pipeline-port");
                Directory.CreateDirectory(directory);
                File.WriteAllText(descriptorPath, JsonUtility.ToJson(payload, true));
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                Chmod(descriptorPath, Convert.ToUInt32("600", 8));
#endif
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"[UnityPipelineDiscoveryGuard] {exception.GetBaseException().Message}");
            }
        }

        private static Type FindLoadedType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int Chmod(string path, uint mode);
#endif
    }
}
