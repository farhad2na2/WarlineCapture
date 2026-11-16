"""Measure original source availability-scope construction without caching."""
from pathlib import Path

def apply(arguments,output):
    matches=[i for i,p in enumerate(arguments) if p.endswith('/454_SourceCampReadScope.cs')]
    if len(matches)!=1: raise ValueError('Exact original source availability scope required')
    text=Path(arguments[matches[0]]).read_text()
    def once(old,new):
        nonlocal text
        if text.count(old)!=1: raise ValueError('Scope timing seam changed: '+old[:100])
        text=text.replace(old,new)
    once('internal sealed class SourceCampReadScope:IDisposable{', '''internal static class SourceS004ScopeTiming {
 internal static long query,identity,catalogCheck,metadata;internal static int calls;
 internal static long Now()=>System.Diagnostics.Stopwatch.GetTimestamp();
 internal static double Seconds(long x)=>x/(double)System.Diagnostics.Stopwatch.Frequency;
 internal static void Reset(){query=identity=catalogCheck=metadata=0;calls=0;}
 internal static Newtonsoft.Json.Linq.JObject Snapshot()=>new Newtonsoft.Json.Linq.JObject{["scope"]="OriginalSourceAvailabilityScopeConstructionTiming",["calls"]=calls,["query_seconds"]=Seconds(query),["identity_seconds"]=Seconds(identity),["catalog_check_seconds"]=Seconds(catalogCheck),["metadata_seconds"]=Seconds(metadata),["mission_ready"]=false};
}
internal sealed class SourceCampReadScope:IDisposable{''')
    once('Em=em;Map=map;using var q=em.CreateEntityQuery', 'Em=em;Map=map;SourceS004ScopeTiming.calls++;long sourceScopeMark=SourceS004ScopeTiming.Now();using var q=em.CreateEntityQuery')
    once('var owner=q.GetSingletonEntity();if(expectedSession', 'var owner=q.GetSingletonEntity();SourceS004ScopeTiming.query+=SourceS004ScopeTiming.Now()-sourceScopeMark;sourceScopeMark=SourceS004ScopeTiming.Now();if(expectedSession')
    once('var setup=record?.Setup;if(fixtureScope?', 'var setup=record?.Setup;SourceS004ScopeTiming.identity+=SourceS004ScopeTiming.Now()-sourceScopeMark;sourceScopeMark=SourceS004ScopeTiming.Now();if(fixtureScope?')
    once('else SourceGroundMissionFamily.RequireCatalogSetup(setup);catalog=new CatalogMetadataReadScope(em,store,facts,record);Current=this;',
         'else SourceGroundMissionFamily.RequireCatalogSetup(setup);SourceS004ScopeTiming.catalogCheck+=SourceS004ScopeTiming.Now()-sourceScopeMark;sourceScopeMark=SourceS004ScopeTiming.Now();catalog=new CatalogMetadataReadScope(em,store,facts,record);Current=this;SourceS004ScopeTiming.metadata+=SourceS004ScopeTiming.Now()-sourceScopeMark;')
    target=output/'S004TimedSourceCampReadScope.cs';target.write_text(text);arguments[matches[0]]=str(target)
    defines=[i for i,p in enumerate(arguments) if p.startswith('-define:')]
    if len(defines)!=1: raise ValueError('One source define set required')
    arguments[defines[0]]+=',WARLINE_S004_SCOPE_TIMING'
