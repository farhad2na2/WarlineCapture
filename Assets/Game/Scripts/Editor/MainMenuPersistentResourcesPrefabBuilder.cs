using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class MainMenuPersistentResourcesPrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string CreditsIconPath = CanonicalUiResourceIconPaths.Credits;
        private const string CommandIconPath = CanonicalUiResourceIconPaths.Command;

        [MenuItem("Game/UI/Rebuild Main Menu Persistent Resources")]
        public static void Rebuild()
        {
            ConfigureIconImporter(CreditsIconPath);
            ConfigureIconImporter(CommandIconPath);
            AssetDatabase.Refresh();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform resourceArea = Require(root.transform, "HeaderContent/HeaderResourceArea");
                Transform creditsPanel = Require(resourceArea, "CreditsPanel");
                Transform commandPanel = Require(resourceArea, "CommandPanel");
                Transform suppliesPanel = resourceArea.Find("SuppliesPanel");

                if (suppliesPanel != null)
                    UnityEngine.Object.DestroyImmediate(suppliesPanel.gameObject);

                RectTransform resourceRect = RequireComponent<RectTransform>(resourceArea);
                resourceRect.sizeDelta = new Vector2(1380f, 160f);

                ConfigurePanel(creditsPanel, -350f, "CREDITS", "187,540", CreditsIconPath);
                ConfigurePanel(commandPanel, 350f, "COMMAND", "2,715", CommandIconPath);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[MainMenuPersistentResources] Rebuilt Credits and Command header panels.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureIconImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                throw new InvalidOperationException($"Could not load texture importer for {path}.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static void ConfigurePanel(
            Transform panel,
            float anchoredX,
            string label,
            string fallbackValue,
            string iconPath)
        {
            RectTransform panelRect = RequireComponent<RectTransform>(panel);
            panelRect.anchoredPosition = new Vector2(anchoredX, 0f);

            Transform frame = Require(panel, "Frame");
            TMP_Text labelText = RequireComponent<TMP_Text>(Require(frame, "Label"));
            TMP_Text valueText = RequireComponent<TMP_Text>(Require(frame, "Value"));
            Image icon = RequireComponent<Image>(Require(frame, "Icon"));
            Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (iconSprite == null)
                throw new InvalidOperationException($"Missing resource icon: {iconPath}");

            labelText.gameObject.SetActive(true);
            labelText.text = label;
            labelText.fontSize = 26f;
            labelText.alignment = TextAlignmentOptions.BottomLeft;
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchoredPosition = new Vector2(150f, -20f);
            labelRect.sizeDelta = new Vector2(330f, 34f);

            valueText.text = fallbackValue;
            valueText.fontSize = 54f;
            valueText.alignment = TextAlignmentOptions.TopLeft;
            RectTransform valueRect = valueText.rectTransform;
            valueRect.anchoredPosition = new Vector2(150f, -58f);
            valueRect.sizeDelta = new Vector2(330f, 76f);

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchoredPosition = new Vector2(-235f, 0f);
            iconRect.sizeDelta = new Vector2(112f, 112f);
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
        }

        private static Transform Require(Transform root, string path)
        {
            Transform result = root != null ? root.Find(path) : null;
            if (result == null)
                throw new InvalidOperationException($"Missing prefab object '{path}'.");
            return result;
        }

        private static T RequireComponent<T>(Transform transform) where T : Component
        {
            T component = transform != null ? transform.GetComponent<T>() : null;
            if (component == null)
                throw new InvalidOperationException($"Missing {typeof(T).Name} on '{transform?.name ?? "<null>"}'.");
            return component;
        }
    }
}
