#!/usr/bin/env python3
"""Prepare same-world S005 spawn with its source-authored opening view.

Source host math/world bridges remain diagnostic; no native camera, scene,
input, or readiness qualification is implied.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / "T Logs/SkirmishProduction/s005-opening-view-owner-compile-20261105-T6"
WORLD = ROOT / "T Logs/SkirmishProduction/s005-same-world-publication-20261105-T3"
PROFILE = ROOT / "T Logs/SkirmishProduction/s005-view-profile-source-control-20261105-T1"
GRAPH = ROOT / "T Logs/SkirmishProduction/s005-view-profile-source-graph-20261105-T1/source-config-graph.json"
MATH = ROOT / "T Logs/SkirmishProduction/s005-focus-math-experiment-20261105-T1"
HELPER = ROOT / "T Logs/SkirmishProduction/s005-terrain-placement-inputs-20261105-T5/helper.cs"
OUTER = ROOT / "T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004OuterSpawnProjection.cs"
MATERIALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/051_SourceConfigMaterializer.cs"
RUNNER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/run_s005_terrain_experiment.py"

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def once(s,old,new):
    if s.count(old)!=1: raise ValueError("exact source anchor required: "+old[:120])
    return s.replace(old,new,1)

def prepare(out):
    out.mkdir(parents=True,exist_ok=False)
    session=(WORLD/"session.cs").read_text()
    session=once(session,
        '            UnityEngine.Resources.BindSourceResources(new Dictionary<string, object>\n                { [SkirmishExpansionCatalogConfig.ResourceName] = catalog });',
        '            var profile=materializer.ByPath<SkirmishMissionViewProfile>("Assets/Game/Resources/SkirmishMissionViewProfile_S005.asset");\n'
        '            if(profile==null || !profile.IsValid) throw new InvalidDataException("Pinned S005 source profile invalid");\n'
        '            UnityEngine.Resources.BindSourceResources(new Dictionary<string, object>\n'
        '                { [SkirmishExpansionCatalogConfig.ResourceName] = catalog, ["SkirmishMissionViewProfile_S005"] = profile });')
    (out/"session.cs").write_text(session)
    helper=HELPER.read_text()
    helper=once(helper,
        'try { forceSucceeded=SkirmishScenarioSpawnSystem.TrySpawnLedgers(em,session,setup,out forceReason,out visualPending); }',
        'try { SkirmishScenarioSpawnSystem.TrySpawn(em,session); var actualSession=em.GetComponentData<SkirmishExpandedSessionComponent>(session); '
        'forceSucceeded=actualSession.SpawnComplete==1 && actualSession.Phase==SkirmishSessionPhase.Playing; '
        'forceReason=actualSession.FailureCode;visualPending=actualSession.SpawnVisualPending; }')
    helper=once(helper,'Source-host SkirmishScenarioSpawnSystem.TrySpawnLedgers projection',
        'Source-host SkirmishScenarioSpawnSystem.TrySpawn projection including original S005 view owner')
    helper=once(helper,'        return new JObject { ["status"]=failure==null?',
        '''        JObject openingFocus = null;
        using (var requestsQuery=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent)))
        {
            int requestCount=requestsQuery.CalculateEntityCount();
            bool hasOwner=em.HasComponent<SkirmishS005OpeningFocusOwner>(session);
            openingFocus=new JObject { ["owner_present"]=hasOwner,["request_count"]=requestCount };
            if(hasOwner)
            {
                var focus=em.GetComponentData<SkirmishS005OpeningFocusOwner>(session);
                openingFocus["actor_id"]=focus.ActorId.ToString();openingFocus["attempt"]=focus.Attempt.ToString();
                openingFocus["setup_hash"]=focus.SetupHash;openingFocus["source_frame"]=focus.RequestedFrame;
                openingFocus["ground_x"]=focus.GroundRequest.x;openingFocus["ground_y"]=focus.GroundRequest.y;
                openingFocus["ground_z"]=focus.GroundRequest.z;
            }
            if(requestCount==1)
            {
                var request=requestsQuery.GetSingleton<RuntimeCameraFocusRequestComponent>();
                openingFocus["requested"]=request.Requested;openingFocus["smooth"]=request.Smooth;
                openingFocus["explicit_perspective"]=request.UseExplicitPerspective;
            }
        }
        return new JObject { ["opening_focus"]=openingFocus, ["status"]=failure==null?''')
    (out/"helper.cs").write_text(helper)
    outer=once(OUTER.read_text(),'S004SourceOuterBoundaries.AssumeNoViewProfile(em,entity,out _)',
        'SkirmishS005OpeningViewPolicy.TryQueueAfterActualSpawn(em,entity,out _)')
    (out/"outer.cs").write_text(outer)
    args=[json.loads(l) for l in (BASE/"compile.rsp").read_text().splitlines() if l.strip()]
    replacements={str(WORLD/"session.cs"):str(out/"session.cs"),str(HELPER):str(out/"helper.cs"),
        str(OUTER):str(out/"outer.cs"),str(MATERIALIZER):str(PROFILE/"SourceConfigMaterializer.cs"),
        str(BASE/"SourceMissionLogicalBindingProvider.cs"):str(MATH/"SourceMissionLogicalBindingProvider.cs")}
    for old,new in replacements.items():
        if args.count(old)!=1: raise ValueError("compile source missing or ambiguous: "+old)
        args[args.index(old)]=new
    args=["-out:"+str(out/"source-force-ledger.exe") if a.startswith("-out:") else a for a in args]
    (out/"compile.rsp").write_text("".join(json.dumps(a)+"\n" for a in args))
    runner=RUNNER.read_text();old="T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json"
    if runner.count(old)!=2: raise ValueError("runner graph anchors changed")
    runner=runner.replace(old,str(GRAPH.relative_to(ROOT)))
    (out/"run.py").write_text(runner)
    # run.py is placed in the output tree, so bind ROOT explicitly to the
    # current checkout rather than deriving it from the generated path.
    text=(out/"run.py").read_text();text=once(text,"ROOT=Path(__file__).resolve().parents[4]",'ROOT=Path('+repr(str(ROOT))+')');(out/"run.py").write_text(text)
    pins={str(p):sha(p) for p in [WORLD/"session.cs",HELPER,OUTER,MATERIALIZER,PROFILE/"SourceConfigMaterializer.cs",
        BASE/"compile.rsp",BASE/"SourceAutonomousEcs.cs",BASE/"SourceSkirmishMissionViewProfileApplication.cs",
        BASE/"SourceRuntimeCameraFocusRequestUtility.cs",MATH/"SourceMissionLogicalBindingProvider.cs",GRAPH,RUNNER,Path(__file__).resolve(),
        out/"session.cs",out/"helper.cs",out/"outer.cs",out/"compile.rsp",out/"run.py"]}
    (out/"preparation.json").write_text(json.dumps({"status":"PreparedExperimentalOnly","source_pins":pins,
        "focus_math_native_parity":False,"world_bridge_lifecycle_qualified":False,"native_scene_validated":False,
        "native_input":False,"mission_ready":False},indent=2)+"\n")

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())
