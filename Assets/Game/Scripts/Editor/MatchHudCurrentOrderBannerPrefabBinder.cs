using Game.UI.Runtime;

namespace Game.Editor
{
    #if UNITY_EDITOR
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    public static class MatchHudCurrentOrderBannerPrefabBinder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string ChevronsSpritePath = "Assets/Game/Art/UI/Icons/scn08_current_order_chevrons.png";

        public static void BindCurrentOrderBanner()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform header = Require(prefab.transform, "HeaderContent");
                Transform banner = Require(header, "CurrentOrderBanner");
                Image chevrons = EnsureChevrons(banner);
                MatchHudCurrentOrderBannerView bannerView = header.GetComponent<MatchHudCurrentOrderBannerView>() ??
                                                            header.gameObject.AddComponent<MatchHudCurrentOrderBannerView>();
                SerializedObject bannerViewObject = new(bannerView);
                SetObject(bannerViewObject, "bannerRoot", banner.gameObject);
                SetObject(bannerViewObject, "chevrons", chevrons.gameObject);
                SetObject(bannerViewObject, "icon", RequireComponent<Image>(banner, "Icon"));
                SetObject(bannerViewObject, "orderText", RequireComponent<TMP_Text>(banner, "OrderText"));
                SetObject(bannerViewObject, "descriptionText", RequireComponent<TMP_Text>(banner, "DescriptionText"));
                bannerViewObject.ApplyModifiedPropertiesWithoutUndo();
                banner.gameObject.SetActive(false);

                MatchOverlayCommandControlsView commandControls = prefab.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
                if (commandControls == null)
                    throw new MissingReferenceException("SCN08_MatchHudContent prefab is missing MatchOverlayCommandControlsView.");

                SerializedObject commandControlsObject = new(commandControls);
                SetObject(commandControlsObject, "selectIcon", RequireCommandIcon(commandControls.SelectButton, "SelectCommand"));
                SetObject(commandControlsObject, "moveIcon", RequireCommandIcon(commandControls.MoveButton, "MoveCommand"));
                SetObject(commandControlsObject, "attackIcon", RequireCommandIcon(commandControls.AttackButton, "AttackCommand"));
                SetObject(commandControlsObject, "scanIcon", RequireCommandIcon(commandControls.ScanButton, "ScanCommand"));
                SetObject(commandControlsObject, "boardIcon", RequireCommandIcon(commandControls.BoardButton, "BoardButton"));
                SetObject(commandControlsObject, "buildIcon", RequireCommandIcon(commandControls.BuildButton, "BuildCommand"));
                SetObject(commandControlsObject, "holdIcon", RequireCommandIcon(commandControls.HoldButton, "HoldCommand"));
                SetObject(commandControlsObject, "stopIcon", RequireCommandIcon(commandControls.StopButton, "StopCommand"));
                commandControlsObject.ApplyModifiedPropertiesWithoutUndo();

                BattleHudRuntimeFeedbackView runtimeFeedback = prefab.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
                if (runtimeFeedback == null)
                    throw new MissingReferenceException("SCN08_MatchHudContent prefab is missing BattleHudRuntimeFeedbackView.");

                SerializedObject runtimeFeedbackObject = new(runtimeFeedback);
                SetObject(runtimeFeedbackObject, "currentOrderBanner", bannerView);
                SetObject(runtimeFeedbackObject, "commandIconSource", commandControls);
                runtimeFeedbackObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                Debug.Log("[MatchHudCurrentOrderBannerPrefabBinder] result=Bound prefab=SCN08_MatchHudContent");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static Transform Require(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child == null)
                throw new MissingReferenceException($"{parent.name} is missing child {childName}.");
            return child;
        }

        private static T RequireComponent<T>(Transform parent, string childName) where T : Component
        {
            Transform child = Require(parent, childName);
            T component = child.GetComponent<T>();
            if (component == null)
                throw new MissingReferenceException($"{parent.name}/{childName} is missing {typeof(T).Name}.");
            return component;
        }

        private static Image EnsureChevrons(Transform banner)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChevronsSpritePath);
            if (sprite == null)
                throw new MissingReferenceException($"Missing current order chevrons sprite at {ChevronsSpritePath}.");

            Transform child = banner.Find("Chevrons");
            if (child == null)
            {
                GameObject childObject = new("Chevrons", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                childObject.layer = banner.gameObject.layer;
                child = childObject.transform;
                child.SetParent(banner, false);
            }

            RectTransform rect = (RectTransform)child;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-316f, 0f);
            rect.sizeDelta = new Vector2(52f, 42f);
            child.SetSiblingIndex(Mathf.Min(1, banner.childCount - 1));

            Image image = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            child.gameObject.SetActive(false);
            return image;
        }

        private static Image RequireCommandIcon(Button button, string commandName)
        {
            if (button == null)
                throw new MissingReferenceException($"{commandName} button reference is missing.");

            foreach (Image image in button.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == "Icon" && image.sprite != null)
                    return image;
            }

            throw new MissingReferenceException($"{commandName} is missing a serialized Icon Image with a sprite.");
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new MissingReferenceException($"{serializedObject.targetObject.GetType().Name} is missing serialized field {propertyName}.");

            property.objectReferenceValue = value;
        }
    }
    #endif
}
