using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class MatchHudResourceHeaderPrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string MaterialsIconPath = CanonicalUiResourceIconPaths.Materials;
        private const string OilIconPath = CanonicalUiResourceIconPaths.Oil;
        private const string FuelIconPath = CanonicalUiResourceIconPaths.Fuel;
        private const string VisualProofPath = "/private/tmp/warline_matchhud_resource_header.png";

        [MenuItem("Game/UI/Rebuild Match HUD Resource Header")]
        public static void Build()
        {
            ConfigureOilIconImporter();
            AssetDatabase.Refresh();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform resourceStrip = FindDescendant(root.transform, "ResourceStrip");
                if (resourceStrip == null)
                    throw new InvalidOperationException($"{PrefabPath} is missing ResourceStrip.");

                RemoveDirectChild(resourceStrip, "CreditsSlot");

                Transform materialsSlot = FindDirectChild(resourceStrip, "MaterialsSlot");
                if (materialsSlot == null)
                {
                    materialsSlot = FindDirectChild(resourceStrip, "SupplySlot");
                    if (materialsSlot == null)
                        throw new InvalidOperationException("ResourceStrip is missing the Materials/Supply source slot.");

                    materialsSlot.name = "MaterialsSlot";
                }

                Transform fuelSlot = RequireDirectChild(resourceStrip, "FuelSlot");
                Transform oilSlot = FindDirectChild(resourceStrip, "OilSlot");
                if (oilSlot == null)
                {
                    oilSlot = UnityEngine.Object.Instantiate(fuelSlot.gameObject, resourceStrip).transform;
                    oilSlot.name = "OilSlot";
                }

                Transform civilianRiskSlot = RequireDirectChild(resourceStrip, "CivilianRiskSlot");
                Sprite materialsIcon = RequireSprite(MaterialsIconPath);
                Sprite oilIcon = RequireSprite(OilIconPath);
                Sprite fuelIcon = RequireSprite(FuelIconPath);

                ConfigureSlot(materialsSlot, "Materials", materialsIcon, -480f, 1);
                ConfigureSlot(oilSlot, "Oil", oilIcon, -160f, 2);
                ConfigureSlot(fuelSlot, "Fuel", fuelIcon, 160f, 3);
                ConfigureSlot(civilianRiskSlot, "Civilian Risk", null, 480f, 4);

                Transform frame = RequireDirectChild(resourceStrip, "Frame");
                frame.SetSiblingIndex(0);
                if (frame is RectTransform frameRect)
                    frameRect.sizeDelta = new Vector2(1280f, frameRect.sizeDelta.y);

                if (resourceStrip is RectTransform stripRect)
                    stripRect.sizeDelta = new Vector2(1280f, stripRect.sizeDelta.y);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
        }

        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing Match HUD prefab at {PrefabPath}.");

            Transform resourceStrip = FindDescendant(prefab.transform, "ResourceStrip");
            if (resourceStrip == null)
                throw new InvalidOperationException("Match HUD is missing ResourceStrip.");
            if (FindDirectChild(resourceStrip, "CreditsSlot") != null ||
                FindDirectChild(resourceStrip, "SupplySlot") != null)
            {
                throw new InvalidOperationException("Match HUD still contains legacy Credits/Supply resource slots.");
            }

            ValidateSlot(resourceStrip, "MaterialsSlot", "Materials", MaterialsIconPath, 1);
            ValidateSlot(resourceStrip, "OilSlot", "Oil", OilIconPath, 2);
            ValidateSlot(resourceStrip, "FuelSlot", "Fuel", FuelIconPath, 3);
            ValidateSlot(resourceStrip, "CivilianRiskSlot", "Civilian Risk", null, 4);
            Transform frame = RequireDirectChild(resourceStrip, "Frame");
            if (frame.GetSiblingIndex() != 0)
                throw new InvalidOperationException("ResourceStrip/Frame must render behind the four header sections.");
            Debug.Log("[MatchHudResourceHeaderPrefabBuilder] Validation passed.");
        }

        [MenuItem("Game/UI/Capture Match HUD Resource Header Proof")]
        public static void CaptureVisualProof()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new("MatchHudResourceHeaderProofCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.10f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 540f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasObject = new("MatchHudResourceHeaderProofCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(4800f, 2160f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate {PrefabPath} for visual proof.");

            instance.transform.SetParent(canvas.transform, false);
            if (instance.transform is RectTransform instanceRect)
            {
                instanceRect.anchorMin = Vector2.zero;
                instanceRect.anchorMax = Vector2.one;
                instanceRect.offsetMin = Vector2.zero;
                instanceRect.offsetMax = Vector2.zero;
                instanceRect.localScale = Vector3.one;
            }

            Canvas.ForceUpdateCanvases();
            CaptureCamera(camera, VisualProofPath);
            Debug.Log($"[MatchHudResourceHeaderPrefabBuilder] Visual proof saved to {VisualProofPath}.");
        }

        private static void CaptureCamera(Camera camera, string path)
        {
            const int width = 1920;
            const int height = 1080;
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                name = "MatchHudResourceHeaderProofTarget"
            };
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureSlot(
            Transform slot,
            string labelText,
            Sprite icon,
            float x,
            int siblingIndex)
        {
            slot.SetSiblingIndex(siblingIndex);
            if (slot is RectTransform rect)
            {
                rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
                rect.sizeDelta = new Vector2(300f, rect.sizeDelta.y);
            }

            TMP_Text label = RequireDirectChild(slot, "Label").GetComponent<TMP_Text>();
            if (label == null)
                throw new InvalidOperationException($"{slot.name}/Label is missing TMP_Text.");

            label.text = labelText;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Min(18f, label.fontSize);
            label.fontSizeMax = Mathf.Max(label.fontSizeMin, label.fontSize);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            if (icon == null)
                return;

            Transform iconTransform = RequireDirectChild(slot, "Icon");
            Image image = iconTransform.GetComponent<Image>();
            if (image == null)
                throw new InvalidOperationException($"{slot.name}/Icon is missing Image.");

            image.sprite = icon;
            image.preserveAspect = true;
        }

        private static void ValidateSlot(
            Transform resourceStrip,
            string slotName,
            string labelText,
            string iconPath,
            int siblingIndex)
        {
            Transform slot = RequireDirectChild(resourceStrip, slotName);
            if (slot.GetSiblingIndex() != siblingIndex)
                throw new InvalidOperationException($"{slotName} has the wrong display order.");

            TMP_Text label = RequireDirectChild(slot, "Label").GetComponent<TMP_Text>();
            if (label == null || label.text != labelText)
                throw new InvalidOperationException($"{slotName} has the wrong label.");

            if (string.IsNullOrEmpty(iconPath))
                return;

            Image image = RequireDirectChild(slot, "Icon").GetComponent<Image>();
            string actualPath = image != null && image.sprite != null
                ? AssetDatabase.GetAssetPath(image.sprite)
                : string.Empty;
            if (!string.Equals(actualPath, iconPath, StringComparison.Ordinal))
                throw new InvalidOperationException($"{slotName} uses '{actualPath}' instead of '{iconPath}'.");
        }

        private static void ConfigureOilIconImporter()
        {
            AssetDatabase.ImportAsset(OilIconPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(OilIconPath) is not TextureImporter importer)
                throw new InvalidOperationException($"Could not load texture importer for {OilIconPath}.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null
                ? sprite
                : throw new InvalidOperationException($"Missing sprite at {path}.");
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            return child != null
                ? child
                : throw new InvalidOperationException($"{parent.name} is missing direct child {name}.");
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDescendant(root.GetChild(i), name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static void RemoveDirectChild(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            if (child != null)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
}
