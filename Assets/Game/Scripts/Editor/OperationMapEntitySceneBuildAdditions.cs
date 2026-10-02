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

            var scenes = new HashSet<Hash128> { sceneGuid };
            // Normal player builds must ship generated campaign EntityScenes even
            // when their renderer-free binding uses an unbound SubScene placeholder.
            // Explicit candidate overrides retain their deliberately isolated content set.
            if (string.IsNullOrEmpty(currentProcessSceneOverride))
            {
                var grounded = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    CH04M04GroundedSignalConfigBuilder.MapPath);
                if (grounded != null)
                {
                    string groundedError = "Logical source binding is not permitted for this independent physical map.";
                    if (grounded.SourceBinding.IsConfigured ||
                        !grounded.TryValidateMetadata(out groundedError) ||
                        !grounded.TryValidateLocalContentReferences(out groundedError))
                        throw new InvalidOperationException("Grounded Signal production entity delivery requires a valid independent map: " + groundedError);
                    string mapPath = CH04M04GroundedSignalConfigBuilder.EntityScenePath;
                    string mapGuid = AssetDatabase.AssetPathToGUID(mapPath);
                    if (!new Hash128(mapGuid).IsValid ||
                        !string.Equals(mapGuid, grounded.NavigationMetadata.AuthoredSubSceneGuid, StringComparison.Ordinal))
                        throw new InvalidOperationException("Grounded Signal production map EntityScene GUID does not match its definition.");
                    string fixtureGuid = AssetDatabase.AssetPathToGUID(CH04M04GroundedSignalContentBuilder.FixturePath);
                    if (!new Hash128(fixtureGuid).IsValid)
                        throw new InvalidOperationException("Grounded Signal production unit fixture missing; run CH04M04GroundedSignalContentBuilder.BuildPackedContent before building a player.");
                    scenes.Add(new Hash128(mapGuid));
                    scenes.Add(new Hash128(fixtureGuid));
                }
                var armor = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    CH04M05ArmorBreakConfigBuilder.MapPath);
                if (armor != null)
                {
                    string armorError = "Logical source binding is not permitted for this independent physical map.";
                    if (armor.SourceBinding.IsConfigured ||
                        !armor.TryValidateMetadata(out armorError) ||
                        !armor.TryValidateLocalContentReferences(out armorError))
                        throw new InvalidOperationException("Armor Break production entity delivery requires a valid independent map: " + armorError);
                    string mapPath = CH04M05ArmorBreakConfigBuilder.EntityScenePath;
                    string mapGuid = AssetDatabase.AssetPathToGUID(mapPath);
                    if (!new Hash128(mapGuid).IsValid ||
                        !string.Equals(mapGuid, armor.NavigationMetadata.AuthoredSubSceneGuid, StringComparison.Ordinal))
                        throw new InvalidOperationException("Armor Break production map EntityScene GUID does not match its definition.");
                    string fixtureGuid = AssetDatabase.AssetPathToGUID(CH04M05ArmorBreakContentBuilder.FixturePath);
                    if (!new Hash128(fixtureGuid).IsValid)
                        throw new InvalidOperationException("Armor Break production unit fixture missing; run CH04M05ArmorBreakContentBuilder.BuildPackedContent before building a player.");
                    scenes.Add(new Hash128(mapGuid));
                    scenes.Add(new Hash128(fixtureGuid));
                }
            }
            return scenes;
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
