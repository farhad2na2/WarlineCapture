using System;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Unity.Mathematics;
using Game.Composition;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    public static partial class M04AirliftEditorProbe
    {
        private const string PlayerVisualKey="Warline.M04.PlayerVisualOnly";
        private const string PlayerGuidanceKey="Warline.M04.PlayerGuidance";
        private static int playerLesson,playerPartialStage,playerQueuedLesson,playerShows;
        private static double playerNext,playerLessonAt;
        public static void RunPlayerGuidanceFa()=>RunPlayerGuidance("fa-IR");
        public static void RunPlayerGuidanceEn()=>RunPlayerGuidance("en");
        public static void RunPlayerGuidanceVisualFa()
        {RunPlayerGuidanceWideFa();SessionState.SetBool(PlayerVisualKey,true);}
        public static void RunPlayerGuidanceWideFa()
        {RunPlayerGuidance("fa-IR");MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);}
        private static void RunPlayerGuidance(string locale)
        {
            SessionState.SetBool(PlayerVisualKey,false);
            SessionState.SetString("Warline.M04.PlayerLocale",locale);SessionState.SetBool(PlayerGuidanceKey,true);
            SessionState.SetString("Warline.M04.ReadinessOutput","/private/tmp/warline-m04-guidance-"+locale);
            SessionState.SetBool(Both,false);SessionState.SetBool(Full,true);
            playerLesson=playerPartialStage=playerQueuedLesson=playerShows=0;playerNext=playerLessonAt=0;
            Run();
        }
        private static void TickPlayerGuidance(EntityManager em,Entity root,CampaignMissionExtractionState extraction)
        {
            if(EditorApplication.timeSinceStartup<playerNext) return;
            if(playerPartialStage==3 && SessionState.GetBool(PlayerVisualKey,false))
            {SessionState.SetBool(PlayerVisualKey,false);Complete(true,"M4 partial-selection visual check: Select cue cleared, missing specialist marker visible, Show Me hidden.");return;}
            if(GameLocalization.CurrentLocaleCode!=SessionState.GetString("Warline.M04.PlayerLocale","en"))
            {GameLocalization.SetLocale(SessionState.GetString("Warline.M04.PlayerLocale","en"),false);PlayerWait();return;}
            var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(aria==null || !aria.IsPresentationVisible || !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel)) return;
            int lesson=panel.TutorialStep;
            if(lesson!=playerLesson)
            {
                Debug.Log("[M04PlayerRoute] lesson="+lesson+" locale="+GameLocalization.CurrentLocaleCode);
                ScreenCapture.CaptureScreenshot(Output+"/lesson-"+lesson+".png");
                playerLesson=lesson;playerLessonAt=EditorApplication.timeSinceStartup;PlayerWait();return;
            }
            if(EditorApplication.timeSinceStartup-playerLessonAt>100)throw new TimeoutException("Player tutorial stalled lesson="+lesson+" partial="+playerPartialStage);
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var camera=match.MatchBootstrap.WorldCamera;
            var input=(RtsSelectionInputCompositionSystemHelper)typeof(SelectionUiCommandUiSystemHelper)
                .GetField("_inputSystem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(match.MatchBootstrap.SelectionUiCommand);
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            UiShellRuntimeGateway.TryReadMatchHudCommandState(out var commandState);var mode=commandState.ActiveCommandMode;
            var indicator=GameObject.Find("AriaAssistantTargetIndicatorRuntime");
            bool uiCue=indicator!=null && indicator.activeInHierarchy;
            bool worldCue=PlayerWorldCueVisible(camera);
            if(aria.ShowMeButton.gameObject.activeSelf && (uiCue || worldCue))
                throw new InvalidOperationException("Show Me is visible beside an already visible indicator at lesson "+lesson);
            if(mode==TacticalCommandMode.Select && uiCue)
            {
                var cue=(RectTransform)indicator.transform;var button=(RectTransform)controls.SelectButton.transform;
                if(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null,cue.position),RectTransformUtility.WorldToScreenPoint(null,button.position))<20)
                    throw new InvalidOperationException("Select highlight persisted after Select was accepted.");
            }
            if(lesson==1){PlayerClick(aria.DoItButton);return;}
            if(lesson is 10 or 12)
            {if(uiCue || worldCue || aria.ShowMeButton.gameObject.activeSelf)throw new InvalidOperationException("Waiting lesson retains misleading guidance.");PlayerWait();return;}
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))throw new InvalidOperationException("Action lesson has no target: "+lesson);
            if(lesson==4 && playerPartialStage==1)
            {
                int selected=em.GetBuffer<CampaignMissionExtractionMember>(root,true).AsNativeArray().ToArray().Count(m=>m.Kind==1 && em.HasComponent<SelectedUnitTag>(m.Entity));
                if(selected<1 || selected>=4)throw new InvalidOperationException("Partial selection setup selected "+selected+" specialists.");
                if(!panel.RecommendationBody.Contains(selected.ToString()))throw new InvalidOperationException("ARIA omits live partial-selection count.");
                ScreenCapture.CaptureScreenshot(Output+"/partial-selection.png");
                Debug.Log("[M04PlayerRoute] partial="+selected+"/4 missing="+target.Selection);
                if(!UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.FocusDeparture))throw new InvalidOperationException("Normal camera navigation rejected.");
                playerPartialStage=2;PlayerWait(1.2);return;
            }
            if(aria.ShowMeButton.gameObject.activeSelf)
            {PlayerClick(aria.ShowMeButton);playerShows++;return;}
            if(lesson==4 && playerPartialStage==2 && worldCue)
            {
                if(playerShows<1)throw new InvalidOperationException("Offscreen recovery never offered Show Me.");
                ScreenCapture.CaptureScreenshot(Output+"/show-me-revealed-missing.png");playerPartialStage=3;PlayerWait(1);return;
            }
            if(target.NeedsSelection)
            {
                if(mode!=TacticalCommandMode.Select && (target.RequiredSelectionCount>1 || mode!=TacticalCommandMode.None))
                {PlayerClick(controls.SelectButton);return;}
                if(!worldCue){PlayerWait();return;}
                if(target.RequiredSelectionCount>1)
                {
                    PlayerSelectBox(em,root,camera,input,lesson==4 && playerPartialStage==0);
                    if(lesson==4 && playerPartialStage==0)playerPartialStage=1;
                }
                else
                {
                    var actor=lesson is 8 or 11?extraction.Aircraft:extraction.Carrier;
                    if(!input.QueueFocusUnitCommandRequest(camera.WorldToScreenPoint((Vector3)em.GetComponentData<LocalTransform>(actor).Position+Vector3.up),Time.frameCount))
                        throw new InvalidOperationException("Normal world selection request rejected.");
                }
                PlayerWait();return;
            }
            if(lesson is 2 or 4 or 8){PlayerWait();return;}
            if(lesson==7)
            {
                var selection=UnityEngine.Object.FindObjectsByType<MatchHudSelectionPanelView>(FindObjectsSortMode.None)
                    .FirstOrDefault(v=>PlayerPassengerButton(v)!=null && PlayerPassengerButton(v).isActiveAndEnabled);
                if(selection==null)throw new InvalidOperationException("No accessible passenger control.");
                PlayerClick(PlayerPassengerButton(selection));return;
            }
            if(playerQueuedLesson==lesson || target.Moving){PlayerWait();return;}
            if((mode==TacticalCommandMode.Board || mode==TacticalCommandMode.Move) && !worldCue){PlayerWait();return;}
            if(lesson is 5 or 9)
            {
                if(mode!=TacticalCommandMode.Board){PlayerClick(controls.CommandWheelPanel.NextBoardButton);return;}
                foreach(var feedback in UnityEngine.Object.FindObjectsByType<BattleHudRuntimeFeedbackView>(FindObjectsSortMode.None))
                    if(feedback.BoardAllButton!=null && feedback.BoardAllButton.gameObject.activeInHierarchy ||
                       feedback.CancelButton!=null && feedback.CancelButton.gameObject.activeInHierarchy)
                        throw new InvalidOperationException("Legacy boarding footer actions must stay hidden.");
                ScreenCapture.CaptureScreenshot(Output+"/boarding-"+lesson+".png");
                if(!input.QueueBoardTransportCommandRequest(camera.WorldToScreenPoint(target.Destination+Vector3.up),Time.frameCount))
                    throw new InvalidOperationException("Normal boarding tap rejected.");
            }
            else
            {
                if(mode!=TacticalCommandMode.Move){PlayerClick(controls.MoveButton);return;}
                using var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                var grid=maps.GetSingleton<OperationMapMetadataComponent>().Blob.Value.Grid;
                var cell=new int2((int)math.floor((target.Destination.x-grid.Origin.x)/grid.CellSize),(int)math.floor((target.Destination.z-grid.Origin.z)/grid.CellSize));
                if(!input.QueueMoveCommandRequest(camera.WorldToScreenPoint(target.Destination),cell,target.Destination,Time.frameCount))
                    throw new InvalidOperationException("Normal movement tap rejected.");
            }
            playerQueuedLesson=lesson;PlayerWait();
        }
        private static void PlayerSelectBox(EntityManager em,Entity root,Camera camera,RtsSelectionInputCompositionSystemHelper input,bool partial)
        {
            var points=em.GetBuffer<CampaignMissionExtractionMember>(root,true).AsNativeArray().ToArray().Where(m=>m.Kind==1)
                .Select(m=>(Vector2)camera.WorldToScreenPoint((Vector3)em.GetComponentData<LocalToWorld>(m.Entity).Position)).ToArray();
            var min=points.Aggregate(Vector2.Min);var max=points.Aggregate(Vector2.Max);
            var rect=Rect.MinMaxRect(min.x-3,min.y-3,max.x+3,max.y+3);
            if(partial)
            {
                var sorted=points.OrderBy(p=>p.x).ToArray();
                rect.xMax=(sorted[2].x+sorted[3].x)/2;
                if(Mathf.Abs(sorted[2].x-sorted[3].x)<2)rect.xMax=(sorted[0].x+sorted[3].x)/2;
            }
            Debug.Log("[M04PlayerRoute] box="+rect+" partial="+partial+" points="+string.Join(";",points.Select(p=>p.ToString())));
            if(!input.QueueSelectAllCommandRequest(rect,Time.frameCount))
                throw new InvalidOperationException("Normal selection box rejected.");
            // Submit through the deferred screen-rectangle command so the normal input flush
            // processes it; pointer rectangles are consumed synchronously inside a drag event.
            UnityEngine.Object.FindAnyObjectByType<MatchSceneView>().MatchBootstrap.SelectionUiCommand.RequestExitSelectionMode();
        }
        private static bool PlayerWorldCueVisible(Camera camera)
        {
            var marker=GameObject.Find("AriaAssistantPreviewHighlightRuntime");if(marker==null || !marker.activeInHierarchy)return false;
            var line=marker.GetComponentInChildren<LineRenderer>();if(line==null)return false;
            var p=camera.WorldToViewportPoint(line.bounds.center);
            return p.z>0 && p.x>.18f && p.x<.72f && p.y>.27f && p.y<.86f;
        }
        private static Button PlayerPassengerButton(MatchHudSelectionPanelView view)=>(Button)typeof(MatchHudSelectionPanelView)
            .GetMethod("ResolvePassengerTutorialButton",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null);
        private static void PlayerClick(Button button)
        {
            if(button==null || !button.isActiveAndEnabled || !button.IsInteractable())throw new InvalidOperationException("Tutorial requested an unavailable button: "+button?.name);
            Debug.Log("[M04PlayerRoute] click="+button.name+" lesson="+playerLesson);button.onClick.Invoke();PlayerWait();
        }
        private static void PlayerWait(double seconds=.75)=>playerNext=EditorApplication.timeSinceStartup+seconds;
    }
}
