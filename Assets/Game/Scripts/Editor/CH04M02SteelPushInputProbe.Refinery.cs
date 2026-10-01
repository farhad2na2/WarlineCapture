using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Transforms;
namespace Game.Editor
{
    public static partial class CH04M02SteelPushInputProbe
    {
        private static bool sourceChecked,manualFocused,replaying,shortageInitialized,socketSeen;
        private static bool ShortageFixture=>SessionState.GetBool(Active+".ShortageFixture",false);
        private static int manualActions,lastManualGuidance,arrivalClock,manualReadinessStage,startingReplayCount;
        private const string ReplayProfile="Design/AgentReports/MapVariantMissionRework/SteelPush/Evidence/20261001-091436-steel-push-en/1c2e4b70cadb4c5a88ffd52734cf91d8/profile.json";
        private static bool SavedReplay=>SessionState.GetBool(Active+".SavedReplay",false);
        private static Vector2 manualPoint;
        private static float manualPointStableAt,lowestFuel;
        private static double lastInput;
        private static AriaTouchInputUiSystemHelper touch;
        private static readonly Dictionary<Entity,Vector3> priorPositions=new();
        private static readonly Dictionary<Entity,float> distances=new();
        private static readonly Dictionary<Entity,Vector2> commandPaths=new();
        public static void RunEnglishManual(){ResetModes();SessionState.SetBool(Active+".ManualInput",true);RunEnglish();}
        public static void RunPersianManual(){ResetModes();SessionState.SetBool(Active+".ManualInput",true);RunPersian();}
        public static void RunRefineryEnglish(){ResetModes();SessionState.SetBool(Active+".ManualInput",false);RunEnglish();}
        private static void ResetModes(){replaying=shortageInitialized=false;SessionState.SetBool(Active+".SavedReplay",false);SessionState.SetBool(Active+".Review",false);SessionState.SetBool(Active+".Replay",false);SessionState.SetBool(Active+".Negative",false);SessionState.SetBool(Active+".ShortageFixture",false);}
        public static void RunSavedReplayEnglish(){ResetModes();SessionState.SetBool(Active+".SavedReplay",true);SessionState.SetBool(Active+".ManualInput",false);RunEnglish();}
        public static void OpenEnglishReview(){ResetModes();SessionState.SetBool(Active+".Review",true);SessionState.SetBool(Active+".ManualInput",true);RunEnglish();}
        public static void RunRefineryEnglishReplay(){ResetModes();SessionState.SetBool(Active+".Replay",true);SessionState.SetBool(Active+".ManualInput",false);RunEnglish();}
        public static void RunShortageFixtureEnglish(){ResetModes();SessionState.SetBool(Active+".ShortageFixture",true);SessionState.SetBool(Active+".ManualInput",true);RunEnglish();}
        private static void ResetRefineryProbe()
        {sourceChecked=manualFocused=socketSeen=false;manualActions=lastManualGuidance=arrivalClock=manualReadinessStage=0;negativeMapPhase=0;lastInput=manualPointStableAt=0;lowestFuel=120;priorPositions.Clear();distances.Clear();commandPaths.Clear();}
        private static void ObserveRefinery(EntityManager em,Entity root,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!socketSeen)
            {
                using var hidden=em.CreateEntityQuery(typeof(OperationMapMissionPresentationHiddenTag),typeof(Unity.Rendering.DisableRendering));
                if(hidden.CalculateEntityCount()>0)
                {
                    using var entities=hidden.ToEntityArray(Unity.Collections.Allocator.Temp);
                    foreach(var entity in entities)if(em.HasComponent<UnitHealth>(entity))throw new InvalidOperationException("Depot socket retired a gameplay health owner.");
                    socketSeen=true;Debug.Log("[SteelPushDepotSocketEvidence] result=Passed hiddenRenderers="+entities.Length+" gameplayHealthOwners=0");
                }
            }
            if(!sourceChecked)
            {
                using var q=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));if(q.CalculateEntityCount()!=1)return;
                var metadata=q.GetSingleton<OperationMapMetadataComponent>();ref var map=ref metadata.Blob.Value;
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                if(replaying&&runtime.RunKind!=Game.Missions.Contracts.MissionRunKind.Replay)throw new InvalidOperationException("Public replay launch did not use Replay run kind.");
                ValidateVehicleFormation(em,root,1,4);
                ValidateVehicleFormation(em,root,2,5);
                if(!map.OperationMapId.Equals(CH04M02SteelPushConfigBuilder.MapId)||!map.SourceOperationMapId.Equals("opmap.skirmish.refinerydistrict_prepared")||!map.SourceContentHash.Equals(CH04M02SteelPushConfigBuilder.PreparedHash)||!map.ContentHash.Equals(AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(CH04M02SteelPushConfigBuilder.MapPath).ContentHash))throw new InvalidOperationException("Unexpected Steel Push physical source/hash.");
                if(!replaying&&store.ReadSupportUnlocks().Contains("ability.smoke_screen"))throw new InvalidOperationException("Smoke must remain locked before Steel Push first clear.");
                sourceChecked=true;Debug.Log("[SteelPushRefinerySource] result=Passed logical="+map.OperationMapId+" logicalHash="+map.ContentHash+" physical="+map.SourceOperationMapId+" hash="+map.SourceContentHash+" runKind="+runtime.RunKind+" SmokeRequired=False");
            }
            if(em.HasComponent<CampaignMissionSteelPushState>(root))
            {
                var reserve=em.GetComponentData<CampaignMissionSteelPushState>(root);
                if(ShortageFixture&&!shortageInitialized&&reserve.Initialized!=0)
                {
                    var faultStorage=em.GetComponentData<BuildingResourceStorageComponent>(reserve.FuelReserve);faultStorage.StoredFuelBarrels=40;faultStorage.Version++;em.SetComponentData(reserve.FuelReserve,faultStorage);shortageInitialized=true;
                    Debug.Log("[SteelPushShortageFixture] setup=InjectedFuelFault usable=0 protected=40 outcome=not-injected health=not-injected input=normal-manual nativeCombat=required");
                }
                lowestFuel=Mathf.Min(lowestFuel,reserve.LowestUsableFuel);
                if(reserve.Initialized!=0&&em.Exists(reserve.FuelReserve))
                {var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve.FuelReserve);if(storage.StoredFuelBarrels<39.999f||storage.CivilianFuelReserveBarrels!=40)throw new InvalidOperationException("Civilian floor violated.");}
            }
            if(!em.HasBuffer<CampaignMissionDefenseMember>(root))return;
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
            {
                if(!em.Exists(member.Entity)||!em.HasComponent<LocalTransform>(member.Entity))continue;
                var p=em.GetComponentData<LocalTransform>(member.Entity).Position;var position=new Vector3(p.x,p.y,p.z);
                if(member.FactionId==1&&em.HasComponent<UnitSourcePrefabKey>(member.Entity)&&em.GetComponentData<UnitSourcePrefabKey>(member.Entity).Value.ToString().Contains("Tank"))
                {
                    if(priorPositions.TryGetValue(member.Entity,out var prior))distances[member.Entity]=distances.GetValueOrDefault(member.Entity)+Vector2.Distance(new Vector2(position.x,position.z),new Vector2(prior.x,prior.z));
                    priorPositions[member.Entity]=position;
                }
                if(member.FactionId!=1&&em.HasComponent<UnitSourcePrefabKey>(member.Entity)&&em.GetComponentData<UnitSourcePrefabKey>(member.Entity).Value.ToString().Contains("APC"))
                {var range=commandPaths.TryGetValue(member.Entity,out var priorRange)?priorRange:new Vector2(position.x,position.x);range.x=Mathf.Min(range.x,position.x);range.y=Mathf.Max(range.y,position.x);commandPaths[member.Entity]=range;}
                if(member.FactionId!=1&&em.HasComponent<UnitHealth>(member.Entity))
                {var health=em.GetComponentData<UnitHealth>(member.Entity);if(health.Current<health.Max&&arrivalClock==0){arrivalClock=facts.ElapsedMilliseconds;Debug.Log("[SteelPushEnemyArrival] firstCombatClock="+arrivalClock+" position="+position);}}
            }
        }
        private static void ValidateRefineryJourney()
        {
            var profile=inputSave.LoadProfile();
            int smokeCount=store.ReadSupportUnlocks().Count(x=>x=="ability.smoke_screen");
            int expectedCredits=startingCredits+(SavedReplay?300:9000+(replaying?300:0)),expectedXp=startingXp+(SavedReplay?0:2000);
            if(!sourceChecked||profile.credits!=expectedCredits||profile.commanderXp!=expectedXp||smokeCount!=1)throw new InvalidOperationException($"Steel Push settlement mismatch: source={sourceChecked} replay={replaying} Credits={profile.credits}/{expectedCredits} XP={profile.commanderXp}/{expectedXp} Smoke={smokeCount}.");
            if(replaying&&store.ReadAll().Single(x=>x.missionId==Game.Missions.Contracts.CampaignMissionSequence.SteelPush).successfulReplayCount!=startingReplayCount+1)throw new InvalidOperationException("Missing native successful replay receipt.");
            if(priorPositions.Count!=3||(!ShortageFixture&&distances.Values.Sum()<2))throw new InvalidOperationException("Three canonical tanks lack counterforce movement evidence.");
            if(arrivalClock<=0||commandPaths.Count!=2||commandPaths.Values.Any(x=>x.y>=875))throw new InvalidOperationException("Missing native enemy combat or command-vehicle interception before Relay.");
            if(!socketSeen)throw new InvalidOperationException("Missing native depot vegetation socket evidence.");
            var em=World.DefaultGameObjectInjectionWorld.EntityManager;using var hidden=em.CreateEntityQuery(typeof(OperationMapMissionPresentationHiddenTag));
            if(hidden.CalculateEntityCount()!=0)throw new InvalidOperationException("Mission scenery socket did not restore on Campaign return.");
            Debug.Log("[SteelPushRefineryJourney] result=Passed replay="+replaying+" shortageFaultFixture="+ShortageFixture+" CreditsDelta="+(profile.credits-startingCredits)+" XPDelta="+(profile.commanderXp-startingXp)+" Smoke=once tankDistances="+string.Join(",",distances.Values.Select(x=>x.ToString("F2")))+" fuelUsed="+(120-lowestFuel).ToString("F2")+" civilianFloor=40 enemyArrivalMs="+arrivalClock+" commandPaths="+string.Join(";",commandPaths.Values)+" commandVehicles=intercepted-before-Relay");
        }
        private static void ValidateVehicleFormation(EntityManager em,Entity root,int faction,int expected)
        {
            var positions=new List<Vector2>();
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
                if(member.FactionId==faction&&em.Exists(member.Entity)&&em.HasComponent<UnitSourcePrefabKey>(member.Entity)&&em.GetComponentData<UnitSourcePrefabKey>(member.Entity).Value.ToString().Contains("Veh_"))
                {var p=em.GetComponentData<LocalTransform>(member.Entity).Position;positions.Add(new Vector2(p.x,p.z));}
            if(positions.Count!=expected)throw new InvalidOperationException("Missing canonical vehicle roster at launch: faction="+faction);
            float separation=float.MaxValue;
            for(int i=0;i<positions.Count;i++)for(int j=i+1;j<positions.Count;j++)separation=Mathf.Min(separation,Vector2.Distance(positions[i],positions[j]));
            if(separation<8)throw new InvalidOperationException("Starting vehicle formation overlaps: faction="+faction+" separation="+separation);
            Debug.Log("[SteelPushVehicleFormation] result=Passed faction="+faction+" vehicles="+expected+" minimumSeparation="+separation.ToString("F2")+" positions="+string.Join(";",positions));
        }
        private static void DriveManual(EntityManager em,Entity root)
        {
            if(!Negative&&!ShortageFixture&&DriveManualReadiness())return;
            if(touch==null){touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Public touch device unavailable.");}
            touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<1)return;
            if(UiShellRuntimeGateway.ReadAriaPlay().Active)throw new InvalidOperationException("ARIA is active during manual journey.");
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();if(view==null||!view.IsPresentationVisible)return;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(guidance.GuidanceId==65001){Click(view.ContinueButton);lastInput=EditorApplication.timeSinceStartup;return;}
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))
            {Click(view.ContinueButton);return;}
            if(target.Moving||target.BattleAction==UiTutorialBattleAction.Watch)return;
            int key=guidance.GuidanceId*2+(target.NeedsSelection?0:1);
            if(lastManualGuidance!=key){lastManualGuidance=key;manualFocused=false;Shot("manual-"+key);}
            if(!manualFocused){Click(view.ShowMeButton);manualFocused=true;return;}
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();var camera=Camera.main;
            if(controls==null||camera==null||!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state))return;
            if(!target.NeedsSelection&&target.BattleAction==UiTutorialBattleAction.Hold){if(Click(controls.HoldButton)){manualActions++;lastInput=EditorApplication.timeSinceStartup;}return;}
            var mode=target.NeedsSelection?TacticalCommandMode.Select:target.BattleAction==UiTutorialBattleAction.Attack?TacticalCommandMode.Attack:TacticalCommandMode.Move;
            if(state.ActiveCommandMode!=mode){Click(mode==TacticalCommandMode.Select?controls.SelectButton:mode==TacticalCommandMode.Attack?controls.AttackButton:controls.MoveButton);return;}
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
                    if(!camera.pixelRect.Contains(p)||selectionHits.Count>0){Click(view.ShowMeButton);return;}
                }
                if(Vector2.Distance(manualPoint,from)>1f){manualPoint=from;manualPointStableAt=Time.unscaledTime;return;}
                if(Time.unscaledTime-manualPointStableAt<.5f)return;
                if(touch.TryGesture(from,to,.5f,.9f,Time.unscaledTime))
                {manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[SteelPushManual] normalTouch=selection-drag units="+target.RequiredSelectionCount);}
                return;
            }
            var world=target.NeedsSelection?target.Selection+Vector3.up:target.Destination;
            var point=camera.WorldToScreenPoint(world);var hits=new List<RaycastResult>();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            // Editor update callbacks report the window size, while the camera
            // and input device use the selected Game View render resolution.
            if(point.z<=0||!camera.pixelRect.Contains(point)||hits.Count>0){Click(view.ShowMeButton);return;}
            if(Vector2.Distance(manualPoint,point)>1f){manualPoint=point;manualPointStableAt=Time.unscaledTime;return;}
            if(Time.unscaledTime-manualPointStableAt<.5f)return;
            if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime))
            {manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[SteelPushManual] normalTouch="+mode+" world="+world);}
        }
        private static bool DriveManualReadiness()
        {
            var aria=UiShellRuntimeGateway.ReadAriaPlay();
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(manualReadinessStage==0)
            {
                var buttons=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude);
                var confirm=buttons.FirstOrDefault(x=>x.name=="ConfirmWatchAria"&&x.IsInteractable());
                if(confirm!=null){if(Click(confirm))manualReadinessStage=1;}
                else Click(buttons.FirstOrDefault(x=>x.name=="WatchAriaPlay"&&x.IsInteractable()));
                return true;
            }
            if(manualReadinessStage==1)
            {
                if(!aria.Active)return true;
                if(aria.Actions!=0)throw new InvalidOperationException("Manual handback check allowed ARIA to issue an action.");
                Shot("aria-stop-visible");
                if(Click(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude).FirstOrDefault(x=>x.name=="WatchAriaPlay"&&x.IsInteractable()))){manualReadinessStage=2;lastInput=EditorApplication.timeSinceStartup;}
                return true;
            }
            if(manualReadinessStage==2)
            {
                if(aria.Active)return true;
                Debug.Log("[SteelPushAriaHandback] result=Passed publicPlayStop=True actions=0 manualControl=restored");
                manualReadinessStage=3;lastInput=EditorApplication.timeSinceStartup;return true;
            }
            if(manualReadinessStage==3)
            {
                if(EditorApplication.timeSinceStartup-lastInput<1)return true;
                if(negativeMapPhase<3){FocusNegativeDestination(new Vector3(824,0,661.5f));return true;}
                if(EditorApplication.timeSinceStartup-lastInput<2)return true;
                Shot("reserve-yard");manualReadinessStage=4;return true;
            }
            return false;
        }
    }
}
