#!/usr/bin/env python3
"""Generate an isolated S005 starting-grant handoff from retained source seams."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
SESSION=ROOT/'T Logs/SkirmishProduction/s005-session-owner-probe-inputs-20261105-T4/S005LayoutCaptureReplaySourceBinding.cs.txt'
LOADER=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/GeneratedBuildingDefinitions.s004.cs'
HELPER=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/S005StartingGrantExperiment.cs.txt'
IDENTITY=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/S005IdentityImportExperiment.cs.txt'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def sub(s,a,b):
    if s.count(a)!=1:raise ValueError('Exact generation anchor required: '+a[:100])
    return s.replace(a,b)
def prepare(out):
    text=SESSION.read_text();text=sub(text,'args.Length != 4','args.Length != 7')
    text=sub(text,'            string matrix = File.ReadAllText(args[1]);',
        '            S005StartingGrantExperiment.Configure(graph, Path.GetFullPath(args[4]), Path.GetFullPath(args[5]), Path.GetFullPath(args[6]));\n            string matrix = File.ReadAllText(args[1]);')
    anchor='        return new JObject { ["source_launch_owner"]="SkirmishExpandedLaunchProjection.TryQueue",'
    text=sub(text,anchor,'        JObject grantProbe = setup.SizeId == SkirmishSizeId.Standard ? S005StartingGrantExperiment.Run(em, entity, setup) : null;\n'+anchor)
    text=sub(text,'["starting_buildings"] = "WaitingForBoundaryAndGrid", ["building_requests"] = 0,',
        '["starting_buildings"] = "InitiallyWaitedForBoundaryAndGrid", ["starting_grant_probe"] = grantProbe,')
    loader=LOADER.read_text().replace('LoadS004Source','LoadS005Source')
    loader=sub(loader,'IDictionary<string, Entity> templates, SkirmishResolvedSetup setup, string setupPath, string setupHash)',
        'IDictionary<string, Entity> templates, SkirmishResolvedSetup setup)')
    loader=sub(loader,'            SourceS004CompiledSetupInput.RequireExact(setup, setupPath, setupHash);',
        '            if (setup == null || setup.CatalogId != "S005" || setup.Seed != 1 || setup.SetupHash != 1865447784u) throw new InvalidDataException("Exact compiled S005 Standard setup required");')
    loader=loader.replace('S004OriginalFullFactoryRunwayDefinitions','S005OriginalFullFactoryRunwayDefinitions').replace('OriginalS004ProductionOverlayAndDefinitionFactory','OriginalS005ProductionOverlayAndDefinitionFactory')
    loader=sub(loader,'expected.Length != 9','expected.Length != 7')
    pins={str(p):sha(p) for p in (SESSION,LOADER,HELPER,IDENTITY,Path(__file__).resolve())}
    out.mkdir(parents=True,exist_ok=False);(out/'session.cs').write_text(text);(out/'loader.cs').write_text(loader)
    (out/'preparation.json').write_text(json.dumps({'status':'PreparedExperimentalOnly','source_pins':pins,
       'scope':'S005 source request handoff with diagnostic object identity handles; no placement or native readiness',
       'mission_ready':False},indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',required=True,type=Path);prepare(p.parse_args().output.resolve())
