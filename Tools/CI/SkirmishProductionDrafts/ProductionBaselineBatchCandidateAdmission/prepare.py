"""Explicit successor: restore existing S005 QA exposure; author real run plans only."""
import copy,csv,hashlib,importlib.util,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
PARENT=ROOT/'Tools/CI/SkirmishProductionDrafts/ProductionBaselineBatch'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,value):
    with p.open('x',encoding='utf-8',newline='\n') as stream: json.dump(value,stream,indent=2);stream.write('\n')
assert sha(PARENT/'manifest.json')=='193cd3d9488f9c67a10fd34303df52432bd4f5117479e899fef66ed81ac47d62'
parent=json.loads((PARENT/'manifest.json').read_text()); m=copy.deepcopy(parent)
target='Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.MenuProbe.cs'
row=next(r for r in m['files'] if r['target']==target)
old=(ROOT/row['source']).read_bytes();a=b'if (selectedCatalog == "S005" && productionCanary == null)';b=b'if (selectedCatalog == "S005")'
assert old.count(a)==1
new=old.replace(a,b);source=HERE/'SkirmishS002AriaRunHarness.MenuProbe.cs.txt';source.write_bytes(new)
row['composed_parent_source']={'path':row['source'],'sha256':row['sha256']}
row.update(source=source.relative_to(ROOT).as_posix(),sha256=sha(source))
m['successor_parent']={'path':(PARENT/'manifest.json').relative_to(ROOT).as_posix(),'sha256':sha(PARENT/'manifest.json')}
m['delta']='Remove only productionCanary==null from original S005 isolated Editor-memory QA exposure. No generic catalog override, publication edit, entitlement bypass, or normal launch/compiler bypass.'
write(HERE/'manifest.json',m)
spec=importlib.util.spec_from_file_location('matrix_authority',PARENT/'skirmish_production_matrix_authority.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
baseline=module.build_baseline(ROOT);write(HERE/'baseline720.plan.json',baseline)
catalog=list(csv.DictReader((ROOT/module.CATALOG).open(encoding='utf-8-sig',newline='')))
groups={}
for r in catalog: groups.setdefault((r['map_id'],r['objective_id'],r['army_profile']),{})[r['start_profile']]=r['scenario_id']
stress={k:copy.deepcopy(v) for k,v in baseline.items() if k!='slots'};stress.update(kind='Stress1200',slots=[])
derivations=[]
for group_index,(key,starts) in enumerate(sorted(groups.items())):
    used=set()
    for i in range(20):
        counter=0
        while True:
            label='warline-stress-v1|'+ '|'.join(key)+'|'+str(i)+'|'+str(counter)
            seed=1+int.from_bytes(hashlib.sha256(label.encode('ascii')).digest()[:4],'big')%2147483647
            if seed not in used: break
            counter+=1
        used.add(seed);start='F' if i%2==0 else 'E'
        stress['slots'].append(dict(catalog_id=starts[start],seed=seed,locale='EN' if (i+group_index)%2==0 else 'FA',size='Standard',difficulty='Regular',control_mode='aria'))
        derivations.append(dict(stratum=list(key),index=i,label=label,seed=seed,start=start))
write(HERE/'stress1200.plan.json',stress)
for name,count in [('baseline720',720),('stress1200',1200)]:
    p=HERE/(name+'.plan.json');actual=module.SamplingAuthority(ROOT,p,sha(p));assert len(actual.slots)==count
governing=['Design/Roadmap/Skirmish_Expansion/'+name for name in ['AI_AND_ARIA.md','SCALABLE_MISSION_PRODUCTION_HANDOFF.md','SCALABLE_MISSION_PRODUCTION_STRATEGY.md']]
write(HERE/'PLAN_AUTHORITY.json',{'governing':[dict(path=p,sha256=sha(ROOT/p)) for p in governing],
 'reason':'The normative handoff specifies20 distinct stress seeds per map/objective/army with alternating starts, but supplies no fixed20-seed list. Root authorized routine deterministic legitimate seed selection; these are planned real gameplay inputs, not measured results.',
 'algorithm':'For sorted stratum/map/objective/army and index0..19: SHA256(ASCII warline-stress-v1|MAP|OBJECTIVE|ARMY|INDEX|COLLISION_COUNTER), first4 bytes big-endian, modulo2147483647 plus1. Counter starts0, increments only for duplicate within stratum.',
 'locale':'Alternate EN/FA within stratum, offset by sorted stratum index to avoid locking all Field starts to one locale. Locale/device acceptance remains separate.',
 'baseline':{'path':(HERE/'baseline720.plan.json').relative_to(ROOT).as_posix(),'sha256':sha(HERE/'baseline720.plan.json'),'slots':720},
 'stress':{'path':(HERE/'stress1200.plan.json').relative_to(ROOT).as_posix(),'sha256':sha(HERE/'stress1200.plan.json'),'strata':60,'slots':1200,'distinct_seeds_per_stratum':20},
 'seed_derivations':derivations,'native_runs':0,'qualifications':0,'publication_changed':False})
print(json.dumps({'manifest':sha(HERE/'manifest.json'),'baseline':sha(HERE/'baseline720.plan.json'),'stress':sha(HERE/'stress1200.plan.json'),'authority':sha(HERE/'PLAN_AUTHORITY.json')}))
