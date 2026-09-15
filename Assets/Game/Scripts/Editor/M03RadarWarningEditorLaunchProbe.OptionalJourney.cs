using System;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static bool optionalJourney,reinforcementQueued;
        private static double reinforcementNext;
        public static void RunOptionalJourneyEnglish()
            =>RunChecked(()=>{RunOptionalLayoutChecks();RunBuildingJourneyEnglish();BeginOptionalJourney();});
        public static void RunOptionalJourneyPersian()
        {RunBuildingJourneyPersian();BeginOptionalJourney();}
        private static void BeginOptionalJourney()
        {
            optionalJourney=true;reinforcementQueued=false;reinforcementNext=0;
            BeginPlacementIndicator();
            MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);
        }
        private static void RunOptionalLayoutChecks()
        {
            foreach(var pair in new[]{("M03PlacementRenderOrderTests","GuideFollowsFinalButtonAfterValidityAndCanvasLayoutChange"),
                ("M03PlacementRenderOrderTests","SkippingOptionalLessonCancelsItsUnfinishedPreview"),
                ("HudRightColumnLayoutValidation","MinimapDockAndContentHeightFollowActualControlsAndCopy")})
            {
                Type type=null;
                foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if((type=assembly.GetType(pair.Item1))!=null) break;
                if(type==null) throw new InvalidOperationException("Missing UI regression fixture: "+pair.Item1);
                type.GetMethod(pair.Item2).Invoke(Activator.CreateInstance(type),null);
            }
            Debug.Log("[M03OptionalLayout] result=Passed late-frame,skip-preview,scrollbar-and-dock");
        }
        private static bool AdvanceOptionalReinforcement(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!optionalJourney || runtime.Phase!=MissionPhaseKind.Engage) return false;
            var p=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(p.GuidanceId!=45009) return false;
            var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            // Mission projection can advance before the throttled HUD presents its next lesson.
            if(aria==null || !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) ||
                panel.TutorialStep!=9 || (byte)typeof(AriaTutorialBriefingView)
                    .GetField("_tutorialStep",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(aria)!=9) return true;
            if(EditorApplication.timeSinceStartup<reinforcementNext) return true;
            reinforcementNext=EditorApplication.timeSinceStartup+1;
            var drawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
            if(reinforcementQueued)
            {
                if(drawer!=null && drawer.IsOpen) ClickCommand(drawer.CloseButton);
                return true;
            }
            if(drawer==null || !drawer.IsOpen)
            {ClickCommand(UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>().BuildButton);return true;}
            var catalog=drawer.GetComponent<BuildDrawerCatalogRuntimeView>();
            var rect=(RectTransform)typeof(BuildDrawerCatalogRuntimeView).GetMethod("ResolveRifleProductionGuidanceTarget",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(catalog,null);
            if(rect==null) throw new InvalidOperationException("No next reinforcement control before production.");
            var button=rect.GetComponent<Button>();
            AssertOptionalClickFrame(button);
            bool produce=button==drawer.PrimaryActionButton;
            var before=ReadBudget(em);
            ClickTutorialButton(button); // Same EventSystem pointer path as a touch/click.
            if(produce)
            {
                var after=ReadBudget(em);
                if(after.Item2>=before.Item2) throw new InvalidOperationException("Rifle production did not spend displayed materials.");
                tutorialCreditsSpent+=before.Item1-after.Item1;tutorialMaterialsSpent+=before.Item2-after.Item2;
                reinforcementQueued=true;guidanceVisited.Add(9);
                Debug.Log("[M03OptionalJourney] queued one four-person rifle squad through Soldiers tab, card and Produce; budget="+after);
            }
            return true;
        }
        private static void AssertOptionalClickFrame(Button button)
        {
            var cue=GameObject.Find("AriaAssistantTargetIndicatorRuntime");
            if(cue==null || !button.IsInteractable()) throw new InvalidOperationException("Optional next click has no usable visible guide.");
            var a=IndicatorScreenBounds((RectTransform)button.transform);var b=IndicatorScreenBounds((RectTransform)cue.transform);
            if(Vector2.Distance(a.center,b.center)>2 || !b.Contains(a.min) || !b.Contains(a.max))
                throw new InvalidOperationException("Optional next-click guide is not on "+button.name+": "+a+" versus "+b);
        }
    }
}
