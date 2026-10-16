using System;
using Game.Components;
using Game.Composition;
using Unity.Collections;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishPlacementCameraFitTests
    {
        static Vector3[] Points(Quaternion model)
        {
            var result=new Vector3[8];var size=new Vector3(209.34288f,30.38876f,54.362015f);
            for(int i=0;i<8;i++)result[i]=new Vector3(500,15,500)+model*Vector3.Scale(size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            return result;
        }
        [TestCase(0,16f/9)][TestCase(90,16f/9)][TestCase(180,16f/9)]
        [TestCase(0,9f/16)][TestCase(90,9f/16)][TestCase(180,9f/16)]
        public void RealAirportDimensionGeometryFitsActualProjection(float yaw,float aspect)
        {
            var points=Points(Quaternion.Euler(0,yaw,0));var rotation=Quaternion.Euler(65,35,0);var usable=Rect.MinMaxRect(.02f,.22f,.72f,.86f);
            Assert.IsTrue(SkirmishPlacementCameraFit.TryFit(points,rotation,50,aspect,usable,.3f,2000,out var position));
            var go=new GameObject("PlacementProjectionControl");
            try
            {
                var camera=go.AddComponent<Camera>();camera.aspect=aspect;camera.fieldOfView=50;camera.nearClipPlane=.3f;camera.farClipPlane=2000;
                camera.transform.SetPositionAndRotation(position,rotation);
                foreach(var p in points){var v=camera.WorldToViewportPoint(p);Assert.That(v.z,Is.GreaterThanOrEqualTo(.3f));Assert.That(v.x,Is.InRange(usable.xMin-.0001f,usable.xMax+.0001f));Assert.That(v.y,Is.InRange(usable.yMin-.0001f,usable.yMax+.0001f));}
                Assert.That(position.y,Is.GreaterThan(40),"Actual large-building fit is a preview pose, not the infantry zoom cap");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void InvalidOrClippedGeometryRefusesWithoutPose(int mutation)
        {
            var points=Points(Quaternion.identity);var rect=new Rect(0,0,1,1);float aspect=16f/9,far=2000;
            if(mutation==0)points[2]=new Vector3(float.NaN,0,0);
            if(mutation==1)aspect=0;
            if(mutation==2)rect=Rect.zero;
            if(mutation==3)far=10;
            if(mutation==4)for(int i=0;i<points.Length;i++)points[i]=Vector3.one;
            Assert.IsFalse(SkirmishPlacementCameraFit.TryFit(points,Quaternion.Euler(65,35,0),50,aspect,rect,.3f,far,out var position));Assert.AreEqual(Vector3.zero,position);
        }
        [TestCase(false)][TestCase(true)]
        public void ActualRequestOwnerRestoresGroundAndPreservesActiveFollow(bool follow)
        {
            using var world=new World("PlacementCameraRequestControl");var em=world.EntityManager;
            var owner=world.GetOrCreateSystemManaged<RtsCameraSystem>();var queue=world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            var go=new GameObject("PlacementOwnedCamera");
            try
            {
                var camera=go.AddComponent<Camera>();camera.aspect=16f/9;camera.fieldOfView=46;camera.transform.SetPositionAndRotation(new Vector3(100,18,100),Quaternion.Euler(65,35,0));
                var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
                Assert.IsTrue(queue.QueuePlacementPreviewPose(em,71,new Vector3(300,200,300),Quaternion.Euler(65,35,0),50));queue.ProcessPendingRequests(em,owner,camera);
                Assert.IsTrue(owner.HasPlacementPreviewPose);Assert.AreEqual(200,camera.transform.position.y);
                queue.QueueReleasePlacementPreview(em,72);queue.ProcessPendingRequests(em,owner,camera);Assert.IsTrue(owner.HasPlacementPreviewPose,"Foreign release token must not take ownership");
                if(follow)
                {
                    var entity=em.CreateEntity();em.AddComponentData(entity,new TacticalFollowCameraPoseComponent{Valid=1});
                    owner.UpdateTacticalFollowPose(camera,new Vector3(10,80,20),Vector3.zero,60,0,false,0,true,Quaternion.Euler(50,35,0));
                }
                var followPosition=camera.transform.position;queue.QueueReleasePlacementPreview(em,71);queue.ProcessPendingRequests(em,owner,camera);Assert.IsFalse(owner.HasPlacementPreviewPose);
                if(follow)Assert.AreEqual(followPosition,camera.transform.position,"Release cannot overwrite actual Follow owner");
                else{Assert.AreEqual(oldPosition,camera.transform.position);Assert.AreEqual(oldRotation,camera.transform.rotation);Assert.AreEqual(46,camera.fieldOfView);Assert.IsFalse(camera.orthographic);}
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [TestCase(false)][TestCase(true)]
        public void PlacementLifecycleReleasesExactlyOnce(bool committed)
        {
            using var world=new World("PlacementLifecycleReleaseControl");var runtime=new RuntimeGameplayStateSystem(world.EntityManager);
            var go=new GameObject("DisposablePlacementFixture");var preview=new GameObject("DisposablePreview");var lifecycle=new BuildingPlacementLifecycleCompositionSystemHelper();
            try
            {
                var definition=new BuildingDefinition{Prefab=go,FootprintCells=Vector2Int.one};
                lifecycle.Begin(definition,new BuildingPlacementLifecycleCompositionSystemHelper.BeginContext(runtime,null,null,go.transform,(d,p)=>preview,null,null,null,null,null,null,null));
                int released=0;lifecycle.ActivePlacement.ReleaseCamera=()=>released++;
                if(committed)lifecycle.ReleasePreviewOwnership(lifecycle.ActivePlacement);
                lifecycle.Cancel(new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(null,null,null));
                lifecycle.Cancel(new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(null,null,null));
                Assert.AreEqual(1,released);Assert.IsFalse(lifecycle.HasPendingBuildingPlacement);
            }
            finally{UnityEngine.Object.DestroyImmediate(preview);UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void SmallPreviewNeverZoomsBelowExistingBuildHeight()
        {
            var points=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(0,1,1),new Vector3(1,1,1)};
            Assert.IsTrue(SkirmishPlacementCameraFit.TryFit(points,Quaternion.Euler(65,35,0),50,16f/9,new Rect(0,0,1,1),.3f,1000,out var position,20));
            Assert.That(position.y,Is.GreaterThanOrEqualTo(20));
        }
        [Test] public void UnscopedCampaignCannotAcquireNewPreviewCamera()
        {
            using var world=new World("UnscopedPlacementCameraControl");var old=World.DefaultGameObjectInjectionWorld;
            var go=new GameObject("UnscopedPreviewCamera");
            try
            {
                World.DefaultGameObjectInjectionWorld=world;var camera=go.AddComponent<Camera>();camera.transform.position=new Vector3(0,18,0);
                var owner=world.GetOrCreateSystemManaged<RtsCameraSystem>();var queue=world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
                var helper=new SelectionUiCameraSystemHelper(owner,queue);helper.Init(null,camera);
                Assert.IsFalse(helper.TryFitPlacementPreview(71,Points(Quaternion.identity),Quaternion.identity,new Rect(0,0,1,1),out var release));
                Assert.IsNull(release);Assert.IsFalse(owner.HasPlacementPreviewPose);Assert.AreEqual(new Vector3(0,18,0),camera.transform.position);
            }
            finally{World.DefaultGameObjectInjectionWorld=old;UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void LetterboxedCameraUsesItsActualViewportCoordinates()
        {
            var go=new GameObject("LetterboxedPreviewProjection");
            var surface=new RenderTexture(1920,1080,0);
            try
            {
                var camera=go.AddComponent<Camera>();camera.targetTexture=surface;camera.rect=new Rect(.1f,.2f,.7f,.6f);camera.aspect=16f/9;camera.fieldOfView=50;camera.farClipPlane=2000;
                var safe=new Rect(.2f,.25f,.5f,.5f);
                bool ambient=SkirmishPlacementCameraFit.TryViewportRect(safe,camera.pixelRect,Screen.width,Screen.height,out var ambientLocal);
                Debug.Log($"[PlacementLetterboxInputs] ambientScreen={Screen.width}x{Screen.height} target={surface.width}x{surface.height} pixelRect={camera.pixelRect.ToString("R")} rect={camera.rect.ToString("R")} safe={safe.ToString("R")} ambientConversion={ambient} ambientLocal={ambientLocal.ToString("R")}");
                Assert.That(camera.pixelRect.xMin,Is.EqualTo(surface.width*.1f).Within(.001f));
                Assert.That(camera.pixelRect.yMin,Is.EqualTo(surface.height*.2f).Within(.001f));
                Assert.That(camera.pixelRect.width,Is.EqualTo(surface.width*.7f).Within(.001f));
                Assert.That(camera.pixelRect.height,Is.EqualTo(surface.height*.6f).Within(.001f));
                Assert.IsFalse(SkirmishPlacementCameraFit.TryViewportRect(safe,camera.pixelRect,0,surface.height,out _),"A missing screen surface must retain fail-closed behavior");
                Assert.IsFalse(SkirmishPlacementCameraFit.TryViewportRect(safe,camera.pixelRect,surface.width,0,out _));
                Assert.IsTrue(SkirmishPlacementCameraFit.TryViewportRect(safe,camera.pixelRect,surface.width,surface.height,out var local));
                Assert.That(local.xMin,Is.EqualTo(1f/7).Within(.0001f));Assert.That(local.yMin,Is.EqualTo(1f/12).Within(.0001f));
                var points=Points(Quaternion.identity);var rotation=Quaternion.Euler(65,35,0);
                Assert.IsTrue(SkirmishPlacementCameraFit.TryFit(points,rotation,50,camera.aspect,local,camera.nearClipPlane,2000,out var position));
                camera.transform.SetPositionAndRotation(position,rotation);
                foreach(var p in points){var screen=camera.WorldToScreenPoint(p);Assert.That(screen.x/surface.width,Is.InRange(safe.xMin-.0001f,safe.xMax+.0001f));Assert.That(screen.y/surface.height,Is.InRange(safe.yMin-.0001f,safe.yMax+.0001f));}
            }
            finally{go.GetComponent<Camera>().targetTexture=null;UnityEngine.Object.DestroyImmediate(surface);UnityEngine.Object.DestroyImmediate(go);}
        }
        [TestCase(false)][TestCase(true)] public void ActualProviderBoundUpdateSequenceYieldsToManualPanAndReportsClipping(bool minimap)
        {
            SkirmishGenericMissionDispatchTests.WithViewBoundQueuedSession((em,session)=>
            {
                var old=World.DefaultGameObjectInjectionWorld;var go=new GameObject("BoundPreviewInputSequence");var surface=new RenderTexture(1920,1080,0);
                using var mapBootstrap=new OperationMapRuntimeBootstrapSceneSystemHelper(em.World);
                try
                {
                    World.DefaultGameObjectInjectionWorld=em.World;var camera=go.AddComponent<Camera>();camera.targetTexture=surface;camera.aspect=16f/9;camera.farClipPlane=2000;camera.fieldOfView=46;camera.transform.position=new Vector3(500,18,500);
                    var owner=em.World.GetOrCreateSystemManaged<RtsCameraSystem>();var queue=em.World.GetOrCreateSystemManaged<RtsCameraRequestSystem>();var helper=new SelectionUiCameraSystemHelper(owner,queue);helper.Init(null,camera);
                    var points=Points(Quaternion.identity);var safe=new Rect(0,0,1,1);
                    bool profileReady=SkirmishMissionViewProfileApplication.TryResolve(em,out var profile);
                    bool viewportReady=SkirmishPlacementCameraFit.TryViewportRect(safe,camera.pixelRect,surface.width,surface.height,out var local);
                    Debug.Log($"[PlacementBoundSequenceInputs] minimap={minimap} screen={Screen.width}x{Screen.height} surface={surface.width}x{surface.height} cameraPixels={camera.pixelRect.ToString("R")} profileReady={profileReady} viewportReady={viewportReady} aspect={camera.aspect:R} near={camera.nearClipPlane:R} far={camera.farClipPlane:R} ownerBefore={owner.HasPlacementPreviewPose}");
                    Assert.IsTrue(profileReady);Assert.IsTrue(viewportReady);
                    var definition=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition;
                    var scenario=new FixedString64Bytes(definition.ScenarioSetupId);var mission=new FixedString64Bytes(definition.DefinitionId);
                    Assert.IsTrue(mapBootstrap.TryPublish(definition.CampaignMap,in scenario,in mission,1,
                        OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out var mapRoot,out string mapError),mapError);
                    // Publish only actual source metadata through its normal owner.
                    // This disposable camera control does not claim streamed map readiness.
                    owner.ApplyPerspectiveCameraModeInstant(camera,18,profile.Pitch,profile.Yaw,46);
                    queue.ProcessPendingRequests(em,owner,camera);
                    Assert.IsTrue(owner.TryGetGroundBoundary(out var initializedBoundary));
                    var sourceBounds=definition.CampaignMap.Bounds;
                    Assert.That(initializedBoundary.xMin,Is.EqualTo(sourceBounds.CameraMin.x));
                    Assert.That(initializedBoundary.yMin,Is.EqualTo(sourceBounds.CameraMin.z));
                    Assert.That(initializedBoundary.xMax,Is.EqualTo(sourceBounds.CameraMax.x));
                    Assert.That(initializedBoundary.yMax,Is.EqualTo(sourceBounds.CameraMax.z));
                    Assert.That(Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(profile.Pitch,profile.Yaw,0)),Is.LessThan(.001f));
                    var originalGroundCameraPosition=camera.transform.position;
                    if(Screen.width<=0||Screen.height<=0)
                    {
                        Assert.IsFalse(helper.TryFitPlacementPreview(71,points,Quaternion.identity,safe,out var absentSurfaceRelease),"Public normal-input path must refuse missing actual Screen");
                        Assert.IsNull(absentSurfaceRelease);Assert.IsFalse(owner.HasPlacementPreviewPose);
                    }
                    Assert.IsTrue(helper.TryFitPlacementPreviewOnSurface(71,points,Quaternion.identity,safe,surface.width,surface.height,out var release),"Exact profile/surface path must fit and apply; see PlacementBoundSequenceInputs");
                    Assert.IsFalse(helper.ReadZoomControlState().ZoomInEnabled);Assert.IsFalse(helper.ReadZoomControlState().ZoomOutEnabled);
                    var fitted=camera.transform.position;var outside=(Vector3[])points.Clone();for(int i=0;i<outside.Length;i++)outside[i]+=Vector3.right*10000;
                    Assert.IsFalse(helper.TryFitPlacementPreviewOnSurface(71,outside,Quaternion.identity,safe,surface.width,surface.height,out _));Assert.AreEqual(fitted,camera.transform.position,"Dragging beyond the viewport reports clipping without camera tracking");
                    Assert.IsTrue(helper.TryFitPlacementPreviewOnSurface(71,points,Quaternion.identity,safe,surface.width,surface.height,out _));
                    Vector3 absoluteTarget=default;bool moveCenterAccepted=false;string moveCenterReason=null;
                    if(minimap)
                    {
                        Assert.IsTrue(owner.TryGetGroundBoundary(out var boundary));absoluteTarget=new Vector3(boundary.center.x,0,boundary.center.y);
                        LogPlacementMinimapFacts("BeforePublicMove",camera,owner,absoluteTarget,boundary,points);
                        helper.MoveCameraGroundCenterTo(absoluteTarget);
                        moveCenterAccepted=MinimapCenterWithinMeasuredPrecision(camera,owner,absoluteTarget,out moveCenterReason);
                        LogPlacementMinimapFacts("AfterPublicMove",camera,owner,absoluteTarget,boundary,points);
                    }
                    else{queue.QueuePan(em,new Vector2(50,0),.1f);queue.ProcessPendingRequests(em,owner,camera);}
                    var panned=camera.transform.position;
                    if(!minimap)Assert.AreNotEqual(fitted,panned);Assert.IsTrue(owner.PlacementPreviewManuallyAdjusted);
                    helper.TryFitPlacementPreviewOnSurface(71,Points(Quaternion.Euler(0,90,0)),Quaternion.Euler(0,90,0),safe,surface.width,surface.height,out _);
                    Assert.AreEqual(panned,camera.transform.position,"Normal pan owns the view; later per-frame rotation cannot recenter it");
                    release();Assert.IsFalse(owner.HasPlacementPreviewPose);Assert.That(camera.transform.position.y,Is.EqualTo(18));
                    if(minimap)
                    {
                        bool restoreCenterAccepted=MinimapCenterWithinMeasuredPrecision(camera,owner,absoluteTarget,out string restoreCenterReason);
                        Assert.IsTrue(owner.TryGetGroundBoundary(out var boundary));
                        LogPlacementMinimapFacts("AfterRelease",camera,owner,absoluteTarget,boundary,points);
                        // Actual native75523: move .02213m/.211px; restore .02016m/1.053px.
                        // Require BOTH physical-pixel centering and bounded world error,
                        // not an unsupported one-millimetre Camera ray round-trip.
                        Assert.IsTrue(moveCenterAccepted,moveCenterReason);
                        Assert.IsTrue(restoreCenterAccepted,restoreCenterReason);
                        var correctRestoredPosition=camera.transform.position;
                        try
                        {
                            // Reproduce the previous incorrect relative-preview delta:
                            // it restores old lens around oldCenter+previewPanDelta,
                            // rather than the public absolute minimap target.
                            var delta=panned-fitted;
                            camera.transform.position=originalGroundCameraPosition+new Vector3(delta.x,0,delta.z);
                            LogPlacementMinimapFacts("WrongRelativeRestoreNegative",camera,owner,absoluteTarget,boundary,points);
                            Assert.That(Vector3.Distance(owner.GetCameraGroundCenterWorld(camera),absoluteTarget),Is.GreaterThan(.5f),"Negative must reproduce a material stale-center offset, not numerical noise");
                            Assert.IsFalse(MinimapCenterWithinMeasuredPrecision(camera,owner,absoluteTarget,out _),"Measured-precision predicate must reject actual wrong relative-center restoration");
                        }
                        finally{camera.transform.position=correctRestoredPosition;}
                    }
                }
                finally{World.DefaultGameObjectInjectionWorld=old;go.GetComponent<Camera>().targetTexture=null;UnityEngine.Object.DestroyImmediate(surface);UnityEngine.Object.DestroyImmediate(go);}
            });
        }
        static bool MinimapCenterWithinMeasuredPrecision(Camera camera,RtsCameraSystem owner,Vector3 target,out string reason)
        {
            reason="Explicit1920x1080 physical render surface and finite visible target required";
            if(camera==null||camera.targetTexture==null||camera.targetTexture.width!=1920||camera.targetTexture.height!=1080||
                camera.pixelRect.width!=1920||camera.pixelRect.height!=1080)return false;
            var center=owner.GetCameraGroundCenterWorld(camera);var projected=camera.WorldToScreenPoint(target);
            bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
            if(!Finite(target)||!Finite(center)||!Finite(projected)||projected.z<=0)return false;
            float worldError=Vector3.Distance(center,target);
            float pixelError=Vector2.Distance(new Vector2(projected.x,projected.y),camera.pixelRect.center);
            reason=$"Actual public minimap center worldError={worldError:R}m physicalPixelError={pixelError:R}px depth={projected.z:R}; require <=.05m AND <=2px on1920x1080";
            return float.IsFinite(worldError)&&float.IsFinite(pixelError)&&worldError<=.05f&&pixelError<=2f;
        }
        static void LogPlacementMinimapFacts(string phase,Camera camera,RtsCameraSystem owner,Vector3 target,Rect boundary,Vector3[] points)
        {
            var ray=camera.ViewportPointToRay(new Vector3(.5f,.5f,0));var center=owner.GetCameraGroundCenterWorld(camera);var error=center-target;
            var forward=camera.transform.forward;var analytic=Mathf.Abs(forward.y)>.0001f?camera.transform.position-forward*(camera.transform.position.y/forward.y):camera.transform.position;
            var targetPixels=camera.WorldToScreenPoint(target);var centerPixels=camera.WorldToScreenPoint(center);var pixelError=new Vector2(targetPixels.x-camera.pixelRect.center.x,targetPixels.y-camera.pixelRect.center.y);
            var min=points[0];var max=min;foreach(var point in points){min=Vector3.Min(min,point);max=Vector3.Max(max,point);}
            Debug.Log($"[PlacementMinimapFacts] phase={phase} screen={Screen.width}x{Screen.height} targetSurface={(camera.targetTexture==null?"null":camera.targetTexture.width+"x"+camera.targetTexture.height)} boundary={boundary.ToString("R")} camera={camera.transform.position.ToString("R")} euler={camera.transform.eulerAngles.ToString("R")} fov={camera.fieldOfView:R} aspect={camera.aspect:R} near={camera.nearClipPlane:R} far={camera.farClipPlane:R} pixelRect={camera.pixelRect.ToString("R")} target={target.ToString("R")} center={center.ToString("R")} error={error.ToString("R")} distance={error.magnitude:R} analyticGround={analytic.ToString("R")} rayOrigin={ray.origin.ToString("R")} rayDirection={ray.direction.ToString("R")} forward={forward.ToString("R")} targetPixels={targetPixels.ToString("R")} centerPixels={centerPixels.ToString("R")} targetPixelError={pixelError.ToString("R")} pointMin={min.ToString("R")} pointMax={max.ToString("R")} previewOwned={owner.HasPlacementPreviewPose} manuallyAdjusted={owner.PlacementPreviewManuallyAdjusted} projection={camera.projectionMatrix.ToString("R")} worldToCamera={camera.worldToCameraMatrix.ToString("R")}");
        }
        public static void RunMinimapPreviewDiagnostic()
        {
            new SkirmishPlacementCameraFitTests().ActualProviderBoundUpdateSequenceYieldsToManualPanAndReportsClipping(true);
            Debug.Log("[SkirmishPlacementMinimapDiagnostic] result=Passed physicalPixelLimit=2 worldLimitMetres=.05 wrongRelativeRestoreRejected=1 nativeNormalPreviewPending=1");
        }
        [Test] public void PreviewRestoresActualOrthographicOwnerState()
        {
            using var world=new World("PreviewIsoRestore");var owner=world.GetOrCreateSystemManaged<RtsCameraSystem>();var queue=world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();var go=new GameObject("IsoRestoreCamera");
            try
            {
                var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=14;owner.NormalIsoModeActive=true;
                queue.QueuePlacementPreviewPose(world.EntityManager,71,new Vector3(100,200,100),Quaternion.Euler(65,35,0),50);queue.ProcessPendingRequests(world.EntityManager,owner,camera);
                Assert.IsFalse(camera.orthographic);Assert.IsFalse(owner.NormalIsoModeActive);
                queue.QueueReleasePlacementPreview(world.EntityManager,71);queue.ProcessPendingRequests(world.EntityManager,owner,camera);
                Assert.IsTrue(camera.orthographic);Assert.IsTrue(owner.NormalIsoModeActive);Assert.AreEqual(14,camera.orthographicSize);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishPlacementCameraFitTests();var failures=new System.Collections.Generic.List<string>();int passed=0;
            void Run(string name,Action action){try{action();passed++;}catch(Exception error){failures.Add(name+": "+error);Debug.LogError("[PlacementPreviewControl] name="+name+" error="+error);}}
            foreach(float aspect in new[]{16f/9,9f/16})foreach(float yaw in new[]{0,90,180})Run("geometry."+aspect+"."+yaw,()=>t.RealAirportDimensionGeometryFitsActualProjection(yaw,aspect));
            for(int i=0;i<5;i++){int mutation=i;Run("invalid."+i,()=>t.InvalidOrClippedGeometryRefusesWithoutPose(mutation));}
            foreach(bool value in new[]{false,true}){Run("owner."+value,()=>t.ActualRequestOwnerRestoresGroundAndPreservesActiveFollow(value));Run("lifecycle."+value,()=>t.PlacementLifecycleReleasesExactlyOnce(value));}
            Run("small",t.SmallPreviewNeverZoomsBelowExistingBuildHeight);Run("campaign",t.UnscopedCampaignCannotAcquireNewPreviewCamera);
            Run("letterbox",t.LetterboxedCameraUsesItsActualViewportCoordinates);
            Run("provider.pan",()=>t.ActualProviderBoundUpdateSequenceYieldsToManualPanAndReportsClipping(false));
            Run("provider.minimap",()=>t.ActualProviderBoundUpdateSequenceYieldsToManualPanAndReportsClipping(true));
            Run("orthographic",t.PreviewRestoresActualOrthographicOwnerState);
            Assert.That(failures.Count,Is.EqualTo(0),"All preview cases collected independently; passed="+passed+" failures="+string.Join("\n",failures));
            Assert.That(passed,Is.EqualTo(21));
            Debug.Log("[SkirmishPlacementCameraFitTests] result=Passed controls=21 nativeNormalPreviewPending=1");
        }
    }
}
