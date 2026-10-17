#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        enum WarmObservation { None, PoseOnly, ScreenshotOnly }
        static WarmObservation warmObservation;
        static bool warmActive,warmStarted,warmDone;
        static string warmPath,warmError,warmAttempt;
        static int warmStartFrame;
        const double WarmStageBudgetSeconds=180;
        static double warmOriginalStartupDeadline,warmStageDeadline;
        static int warmEofCount,warmLastEofFrame;
        static SkirmishWarmRenderDiagnosticFrame warmOwner;
        static readonly int[] WarmOffsets={15,34,71,90};
        [Serializable] sealed class WarmFact
        {
            public string phase,mode,attempt,error,stableActor,png,pngSha256;
            public int frame,startFrame,offset,sample,actorIndex,actorVersion;
            public int renderedFrame,eofCount,lastEofFrame;
            public double editorTime,realtime,originalStartupDeadline,warmDeadline;
            public bool ownerCreated,ownerNativeDestroyed;
            public float timeScale,fieldOfView; public Vector3 cameraPosition,cameraEuler;
            public bool poseInvoked,pngInvoked,readinessClaim=false;
            public SkirmishCameraActorMeasurement measurement;
            public System.Collections.Generic.List<SkirmishCameraActorMeasurement> measurements;
        }
        public static void RunS005WarmRenderNoObservation()=>BeginWarmRender(WarmObservation.None);
        public static void RunS005WarmRenderPoseOnly()=>BeginWarmRender(WarmObservation.PoseOnly);
        public static void RunS005WarmRenderScreenshotOnly()=>BeginWarmRender(WarmObservation.ScreenshotOnly);
        static void BeginWarmRender(WarmObservation mode)
        {
            if(warmActive||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Fresh idle owned diagnostic Editor required");
            string census=Environment.GetEnvironmentVariable("WARLINE_S005_EXIT_DIAGNOSTIC_OUTPUT");
            if(string.IsNullOrWhiteSpace(census)||!Path.IsPathRooted(census))throw new InvalidOperationException("Fresh persistent absolute startup diagnostic output required");
            warmPath=Path.GetFullPath(census)+".warm.jsonl";
            string allowed=Path.GetFullPath("Logs/SkirmishProduction")+Path.DirectorySeparatorChar;
            if(!warmPath.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Warm evidence must remain in project Logs/SkirmishProduction");
            Directory.CreateDirectory(Path.GetDirectoryName(warmPath));using(var stream=new FileStream(warmPath,FileMode.CreateNew,FileAccess.Write)){}
            warmObservation=mode;warmStarted=false;warmDone=false;warmError=null;warmAttempt=null;warmActive=true;
            warmOriginalStartupDeadline=warmStageDeadline=0;warmEofCount=0;warmLastEofFrame=0;warmOwner=null;
            try{RunS005NormalStartupExitDiagnostic();}
            catch{warmActive=false;throw;}
            WriteWarm(new WarmFact{phase="Requested",mode=mode.ToString(),frame=Time.frameCount,timeScale=Time.timeScale});
        }
        // Called only inside the original startup-only branch, after unchanged
        // normal shader/packed rendering readiness. It yields the existing owner.
        static bool TickS005WarmRenderExitDiagnostic(EntityManager em,Entity session)
        {
            if(!warmActive)return false;
            if(!Mathf.Approximately(Time.timeScale,1)||!SkirmishLaunchProjection.IsSimulationActive(em))throw new InvalidOperationException("Warm diagnostic lost normal simulation");
            if(!string.IsNullOrEmpty(warmError))throw new InvalidOperationException("Warm diagnostic: "+warmError);
            if(!warmStarted)
            {
                double readyNow=EditorApplication.timeSinceStartup;
                if(deadline<=0||readyNow>deadline)throw new InvalidOperationException("Warm readiness must meet the unchanged original startup deadline");
                warmOriginalStartupDeadline=deadline;
                // Only this explicit diagnostic gets a fresh bounded window,
                // after the original shader/packed/session readiness has passed.
                warmStageDeadline=readyNow+WarmStageBudgetSeconds;deadline=warmStageDeadline;
                warmAttempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString();warmStartFrame=Time.frameCount;warmStarted=true;
                var owner=new GameObject("S005WarmRenderDiagnosticOwner").AddComponent<SkirmishWarmRenderDiagnosticFrame>();
                warmOwner=owner;
                owner.StartCoroutine(ObserveWarmFrames(owner));
                WriteWarm(new WarmFact{phase="NativeRenderReady",mode=warmObservation.ToString(),attempt=warmAttempt,frame=Time.frameCount,startFrame=warmStartFrame,timeScale=Time.timeScale});
                return true;
            }
            if(!warmDone)return true;
            if(warmEofCount!=4||warmLastEofFrame-warmStartFrame<90)throw new InvalidOperationException("Four actual EOF opportunities and normal warm interval required");
            WriteWarm(new WarmFact{phase="WarmStageCompleted",mode=warmObservation.ToString(),attempt=warmAttempt,frame=Time.frameCount,startFrame=warmStartFrame,offset=Time.frameCount-warmStartFrame,timeScale=Time.timeScale});
            warmActive=false;
            Debug.Log("[S005WarmRenderExitDiagnostic] result=Collected mode="+warmObservation+" samples=4 minimumFrameOffset=90 readinessClaim=0 wrapperExitStillRequired=1");
            return false; // Original startup roster audit, source Finish and Exit own completion.
        }
        static IEnumerator ObserveWarmFrames(SkirmishWarmRenderDiagnosticFrame owner)
        {
            for(int sample=0;sample<WarmOffsets.Length;sample++)
            {
                while(Time.frameCount-warmStartFrame<WarmOffsets[sample])yield return null;
                yield return new WaitForEndOfFrame();
                try
                {
                    var world=World.DefaultGameObjectInjectionWorld;
                    if(world==null||!world.IsCreated||!SkirmishLaunchProjection.TryGet(world.EntityManager,out var session,out _)||!SkirmishLaunchProjection.IsSimulationActive(world.EntityManager))
                        throw new InvalidOperationException("Actual normal World/session lost");
                    var em=world.EntityManager;var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                    if(native.SessionId.ToString()!=warmAttempt||!Mathf.Approximately(Time.timeScale,1))throw new InvalidOperationException("Attempt or normal speed changed");
                    var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Actual camera missing");
                    var row=new WarmFact{phase="EndOfFrameSample",mode=warmObservation.ToString(),attempt=warmAttempt,frame=Time.frameCount,startFrame=warmStartFrame,
                        offset=Time.frameCount-warmStartFrame,sample=sample,timeScale=Time.timeScale,cameraPosition=camera.transform.position,cameraEuler=camera.transform.eulerAngles,fieldOfView=camera.fieldOfView};
                    if(warmObservation==WarmObservation.PoseOnly)
                    {
                        if(!em.HasComponent<SkirmishS005OpeningFocusOwner>(session))throw new InvalidOperationException("Native opening focus owner missing");
                        var focus=em.GetComponentData<SkirmishS005OpeningFocusOwner>(session);Entity actor=Entity.Null;
                        using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(UnitHealth));
                        using var entities=query.ToEntityArray(Allocator.Temp);
                        foreach(var candidate in entities)
                        {
                            var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);
                            if(owned.SessionId!=native.SessionId||owned.StableObjectId!=focus.ActorId)continue;
                            if(actor!=Entity.Null||owned.FactionId!=1||owned.CapacityReleased!=0||em.GetComponentData<UnitHealth>(candidate).Current<=0)throw new InvalidOperationException("Exact live own focus actor lineage invalid");
                            actor=candidate;
                        }
                        if(actor==Entity.Null)throw new InvalidOperationException("Exact normal focus actor missing");
                        row.actorIndex=actor.Index;row.actorVersion=actor.Version;row.stableActor=focus.ActorId.ToString();row.poseInvoked=true;
                        row.measurements=new System.Collections.Generic.List<SkirmishCameraActorMeasurement>();
                        foreach(var candidate in entities)
                        {
                            var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);
                            if(owned.FactionId!=1||owned.CapacityReleased!=0||owned.SessionId!=native.SessionId||em.GetComponentData<UnitHealth>(candidate).Current<=0||
                                !em.HasComponent<SkirmishUnitRoleComponent>(candidate)||em.GetComponentData<SkirmishUnitRoleComponent>(candidate).Category.ToString()!="Infantry")continue;
                            var measured=SkirmishCameraFramingMeasurement.Capture(em,candidate,camera);
                            row.measurements.Add(measured);if(candidate==actor)row.measurement=measured;
                        }
                        if(row.measurement==null)throw new InvalidOperationException("Focused actor missing from actual all-infantry measurement workload");
                    }
                    if(warmObservation==WarmObservation.ScreenshotOnly)
                    {
                        row.pngInvoked=true;row.png=warmPath+".sample"+sample+"-frame"+Time.frameCount+".png";
                        var texture=ScreenCapture.CaptureScreenshotAsTexture();if(texture==null)throw new InvalidOperationException("Actual screenshot texture missing");
                        byte[] bytes;try{bytes=texture.EncodeToPNG();}finally{UnityEngine.Object.Destroy(texture);}
                        if(bytes==null||bytes.Length<32)throw new InvalidOperationException("Actual PNG empty");
                        using(var stream=new FileStream(row.png,FileMode.CreateNew,FileAccess.Write,FileShare.Read))stream.Write(bytes,0,bytes.Length);
                        using var sha=SHA256.Create();row.pngSha256=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
                    }
                    warmEofCount++;warmLastEofFrame=row.frame;WriteWarm(row);
                }
                catch(Exception error){warmError=error.ToString();WriteWarm(new WarmFact{phase="Failed",mode=warmObservation.ToString(),attempt=warmAttempt,frame=Time.frameCount,error=warmError,timeScale=Time.timeScale});break;}
            }
            warmDone=true;UnityEngine.Object.Destroy(owner.gameObject);
        }
        static void WriteWarm(WarmFact row)
        {
            row.editorTime=EditorApplication.timeSinceStartup;row.realtime=Time.realtimeSinceStartupAsDouble;row.renderedFrame=Time.renderedFrameCount;
            row.eofCount=warmEofCount;row.lastEofFrame=warmLastEofFrame;row.originalStartupDeadline=warmOriginalStartupDeadline;row.warmDeadline=warmStageDeadline;
            row.ownerCreated=warmStarted;row.ownerNativeDestroyed=warmStarted&&warmOwner==null;
            File.AppendAllText(warmPath,JsonUtility.ToJson(row)+"\n");
        }
    }
    public sealed class SkirmishWarmRenderDiagnosticFrame:MonoBehaviour {}
}
#endif
