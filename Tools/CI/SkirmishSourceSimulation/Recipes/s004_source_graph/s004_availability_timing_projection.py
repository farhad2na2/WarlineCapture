"""Timing-only source projection for recruitment read substeps; no caching."""
from pathlib import Path


def apply(arguments, output):
    matches=[i for i,p in enumerate(arguments) if p.endswith('/451_SourceCampAvailability.cs')]
    if len(matches)!=1: raise ValueError('Exact source availability adapter required')
    text=Path(arguments[matches[0]]).read_text()
    def once(old,new):
        nonlocal text
        if text.count(old)!=1: raise ValueError('Exact availability timing seam changed: '+old[:100])
        text=text.replace(old,new)
    once('internal sealed partial class SourceCampAvailability{', '''internal static class SourceS004AvailabilityTiming {
 internal static long rolePreflight,roleScope,roleBody,semanticPreflight,semanticScope,semanticKernel;internal static int roleCalls,semanticCalls;
 internal static long Now()=>System.Diagnostics.Stopwatch.GetTimestamp();
 internal static double Seconds(long ticks)=>ticks/(double)System.Diagnostics.Stopwatch.Frequency;
 internal static void Reset(){rolePreflight=roleScope=roleBody=semanticPreflight=semanticScope=semanticKernel=0;roleCalls=semanticCalls=0;}
 internal static Newtonsoft.Json.Linq.JObject Snapshot()=>new Newtonsoft.Json.Linq.JObject{["scope"]="SourceReadTiming_IncludesAllAvailabilityCallsDuringTickWindow",["role_calls"]=roleCalls,["semantic_calls"]=semanticCalls,["role_preflight_seconds"]=Seconds(rolePreflight),["role_scope_seconds"]=Seconds(roleScope),["role_body_seconds"]=Seconds(roleBody),["semantic_preflight_seconds"]=Seconds(semanticPreflight),["semantic_scope_seconds"]=Seconds(semanticScope),["semantic_kernel_seconds"]=Seconds(semanticKernel),["mission_ready"]=false};
}
internal sealed partial class SourceCampAvailability{''')
    once('prefab=null;materialsFallback=0;PreflightSemanticRequest();using var scope=new SourceCampReadScope(em,null,null,map,fixtureScope,session);',
         'prefab=null;materialsFallback=0;SourceS004AvailabilityTiming.roleCalls++;long timing=SourceS004AvailabilityTiming.Now();PreflightSemanticRequest();SourceS004AvailabilityTiming.rolePreflight+=SourceS004AvailabilityTiming.Now()-timing;timing=SourceS004AvailabilityTiming.Now();using var scope=new SourceCampReadScope(em,null,null,map,fixtureScope,session);SourceS004AvailabilityTiming.roleScope+=SourceS004AvailabilityTiming.Now()-timing;timing=SourceS004AvailabilityTiming.Now();')
    once('prefab=chosen;return chosen!=null;', 'prefab=chosen;SourceS004AvailabilityTiming.roleBody+=SourceS004AvailabilityTiming.Now()-timing;return chosen!=null;')
    once('PreflightSemanticRequest();using var scope=new SourceCampReadScope(em,null,null,map,fixtureScope,session);map.Identity(prefab);context.ResourceMaterials=resources.CurrentMaterials;\n  var failure=kernel.GetCampRequestFailure(context,prefab,materialsFallback,out var required);',
         'SourceS004AvailabilityTiming.semanticCalls++;long timing=SourceS004AvailabilityTiming.Now();PreflightSemanticRequest();SourceS004AvailabilityTiming.semanticPreflight+=SourceS004AvailabilityTiming.Now()-timing;timing=SourceS004AvailabilityTiming.Now();using var scope=new SourceCampReadScope(em,null,null,map,fixtureScope,session);map.Identity(prefab);context.ResourceMaterials=resources.CurrentMaterials;SourceS004AvailabilityTiming.semanticScope+=SourceS004AvailabilityTiming.Now()-timing;timing=SourceS004AvailabilityTiming.Now();\n  var failure=kernel.GetCampRequestFailure(context,prefab,materialsFallback,out var required);SourceS004AvailabilityTiming.semanticKernel+=SourceS004AvailabilityTiming.Now()-timing;')
    target=output/'S004TimedSourceCampAvailability.cs';target.write_text(text);arguments[matches[0]]=str(target)
    defines=[i for i,p in enumerate(arguments) if p.startswith('-define:')]
    if len(defines)!=1: raise ValueError('One source define set required')
    arguments[defines[0]]+=',WARLINE_S004_AVAILABILITY_TIMING'
