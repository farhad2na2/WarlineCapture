using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string PerformanceKey="Warline.M03.Probe.Performance";
        private const string FocusedPerformanceKey="Warline.M03.Probe.Performance.FocusedGameView";
        private static bool PerformanceActive=>SessionState.GetBool(PerformanceKey,false);
        private static readonly string[] performanceMarkers={
            "Default World Game.Runtime.UnitPathfindingSystem",
            "Default World Game.Runtime.ThreatDetectionWarningSystem",
            "Default World Game.Runtime.ThreatWarningResolveSystem",
            "Default World Game.Runtime.CampaignMissionGuidanceProjectionSystem",
            "Default World Game.Runtime.CampaignMissionPatrolOrderSystem",
            "Default World Game.Runtime.RadarPingRequestSystem",
            "MainMenuPlayUI.MinimapUpdate",
            "BuildingDefenseAttackSystem.TargetSelection",
            "Default World Unity.Entities.SimulationSystemGroup"};
        private sealed class PerformanceSamples
        {
            public readonly string Name;
            public readonly List<double> Frames=new(24000),Allocated=new(24000),UnityAllocated=new(24000);
            public PerformanceSamples(string name) {Name=name;}
        }
        private static PerformanceSamples[] performancePhases;
        private static List<double>[] performanceSystems;
        private static ProfilerRecorder[] performanceRecorders;
        private static ProfilerRecorder performanceGcRecorder;
        private static bool performanceThreadCounterAvailable;
        private static int performanceFrame,performanceWarmup;
        private static long performanceAllocated;
        private static double performancePeakMemory,performancePeakMono;
        private static bool performanceStarted;
        private static EditorWindow performanceGameView;
        private static bool performanceOldMaximized;
        public static void RunFocusedGameViewPerformanceValidation()
        {
            SessionState.SetBool(FocusedPerformanceKey,true);
            RunPerformanceValidation();
        }
        public static void RunPerformanceValidation()=>RunChecked(()=>
        {
            performancePhases=new[]{new PerformanceSamples("preparation"),new PerformanceSamples("vanguard active"),
                new PerformanceSamples("main warning"),new PerformanceSamples("main active")};
            performanceSystems=performanceMarkers.Select(_=>new List<double>(24000)).ToArray();
            performanceStarted=false; performanceFrame=-1; performanceWarmup=0; performancePeakMemory=performancePeakMono=0;
            SessionState.SetBool(PerformanceKey,true); RunRifleDefense();
        });
        private static bool AdvancePerformanceValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!PerformanceActive) return false;
            if(runtime.Phase>=MissionPhaseKind.SecureCorridor)
            {FinishPerformance(em,root,in runtime,in facts); return true;}
            if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<5000) return false;
            if(!performanceStarted)
            {
                if(SessionState.GetBool(FocusedPerformanceKey,false))
                {
                    MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
                    performanceGameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                    performanceOldMaximized=performanceGameView.maximized;
                    performanceGameView.maximized=true;performanceGameView.Focus();
                }
                performanceRecorders=new ProfilerRecorder[performanceMarkers.Length];
                for(int i=0;i<performanceMarkers.Length;i++) performanceRecorders[i]=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,performanceMarkers[i]);
                performanceGcRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
                long before=GC.GetAllocatedBytesForCurrentThread();
                var allocationControl=new byte[8192]; GC.KeepAlive(allocationControl);
                performanceThreadCounterAvailable=GC.GetAllocatedBytesForCurrentThread()-before>=8192;
                Debug.Log($"[M03PerformanceProbe] allocation positive control currentThreadAvailable={performanceThreadCounterAvailable} unityFrameCounterAvailable={performanceGcRecorder.Valid}");
                performanceStarted=true;
            }
            if(performanceFrame==Time.frameCount) return false;
            performanceFrame=Time.frameCount;
            long allocated=GC.GetAllocatedBytesForCurrentThread();
            performanceWarmup++;
            if(performanceWarmup>180 && facts.ElapsedMilliseconds>=15000)
            {
                int phase=facts.ElapsedMilliseconds<45000 ? 0 : facts.ElapsedMilliseconds<100000 ? 1 : facts.ElapsedMilliseconds<140000 ? 2 : 3;
                performancePhases[phase].Frames.Add(Time.unscaledDeltaTime*1000d);
                performancePhases[phase].Allocated.Add(Math.Max(0,allocated-performanceAllocated));
                if(performanceGcRecorder.Valid) performancePhases[phase].UnityAllocated.Add(performanceGcRecorder.LastValue);
                for(int i=0;i<performanceRecorders.Length;i++)
                    if(performanceRecorders[i].Valid) performanceSystems[i].Add(performanceRecorders[i].LastValue/1000000d);
                performancePeakMemory=Math.Max(performancePeakMemory,Profiler.GetTotalAllocatedMemoryLong()/1048576d);
                performancePeakMono=Math.Max(performancePeakMono,Profiler.GetMonoUsedSizeLong()/1048576d);
            }
            performanceAllocated=allocated;
            return false;
        }
        [Serializable] private sealed class PerformanceDistribution
        {public int count; public double total,average,p95,p99,max;}
        [Serializable] private sealed class PerformancePhaseReport
        {public string name; public PerformanceDistribution frameMilliseconds,currentThreadAllocatedBytes,unityFrameAllocatedBytes; public bool frameBudgetPassed;}
        [Serializable] private sealed class PerformanceSystemReport
        {public string name; public bool available; public PerformanceDistribution milliseconds;}
        [Serializable] private sealed class PerformanceLimits
        {public double editorP95FrameBudgetMs; public int minimumFrameCount; public long currentThreadAllocatedBytesBudget;}
        [Serializable] private sealed class PerformanceReport
        {
            public string scope,environment,allocationScope,unitScaleScope,utc;
            public float timeScale;
            public bool frameBudgetsPassed,zeroAllocationTargetPassed,combatCompleted,currentThreadCounterAvailable,unityFrameGcCounterAvailable,gameViewMaximized;
            public int screenWidth,screenHeight,qualityLevel;
            public int elapsedMilliseconds,missionMembers,hostilesDefeated,unitEntities,missileProjectiles,minimapMarkers,activeUiBehaviours;
            public double peakUnityAllocatedMegabytes,peakMonoMegabytes;
            public PerformanceLimits limits;
            public PerformancePhaseReport[] phases;
            public PerformanceSystemReport[] systems;
        }
        private static PerformanceDistribution Distribution(List<double> values)
        {
            if(values.Count==0) return new PerformanceDistribution();
            double[] sorted=values.ToArray(); Array.Sort(sorted);
            return new PerformanceDistribution {count=sorted.Length,total=sorted.Sum(),average=sorted.Average(),
                p95=sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*.95)-1)],
                p99=sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*.99)-1)],max=sorted[^1]};
        }
        private static void FinishPerformance(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            var limits=JsonUtility.FromJson<PerformanceLimits>(File.ReadAllText("Design/Architecture/performance_regression_accepted_baseline.json"));
            if(limits.editorP95FrameBudgetMs<=0 || limits.minimumFrameCount<=0) throw new InvalidOperationException("Tracked Editor performance limits are missing.");
            var report=new PerformanceReport {
                scope="Real M3 rifle defense, normal Editor rendering, 180-frame / 15-second warmup; capture and probe diagnostics disabled while sampling",
                environment=Application.unityVersion+" / "+SystemInfo.operatingSystem+" / "+SystemInfo.graphicsDeviceName,
                allocationScope="Inclusive Editor main-thread allocations between rendered frames, including the game and probe. These cannot be attributed to M3 alone.",
                unitScaleScope="Canonical M3 20-member roster; does not replace the separate 700-unit / 600-building regression fixture.",
                utc=DateTime.UtcNow.ToString("O"),timeScale=Time.timeScale,limits=limits,
                currentThreadCounterAvailable=performanceThreadCounterAvailable,unityFrameGcCounterAvailable=performanceGcRecorder.Valid,
                screenWidth=Camera.main!=null ? Camera.main.pixelWidth : Screen.width,
                screenHeight=Camera.main!=null ? Camera.main.pixelHeight : Screen.height,
                gameViewMaximized=performanceGameView!=null && performanceGameView.maximized,qualityLevel=QualitySettings.GetQualityLevel(),
                elapsedMilliseconds=facts.ElapsedMilliseconds,missionMembers=em.GetBuffer<CampaignMissionDefenseMember>(root).Length,
                hostilesDefeated=facts.HostileDefeatedCount,combatCompleted=facts.HostileDefeatedCount==7 && facts.CoreBreached==0 && facts.ForwardPostDestroyed==0 && runtime.Outcome!=MissionOutcomeKind.Defeat,
                peakUnityAllocatedMegabytes=performancePeakMemory,peakMonoMegabytes=performancePeakMono};
            report.phases=performancePhases.Select(p=>new PerformancePhaseReport {name=p.Name,frameMilliseconds=Distribution(p.Frames),
                currentThreadAllocatedBytes=performanceThreadCounterAvailable ? Distribution(p.Allocated) : null,
                unityFrameAllocatedBytes=performanceGcRecorder.Valid ? Distribution(p.UnityAllocated) : null}).ToArray();
            foreach(var phase in report.phases) phase.frameBudgetPassed=phase.frameMilliseconds.count>=limits.minimumFrameCount && phase.frameMilliseconds.p95<=limits.editorP95FrameBudgetMs;
            report.frameBudgetsPassed=report.phases.All(p=>p.frameBudgetPassed);
            report.zeroAllocationTargetPassed=performanceThreadCounterAvailable && report.phases.Sum(p=>p.currentThreadAllocatedBytes.total)<=limits.currentThreadAllocatedBytesBudget;
            report.systems=performanceMarkers.Select((name,i)=>new PerformanceSystemReport {name=name,available=performanceRecorders[i].Valid,milliseconds=Distribution(performanceSystems[i])}).ToArray();
            using(var units=em.CreateEntityQuery(typeof(UnitHealth))) report.unitEntities=units.CalculateEntityCount();
            using(var air=em.CreateEntityQuery(typeof(AirMissileProjectileComponent))) report.missileProjectiles=air.CalculateEntityCount();
            using(var ground=em.CreateEntityQuery(typeof(GroundMissileProjectileComponent))) report.missileProjectiles+=ground.CalculateEntityCount();
            using(var markers=em.CreateEntityQuery(typeof(MatchHudMinimapMarkerElement)))
                if(markers.CalculateEntityCount()==1) report.minimapMarkers=em.GetBuffer<MatchHudMinimapMarkerElement>(markers.GetSingletonEntity()).Length;
            report.activeUiBehaviours=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.UIBehaviour>(FindObjectsInactive.Exclude).Length;
            File.WriteAllText(Output+"/editor-performance.json",JsonUtility.ToJson(report,true));
            StopPerformanceRecorders();
            Complete(report.combatCompleted && report.frameBudgetsPassed && report.zeroAllocationTargetPassed,
                $"M3 real-time Editor performance recorded; combat={report.combatCompleted} frameBudget={report.frameBudgetsPassed} inclusiveZeroAllocationTarget={report.zeroAllocationTargetPassed}; report={Output}/editor-performance.json");
        }
        private static void StopPerformanceRecorders()
        {
            if(performanceRecorders!=null) for(int i=0;i<performanceRecorders.Length;i++) performanceRecorders[i].Dispose();
            performanceRecorders=null;
            if(performanceGcRecorder.Valid) performanceGcRecorder.Dispose();
            performanceGcRecorder=default;
            if(performanceGameView!=null) performanceGameView.maximized=performanceOldMaximized;
            performanceGameView=null;
            SessionState.SetBool(FocusedPerformanceKey,false);
        }
    }
}
