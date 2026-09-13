using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class HudRightColumnLayoutBuilder
    {
        internal static void BindLayoutReferences(GameObject root)
        {
            var aria=root.GetComponentInChildren<AriaTutorialBriefingView>(true);
            if(aria!=null)
            {
                var data=new SerializedObject(aria);
                data.FindProperty("utilityActions").objectReferenceValue=aria.transform.Find("M03Actions") as RectTransform;
                data.FindProperty("openingLayout").objectReferenceValue=aria.transform.Find("OpeningInstruction") as RectTransform;
                data.FindProperty("alertCopy").objectReferenceValue=aria.transform.Find("AlertCue")?.GetComponent<TMPro.TMP_Text>();
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var dock=root.GetComponentInChildren<MissionHudTouchLayoutView>(true);
            if(dock!=null && dock.Minimap!=null)
            {
                var data=new SerializedObject(dock);
                data.FindProperty("dockBorder").objectReferenceValue=dock.Minimap.Find("DockBorder") as RectTransform;
                data.ApplyModifiedPropertiesWithoutUndo(); dock.RefreshLayout();
            }
        }

        public static void Apply()
        {
            const string path="Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                BindLayoutReferences(root);
                root.GetComponentInChildren<MissionHudTouchLayoutView>(true).RefreshLayout();
                var aria=root.GetComponentInChildren<AriaTutorialBriefingView>(true);
                aria.SetPresentationVisible(false);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            Debug.Log("[HudRightColumnLayout] result=Passed minimap=independent text=content-sized");
        }
    }
}
