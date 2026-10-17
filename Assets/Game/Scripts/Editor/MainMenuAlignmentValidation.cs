using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Rebuilds the Main Menu, restores committed localization bindings a rebuild drops, and captures EN/FA home screens.</summary>
    public static class MainMenuAlignmentValidation
    {
        private const string Marker = "[MainMenuAlignment]";
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string ArtRoot = "Assets/Game/Art/UI";
        private static readonly string[] BindingFields =
            { "localizationKey", "englishFallback", "observeRuntimeSourceChanges", "minimumPresentationSize", "maximumPresentationSize" };

        public static void BuildAndCapture() => _ = BuildAndCaptureChecked();

        public static async Task<int> BuildAndCaptureChecked()
        {
            try
            {
                var before = Snapshot(out var committedBindings);
                var importSettings = Directory.GetFiles(ArtRoot, "*.png.meta", SearchOption.AllDirectories)
                    .ToDictionary(path => path, File.ReadAllText, StringComparer.Ordinal);
                MainMenuV3PrefabBuilder.Build();
                int restoredImports = RestoreImportSettings(importSettings);
                int restored = RestoreBindings(committedBindings);
                Debug.Log(Marker + " restoredImportSettings=" + restoredImports);
                var after = Snapshot(out _);
                var removed = before.Except(after).OrderBy(s => s, StringComparer.Ordinal).ToArray();
                var added = after.Except(before).OrderBy(s => s, StringComparer.Ordinal).ToArray();
                foreach (string entry in removed) Debug.Log(Marker + " removed " + entry);
                foreach (string entry in added) Debug.Log(Marker + " added " + entry);
                Debug.Log(Marker + " rebuild=Passed restoredBindings=" + restored + " removed=" + removed.Length + " added=" + added.Length);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError(Marker + " result=Failed");
                MissionEditorValidationExit.Complete(false);
                return 1;
            }
            return await MenuUiAuditCapture.RunMaterialReviewChecked();
        }

        private static HashSet<string> Snapshot(out Dictionary<string, Dictionary<string, string>> bindings)
        {
            var entries = new HashSet<string>(StringComparer.Ordinal);
            bindings = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    string path = PathOf(node, root.transform);
                    foreach (Component component in node.GetComponents<Component>())
                        if (component != null) entries.Add(path + ":" + component.GetType().Name);
                    var binding = node.GetComponent<V3LocalizedTextBindingView>();
                    if (binding == null) continue;
                    var data = new SerializedObject(binding);
                    bindings[path] = BindingFields.ToDictionary(field => field, field => Read(data.FindProperty(field)));
                    entries.Add(path + ":binding=" + bindings[path]["localizationKey"]);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return entries;
        }

        // The builder re-imports its textures with its own defaults, overwriting settings later builders committed.
        private static int RestoreImportSettings(Dictionary<string, string> committed)
        {
            int restored = 0;
            foreach (var meta in committed)
            {
                if (!File.Exists(meta.Key) || File.ReadAllText(meta.Key) == meta.Value) continue;
                File.WriteAllText(meta.Key, meta.Value);
                AssetDatabase.ImportAsset(meta.Key.Substring(0, meta.Key.Length - ".meta".Length), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                Debug.Log(Marker + " restored import settings " + meta.Key);
                restored++;
            }
            return restored;
        }

        private static int RestoreBindings(Dictionary<string, Dictionary<string, string>> committed)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            int restored = 0;
            try
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!committed.TryGetValue(PathOf(node, root.transform), out var fields) ||
                        node.GetComponent<V3LocalizedTextBindingView>() != null || node.GetComponent<TMPro.TMP_Text>() == null)
                        continue;
                    var data = new SerializedObject(node.gameObject.AddComponent<V3LocalizedTextBindingView>());
                    foreach (var field in fields) Write(data.FindProperty(field.Key), field.Value);
                    data.ApplyModifiedPropertiesWithoutUndo();
                    restored++;
                }
                if (restored != 0) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            return restored;
        }

        private static string PathOf(Transform node, Transform root)
        {
            var parts = new List<string>();
            for (Transform t = node; t != null && t != root; t = t.parent)
            {
                int index = 0;
                if (t.parent != null)
                    for (int i = 0; i < t.GetSiblingIndex(); i++)
                        if (t.parent.GetChild(i).name == t.name) index++;
                parts.Add(index == 0 ? t.name : t.name + "#" + index);
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Read(SerializedProperty property) => property == null ? null : property.propertyType switch
        {
            SerializedPropertyType.Boolean => property.boolValue ? "1" : "0",
            SerializedPropertyType.Float => property.floatValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            _ => property.stringValue
        };

        private static void Write(SerializedProperty property, string value)
        {
            if (property == null || value == null) return;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean: property.boolValue = value == "1"; break;
                case SerializedPropertyType.Float: property.floatValue = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                default: property.stringValue = value; break;
            }
        }
    }
}
