#if UNITY_EDITOR
using Game.UI.Runtime;
using Game.Configs;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class MatchHudV3PrefabBuilder
    {
        public static void BuildSelectionWheel()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplySelectionWheel(root);
                HudRightColumnLayoutBuilder.BindLayoutReferences(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("[SelectionWheelBuilder] result=Passed");
        }

        public static void CaptureSelectionWheel()
        {
            var localization = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            foreach (var language in new[]{GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode})
            {
                GameLocalization.Initialize(localization, language, persist: false);
                Capture("/private/tmp/warline-selection-wheel-"+language+".png", 1920, 1080, true, applyLocalization: true);
                Capture("/private/tmp/warline-selection-wheel-transport-"+language+".png", 2400, 1080, true, showTransportPassengers: true, applyLocalization: true);
            }
        }

        internal static void ApplySelectionWheel(GameObject root)
        {
            LoadAssets();
            ApplyMobileCommandLayout(root);
            ApplySquadPagination(root, FindDeepChild(root.transform, "SquadTray") as RectTransform);
            var controller = root.GetComponentInChildren<CommandWheelPanelView>(true);
            var selection = root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            if (controller == null || selection == null) return;
            var wheel = RequireRect(controller.transform, "Wheel");
            var overlay = wheel.parent;
            var card = FindDeepChild(overlay, "WheelUnitCard");
            if (card != null) Object.DestroyImmediate(card.gameObject);
            foreach (var wedge in wheel.GetComponentsInChildren<V3RadialWedgeGraphic>(true))
                Object.DestroyImmediate(wedge.gameObject);
            Button[] actions =
            {
                BuildWheelSector(wheel, "DestroySector", 2, 86, RedTop, RedBottom, theme.OrangeRed,
                    RequireSprite(V3UiFoundationBuilder.MatchDestroyIconPath), "DESTROY", true),
                BuildWheelSector(wheel, "ReturnSector", 92, 86, GreenTop, GreenBottom, theme.Green,
                    RequireSprite(V3UiFoundationBuilder.MatchReturnIconPath), "RETURN", true),
                BuildWheelSector(wheel, "CameraSector", 182, 86, RaisedTop, DarkBottom, theme.Cyan,
                    RequireSprite(V3UiFoundationBuilder.MatchCameraIconPath), "CAMERA", true),
                BuildWheelSector(wheel, "BoardSector", 272, 86, BlueTop, BlueBottom, theme.Blue,
                    RequireSprite(V3UiFoundationBuilder.MatchBoardIconPath), "BOARD", true)
            };
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                action.gameObject.AddComponent<CanvasGroup>();
                var label = action.GetComponentInChildren<TMP_Text>(true);
                label.fontSize = 22; label.fontSizeMin = 18; label.fontSizeMax = 22;
                var source = selection.ResolveWheelAction(i).GetComponentInChildren<V3LocalizedTextBindingView>(true);
                var binding = label.gameObject.AddComponent<V3LocalizedTextBindingView>();
                binding.Configure(source.LocalizationKey, source.EnglishFallback);
            }
            var center = RequireRect(wheel, "CenterDisc"); center.SetAsLastSibling();
            var portrait = RequireRect(center, "Portrait").GetComponent<Image>();
            var fitter = portrait.GetComponent<AspectRatioFitter>();
            if (fitter != null) Object.DestroyImmediate(fitter);
            Stretch(portrait.rectTransform); portrait.preserveAspect = true; portrait.sprite = null;
            var title = RequireRect(center, "Label").GetComponent<TMP_Text>();
            title.gameObject.SetActive(false);
            var titleBinding = title.GetComponent<V3LocalizedTextBindingView>();
            if (titleBinding != null) Object.DestroyImmediate(titleBinding);
            // No generic targeting instruction: these four actions execute immediately.
            var data = new SerializedObject(controller);
            foreach (string field in new[]{"wheelMoveButton","wheelAttackButton","moveWedge","attackWedge","unitCard"})
                data.FindProperty(field).objectReferenceValue = null;
            var instruction = data.FindProperty("instructionRoot").objectReferenceValue as GameObject;
            if (instruction != null) instruction.SetActive(false);
            data.FindProperty("instructionRoot").objectReferenceValue = null;
            data.FindProperty("selectionPortrait").objectReferenceValue = portrait;
            var array = data.FindProperty("selectionActions"); array.arraySize = actions.Length;
            for (int i = 0; i < actions.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = actions[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            var controls = root.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            data = new SerializedObject(controls);
            data.FindProperty("commandWheelStopButton").objectReferenceValue = BuildCeaseFireButton(root);
            data.ApplyModifiedPropertiesWithoutUndo();

            var frame = RequireRect(selection.transform, "PortraitFrame");
            var oldButton = frame.GetComponent<Button>();
            if (oldButton != null) Object.DestroyImmediate(oldButton);
            var cue = RequireRect(selection.transform, "CommandWheelCue");
            cue.SetParent(frame.parent, false);
            SetTopLeft(cue, 14, 252, 358, 72);
            cue.anchorMax = Vector2.one; cue.sizeDelta = new Vector2(-28, 72);
            var opener = cue.GetComponent<Button>() ?? cue.gameObject.AddComponent<Button>();
            EnsureGradient(cue, CyanTop, CyanBottom, theme.Cyan, 2, opener);
            var cueLabel = cue.GetComponentInChildren<TMP_Text>(true);
            cueLabel.raycastTarget = false;
            SetTopLeft(RequireRect(frame.parent, "HealthPanel"), 17, 336, 352, 31);
            SetTopLeft(RequireRect(frame.parent, "OrderLabel"), 17, 379, 352, 55);
            SetTopLeft(RequireRect(frame.parent, "V3PlayerControl"), 17, 439, 352, 40);
            RequireRect(frame.parent, "CommandButtons").gameObject.SetActive(false);
            data = new SerializedObject(selection);
            data.FindProperty("commandWheelOpenButton").objectReferenceValue = opener;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
