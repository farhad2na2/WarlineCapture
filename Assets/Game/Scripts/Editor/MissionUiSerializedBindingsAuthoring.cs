using System;
using System.Linq;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Author concrete prefab references once; gameplay never searches hierarchy strings.</summary>
    public static class MissionUiSerializedBindingsAuthoring
    {
        public static void RepairCommittedPrefabs()
        {
            int changed = 0;
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/UI" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if(!Apply(root)) continue;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[MissionUiBindings] result=Passed updated=" + changed);
        }

        public static bool Apply(GameObject root)
        {
            bool changed = false;
            foreach(var view in root.GetComponentsInChildren<MissionFieldGuideView>(true))
            {
                var data=new SerializedObject(view);
                Set(data,"extractionGuide",AssetDatabase.LoadAssetAtPath<Game.Configs.MissionFieldGuideConfig>(M04AirliftPresentationBuilder.GuidePath));
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            if(root.name == "SCN03_CommanderProfileContent")
            {
                var sections=root.GetComponent<UIShellContentSectionsView>();
                if(sections != null && sections.TryGetSection(UIShellContentSectionId.Middle,out var middle))
                {
                    var old=middle.GetComponentInChildren<CommanderProfileContentView>(true);
                    if(old != null && old.gameObject != middle)
                    {
                        var owner=middle.AddComponent<CommanderProfileContentView>();
                        owner.Configure(old.CommanderNameLabel,old.CommanderSubtitleLabel);
                        UnityEngine.Object.DestroyImmediate(old); changed=true;
                    }
                }
            }
            foreach(var view in root.GetComponentsInChildren<BuildPlacementConfirmationBarView>(true))
            {
                var data=new SerializedObject(view);
                Set(data,"confirmIconSprite",AssetDatabase.LoadAssetAtPath<Sprite>(V3UiFoundationBuilder.MatchSelectIconPath));
                Set(data,"infoIconSprite",AssetDatabase.LoadAssetAtPath<Sprite>(V3UiFoundationBuilder.MatchInfoIconPath));
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var view in root.GetComponentsInChildren<MatchHudSelectionPanelView>(true))
            {
                var data = new SerializedObject(view);
                var chip = data.FindProperty("passengerChipRoot").objectReferenceValue as GameObject;
                Set(data, "passengerChipIcon", chip != null ? chip.transform.Find("Icon") : null);
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var view in root.GetComponentsInChildren<MatchHudSquadTrayView>(true))
            {
                var data = new SerializedObject(view);
                var cards = data.FindProperty("cards");
                for(int i = 0; i < cards.arraySize; i++)
                {
                    var card = cards.GetArrayElementAtIndex(i);
                    var button = card.FindPropertyRelative("Button").objectReferenceValue as Button;
                    if(button == null) continue;
                    card.FindPropertyRelative("NameStrip").objectReferenceValue = button.transform.Find("NameStrip");
                    card.FindPropertyRelative("NameLabel").objectReferenceValue = button.transform.Find("NameStrip/Label")?.GetComponent<TMP_Text>();
                    card.FindPropertyRelative("DisabledWash").objectReferenceValue = button.transform.Find("MissionDisabledBlueWash")?.GetComponent<Image>();
                }
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var view in root.GetComponentsInChildren<NarrativePlaybackControlsView>(true))
            {
                var data = new SerializedObject(view);
                var pause = view.transform.Find("PauseButton")?.GetComponent<Button>();
                var subtitles = view.transform.Find("SubtitlesButton")?.GetComponent<Button>();
                var timeline = view.transform.parent?.Find("ComicTimeline");
                Set(data, "pauseButton", pause); Set(data, "subtitlesButton", subtitles);
                Set(data, "pauseLabel", pause?.GetComponentInChildren<TMP_Text>(true));
                Set(data, "subtitleLabel", subtitles?.GetComponentInChildren<TMP_Text>(true));
                Set(data, "stateLabel", timeline?.Find("State")?.GetComponent<TMP_Text>());
                Set(data, "pageLabel", timeline?.Find("Page")?.GetComponent<TMP_Text>());
                Set(data, "progressTrack", timeline?.Find("Track"));
                Set(data, "progressFill", timeline?.Find("Fill"));
                Set(data, "progressHandle", timeline?.Find("Handle"));
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var view in root.GetComponentsInChildren<BuildDrawerView>(true))
            {
                var data = new SerializedObject(view);
                var entries = data.FindProperty("availabilityVisuals");
                Transform content = view.ItemContentRoot;
                entries.arraySize = content != null ? content.childCount : 0;
                for(int i = 0; i < entries.arraySize; i++)
                {
                    var card = content.GetChild(i); var entry = entries.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("Root").objectReferenceValue = card;
                    entry.FindPropertyRelative("Overlay").objectReferenceValue = card.Find("DisabledOverlay")?.gameObject;
                    entry.FindPropertyRelative("Art").objectReferenceValue = card.Find("ArtClip/Thumb")?.GetComponent<Image>();
                }
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var view in root.GetComponentsInChildren<CampaignOperationsScreenView>(true))
            {
                var data = new SerializedObject(view);
                SetArray(data, "missionLockIcons", (view.MissionNodes ?? Array.Empty<RectTransform>()).Select(n => n != null ? n.Find("Lock")?.gameObject : null).ToArray());
                Transform briefing = view.MissionBriefing;
                var objectives = briefing != null ? Enumerable.Range(0, briefing.childCount).Select(briefing.GetChild)
                    .Where(t => t.name == "Objective").Select(t => t.Find("Label")?.GetComponent<TMP_Text>()).ToArray() : Array.Empty<TMP_Text>();
                SetArray(data, "objectiveCards", objectives);
                SetArray(data, "starGoalLabels", Enumerable.Range(0, 3).Select(i => briefing?.Find("Goal" + i + "/Copy")?.GetComponent<TMP_Text>()).ToArray());
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var view in root.GetComponentsInChildren<MatchOverlayCommandControlsView>(true))
            {
                var data = new SerializedObject(view);
                var buttons = new[] { view.SelectButton, view.MoveButton, view.AttackButton, view.HoldButton, view.StopButton, view.ScanButton, view.BoardButton, view.BuildButton };
                SetArray(data, "selectedStateButtons", buttons);
                SetArray(data, "selectedStateVisuals", buttons.Select(b => b != null ? b.transform.Find("V3SelectedState")?.gameObject : null).ToArray());
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var view in root.GetComponentsInChildren<MissionResultPopupView>(true))
            {
                var data = new SerializedObject(view);
                var stars = data.FindProperty("starRoots");
                var roots = Enumerable.Range(0, stars.arraySize).Select(i => stars.GetArrayElementAtIndex(i).objectReferenceValue as GameObject).ToArray();
                SetArray(data, "filledStars", roots.Select(s => s != null ? s.transform.Find("StarFilled")?.gameObject : null).ToArray());
                SetArray(data, "outlinedStars", roots.Select(s => s != null ? s.transform.Find("StarOutline")?.gameObject : null).ToArray());
                changed |= data.ApplyModifiedPropertiesWithoutUndo();
            }
            return changed;
        }

        private static void Set(SerializedObject data, string field, UnityEngine.Object value) => data.FindProperty(field).objectReferenceValue = value;
        private static void SetArray<T>(SerializedObject data, string field, T[] values) where T : UnityEngine.Object
        {
            var array = data.FindProperty(field); array.arraySize = values.Length;
            for(int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
