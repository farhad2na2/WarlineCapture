#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        static bool transportCruiseParked,transportCruisePngDone,transportCruiseRolling;
        static int transportCruiseStep,transportCruiseBaseline,transportCruiseRequest,transportCruiseTouchFrame,transportCruiseReceiptFrame,transportCruiseStable;
        static uint transportCruiseReceipt,transportCruiseGesture,transportCruiseFollowGesture;
        static float transportCruiseFuelBefore;static double transportCruiseStarted;
        static int2 transportCruiseGoal;static Vector3 transportCruiseWorld;
        static Entity transportCruiseActor;static string transportCruiseFailure;
        static Vector3 transportCruiseParkedWorld,transportCruisePreviousStableWorld;
        static int transportCruiseLastStableFrame=-1;
        static bool transportParkedFollowPngDone;static int transportParkedStable,transportParkedLastFrame;
        static Vector3 transportParkedPriorPosition,transportParkedLastPosition;static Quaternion transportParkedPriorRotation,transportParkedLastRotation;
        static float transportParkedPriorFov,transportParkedPriorSize,transportParkedLastFov;static bool transportParkedPriorOrtho,transportParkedPriorZoomIn,transportParkedPriorZoomOut;
        static uint transportParkedFollowGesture,transportParkedExitGesture;
        static bool transportParkedSnapshotCaptured;
        static Vector3 transportParkedNativePosition;static Quaternion transportParkedNativeRotation;
        static float transportParkedNativeFov,transportParkedNativeSize;static bool transportParkedNativeOrtho;
        [Serializable] sealed class TransportCruiseRow
        {
            public string stage,attempt,stable,reason="",anchor="anchor.skirmish.db.staging_player",transformSource="UnparentedLocalTransform";
            public string layoutId,layoutContentHash,padId,padKind,nativeResultKind;
            public string campaignSourceHash,bindingSourceHash,fuelScope="FactionPooledFuel";
            public bool aircraftFuelSpendAttributed=false;
            public uint parkedFollowGesture,parkedExitGesture;
            public Vector3 cameraPosition;public Quaternion cameraRotation;public float cameraFieldOfView,cameraOrthoSize;
            public bool cameraOrthographic,zoomControlsObserved,zoomInInteractable,zoomOutInteractable;
            public Vector3 nativeRestorePosition;public Quaternion nativeRestoreRotation;public float nativeRestoreFov,nativeRestoreSize;
            public bool nativeRestoreOrthographic;public byte nativeRestoreValid;public int nativeFollowEnteredFrame;
            public int padFaction,resultActorIndex,resultActorVersion,resultGoalX,resultGoalZ,resultAirUnits,resultRejection;
            public int consumptionCellX,consumptionCellZ;public byte consumptionInitialized,consumptionEnabled;
            public float airFuelPerCell,parkedX,parkedZ,horizontalFromParked,horizontalStep;
            public int frame,actorIndex,actorVersion,goalX,goalZ,request,baseline,receiptFrame,touchFrame,step;
            public uint reservation,receipt,gesture,followGesture,completed,dispatched,generation;
            public int device;public float timeScale,fuel,fuelBefore,admissionFuel,planningFuel,worldX,worldZ,actorX,actorY,actorZ,cruiseY;
            public byte airborne,takeoff,landing,returning,cruiseInitialized,followEnabled,panLocked;
            public bool verified,issued,normalBudget=true,fullMissionCertified=false;
            public SkirmishCameraActorMeasurement framing;
        }
        static void ResetNormalTransportCruise()
        {
            if(File.Exists(planeCanaryOutput+".cruise.jsonl"))throw new InvalidOperationException("Fresh cruise evidence path required");
            using(File.Open(planeCanaryOutput+".cruise.jsonl",FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}
            transportCruiseParked=transportCruisePngDone=transportCruiseRolling=false;transportCruiseStep=transportCruiseStable=0;
            transportCruiseReceipt=transportCruiseGesture=transportCruiseFollowGesture=0;transportCruiseStarted=0;transportCruiseFailure=null;
            transportCruiseActor=Entity.Null;AriaCommandEvidence.Recorded-=ObserveNormalTransportCruiseReceipt;
            transportCruiseLastStableFrame=-1;transportCruiseParkedWorld=transportCruisePreviousStableWorld=Vector3.zero;
            transportParkedFollowPngDone=transportParkedSnapshotCaptured=false;transportParkedStable=0;transportParkedLastFrame=-1;transportParkedFollowGesture=transportParkedExitGesture=0;
            AriaCommandEvidence.Recorded+=ObserveNormalTransportCruiseReceipt;
        }
        static TransportCruiseRow ReadNormalTransportCruise(EntityManager em,NormalPlaneLineageRow paid,string stage)
        {
            var actor=new Entity{Index=paid.actorIndex,Version=paid.actorVersion};
            if(actor==Entity.Null||!em.Exists(actor)||em.HasComponent<Parent>(actor)||!em.HasComponent<UnitAirComponent>(actor)||
                !em.HasComponent<LocalTransform>(actor)||!em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0)
                throw new InvalidOperationException("Cruise exact healthy unparented native Plane lost");
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            if(own.FactionId!=1||own.CapacityReleased!=0||own.ReservationId!=paid.reservation||own.SessionId.ToString()!=planeCanaryAttempt||
                em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role!=SkirmishRoleKind.TransportPlane)
                throw new InvalidOperationException("Cruise paid Plane actor ownership/role changed");
            var air=em.GetComponentData<UnitAirComponent>(actor);var position=em.GetComponentData<LocalTransform>(actor).Position;
            var consumption=em.GetComponentData<UnitFuelConsumption>(actor);var consumptionState=em.GetComponentData<UnitFuelConsumptionState>(actor);
            if(!UiShellRuntimeGateway.TryReadSkirmish(out var model))throw new InvalidOperationException("Native Fuel read model unavailable");
            var row=new TransportCruiseRow{stage=stage,attempt=planeCanaryAttempt,stable=own.StableObjectId.ToString(),reservation=paid.reservation,
                actorIndex=actor.Index,actorVersion=actor.Version,frame=Time.frameCount,step=transportCruiseStep,timeScale=Time.timeScale,
                fuel=model.OwnAirFuel,fuelBefore=transportCruiseFuelBefore,goalX=transportCruiseGoal.x,goalZ=transportCruiseGoal.y,
                worldX=transportCruiseWorld.x,worldZ=transportCruiseWorld.z,actorX=position.x,actorY=position.y,actorZ=position.z,
                airborne=air.Airborne,takeoff=air.TakeoffRolling,landing=air.LandingRolling,returning=air.ReturningHome,
                cruiseY=air.FixedWingCruiseY,cruiseInitialized=air.FixedWingCruiseYInitialized,request=transportCruiseRequest,baseline=transportCruiseBaseline,
                receipt=transportCruiseReceipt,receiptFrame=transportCruiseReceiptFrame,touchFrame=transportCruiseTouchFrame,
                gesture=transportCruiseGesture,followGesture=transportCruiseFollowGesture,
                device=playerResumeTouch.DeviceId,generation=playerResumeTouch.Generation,completed=playerResumeTouch.CompletedGestures,dispatched=playerResumeTouch.DispatchedGestures};
            row.parkedX=transportCruiseParkedWorld.x;row.parkedZ=transportCruiseParkedWorld.z;
            row.horizontalFromParked=math.distance(new float2(position.x,position.z),new float2(row.parkedX,row.parkedZ));
            row.horizontalStep=math.distance(new float2(position.x,position.z),new float2(transportCruisePreviousStableWorld.x,transportCruisePreviousStableWorld.z));
            row.airFuelPerCell=consumption.AirFuelPerCell;row.consumptionEnabled=consumption.Enabled;row.consumptionInitialized=consumptionState.Initialized;
            row.consumptionCellX=consumptionState.LastCell.x;row.consumptionCellZ=consumptionState.LastCell.y;
            var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Actual native camera missing during transport lifecycle");
            row.cameraPosition=camera.transform.position;row.cameraRotation=camera.transform.rotation;row.cameraFieldOfView=camera.fieldOfView;
            row.cameraOrthoSize=camera.orthographicSize;row.cameraOrthographic=camera.orthographic;
            row.parkedFollowGesture=transportParkedFollowGesture;row.parkedExitGesture=transportParkedExitGesture;
            var rails=UnityEngine.Object.FindObjectsByType<MatchHudRightQuickRailView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            if(rails.Length==1&&rails[0].ZoomInButton!=null&&rails[0].ZoomOutButton!=null){row.zoomControlsObserved=true;
                row.zoomInInteractable=rails[0].ZoomInButton.interactable;row.zoomOutInteractable=rails[0].ZoomOutButton.interactable;}
            using(var q=em.CreateEntityQuery(typeof(TacticalFollowCameraModeComponent)))if(q.CalculateEntityCount()==1){
                var follow=q.GetSingleton<TacticalFollowCameraModeComponent>();row.followEnabled=follow.Enabled;row.panLocked=follow.PanInputLocked;
                row.nativeRestorePosition=follow.RestorePosition;row.nativeRestoreRotation=new Quaternion(follow.RestoreRotation.value.x,follow.RestoreRotation.value.y,follow.RestoreRotation.value.z,follow.RestoreRotation.value.w);
                row.nativeRestoreFov=follow.RestoreFieldOfView;row.nativeRestoreSize=follow.RestoreOrthographicSize;row.nativeRestoreOrthographic=follow.RestoreOrthographic!=0;
                row.nativeRestoreValid=follow.RestorePoseValid;row.nativeFollowEnteredFrame=follow.ModeEnteredFrame;
                if(follow.Enabled!=0&&follow.BaseTargetEntity!=actor)throw new InvalidOperationException("Follow target differs from exact paid Plane");}
            return row;
        }
        static void BeginNormalTransportCruise(EntityManager em,NormalPlaneLineageRow paid)
        {
            var actor=new Entity{Index=paid.actorIndex,Version=paid.actorVersion};
            if(em.HasComponent<Parent>(actor))throw new InvalidOperationException("Parked world baseline requires unparented native root");
            transportCruiseParkedWorld=em.GetComponentData<LocalTransform>(actor).Position;
            transportCruisePreviousStableWorld=transportCruiseParkedWorld;
            WriteNormalTransportCruise(ReadNormalTransportCruise(em,paid,"ParkedWorldBaseline"));
        }
        static void WriteNormalTransportCruise(TransportCruiseRow row)=>File.AppendAllText(planeCanaryOutput+".cruise.jsonl",JsonUtility.ToJson(row)+"\n");
        static void ResolveFriendlyTransportFlight(EntityManager em,Entity session,Entity actor,TransportCruiseRow row)
        {
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var layout=AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishLayout_S005.asset");
            var catalog=SkirmishExpansionCatalogConfig.Load();
            if(catalog==null||!catalog.TryGetDefinition(setup.CatalogId,out var definition)||definition.DefinitionId!=setup.DefinitionId||
                definition.ContentVersion!=setup.ContentVersion||definition.MapLayout!=layout||definition.CampaignMap==null||
                layout==null||layout.WorldBinding.SourceHash!=definition.CampaignMap.ContentHash)
                throw new InvalidOperationException("Typed friendly destination does not match actual packaged Campaign source/layout binding");
            if(layout==null||setup.LayoutId!=layout.LayoutId||setup.OperationMapId!=layout.OperationMapId||!layout.HasWorldBinding||
                !layout.TryGetAnchor("staging.player",out var anchor)||anchor.AnchorId!="anchor.skirmish.db.staging_player"||
                !layout.TryGetPad(1,SkirmishLegalPadKind.GroundStaging,out var pad)||pad.AnchorId!=anchor.AnchorId||pad.RoleId!="staging.player"||
                !layout.TryProjectLocalToMap(anchor.WorldX,anchor.WorldZ,out float x,out float z))
                throw new InvalidOperationException("Friendly typed staging source-fit unavailable");
            using(var q=em.CreateEntityQuery(typeof(GridConfig))){
                if(q.CalculateEntityCount()!=1)throw new InvalidOperationException("Unique native flight Grid required");
                var grid=q.GetSingleton<GridConfig>();transportCruiseWorld=new Vector3(x,grid.Origin.y,z);transportCruiseGoal=GridUtils.WorldToCell(grid,transportCruiseWorld);
                if(transportCruiseGoal.x<0||transportCruiseGoal.y<0||transportCruiseGoal.x>=grid.Width||transportCruiseGoal.y>=grid.Height)
                    throw new InvalidOperationException("Friendly staging target outside actual native grid");
                var consumption=em.GetComponentData<UnitFuelConsumption>(actor);var air=em.GetComponentData<UnitAirComponent>(actor);
                if(consumption.Enabled==0||consumption.AirFuelPerCell<=0||air.UsesRunway==0)throw new InvalidOperationException("Native runway/Fuel consumption unavailable");
                var delta=transportCruiseGoal-em.GetComponentData<UnitGrid>(actor).Cell;float cells=math.abs(delta.x)+math.abs(delta.y);
                float speed=em.GetComponentData<UnitMove>(actor).Speed;float radius=math.max(.01f,speed)/math.radians(45);
                float runway=math.distance(air.RunwayTakeoffPosition,air.RunwayLandingPosition),straight=math.max(grid.CellSize*10,speed*1.5f);
                float approach=math.max(math.max(grid.CellSize*24,runway*1.25f),straight+radius*1.5f+grid.CellSize*6);
                row.admissionFuel=cells*consumption.AirFuelPerCell;
                row.planningFuel=math.ceil((2*cells+math.SQRT2*(2*runway+4*approach+2*math.PI*radius)/grid.CellSize)*consumption.AirFuelPerCell)+10;
                row.goalX=transportCruiseGoal.x;row.goalZ=transportCruiseGoal.y;row.worldX=x;row.worldZ=z;
                row.layoutId=layout.LayoutId;row.layoutContentHash=layout.ContentHash;row.padId=pad.PadId;row.padKind=pad.Kind.ToString();row.padFaction=pad.FactionId;
                row.campaignSourceHash=definition.CampaignMap.ContentHash;row.bindingSourceHash=layout.WorldBinding.SourceHash;
            }
        }
        static void ObserveNormalTransportCruiseReceipt(string operation,uint receipt,bool verified)
        {
            if(!transportCruiseParked||transportCruiseStep!=2)return;
            if(operation!="Move"||!verified||receipt==0||transportCruiseReceipt!=0||transportCruiseGesture==0){transportCruiseFailure="Unexpected/rejected/duplicate native Move receipt";return;}
            var em=World.DefaultGameObjectInjectionWorld.EntityManager;
            using(var q=em.CreateEntityQuery(typeof(UnitMoveOrderQueueComponent),typeof(UnitMoveOrderResultElement))){
                if(q.CalculateEntityCount()!=1){transportCruiseFailure="Native Move queue missing";return;}
                transportCruiseRequest=q.GetSingleton<UnitMoveOrderQueueComponent>().LastRequestId;
                if(transportCruiseRequest!=transportCruiseBaseline+1){transportCruiseFailure="Public Move did not issue exactly one native request";return;}}
            transportCruiseReceipt=receipt;transportCruiseReceiptFrame=Time.frameCount;
        }
        static void TickNormalTransportCruise(EntityManager em,Entity session,NormalPlaneLineageRow paid)
        {
            if(transportCruiseStarted==0)transportCruiseStarted=EditorApplication.timeSinceStartup;
            double now=EditorApplication.timeSinceStartup;
            if(now-transportCruiseStarted>240)throw new InvalidOperationException("FriendlyStagingCruiseDurationNotEstablished: bounded normal Fuel/input/flight probe expired");
            if(transportCruiseFailure!=null)throw new InvalidOperationException(transportCruiseFailure);
            transportCruiseActor=new Entity{Index=paid.actorIndex,Version=paid.actorVersion};
            var row=ReadNormalTransportCruise(em,paid,"CruiseNativeSample");WriteNormalTransportCruise(row);
            if(transportCruiseStep>=2&&row.takeoff!=0)transportCruiseRolling=true;
            if(transportCruiseStep>=2&&(row.returning!=0||row.landing!=0)&&!transportCruisePngDone)
                throw new InvalidOperationException("FriendlyStagingCruiseDurationNotEstablished: native return/landing preceded stable capture");
            if(playerResumeTouch.IsBusy||now<playerResumeDue){transportCruiseStable=transportParkedStable=0;transportCruiseLastStableFrame=transportParkedLastFrame=-1;return;}
            var drawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if(transportCruiseStep==0){
                ResolveFriendlyTransportFlight(em,session,transportCruiseActor,row);row.stage="FriendlyFlightPlan";WriteNormalTransportCruise(row);
                if(row.fuel<row.planningFuel){
                    if(!TickFixedWingFuelFleetThroughInput(em,session,now,drawer,controls))return;
                    if(!PauseFixedWingMaterialProductionThroughInput(em,now,controls))return;
                    if(!TickFixedWingLocalFuelSupplyThroughInput(em,now))return;
                    ResetFixedWingLocalFuelSupplyProbe();return;
                }
                if(drawer!=null&&drawer.IsOpen){PlayerResumeTap(drawer.CloseButton);return;}
                if(!SelectFixedWingCard(em,session,transportCruiseActor,now))return;
                using(var selected=em.CreateEntityQuery(typeof(SelectedUnitTag)))if(selected.CalculateEntityCount()!=1||selected.GetSingletonEntity()!=transportCruiseActor)return;
                transportCruiseStep=10;return;
            }
            if(transportCruiseStep==10){
                if(!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command))return;
                if(command.ActiveCommandMode!=TacticalCommandMode.None&&command.ActiveCommandMode!=TacticalCommandMode.Select){PlayerResumeTap(controls?.SelectButton);return;}
                if(row.followEnabled!=0||row.panLocked!=0||!row.zoomControlsObserved)throw new InvalidOperationException("Parked Follow baseline requires ordinary unlocked camera and visible zoom controls");
                if(Time.frameCount==transportParkedLastFrame)return;
                bool still=transportParkedLastFrame==Time.frameCount-1&&Vector3.Distance(row.cameraPosition,transportParkedLastPosition)<.01f&&
                    Quaternion.Angle(row.cameraRotation,transportParkedLastRotation)<.05f&&Mathf.Abs(row.cameraFieldOfView-transportParkedLastFov)<.01f;
                transportParkedLastFrame=Time.frameCount;transportParkedLastPosition=row.cameraPosition;transportParkedLastRotation=row.cameraRotation;transportParkedLastFov=row.cameraFieldOfView;
                transportParkedStable=still?transportParkedStable+1:0;if(transportParkedStable<15)return;
                transportParkedPriorPosition=row.cameraPosition;transportParkedPriorRotation=row.cameraRotation;transportParkedPriorFov=row.cameraFieldOfView;
                transportParkedPriorSize=row.cameraOrthoSize;transportParkedPriorOrtho=row.cameraOrthographic;
                transportParkedPriorZoomIn=row.zoomInInteractable;transportParkedPriorZoomOut=row.zoomOutInteractable;
                if(!TryTapNormalPaidPlanePresentedFollow())return;
                transportParkedFollowGesture=playerResumeTouch.DispatchedGestures;row.parkedFollowGesture=transportParkedFollowGesture;
                row.dispatched=playerResumeTouch.DispatchedGestures;row.stage="PublicParkedFollowTouch";WriteNormalTransportCruise(row);
                transportParkedLastFrame=-1;transportParkedStable=0;transportCruiseStep=11;return;
            }
            if(transportCruiseStep==11){
                if(!transportParkedSnapshotCaptured&&row.followEnabled==1&&row.panLocked==1){
                    if(row.nativeRestoreValid!=1)throw new InvalidOperationException("Public Follow did not retain its native prior-pose snapshot");
                    if(Vector3.Distance(row.nativeRestorePosition,transportParkedPriorPosition)>.05f||Quaternion.Angle(row.nativeRestoreRotation,transportParkedPriorRotation)>.1f||
                       Mathf.Abs(row.nativeRestoreFov-transportParkedPriorFov)>.05f||row.nativeRestoreOrthographic!=transportParkedPriorOrtho||
                       Mathf.Abs(row.nativeRestoreSize-transportParkedPriorSize)>.05f)throw new InvalidOperationException("Native Follow restore snapshot differs from settled pre-Touch camera baseline");
                    transportParkedNativePosition=row.nativeRestorePosition;transportParkedNativeRotation=row.nativeRestoreRotation;
                    transportParkedNativeFov=row.nativeRestoreFov;transportParkedNativeSize=row.nativeRestoreSize;transportParkedNativeOrtho=row.nativeRestoreOrthographic;
                    transportParkedSnapshotCaptured=true; // Keep both independent baselines intact for Exit verification.
                    row.stage="NativeParkedFollowEntered";WriteNormalTransportCruise(row);
                }
                if(Time.frameCount==transportParkedLastFrame)return;
                bool settled=transportParkedLastFrame==Time.frameCount-1&&Vector3.Distance(row.cameraPosition,transportParkedLastPosition)<.01f&&
                    Quaternion.Angle(row.cameraRotation,transportParkedLastRotation)<.05f&&Mathf.Abs(row.cameraFieldOfView-transportParkedLastFov)<.01f;
                transportParkedLastFrame=Time.frameCount;transportParkedLastPosition=row.cameraPosition;transportParkedLastRotation=row.cameraRotation;transportParkedLastFov=row.cameraFieldOfView;
                var sample=SkirmishCameraFramingMeasurement.Capture(em,transportCruiseActor,Camera.main);
                if(!settled||row.followEnabled!=1||row.panLocked!=1||row.airborne!=0||row.takeoff!=0||row.landing!=0||row.returning!=0||row.horizontalFromParked>1.5f||
                   !sample.Valid||sample.WidthFraction>.4501f||sample.HeightFraction>.4501f||
                   sample.ProjectedPixels.xMin<sample.CameraPixels.xMin+8||sample.ProjectedPixels.xMax>sample.CameraPixels.xMax-8||
                   sample.ProjectedPixels.yMin<sample.CameraPixels.yMin+8||sample.ProjectedPixels.yMax>sample.CameraPixels.yMax-8){transportParkedStable=0;return;}
                transportParkedStable++;row.stage="ParkedFollowStable";row.framing=sample;WriteNormalTransportCruise(row);
                if(transportParkedStable<15)return;
                QueueNormalPlaneCamera("parked-follow-transport",transportCruiseActor,paid.reservation);transportCruiseStep=12;return;
            }
            if(transportCruiseStep==12){
                if(!transportParkedFollowPngDone)return;
                if(!TryTapNormalPaidPlanePresentedFollow())return;
                transportParkedExitGesture=playerResumeTouch.DispatchedGestures;row.parkedExitGesture=transportParkedExitGesture;
                row.dispatched=playerResumeTouch.DispatchedGestures;row.stage="PublicParkedExitTouch";WriteNormalTransportCruise(row);transportCruiseStep=13;return;
            }
            if(transportCruiseStep==13){
                if(row.followEnabled!=0||row.panLocked!=0||Vector3.Distance(row.cameraPosition,transportParkedPriorPosition)>.05f||
                   Quaternion.Angle(row.cameraRotation,transportParkedPriorRotation)>.1f||Mathf.Abs(row.cameraFieldOfView-transportParkedPriorFov)>.05f||
                   row.cameraOrthographic!=transportParkedPriorOrtho||Mathf.Abs(row.cameraOrthoSize-transportParkedPriorSize)>.05f||
                   Vector3.Distance(row.cameraPosition,transportParkedNativePosition)>.05f||Quaternion.Angle(row.cameraRotation,transportParkedNativeRotation)>.1f||
                   Mathf.Abs(row.cameraFieldOfView-transportParkedNativeFov)>.05f||row.cameraOrthographic!=transportParkedNativeOrtho||
                   Mathf.Abs(row.cameraOrthoSize-transportParkedNativeSize)>.05f||
                   !row.zoomControlsObserved||row.zoomInInteractable!=transportParkedPriorZoomIn||row.zoomOutInteractable!=transportParkedPriorZoomOut)return;
                row.stage="ParkedFollowExitedRestored";WriteNormalTransportCruise(row);transportCruiseStep=1;return;
            }
            if(transportCruiseStep==1){
                if(!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)||command.ActiveCommandMode!=TacticalCommandMode.Move){PlayerResumeTap(controls?.MoveButton);return;}
                using(var q=em.CreateEntityQuery(typeof(UnitMoveOrderQueueComponent)))if(q.CalculateEntityCount()==1)transportCruiseBaseline=q.GetSingleton<UnitMoveOrderQueueComponent>().LastRequestId;else throw new InvalidOperationException("Native Move queue absent before Touch");
                transportCruiseFuelBefore=row.fuel;
                if(!PlayerResumeWorldPointTap(transportCruiseWorld,now))return;
                transportCruiseGesture=playerResumeTouch.DispatchedGestures;transportCruiseTouchFrame=Time.frameCount;transportCruiseStep=2;
                row.stage="PublicFriendlyMoveTouch";row.gesture=transportCruiseGesture;row.dispatched=playerResumeTouch.DispatchedGestures;row.touchFrame=transportCruiseTouchFrame;row.baseline=transportCruiseBaseline;row.fuelBefore=transportCruiseFuelBefore;WriteNormalTransportCruise(row);return;
            }
            if(transportCruiseStep==2){
                if(transportCruiseReceipt==0)return;
                bool found=false;using(var q=em.CreateEntityQuery(typeof(UnitMoveOrderResultElement))) {
                    if(q.CalculateEntityCount()!=1)throw new InvalidOperationException("Native Move result queue missing");
                    foreach(var result in em.GetBuffer<UnitMoveOrderResultElement>(q.GetSingletonEntity(),true))if(result.RequestId==transportCruiseRequest){
                        row.nativeResultKind=result.Kind.ToString();row.resultActorIndex=result.Entity.Index;row.resultActorVersion=result.Entity.Version;
                        row.resultGoalX=result.Goal.x;row.resultGoalZ=result.Goal.y;row.resultAirUnits=result.AirUnits;row.resultRejection=result.RejectionReasonCode;
                        if(found||result.Entity!=transportCruiseActor||math.any(result.Goal!=transportCruiseGoal)||result.Kind!=UnitMoveOrderRequestKind.GroupedManual||result.Issued==0||result.AirUnits!=1||result.RejectionReasonCode!=0)
                            throw new InvalidOperationException("Exact native public Plane Move result differs/rejected");found=true;}}
                if(!found)return;
                row.stage="NativeFriendlyMoveAccepted";row.receipt=transportCruiseReceipt;row.request=transportCruiseRequest;row.receiptFrame=transportCruiseReceiptFrame;row.verified=row.issued=true;WriteNormalTransportCruise(row);
                transportCruiseStep=3;return;
            }
            if(transportCruiseStep==3){
                if(!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command))return;
                if(command.ActiveCommandMode!=TacticalCommandMode.None&&command.ActiveCommandMode!=TacticalCommandMode.Select){PlayerResumeTap(controls?.SelectButton);return;}
                if(!SelectFixedWingCard(em,session,transportCruiseActor,now))return;
                if(!TryTapNormalPaidPlanePresentedFollow())return;
                transportCruiseFollowGesture=playerResumeTouch.DispatchedGestures;row.stage="PublicPlaneFollowTouch";row.followGesture=transportCruiseFollowGesture;row.dispatched=playerResumeTouch.DispatchedGestures;WriteNormalTransportCruise(row);transportCruiseStep=4;return;
            }
            if(transportCruiseStep==4){
                if(Time.frameCount==transportCruiseLastStableFrame)return;
                if(transportCruiseLastStableFrame>=0&&Time.frameCount!=transportCruiseLastStableFrame+1)transportCruiseStable=0;
                if(row.followEnabled!=1||row.panLocked!=1||row.airborne!=1||row.takeoff!=0||row.cruiseInitialized!=1||!transportCruiseRolling||row.fuel>=transportCruiseFuelBefore||math.abs(row.actorY-row.cruiseY)>1){transportCruiseStable=0;return;}
                if(row.horizontalFromParked<5||row.horizontalStep<.001f||row.consumptionEnabled!=1||row.consumptionInitialized!=1||row.airFuelPerCell<=0){transportCruiseStable=0;return;}
                var sample=SkirmishCameraFramingMeasurement.Capture(em,transportCruiseActor,Camera.main);
                if(!sample.Valid||sample.WidthFraction>.4501f||sample.HeightFraction>.4501f||
                   sample.ProjectedPixels.xMin<sample.CameraPixels.xMin+8||sample.ProjectedPixels.xMax>sample.CameraPixels.xMax-8||
                   sample.ProjectedPixels.yMin<sample.CameraPixels.yMin+8||sample.ProjectedPixels.yMax>sample.CameraPixels.yMax-8){transportCruiseStable=0;return;}
                transportCruiseStable++;transportCruiseLastStableFrame=Time.frameCount;transportCruisePreviousStableWorld=new Vector3(row.actorX,row.actorY,row.actorZ);
                row.stage="StableNativeCruise";row.framing=sample;WriteNormalTransportCruise(row);
                if(transportCruiseStable<15)return;
                QueueNormalPlaneCamera("cruising-transport",transportCruiseActor,paid.reservation);transportCruiseStep=5;return;
            }
            if(transportCruiseStep==5&&transportCruisePngDone){
                row.stage="CruiseCameraScopedComplete";WriteNormalTransportCruise(row);
                Debug.Log("[S005NormalPaidPlaneCruise] result=Passed scope=NormalPaidTransportCruiseCameraOnly fullMissionCertified=0 playerReady=0");
                EndNormalPaidPlaneCanary(false,"normalPaidTransportCruiseCameraOnly");
            }
        }
    }
}
#endif
