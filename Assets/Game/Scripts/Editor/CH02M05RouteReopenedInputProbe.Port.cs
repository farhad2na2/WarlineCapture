using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace Game.Editor
{
    public static partial class CH02M05RouteReopenedInputProbe
    {
        private static bool sourceChecked,manualFocused,inputConfigured;
        private static int manualActions,ariaActions,lastManualGuidance;
        private static Vector2 manualPoint;
        private static float manualPointStableAt;
        private static bool recordsHoldCaptured;
        private static int reliefDeckSamples,fuelDeckSamples;
        private static readonly HashSet<string> playedVoices=new();
        private static readonly HashSet<string> seenPanels=new();
        private static AudioClip priorComicClip;
        private static float comicVoiceProgress;
        private static SaveService inputSave;
        private static int startingCredits,startingXp;
        public static void OpenEnglishReview()
        {
            ResetSpecialRuns();
            SessionState.SetString(Active+".Negative","");SessionState.SetBool(Active+".Review",true);
            SessionState.SetBool(Manual,true);SessionState.SetBool(Active+".ManualInput",true);SessionState.SetString(Active+".Locale","en");Run();
        }
        private static void ValidateCompletedJourney()
        {
            if(reliefDeckSamples==0||fuelDeckSamples==0)throw new InvalidOperationException("Both convoys must be observed on their bridge decks.");
            if(!store.ReadAll().Single(x=>x.missionId==Game.Missions.Contracts.CampaignMissionSequence.RouteReopened).firstClearRewardSettled)
                throw new InvalidOperationException("Victory was not settled.");
            var profile=inputSave.LoadProfile();
            if(profile.credits!=startingCredits+6000||profile.commanderXp!=startingXp+1200)
                throw new InvalidOperationException("Unexpected first-clear Credits or Commander XP.");
            if(playedVoices.Count<7||seenPanels.Count<7)throw new InvalidOperationException("Required native story panels or voice playback missing.");
            Debug.Log($"[RouteReopenedJourney] result=Passed bridgeSamples={reliefDeckSamples}/{fuelDeckSamples} voices={playedVoices.Count} panels={seenPanels.Count} Credits=6000 XP=1200");
        }
        private static string WatchDiagnostics(EntityManager em)
        {
            using var q=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.AriaPlaySessionComponent),typeof(Game.UI.Shell.Contracts.Ecs.AriaPlayObservationComponent));
            if(q.CalculateEntityCount()!=1)return "ARIA owner unavailable";
            var session=q.GetSingleton<Game.UI.Shell.Contracts.Ecs.AriaPlaySessionComponent>();var observation=q.GetSingleton<Game.UI.Shell.Contracts.Ecs.AriaPlayObservationComponent>();
            return $"stop={session.StopReason} actions={session.Actions} attempts={session.Attempts} target={session.TargetId}/{session.Target} observation={observation.Kind}/{observation.Position} goal={session.GoalId}";
        }
        private static void ObserveMedia()
        {
            foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
                if(source.isPlaying&&source.clip!=null&&source.timeSamples>0&&!source.mute&&source.volume>0)
                {
                    var path=AssetDatabase.GetAssetPath(source.clip);
                    if(path.Contains("CH02M05RouteReopened")&&playedVoices.Add(path))Debug.Log("[RouteReopenedVoice] native-playback="+path);
                }
            var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            var voice=narrative?.VoiceSource;var clip=voice!=null&&voice.isPlaying?voice.clip:null;
            if(clip!=priorComicClip)
            {
                if(priorComicClip!=null&&comicVoiceProgress<priorComicClip.length-.45f)throw new InvalidOperationException("Comic voice was cut off: "+priorComicClip.name);
                priorComicClip=clip;comicVoiceProgress=0;
            }
            if(clip!=null)comicVoiceProgress=Mathf.Max(comicVoiceProgress,(float)voice.timeSamples/clip.frequency);
            // ARIA pauses tactical input during comics. The test player advances
            // the existing dialogue surface after native voice playback finishes.
            if(narrative!=null&&clip==null&&narrative.DialogueView?.Phase==Game.UI.Runtime.NarrativeDialoguePhase.AdvanceReady&&EditorApplication.timeSinceStartup-lastInput>=1)
                SkipNarrative(narrative);
            var panel=narrative?.CurrentPanelSprite;
            if(panel!=null&&AssetDatabase.GetAssetPath(panel).StartsWith(CH02M05RouteReopenedMediaImporter.ArtRoot+"/",StringComparison.Ordinal)&&seenPanels.Add(panel.name))
            {string name=panel.name;EditorApplication.delayCall+=()=>ScreenCapture.CaptureScreenshot(Output+"/comic-"+name+".png");}
        }
        private static InputSettings.EditorInputBehaviorInPlayMode savedEditorInput;
        private static InputSettings.BackgroundBehavior savedBackgroundInput;
        private static void ConfigureInputFixture()
        {
            var input=InputSystem.settings;savedEditorInput=input.editorInputBehaviorInPlayMode;savedBackgroundInput=input.backgroundBehavior;
            input.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            input.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;inputConfigured=true;
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");Application.runInBackground=true;
        }
        private static void RestoreInputFixture()
        {
            if(!inputConfigured)return;InputSystem.settings.editorInputBehaviorInPlayMode=savedEditorInput;InputSystem.settings.backgroundBehavior=savedBackgroundInput;
            inputConfigured=false;Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",null);
        }
        private static void ValidatePortSource(EntityManager em)
        {
            using var q=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
            if(q.CalculateEntityCount()!=1)throw new InvalidOperationException("Expected one active Route Reopened map owner.");
            var metadata=q.GetSingleton<OperationMapMetadataComponent>();ref var map=ref metadata.Blob.Value;
            if(!map.OperationMapId.Equals(CH02M05RouteReopenedConfigBuilder.MapId)||!map.SourceOperationMapId.Equals("opmap.skirmish.ashlineport_prepared")||!map.SourceContentHash.Equals(CH02M05RouteReopenedConfigBuilder.PreparedHash))
                throw new InvalidOperationException("Route Reopened loaded an unexpected source/hash.");
            sourceChecked=true;reliefDeckSamples=fuelDeckSamples=0;Debug.Log("[RouteReopenedPortSource] result=Passed logical="+map.OperationMapId+" physical="+map.SourceOperationMapId+" hash="+map.SourceContentHash);
            var adapterType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Game.Composition.MatchHudMinimapDataSourceAdapter")).First(t=>t!=null);
            var source=(IMatchHudMinimapDataSource)Activator.CreateInstance(adapterType,true);
            var features=new List<MatchHudMinimapSurfaceFeatureModel>();source.GetSurfaceFeatures(new MatchHudMinimapAreaModel(new Vector3(540,0,365),230,275),features);
            int bridges=features.Count(x=>x.Kind==MatchHudMinimapSurfaceFeatureKind.Bridge),roads=features.Count(x=>x.Kind==MatchHudMinimapSurfaceFeatureKind.Road);
            if(bridges==0||roads==0)throw new InvalidOperationException("Prepared port roads or bridges missing from the native minimap data.");
            Debug.Log($"[RouteReopenedMinimap] result=Passed features={features.Count} bridges={bridges} roads={roads}");
        }
        private static void ObserveRouteCrossings(EntityManager em,Entity root)
        {
            if(!sourceChecked||!em.HasBuffer<CampaignMissionRouteReopenedMember>(root))return;
            if(!recordsHoldCaptured&&em.GetComponentData<CampaignMissionRouteReopenedState>(root).RecordsHoldMilliseconds>=2500)
            {recordsHoldCaptured=true;ScreenCapture.CaptureScreenshot(Output+"/records-office-hold.png");}
            using var query=em.CreateEntityQuery(typeof(MapSurfaceComponent));if(query.CalculateEntityCount()!=1)return;
            var surface=query.GetSingleton<MapSurfaceComponent>();if(!surface.SurfaceBlob.IsCreated)return;
            foreach(var member in em.GetBuffer<CampaignMissionRouteReopenedMember>(root,true))
            {
                if(member.Kind is not (2 or 3)||!em.Exists(member.Entity)||!em.HasComponent<Unity.Transforms.LocalTransform>(member.Entity))continue;
                var p=em.GetComponentData<Unity.Transforms.LocalTransform>(member.Entity).Position;
                if(p.x<661||p.x>699)continue;
                var cell=new Unity.Mathematics.int2((int)p.x,(int)p.z);
                if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value,cell,out var sample)||sample.SurfaceType!=MapSurfaceType.BridgeDeck||sample.Height<0||p.y<sample.Height-.25f)
                    throw new InvalidOperationException("Convoy entered canal bed or an unqualified crossing: "+p);
                if(member.Kind==2){if(reliefDeckSamples++==0)ScreenCapture.CaptureScreenshot(Output+"/relief-south-bridge.png");}
                else if(fuelDeckSamples++==0)ScreenCapture.CaptureScreenshot(Output+"/fuel-main-bridge.png");
            }
        }
        private static void DriveManual(EntityManager em,Entity root)
        {
            if(UiShellRuntimeGateway.ReadAriaPlay().Active)throw new InvalidOperationException("ARIA is active during manual journey.");
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();if(view==null||!view.IsPresentationVisible)return;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))
            {Tap(view.ContinueButton);return;}
            if(target.Moving||target.BattleAction==UiTutorialBattleAction.Watch)return;
            int key=guidance.GuidanceId*2+(target.NeedsSelection?0:1);
            if(lastManualGuidance!=key){lastManualGuidance=key;manualFocused=false;ScreenCapture.CaptureScreenshot(Output+"/manual-"+key+".png");}
            if(!manualFocused){Tap(view.ShowMeButton);manualFocused=true;return;}
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();var camera=Camera.main;
            if(controls==null||camera==null||!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state))return;
            var mode=target.NeedsSelection?TacticalCommandMode.Select:target.BattleAction==UiTutorialBattleAction.Attack?TacticalCommandMode.Attack:TacticalCommandMode.Move;
            if(state.ActiveCommandMode!=mode){Tap(mode==TacticalCommandMode.Select?controls.SelectButton:mode==TacticalCommandMode.Attack?controls.AttackButton:controls.MoveButton);return;}
            if(target.NeedsSelection&&target.DragSelection)
            {
                var from=new Vector2(float.MaxValue,float.MaxValue);
                var to=new Vector2(float.MinValue,float.MinValue);
                for(int i=0;i<8;i++)
                {
                    var worldCorner=new Vector3((i&1)==0?target.SelectionMin.x:target.SelectionMax.x,
                        (i&2)==0?target.SelectionMin.y:target.SelectionMax.y,
                        (i&4)==0?target.SelectionMin.z:target.SelectionMax.z);
                    var screen=camera.WorldToScreenPoint(worldCorner);
                    if(screen.z<=0)return;
                    from=Vector2.Min(from,screen);to=Vector2.Max(to,screen);
                }
                from-=Vector2.one*8;to+=Vector2.one*8;
                var selectionHits=new List<RaycastResult>();
                foreach(Vector2 p in new[]{(Vector2)from,(Vector2)to,((Vector2)from+(Vector2)to)*.5f})
                {
                    selectionHits.Clear();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=p},selectionHits);
                    if(!camera.pixelRect.Contains(p)||selectionHits.Count>0){Tap(view.ShowMeButton);return;}
                }
                if(Vector2.Distance(manualPoint,from)>1f){manualPoint=from;manualPointStableAt=Time.unscaledTime;return;}
                if(Time.unscaledTime-manualPointStableAt<.5f)return;
                if(touch.TryGesture(from,to,.5f,.9f,Time.unscaledTime))
                {manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[RouteReopenedManual] normalTouch=selection-drag rifles="+target.RequiredSelectionCount);}
                return;
            }
            var world=target.NeedsSelection?target.Selection+Vector3.up:target.Destination;
            var point=camera.WorldToScreenPoint(world);var hits=new List<RaycastResult>();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            // Editor update callbacks report the window size, while the camera
            // and input device use the selected Game View render resolution.
            if(point.z<=0||!camera.pixelRect.Contains(point)||hits.Count>0){Tap(view.ShowMeButton);return;}
            if(Vector2.Distance(manualPoint,point)>1f){manualPoint=point;manualPointStableAt=Time.unscaledTime;return;}
            if(Time.unscaledTime-manualPointStableAt<.5f)return;
            if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime))
            {manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[RouteReopenedManual] normalTouch="+mode+" world="+world);}
        }
    }
}
