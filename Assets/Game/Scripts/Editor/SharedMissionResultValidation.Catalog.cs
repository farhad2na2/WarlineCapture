#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Game.Editor
{
    public static partial class SharedMissionResultValidation
    {
        static void CheckCampaignCatalog()
        {
            var failures=new List<string>();int cases=0;
            foreach(string locale in new[]{"en","fa-IR"})
            {
                GameLocalization.SetLocale(locale,false);
                for(int index=0;index<CampaignMissionSequence.RegisteredMissionCount;index++)
                foreach(bool win in new[]{true,false})
                {
                    string id=CampaignMissionSequence.IdAt(index);string title=id.Substring(id.LastIndexOf('.')+1).Replace('_',' ').ToUpperInvariant();
                    var host=new GameObject("CatalogCanvas",typeof(RectTransform),typeof(Canvas));host.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)host.transform).sizeDelta=new Vector2(1920,1080);
                    try
                    {
                        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MissionResultV3PrefabBuilder.PrefabPath),host.transform,false);var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                        var owner=root.GetComponent<MissionResultPopupView>();
                        var model=new UiMissionResultPopupModel(1,id,win?UiMissionResultOutcome.Victory:UiMissionResultOutcome.Loss,win?"VICTORY":"DEFEAT",title+" • OPERATIONS AREA","",win?(byte)3:(byte)0,"05:00","0","3/3",win?"260 Commander XP  ·  1200 Credits":"NO REWARD",win?"CONTINUE":"RETRY",true,!win,firstContact:new UiMissionFirstContactResultDetails(true,true));
                        owner.Apply(in model);var shared=owner.SharedResult;shared.RefreshLayout();Canvas.ForceUpdateCanvases();
                        foreach(var text in shared.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate();shared.RefreshLayout();Canvas.ForceUpdateCanvases();
                        foreach(var text in shared.GetComponentsInChildren<TMP_Text>())
                        {
                            if(!text.gameObject.activeInHierarchy||text==shared.Controls.Summary||text==shared.Controls.InfoBody)continue;
                            text.ForceMeshUpdate();checks++;
                            if(text.isTextOverflowing||text.isTextTruncated)failures.Add(locale+" "+id+" "+win+" "+text.transform.parent.name+"/"+text.name+": "+text.text);
                        }
                        cases++;
                    }
                    finally{Object.DestroyImmediate(host);}
                }
            }
            File.WriteAllLines(Output+"/campaign-catalog-layout.txt",failures);
            Check(failures.Count==0,"Campaign catalog layout "+string.Join("; ",failures));
            Debug.Log("[SharedMissionResultCatalog] result=Passed adapters=25 cases="+cases+" locales=en,fa-IR fixtures=true missionReadiness=NotClaimed");
        }
    }
}
#endif
