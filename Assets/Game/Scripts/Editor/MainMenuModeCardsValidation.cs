using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Missions.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class MainMenuModeCardsValidation
    {
        public const string Root="Design/AgentReports/MainMenuNextMission20261021";
        private static int checks,captures;
        public static void Run()=>MissionEditorValidationExit.Complete(Validate());
        public static bool Validate()
        {
            bool passed=false;string locale=GameLocalization.CurrentLocaleCode;
            try
            {
                checks=0;captures=0;Directory.CreateDirectory(Root+"/Native");
                ValidateProjection();
                var suite=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("MainMenuModeCardsRegression")).FirstOrDefault(t=>t!=null);
                if(suite==null)throw new InvalidOperationException("Missing focused persistence fixture assembly.");
                suite.GetMethod("RunFocusedValidation").Invoke(null,null);
                foreach(string language in new[]{"en","fa-IR"})
                {
                    GameLocalization.SetLocale(language,false);
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2400,1080),new Vector2Int(1600,1200)})
                        foreach(bool complete in new[]{false,true}) Capture(language,size,complete);
                }
                File.WriteAllText(Root+"/VALIDATION.json",JsonUtility.ToJson(new Report{checks=checks,captures=captures,passed=true},true));
                Debug.Log("[MainMenuModeCards] result=Passed checks="+checks+" captures="+captures+" layouts=A,C locales=en,fa-IR next=actual-playable fixtures=true playerDeviceAcceptance=Pending");
                passed=true;
            }
            catch(Exception error){Debug.LogException(error);Debug.LogError("[MainMenuModeCards] result=Failed "+error.Message);}
            finally{GameLocalization.SetLocale(locale,false);}
            return passed;
        }
        private static void ValidateProjection()
        {
            var profile=new PlayerProfileSaveData();
            string before=JsonUtility.ToJson(profile);
            var first=MainMenuModeProgressComposition.Project(MainMenuMode.Skirmish,profile,_=>ContentAccessState.Allowed);
            Require(first.Available&&first.Total==120&&first.Completed==0&&first.HasNext,"Fresh Skirmish must recommend a published mission with 0/120.");
            Require(SkirmishBattleCatalogConfig.Load().TryGet(first.MissionId,out var chosen)&&chosen.IsPlayable,"Recommendation is published.");
            Require(JsonUtility.ToJson(profile)==before,"Recommendation must not mutate save.");
            bool canonicalAccess=true;
            MainMenuModeProgressComposition.Project(MainMenuMode.Skirmish,profile,id=>{
                canonicalAccess &= id.StartsWith("skirmish.s",StringComparison.Ordinal);
                return ContentAccessState.Allowed;
            });
            Require(canonicalAccess,"Home uses the same canonical entitlement IDs as the mission library.");
            var blocked=MainMenuModeProgressComposition.Project(MainMenuMode.Skirmish,profile,_=>ContentAccessState.ProgressionLocked);
            Require(!blocked.HasNext,"No inaccessible Skirmish recommendation.");
            profile.skirmishMedals=new[]{
                new SkirmishMedalSaveData{catalogId=first.MissionId,difficulty=1,sessionId="first"},
                new SkirmishMedalSaveData{catalogId=first.MissionId,difficulty=1,sessionId="duplicate"},
                new SkirmishMedalSaveData{catalogId=first.MissionId,difficulty=2,sessionId="second-tier"},
                new SkirmishMedalSaveData{catalogId="S999",difficulty=0,sessionId="unknown"}};
            var progressed=MainMenuModeProgressComposition.Project(MainMenuMode.Skirmish,profile,_=>ContentAccessState.Allowed);
            Require(progressed.Completed==1&&progressed.Recruit==1&&progressed.Regular==1,"Distinct wins and tier medals deduplicate and ignore unknown IDs.");
            Require(progressed.MissionId!=first.MissionId||progressed.Replay,"Recommend uncompleted published mission before replay.");
            var rows=new List<SkirmishMedalSaveData>();
            foreach(var entry in SkirmishBattleCatalogConfig.Load().Entries)
                if(entry.IsPlayable)rows.Add(new SkirmishMedalSaveData{catalogId=entry.ScenarioId,difficulty=2,sessionId=entry.ScenarioId});
            profile.skirmishMedals=rows.ToArray();
            var all=MainMenuModeProgressComposition.Project(MainMenuMode.Skirmish,profile,_=>ContentAccessState.Allowed);
            Require(all.HasNext&&all.Replay,"All released wins offer a replay, never a planned mission.");
            profile.operations.firstClearRewardIds=new[]{"operation.o001","operation.o001","operation.o999"};
            profile.operationsMissionMedals=new[]{
                new OperationsMissionMedalSaveData{missionId="operation.o001",difficulty=1},
                new OperationsMissionMedalSaveData{missionId="operation.o001",difficulty=1},
                new OperationsMissionMedalSaveData{missionId="operation.o999",difficulty=1}};
            before=JsonUtility.ToJson(profile);
            var op=MainMenuModeProgressComposition.Project(MainMenuMode.Operations,profile,_=>ContentAccessState.Allowed);
            Require(op.Total==60&&op.Completed==1&&op.Regular==1,"Operations clears and medals are distinct and catalog-backed.");
            Require(op.HasNext&&op.MissionId=="operation.o001"&&op.Replay,"Only published Operations adapter may be recommended.");
            Require(JsonUtility.ToJson(profile)==before,"Operations reducer preview cannot mutate the profile.");
            Require(!MainMenuModeProgressComposition.Project(MainMenuMode.Operations,profile,_=>ContentAccessState.ProgressionLocked).HasNext,"Operations honors content access.");
            profile.operations.schemaVersion=int.MaxValue;
            Require(!MainMenuModeProgressComposition.Project(MainMenuMode.Operations,profile,_=>ContentAccessState.Allowed).HasNext,"Unknown Operations schema cannot be played or rewritten.");
        }
        private static void Capture(string locale,Vector2Int size,bool complete)
        {
            var profile=new PlayerProfileSaveData();
            var first=MainMenuModeProgressComposition.Project(MainMenuMode.Skirmish,profile,_=>ContentAccessState.Allowed);
            profile.skirmishMedals=new[]{new SkirmishMedalSaveData{catalogId=first.MissionId,difficulty=2,sessionId="native-fixture"}};
            var sk=MainMenuModeProgressComposition.Project(MainMenuMode.Skirmish,profile,_=>ContentAccessState.Allowed);
            var op=MainMenuModeProgressComposition.Project(MainMenuMode.Operations,profile,_=>ContentAccessState.Allowed);
            string name=(complete?"C-complete":"A-campaign")+"-"+locale+"-"+size.x;
            MainMenuV3PrefabBuilder.CaptureConfigured(Root+"/Native/"+name+".png",size.x,size.y,root=>{
                var hero=root.GetComponentInChildren<MainMenuCampaignCardView>(true);
                var so=new SerializedObject(hero);
                var plates=so.FindProperty("plates");
                Sprite plate=plates.GetArrayElementAtIndex(0).FindPropertyRelative("plate").objectReferenceValue as Sprite;
                var art=hero.transform.Find("CampaignArt").GetComponent<UnityEngine.UI.Image>();art.sprite=plate;art.enabled=true;
                root.transform.Find("MenuBackgroundContent/ComicBackdrop").GetComponent<UnityEngine.UI.Image>().sprite=plate;
                bool fa=locale=="fa-IR";
                UiLocalizedText.Set(hero.transform.Find("Title").GetComponent<TMP_Text>(),fa?"اولین تماس":"FIRST CONTACT");
                UiLocalizedText.Set(hero.transform.Find("Chapter").GetComponent<TMP_Text>(),fa?"فصل ۱ · ماموریت ۱":"CHAPTER 1 · MISSION 1");
                hero.transform.Find("Chapter").gameObject.SetActive(true);
                UiLocalizedText.Set(hero.transform.Find("Purpose").GetComponent<TMP_Text>(),fa?"نیروها را گرد هم بیاورید و از غیرنظامیان محافظت کنید.":"Rally your squad and protect the civilians.");
                UiLocalizedText.Set(hero.transform.Find("ContinueButton/Label").GetComponent<TMP_Text>(),fa?"ادامه کارزار  ‹":"CONTINUE CAMPAIGN  ›");
                root.transform.Find("FooterContent/StoreButton").gameObject.SetActive(true);
                root.transform.Find("FooterContent/OpenArmoryButton").gameObject.SetActive(true);
                var view=root.GetComponentInChildren<MainMenuModeCardsView>(true);
                view.PresentForReview(complete,op,sk);
                Require(view.Expanded==complete,"Correct A/C state.");
                foreach(var card in root.GetComponentsInChildren<MainMenuModeCardView>(true))
                {
                    Require(card.State.HasNext,"Fixture recommends a real published mission.");
                    Require(card.PreviewTexture!=null,"Actual mission thumbnail missing: "+card.State.MissionId);
                    var shared=MissionAtlasMissionArtworkCatalog.Load()?.Find(card.State.MissionId);
                    if(shared!=null)Require(card.PreviewTexture==shared,"Home and mission selector share exact artwork: "+card.State.MissionId);
                }
            },root=>ValidateLayout(root,name));
            captures++;
        }
        private static void ValidateLayout(GameObject root,string name)
        {
            var report=new System.Text.StringBuilder();
            foreach(var card in root.GetComponentsInChildren<MainMenuModeCardView>(true))
            {
                var parent=(RectTransform)card.transform;
                Rect area=parent.rect;
                foreach(var label in card.GetComponentsInChildren<TMP_Text>())
                {
                    if(!label.isActiveAndEnabled)continue;
                    label.ForceMeshUpdate(false,true);
                    int visible=label.textInfo.characterInfo.Take(label.textInfo.characterCount).Count(c=>c.isVisible);
                    report.AppendLine(label.name+" font="+label.font?.name+" size="+label.fontSize+" visible="+visible+" rect="+label.rectTransform.rect+" text="+label.text);
                    Require(!string.IsNullOrWhiteSpace(label.text)&&visible>0,"Invisible mode label: "+label.name+" / "+name);
                    if(label.name=="UniqueMissionProgress" || label.name=="NextPlayableMission" || label.name=="ModeTitle" || label.name=="Label")
                        Require(!label.isTextTruncated,"Truncated primary mode text: "+label.name+" / "+name);
                    var b=RectTransformUtility.CalculateRelativeRectTransformBounds(parent,label.rectTransform);
                    Require(b.min.x>=area.xMin-1&&b.max.x<=area.xMax+1&&b.min.y>=area.yMin-1&&b.max.y<=area.yMax+1,
                        "Mode label escapes panel: "+label.name+" / "+name);
                }
                var next=card.transform.Find("NextPlayableMission") as RectTransform;
                var count=card.transform.Find("UniqueMissionProgress") as RectTransform;
                var n=RectTransformUtility.CalculateRelativeRectTransformBounds(parent,next);
                var c=RectTransformUtility.CalculateRelativeRectTransformBounds(parent,count);
                Require(n.min.y>=c.max.y-1,"Next mission and completion text overlap: "+name);
            }
            File.WriteAllText(Root+"/Native/"+name+".text-layout.txt",report.ToString());
        }
        private static void Require(bool condition,string message){checks++;if(!condition)throw new InvalidOperationException(message);}
        [Serializable]private sealed class Report{public bool passed;public int checks,captures;public string evidence="Native renders use isolated presentation fixtures; mission outcomes and player/device readiness are not claimed.";}
    }
}
