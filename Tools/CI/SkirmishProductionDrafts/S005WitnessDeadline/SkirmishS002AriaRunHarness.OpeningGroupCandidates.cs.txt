#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
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
        static bool openingGroupsRequested,openingGroupsStarted,openingGroupsDone;
        static bool measuredOpeningCandidates;
        static int measuredOpeningRuntimeErrors;
        static readonly List<string> measuredOpeningRuntimeErrorFacts=new List<string>();
        static string openingGroupsOutput,openingGroupsError,openingGroupsAttempt;
        static SkirmishOpeningGroupCandidateFrame openingGroupsOwner;
        sealed class OpeningGroupActor {public Entity Entity;public string Id;public uint Group;public Vector3 Position;}
        sealed class OpeningGroupCandidate {public uint Group;public float Yaw;public float Height;public Vector2 CameraOffset;public OpeningGroupActor[] Actors;public Vector3 Center;}

        // Explicit QA candidate views only: no profile/source assets changed,
        // no orders/selection, no automatic winner or shipping camera heuristic.
        public static void RunS005OpeningGroupViewCandidates()=>BeginS005OpeningCandidates(false);
        public static void RunS005MeasuredOpeningGroupViewCandidates()=>BeginS005OpeningCandidates(true);
        static void BeginS005OpeningCandidates(bool measured,bool placementWitness=false)
        {
            if(openingGroupsRequested||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Fresh idle candidate diagnostic required");
            string output=Environment.GetEnvironmentVariable("WARLINE_S005_OPENING_GROUP_OUTPUT");
            string allowed=Path.GetFullPath("Logs/SkirmishProduction")+Path.DirectorySeparatorChar;
            if(string.IsNullOrEmpty(output)||!Path.IsPathRooted(output)||!Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Fresh persistent absolute output under Logs/SkirmishProduction required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write)){}
            openingPlacementWitness=placementWitness;
            measuredOpeningCandidates=measured;measuredOpeningRuntimeErrors=0;measuredOpeningRuntimeErrorFacts.Clear();
            if(measured){Application.logMessageReceived+=ObserveMeasuredOpeningRuntimeLog;EditorApplication.quitting+=CompleteMeasuredOpeningRuntimeGate;}
            openingGroupsOutput=output;openingGroupsRequested=true;openingGroupsStarted=openingGroupsDone=false;openingGroupsError=null;openingGroupsAttempt=null;
            try{RunS005NormalStartupExitDiagnostic();if(placementWitness){deadline=EditorApplication.timeSinceStartup+480d;Debug.Log("[S005OpeningPlacementWitnessDeadline] totalBudgetSeconds=480 candidateBudgetSeconds=180 gameplayTimeoutChanged=0");}}catch{openingGroupsRequested=false;Application.logMessageReceived-=ObserveMeasuredOpeningRuntimeLog;EditorApplication.quitting-=CompleteMeasuredOpeningRuntimeGate;throw;}
        }
        static bool TickS005OpeningGroupCandidates(EntityManager em,Entity session)
        {
            if(!openingGroupsRequested)return false;
            if(openingGroupsError!=null)throw new InvalidOperationException(openingGroupsError);
            if(measuredOpeningCandidates&&measuredOpeningRuntimeErrors!=0)throw new InvalidOperationException("Measured opening runtime error gate failed: "+measuredOpeningRuntimeErrors);
            if(!openingGroupsStarted)
            {
                double now=EditorApplication.timeSinceStartup;
                if(deadline<=0||now>deadline)throw new InvalidOperationException("Original normal startup readiness deadline must pass before candidate views");
                deadline=openingPlacementWitness?Math.Min(deadline,now+180d):now+180d;openingGroupsStarted=true;
                openingGroupsOwner=new GameObject("S005OpeningGroupCandidateOwner").AddComponent<SkirmishOpeningGroupCandidateFrame>();
                openingGroupsOwner.StartCoroutine(CaptureS005OpeningGroupCandidates(openingGroupsOwner));return true;
            }
            if(!openingGroupsDone)return true;
            openingGroupsRequested=false;
            if(openingPlacementWitness)Debug.Log("[S005OpeningPlacementWitness] result=CapturedUnqualified views=4 geometryOnly=1 placementAccepted=0 occlusionAccepted=0 wrapperExitStillRequired=1");
            if(measuredOpeningCandidates)Debug.Log("[S005MeasuredOpeningCandidates] result=CapturedUnqualified views=4 profileChanged=0 commandsIssued=0 selectionChanged=0 runtimeGateAtQuitRequired=1");
            Debug.Log("[S005OpeningGroupCandidates] result=CapturedUnqualified views=4 profileChanged=0 commandsIssued=0 selectionChanged=0 wrapperExitStillRequired=1");
            return false; // Existing startup audit, Finish, restore and Exit remain owners.
        }
        static IEnumerator CaptureS005OpeningGroupCandidates(SkirmishOpeningGroupCandidateFrame owner)
        {
            OpeningGroupCandidate[] candidates=null;SkirmishMissionViewProfile profile=null;
            try
            {
                var world=World.DefaultGameObjectInjectionWorld;
                if(world==null||!world.IsCreated||!SkirmishLaunchProjection.TryGet(world.EntityManager,out var session,out _))throw new InvalidOperationException("Actual normal session missing");
                var em=world.EntityManager;var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                var record=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                if(native.CatalogId.ToString()!="S005"||native.IsLegacy!=0||!record.Matches(setup,native)||
                    !SkirmishMissionViewProfileApplication.TryResolve(em,out profile)||!profile.Matches(setup,record.Definition))throw new InvalidOperationException("Exact S005 source/layout/profile binding required");
                openingGroupsAttempt=native.SessionId.ToString();var actors=ReadActualS005OpeningGroups(em,native);
                if(measuredOpeningCandidates&&(!Mathf.Approximately(profile.Pitch,65)||!Mathf.Approximately(profile.NormalFieldOfView,46)))throw new InvalidOperationException("Recorded analytic QA requires actual pitch65/FOV46; no source profile rewrite");
                candidates=measuredOpeningCandidates?BuildS005MeasuredOpeningCandidates(actors):BuildS005OpeningGroupCandidates(actors,profile.Yaw);
                foreach(var candidate in candidates)if(!measuredOpeningCandidates)candidate.Height=profile.NormalHeight;
                WriteOpeningGroupFact(new JObject{["phase"]="SourceBoundCandidates",["attempt"]=openingGroupsAttempt,["definition"]=setup.DefinitionId,
                    ["setupHash"]=setup.SetupHash,["sourceContentHash"]=record.Definition.CampaignMap.ContentHash,["sourceIdentityHash"]=record.Definition.CampaignMap.SourceIdentityHash,
                    ["placementWitnessRequested"]=openingPlacementWitness,["profiler"]=openingPlacementWitness?ReadOpeningWitnessProfiler():null,
                    ["profileNormalHeight"]=profile.NormalHeight,["profileMinHeight"]=profile.MinHeight,["profileMaxHeight"]=profile.MaxHeight,
                    ["pitch"]=profile.Pitch,["fieldOfView"]=profile.NormalFieldOfView,["candidateYawScope"]=measuredOpeningCandidates?"Measured QA G1 28m121/301, G2 20m127/307; no automatic selection":"Actual profile heading and opposite; unqualified alternatives, no automatic selection",["analyticInputSha256"]=measuredOpeningCandidates?"2619ab959b645b668ee468962f2e6e132d5c76a5455bb10f830fb4e73c0d4377":""});
            }
            catch(Exception error){openingGroupsError=error.ToString();}
            if(openingGroupsError==null)for(int i=0;i<candidates.Length;i++)
            {
                Vector3 desired=default;Quaternion rotation=default;Camera camera=null;
                try
                {
                    var world=World.DefaultGameObjectInjectionWorld;var em=world.EntityManager;
                    if(!SkirmishLaunchProjection.TryGet(em,out var session,out _)||em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString()!=openingGroupsAttempt||!SkirmishLaunchProjection.IsSimulationActive(em)||!Mathf.Approximately(Time.timeScale,1))throw new InvalidOperationException("Actual normal attempt/speed lost");
                    var candidate=candidates[i];
                    var currentActors=ReadActualS005OpeningGroups(em,em.GetComponentData<SkirmishExpandedSessionComponent>(session)).Where(a=>a.Group==candidate.Group).ToArray();
                    if(currentActors.Length!=4||!currentActors.All(a=>candidate.Actors.Any(old=>old.Entity==a.Entity&&old.Id==a.Id)))throw new InvalidOperationException("Original group lineage changed before request");
                    candidate.Actors=currentActors;candidate.Center=currentActors.Aggregate(Vector3.zero,(sum,a)=>sum+a.Position)/currentActors.Length;
                    camera=Camera.main;if(camera==null)throw new InvalidOperationException("Actual normal camera missing");
                    rotation=Quaternion.Euler(profile.Pitch,candidate.Yaw,0);var forward=rotation*Vector3.forward;
                    Vector3 focus=candidate.Center;
                    if(measuredOpeningCandidates)
                    {
                        desired=new Vector3(candidate.Center.x+candidate.CameraOffset.x,candidate.Height,candidate.Center.z+candidate.CameraOffset.y);
                        focus=desired+forward*((candidate.Center.y-candidate.Height)/forward.y);
                    }
                    else desired=candidate.Center-forward*((candidate.Center.y-candidate.Height)/forward.y);
                    if(!SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(focus,candidate.Height,profile.Pitch,candidate.Yaw,out var ground))throw new InvalidOperationException("Actual group role focus projection refused");
                    using var requests=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));if(requests.CalculateEntityCount()!=1)throw new InvalidOperationException("Actual normal camera request owner missing");
                    em.SetComponentData(requests.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent{Requested=1,Smooth=1,UseExplicitPerspective=1,World=ground,
                        Perspective=new float4(candidate.Height,profile.Pitch,candidate.Yaw,profile.NormalFieldOfView),SmoothTimeSeconds=RuntimeCameraFocusRequestUtility.EnsureMissionIntroSmoothTime(2.25f)});
                }
                catch(Exception error){openingGroupsError=error.ToString();break;}
                int settled=0;double end=EditorApplication.timeSinceStartup+30;
                while(settled<3&&EditorApplication.timeSinceStartup<end)
                {
                    yield return new WaitForEndOfFrame();
                    if(camera!=null&&Vector3.Distance(camera.transform.position,desired)<.05f&&Quaternion.Angle(camera.transform.rotation,rotation)<.1f&&Mathf.Abs(camera.fieldOfView-profile.NormalFieldOfView)<.05f)settled++;else settled=0;
                }
                try
                {
                    if(settled<3)throw new InvalidOperationException("Normal camera did not settle to explicit candidate pose");
                    if(openingPlacementWitness)World.DefaultGameObjectInjectionWorld.EntityManager.CompleteAllTrackedJobs();
                    var world=World.DefaultGameObjectInjectionWorld;var em=world.EntityManager;
                    if(!SkirmishLaunchProjection.TryGet(em,out var session,out _)||em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString()!=openingGroupsAttempt)throw new InvalidOperationException("Attempt changed before EOF capture");
                    var candidate=candidates[i];var measured=new JArray();var posedWitness=new List<SkirmishCameraActorMeasurement>();
                    var currentActors=ReadActualS005OpeningGroups(em,em.GetComponentData<SkirmishExpandedSessionComponent>(session)).Where(a=>a.Group==candidate.Group).ToArray();
                    if(currentActors.Length!=4||!currentActors.All(a=>candidate.Actors.Any(old=>old.Entity==a.Entity&&old.Id==a.Id)))throw new InvalidOperationException("Original group lineage changed before capture");
                    candidate.Actors=currentActors;
                    foreach(var actor in candidate.Actors)
                    {
                        if(!em.Exists(actor.Entity)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor.Entity)||em.GetComponentData<SkirmishAttemptOwnedComponent>(actor.Entity).StableObjectId.ToString()!=actor.Id)throw new InvalidOperationException("Original actor replaced before capture");
                        var measurement=SkirmishCameraFramingMeasurement.Capture(em,actor.Entity,camera);
                        if(measuredOpeningCandidates&&(!measurement.Valid||!string.IsNullOrEmpty(measurement.Failure)||measurement.Frame!=Time.frameCount||measurement.StableActorId!=actor.Id))throw new InvalidOperationException("Actual same-frame original actor posed measurement invalid");
                        measured.Add(JObject.Parse(JsonUtility.ToJson(measurement)));
                        if(openingPlacementWitness)posedWitness.Add(measurement);
                    }
                    string png=openingGroupsOutput+".group"+candidate.Group+"-yaw"+candidate.Yaw.ToString("F0",System.Globalization.CultureInfo.InvariantCulture)+"-frame"+Time.frameCount+".png";
                    var texture=ScreenCapture.CaptureScreenshotAsTexture();if(texture==null)throw new InvalidOperationException("Actual EOF image unavailable");
                    byte[] bytes;try{bytes=texture.EncodeToPNG();}finally{UnityEngine.Object.Destroy(texture);}
                    if(bytes==null||bytes.Length<32)throw new InvalidOperationException("Actual EOF PNG empty");
                    using(var stream=new FileStream(png,FileMode.CreateNew,FileAccess.Write,FileShare.Read))stream.Write(bytes,0,bytes.Length);
                    WriteOpeningGroupFact(new JObject{["phase"]="CandidateEndOfFrame",["attempt"]=openingGroupsAttempt,["candidate"]=i,["group"]=candidate.Group,
                        ["yaw"]=candidate.Yaw,["requestedAbsoluteHeight"]=candidate.Height,["measuredQaCandidate"]=measuredOpeningCandidates,["activeUiRectangles"]=measuredOpeningCandidates?ReadMeasuredOpeningUiRectangles():null,["runtimeErrorsSoFar"]=measuredOpeningRuntimeErrors,["actualCameraPosition"]=EncodeGroupVector(camera.transform.position),["actualEuler"]=EncodeGroupVector(camera.transform.eulerAngles),["actualFieldOfView"]=camera.fieldOfView,
                        ["actualGroupCenterAtRequest"]=EncodeGroupVector(candidate.Center),["actorMeasurements"]=measured,["png"]=png,["pngSha256"]=OpeningGroupFileHash(png),
                        ["placementWitness"]=openingPlacementWitness?ReadOpeningPlacementWitness(em,session,candidate,camera,posedWitness):null,
                        ["sourceGeometryAabbRisks"]=ReadGroupSourceBoundsRisks(em,candidate,camera.transform.position),["occlusionPixelsAccepted"]=false});
                }
                catch(Exception error){openingGroupsError=error.ToString();break;}
            }
            if(openingGroupsError!=null)WriteOpeningGroupFact(new JObject{["phase"]="Failed",["error"]=openingGroupsError});
            openingGroupsDone=true;UnityEngine.Object.Destroy(owner.gameObject);
        }
        static OpeningGroupActor[] ReadActualS005OpeningGroups(EntityManager em,SkirmishExpandedSessionComponent native)
        {
            var result=new List<OpeningGroupActor>();var ids=new HashSet<string>(StringComparer.Ordinal);
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(SkirmishUnitRoleComponent),typeof(UnitHealth),typeof(LocalTransform));
            using var actors=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
                if(own.SessionId!=native.SessionId||own.FactionId!=1||role.Role!=SkirmishRoleKind.Rifle)continue;
                if(own.IsStructure!=0||own.ReservationId!=0||own.CapacityReleased!=0||role.Category!=SkirmishPopulationCategory.Infantry||!em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1||
                    em.HasComponent<Parent>(actor)||em.HasComponent<PostTransformMatrix>(actor)||em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                    em.HasComponent<UnitTransportParachuteDropComponent>(actor)||em.HasComponent<UnitTransportRopeDropComponent>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0)throw new InvalidOperationException("Original unmounted live own Rifle identity required");
                var membership=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor);var position=em.GetComponentData<LocalTransform>(actor).Position;
                if(membership.GroupId==0||membership.Domain!=SkirmishPopulationCategory.Infantry||!math.all(math.isfinite(position))||!ids.Add(own.StableObjectId.ToString()))throw new InvalidOperationException("Exact unique group/original/world position required");
                result.Add(new OpeningGroupActor{Entity=actor,Id=own.StableObjectId.ToString(),Group=membership.GroupId,Position=position});
            }
            return result.ToArray();
        }
        static OpeningGroupCandidate[] BuildS005OpeningGroupCandidates(OpeningGroupActor[] actors,float yaw)
        {
            if(!float.IsFinite(yaw))throw new InvalidOperationException("Actual authored finite heading required");
            var groups=actors.GroupBy(a=>a.Group).OrderBy(g=>g.Key).ToArray();
            if(groups.Length!=2||groups.Any(g=>g.Count()!=4))throw new InvalidOperationException("Actual two complete four-member original Rifle groups required");
            return groups.SelectMany(g=>new[]{yaw,Mathf.Repeat(yaw+180,360)}.Select(heading=>new OpeningGroupCandidate{
                Group=g.Key,Yaw=heading,Actors=g.ToArray(),Center=g.Aggregate(Vector3.zero,(sum,a)=>sum+a.Position)/g.Count()})).ToArray();
        }
        public static void RunS005OpeningGroupCandidateControls()
        {
            int controls=0;
            for(int mutation=0;mutation<=10;mutation++)
            {
                using var world=new World("OpeningGroupCandidateControl");var em=world.EntityManager;
                var native=new SkirmishExpandedSessionComponent{SessionId=new FixedString64Bytes("opening-group-control")};var actors=new Entity[8];
                for(int i=0;i<8;i++)
                {
                    var actor=em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishSharedActorTag),typeof(SkirmishArmyGroupMembershipComponent),typeof(SkirmishUnitRoleComponent),typeof(Faction),typeof(UnitHealth),typeof(LocalTransform));actors[i]=actor;
                    em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=native.SessionId,FactionId=1,StableObjectId=new FixedString64Bytes("original.rifle."+i)});
                    em.SetComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=(uint)(i/4+1),Domain=SkirmishPopulationCategory.Infantry});
                    em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Rifle,Category=SkirmishPopulationCategory.Infantry});
                    em.SetComponentData(actor,new Faction{Id=1});em.SetComponentData(actor,new UnitHealth{Current=100,Max=100});em.SetComponentData(actor,LocalTransform.FromPosition(500+i*2,0,500+i));
                }
                var first=actors[0];var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(first);
                if(mutation==1)em.DestroyEntity(first);
                if(mutation==2)em.SetComponentData(first,new Faction{Id=2});
                if(mutation==3){owned.ReservationId=1;em.SetComponentData(first,owned);}
                if(mutation==4)em.AddComponentData(first,new Parent{Value=actors[1]});
                if(mutation==5)em.SetComponentData(first,LocalTransform.FromPosition(float.NaN,0,500));
                if(mutation==6){owned.StableObjectId=em.GetComponentData<SkirmishAttemptOwnedComponent>(actors[1]).StableObjectId;em.SetComponentData(first,owned);}
                if(mutation==7){owned.SessionId=new FixedString64Bytes("foreign-attempt");em.SetComponentData(first,owned);}
                if(mutation==8)em.SetComponentData(first,new UnitHealth{Current=0,Max=100});
                if(mutation==9)em.SetComponentData(first,new SkirmishArmyGroupMembershipComponent{GroupId=0,Domain=SkirmishPopulationCategory.Infantry});
                if(mutation==10)em.AddComponent<UnitTransportPassenger>(first);
                bool rejected=false;OpeningGroupCandidate[] values=null;
                try{values=BuildS005OpeningGroupCandidates(ReadActualS005OpeningGroups(em,native),35);}catch(InvalidOperationException){rejected=true;}
                if(mutation==0)
                {
                    if(rejected||values.Length!=4||values[0].Group!=1||values[2].Group!=2||values[0].Yaw!=35||values[1].Yaw!=215||values[0].Actors.Length!=4||
                        Vector3.Distance(values[0].Center,new Vector3(503,0,501.5f))>.0001f||Vector3.Distance(values[2].Center,new Vector3(511,0,505.5f))>.0001f)
                        throw new InvalidOperationException("Actual group membership/center/heading control failed");
                }
                else if(!rejected)throw new InvalidOperationException("Actual original/group identity corruption accepted: "+mutation);
                controls++;
            }
            Debug.Log("[S005OpeningGroupCandidateControls] result=Passed controls="+controls+" nativeCandidateViewsPending=1 readinessClaim=0");
        }
        static OpeningGroupCandidate[] BuildS005MeasuredOpeningCandidates(OpeningGroupActor[] actors)
        {
            var old=BuildS005OpeningGroupCandidates(actors,35);
            if(old[0].Group!=1||old[2].Group!=2)throw new InvalidOperationException("Measured QA exact original groups1/2 required");
            float[] heights={28,28,20,20},yaws={121,301,127,307};
            Vector2[] offsets={new Vector2(-12.485554f,4.620550f),new Vector2(11.659108f,-7.091067f),new Vector2(-8.582710f,6.162482f),new Vector2(7.383584f,-3.786131f)};
            for(int i=0;i<old.Length;i++){old[i].Height=heights[i];old[i].Yaw=yaws[i];old[i].CameraOffset=offsets[i];}
            return old;
        }
        public static void RunS005MeasuredOpeningCandidateControls()
        {
            RunS005OpeningGroupCandidateControls();
            var actors=new OpeningGroupActor[8];for(int i=0;i<8;i++)actors[i]=new OpeningGroupActor{Group=(uint)(i/4+1),Id="control"+i,Position=new Vector3(500+i,0,500)};
            var values=BuildS005MeasuredOpeningCandidates(actors);
            if(values.Length!=4||values[0].Height!=28||values[1].Yaw!=301||values[2].Height!=20||values[3].Yaw!=307||values.Any(x=>x.Actors.Length!=4))throw new InvalidOperationException("Measured QA pose vector changed");
            for(int i=0;i<4;i++)
            {
                var value=values[i];var forward=Quaternion.Euler(65,value.Yaw,0)*Vector3.forward;
                var camera=new Vector3(value.Center.x+value.CameraOffset.x,value.Height,value.Center.z+value.CameraOffset.y);
                var focus=camera+forward*((value.Center.y-value.Height)/forward.y);
                if(!SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(focus,value.Height,65,value.Yaw,out var ground)||Vector3.Distance(camera,ground-forward*((-value.Height)/forward.y))>.002f)throw new InvalidOperationException("Explicit measured QA request/focus algebra changed");
            }
            foreach(var actor in actors)if(actor.Group==2)actor.Group=3;
            bool refused=false;try{BuildS005MeasuredOpeningCandidates(actors);}catch(InvalidOperationException){refused=true;}
            if(!refused)throw new InvalidOperationException("Measured QA accepted different original group IDs");
            Debug.Log("[S005MeasuredOpeningCandidateControls] result=Passed controls=3 preservedOriginalControls=11 readinessClaim=0");
        }
        static void ObserveMeasuredOpeningRuntimeLog(string message,string trace,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Assert&&type!=LogType.Exception)return;
            measuredOpeningRuntimeErrors++;
            if(measuredOpeningRuntimeErrorFacts.Count<32)measuredOpeningRuntimeErrorFacts.Add(type+": "+message+"\n"+trace);
        }
        static void CompleteMeasuredOpeningRuntimeGate()
        {
            Application.logMessageReceived-=ObserveMeasuredOpeningRuntimeLog;EditorApplication.quitting-=CompleteMeasuredOpeningRuntimeGate;
            WriteOpeningGroupFact(new JObject{["phase"]="MeasuredRuntimeGateAtQuit",["runtimeErrors"]=measuredOpeningRuntimeErrors,["firstErrorFacts"]=new JArray(measuredOpeningRuntimeErrorFacts),["errorFactsBound"]=32,["allErrorsCounted"]=true});
            if(measuredOpeningRuntimeErrors==0&&openingGroupsDone&&openingGroupsError==null)Debug.Log("[S005MeasuredOpeningRuntimeGate] result=Passed errors=0 viewsCompleted=4 wrapperExitStillRequired=1");
        }
        static string MeasuredOpeningUiPath(Transform transform)
        {
            var names=new List<string>();for(var node=transform;node!=null;node=node.parent)names.Add(node.name+"["+node.GetSiblingIndex()+"]");names.Reverse();return string.Join("/",names);
        }
        static JArray ReadMeasuredOpeningUiRectangles()
        {
            var result=new JArray();var seen=new HashSet<RectTransform>();
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if(!canvas.isActiveAndEnabled||!canvas.gameObject.activeInHierarchy)continue;
                foreach(var rect in canvas.GetComponentsInChildren<RectTransform>(false))seen.Add(rect);
            }
            if(seen.Count>4096)throw new InvalidOperationException("Active UI rectangle census exceeded explicit4096 bound; no truncation");
            foreach(var rect in seen.OrderBy(r=>MeasuredOpeningUiPath(r),StringComparer.Ordinal))
            {
                var nearest=rect.GetComponentInParent<Canvas>();var effective=nearest!=null?nearest.rootCanvas:null;
                if(effective==null||!effective.isActiveAndEnabled)throw new InvalidOperationException("Actual effective root Canvas unavailable: "+MeasuredOpeningUiPath(rect));
                bool overlay=effective.renderMode==RenderMode.ScreenSpaceOverlay;
                var uiCamera=overlay?null:effective.worldCamera;
                if(!overlay&&(uiCamera==null||!uiCamera.isActiveAndEnabled))throw new InvalidOperationException("Unsupported non-overlay effective Canvas camera ownership: "+MeasuredOpeningUiPath(effective.transform));
                var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=new JArray();
                foreach(var corner in corners){var p=RectTransformUtility.WorldToScreenPoint(uiCamera,corner);if(!float.IsFinite(p.x)||!float.IsFinite(p.y))throw new InvalidOperationException("Nonfinite actual UI rectangle");points.Add(new JObject{["x"]=p.x,["y"]=p.y});}
                JObject pixels=null;if(uiCamera!=null){var p=uiCamera.pixelRect;pixels=new JObject{["x"]=p.x,["y"]=p.y,["width"]=p.width,["height"]=p.height};}
                result.Add(new JObject{["path"]=MeasuredOpeningUiPath(rect),["nearestCanvasPath"]=MeasuredOpeningUiPath(nearest.transform),["effectiveRootCanvasPath"]=MeasuredOpeningUiPath(effective.transform),["renderMode"]=effective.renderMode.ToString(),["cameraPath"]=uiCamera!=null?MeasuredOpeningUiPath(uiCamera.transform):null,["cameraPixelRect"]=pixels,["activeInHierarchy"]=rect.gameObject.activeInHierarchy,["screenWidth"]=Screen.width,["screenHeight"]=Screen.height,["screenCorners"]=points,["scope"]="All active Canvas descendant RectTransforms using effective root Canvas camera; may be transparent/culled. Recorded geometry, not inferred safe-area or occlusion proof"});
            }
            if(result.Count==0)throw new InvalidOperationException("Actual active HUD rectangle census unavailable");
            return result;
        }
        static JArray ReadGroupSourceBoundsRisks(EntityManager em,OpeningGroupCandidate candidate,Vector3 camera)
        {
            var result=new JArray();using var query=em.CreateEntityQuery(typeof(OperationMapRenderDatabaseComponent));
            if(query.CalculateEntityCount()!=1)throw new InvalidOperationException("Exact source render database required for roof-risk diagnostics");
            var database=em.GetComponentData<OperationMapRenderDatabaseComponent>(query.GetSingletonEntity());if(!database.Blob.IsCreated)throw new InvalidOperationException("Actual source render blob unavailable");
            ref var blob=ref database.Blob.Value;
            foreach(var actor in candidate.Actors)
            {
                var risks=new JArray();var target=actor.Position;var delta=target-camera;float distance=delta.magnitude;var ray=new Ray(camera,delta.normalized);
                for(int i=0;i<blob.Placements.Length;i++)
                {
                    ref var placement=ref blob.Placements[i];if(placement.SemanticCategory!=DenseCityPresentationSemanticCategory.GameplayBuildingIntact&&placement.SemanticCategory!=DenseCityPresentationSemanticCategory.BuildingAttachmentIntact)continue;
                    var local=blob.Prototypes[placement.PrototypeIndex].CombinedLocalBounds;var matrix=placement.WorldMatrix;
                    var center=math.transform(matrix,local.Center);var extents=math.abs(matrix.c0.xyz)*local.Extents.x+math.abs(matrix.c1.xyz)*local.Extents.y+math.abs(matrix.c2.xyz)*local.Extents.z;
                    var bounds=new Bounds(center,extents*2);
                    if(bounds.IntersectRay(ray,out float enter)&&enter<distance)risks.Add(new JObject{["placementIndex"]=i,["stateOwnerIndex"]=placement.StateOwnerIndex,["requiredVisualState"]=placement.RequiredVisualState.ToString(),["boundsCenter"]=EncodeGroupVector(center),["boundsSize"]=EncodeGroupVector(extents*2),["rayEntryDistance"]=enter});
                }
                result.Add(new JObject{["actorId"]=actor.Id,["scope"]="Conservative source prototype AABB ray risks only; no pixel occlusion or live-state acceptance",["potentialBlockers"]=risks});
            }
            return result;
        }
        static bool openingPlacementWitness;
        public static void RunS005OpeningPlacementWitnessDiagnostic()=>BeginS005OpeningCandidates(true,true);
        public static void RunS005OpeningPlacementWitnessPreparation()
        {
            RunS005MeasuredOpeningCandidateControls();
            RunS005OpeningPlacementWitnessControls();
            S005SceneArtifactWarmup.Run();
            Debug.Log("[S005OpeningPlacementWitnessPreparation] result=Passed original=11 measured=3 witness=4 warmup=1 gameplayCertified=0");
        }
        static JObject WitnessEntity(Entity e)=>new JObject{["index"]=e.Index,["version"]=e.Version};
        static JObject WitnessCell(int2 c)=>new JObject{["x"]=c.x,["z"]=c.y};
        static JObject WitnessBounds(Bounds b)=>new JObject{["center"]=EncodeGroupVector(b.center),["size"]=EncodeGroupVector(b.size),["min"]=EncodeGroupVector(b.min),["max"]=EncodeGroupVector(b.max)};
        static Rect WitnessCellRect(in GridConfig grid,int2 min,int2 size)=>new Rect(grid.Origin.x+min.x*grid.CellSize,grid.Origin.z+min.y*grid.CellSize,size.x*grid.CellSize,size.y*grid.CellSize);
        static JObject WitnessRect(Rect r)=>new JObject{["minX"]=r.xMin,["minZ"]=r.yMin,["maxX"]=r.xMax,["maxZ"]=r.yMax,["width"]=r.width,["depth"]=r.height};
        static bool WitnessOverlap(Rect a,Rect b)=>a.xMin<b.xMax&&a.xMax>b.xMin&&a.yMin<b.yMax&&a.yMax>b.yMin;
        static bool WitnessHasFootprint(EntityManager em,Entity e)=>em.HasComponent<UnitGrid>(e)&&em.HasComponent<UnitFootprint>(e)&&math.all(em.GetComponentData<UnitFootprint>(e).Size>0);
        static Rect WitnessUnitRect(EntityManager em,Entity e,in GridConfig grid)
        {var size=em.GetComponentData<UnitFootprint>(e).Size;return WitnessCellRect(grid,UnitFootprintUtility.GetMinCell(em.GetComponentData<UnitGrid>(e).Cell,size),size);}
        static JObject ReadOpeningWitnessProfiler()
        {
            var row=new JObject{["enabled"]=UnityEngine.Profiling.Profiler.enabled,["warlineProfilerMarkersArgument"]=Environment.GetCommandLineArgs().Contains("-warlineProfilerMarkers"),["settingsChanged"]=false};
            try
            {
                var type=typeof(UnityEditorInternal.ProfilerDriver);row["driverAssembly"]=type.Assembly.FullName;
                var property=type.GetProperty("deepProfiling",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                if(property==null||property.PropertyType!=typeof(bool)||property.GetIndexParameters().Length!=0||property.GetGetMethod(true)==null)
                    row["deepProfilingUnavailable"]="Exact static Boolean getter not available";
                else{row["deepProfiling"]=(bool)property.GetValue(null);row["deepProfilingGetter"]=property.GetGetMethod(true).ToString();}
            }
            catch(Exception e){row["deepProfilingUnavailable"]=e.GetType().FullName+":"+e.Message;}
            return row;
        }
        static JObject ReadOpeningPlacementWitness(EntityManager em,Entity session,OpeningGroupCandidate candidate,Camera camera,IReadOnlyList<SkirmishCameraActorMeasurement> posed)
        {
            // Observation barrier only; no simulation update, pause, transform sync or mutation.
            em.CompleteAllTrackedJobs();
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using var grids=em.CreateEntityQuery(typeof(GridConfig));
            if(grids.CalculateEntityCount()!=1)throw new InvalidOperationException("Witness requires exact actual grid");
            var ge=grids.GetSingletonEntity();var grid=em.GetComponentData<GridConfig>(ge);
            if(grid.Width<=0||grid.Height<=0||!float.IsFinite(grid.CellSize)||grid.CellSize<=0||!math.all(math.isfinite(grid.Origin)))throw new InvalidOperationException("Witness actual grid malformed");
            using var owners=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var ownerEntities=owners.ToEntityArray(Allocator.Temp);
            var structures=new List<Entity>();
            foreach(var entity in ownerEntities){var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);if(own.SessionId==native.SessionId&&own.IsStructure!=0&&!em.HasComponent<Prefab>(entity))structures.Add(entity);}
            if(structures.Count>128)throw new InvalidOperationException("Witness actual structure census exceeds128; no truncation");
            structures=structures.OrderBy(e=>em.GetComponentData<SkirmishAttemptOwnedComponent>(e).StableObjectId.ToString(),StringComparer.Ordinal).ThenBy(e=>e.Index).ToList();
            var facilityRows=new JArray();foreach(var entity in structures)facilityRows.Add(ReadOpeningFacilityWitness(em,entity,grid,candidate,camera,posed));
            var actorRows=new JArray();foreach(var actor in candidate.Actors)actorRows.Add(ReadOpeningActorCells(em,ge,grid,actor,structures));
            return new JObject{["scope"]="Read-only current native allocation and geometric witnesses; blocked/occupied may include self or friendly pass. No legality, pixels or readiness conclusion",["attempt"]=native.SessionId.ToString(),["worldSequence"]=em.World.SequenceNumber.ToString(),["trackedJobsCompletedForRead"]=true,["gridEntity"]=WitnessEntity(ge),["gridOrigin"]=EncodeGroupVector(grid.Origin),["gridCellSize"]=grid.CellSize,["gridWidth"]=grid.Width,["gridHeight"]=grid.Height,["structures"]=facilityRows,["actors"]=actorRows,["sourcePrototypeIntersections"]=ReadOpeningSourceIntersections(em,grid,candidate,camera),["profiler"]=ReadOpeningWitnessProfiler(),["physicsTransformsSyncedByDiagnostic"]=false};
        }
        static JObject ReadOpeningActorCells(EntityManager em,Entity ge,GridConfig grid,OpeningGroupActor actor,List<Entity> structures)
        {
            if(!WitnessHasFootprint(em,actor.Entity))throw new InvalidOperationException("Target native UnitGrid/positive UnitFootprint unavailable");
            var cell=em.GetComponentData<UnitGrid>(actor.Entity).Cell;var size=em.GetComponentData<UnitFootprint>(actor.Entity).Size;var min=UnitFootprintUtility.GetMinCell(cell,size);
            if(size.x>64||size.y>64)throw new InvalidOperationException("Target footprint exceeds bounded64x64 witness");
            bool walkAvailable=em.HasBuffer<GridWalkable>(ge),blockAvailable=em.HasComponent<DynamicBlockerComponent>(ge),occupancyAvailable=em.HasComponent<DynamicOccupancyComponent>(ge);
            var walk=walkAvailable?em.GetBuffer<GridWalkable>(ge,true):default;var blocks=blockAvailable?em.GetComponentData<DynamicBlockerComponent>(ge):default;var occupied=occupancyAvailable?em.GetComponentData<DynamicOccupancyComponent>(ge):default;
            var rect=WitnessCellRect(grid,min,size);var cells=new JArray();
            using var all=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<UnitFootprint>()},None=new[]{ComponentType.ReadOnly<Prefab>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var allUnits=all.ToEntityArray(Allocator.Temp);
            using var staticQuery=em.CreateEntityQuery(typeof(StaticBlockerPrevBounds));using var statics=staticQuery.ToEntityArray(Allocator.Temp);
            for(int z=min.y;z<min.y+size.y;z++)for(int x=min.x;x<min.x+size.x;x++)
            {
                var at=new int2(x,z);bool inside=GridUtils.InBounds(at,grid.Width,grid.Height);int index=inside?GridUtils.CellToIndex(at,grid.Width):-1;
                var containing=new JArray();foreach(var unit in allUnits)
                {
                    if(!WitnessHasFootprint(em,unit))continue;
                    var us=em.GetComponentData<UnitFootprint>(unit).Size;var um=UnitFootprintUtility.GetMinCell(em.GetComponentData<UnitGrid>(unit).Cell,us);
                    if(x<um.x||z<um.y||x>=um.x+us.x||z>=um.y+us.y)continue;
                    var hit=new JObject{["entity"]=WitnessEntity(unit),["self"]=unit==actor.Entity,["disabled"]=em.HasComponent<Disabled>(unit)};
                    if(em.HasComponent<SkirmishAttemptOwnedComponent>(unit)){var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);hit["stableId"]=own.StableObjectId.ToString();hit["session"]=own.SessionId.ToString();hit["faction"]=own.FactionId;hit["isStructure"]=own.IsStructure;}
                    containing.Add(hit);
                }
                var staticOwners=new JArray();foreach(var entity in statics){var b=em.GetComponentData<StaticBlockerPrevBounds>(entity);if(x>=b.Min.x&&z>=b.Min.y&&x<b.Max.x&&z<b.Max.y)staticOwners.Add(new JObject{["entity"]=WitnessEntity(entity),["minCell"]=WitnessCell(b.Min),["maxCellExclusive"]=WitnessCell(b.Max),["friendlyPassFaction"]=b.FriendlyPassFactionId});}
                cells.Add(new JObject{["cell"]=WitnessCell(at),["inGrid"]=inside,["index"]=index,["walkable"]=inside&&walkAvailable&&index<walk.Length?(JToken)new JValue(walk[index].Value):JValue.CreateNull(),["blocked"]=inside&&blocks.Blocked.IsCreated&&index<blocks.Blocked.Length?(JToken)new JValue(blocks.Blocked.IsSet(index)):JValue.CreateNull(),["blockerCount"]=inside&&blocks.Counts.IsCreated&&index<blocks.Counts.Length?(JToken)new JValue(blocks.Counts[index]):JValue.CreateNull(),["friendlyPassFaction"]=inside&&blocks.FriendlyPassFactionIds.IsCreated&&index<blocks.FriendlyPassFactionIds.Length?(JToken)new JValue(blocks.FriendlyPassFactionIds[index]):JValue.CreateNull(),["occupied"]=inside&&occupied.Occupied.IsCreated&&index<occupied.Occupied.Length?(JToken)new JValue(occupied.Occupied.IsSet(index)):JValue.CreateNull(),["nativeFootprintsContainingCell"]=containing,["staticBlockerOwnersContainingCell"]=staticOwners});
            }
            var overlaps=new JArray();foreach(var entity in structures)
            {
                if(WitnessHasFootprint(em,entity)&&WitnessOverlap(rect,WitnessUnitRect(em,entity,grid)))overlaps.Add(new JObject{["entity"]=WitnessEntity(entity),["allocationDomain"]="UnitGrid centered GetMinCell footprint"});
                if(em.HasComponent<RuntimeBuildingCombatInfo>(entity)){var info=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);if(math.all(info.FootprintCells>0)&&WitnessOverlap(rect,WitnessCellRect(grid,info.OriginCell,info.FootprintCells)))overlaps.Add(new JObject{["entity"]=WitnessEntity(entity),["runtimeBuildingId"]=info.RuntimeBuildingId,["allocationDomain"]="RuntimeBuildingCombatInfo origin footprint"});}
            }
            return new JObject{["actor"]=WitnessEntity(actor.Entity),["stableId"]=actor.Id,["position"]=EncodeGroupVector(actor.Position),["centerCell"]=WitnessCell(cell),["positionWorldToCell"]=WitnessCell(GridUtils.WorldToCell(grid,actor.Position)),["size"]=WitnessCell(size),["minCell"]=WitnessCell(min),["worldRect"]=WitnessRect(rect),["cells"]=cells,["overlappingNativeStructureFootprints"]=overlaps,["occupancyInterpretation"]="Raw bits and overlapping native footprint owners include self; not an illegal-placement predicate"};
        }
        static JObject ReadOpeningFacilityWitness(EntityManager em,Entity e,GridConfig grid,OpeningGroupCandidate candidate,Camera camera,IReadOnlyList<SkirmishCameraActorMeasurement> posed)
        {
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(e);var row=new JObject{["entity"]=WitnessEntity(e),["stableId"]=own.StableObjectId.ToString(),["session"]=own.SessionId.ToString(),["faction"]=own.FactionId,["isStructure"]=own.IsStructure,["capacityReleased"]=own.CapacityReleased,["disabled"]=em.HasComponent<Disabled>(e),["hasLocalTransform"]=em.HasComponent<LocalTransform>(e),["hasUnitGrid"]=em.HasComponent<UnitGrid>(e),["hasUnitFootprint"]=em.HasComponent<UnitFootprint>(e)};
            if(em.HasComponent<SkirmishStructureIdentityComponent>(e)){var id=em.GetComponentData<SkirmishStructureIdentityComponent>(e);row["structureId"]=id.StructureId.ToString();row["producer"]=id.Producer.ToString();row["designatedBase"]=id.DesignatedBase;}
            if(em.HasComponent<LocalTransform>(e)){var t=em.GetComponentData<LocalTransform>(e);row["localPosition"]=EncodeGroupVector(t.Position);row["localScale"]=t.Scale;row["localRotation"]=new JArray(t.Rotation.value.x,t.Rotation.value.y,t.Rotation.value.z,t.Rotation.value.w);row["hasParent"]=em.HasComponent<Parent>(e);}
            if(WitnessHasFootprint(em,e)){var cell=em.GetComponentData<UnitGrid>(e).Cell;var size=em.GetComponentData<UnitFootprint>(e).Size;row["unitCenterCell"]=WitnessCell(cell);row["unitFootprintSize"]=WitnessCell(size);row["unitMinCell"]=WitnessCell(UnitFootprintUtility.GetMinCell(cell,size));row["unitWorldRect"]=WitnessRect(WitnessUnitRect(em,e,grid));}
            if(em.HasComponent<RuntimeBuildingCombatInfo>(e)){var info=em.GetComponentData<RuntimeBuildingCombatInfo>(e);row["runtimeBuildingId"]=info.RuntimeBuildingId;row["runtimeOriginCell"]=WitnessCell(info.OriginCell);row["runtimeFootprintSize"]=WitnessCell(info.FootprintCells);row["runtimeWorldRect"]=WitnessRect(WitnessCellRect(grid,info.OriginCell,info.FootprintCells));}
            if(!em.HasComponent<SkirmishNativeBuildingCheckpointRecord>(e)){row["nativeVisualUnavailable"]="No actual native building owner record on this exact entity";return row;}
            var building=em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(e).Building;
            if(building==null||building.Instance==null){row["nativeVisualUnavailable"]="Actual native building/Instance missing";return row;}
            if(!em.HasComponent<RuntimeBuildingCombatInfo>(e)||em.GetComponentData<RuntimeBuildingCombatInfo>(e).RuntimeBuildingId!=building.Id||building.OwnerFactionId!=own.FactionId)throw new InvalidOperationException("Exact native facility record/entity identity mismatch");
            row["nativeInstanceEntityId"]=building.Instance.GetEntityId().ToString();row["nativeInstancePath"]=MeasuredOpeningUiPath(building.Instance.transform);row["nativeDestroyed"]=building.IsDestroyed;
            row["nativeInstancePosition"]=EncodeGroupVector(building.Instance.transform.position);row["nativeInstanceScale"]=EncodeGroupVector(building.Instance.transform.lossyScale);row["nativeDefinitionName"]=building.Definition!=null?building.Definition.DisplayName:null;row["nativeDefinitionPrefabPath"]=building.Definition!=null?AssetDatabase.GetAssetPath(building.Definition.Prefab):null;
            var renderers=building.Instance.GetComponentsInChildren<Renderer>(true);var colliders=building.Instance.GetComponentsInChildren<Collider>(true);
            if(renderers.Length>1024||colliders.Length>1024)throw new InvalidOperationException("Native facility witness exceeds1024 draws/colliders; no truncation");
            var draws=new JArray();foreach(var renderer in renderers.OrderBy(x=>MeasuredOpeningUiPath(x.transform),StringComparer.Ordinal))
            {
                bool indexedAvailable=TryReadOpeningFacilityIndexedBounds(renderer,out var indexed,out var indexedFailure);
                var filter=renderer.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;var matrix=new JArray();var nativeMatrix=renderer.localToWorldMatrix;for(int column=0;column<4;column++)for(int component=0;component<4;component++)matrix.Add(nativeMatrix[component,column]);
                var overlaps=new JArray();if(indexedAvailable)foreach(var actor in candidate.Actors)if(WitnessHasFootprint(em,actor.Entity)&&WitnessOverlap(WitnessUnitRect(em,actor.Entity,grid),new Rect(indexed.min.x,indexed.min.z,indexed.size.x,indexed.size.z)))overlaps.Add(new JObject{["stableActorId"]=actor.Id,["actorOriginInsideIndexedWorldAabb"]=indexed.Contains(actor.Position)});
                var materials=new JArray();foreach(var material in renderer.sharedMaterials)materials.Add(material==null?JValue.CreateNull():(JToken)new JObject{["path"]=AssetDatabase.GetAssetPath(material),["shader"]=material.shader!=null?material.shader.name:null});
                draws.Add(new JObject{["path"]=MeasuredOpeningUiPath(renderer.transform),["nativeEntityId"]=renderer.GetEntityId().ToString(),["enabled"]=renderer.enabled,["activeInHierarchy"]=renderer.gameObject.activeInHierarchy,["isVisibleAnyCamera"]=renderer.isVisible,["meshPath"]=mesh!=null?AssetDatabase.GetAssetPath(mesh):null,["meshEntityId"]=mesh!=null?(JToken)new JValue(mesh.GetEntityId().ToString()):JValue.CreateNull(),["meshSubmeshCount"]=mesh!=null?(JToken)new JValue(mesh.subMeshCount):JValue.CreateNull(),["actualLocalToWorldColumnMajor"]=matrix,["rendererCullingWorldBoundsRaw"]=WitnessBounds(renderer.bounds),["indexedDrawBoundsAvailable"]=indexedAvailable,["indexedDrawBoundsUnavailable"]=indexedFailure,["indexedDrawWorldBounds"]=indexedAvailable?WitnessBounds(indexed):null,["materials"]=materials,["targetIndexedFootprintAabbOverlaps"]=indexedAvailable?overlaps:null,["scope"]="Indexed static triangle geometry under actual current native renderer matrix when supported. Culling bounds raw only, never fallback. AABB overlap is not solid-volume or pixel occlusion proof"});
            }
            var hits=new JArray();foreach(var collider in colliders)
            {
                var rays=new JArray();if(collider.enabled&&collider.gameObject.activeInHierarchy)foreach(var actor in candidate.Actors)
                {
                    var targets=new List<KeyValuePair<string,Vector3>>{new KeyValuePair<string,Vector3>("nativeActorOrigin",actor.Position)};
                    var measurement=posed.FirstOrDefault(p=>p.StableActorId==actor.Id);Vector3 top=default;bool found=false;
                    if(measurement?.PosedMeshes!=null)foreach(var mesh in measurement.PosedMeshes)if(mesh.DrawnWorldVertices!=null)foreach(var vertex in mesh.DrawnWorldVertices)if(!found||vertex.y>top.y){top=vertex;found=true;}
                    if(found)targets.Add(new KeyValuePair<string,Vector3>("actualHighestDrawnPosedVertex",top));
                    foreach(var target in targets)
                    {var delta=target.Value-camera.transform.position;float distance=delta.magnitude;if(!float.IsFinite(distance)||distance<=0)throw new InvalidOperationException("Native collider witness ray invalid");bool hit=collider.Raycast(new Ray(camera.transform.position,delta/distance),out var witness,distance);rays.Add(new JObject{["stableActorId"]=actor.Id,["targetKind"]=target.Key,["target"]=EncodeGroupVector(target.Value),["targetDistance"]=distance,["hitBeforeTarget"]=hit,["hitDistance"]=hit?(JToken)new JValue(witness.distance):JValue.CreateNull(),["hitPoint"]=hit?EncodeGroupVector(witness.point):null,["triangleIndex"]=hit?(JToken)new JValue(witness.triangleIndex):JValue.CreateNull()});}
                }
                hits.Add(new JObject{["path"]=MeasuredOpeningUiPath(collider.transform),["nativeEntityId"]=collider.GetEntityId().ToString(),["type"]=collider.GetType().FullName,["enabled"]=collider.enabled,["activeInHierarchy"]=collider.gameObject.activeInHierarchy,["isTrigger"]=collider.isTrigger,["bounds"]=WitnessBounds(collider.bounds),["rays"]=rays});
            }
            row["nativeRenderers"]=draws;row["nativeColliderWitnessAvailable"]=colliders.Length>0;row["nativeColliders"]=hits;row["colliderWitnessScope"]="Direct exact native facility Collider.Raycast only; cached physics pose, no SyncTransforms, visibility or rendered pixel acceptance";return row;
        }
        static bool TryReadOpeningFacilityIndexedBounds(Renderer renderer,out Bounds bounds,out string unavailable)
        {
            bounds=default;unavailable=null;
            if(!(renderer is MeshRenderer rigid)||rigid.additionalVertexStreams!=null){unavailable="Unsupported non-rigid or additional vertex stream renderer";return false;}
            var filter=renderer.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;var materials=renderer.sharedMaterials;
            if(mesh==null||mesh.subMeshCount<1||materials.Length!=mesh.subMeshCount){unavailable="Exact mesh/material/submesh binding unavailable";return false;}
            foreach(var material in materials)
            {
                if(material==null||material.shader==null){unavailable="Native shader missing";return false;}
                var path=AssetDatabase.GetAssetPath(material.shader);bool supported=
                    material.shader.name=="Universal Render Pipeline/Lit"&&path=="Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"||
                    material.shader.name=="Universal Render Pipeline/Simple Lit"&&path=="Packages/com.unity.render-pipelines.universal/Shaders/SimpleLit.shader"||
                    material.shader.name=="Universal Render Pipeline/Unlit"&&path=="Packages/com.unity.render-pipelines.universal/Shaders/Unlit.shader";
                if(!supported){unavailable="Unsupported native shader deformation: "+path;return false;}
            }
            try
            {
                using var data=MeshUtility.AcquireReadOnlyMeshData(mesh);var draw=data[0];
                using var vertices=new NativeArray<Vector3>(draw.vertexCount,Allocator.Temp);draw.GetVertices(vertices);bool any=false;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    if(mesh.GetTopology(sub)!=MeshTopology.Triangles){unavailable="Nontriangle native submesh";return false;}
                    using var indices=new NativeArray<int>(draw.GetSubMesh(sub).indexCount,Allocator.Temp);draw.GetIndices(indices,sub,true);
                    foreach(int index in indices)
                    {
                        if(index<0||index>=vertices.Length){unavailable="Native draw index out of range";return false;}
                        Vector3 point=renderer.localToWorldMatrix.MultiplyPoint3x4(vertices[index]);
                        if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z)){unavailable="Nonfinite current native drawn vertex";return false;}
                        if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
                    }
                }
                if(!any)unavailable="Empty native indexed draws";return any;
            }
            catch(Exception e)when(e is InvalidOperationException||e is ArgumentException||e is UnityException)
            {unavailable="Native indexed read unavailable: "+e.GetType().Name+":"+e.Message;return false;}
        }
        static JArray ReadOpeningSourceIntersections(EntityManager em,GridConfig grid,OpeningGroupCandidate candidate,Camera camera)
        {
            using var q=em.CreateEntityQuery(typeof(OperationMapRenderDatabaseComponent));if(q.CalculateEntityCount()!=1)throw new InvalidOperationException("Witness exact source database missing");
            var database=em.GetComponentData<OperationMapRenderDatabaseComponent>(q.GetSingletonEntity());if(!database.Blob.IsCreated)throw new InvalidOperationException("Witness source blob missing");ref var blob=ref database.Blob.Value;
            using var states=em.CreateEntityQuery(typeof(OperationMapRenderCanonicalStateComponent));int stateCount=states.CalculateEntityCount();var stateBuffer=stateCount==1?em.GetBuffer<OperationMapRenderCanonicalStateComponent>(states.GetSingletonEntity(),true):default;
            var rows=new JArray();foreach(var actor in candidate.Actors)
            {
                var footprint=WitnessUnitRect(em,actor.Entity,grid);var intersections=new JArray();
                for(int i=0;i<blob.Placements.Length;i++)
                {
                    ref var placement=ref blob.Placements[i];if(placement.PrototypeIndex<0||placement.PrototypeIndex>=blob.Prototypes.Length)throw new InvalidOperationException("Source prototype index invalid");ref var prototype=ref blob.Prototypes[placement.PrototypeIndex];var local=prototype.CombinedLocalBounds;var matrix=placement.WorldMatrix;
                    if(!math.all(math.isfinite(local.Center))||!math.all(math.isfinite(local.Extents))||!math.all(local.Extents>=0))throw new InvalidOperationException("Nonfinite source prototype bounds");for(int column=0;column<4;column++)if(!math.all(math.isfinite(matrix[column])))throw new InvalidOperationException("Nonfinite source placement matrix");
                    var center=math.transform(matrix,local.Center);var extent=math.abs(matrix.c0.xyz)*local.Extents.x+math.abs(matrix.c1.xyz)*local.Extents.y+math.abs(matrix.c2.xyz)*local.Extents.z;var bounds=new Bounds(center,extent*2);
                    if(!WitnessOverlap(footprint,new Rect(bounds.min.x,bounds.min.z,bounds.size.x,bounds.size.z)))continue;
                    if(intersections.Count>=256)throw new InvalidOperationException("Source prototype intersections exceed256 per actor; no truncation");
                    bool stateAvailable=stateCount==1&&placement.StateOwnerIndex>=0&&placement.StateOwnerIndex<stateBuffer.Length;
                    intersections.Add(new JObject{["placementIndex"]=i,["prototypeIndex"]=placement.PrototypeIndex,["stableHashLow"]=placement.StableIdentityHash.Low.ToString(),["stableHashHigh"]=placement.StableIdentityHash.High.ToString(),["sourceContentHash"]=database.ContentHash.ToString(),["sourceMapId"]=blob.OperationMapId.ToString(),["semanticCategory"]=placement.SemanticCategory.ToString(),["stateOwnerIndex"]=placement.StateOwnerIndex,["requiredVisualState"]=placement.RequiredVisualState.ToString(),["canonicalStateAvailable"]=stateAvailable,["canonicalVisualState"]=stateAvailable?stateBuffer[placement.StateOwnerIndex].VisualState.ToString():null,["worldBounds"]=WitnessBounds(bounds),["actorOriginInsideWorldAabb"]=bounds.Contains(actor.Position),["scope"]="Conservative immutable prototype AABB ground intersection; canonical state observed where available, not active draw or pixel proof"});
                }
                rows.Add(new JObject{["stableActorId"]=actor.Id,["worldFootprint"]=WitnessRect(footprint),["intersections"]=intersections});
            }
            return rows;
        }
        public static void RunS005OpeningPlacementWitnessControls()
        {
            var grid=new GridConfig{Width=16,Height=16,CellSize=2,Origin=new float3(100,0,200)};
            var odd=WitnessCellRect(grid,UnitFootprintUtility.GetMinCell(new int2(4,4),new int2(3,3)),new int2(3,3));
            var even=WitnessCellRect(grid,UnitFootprintUtility.GetMinCell(new int2(4,4),new int2(2,2)),new int2(2,2));
            if(odd.xMin!=106||odd.yMin!=206||odd.width!=6||even.xMin!=108||even.yMin!=208||even.width!=4)throw new InvalidOperationException("Native odd/even centered footprint witness shifted");
            if(WitnessOverlap(new Rect(0,0,1,1),new Rect(1,0,1,1))||!WitnessOverlap(new Rect(0,0,1,1),new Rect(.5f,0,1,1)))throw new InvalidOperationException("Witness full footprint overlap/edge semantics changed");
            using(var world=new World("OpeningPlacementWitnessControl"))
            {
                var em=world.EntityManager;var e=em.CreateEntity(typeof(UnitGrid),typeof(UnitFootprint));em.SetComponentData(e,new UnitGrid{Cell=new int2(4,4)});em.SetComponentData(e,new UnitFootprint{Size=new int2(3,3)});
                if(!WitnessHasFootprint(em,e)||WitnessUnitRect(em,e,grid)!=odd)throw new InvalidOperationException("Actual entity footprint witness mismatch");em.RemoveComponent<UnitFootprint>(e);if(WitnessHasFootprint(em,e))throw new InvalidOperationException("Missing native footprint silently guessed");
            }
            var bounds=new Bounds(new Vector3(5,5,5),new Vector3(4,4,4));var encoded=WitnessBounds(bounds);if((float)encoded["size"]["x"]!=4||!bounds.Contains(new Vector3(5,5,5))||bounds.Contains(new Vector3(5,0,5)))throw new InvalidOperationException("Roof AABB/explicit bounds encoding lost vertical distinction");
            Debug.Log("[S005OpeningPlacementWitnessControls] result=Passed controls=4 nativePlacementOrOcclusionAccepted=0");
        }

        static JObject EncodeGroupVector(Vector3 value)=>new JObject{["x"]=value.x,["y"]=value.y,["z"]=value.z};
        static string OpeningGroupFileHash(string path){using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static void WriteOpeningGroupFact(JObject fact){fact["frame"]=Time.frameCount;fact["renderedFrame"]=Time.renderedFrameCount;fact["timeScale"]=Time.timeScale;fact["readinessClaim"]=false;File.AppendAllText(openingGroupsOutput,fact.ToString(Newtonsoft.Json.Formatting.None)+"\n");}
    }
    public sealed class SkirmishOpeningGroupCandidateFrame:MonoBehaviour{}
}
#endif
