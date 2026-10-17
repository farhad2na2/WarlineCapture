using System;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Game.Rendering;
using Game.Rendering.Contracts;
using Unity.Rendering;
using Unity.Mathematics;

namespace Game.Tests.Editor
{
    public sealed class SkirmishAircraftProductionFocusTests
    {
        [Serializable] sealed class NativeFocusDiagnostic
        {
            public string stage,actor,session,profileCatalog,profileDefinition;
            public bool context,profileResolved,profileValid,hierarchyRead,poseFit,fullApply;
            public float aspect,nearClip,fov,minDistance,maxDistance,requiredFitDistance,elapsedSeconds,actorScale;
            public Vector3 worldCenter,worldExtent,posePosition;
            public Vector3 sourceRootPosition,sourceRootScale,actorPosition;
            public float[] sourceRootMatrix;
            public AircraftProductionFocusFact[] events;
            public DrawDiagnostic[] draws;
        }
        [Serializable] sealed class DrawDiagnostic
        {public int entity,version;public bool read,hasDraw;public string error;public Vector3 center,extent;public float[] worldMatrix;}
        static void LogNativeFocusFailure(EntityManager em,Entity root,Entity actor,Camera camera,RtsCameraSystem owner,
            System.Collections.Generic.List<AircraftProductionFocusFact> events,double started)
        {
            var result=new NativeFocusDiagnostic{stage="FirstActualAppliedFailure",actor=actor.ToString(),context=owner.HasAircraftFocusContext,
                aspect=camera.aspect,nearClip=camera.nearClipPlane,fov=camera.fieldOfView,elapsedSeconds=(float)(UnityEngine.Time.realtimeSinceStartupAsDouble-started),events=events.ToArray()};
            result.profileResolved=SkirmishMissionViewProfileApplication.TryResolve(em,out var profile);
            var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishRigidAircraftGeometryAuthoring.Prefabs[0]);
            result.sourceRootPosition=source.transform.localPosition;result.sourceRootScale=source.transform.localScale;result.sourceRootMatrix=new float[16];
            for(int i=0;i<16;i++)result.sourceRootMatrix[i]=source.transform.localToWorldMatrix[i];
            var actorTransform=em.GetComponentData<LocalTransform>(actor);result.actorPosition=actorTransform.Position;result.actorScale=actorTransform.Scale;
            if(profile!=null){result.profileValid=profile.IsValid;result.profileCatalog=profile.CatalogId;result.profileDefinition=profile.DefinitionId;result.minDistance=profile.AircraftMinimumDistance;result.maxDistance=profile.AircraftMaximumDistance;}
            var children=em.GetBuffer<Child>(root,true);var draws=new System.Collections.Generic.List<DrawDiagnostic>();
            foreach(var child in children)
            {
                bool read=RigidAircraftVisibleGeometryPresentation.TryRead(em,child.Value,out var bounds,out bool hasDraw,out string error);
                var matrix=em.GetComponentData<LocalToWorld>(child.Value).Value;var scalars=new float[16];for(int c=0;c<4;c++)for(int r=0;r<4;r++)scalars[c*4+r]=matrix[c][r];
                draws.Add(new DrawDiagnostic{entity=child.Value.Index,version=child.Value.Version,read=read,hasDraw=hasDraw,error=error,center=bounds.center,extent=bounds.extents,worldMatrix=scalars});
            }
            result.draws=draws.ToArray();
            result.hierarchyRead=SkirmishMissionViewProfileApplication.TryReadRigidAircraftVisibleHierarchy(em,root,RenderBoundsGateway.Provider as IRenderVisibleGeometryPresentation,out var full);
            result.worldCenter=full.center;result.worldExtent=full.extents;
            result.poseFit=SkirmishMissionViewProfileApplication.TryAircraftPose(full,profile,camera.aspect,camera.nearClipPlane,out var position,out _);result.posePosition=position;
            result.requiredFitDistance=float.NaN;
            if(profile!=null&&camera.aspect>0)
            {
                var rotation=Quaternion.Euler(profile.AircraftPitch,profile.Yaw,0);var right=rotation*Vector3.right;var up=rotation*Vector3.up;var forward=rotation*Vector3.forward;
                float tangent=Mathf.Tan(profile.AircraftFieldOfView*Mathf.Deg2Rad*.5f);float distance=profile.AircraftMinimumDistance;
                for(int i=0;i<8;i++)
                {
                    var point=Vector3.Scale(full.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));float depth=Vector3.Dot(point,forward);
                    distance=Mathf.Max(distance,Mathf.Abs(Vector3.Dot(point,right))/(tangent*camera.aspect*profile.PlaneMaximumViewportDimension)-depth);
                    distance=Mathf.Max(distance,Mathf.Abs(Vector3.Dot(point,up))/(tangent*profile.PlaneMaximumViewportDimension)-depth);
                    distance=Mathf.Max(distance,camera.nearClipPlane*2-depth);
                }
                result.requiredFitDistance=distance;
            }
            var pose=default(TacticalFollowCameraPoseComponent);result.fullApply=SkirmishMissionViewProfileApplication.TryApplyAircraftFollowPose(em,actor,camera,ref pose);
            Debug.Log("[AircraftProductionFocusNativeDiagnostic] "+JsonUtility.ToJson(result));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
        [TestCase(10)][TestCase(11)][TestCase(12)]
        public void NormalRequestKeepsExactPaidActorAndYieldsOwnership(int mutation)
            =>RunControl(mutation,mutation==12?.25707865f:16f/9);
        [Test] public void PortraitProductionFocusHasThreeActualBoundedZoomDistances()=>RunControl(10,9f/16);
        void RunControl(int mutation,float aspect)
        {
            using var world=new World("AircraftProductionFocusFixture");var em=world.EntityManager;
            var go=new GameObject("AircraftProductionFocusCamera");var camera=go.AddComponent<Camera>();
            camera.aspect=aspect;
            var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _));
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,155926,
                new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out var errors),string.Join(";",errors));
            var session=em.CreateEntity();em.AddComponentData(session,new SkirmishExpandedSessionComponent{SessionId="aircraft-focus-fixture",Phase=SkirmishSessionPhase.Playing});
            em.AddComponentObject(session,new SkirmishResolvedSetupRecord{Setup=setup});
            var state=em.CreateEntity();em.AddComponentData(state,new RuntimeGameplayStateComponent());
            var actor=em.CreateEntity();
            var owner=new SkirmishAttemptOwnedComponent{SessionId="aircraft-focus-fixture",StableObjectId="paid-transport-original",FactionId=1,ReservationId=81};
            em.AddComponentData(actor,owner);em.AddComponentData(actor,new Faction{Id=1});em.AddComponentData(actor,new UnitHealth{Current=100,Max=100});
            em.AddComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.TransportPlane});
            em.AddComponentData(actor,LocalTransform.FromPosition(new float3(100,55,100)));
            em.AddComponent<UnitAirMovement>(actor);em.AddComponent<UnitGrid>(actor);em.AddComponent<UnitMovementBehavior>(actor);
            var cameraOwner=world.GetOrCreateSystemManaged<RtsCameraSystem>();var queue=world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            Entity repeatedGround=Entity.Null;
            if(mutation==10||mutation==12){repeatedGround=em.CreateEntity();em.AddComponentData(repeatedGround,new Faction{Id=1});em.AddComponentData(repeatedGround,new UnitHealth{Current=100});em.AddComponent<SelectedUnitTag>(repeatedGround);}
            int facts=0;AircraftProductionFocusFact requested=null;double started=UnityEngine.Time.realtimeSinceStartupAsDouble;
            var focusEvents=new System.Collections.Generic.List<AircraftProductionFocusFact>();
            void Observe(AircraftProductionFocusFact fact){facts++;focusEvents.Add(fact);if(fact.stage=="Requested")requested=fact;}
            AircraftProductionFocusEvidence.Recorded+=Observe;
            try
            {
                if(mutation==1)em.SetComponentData(actor,new Faction{Id=2});
                if(mutation==2){owner.ReservationId=0;em.SetComponentData(actor,owner);}
                if(mutation==3)em.SetComponentData(actor,new UnitHealth{Current=0,Max=100});
                if(mutation==4)em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Rifle});
                if(mutation==5)em.AddComponentData(actor,new Parent{Value=em.CreateEntity()});
                if(mutation==11)em.RemoveComponent<UnitAirMovement>(actor);
                bool accepted=cameraOwner.TryRequestAircraftFocus(em,actor,camera,42);
                Assert.That(accepted,Is.EqualTo(mutation==0||mutation>=6&&mutation<=10||mutation==12));
                if(!accepted){Assert.AreEqual(0,facts);Assert.IsFalse(cameraOwner.HasAircraftFocusContext);return;}
                Assert.AreEqual(actor.Index,requested.actorIndex);Assert.AreEqual(actor.Version,requested.actorVersion);
                Assert.AreEqual(81,requested.reservation);Assert.AreEqual(42,requested.requestId);
                Assert.AreEqual(Vector3.zero,camera.transform.position,"A queued focus cannot mutate pose before visible geometry is validated");
                if(mutation==10||mutation==12)
                {
                    var originalProvider=RenderBoundsGateway.Provider;
                    try
                    {
                        SkirmishRigidAircraftGeometryAuthoring.VerifyInstalledTransportMetadata();
                        Assert.IsFalse(cameraOwner.TickAircraftFocus(em,camera,false),"A normal missing render root remains pending");
                        Assert.IsTrue(cameraOwner.HasAircraftFocusContext);Assert.That(camera.transform.position,Is.EqualTo(Vector3.zero),"Pending geometry cannot cause a guessed acquisition view");
                        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishRigidAircraftGeometryAuthoring.Prefabs[0]);
                        var root=em.CreateEntity();var children=em.AddBuffer<Child>(root);
                        var parts=new System.Collections.Generic.List<Entity>();
                        foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                            for(int sub=0;sub<mesh.subMeshCount;sub++)
                            {
                                var draw=em.CreateEntity();parts.Add(draw);
                                var relative=source.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                                em.AddComponentData(draw,new LocalToWorld{Value=(float4x4)(Matrix4x4.Translate(new Vector3(100,55,100))*relative)});
                                em.AddSharedComponentManaged(draw,new RenderMeshArray(new[]{renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)]},new[]{mesh}));
                                em.AddComponentData(draw,MaterialMeshInfo.FromRenderMeshArrayIndices(0,0,(ushort)sub));
                                em.GetBuffer<Child>(root).Add(new Child{Value=draw});
                            }
                        }
                        Assert.GreaterOrEqual(parts.Count,19,"Actual installed Transport parts supply the fit");
                        em.AddComponentData(actor,new UnitDetailedVisualReference{Root=root});
                        em.AddComponentData(actor,new UnitRenderVisualComponent{Current=(byte)UnitRenderVisualKind.Detail,Desired=(byte)UnitRenderVisualKind.Detail});
                        RenderBoundsGateway.Bind(RenderBoundsPresentation.Create());
                        AircraftProductionFocusFact applied=null;
                        void Applied(AircraftProductionFocusFact row){if(row.stage=="Applied")applied=row;}
                        AircraftProductionFocusEvidence.Recorded+=Applied;
                        try
                        {
                            bool actualApplied=cameraOwner.TickAircraftFocus(em,camera,false);
                            if(mutation==12)
                            {
                                Assert.IsFalse(actualApplied,"Unsupported narrow viewport must retain hard maximum fit refusal");Assert.IsNull(applied);
                                Assert.IsTrue(cameraOwner.HasAircraftFocusContext);Assert.That(camera.transform.position,Is.EqualTo(Vector3.zero));
                                Assert.IsTrue(SkirmishMissionViewProfileApplication.TryReadRigidAircraftVisibleHierarchy(em,root,RenderBoundsGateway.Provider as IRenderVisibleGeometryPresentation,out var measured));
                                Assert.IsTrue(SkirmishMissionViewProfileApplication.TryResolve(em,out var authored));
                                Assert.IsFalse(SkirmishMissionViewProfileApplication.TryAircraftPose(measured,authored,camera.aspect,camera.nearClipPlane,out _,out _));
                                return;
                            }
                            if(!actualApplied)LogNativeFocusFailure(em,root,actor,camera,cameraOwner,focusEvents,started);
                            Assert.IsTrue(actualApplied,"Actual first Applied rejected; inspect AircraftProductionFocusNativeDiagnostic, preserving all geometry gates");Assert.IsNotNull(applied);
                            Assert.That(applied.fieldOfView,Is.EqualTo(camera.fieldOfView));Assert.That(applied.desiredFieldOfView,Is.EqualTo(60));
                            Assert.That(applied.cameraRotation,Is.EqualTo(camera.transform.rotation));Assert.IsFalse(camera.orthographic);
                            var captured=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
                            em.SetComponentData(actor,LocalTransform.FromPosition(new float3(200,55,100)));
                            foreach(var part in parts){var moved=em.GetComponentData<LocalToWorld>(part);moved.Value.c3.x+=100;em.SetComponentData(part,moved);}
                            Assert.IsTrue(cameraOwner.TickAircraftFocus(em,camera,false));Assert.That(camera.transform.position,Is.EqualTo(captured),"One-shot context cannot track actor movement");
                            float previousDistance=-1;int previousLevel=-2;
                            Assert.IsTrue(SkirmishMissionViewProfileApplication.TryResolve(em,out var zoomProfile));
                            for(int level=-2;level<=2;level++)
                            {
                                Assert.IsTrue(queue.QueueZoomAircraft(em,level));queue.ProcessPendingRequests(em,cameraOwner,camera);
                                int boundedLevel=Mathf.Clamp(level,-1,1);Assert.AreEqual(boundedLevel,cameraOwner.AircraftFocusZoomLevel);Assert.That(camera.fieldOfView,Is.EqualTo(fov));
                                float actualDistance=Vector3.Distance(camera.transform.position,applied.capturedCenter);
                                Assert.That(actualDistance,Is.GreaterThanOrEqualTo(zoomProfile.AircraftMinimumDistance-.001f));Assert.That(actualDistance,Is.LessThanOrEqualTo(zoomProfile.AircraftMaximumDistance+.001f));
                                if(boundedLevel>previousLevel&&previousDistance>0)Assert.That(actualDistance,Is.GreaterThan(previousDistance+.1f),"Supported viewport must have distinct ACTUAL camera distances for +/-");
                                if(boundedLevel==previousLevel)Assert.That(actualDistance,Is.EqualTo(previousDistance).Within(.001f),"Actual endstop must not move beyond its bound");
                                previousDistance=actualDistance;previousLevel=boundedLevel;
                            }
                            queue.QueueZoomAircraft(em,0);queue.ProcessPendingRequests(em,cameraOwner,camera);captured=camera.transform.position;
                            var followPosition=captured+new Vector3(20,10,5);
                            cameraOwner.UpdateTacticalFollowPose(camera,followPosition,Vector3.zero,70,0,false,0,true,rotation);
                            Assert.IsFalse(cameraOwner.TickAircraftFocus(em,camera,true));Assert.That(camera.transform.position,Is.EqualTo(followPosition),"Actual Follow owner keeps its pose");
                            cameraOwner.UpdateTacticalFollowPose(camera,captured,Vector3.zero,fov,0,false,0,true,rotation);
                            Assert.IsTrue(cameraOwner.TickAircraftFocus(em,camera,false));Assert.That(camera.transform.position,Is.EqualTo(captured));Assert.That(camera.fieldOfView,Is.EqualTo(fov));
                            Assert.IsTrue(RtsCameraSystem.ObserveNormalGroundSelection(em,repeatedGround),"Reselecting the SAME original ground selection must yield aircraft context");
                            queue.ProcessPendingRequests(em,cameraOwner,camera);Assert.IsFalse(cameraOwner.HasAircraftFocusContext);
                            Assert.IsTrue(cameraOwner.TryRequestAircraftFocus(em,actor,camera,43));Assert.IsTrue(cameraOwner.TickAircraftFocus(em,camera,false));
                            Assert.IsTrue(queue.QueuePan(em,new Vector2(1,1),.01f));queue.ProcessPendingRequests(em,cameraOwner,camera);
                            Assert.IsFalse(cameraOwner.HasAircraftFocusContext,"Ordinary pan releases the captured aircraft lens");
                            Assert.IsTrue(cameraOwner.TryRequestAircraftFocus(em,actor,camera,44));Assert.IsTrue(cameraOwner.TickAircraftFocus(em,camera,false));
                            cameraOwner.UpdateTacticalFollowPose(camera,followPosition,Vector3.zero,70,0,false,0,true,rotation);
                            var followOwner=em.CreateEntity();em.AddComponentData(followOwner,new TacticalFollowCameraPoseComponent{Valid=1});
                            Assert.IsTrue(RtsCameraSystem.ObserveNormalGroundSelection(em,repeatedGround));queue.ProcessPendingRequests(em,cameraOwner,camera);
                            Assert.IsFalse(cameraOwner.HasAircraftFocusContext);Assert.That(camera.transform.position,Is.EqualTo(followPosition),"Clearing a suspended lens cannot overwrite the active Follow owner");
                        }
                        finally{AircraftProductionFocusEvidence.Recorded-=Applied;}
                    }
                    finally{RenderBoundsGateway.Bind(originalProvider);}
                    return;
                }
                if(mutation==0){Assert.IsTrue(queue.QueueFocusAircraft(em,actor));var buffer=em.GetBuffer<RtsCameraRequestElement>(queue.EnsureCameraEntity(em));Assert.AreEqual(actor,buffer[0].TargetEntity);Assert.Greater(buffer[0].RequestId,0);return;}
                if(mutation==6){owner.SessionId="foreign-attempt";em.SetComponentData(actor,owner);}
                if(mutation==7)em.SetComponentData(state,new RuntimeGameplayStateComponent{BuildModeActive=1});
                if(mutation==8)
                {
                    var ground=em.CreateEntity();em.AddComponentData(ground,new Faction{Id=1});em.AddComponentData(ground,new UnitHealth{Current=100});em.AddComponent<SelectedUnitTag>(ground);
                }
                if(mutation==9)em.DestroyEntity(actor);
                Assert.IsFalse(cameraOwner.TickAircraftFocus(em,camera,false));Assert.IsFalse(cameraOwner.HasAircraftFocusContext);
                Assert.AreEqual(Vector3.zero,camera.transform.position,"Failed pending focus cannot manufacture an aircraft pose");
            }
            finally{AircraftProductionFocusEvidence.Recorded-=Observe;UnityEngine.Object.DestroyImmediate(go);}
        }
        public static void RunFocusedValidation()
        {var tests=new SkirmishAircraftProductionFocusTests();for(int i=0;i<13;i++)tests.NormalRequestKeepsExactPaidActorAndYieldsOwnership(i);tests.PortraitProductionFocusHasThreeActualBoundedZoomDistances();Debug.Log("[SkirmishAircraftProductionFocusTests] result=Passed tests=14 normalLensAcceptance=Pending");}
    }
}
