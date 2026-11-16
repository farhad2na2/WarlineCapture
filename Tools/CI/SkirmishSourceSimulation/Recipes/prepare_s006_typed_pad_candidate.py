#!/usr/bin/env python3
"""Exercise original instance-pad owners on disposable S006 configuration."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
BASE=ROOT/'T Logs/SkirmishProduction/s006-canonical-v2-candidate-20261105-T7'
PROPOSALS=ROOT/'T Logs/SkirmishProduction/s006-static-footprint-proposals-20261105-T1/report.json'
CHECKS=ROOT/'T Logs/SkirmishProduction/s006-source-footprint-owner-20261105-T3'
BUILD=ROOT/'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
GRAPH=ROOT/'T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json'
MATRIX=ROOT/'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'

METHOD=r'''static JObject TestPads(SkirmishMapLayoutConfig original,SkirmishResolvedSetup setup,OperationMapDefinition map,JObject input){
 var layout=Activator.CreateInstance<SkirmishMapLayoutConfig>();foreach(var f in typeof(SkirmishMapLayoutConfig).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly))f.SetValue(layout,f.GetValue(original));
 var frames=layout.WorldBinding;if(Math.Abs(frames.ForwardX-1f)>.00001f||Math.Abs(frames.ForwardZ)>.00001f||Math.Abs(frames.AcrossX)>.00001f||Math.Abs(frames.AcrossZ-1f)>.00001f)throw new Exception("Exact axis-aligned candidate projection required");
 var group=((JArray)input["sizes"]).OfType<JObject>().Single(r=>(string)r["size"]==setup.SizeId.ToString());var proposals=(JArray)group["proposals"];
 if(proposals.Count!=setup.Structures.Length||(uint)group["setup_hash"]!=setup.SetupHash)throw new Exception("Exact candidate structure proposal required");
 var anchors=layout.Anchors.ToList();var pads=layout.Pads.ToList();var bindings=new List<SkirmishStructureInstancePadBinding>();var seen=new Dictionary<string,int>();
 for(int i=0;i<proposals.Count;i++){var item=(JObject)proposals[i];var s=setup.Structures[i];if((int)item["structure_index"]!=i||(string)item["structure_id"]!=s.StructureId||(int)item["faction_id"]!=s.FactionId||item["proposed_origin"].Type==JTokenType.Null)throw new Exception("Proposal identity unavailable");
  int x=(int)item["proposed_origin"][0],z=(int)item["proposed_origin"][1],w=(int)item["footprint"][0],h=(int)item["footprint"][1];float centerX=x+w*.5f,centerZ=z+h*.5f;
  float u=(centerX-frames.OriginX)/frames.ForwardMetres,v=(centerZ-frames.OriginZ)/frames.AcrossMetres+.5f;float lx=layout.OriginX+u*layout.WorldWidthMetres,lz=layout.OriginZ+v*layout.WorldDepthMetres;
  string id="s006.fixture."+setup.SizeId+"."+i,anchorId=id+".anchor",padId=id+".pad";
  anchors.Add(new SkirmishLayoutAnchorConfig{AnchorId=anchorId,RoleId=id,NormalizedU=u,NormalizedV=v,WorldX=lx,WorldZ=lz});
  pads.Add(new SkirmishLegalPadConfig{PadId=padId,AnchorId=anchorId,RoleId=id,Kind=SkirmishMapLayoutBuilder.PadKindForStructure(s.StructureId),FactionId=s.FactionId,CenterX=lx,CenterZ=lz,WidthMetres=(w+2f)*layout.WorldWidthMetres/frames.ForwardMetres,DepthMetres=(h+2f)*layout.WorldDepthMetres/frames.AcrossMetres});
  string key=s.FactionId+"/"+s.StructureId;int ordinal=seen.TryGetValue(key,out int prior)?prior:0;seen[key]=ordinal+1;
  bindings.Add(new SkirmishStructureInstancePadBinding{SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,FactionId=s.FactionId,StructureId=s.StructureId,InstanceOrdinal=ordinal,AnchorId=anchorId,PadId=padId});}
 var af=typeof(SkirmishMapLayoutConfig).GetField("anchors",BindingFlags.Instance|BindingFlags.NonPublic);var pf=typeof(SkirmishMapLayoutConfig).GetField("pads",BindingFlags.Instance|BindingFlags.NonPublic);if(af==null||pf==null)throw new Exception("Exact layout arrays required");af.SetValue(layout,anchors.ToArray());pf.SetValue(layout,pads.ToArray());layout.BindStructureInstancePads(bindings.ToArray());
 var projection=new SkirmishResolvedSetup{SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,Structures=(SkirmishResolvedStructureEntry[])setup.Structures.Clone()};var reasons=new List<SkirmishCompileReason>();bool applied=SkirmishStructureInstanceBinding.TryApply(layout,projection,reasons);var diagnostics=new JArray();var grid=new Game.Components.GridConfig{Width=2048,Height=1024,CellSize=1f,Origin=new Unity.Mathematics.float3(0,0,0)};
 if(applied)for(int i=0;i<proposals.Count;i++){var p=(JObject)proposals[i];bool resolved=SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout,projection,i,out var pad);var rect=new RectInt((int)p["proposed_origin"][0],(int)p["proposed_origin"][1],(int)p["footprint"][0],(int)p["footprint"][1]);bool contained=resolved&&SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout,pad,grid,map.Bounds,rect);diagnostics.Add(new JObject{["structure_index"]=i,["resolved"]=resolved,["footprint_contained"]=contained});}
 layout.BindStructureInstancePads(bindings.Take(bindings.Count-1).ToArray());var negative=new SkirmishResolvedSetup{SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,Structures=(SkirmishResolvedStructureEntry[])setup.Structures.Clone()};bool missingRejected=!SkirmishStructureInstanceBinding.TryApply(layout,negative,new List<SkirmishCompileReason>());
 var failures=new JArray();foreach(var r in reasons)failures.Add(r.ToString());return new JObject{["binding_applied"]=applied,["missing_binding_rejected"]=missingRejected,["binding_reasons"]=failures,["checks"]=diagnostics,["scope"]="Disposable instance-binding and footprint-containment projection; not full layout compilation, spawn, live fit or readiness"};}
 '''


def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);p.add_argument('--candidate',type=Path,default=BASE);p.add_argument('--proposals',type=Path,default=PROPOSALS);p.add_argument('--checks',type=Path,default=CHECKS);p.add_argument('--compile-bound',action='store_true');p.add_argument('--declared-context',action='store_true');options=p.parse_args();out=options.output.resolve();base=options.candidate.resolve(strict=True);proposal_path=options.proposals.resolve(strict=True);checks_path=options.checks.resolve(strict=True)
    if not out.is_relative_to(ROOT):raise ValueError('Repository output required')
    if any(not path.is_relative_to(ROOT) for path in (base,proposal_path,checks_path)):raise ValueError('Repository evidence required')
    out.mkdir(parents=True,exist_ok=False);prior=json.loads((base/'receipt.json').read_bytes());check=json.loads((checks_path/'receipt.json').read_bytes());proposal=json.loads(proposal_path.read_bytes());checked=json.loads((checks_path/'result.json').read_bytes())
    if prior.get('status')!='CollectedSourceCandidate' or prior.get('result_sha256')!=sha(base/'result.json') or check.get('status')!='CollectedOriginalSourceFootprintChecks' or check.get('result_sha256')!=sha(checks_path/'result.json') or proposal.get('mission_ready') is not False:raise ValueError('Exact prior candidate and owner checks required')
    if check.get('source_pins',{}).get(str(proposal_path))!=sha(proposal_path) or proposal.get('input_sha256',{}).get(str(base/'result.json'))!=sha(base/'result.json'):raise ValueError('Candidate, proposals and original owner checks must be linked')
    if any(not all(r.get('original_rect_valid') is True and r.get('original_surface_valid') is True for r in group['rows']) for group in checked['sizes']):raise ValueError('Each proposal must pass retained source owner checks')
    pins=dict(prior['source_pins'])
    for group in (check['source_pins'],proposal['input_sha256']):
        for name,digest in group.items():
            if name in pins and pins[name]!=digest:raise ValueError('Conflicting pin: '+name)
            pins[name]=digest
    for path in (base/'receipt.json',base/'compile.rsp',base/'S006SourceCandidateControl.cs',proposal_path,checks_path/'receipt.json',checks_path/'result.json',BUILD,GRAPH,MATRIX,Path(__file__).resolve()):pins[str(path)]=sha(path)
    def verify():
        for name,digest in pins.items():
            if not Path(name).is_file() or sha(Path(name))!=digest:raise ValueError('Pinned input changed: '+name)
    verify();source=(base/'S006SourceCandidateControl.cs').read_text();anchor=' static void Main(string[] a)';
    if source.count(anchor)!=1:raise ValueError('Exact source candidate entry required')
    method=METHOD
    invocation='TestPads(candidateLayout,setup,combined.CampaignMap,JObject.Parse(File.ReadAllText(a[5])))'
    if options.declared_context and not options.compile_bound:raise ValueError('Declared-context experiment requires bound compiler')
    if options.declared_context:
        init=' var anchors=layout.Anchors.ToList();var pads=layout.Pads.ToList();var bindings=new List<SkirmishStructureInstancePadBinding>();'
        if method.count(init)!=1:raise ValueError('Exact initial pad data required')
        method=method.replace(init,init+'var removedPadIds=new List<string>();',1)
        before=' var af=typeof(SkirmishMapLayoutConfig).GetField("anchors",BindingFlags.Instance|BindingFlags.NonPublic);'
        if method.count(before)!=1:raise ValueError('Exact pad declaration insertion required')
        declaration=''' var instancePads=pads.Where(p=>p.PadId.StartsWith("s006.fixture.",StringComparison.Ordinal)).ToArray();
 removedPadIds.AddRange(pads.Where(p=>!p.PadId.StartsWith("s006.fixture.",StringComparison.Ordinal)&&instancePads.Any(q=>q.FactionId==p.FactionId&&q.Kind==p.Kind)).Select(p=>p.PadId));
 pads.RemoveAll(p=>removedPadIds.Contains(p.PadId));
 var declarationField=typeof(SkirmishMapLayoutConfig).GetField("declaredConfigurations",BindingFlags.Instance|BindingFlags.NonPublic);if(declarationField==null)throw new Exception("Exact declared configuration field required");
 declarationField.SetValue(layout,new[]{new SkirmishDeclaredLayoutConfiguration{SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,PlayerDeployment=frames.PlayerDeployment,EnemyDeployment=frames.EnemyDeployment}});
 frames.HasMeasuredDeployments=false;frames.BindAllConfigurations=false;layout.ApplyWorldBinding(frames);
'''
        method=method.replace(before,declaration+before,1)
    if options.compile_bound:
        signature='SkirmishMapLayoutConfig original,SkirmishResolvedSetup setup,OperationMapDefinition map,JObject input)'
        if method.count(signature)!=1:raise ValueError('Exact pad method signature required')
        method=method.replace(signature,'SkirmishMapLayoutConfig original,SkirmishResolvedSetup setup,OperationMapDefinition map,JObject input,SkirmishScenarioDefinitionConfig sourceDefinition,SkirmishContentManifest manifest,IReadOnlyList<SkirmishSetupMatrixRow> matrix)',1)
        injection=''' var bound=Activator.CreateInstance<SkirmishScenarioDefinitionConfig>();
 bound.Configure("S006",sourceDefinition.ObjectiveConfig,sourceDefinition.ArmyProfileConfig,sourceDefinition.StartPackageConfig,sourceDefinition.EconomyConfig,sourceDefinition.UpgradeCatalog,sourceDefinition.IntelConfig,sourceDefinition.RoleCatalog,layout,featureIds:sourceDefinition.RequiredFeatureIds);bound.BindCampaignMap(map);
 bool compiled=SkirmishSetupCompiler.TryCompile(bound,setup.DifficultyId,setup.SizeId,setup.Seed,manifest,matrix,out var boundSetup,out var compileReasons);var boundReasons=new JArray();foreach(var reason in compileReasons)boundReasons.Add(reason.ToString());
 var boundEvidence=new JObject{["compiled"]=compiled,["setup_hash"]=boundSetup==null?(JToken)JValue.CreateNull():new JValue(boundSetup.SetupHash),["setup"]=boundSetup==null?(JToken)JValue.CreateNull():JObject.FromObject(boundSetup),["structure_count"]=boundSetup==null?-1:boundSetup.Structures.Length,["force_count"]=boundSetup==null?-1:boundSetup.Forces.Length,["reasons"]=boundReasons,["candidate_only"]=true};
 var additions=new JObject{["anchors"]=JArray.FromObject(anchors.Skip(original.Anchors.Length).ToArray()),["pads"]=JArray.FromObject(pads.Where(p=>p.PadId.StartsWith("s006.fixture.",StringComparison.Ordinal)).ToArray()),["instance_bindings"]=JArray.FromObject(bindings),["scope"]="Disposable data-only additions; no authored asset or runtime admission"};
'''
        before=' layout.BindStructureInstancePads(bindings.Take(bindings.Count-1).ToArray());'
        if method.count(before)!=1 or method.count('["binding_applied"]=applied')!=1:raise ValueError('Exact bound compile insertion required')
        if options.declared_context:
            injection=injection.replace('["scope"]="Disposable data-only additions; no authored asset or runtime admission"',
                '["removed_pad_ids"]=JArray.FromObject(removedPadIds),["declared_size"]=setup.SizeId.ToString(),["declared_difficulty"]=setup.DifficultyId.ToString(),["player_deployment"]=new JArray(frames.PlayerDeployment.x,frames.PlayerDeployment.y),["enemy_deployment"]=new JArray(frames.EnemyDeployment.x,frames.EnemyDeployment.y),["measured_deployments"]=frames.HasMeasuredDeployments,["bind_all_configurations"]=frames.BindAllConfigurations,["scope"]="Disposable single-configuration data delta; no authored asset or runtime admission"',1)
        method=method.replace(before,injection+before,1).replace('["binding_applied"]=applied','["bound_compiler"]=boundEvidence,["candidate_additions"]=additions,["binding_applied"]=applied',1)
        invocation='TestPads(candidateLayout,setup,combined.CampaignMap,JObject.Parse(File.ReadAllText(a[5])),candidate,manifest,rows)'
    source=source.replace(anchor,' '+method+anchor,1);seam='    results.Add(new JObject{["size"]=size.ToString()'
    if source.count(seam)!=1:raise ValueError('Exact result projection required')
    source=source.replace(seam,'    var padProjection=accepted?'+invocation+':null;\n'+seam,1)
    source=source.replace('["source_pad_resolution"]=pads,["reasons"]=failures','["source_pad_resolution"]=pads,["disposable_pad_projection"]=padProjection,["reasons"]=failures',1)
    control=out/'S006TypedPadCandidateControl.cs';control.write_text(source)
    args=[json.loads(line) for line in (base/'compile.rsp').read_text().splitlines() if line.strip()];old=str(base/'S006SourceCandidateControl.cs')
    if args.count(old)!=1:raise ValueError('Exact original source fixture required')
    args=[str(control) if x==old else '-out:'+str(out/'control.exe') if x.startswith('-out:') else x for x in args]
    rsp=out/'compile.rsp';rsp.write_text(''.join(json.dumps(x)+'\n' for x in args));csc=next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    built=subprocess.run(['dotnet',str(csc),'@'+str(rsp)],capture_output=True,text=True,timeout=300);(out/'compile.log').write_text(built.stdout+built.stderr)
    if built.returncode:raise RuntimeError('Pad candidate failed to compile')
    runtime=json.loads(BUILD.read_bytes())['runtime_selection'];env=os.environ.copy();env['MONO_PATH']=os.pathsep.join((str(BUILD.parent/'placement'),runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(out)))
    run=subprocess.run([runtime['mono'],str(out/'control.exe'),str(GRAPH),str(MATRIX),str(out/'result.json'),str(out/'layout.json'),'1',str(proposal_path)],cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(out/'run.log').write_text(run.stdout+run.stderr)
    marker='[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0'
    if run.returncode or run.stdout.splitlines().count(marker)!=1:raise RuntimeError('Pad candidate execution failed')
    verify();result=json.loads((out/'result.json').read_bytes());summary=[]
    for row in result['rows']:
        value=row.get('disposable_pad_projection') or {};checks=value.get('checks',[])
        bound=value.get('bound_compiler') or {};bound_setup=bound.get('setup');baseline=row.get('setup');mismatches=[]
        if isinstance(bound_setup,dict) and isinstance(baseline,dict):
            ignored={'SetupHash','LayoutBindingFingerprint','PlayerBaseWorldX','PlayerBaseWorldZ','EnemyBaseWorldX','EnemyBaseWorldZ','PlayerStagingWorldX','PlayerStagingWorldZ','EnemyStagingWorldX','EnemyStagingWorldZ','PlayerSpawnPadX','PlayerSpawnPadZ','PlayerRallyPadX','PlayerRallyPadZ','EnemySpawnPadX','EnemySpawnPadZ','EnemyRallyPadX','EnemyRallyPadZ'}
            for key,prior in baseline.items():
                if key not in ignored and isinstance(prior,(str,int,float,bool)) and bound_setup.get(key)!=prior:mismatches.append(key)
            for section in ('Forces','Structures'):
                def stable(entries):return [{k:v for k,v in entry.items() if k not in ('SpawnAnchorId','SpawnWorldX','SpawnWorldZ','AcrossOffsetMetres','ForwardOffsetMetres')} for entry in entries]
                if stable(baseline[section])!=stable(bound_setup[section]):mismatches.append(section+'IdentityOrQuantity')
        concise={k:v for k,v in bound.items() if k!='setup'};concise['fixed_data_mismatches']=mismatches if isinstance(bound_setup,dict) else None
        summary.append({'size':row['size'],'binding_applied':value.get('binding_applied'),'missing_binding_rejected':value.get('missing_binding_rejected'),'contained':sum(c.get('footprint_contained') is True for c in checks),'count':len(checks),'reasons':value.get('binding_reasons'),'bound_compiler':concise})
    (out/'receipt.json').write_text(json.dumps({'status':'CollectedDisposablePadCandidate','result_sha256':sha(out/'result.json'),'source_pins':pins,'summary':summary,'mission_ready':False},indent=2)+'\n');print(json.dumps({'status':'CollectedDisposablePadCandidate','sizes':summary,'mission_ready':False}))


if __name__=='__main__':main()
