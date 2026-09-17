#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class MatchHudV3PrefabBuilder
    {
        public static void RepairM03Clarity()
        {
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(t.name is "ReturnWarningCamera" or "SkipCameraTour") t.gameObject.SetActive(false);
                var threat=RequireRect(root.transform,"ThreatJumpPanel");
                threat.sizeDelta=new Vector2(threat.sizeDelta.x,120);
                var text=FindDeepChild(threat,"Title").GetComponent<TMP_Text>();
                text.enableAutoSizing=false; text.fontSize=26; text.fontSizeMin=text.fontSizeMax=26;
                SetTopLeft(text.rectTransform,70,6,360,108);
                SetTopLeft(RequireRect(threat,"WarningIcon"),12,36,47,47);
                var jump=RequireRect(threat,"V3ThreatJump"); SetTopLeft(jump,441,24,72,72);
                SetTopLeft(RequireRect(jump,"Icon"),17,17,38,38);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath,out bool saved);
                if(!saved) throw new InvalidOperationException("M3 clarity prefab save failed.");
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
            M03RadarWarningLocalizationBuilder.Import(); AssetDatabase.SaveAssets();
            Debug.Log("[M03ClarityRepair] result=Passed warning=26px countdown=34px returnCamera=hidden");
        }
    }
}
#endif
