"""Read a SHA-pinned complete finite baseline/stress plan; never execute or qualify."""
import csv
import hashlib
import json
from pathlib import Path
import re

CATALOG='Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv'
RULES='Design/Roadmap/Skirmish_Expansion/AI_AND_ARIA.md'
FIELDS={'catalog_id','seed','locale','size','difficulty','control_mode'}
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def strict(path):
    def pairs(rows):
        result={}
        for k,v in rows:
            if k in result:raise ValueError('Duplicate JSON key: '+k)
            result[k]=v
        return result
    return json.loads(Path(path).read_text(encoding='utf-8-sig'),object_pairs_hook=pairs)
def need(ok,error):
    if not ok:raise ValueError(error)
def slot_key(row):return row['catalog_id'],row['seed'],row['locale']
def build_baseline(project):
    """Exact handoff baseline only; never invent a stress seed plan."""
    project=Path(project).resolve()
    with (project/CATALOG).open(encoding='utf-8-sig',newline='') as stream:rows=list(csv.DictReader(stream))
    paths={CATALOG,RULES}|{SamplingAuthority.packet(r) for r in rows};slots=[]
    for row in rows:
        text=(project/SamplingAuthority.packet(row)).read_text(encoding='utf-8')
        section=re.search(r'^## '+row['scenario_id']+r'\s*$([\s\S]*?)(?=^## |\Z)',text,re.M);need(section,'Packet section missing')
        match=re.search(r'Regular seeds ((?:`\d+`[ ,and]*)+) in both EN and FA at Standard',section[1]);need(match,'Three exact baseline seed authority required')
        seeds=[int(v)for v in re.findall(r'`(\d+)`',match[1])];need(len(seeds)==len(set(seeds))==3,'Three distinct baseline seeds required')
        for seed in seeds:
            for locale in ('EN','FA'):slots.append(dict(catalog_id=row['scenario_id'],seed=seed,locale=locale,size='Standard',difficulty='Regular',control_mode='aria'))
    return dict(schema_version=1,kind='Baseline720',source_pins=[dict(path=p,sha256=sha(project/p)) for p in sorted(paths)],slots=slots)
