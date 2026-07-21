using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

namespace Game.Editor
{
    public static class OperationMapCurrentCompatibilitySceneStager
    {
        public const string SourceScenePath = "Assets/Game/Scenes/Match.unity";
        public const string DestinationFolderPath = "Assets/Game/Scenes/OperationMaps/Skirmish";
        public const string DestinationScenePath =
            DestinationFolderPath + "/opmap_skirmish_desert_base_01.unity";

        [MenuItem("Game/Operation Maps/Stage Current Compatibility Scene")]
        public static void Stage()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
                throw new FileNotFoundException("Canonical Match scene is missing.", SourceScenePath);

            EnsureDestinationFolder();
            SceneAsset destination = AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScenePath);
            bool created = destination == null;
            if (created && !AssetDatabase.CopyAsset(SourceScenePath, DestinationScenePath))
            {
                throw new InvalidOperationException(
                    $"AssetDatabase failed to stage '{SourceScenePath}' at '{DestinationScenePath}'.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);
            if (created && !string.Equals(
                    ComputeSha256(SourceScenePath),
                    ComputeSha256(DestinationScenePath),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The newly staged operation-map scene is not an exact serialized duplicate of Match.unity.");
            }
        }

        public static void StageForBatch() => Stage();

        public static bool TryValidate(out string error)
        {
            if (!AssetDatabase.IsValidFolder(DestinationFolderPath))
            {
                error = $"Staged operation-map folder is missing: '{DestinationFolderPath}'.";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScenePath) == null)
            {
                error = "Source and staged operation-map scenes must both exist.";
                return false;
            }

            string sourceGuid = AssetDatabase.AssetPathToGUID(SourceScenePath);
            string destinationGuid = AssetDatabase.AssetPathToGUID(DestinationScenePath);
            if (string.IsNullOrEmpty(sourceGuid) ||
                string.IsNullOrEmpty(destinationGuid) ||
                string.Equals(sourceGuid, destinationGuid, StringComparison.Ordinal))
            {
                error = "The staged operation-map scene requires a distinct, non-empty Unity GUID.";
                return false;
            }

            error = null;
            return true;
        }

        private static void EnsureDestinationFolder()
        {
            const string operationMapsFolder = "Assets/Game/Scenes/OperationMaps";
            if (!AssetDatabase.IsValidFolder(operationMapsFolder))
                AssetDatabase.CreateFolder("Assets/Game/Scenes", "OperationMaps");
            if (!AssetDatabase.IsValidFolder(DestinationFolderPath))
                AssetDatabase.CreateFolder(operationMapsFolder, "Skirmish");
        }

        private static string ComputeSha256(string assetPath)
        {
            using FileStream stream = File.OpenRead(Path.GetFullPath(assetPath));
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(stream);
            var builder = new StringBuilder(bytes.Length * 2);
            for (int index = 0; index < bytes.Length; index++)
                builder.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }
}
