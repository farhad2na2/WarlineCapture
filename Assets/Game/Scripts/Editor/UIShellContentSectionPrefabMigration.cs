using Game.UI.Runtime;

namespace Game.Editor
{
    #if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public static class UIShellContentSectionPrefabMigration
    {
        private const string MainMenuContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string MatchHudContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string ArmoryContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab";

        private static readonly SectionBinding[] MainMenuSections =
        {
            new(UIShellContentSectionId.MenuBackground, "MenuBackgroundContent"),
            new(UIShellContentSectionId.Header, "HeaderContent"),
            new(UIShellContentSectionId.Left, "LeftContent"),
            new(UIShellContentSectionId.Middle, "MiddleContent"),
            new(UIShellContentSectionId.Right, "RightContent"),
            new(UIShellContentSectionId.Footer, "FooterContent")
        };

        private static readonly SectionBinding[] MatchHudSections =
        {
            new(UIShellContentSectionId.Header, "HeaderContent"),
            new(UIShellContentSectionId.Left, "LeftContent"),
            new(UIShellContentSectionId.Right, "RightContent"),
            new(UIShellContentSectionId.Footer, "FooterContent")
        };

        private static readonly SectionBinding[] ArmorySections =
        {
            new(UIShellContentSectionId.Left, "LeftContent"),
            new(UIShellContentSectionId.Middle, "MiddleContent"),
            new(UIShellContentSectionId.Right, "RightContent"),
            new(UIShellContentSectionId.Footer, "FooterContent")
        };

        [MenuItem("Game/UI/Populate Shell Content Sections")]
        public static void PopulateAll()
        {
            Populate(MainMenuContentPath, MainMenuSections);
            Populate(MatchHudContentPath, MatchHudSections);
            Populate(ArmoryContentPath, ArmorySections);
            AssetDatabase.SaveAssets();
        }

        private static void Populate(string prefabPath, IReadOnlyList<SectionBinding> bindings)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                UIShellContentSectionsView view = prefabRoot.GetComponent<UIShellContentSectionsView>();
                if (view == null)
                    view = prefabRoot.AddComponent<UIShellContentSectionsView>();

                var sections = new List<UIShellContentSectionsView.SectionReference>(bindings.Count);
                for (int i = 0; i < bindings.Count; i++)
                {
                    SectionBinding binding = bindings[i];
                    Transform child = FindDirectChild(prefabRoot.transform, binding.ChildName);
                    if (child == null)
                    {
                        Debug.LogWarning($"[UIShellContentSectionMigration] Missing section '{binding.ChildName}' in {prefabPath}");
                        continue;
                    }

                    sections.Add(new UIShellContentSectionsView.SectionReference(binding.SectionId, child.gameObject));
                }

                view.ConfigureSections(sections.ToArray());
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != null && child.name == childName)
                    return child;
            }

            return null;
        }

        private readonly struct SectionBinding
        {
            public SectionBinding(UIShellContentSectionId sectionId, string childName)
            {
                SectionId = sectionId;
                ChildName = childName;
            }

            public UIShellContentSectionId SectionId { get; }
            public string ChildName { get; }
        }
    }
    #endif
}
