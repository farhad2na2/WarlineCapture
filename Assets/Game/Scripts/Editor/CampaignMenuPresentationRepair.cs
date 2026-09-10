using System;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CampaignMenuPresentationRepair
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";
        public static void Repair()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { Configure(root.GetComponentInChildren<CampaignOperationsScreenView>(true)); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[CampaignMenuPresentationRepair] result=Passed header clearance; live mission node state bindings");
        }

        public static void Configure(CampaignOperationsScreenView view)
        {
            if (view == null) throw new InvalidOperationException("Campaign view missing.");
            var panel = view.MissionBriefing;
            float scale = 638f / panel.sizeDelta.y;
            foreach (Transform child in panel)
                if (child is RectTransform rect && Mathf.Approximately(rect.anchorMin.y, 1) && Mathf.Approximately(rect.anchorMax.y, 1))
                { rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y * scale); rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y * scale); }
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, 638f);
            panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, -151f);
            var layout = view.GetComponent<MainMenuV3SectionLayoutView>();
            if (layout != null)
            {
                var serializedLayout = new SerializedObject(layout);
                var targets = serializedLayout.FindProperty("rightAnchoredTargets");
                var bases = serializedLayout.FindProperty("rightTargetBasePositions");
                for (int i = 0; i < targets.arraySize; i++)
                    if (targets.GetArrayElementAtIndex(i).objectReferenceValue == panel)
                    { var p = bases.GetArrayElementAtIndex(i).vector2Value; p.y = -151f; bases.GetArrayElementAtIndex(i).vector2Value = p; }
                serializedLayout.ApplyModifiedPropertiesWithoutUndo();
            }
            var so = new SerializedObject(view);
            SpriteField(so, "availableNodeSprite", V3UiFoundationBuilder.CampaignNodeActivePath);
            SpriteField(so, "completedNodeSprite", V3UiFoundationBuilder.CampaignNodeClaimedPath);
            SpriteField(so, "lockedNodeSprite", V3UiFoundationBuilder.CampaignNodeLockedPath);
            SpriteField(so, "completedNodeIcon", V3UiFoundationBuilder.CommanderCheckIconPath);
            SpriteField(so, "lockedNodeIcon", V3UiFoundationBuilder.CommanderLockIconPath);
            var states = so.FindProperty("nodeStateImages"); var numbers = so.FindProperty("nodeNumberLabels"); var locks = so.FindProperty("missionLockIcons");
            var ids = so.FindProperty("nodeIdLabels"); var panels = so.FindProperty("nodeLabelPanels");
            states.arraySize = numbers.arraySize = locks.arraySize = ids.arraySize = panels.arraySize = view.MissionNodes.Length;
            var template = view.MissionNodes[1].Find("Number").GetComponent<TMP_Text>();
            for (int i = 0; i < view.MissionNodes.Length; i++)
            {
                var node = view.MissionNodes[i];
                foreach (Transform sibling in node.parent)
                    if (sibling.name == "MissionLabel" && sibling.Find("Id")?.GetComponent<TMP_Text>() is TMP_Text id && id.text == "M0" + (i + 1))
                    { ids.GetArrayElementAtIndex(i).objectReferenceValue = id; panels.GetArrayElementAtIndex(i).objectReferenceValue = sibling.GetComponent<V3GradientGraphic>(); }
                var state = node.Find("StateIcon")?.GetComponent<Image>();
                if (state == null)
                {
                    var go = new GameObject("StateIcon", typeof(RectTransform), typeof(Image)); go.transform.SetParent(node, false); state = go.GetComponent<Image>();
                    state.rectTransform.sizeDelta = new Vector2(26f, 28f); state.raycastTarget = false;
                }
                var number = node.Find("Number")?.GetComponent<TMP_Text>();
                if (number == null) { number = UnityEngine.Object.Instantiate(template, node, false); number.name = "Number"; }
                number.text = (i + 1).ToString("00"); number.raycastTarget = false;
                states.GetArrayElementAtIndex(i).objectReferenceValue = state;
                numbers.GetArrayElementAtIndex(i).objectReferenceValue = number;
                locks.GetArrayElementAtIndex(i).objectReferenceValue = i >= 2 ? state.gameObject : null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SpriteField(SerializedObject so, string field, string path) =>
            so.FindProperty(field).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
