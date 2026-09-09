using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string AllocationKey="Warline.M03.Probe.Allocation";
        private static bool AllocationActive=>SessionState.GetBool(AllocationKey,false);
        private static readonly int[] allocationAt={15000,60000,110000,150000};
        private static readonly string[] allocationOwners={"ThreatDetectionWarningSystem","ThreatWarningResolveSystem",
            "CampaignMissionGuidanceProjectionSystem","CampaignMissionPatrolOrderSystem","RadarPingRequestSystem",
            "MissionDefenseInteractionSystem","M03RadioReportProjectionSystem","UiCampaignMissionProjectionSystem",
            "CampaignMissionObjectiveProjectionSystem","CampaignMissionAttemptFactProjectionSystem",
            "MissionDefenseHudView.Update"};
        [Serializable] private sealed class AllocationOwner
        {public string name; public int samples,allocatedSamples,skippedNestedSamples; public long bytes,maxBytes; public List<string> allocationContexts=new();}
        [Serializable] private sealed class AllocationPhase
        {public int elapsedMilliseconds,frames; public long positiveControlBytes,totalMainThreadBytes; public AllocationOwner[] owners;}
        [Serializable] private sealed class AllocationReport
        {public string scope; public bool valid; public AllocationPhase[] phases;}
        private static readonly List<AllocationPhase> allocationPhases=new();
        private static int allocationPhase,allocationStartFrame;
        private static bool allocationRecording,allocationProfilerEnabled,allocationScriptsEnabled,allocationMemoryEnabled;
        private static AllocationPositiveControl allocationControl;
        public static void RunAllocationAttribution()=>RunChecked(()=>
        {
            allocationPhase=0; allocationRecording=false; allocationPhases.Clear();
            allocationProfilerEnabled=Profiler.enabled;
            allocationScriptsEnabled=Profiler.IsCategoryEnabled(ProfilerCategory.Scripts);
            allocationMemoryEnabled=Profiler.IsCategoryEnabled(ProfilerCategory.Memory);
            SessionState.SetBool(AllocationKey,true); RunRifleDefense();
        });
        private static bool AdvanceAllocationValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!AllocationActive) return false;
            if(runtime.Phase>=MissionPhaseKind.SecureCorridor)
            {
                FinishAllocationAttribution(); return true;
            }
            if(runtime.Phase!=MissionPhaseKind.Engage || allocationPhase>=allocationAt.Length || facts.ElapsedMilliseconds<allocationAt[allocationPhase]) return false;
            if(!allocationRecording)
            {
                Profiler.enabled=false; ProfilerDriver.ClearAllFrames();
                Profiler.SetCategoryEnabled(ProfilerCategory.Scripts,true); Profiler.SetCategoryEnabled(ProfilerCategory.Memory,true);
                Profiler.enabled=true;
                if(allocationControl==null) allocationControl=new GameObject("M3 QA allocation positive control").AddComponent<AllocationPositiveControl>();
                allocationControl.Pending=true;
                allocationStartFrame=Time.frameCount; allocationRecording=true; return false;
            }
            if(Time.frameCount-allocationStartFrame<210) return false;
            Profiler.enabled=false;
            var phase=new AllocationPhase {elapsedMilliseconds=facts.ElapsedMilliseconds,owners=allocationOwners.Select(n=>new AllocationOwner {name=n}).ToArray()};
            var markerOwners=new Dictionary<int,int>();
            for(int f=ProfilerDriver.firstFrameIndex;f<=ProfilerDriver.lastFrameIndex;f++)
            {
                using var frame=ProfilerDriver.GetRawFrameDataView(f,0);
                if(!frame.valid) continue;
                phase.frames++;
                int gc=frame.GetMarkerId("GC.Alloc");
                if(gc==FrameDataView.invalidMarkerId) continue;
                var countedUntil = new int[allocationOwners.Length];
                // Raw samples are in preorder. A sample owns its recursive child interval.
                // Read actual allocation metadata, including EditorOnly children, without subtracting a baseline.
                for(int i=0;i<frame.sampleCount;i++)
                {
                    if(frame.GetSampleMarkerId(i)==gc && frame.GetSampleMetadataCount(i)>0)
                        phase.totalMainThreadBytes+=frame.GetSampleMetadataAsLong(i,0);
                    int marker=frame.GetSampleMarkerId(i);
                    if(!markerOwners.TryGetValue(marker,out int owner))
                    {
                        string name=frame.GetSampleName(i);
                        if(string.IsNullOrEmpty(name)) continue;
                        owner=name=="M03.QA.AllocationPositiveControl" ? allocationOwners.Length : -1;
                        if(owner<0) for(int k=0;k<allocationOwners.Length;k++)
                            if(name.Contains(allocationOwners[k],StringComparison.Ordinal)) {owner=k; break;}
                        markerOwners[marker]=owner;
                    }
                    bool control=owner==allocationOwners.Length;
                    if(owner<0 && !control) continue;
                    int end=Math.Min(frame.sampleCount,i+frame.GetSampleChildrenCountRecursive(i)+1);
                    if(!control)
                    {
                        // Unity may emit an Invoke wrapper and a nested method marker
                        // with the same owner name. Count each owner's outer interval once.
                        if(i<countedUntil[owner]) {phase.owners[owner].skippedNestedSamples++; continue;}
                        countedUntil[owner]=end;
                    }
                    long bytes=0;
                    for(int j=i+1;j<end;j++) if(frame.GetSampleMarkerId(j)==gc && frame.GetSampleMetadataCount(j)>0)
                        bytes+=frame.GetSampleMetadataAsLong(j,0);
                    if(control) phase.positiveControlBytes+=bytes;
                    else
                    {
                        var measured=phase.owners[owner]; measured.samples++; measured.bytes+=bytes;
                        measured.maxBytes=Math.Max(measured.maxBytes,bytes);
                        if(bytes>0)
                        {
                            measured.allocatedSamples++;
                            if(measured.allocationContexts.Count<4)
                            {
                                var stack=new List<string>();
                                for(int j=i;j<end;j++)
                                    if(frame.GetSampleMarkerId(j)==gc && frame.GetSampleMetadataCount(j)>0)
                                    {
                                        var names=new List<string>();
                                        for(int k=i;k<j;k++)
                                            if(k+frame.GetSampleChildrenCountRecursive(k)>=j) names.Add(frame.GetSampleName(k));
                                        stack.Add(frame.GetSampleMetadataAsLong(j,0)+" B: "+string.Join(" > ",names));
                                    }
                                measured.allocationContexts.Add(string.Join("; ",stack));
                            }
                        }
                    }
                }
            }
            allocationPhases.Add(phase); allocationPhase++; allocationRecording=false;
            Debug.Log($"[M03AllocationProbe] phase={allocationPhase} frames={phase.frames} positiveControl={phase.positiveControlBytes} "+string.Join(";",phase.owners.Select(o=>$"{o.name} samples={o.samples} bytes={o.bytes}")));
            return false;
        }
        private static void FinishAllocationAttribution()
        {
            var report=new AllocationReport {
                scope="Actual live main-thread Unity raw Profiler samples in four combat phases, 210 frames per window; no double system updates. Includes EditorOnly allocations; nested markers for the same owner are counted once. This is allocation attribution, not a timing benchmark. A real 8192-byte allocation inside a player-loop marker validates each window.",
                valid=allocationPhases.Count==4 && allocationPhases.All(p=>p.frames>=180 && p.positiveControlBytes>=8192),
                phases=allocationPhases.ToArray()};
            File.WriteAllText(Output+"/allocation-attribution.json",JsonUtility.ToJson(report,true));
            Complete(report.valid,"M3 raw allocation attribution captured; inspect each owner's measured bytes; zero allocation acceptance is separate");
        }
        private static void StopAllocationAttribution()
        {
            if(!AllocationActive) return;
            Profiler.enabled=allocationProfilerEnabled;
            Profiler.SetCategoryEnabled(ProfilerCategory.Scripts,allocationScriptsEnabled);
            Profiler.SetCategoryEnabled(ProfilerCategory.Memory,allocationMemoryEnabled);
            SessionState.SetBool(AllocationKey,false);
        }
        public sealed class AllocationPositiveControl:MonoBehaviour
        {
            public bool Pending;
            private void Update()
            {
                if(!Pending) return;
                Pending=false;
                Profiler.BeginSample("M03.QA.AllocationPositiveControl");
                var sample=new byte[8192]; GC.KeepAlive(sample);
                Profiler.EndSample();
            }
        }
    }
}
