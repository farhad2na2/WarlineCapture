#!/usr/bin/env python3
"""Generate an isolated S026 owned-backend path diagnostic overlay."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = Path("Tools/CI/SkirmishSourceSimulation/Adapters/S026/119-123_OwnedSystemRuntime.cs")
ANCHOR = 'for(int i=0;i<count;i++)m.Invoke(boxed,new object[]{i});return default;'
DIAG = '''for(int i=0;i<count;i++){bool trace=typeof(T).FullName=="Game.Runtime.PathfindBatchJob"&&Environment.GetEnvironmentVariable("WARLINE_PATHFIND_DIAGNOSTIC")=="1";string before=null;try{if(trace)before=PathfindDiagnostic.Read(boxed,i,true);}catch(Exception e){before="inputDiagnosticError="+e.GetType().Name;}long started=trace?System.Diagnostics.Stopwatch.GetTimestamp():0L;m.Invoke(boxed,new object[]{i});if(trace)PathfindDiagnostic.Write(boxed,i,before,System.Diagnostics.Stopwatch.GetTimestamp()-started);}return default;'''
HELPER = r'''
 internal static class PathfindDiagnostic {
  static int rows,overflow; const int Limit=256;
  static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(target);
  static object At(object target,string field,int index){var array=Field(target,field);if(array==null)return "<missing>";return array.GetType().GetProperty("Item")?.GetValue(array,new object[]{index})??"<missing-item>";}
  static object Member(object target,string name){if(target==null)return "<null>";var type=target.GetType();return type.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(target)??type.GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(target)??"<missing>";}
  internal static string Read(object job,int i,bool before){object grid=At(job,"UnitGrids",i),request=At(job,"RequestedGoals",i),goal=At(job,"Goals",i);return "entity="+At(job,"Entities",i)+" start="+Member(grid,"Cell")+" desired="+Member(request,"Goal")+" assigned="+goal+" vehicle="+At(job,"MovementBehaviors",i)+" manual="+At(job,"ManualMoves",i)+" cheapMode="+At(job,"CheapSegmentModes",i)+" skipAlternate="+At(job,"AlternateSearchSkipped",i);}
  internal static void Write(object job,int i,string input,long elapsed){try{int row=System.Threading.Interlocked.Increment(ref rows);if(row>Limit){if(System.Threading.Interlocked.Increment(ref overflow)==1)Console.WriteLine("[PathfindBatchDiagnostic/v1] provenance=S026-owned-backend-source-overlay overflow=true rowLimit="+Limit);return;}Console.WriteLine("[PathfindBatchDiagnostic/v1] provenance=S026-owned-backend-source-overlay frame="+UnityEngine.Time.frameCount+" index="+i+" "+input+" assignedAfter="+At(job,"Goals",i)+" status="+At(job,"Status",i)+" failure="+At(job,"FailureCodes",i)+" finalExpansionsLastAttempt="+At(job,"ExpansionCounts",i)+" alternateAttempts="+At(job,"AlternateAttempts",i)+" elapsedTicks="+elapsed+" timerFrequency="+System.Diagnostics.Stopwatch.Frequency);}catch(Exception e){Console.WriteLine("[PathfindBatchDiagnostic/v1] provenance=S026-owned-backend-source-overlay diagnosticError="+e.GetType().Name);}}
 }
'''

def transform(source: str) -> str:
    if source.count(ANCHOR) != 1:
        raise ValueError("Fail-closed: expected exactly one OwnedJobBackend reflection loop anchor")
    if "PathfindBatchDiagnostic" in source:
        raise ValueError("Source already contains path diagnostic overlay")
    modified = source.replace(ANCHOR, DIAG, 1)
    closing = "}\nnamespace Unity.Collections{"
    if modified.count(closing) != 1:
        raise ValueError("Fail-closed: expected exact S026 namespace boundary")
    return modified.replace(closing, "}\n" + HELPER + "\nnamespace Unity.Collections{", 1)

def generate(repo: Path, output: Path) -> dict:
    root=repo.resolve(strict=True); source_path=root/SOURCE; raw=source_path.read_bytes()
    rendered=transform(raw.decode("utf-8")).encode("utf-8")
    if source_path.read_bytes()!=raw: raise ValueError("Adapter source changed during generation")
    output.mkdir(parents=True,exist_ok=False)
    candidate="119-123_OwnedSystemRuntime.PathDiagnostic.cs"
    (output/candidate).write_bytes(rendered)
    manifest={"experimental_only":True,"source_pins":{str(source_path):hashlib.sha256(raw).hexdigest()},"outputs":{candidate:hashlib.sha256(rendered).hexdigest()},"kind":"experimental-source-only-s026-owned-job-path-diagnostic","source":str(SOURCE),"source_sha256":hashlib.sha256(raw).hexdigest(),"candidate":candidate,"candidate_sha256":hashlib.sha256(rendered).hexdigest(),"output_identity":"sha256(candidate bytes)","opt_in":"WARLINE_PATHFIND_DIAGNOSTIC=1","row_limit":256,"overflow":"single overflow marker after cap; later rows omitted","frame":"UnityEngine.Time.frameCount","scope":"only actual boxed Game.Runtime.PathfindBatchJob; existing reflection Execute invocation retained","build_inputs_changed":False,"mission_run":False}
    (output/"manifest.json").write_text(json.dumps(manifest,indent=2)+"\n")
    return manifest

if __name__=="__main__":
    parser=argparse.ArgumentParser();parser.add_argument("--repo",type=Path,default=ROOT);parser.add_argument("--output",required=True,type=Path);args=parser.parse_args();print(json.dumps(generate(args.repo,args.output),indent=2))
