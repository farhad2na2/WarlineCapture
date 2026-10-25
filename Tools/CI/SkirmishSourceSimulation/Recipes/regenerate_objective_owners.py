"""Finite CURRENT native objective-owner -> existing same-world Step binding.
No stored terminal/reducer policy bodies. Required components/order remain admitted
by the existing same-world scope/compiled current-header scheduler profile.
"""
from pathlib import Path
import argparse,hashlib,json,re
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
OWNERS={'SkirmishBaseAssaultObjectiveSystem':{'required':['SkirmishExpandedSessionComponent','SkirmishObjectiveStateComponent'],'queries':2},'SkirmishObjectiveFactProjectionSystem':{'required':['SkirmishExpandedSessionComponent'],'queries':4},'SkirmishOutcomeSystem':{'required':['SkirmishExpandedSessionComponent'],'queries':1}}
def once(text,old,new):
 if text.count(old)!=1:raise ValueError('Unique native objective binding anchor required: '+old)
 return text.replace(old,new,1)
def adapt(name,text):
 cfg=OWNERS[name];requirements=re.findall(r'^            state\.RequireForUpdate<([^>]+)>\(\);$',text,re.M)
 if requirements!=cfg['required']:raise ValueError('Native required-update domain changed: '+name)
 text=re.sub(r'^    \[Update(?:InGroup|Before|After)\([^\n]*\]\n','',text,flags=re.M)
 text=once(text,'public partial struct '+name+' : ISystem','public partial struct '+name)
 text=once(text,'public void OnCreate(ref SystemState state)','public '+name+'(EntityManager em)')
 for required in requirements:text=once(text,'            state.RequireForUpdate<'+required+'>();\n','')
 if text.count('state.GetEntityQuery(')!=cfg['queries']:raise ValueError('Native private query acquisition changed: '+name)
 text=text.replace('state.GetEntityQuery(','em.CreateEntityQuery(')
 text=once(text,'public void OnUpdate(ref SystemState state)','internal void Step(EntityManager em)')
 text=once(text,'            EntityManager em = state.EntityManager;\n','')
 if name=='SkirmishBaseAssaultObjectiveSystem':text=once(text,'SystemAPI.TryGetSingleton(out SkirmishMatchState match)','AutonomousOwnerSingleton.Try(em,out SkirmishMatchState match)')
 elif name=='SkirmishObjectiveFactProjectionSystem':
  text=once(text,'SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay)','AutonomousOwnerSingleton.Try(em,out RuntimeGameplayStateComponent gameplay)')
  text=once(text,'SystemAPI.Time.DeltaTime','em.WorldUnmanaged.Time.DeltaTime')
 else:
  text=once(text,'SystemAPI.TryGetSingleton<RuntimeGameplayStateComponent>(out var gameplay)','AutonomousOwnerSingleton.Try<RuntimeGameplayStateComponent>(em,out var gameplay)')
  text=once(text,'SystemAPI.GetSingletonEntity<RuntimeGameplayStateComponent>()','AutonomousOwnerSingleton.Entity<RuntimeGameplayStateComponent>(em)')
 if any('SystemAPI.'in line and not line.lstrip().startswith('//')for line in text.splitlines()):raise ValueError('Unbound new native objective system access: '+name)
 return text

def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);paths={name:repo/'Assets/Game/Scripts/Runtime/Skirmish'/(name+'.cs')for name in OWNERS};raw={name:path.read_bytes()for name,path in paths.items()};files={name+'.cs':adapt(name,data.decode('utf-8').replace('\r\n','\n'))for name,data in raw.items()}
 if any(paths[n].read_bytes()!=data for n,data in raw.items()):raise ValueError('Native objective source drift during extraction')
 for filename,text in files.items():write_lf(output/filename,text)
 proof={'kind':'CurrentNativeObjectiveOwnerStepBinding','source_inputs':{str(paths[name]):hashlib.sha256(data).hexdigest()for name,data in raw.items()},'outputs':{name:hashlib.sha256(text.encode()).hexdigest()for name,text in files.items()},'requirements':OWNERS,'bindings':['Current metadata ordering projected separately by finite current-header admission; only system-declaration Update attributes removed here','OnCreate -> same-world constructor, original private query definitions preserved','Known RequireForUpdate domains asserted by genuine session scope; changed/new required domains reject','OnUpdate -> Step(em), SystemAPI singleton/time acquisitions bind to existing strict same-world adapters'],'policy_body_rewrites':False};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if any(paths[n].read_bytes()!=data for n,data in raw.items()):raise ValueError('Native objective source drift during output')
 return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
