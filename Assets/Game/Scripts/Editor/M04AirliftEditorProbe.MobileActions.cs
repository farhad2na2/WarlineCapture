using System;
using System.Reflection;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    public static partial class M04AirliftEditorProbe
    {
        private static bool mobileSix,mobileEleven,mobileJet;
        private static int mobileUnload;
        private static void ReviewMobileDestinations(EntityManager em,CampaignMissionExtractionState rescue)
        {
            if(!mobileJet && UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var limits))
            {
                if((limits.AvailableSquadMask&8)!=0)throw new InvalidOperationException("Empty Jet class is available in M4.");
                if((limits.AvailableSquadMask&22)!=22)throw new InvalidOperationException("APC/helicopter categories cannot select their live transports.");
                mobileJet=true;Debug.Log("[M04MobileActions] live roster availability=Passed");
            }
            if(!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel))return;
            int lesson=panel.TutorialStep;
            if(lesson!=6 && lesson!=11 || lesson==6 && mobileSix || lesson==11 && mobileEleven)return;
            var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(aria==null || VisibleLesson(aria)!=lesson || !aria.ShowMeButton.IsInteractable() || !aria.ShowMeButton.isActiveAndEnabled)return;
            aria.ShowMeButton.onClick.Invoke();
            using var cameras=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            var focus=cameras.GetSingleton<RuntimeCameraFocusRequestComponent>();
            if(math.distance(focus.World,lesson==6?rescue.LandingCenter:rescue.DepartureCenter)>1)
                throw new InvalidOperationException("Show Me did not frame the destination for lesson "+lesson);
            if(lesson==6)mobileSix=true;else mobileEleven=true;
            Debug.Log("[M04MobileActions] actual Show Me destination=Passed lesson="+lesson);
        }
        private static byte VisibleLesson(AriaTutorialBriefingView view)=>(byte)typeof(AriaTutorialBriefingView).GetField("_tutorialStep",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
        private static bool UnloadThroughTutorial(EntityManager em,CampaignMissionExtractionState rescue)
        {
            if(mobileUnload==0)
            {Transport(em,new RtsSelectionCommandIntentRequestElement{Kind=RtsSelectionCommandIntentKind.FocusUnit,TargetEntity=rescue.Carrier,HasTargetEntity=1});mobileUnload=1;frame=Time.frameCount;return false;}
            if(Time.frameCount-frame<8)return false;
            var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(mobileUnload==1)
            {
                if(!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep!=7 || aria==null || VisibleLesson(aria)!=7)return false;
                foreach(var candidate in UnityEngine.Object.FindObjectsByType<MatchHudSelectionPanelView>(FindObjectsSortMode.None))
                {
                    var chip = (Button)typeof(MatchHudSelectionPanelView).GetField("passengerChipButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(candidate);
                    if(chip == null || !chip.isActiveAndEnabled || !chip.IsInteractable())continue;
                    chip.onClick.Invoke();mobileUnload=2;frame=Time.frameCount;return false;
                }
                return false;
            }
            MatchHudSelectionPanelView selection=null;
            foreach(var candidate in UnityEngine.Object.FindObjectsByType<MatchHudSelectionPanelView>(FindObjectsSortMode.None))
                if(candidate.IsPassengerDrawerOpen){selection=candidate;break;}
            var drawer=selection==null ? null : (MatchHudTransportPassengerDrawerView)typeof(MatchHudSelectionPanelView).GetField("passengerDrawer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(selection);
            if(mobileUnload==2)
            {
                if(selection==null || !selection.IsPassengerDrawerOpen || drawer==null || UiShellRuntimeGateway.IsMissionFieldGuidePresenting())
                    throw new InvalidOperationException("Lesson 7 passenger state: selection="+(selection!=null)+" open="+(selection!=null && selection.IsPassengerDrawerOpen)+" drawer="+(drawer!=null)+" guide="+UiShellRuntimeGateway.IsMissionFieldGuidePresenting());
                if(!UiShellRuntimeGateway.TryReadMissionExtraction(out var model) || model.Aboard!=4)throw new InvalidOperationException("Opening passengers performed extra actions.");
                var exit=(Button)typeof(MatchHudTransportPassengerDrawerView).GetField("exitAllButton",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(drawer);
                if(!exit.isActiveAndEnabled || !exit.IsInteractable())
                {
                    if(Time.frameCount-frame==8)Debug.Log("[M04MobileActions] waiting for unload: active="+exit.isActiveAndEnabled+" own="+exit.interactable+" hierarchy="+exit.IsInteractable());
                    return false;
                }
                ScreenCapture.CaptureScreenshot(Output+"/tutorial-passengers.png");exit.onClick.Invoke();mobileUnload=3;frame=Time.frameCount;return false;
            }
            if(!UiShellRuntimeGateway.TryReadMissionExtraction(out var after) || after.Aboard!=0)return false;
            if(drawer!=null)((Button)typeof(MatchHudTransportPassengerDrawerView).GetField("closeButton",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(drawer)).onClick.Invoke();
            Debug.Log("[M04MobileActions] visible passenger chip opens drawer, actual Exit All unloads=Passed");return true;
        }
    }
}
