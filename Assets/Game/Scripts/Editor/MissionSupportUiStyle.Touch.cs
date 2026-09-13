using System.Linq;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    internal static partial class MissionSupportUiStyle
    {
        private static void MobileSelection(GameObject root)
        {
            MatchHudV3PrefabBuilder.ApplySelectionWheel(root);
            var chip=Find(root,"PassengerChip");Place(chip,17,636,352,72);
            Place(chip.Find("Label"),52,6,290,60);Place(chip.Find("Icon"),10,18,36,36);
            var aria=root.GetComponentInChildren<AriaTutorialBriefingView>(true);
            var map=(RectTransform)Find(root,"MinimapPanel");
            foreach(string name in new[]{"Map","MapOverlay"})
            {
                var rect=(RectTransform)map.Find(name);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
                rect.offsetMin=new Vector2(4,4);rect.offsetMax=new Vector2(-4,-4);
            }
            var header=Find(root,"HeaderContent");
            var layout=header.GetComponent<MissionHudTouchLayoutView>()??header.gameObject.AddComponent<MissionHudTouchLayoutView>();
            var data=new SerializedObject(layout);data.FindProperty("minimap").objectReferenceValue=map;
            var buttons=data.FindProperty("primaryActions");buttons.arraySize=2;
            buttons.GetArrayElementAtIndex(0).objectReferenceValue=(RectTransform)aria.DoItButton.transform;
            buttons.GetArrayElementAtIndex(1).objectReferenceValue=(RectTransform)aria.ShowMeButton.transform;
            data.ApplyModifiedPropertiesWithoutUndo();
            HudRightColumnLayoutBuilder.BindLayoutReferences(root);
            layout.RefreshLayout();
            data=new SerializedObject(header.GetComponent<MissionDefenseHudView>());
            data.FindProperty("touchLayout").objectReferenceValue=layout;data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
