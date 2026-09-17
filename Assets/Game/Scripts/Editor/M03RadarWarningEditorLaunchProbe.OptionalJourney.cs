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
        private static bool optionalJourney,reinforcementQueued,reinforcementDrawerClosed;
        private static double reinforcementNext, reinforcementCloseDeadline;
        private static int reinforcementProduced;
        private static string reinforcementSourceKey;
        private static double reinforcementDiagnosticAt;
        public static void RunOptionalJourneyEnglish()
            =>RunChecked(()=>{M03RadarWarningLocalizationBuilder.Import();RunOptionalLayoutChecks();RunBuildingJourneyEnglish();BeginOptionalJourney();});
        public static void RunOptionalJourneyPersian()
        {M03RadarWarningLocalizationBuilder.Import();RunBuildingJourneyPersian();BeginOptionalJourney();}
        public static void RunUnifiedScanProductionPersian()
        {
            RunOptionalJourneyPersian();
            unifiedScanJourney=true;radarToolbarLocale="fa-IR";
            radarToolbarAudit=true;radarToolbarClicked=false;radarToolbarSeenAt=0;
        }
        private static void BeginOptionalJourney()
        {
            optionalJourney=true;reinforcementQueued=false;reinforcementDrawerClosed=false;reinforcementNext=0;reinforcementProduced=0;reinforcementDiagnosticAt=0;
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
            if(reinforcementQueued && !reinforcementDrawerClosed)
            {
                var closingDrawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
                if(closingDrawer==null || !closingDrawer.IsOpen)
                {
                    reinforcementDrawerClosed=true;
                    Debug.Log("[M03OptionalJourney] production popup closed automatically; no close-button click");
                }
                else if(EditorApplication.timeSinceStartup>reinforcementCloseDeadline)
                    throw new InvalidOperationException("Production popup did not close after its animation.");
            }
            if(reinforcementQueued) ObserveOptionalProduction(em);
            if(p.GuidanceId!=45009) return false;
            var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            // Mission projection can advance before the throttled HUD presents its next lesson.
            if(aria==null || !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) ||
                panel.TutorialStep!=9 || (byte)typeof(AriaTutorialBriefingView)
                    .GetField("_tutorialStep",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(aria)!=9) return true;
            if(EditorApplication.timeSinceStartup<reinforcementNext) return true;
            reinforcementNext=EditorApplication.timeSinceStartup+1;
            var drawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
            if(reinforcementQueued) return true;
            if(drawer==null || !drawer.IsOpen)
            {ClickCommand(UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>().BuildButton);return true;}
            var catalog=drawer.GetComponent<BuildDrawerCatalogRuntimeView>();
            var rect=(RectTransform)typeof(BuildDrawerCatalogRuntimeView).GetMethod("ResolveRifleProductionGuidanceTarget",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(catalog,null);
            if(rect==null) throw new InvalidOperationException("No next reinforcement control before production.");
            var button=rect.GetComponent<Button>();
            AssertOptionalClickFrame(button);
            bool produce=button==drawer.PrimaryActionButton;
            var before=ReadBudget(em);
            if(produce) reinforcementSourceKey=((BuildDrawerCatalogItem)typeof(BuildDrawerCatalogRuntimeView)
                .GetField("_selectedItem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(catalog)).Prefab.name;
            ClickTutorialButton(button); // Same EventSystem pointer path as a touch/click.
            if(produce)
            {
                reinforcementCloseDeadline=EditorApplication.timeSinceStartup+2;
                var after=ReadBudget(em);
                if(after.Item2>=before.Item2) throw new InvalidOperationException("Rifle production did not spend displayed materials.");
                tutorialCreditsSpent+=before.Item1-after.Item1;tutorialMaterialsSpent+=before.Item2-after.Item2;
                reinforcementQueued=true;guidanceVisited.Add(9);
                Debug.Log("[M03OptionalJourney] queued one four-person rifle squad through Soldiers tab, card and Produce; source="+reinforcementSourceKey+" budget="+after);
            }
            return true;
        }
        private static void VerifyOptionalProduction(EntityManager em)
        {
            if(!optionalJourney) return;
            if(!reinforcementQueued || !reinforcementDrawerClosed)
                throw new InvalidOperationException("Optional production did not queue and close its popup.");
            if(reinforcementProduced!=4) throw new InvalidOperationException("Expected four recruited soldiers after popup closed, observed "+reinforcementProduced);
            Debug.Log("[M03OptionalJourney] result=Passed automatic popup close, exact purchase and four produced soldiers");
        }
        private static void ObserveOptionalProduction(EntityManager em)
        {
            if(reinforcementProduced==4 || EditorApplication.timeSinceStartup<reinforcementDiagnosticAt) return;
            reinforcementDiagnosticAt=EditorApplication.timeSinceStartup+5;
            int produced=0;var rows=new System.Collections.Generic.List<string>();
            using(var query=em.CreateEntityQuery(typeof(BuildingProducedUnitReadModel)))
            using(var owners=query.ToEntityArray(Unity.Collections.Allocator.Temp))
            foreach(var owner in owners)
            foreach(var unit in em.GetBuffer<BuildingProducedUnitReadModel>(owner,true))
            {
                rows.Add(unit.UnitSourceKey+" owner="+unit.HasOwnerFaction+"/"+unit.OwnerFactionId+" exists="+em.Exists(unit.Unit));
                if(unit.HasOwnerFaction!=0 && unit.OwnerFactionId==1 && em.Exists(unit.Unit) &&
                    string.Equals(unit.UnitSourceKey.ToString(),reinforcementSourceKey,StringComparison.OrdinalIgnoreCase)) produced++;
            }
            reinforcementProduced=Math.Max(reinforcementProduced,produced);
            Debug.Log("[M03OptionalProduction] source="+reinforcementSourceKey+" produced="+produced+" rows="+string.Join(";",rows));
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
