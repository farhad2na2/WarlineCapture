using System;
using System.Collections.Generic;
using Game.Configs;
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
            string scenePath = currentProcessSceneOverride;
            string guid;
            if (!string.IsNullOrEmpty(scenePath))
            {
                guid = AssetDatabase.AssetPathToGUID(scenePath);
            }
            else
            {
                OperationMapDefinition definition =
                    AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                        OperationMapAddressablesLayoutBuilder.DefinitionPath);
                bool usesEntityScene = definition != null &&
                    definition.PresentationKind == OperationMapPresentationKind.EntityScene;
                scenePath = usesEntityScene
                    ? AssetDatabase.GUIDToAssetPath(
                        definition.NavigationMetadata.AuthoredSubSceneGuid)
                    : OperationMapAddressablesLayoutBuilder.SourceSubScenePath;
                guid = usesEntityScene
                    ? definition.NavigationMetadata.AuthoredSubSceneGuid
                    : AssetDatabase.AssetPathToGUID(scenePath);
            }
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
