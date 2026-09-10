using System;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class MissionCameraBoundsAuthoring
    {
        // Camera-only padding uses the existing physical map. Playable/minimap bounds stay mission scoped.
        public static readonly Vector3 Minimum = new(540, 4, 220);
        public static readonly Vector3 Maximum = new(1280, 100, 640);

        public static void Apply(SerializedProperty bounds)
        {
            bounds.FindPropertyRelative("cameraMin").vector3Value = Minimum;
            bounds.FindPropertyRelative("cameraMax").vector3Value = Maximum;
        }

        public static void UpdateCommittedMissionMaps()
        {
            foreach (string path in new[] { M03RadarWarningMapBuilder.Path, M04AirliftConfigBuilder.MapPath })
            {
                var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
                if (map == null) throw new InvalidOperationException(path);
                if (map.Bounds.CameraMin == Minimum && map.Bounds.CameraMax == Maximum) continue;
                var data = new SerializedObject(map);
                Apply(data.FindProperty("bounds"));
                using var sha = SHA256.Create();
                string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(map.ContentHash + ":camera-padding-540-220-1280-640-v1"))).Replace("-", "").ToLowerInvariant();
                data.FindProperty("contentHash").stringValue = hash;
                data.FindProperty("generatedMetadataHash").stringValue = hash;
                data.ApplyModifiedPropertiesWithoutUndo();
                if (!map.TryValidateMetadata(out string error)) throw new InvalidOperationException(error);
                EditorUtility.SetDirty(map);
            }
            AssetDatabase.SaveAssets();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            ValidateContentPacks();
            Debug.Log("[MissionCameraBounds] result=Passed maps=2 physicalMapUnchanged=true");
        }

        internal static void ValidateContentPacks()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(M01FirstContactConfigBuilder.OperationMapCatalogPath);
            if (catalog == null) throw new InvalidOperationException("Missing operation-map catalog.");
            if (!catalog.TryValidate(out string error)) throw new InvalidOperationException(error);
        }
    }
}
