using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class OperationMapCurrentStagedDefinitionBuilder
    {
        public const string DefinitionPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Staged_DesertBase01.asset";

        private const string DefinitionName = "OperationMap_Staged_DesertBase01";
        private const string MetadataHashDomain = "warline.operation-map.staged-navigation.v1";

        [MenuItem("Game/Operation Maps/Build Current Staged Definition")]
        public static void Stage()
        {
            OperationMapCurrentCompatibilityPlacementStager.Stage();
            OperationMapCurrentCompatibilitySubSceneStager.Stage();

            OperationMapDefinition source = LoadRequired<OperationMapDefinition>(
                OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath);
            OperationMapDefinition destination = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                DefinitionPath);
            if (destination == null)
            {
                if (!AssetDatabase.CopyAsset(
                        OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath,
                        DefinitionPath))
                    throw new InvalidOperationException("Failed to create the staged operation-map definition.");
                destination = LoadRequired<OperationMapDefinition>(DefinitionPath);
            }

            EditorUtility.CopySerialized(source, destination);
            destination.name = DefinitionName;
            string stagedSubSceneGuid = AssetDatabase.AssetPathToGUID(
                OperationMapCurrentCompatibilitySubSceneStager.DestinationSubScenePath);
            OperationMapNavigationMetadataConfig navigation = source.NavigationMetadata;
            var serialized = new SerializedObject(destination);
            serialized.FindProperty("navigationMetadata")
                .FindPropertyRelative("authoredSubSceneGuid").stringValue = stagedSubSceneGuid;
            serialized.FindProperty("generatedMetadataHash").stringValue = ComputeMetadataHash(
                source.GeneratedMetadataHash,
                stagedSubSceneGuid,
                navigation.GridAuthoringLocalId,
                navigation.StaticGridBlockerCount);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(destination);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);
        }

        public static void StageForBatch() => Stage();

        public static bool TryValidate(out string error)
        {
            OperationMapDefinition source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath);
            OperationMapDefinition destination = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                DefinitionPath);
            if (source == null || destination == null)
            {
                error = "Compatibility and staged operation-map definitions must both exist.";
                return false;
            }

            string sourceGuid = AssetDatabase.AssetPathToGUID(
                OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath);
            string destinationGuid = AssetDatabase.AssetPathToGUID(DefinitionPath);
            string stagedSubSceneGuid = AssetDatabase.AssetPathToGUID(
                OperationMapCurrentCompatibilitySubSceneStager.DestinationSubScenePath);
            if (string.IsNullOrEmpty(sourceGuid) || string.IsNullOrEmpty(destinationGuid) ||
                string.Equals(sourceGuid, destinationGuid, StringComparison.Ordinal) ||
                !string.Equals(destination.name, DefinitionName, StringComparison.Ordinal) ||
                !string.Equals(source.OperationMapId, destination.OperationMapId, StringComparison.Ordinal) ||
                !string.Equals(source.SourceIdentityHash, destination.SourceIdentityHash, StringComparison.Ordinal) ||
                !string.Equals(source.ContentHash, destination.ContentHash, StringComparison.Ordinal) ||
                !string.Equals(
                    destination.NavigationMetadata.AuthoredSubSceneGuid,
                    stagedSubSceneGuid,
                    StringComparison.Ordinal) ||
                string.Equals(
                    source.NavigationMetadata.AuthoredSubSceneGuid,
                    destination.NavigationMetadata.AuthoredSubSceneGuid,
                    StringComparison.Ordinal))
            {
                error = "Staged operation-map definition identity or navigation binding drifted.";
                return false;
            }

            string expectedMetadataHash = ComputeMetadataHash(
                source.GeneratedMetadataHash,
                stagedSubSceneGuid,
                source.NavigationMetadata.GridAuthoringLocalId,
                source.NavigationMetadata.StaticGridBlockerCount);
            if (!string.Equals(destination.GeneratedMetadataHash, expectedMetadataHash, StringComparison.Ordinal))
            {
                error = "Staged operation-map metadata hash drifted.";
                return false;
            }

            return destination.TryValidateMetadata(out error);
        }

        private static string ComputeMetadataHash(
            string compatibilityMetadataHash,
            string subSceneGuid,
            long gridAuthoringLocalId,
            int staticGridBlockerCount)
        {
            string payload = string.Concat(
                MetadataHashDomain, "\n",
                compatibilityMetadataHash, "\n",
                subSceneGuid, "\n",
                gridAuthoringLocalId.ToString(CultureInfo.InvariantCulture), "\n",
                staticGridBlockerCount.ToString(CultureInfo.InvariantCulture));
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var result = new StringBuilder(hash.Length * 2);
            for (int index = 0; index < hash.Length; index++)
                result.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new InvalidOperationException($"Required asset is missing: '{path}'.");
        }
    }
}
