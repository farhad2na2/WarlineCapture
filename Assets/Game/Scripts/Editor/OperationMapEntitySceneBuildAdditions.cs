using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes.Editor;
using UnityEditor;

namespace Game.Editor
{
    public sealed class OperationMapEntitySceneBuildAdditions : IEntitySceneBuildAdditions
    {
        private static string currentProcessSceneOverride;

        internal static IDisposable UseCurrentProcessSceneOverride(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                throw new ArgumentException("EntityScene build override path is required.", nameof(scenePath));
            if (!string.IsNullOrEmpty(currentProcessSceneOverride))
                throw new InvalidOperationException("An EntityScene build override is already active.");

            currentProcessSceneOverride = scenePath;
            return new SceneOverrideScope();
        }

        public HashSet<Hash128> RegisterAdditionalEntityScenesToBuild()
        {
            string scenePath = string.IsNullOrEmpty(currentProcessSceneOverride)
                ? OperationMapAddressablesLayoutBuilder.SourceSubScenePath
                : currentProcessSceneOverride;
            string guid = AssetDatabase.AssetPathToGUID(scenePath);
            var sceneGuid = new Hash128(guid);
            if (!sceneGuid.IsValid)
            {
                throw new InvalidOperationException(
                    $"The operation-map EntityScene build input must resolve to a valid asset GUID: {scenePath}");
            }

            return new HashSet<Hash128>
            {
                sceneGuid
            };
        }

        private sealed class SceneOverrideScope : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                currentProcessSceneOverride = null;
            }
        }
    }
}