class SamplingAuthority:
    def __init__(self,project,path,expected):
        self.project=Path(project).resolve();self.path=Path(path).resolve();self.expected=expected
        need(re.fullmatch('[a-f0-9]{64}',expected or '') and sha(self.path)==expected,'Sampling plan hash mismatch')
        plan=strict(self.path);need(set(plan)=={'schema_version','kind','source_pins','slots'} and type(plan['schema_version']) is int and plan['schema_version']==1,'Unknown sampling schema')
        self.kind=plan['kind'];need(self.kind in ('Baseline720','Stress1200'),'Unknown sampling kind')
        with (self.project/CATALOG).open(encoding='utf-8-sig',newline='') as stream:catalog=list(csv.DictReader(stream))
        self.catalog={r['scenario_id']:r for r in catalog};need(len(catalog)==120 and set(self.catalog)=={'S'+str(i).zfill(3)for i in range(1,121)},'Exact S001–S120 catalog required')
        tuples={(r['map_id'],r['objective_id'],r['army_profile'],r['start_profile'])for r in catalog}
        need(tuples=={(m,o,a,s)for m in ('DB','CC','MP','IB','AP')for o in ('BA','FC','BT','CE')for a in ('G','A','C')for s in ('F','E')},'Exact5maps/4objectives/3armies/2starts required')
        self.pins={}
        for row in plan['source_pins']:
            need(set(row)=={'path','sha256'} and row['path'] not in self.pins and re.fullmatch('[a-f0-9]{64}',row['sha256'] or ''),'Invalid source pin')
            p=Path(row['path']);need(not p.is_absolute() and '..' not in p.parts,'Unsafe source pin');self.pins[row['path']]=row['sha256']
        expected_pins={CATALOG,RULES}|{self.packet(r) for r in catalog};need(set(self.pins)==expected_pins,'Exact catalog/rules/twenty packets required');self.recheck()
        self.slots={}
        for row in plan['slots']:
            need(set(row)==FIELDS and row['catalog_id'] in self.catalog and type(row['seed']) is int and 1<=row['seed']<=2147483647,'Malformed canonical sampling slot')
            need(row['locale'] in ('EN','FA') and row['size']=='Standard' and row['difficulty']=='Regular' and row['control_mode']=='aria','Only normal ARIA Regular Standard slots admitted')
            key=slot_key(row);need(key not in self.slots,'Duplicate sampling slot');self.slots[key]=row
        if self.kind=='Baseline720':
            expected_slots=set()
            for ident,row in self.catalog.items():
                text=(self.project/self.packet(row)).read_text(encoding='utf-8')
                section=re.search(r'^## '+ident+r'\s*$([\s\S]*?)(?=^## |\Z)',text,re.M);need(section,'Packet catalog section absent')
                match=re.search(r'Regular seeds ((?:`\d+`[ ,and]*)+) in both EN and FA at Standard',section[1]);need(match,'Baseline seed authority absent')
                seeds=[int(v) for v in re.findall(r'`(\d+)`',match[1])];need(len(seeds)==len(set(seeds))==3,'Three exact baseline seeds required')
                expected_slots|={(ident,seed,locale) for seed in seeds for locale in ('EN','FA')}
            need(len(self.slots)==720 and set(self.slots)==expected_slots,'All exact720 baseline slots required')
        else:
            groups={}
            for row in plan['slots']:
                c=self.catalog[row['catalog_id']];groups.setdefault((c['map_id'],c['objective_id'],c['army_profile']),[]).append(row)
            need(len(self.slots)==1200 and len(groups)==60,'All1200 slots/60 stress strata required')
            for rows in groups.values():
                need(len(rows)==20 and len({r['seed'] for r in rows})==20,'Twenty distinct stress seeds per stratum required')
                need(all(self.catalog[r['catalog_id']]['start_profile']==('F' if i%2==0 else 'E') for i,r in enumerate(rows)),'Actual Field/Established starts must alternate')
    @staticmethod
    def packet(row):return 'Design/Roadmap/Skirmish_Expansion/Scenarios/'+row['map_id']+'_'+row['objective_id']+'.md'
    def recheck(self):
        need(sha(self.path)==self.expected,'Sampling plan changed')
        for path,value in self.pins.items():need(sha(self.project/path)==value,'Sampling source changed: '+path)
    def validate_job(self,job):
        self.recheck();need(self.slots.get(slot_key(job))=={k:job[k] for k in FIELDS},'Job differs from explicit reviewed sampling slot')
        packet=self.packet(self.catalog[job['catalog_id']]);need(job['packet_path']==packet and job['packet_sha256']==self.pins[packet],'Job packet does not join canonical family')
    def validate_batch(self,jobs):
        need(1<=len(jobs)<=1200,'Finite nonempty batch required')
        for job in jobs:self.validate_job(job)
        keys=[slot_key(job)for job in jobs];need(len(keys)==len(set(keys)),'Duplicate sampling slot in batch')
        if self.kind=='Stress1200':
            order={key:i for i,key in enumerate(self.slots)}
            need(all(order[b]==order[a]+1 for a,b in zip(keys,keys[1:])),'Stress dispatch must be a contiguous ordered plan slice, preserving alternating starts')

if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--project',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    output=args.output.resolve();project=args.project.resolve();need(not output.is_relative_to(project/'Assets'),'Do not write imported Assets')
    with output.open('x',encoding='utf-8') as stream:json.dump(build_baseline(project),stream,indent=2);stream.write('\n')
    authority=SamplingAuthority(project,output,sha(output));print(json.dumps(dict(result='PreparedUnqualified',kind=authority.kind,slots=len(authority.slots),path=str(output),sha256=sha(output),native_runs=0,queue_success_enabled=False)))
