#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool codedSizeCameraActive, codedSizeCameraFinishing;
        private static SkirmishResolvedSetup codedSizeExpected;
        private static string codedSizeLocale, codedSizeDirectory;
        private static int codedSizeTapFrame=-1, codedSizeCaptureFrame=-1;
        private static string codedSizePng;
        private static bool codedSizeEnvironmentCaptured;
        private static bool codedSettledRequired;
        private static double codedSettledDeadline;
        private static string codedSettledReason;
        private static JObject codedSettledObservation;
        private static readonly CodedOpeningSettleState codedSettledState=new CodedOpeningSettleState();
        private static string codedSizePreviousSaveRoot, codedSizePreviousBackground;

        private static void RestoreCodedSizeEnvironment()
        {
            if(!codedSizeEnvironmentCaptured)return;
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",codedSizePreviousSaveRoot);
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",codedSizePreviousBackground);
            codedSizeEnvironmentCaptured=false;
        }

        // Separate observational entry. Never creates a production job/claim.
        public static void RunCodedBaSizeCameraObservation()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || productionCanary!=null || codedSizeCameraActive)
                throw new InvalidOperationException("Idle exclusive camera observation required.");
            string id=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_CATALOG");
            string text=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_SIZE");
            if(!new[]{"S002","S003","S004"}.Contains(id) ||
                !Enum.TryParse(text,false,out SkirmishSizeId size) ||
                !new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar}.Contains(size))
                throw new InvalidOperationException("Exact coded catalog and canonical size required.");
            int chosenSeed=id=="S002"?104731:id=="S003"?104732:155925;
            string settle=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_REQUIRE_SETTLED_PROFILE");
            if(settle!=null&&settle!="0"&&settle!="1")throw new InvalidOperationException("Explicit settled-profile flag required.");
            codedSettledRequired=settle=="1";codedSettledDeadline=0;codedSettledReason="NotObserved";
            codedSettledState.Reset();codedSettledObservation=null;
            if(codedSettledRequired)RunCodedOpeningSettleControls();
            codedSizeLocale=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_LOCALE")??"en";
            if(codedSizeLocale!="en"&&codedSizeLocale!="fa-IR")throw new InvalidOperationException("Supported locale required.");
            var catalog=SkirmishExpansionCatalogConfig.Load();
            if(catalog==null||!catalog.TryGetDefinition(id,out var definition)||!AssetDatabase.Contains(definition)||
                EditorUtility.IsDirty(definition)||!SkirmishSetupBriefing.TryLoad(id,chosenSeed,size,out codedSizeExpected))
                throw new InvalidOperationException("Exact clean existing coded definition/compiler required.");
            codedSizeDirectory=Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_NATIVE_DIRECTORY");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            if(string.IsNullOrEmpty(codedSizeDirectory)||!Path.IsPathRooted(codedSizeDirectory)||
                !Path.GetFullPath(codedSizeDirectory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)||
                Directory.Exists(codedSizeDirectory)||File.Exists(codedSizeDirectory))
                throw new InvalidOperationException("Fresh owned camera output directory required.");
            Directory.CreateDirectory(codedSizeDirectory);
            codedSizeTapFrame=codedSizeCaptureFrame=-1;
            codedSizePng=Path.Combine(codedSizeDirectory,"normal-opening.png");
            codedSizePreviousSaveRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            codedSizePreviousBackground=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            codedSizeEnvironmentCaptured=true;
            codedSizeCameraFinishing=false;
            codedSizeCameraActive=true;
            try { RunEditorJourney(codedSizeLocale,false,false,id,chosenSeed); }
            catch(Exception error) { FinishCodedSizeCamera(false,"LaunchException:"+error);throw; }
        }

        // Uses only the actual selected control and ordinary compiled briefing.
        private static bool CodedSizeMenuReady(QuickCustomScreenView setup)
        {
            var size=codedSizeExpected.SizeId;
            var expected=size==SkirmishSizeId.War?UiSkirmishMissionSize.War:
                size==SkirmishSizeId.LargeWar?UiSkirmishMissionSize.LargeWar:UiSkirmishMissionSize.Standard;
            var control=setup.MissionSizeControl;
            if(control==null||!control.gameObject.activeInHierarchy||control.SegmentButtons==null||control.SegmentButtons.Length!=3)
                throw new InvalidOperationException("Visible normal army-size control absent.");
            if(setup.SelectedMissionSize!=expected)
            {
                int index=size==SkirmishSizeId.War?1:size==SkirmishSizeId.LargeWar?2:0;
                if(MenuTap(control.SegmentButtons[index])) codedSizeTapFrame=Time.frameCount;
                return false;
            }
            if(codedSizeTapFrame>=0 && Time.frameCount<=codedSizeTapFrame+1)return false;
            var presented=setup.PresentedMissionSizeSetup;
            if(presented==null||presented.CatalogId!=codedSizeExpected.CatalogId||
                presented.DefinitionId!=codedSizeExpected.DefinitionId||presented.ContentVersion!=codedSizeExpected.ContentVersion||
                presented.SizeId!=size||presented.DifficultyId!=SkirmishDifficultyId.Regular||
                presented.Seed!=codedSizeExpected.Seed||presented.SetupHash!=codedSizeExpected.SetupHash)
                throw new InvalidOperationException("Presented selected-size briefing differs from compiled canonical identity.");
            return true;
        }

        private static void ValidateCodedSizeNativeIdentity(EntityManager em,Entity session)
        {
            if(!em.HasComponent<SkirmishResolvedSetupRecord>(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                throw new InvalidOperationException("Actual expanded setup missing.");
            var actual=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(actual==null||actual.CatalogId!=codedSizeExpected.CatalogId||actual.DefinitionId!=codedSizeExpected.DefinitionId||
                actual.ContentVersion!=codedSizeExpected.ContentVersion||actual.SizeId!=codedSizeExpected.SizeId||
                actual.DifficultyId!=SkirmishDifficultyId.Regular||actual.Seed!=codedSizeExpected.Seed||
                actual.SetupHash!=codedSizeExpected.SetupHash||
                SkirmishAcceptanceCensusCapture.ComputeConfigHash(actual)!=SkirmishAcceptanceCensusCapture.ComputeConfigHash(codedSizeExpected)||
                native.IsLegacy!=0||native.CatalogId.ToString()!=actual.CatalogId||native.DefinitionId.ToString()!=actual.DefinitionId||
                native.ContentVersion!=actual.ContentVersion||native.SizeId!=actual.SizeId||native.DifficultyId!=actual.DifficultyId||
                native.Seed!=actual.Seed||native.SetupHash!=actual.SetupHash)
                throw new InvalidOperationException("Actual native selected-size identity mismatch.");
        }

        private static void TickCodedSizeCamera(EntityManager em,Entity session,in SkirmishMatchState match,bool active,float elapsed)
        {
            ValidateCodedSizeNativeIdentity(em,session);
            if(match.Phase!=SkirmishPhase.Playing||!active||elapsed<=0)return;
            if(codedSizeCaptureFrame<0)
            {
                if(codedSettledRequired&&!CodedSettledOpeningReady(em,session))return;
                SkirmishGroundSourceFitSurvey.CaptureCodedBaCameraObservation(em,session,in match,active,elapsed);
                int censusReceipts=Directory.GetFiles(codedSizeDirectory,"*.json").Length;
                if(!CodedSizeCensusReceiptReady(censusReceipts))return;
                if(codedSettledRequired)
                {
                    string path=Path.Combine(codedSizeDirectory,"settled-opening.json");
                    codedSettledObservation["pngQueuedFrame"]=Time.frameCount;codedSettledObservation["png"]=codedSizePng;
                    codedSettledObservation["pngPixelFrameProven"]=false;
                    using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                    using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
                    {writer.Write(codedSettledObservation.ToString());writer.Flush();stream.Flush(true);}
                }
                ScreenCapture.CaptureScreenshot(codedSizePng);
                codedSizeCaptureFrame=Time.frameCount;
                return;
            }
            if(Time.frameCount<=codedSizeCaptureFrame+2)return;
            if(!File.Exists(codedSizePng)||new FileInfo(codedSizePng).Length==0)return;
            FinishCodedSizeCamera(true,"ObservedNormalOpeningOnly");
        }

        private sealed class CodedOpeningSettleState
        {
            public int LastFrame=-1,Count;public Vector3 Position;public Quaternion Rotation;public float Fov;
            public void Reset(){LastFrame=-1;Count=0;}
            public bool Step(int frame,bool ready,Vector3 position,Quaternion rotation,float fov,Vector3 expected,Quaternion expectedRotation,float expectedFov)
            {
                bool finite=float.IsFinite(position.x)&&float.IsFinite(position.y)&&float.IsFinite(position.z)&&float.IsFinite(fov);
                bool pose=ready&&finite&&Vector3.Distance(position,expected)<.05f&&Quaternion.Angle(rotation,expectedRotation)<.1f&&Mathf.Abs(fov-expectedFov)<.05f;
                if(!pose){Count=0;LastFrame=frame;return false;}
                if(frame==LastFrame)return Count>=15;
                bool stable=frame>LastFrame&&Count>0&&Vector3.Distance(position,Position)<.01f&&Quaternion.Angle(rotation,Rotation)<.05f&&Mathf.Abs(fov-Fov)<.01f;
                Count=stable?Count+1:1;LastFrame=frame;Position=position;Rotation=rotation;Fov=fov;return Count>=15;
            }
        }
        public static void RunCodedOpeningSettleControls()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||codedSizeCameraActive||productionCanary!=null)throw new InvalidOperationException("Idle settle controls required");
            var value=new CodedOpeningSettleState();var p=new Vector3(3,20,8);var q=Quaternion.Euler(65,307,0);int count=0;
            void Require(bool v){if(!v)throw new InvalidOperationException("Actual settle helper control refused");count++;}
            Require(!value.Step(1,false,p,q,46,p,q,46)&&value.Count==0);
            for(int i=2;i<=15;i++)if(value.Step(i,true,p,q,46,p,q,46))throw new InvalidOperationException("Settled before fifteen distinct frames");
            Require(value.Count==14);Require(value.Step(16,true,p,q,46,p,q,46));
            Require(value.Step(16,true,p,q,46,p,q,46)&&value.Count==15);
            Require(!value.Step(17,true,p+Vector3.one,q,46,p,q,46)&&value.Count==0);
            Require(!value.Step(18,false,p,q,46,p,q,46));
            Require(!value.Step(19,true,p,Quaternion.identity,46,p,q,46));
            Require(!value.Step(20,true,p,q,60,p,q,46));
            Require(!value.Step(21,true,new Vector3(float.NaN,20,8),q,46,p,q,46));
            if(count!=9)throw new InvalidOperationException("Settle control cardinality changed");
            Debug.Log("[CodedOpeningSettleControls] result=Passed controls=9 diagnosticOnly=1");
        }
        private static bool CodedSettledOpeningReady(EntityManager em,Entity session)
        {
            if(codedSettledDeadline==0)codedSettledDeadline=EditorApplication.timeSinceStartup+120;
            if(EditorApplication.timeSinceStartup>codedSettledDeadline)throw new InvalidOperationException("Settled actual opening observation unavailable: "+codedSettledReason);
            codedSettledReason="Actual profile/owner/request/camera pending";
            if(!Mathf.Approximately(Time.timeScale,1)||!SkirmishMissionViewProfileApplication.TryResolve(em,out var profile)||
                !SkirmishS005OpeningViewPolicy.OwnsIntroduction(em,session)||!em.HasComponent<SkirmishS005OpeningFocusOwner>(session))
            {codedSettledState.Reset();return false;}
            var owner=em.GetComponentData<SkirmishS005OpeningFocusOwner>(session);var camera=Camera.main;
            using var requests=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if(camera==null||requests.CalculateEntityCount()!=1){codedSettledState.Reset();return false;}
            var request=requests.GetSingleton<RuntimeCameraFocusRequestComponent>();var forward=Quaternion.Euler(profile.Pitch,profile.Yaw,0)*Vector3.forward;
            var expected=(Vector3)owner.GroundRequest-forward*(profile.NormalHeight/-forward.y);
            bool same=request.UseExplicitPerspective==1&&Vector3.Distance((Vector3)request.World,(Vector3)owner.GroundRequest)<.001f&&
                Mathf.Abs(request.Perspective.x-profile.NormalHeight)<.001f&&Mathf.Abs(request.Perspective.y-profile.Pitch)<.001f&&
                Mathf.Abs(request.Perspective.z-profile.Yaw)<.001f&&Mathf.Abs(request.Perspective.w-profile.NormalFieldOfView)<.001f;
            bool ready=same&&request.Requested==0&&!camera.orthographic&&camera.pixelWidth>0&&camera.pixelHeight>0;
            if(!codedSettledState.Step(Time.frameCount,ready,camera.transform.position,camera.transform.rotation,camera.fieldOfView,
                expected,Quaternion.Euler(profile.Pitch,profile.Yaw,0),profile.NormalFieldOfView))return false;
            codedSettledReason="Complete original group/current posed geometry pending";
            if(!TryReadCodedSettledGroup(em,session,profile,owner,camera,out var group))return false;
            string profilePath=AssetDatabase.GetAssetPath(profile);
            if(string.IsNullOrEmpty(profilePath)||EditorUtility.IsDirty(profile))throw new InvalidOperationException("Actual persisted clean profile required");
            codedSettledObservation=new JObject { ["scope"]="PassiveSettledOwnedOpening_NotAcceptance",["frame"]=Time.frameCount,
                ["attempt"]=owner.Attempt.ToString(),["setupHash"]=owner.SetupHash,["ownerRequestedFrame"]=owner.RequestedFrame,
                ["stableDistinctFrames"]=codedSettledState.Count,["sameOwnedRequestPayload"]=same,["requestObservedConsumed"]=request.Requested==0,
                ["profilePath"]=profilePath,["profileSha256"]=SkirmishProductionCanaryRequest.FileHash(Path.GetFullPath(Path.Combine(Application.dataPath,"..",profilePath))),
                ["profileNormalHeight"]=profile.NormalHeight,["profilePitch"]=profile.Pitch,["profileYaw"]=profile.Yaw,["profileFov"]=profile.NormalFieldOfView,
                ["normalTimeScale"]=Time.timeScale,["group"]=group,["cameraAccepted"]=false,["normalMissionReady"]=false };
            codedSettledReason="Observed";return true;
        }
        private static bool TryReadCodedSettledGroup(EntityManager em,Entity session,SkirmishMissionViewProfile profile,
            SkirmishS005OpeningFocusOwner owner,Camera camera,out JObject fact)
        {
            fact=null;var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(setup==null||!em.HasBuffer<SkirmishArmyGroupRecord>(session))return false;
            using var query=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using var actors=query.ToEntityArray(Allocator.Temp);Entity hint=Entity.Null;
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=owner.Attempt||own.StableObjectId!=owner.ActorId)continue;
                if(hint!=Entity.Null)return false;hint=actor;
            }
            if(hint==Entity.Null||!em.HasComponent<SkirmishArmyGroupMembershipComponent>(hint))return false;
            uint group=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(hint).GroupId;if(group==0)return false;
            int records=0;SkirmishArmyGroupRecord selected=default;
            foreach(var row in em.GetBuffer<SkirmishArmyGroupRecord>(session,true))if(row.GroupId==group){records++;selected=row;}
            if(records!=1||selected.FactionId!=setup.PlayerFaction||selected.Role!=SkirmishRoleKind.Rifle||selected.Domain!=SkirmishPopulationCategory.Infantry||
                selected.MemberCount!=SkirmishRoleIds.InfantrySquadMembers||selected.AliveCount!=SkirmishRoleIds.InfantrySquadMembers)return false;
            var originals=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<setup.Forces.Length;i++)
            {
                var force=setup.Forces[i];if(force.FactionId!=setup.PlayerFaction||force.RoleKind!=SkirmishRoleKind.Rifle)continue;
                for(int j=0;j<force.Quantity;j++)originals.Add(force.RoleId+"."+force.FactionId.ToString(System.Globalization.CultureInfo.InvariantCulture)+"."+
                    i.ToString(System.Globalization.CultureInfo.InvariantCulture)+"."+j.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);var members=new JArray();Vector3 sum=default;
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=owner.Attempt||!em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor))continue;
                var member=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor);if(member.GroupId!=group)continue;
                string stable=own.StableObjectId.ToString();
                if(member.Domain!=SkirmishPopulationCategory.Infantry||own.FactionId!=setup.PlayerFaction||own.IsStructure!=0||own.ReservationId!=0||own.CapacityReleased!=0||
                    !originals.Contains(stable)||!ids.Add(stable)||ids.Count>SkirmishRoleIds.InfantrySquadMembers||!em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<Faction>(actor)||
                    em.GetComponentData<Faction>(actor).Id!=setup.PlayerFaction||!em.HasComponent<UnitHealth>(actor)||(em.GetComponentData<UnitHealth>(actor).Current<=0||em.GetComponentData<UnitHealth>(actor).Max<=0)||
                    !em.HasComponent<LocalTransform>(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||em.HasComponent<Disabled>(actor)||
                    em.HasComponent<Parent>(actor)||em.HasComponent<PostTransformMatrix>(actor)||em.HasComponent<UnitTransportPassenger>(actor)||
                    em.HasComponent<UnitTransportCargoPassenger>(actor)||em.HasComponent<UnitTransportParachuteDropComponent>(actor)||em.HasComponent<UnitTransportRopeDropComponent>(actor))return false;
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
                if(role.Role!=SkirmishRoleKind.Rifle||role.Category!=SkirmishPopulationCategory.Infantry)return false;
                Vector3 position=(Vector3)em.GetComponentData<LocalTransform>(actor).Position;if(!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z))return false;
                var sample=SkirmishCameraFramingMeasurement.Capture(em,actor,camera);
                if(!sample.Valid||!string.IsNullOrEmpty(sample.Failure)||sample.Frame!=Time.frameCount||sample.SessionId!=owner.Attempt.ToString()||
                    sample.StableActorId!=stable||sample.CurrentVisual!=sample.DesiredVisual||sample.ProjectionGeometrySource!="ActualActiveDrawnPosedMeshVertices")return false;
                sum+=position;members.Add(new JObject { ["stableActorId"]=stable,["entityIndex"]=actor.Index,["entityVersion"]=actor.Version,
                    ["measurement"]=JObject.Parse(JsonUtility.ToJson(sample)) });
            }
            if(members.Count!=SkirmishRoleIds.InfantrySquadMembers)return false;
            Vector3 center=sum/members.Count,initial=owner.ActualActorWorld;
            fact=new JObject { ["groupId"]=group,["faction"]=setup.PlayerFaction,["role"]=selected.Role.ToString(),["memberCount"]=selected.MemberCount,
                ["aliveCount"]=selected.AliveCount,["ownerActorId"]=owner.ActorId.ToString(),["initialCenter"]=new JArray(initial.x,initial.y,initial.z),
                ["currentCenter"]=new JArray(center.x,center.y,center.z),["members"]=members,["pixelVisibilityAccepted"]=false };
            return true;
        }
        private static bool CodedSizeCensusReceiptReady(int count)
        {
            if(count<0||count>1)throw new InvalidOperationException("Duplicate/invalid passive source/camera receipts refused.");
            return count==1;
        }

        // Uses the actual admission/restoration helpers without scheduling Play/quit or touching the ledger.
        public static int RunCodedSizeReceiptControls()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||codedSizeCameraActive||productionCanary!=null)
                throw new InvalidOperationException("Idle receipt controls required.");
            int count=0;
            void Require(bool value,string label)
            {if(!value)throw new InvalidOperationException("Coded size control failed: "+label);count++;}
            Require(!CodedSizeCensusReceiptReady(0),"zero receipts must poll");
            Require(CodedSizeCensusReceiptReady(1),"one receipt permits screenshot");
            bool rejected=false;try{CodedSizeCensusReceiptReady(2);}catch(InvalidOperationException){rejected=true;}
            Require(rejected,"duplicate receipts refused");
            string save=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            string background=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            string priorSave=codedSizePreviousSaveRoot,priorBackground=codedSizePreviousBackground;
            bool captured=codedSizeEnvironmentCaptured;
            try
            {
                codedSizePreviousSaveRoot=save;codedSizePreviousBackground=background;codedSizeEnvironmentCaptured=true;
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT","size-control-temporary");
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","size-control-temporary");
                RestoreCodedSizeEnvironment();
                Require(Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT")==save&&
                    Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION")==background,"exact previous environment restored");
                RestoreCodedSizeEnvironment();
                Require(!codedSizeEnvironmentCaptured&&Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT")==save&&
                    Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION")==background,"restoration idempotent");
                codedSizeCameraActive=true;codedSizeCameraFinishing=true;
                CompleteCodedSizeCameraAfterOwnedPlay();
                Require(!codedSizeCameraActive&&!codedSizeCameraFinishing,"owned post-Play cleanup releases finishing latch");
            }
            finally
            {
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",save);
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",background);
                codedSizePreviousSaveRoot=priorSave;codedSizePreviousBackground=priorBackground;
                codedSizeEnvironmentCaptured=captured;codedSizeCameraActive=false;codedSizeCameraFinishing=false;
            }
            return count;
        }

        private static void CompleteCodedSizeCameraAfterOwnedPlay()
        {
            if(!codedSizeCameraActive || !codedSizeCameraFinishing)return;
            RestoreCodedSizeEnvironment();
            codedSizeCameraActive=false;codedSizeCameraFinishing=false;
        }

        private static void FinishCodedSizeCamera(bool observed,string reason)
        {
            if(!codedSizeCameraActive || codedSizeCameraFinishing)return;
            // Ownership remains latched until the shared post-Play cleanup.
            // An artifact exception must never fall through to the normal ledger.
            codedSizeCameraFinishing=true;
            bool receiptWritten=false;
            try
            {
                EditorApplication.update-=Tick;
                StopS004MenuProbe();RestoreBackgroundInput();
                RestoreCodedSizeEnvironment();
                SessionState.SetBool(Key,false);SessionState.SetBool(NormalKey,false);
                var row=new JObject{["scope"]="NormalMenuSizeCameraObservation_NotQualification",
                    ["catalogId"]=codedSizeExpected.CatalogId,["size"]=codedSizeExpected.SizeId.ToString(),
                    ["definitionId"]=codedSizeExpected.DefinitionId,["definitionVersion"]=codedSizeExpected.ContentVersion,
                    ["seed"]=codedSizeExpected.Seed,["setupHash"]=codedSizeExpected.SetupHash,
                    ["nativeConfigSha256"]=SkirmishAcceptanceCensusCapture.ComputeConfigHash(codedSizeExpected),
                    ["normalOpeningObserved"]=observed,["reason"]=reason,["cameraAccepted"]=false,
                    ["normalMissionReady"]=false,["qualifiedProductionJobs"]=0,["png"]=codedSizePng};
                using(var stream=new FileStream(Path.Combine(codedSizeDirectory,"journey.json"),FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
                {writer.Write(row.ToString());writer.Flush();stream.Flush(true);}
                receiptWritten=true;
                Debug.Log("[CodedBaSizeCameraJourney] result="+(observed?"Observed":"Failed")+
                    " catalog="+codedSizeExpected.CatalogId+" size="+codedSizeExpected.SizeId+
                    " normalInput=1 cameraAccepted=0 normalMissionReady=0 qualifiedProductionJobs=0");
            }
            finally
            {
                try { RestoreCodedSizeEnvironment(); }
                finally
                {
                    SessionState.SetBool(QuitPendingKey,true);
                    SessionState.SetInt(QuitCodeKey,observed&&receiptWritten?0:2);
                    EditorApplication.update-=FinishQuitAfterPlayMode;EditorApplication.update+=FinishQuitAfterPlayMode;
                    if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();
                }
            }
        }
    }
}
#endif
