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
            // The entire portrait is already the command button; make its visible cue equally clear.
            var cue=Find(root,"CommandWheelCue");Place(cue,5,94,348,64);
            var cueRect=(RectTransform)cue;cueRect.anchorMax=Vector2.one;cueRect.sizeDelta=new Vector2(-10,64);
            var label=cue.GetComponentInChildren<TMP_Text>(true);label.fontSize=22;label.fontSizeMax=22;label.fontSizeMin=18;
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
            data=new SerializedObject(header.GetComponent<MissionDefenseHudView>());
            data.FindProperty("touchLayout").objectReferenceValue=layout;data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
