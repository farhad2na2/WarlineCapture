#!/usr/bin/env python3
"""Prepare an isolated original session-owner probe on compiled S005 setups."""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
BASE=ROOT/'T Logs/SkirmishProduction/s005-experimental-setup-export-inputs-20261105-T1'
FRAGMENT=ROOT/'T Logs/SkirmishProduction/s004-session-owner-compile-20261104-T1/S004OriginalOwnerFragments.cs'
OWNER=ROOT/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishSessionInitializationSystem.cs'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def extract(text,signature):
    start=text.index(signature); opening=text.index('{',start); depth=0
    for i in range(opening,len(text)):
        if text[i]=='{': depth+=1
        elif text[i]=='}':
            depth-=1
            if depth==0: return re.sub(r'\s+','',text[start:i+1])
    raise ValueError('Unclosed source method')

def prepare(out):
    original=OWNER.read_text();fragment=FRAGMENT.read_text()
    for signature in ('public void OnUpdate(', 'private static void TryInitialize(', 'private static void ProjectRoles(', 'private static void Fail('):
        if extract(original,signature)!=extract(fragment,signature): raise ValueError('Original owner fragment drift: '+signature)
    src=BASE/'S005LayoutCaptureReplaySourceBinding.cs.txt'; text=src.read_text()
    anchor='                    compilerResults.Add(new JObject { ["size"] = size.ToString(), ["setupHash"] = setup.SetupHash,'
    if text.count(anchor)!=1: raise ValueError('Compiler owner handoff anchor changed')
    text=text.replace(anchor, '                    var sessionProbe = ProbeOriginalSessionOwner(setup,definition);\n'+anchor)
    text=text.replace('["compiled_setup"] = JObject.FromObject(setup)', '["compiled_setup"] = JObject.FromObject(setup), ["session_probe"] = sessionProbe')
    anchor='    private static List<SkirmishSetupMatrixRow> LoadMatrix(string text)'
    method='''    private static JObject ProbeOriginalSessionOwner(SkirmishResolvedSetup setup,SkirmishScenarioDefinitionConfig definition)
    {
        int next = 400000;
        var em = new Unity.Entities.EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Unity.Entities.Entity { Index = next++, Version = 1 };
        var payload = new SkirmishLaunchPayload { CatalogId=setup.CatalogId,DefinitionId=setup.DefinitionId,
            ContentVersion=setup.ContentVersion,AllConfigHashes=setup.SetupHash.ToString("X8",System.Globalization.CultureInfo.InvariantCulture),
            MapId=setup.OperationMapId,LayoutId=setup.LayoutId,DifficultyId=setup.DifficultyId,SizeId=setup.SizeId,Seed=setup.Seed };
        if(!Game.Composition.SkirmishExpandedLaunchProjection.TryQueue(em,payload,setup,null,definition))
            throw new InvalidDataException("Original source launch projection rejected the S005 setup");
        using var sessions=em.CreateEntityQuery(typeof(Game.Components.SkirmishExpandedSessionComponent));
        if(sessions.CalculateEntityCount()!=1)throw new InvalidDataException("Unique source launch session required");
        var entity=sessions.GetSingletonEntity();
        var state = new Unity.Entities.SystemState { EntityManager = em, Enabled = true };
        var owner = new Game.Runtime.SkirmishSessionInitializationSystem(); owner.OnUpdate(ref state);
        var session = em.GetComponentData<Game.Components.SkirmishExpandedSessionComponent>(entity);
        var economy = em.GetComponentData<Game.Components.SkirmishEconomyStockComponent>(entity);
        if (session.InitializationComplete != 1 || session.Phase != Game.Skirmish.Contracts.SkirmishSessionPhase.Spawning || economy.Materials != setup.MaterialsEach)
            throw new InvalidDataException("Original session owner did not initialize actual S005 setup");
        bool buildingReady = Game.Runtime.SkirmishStartingBuildingsService.Step(em, entity, setup, out var reason);
        if (buildingReady || reason != Game.Skirmish.Contracts.SkirmishReasonCode.None ||
            em.HasBuffer<Game.Runtime.SkirmishStartingBuildingRequest>(entity))
            throw new InvalidDataException("Original starting-building owner did not wait for absent real boundary and grid");
        return new JObject { ["source_launch_owner"]="SkirmishExpandedLaunchProjection.TryQueue", ["native_input_claimed"]=false,
            ["owner"] = "SkirmishSessionInitializationSystem.OnUpdate", ["initialized"] = true,
            ["phase"] = session.Phase.ToString(), ["materials"] = economy.Materials,
            ["starting_buildings"] = "WaitingForBoundaryAndGrid", ["building_requests"] = 0,
            ["scope"] = "Source diagnostic session only; no placement or native input" };
    }

'''
    if text.count(anchor)!=1: raise ValueError('Probe method anchor changed')
    text=text.replace(anchor,method+anchor)
    out.mkdir(parents=True,exist_ok=False)
    (out/'S005LayoutCaptureReplaySourceBinding.cs.txt').write_text(text)
    for name in ('S005DiagnosticJsonUtilityAdapter.cs.txt','S005OriginalCompilerSourceBinding.cs.txt','native-receipt.json'):
        (out/name).write_bytes((BASE/name).read_bytes())
    pins={str(p):sha(p) for p in (OWNER,FRAGMENT,src,Path(__file__).resolve())}
    (out/'preparation.json').write_text(json.dumps({'status':'PreparedOnly','source_pins':pins,'original_owner_methods_verified':4,
        'native_parity':False,'mission_ready':False},indent=2)+'\n')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',required=True,type=Path);prepare(p.parse_args().output.resolve())
