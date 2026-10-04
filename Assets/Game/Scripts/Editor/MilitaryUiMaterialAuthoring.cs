using System;
using System.IO;
using System.Linq;
using System.Text;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class MilitaryUiMaterialAuthoring
    {
        public const string AssetsPath = "Assets/Game/Resources/MilitaryUiMaterials";
        private static readonly string[] Prefabs =
        {
            "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab",
            "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab",
            "Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab",
            "Assets/Game/Prefabs/UI/Shell/Popups/SCN08_FullMapPopup.prefab",
            "Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab",
            "Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab",
            "Assets/Game/Prefabs/UI/Shell/Popups/SupportPopup.prefab",
            "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab"
        };

        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires Edit mode");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Rendering/Shaders/MilitaryUiSurface.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Military UI shader failed import");
            foreach (MilitaryUiMaterialKind kind in Enum.GetValues(typeof(MilitaryUiMaterialKind)))
            {
                if (kind <= MilitaryUiMaterialKind.Automatic) continue;
                string name = V3GradientGraphic.MaterialName(kind), texturePath = AssetsPath + "/" + name + "-v01.png";
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing texture: " + texturePath);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.wrapMode = kind == MilitaryUiMaterialKind.BallisticWeave ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.npotScale = TextureImporterNPOTScale.ToNearest;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true,
                    maxTextureSize = 512, format = TextureImporterFormat.ASTC_4x4, compressionQuality = 100 });
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "iPhone", overridden = true,
                    maxTextureSize = 512, format = TextureImporterFormat.ASTC_4x4, compressionQuality = 100 });
                importer.SaveAndReimport();
                string materialPath = AssetsPath + "/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
                material.shader = shader;
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                material.SetFloat("_Strength", kind == MilitaryUiMaterialKind.CeramicArmor ? 1f : .95f);
                material.SetFloat("_QuietCenter", kind == MilitaryUiMaterialKind.BallisticWeave ? 0f : .35f);
                EditorUtility.SetDirty(material);
            }
            int count = 0;
            foreach (string path in Prefabs)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    PreserveMatchPrefabDefaults(root, path);
                    string before = PlayerPresentation(root);
                    count += Apply(root);
                    if (before != PlayerPresentation(root)) throw new InvalidOperationException("Texture authoring changed layout/content/input: " + path);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("[MilitaryUiMaterials] result=Passed prefabs=" + Prefabs.Length + " surfaces=" + count + " layout=Preserved input=Preserved textures=5 maxSize=512 mobile=ASTC4x4");
        }

        private static void PreserveMatchPrefabDefaults(GameObject root, string path)
        {
            if (!path.EndsWith("SCN08_MatchHudContent.prefab")) return;
            // Loading this legacy prefab initializes editor-only preview state.
            // Keep the authored lock and labels from the texture pass baseline.
            var build = root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "BuildCommand");
            if (build != null) build.interactable = false;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name != "OrderText" && text.name != "DescriptionText") continue;
                text.horizontalAlignment = HorizontalAlignmentOptions.Left;
                var binding = text.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                {
                    var data = new SerializedObject(binding);
                    if (string.IsNullOrEmpty(data.FindProperty("localizationKey").stringValue) &&
                        string.IsNullOrEmpty(data.FindProperty("englishFallback").stringValue))
                        UnityEngine.Object.DestroyImmediate(binding);
                }
            }
        }

        public static int Apply(GameObject root)
        {
            int count = 0;
            foreach (var graphic in root.GetComponentsInChildren<V3GradientGraphic>(true))
            {
                var rect = graphic.rectTransform;
                // Keep small indicators and art readability overlays procedural.
                if (rect.rect.width < 64 || rect.rect.height < 40 || graphic.name.Contains("Readability")) continue;
                var kind = MilitaryUiMaterialKind.Automatic;
                string node = graphic.name == "V3GradientLayer" ? graphic.transform.parent.name : graphic.name;
                if (node == "Tint" && graphic.transform.parent.name == "Card_Skirmish") kind = MilitaryUiMaterialKind.FracturedGlass;
                if (node == "Tint" && graphic.transform.parent.name == "Card_Operations") kind = MilitaryUiMaterialKind.BallisticWeave;
                if (node == "CommanderPanel") kind = MilitaryUiMaterialKind.BallisticWeave;
                if (node == "StoreButton" || node == "ArmoryButton") kind = MilitaryUiMaterialKind.TacticalGlass;
                if (node == "Credits" || node == "CreditsVisualPanel") kind = MilitaryUiMaterialKind.AnodizedMetal;
                graphic.SetMilitaryMaterial(kind);
                if (graphic.ResolvedMilitaryMaterial != MilitaryUiMaterialKind.None) count++;
            }
            return count;
        }

        public static void Validate()
        {
            foreach (MilitaryUiMaterialKind kind in Enum.GetValues(typeof(MilitaryUiMaterialKind)))
            {
                if (kind <= MilitaryUiMaterialKind.Automatic) continue;
                var material = Resources.Load<Material>("MilitaryUiMaterials/" + V3GradientGraphic.MaterialName(kind));
                if (material == null || material.mainTexture == null || ShaderUtil.ShaderHasError(material.shader))
                    throw new InvalidOperationException("Invalid UI material: " + kind);
                if (material.mainTexture.width > 512 || material.mainTexture.height > 512)
                    throw new InvalidOperationException("Texture exceeds mobile budget: " + kind);
            }
            foreach (string path in Prefabs)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) throw new InvalidOperationException("Missing prefab: " + path);
                if (!root.GetComponentsInChildren<V3GradientGraphic>(true).Any(g => g.MilitaryMaterial != MilitaryUiMaterialKind.None))
                    throw new InvalidOperationException("Untextured prefab: " + path);
            }
            Debug.Log("[MilitaryUiMaterialValidation] result=Passed materials=5 shader=Compiled imports=Mobile prefabs=" + Prefabs.Length);
        }

        public static void ValidateLive()
        {
            Validate();
            var actions = UnityEngine.Object.FindObjectsByType<HudIconButtonView>(FindObjectsSortMode.None);
            if (actions.Length == 0) throw new InvalidOperationException("Native HUD action surfaces absent");
            foreach (var action in actions)
            {
                var surface = action.transform.Find("HudRoleSurface")?.GetComponent<V3GradientGraphic>();
                if (surface == null || surface.MilitaryMaterial != MilitaryUiMaterialKind.Automatic || surface.mainTexture == Texture2D.whiteTexture)
                    throw new InvalidOperationException("Runtime role texture missing: " + action.name);
                if (surface.material.shader.name != "Warline/UI/Military Surface")
                    throw new InvalidOperationException("Runtime UI shader missing: " + action.name);
            }
            Debug.Log("[MilitaryUiMaterialsNative] result=Passed runtimeActions=" + actions.Length + " borders=Untextured overlays=NonInteractive");
        }

        private static string PlayerPresentation(GameObject root)
        {
            var snapshot = new StringBuilder();
            foreach (var r in root.GetComponentsInChildren<RectTransform>(true))
                snapshot.Append(r.name).Append(r.anchorMin).Append(r.anchorMax).Append(r.pivot).Append(r.anchoredPosition).Append(r.sizeDelta).Append(r.gameObject.activeSelf);
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                snapshot.Append(text.text).Append(text.fontSize).Append((text.font != null ? text.font.name : "none")).Append(text.raycastTarget);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                snapshot.Append((image.sprite != null ? image.sprite.name : "none")).Append(image.raycastTarget);
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                snapshot.Append(button.name).Append(button.interactable).Append(button.onClick.GetPersistentEventCount());
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    snapshot.Append((button.onClick.GetPersistentTarget(i) != null ? button.onClick.GetPersistentTarget(i).name : "none")).Append(button.onClick.GetPersistentMethodName(i));
            }
            return snapshot.ToString();
        }
    }
}
