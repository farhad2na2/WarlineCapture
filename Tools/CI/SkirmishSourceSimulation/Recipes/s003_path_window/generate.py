"""Extend the existing opt-in source pending-path trace to the S003 divergence window."""
import hashlib
from pathlib import Path

SOURCE="Tools/CI/SkirmishSourceSimulation/Adapters/117_OriginalGroundOwners.cs"
OUTPUT_OWNER="Tools/CI/SkirmishSourceSimulation/Adapters/S026/294-312_PaidSourceOwnerStep.retry.s003.cs"
BUDGET="Assets/Game/Scripts/Systems/UnitPathfindingBudget.cs"
def generate(repo, output):
    repo=Path(repo).resolve(strict=True); raw=(repo/SOURCE).read_bytes(); text=raw.decode().replace("\r\n","\n")
    old="const int PathTraceFirstTick=240,PathTraceLastTick=320,PathTraceCapacity=96;"
    new="const int PathTraceFirstTick=1900,PathTraceLastTick=2260,PathTraceCapacity=384;"
    if text.count(old)!=1: raise ValueError("Exact path trace window required")
    data=text.replace(old,new,1).encode();output=Path(output);output.mkdir(parents=True,exist_ok=False)
    (output/"S003PathWindow.cs").write_bytes(data)
    owner_raw=(repo/OUTPUT_OWNER).read_bytes();owner=owner_raw.decode().replace("\r\n","\n")
    old=':new JObject{["enabled"]=false};';new=':new JObject{["enabled"]=false,["path_schedule_trace"]=ground.SourcePathScheduleTrace};'
    if owner.count(old)!=1:raise ValueError("Exact S003 trace-output boundary required")
    owner=owner.replace(old,new,1)
    old='ground.PathPhase()';new='ground.PathPhase(checked((int)Tick))'
    if owner.count(old)!=1:raise ValueError("Exact S003 path phase invocation required")
    owner_data=owner.replace(old,new,1).encode();(output/"S003PathTraceOutput.cs").write_bytes(owner_data)
    budget_raw=(repo/BUDGET).read_bytes();budget=budget_raw.decode().replace("\r\n","\n")
    old="            PathRequestBudgetKernel.ReportCompletedJob(ref _state,pendingFrames,pendingWallTime,requestCount,manualRequestCount,vehicleLikeCount,pendingRequestBudget);"
    new="            var sourceBefore=_state;\n"+old+"\n            SourcePathBudgetObservation.Record(sourceBefore,_state,pendingFrames,pendingWallTime,requestCount,manualRequestCount,vehicleLikeCount,pendingRequestBudget);"
    if budget.count(old)!=1:raise ValueError("Exact completed path budget call required")
    budget=budget.replace(old,new,1)+'''\nnamespace Game.Runtime { internal static class SourcePathBudgetObservation {
 static readonly System.Collections.Generic.List<string> rows=new System.Collections.Generic.List<string>();
 static SourcePathBudgetObservation(){System.AppDomain.CurrentDomain.ProcessExit+=(s,e)=>{foreach(var row in rows)System.Console.Error.WriteLine(row);};}
 internal static void Record(Game.Runtime.Pathfinding.PathRequestBudgetState before,Game.Runtime.Pathfinding.PathRequestBudgetState after,int frames,double wall,int requests,int manual,int vehicle,int budget){int frame=UnityEngine.Time.frameCount;if(System.Environment.GetEnvironmentVariable("WARLINE_SOURCE_PATH_SCHEDULE_TRACE")!="1"||frame<2150||frame>2200)return; if(rows.Count>=64)throw new System.InvalidOperationException("Path budget trace overflow"); rows.Add("[SourcePathBudget] frame="+frame+" before="+before.AdaptiveRequestsPerFrame+" after="+after.AdaptiveRequestsPerFrame+" stable_before="+before.StableOneFrameBatchCount+" stable_after="+after.StableOneFrameBatchCount+" pending_frames="+frames+" pending_wall="+wall.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" requests="+requests+" manual="+manual+" vehicle="+vehicle+" scheduled_budget="+budget); }
} }\n'''
    budget_data=budget.encode();(output/"S003PathBudgetObservation.cs").write_bytes(budget_data)
    return {"source_pins":{str(repo/SOURCE):hashlib.sha256(raw).hexdigest(),str(repo/OUTPUT_OWNER):hashlib.sha256(owner_raw).hexdigest(),str(repo/BUDGET):hashlib.sha256(budget_raw).hexdigest()},"outputs":{"S003PathWindow.cs":hashlib.sha256(data).hexdigest(),"S003PathTraceOutput.cs":hashlib.sha256(owner_data).hexdigest(),"S003PathBudgetObservation.cs":hashlib.sha256(budget_data).hexdigest()},"experimental_only":True,"scope":"Pending projection and direct completed-budget observation; no native parity"}
