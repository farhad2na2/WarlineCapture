"""CURRENT source path partial materialization. No stored gameplay body or old-body fallback."""
from pathlib import Path
import argparse,hashlib,json
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
SOURCE_FILES={
 'UnitPathfindingScheduler.profile.cs':'Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs',
 'UnitPathLiveUnitSnapshot.cs':'Assets/Game/Scripts/Systems/UnitPathLiveUnitSnapshot.cs',
 'UnitPathPlacementValidation.cs':'Assets/Game/Scripts/Systems/UnitPathPlacementValidation.cs'}
REQUIRED_CURRENT_LINKS=['Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs','Assets/Game/Scripts/Systems/Pathfinding/PathUnitCoverageKernel.cs','Assets/Game/Scripts/Systems/Pathfinding/PathPlacementKernel.cs']
PROFILE_EDITS=[
 ('            double startTime = Time.realtimeSinceStartupAsDouble;','            double startTime = Time.realtimeSinceStartupAsDouble;\n            double copiedGridSeconds=0,ownedJobSeconds=0;'),
 ('                gridSnapshot.Capture(','                double gridCopyStart=Time.realtimeSinceStartupAsDouble;\n                gridSnapshot.Capture('),
 ('                    occupied);','                    occupied);\n                copiedGridSeconds=Time.realtimeSinceStartupAsDouble-gridCopyStart;'),
 ('                JobHandle pendingPathHandle = job.Schedule(requestCount, default);','                double ownedJobStart=Time.realtimeSinceStartupAsDouble;\n                JobHandle pendingPathHandle = job.Schedule(requestCount, default);\n                ownedJobSeconds=Time.realtimeSinceStartupAsDouble-ownedJobStart;'),
 ('                double elapsed = Time.realtimeSinceStartupAsDouble - startTime;','                double elapsed = Time.realtimeSinceStartupAsDouble - startTime;\n                System.Console.Error.WriteLine("[PathPhaseProfile] "+new Newtonsoft.Json.Linq.JObject{["frame"]=Time.frameCount,["total"]=elapsed,["scratch"]=afterScratchTime-afterGridTime,["live_snapshot"]=afterSnapshotTime-afterScratchTime,["request_collect"]=afterRequestCollectTime-afterSnapshotTime,["goal_assign"]=afterGoalAssignTime-afterRequestCollectTime,["grid_snapshot"]=copiedGridSeconds,["owned_job"]=ownedJobSeconds,["requests"]=requestCountForLog}.ToString(Newtonsoft.Json.Formatting.None));')]
def profile(source):
 for before,after in PROFILE_EDITS:
  if source.count(before)!=1:raise ValueError('Unique current scheduler profiling anchor required: '+before)
  source=source.replace(before,after,1)
 return source

def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);paths=list(dict.fromkeys(list(SOURCE_FILES.values())+REQUIRED_CURRENT_LINKS));raw={name:(repo/name).read_bytes()for name in paths};source={name:data.decode('utf-8').replace('\r\n','\n')for name,data in raw.items()};files={}
 for filename,path in SOURCE_FILES.items():files[filename]=profile(source[path])if filename.endswith('.profile.cs')else source[path]
 # These are required live source links, not copies stripped to match the pre-broadphase victory fixture.
 if 'LiveUnitCoverage = liveUnitSnapshot.Coverage'not in source[SOURCE_FILES['UnitPathfindingScheduler.profile.cs']]or'PathUnitCoverageKernel'not in source[SOURCE_FILES['UnitPathLiveUnitSnapshot.cs']]or'CanPlaceForPathingWithCoverage'not in source[SOURCE_FILES['UnitPathPlacementValidation.cs']]:raise ValueError('Current coherent shared broadphase contract missing; never fall back to old gameplay')
 if any((repo/name).read_bytes()!=data for name,data in raw.items()):raise ValueError('Current path source drift during extraction')
 for name,text in files.items():write_lf(output/name,text)
 proof={'kind':'CurrentNativePathPartialProjection','source_inputs':{str(repo/name):hashlib.sha256(data).hexdigest()for name,data in raw.items()},'required_current_links':[str(repo/name)for name in REQUIRED_CURRENT_LINKS],'outputs':{name:hashlib.sha256(text.encode()).hexdigest()for name,text in files.items()},'bindings':['Scheduler profiling-only timing additions; elapsed used by original budget computed before logging','Snapshot and placement files byte-exact CURRENT native; existing source-compatible managed collection backend','No engine/visibility/gameplay branch removed'],'coverage_upgrade':'Includes current b121 shared live-unit coverage across scheduler/snapshot/placement AND current companion job/kernel. Old victory deliberately predates this optimization; no false old-body equality.'};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if any((repo/name).read_bytes()!=data for name,data in raw.items()):raise ValueError('Current path source drift during output')
 return proof
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--repo',required=True);parser.add_argument('--output',required=True);args=parser.parse_args();generate(args.repo,args.output)
