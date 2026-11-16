#!/usr/bin/env python3
"""Inventory authored Skirmish catalog rows without inferring mission readiness."""
import argparse,collections,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
SOURCE=ROOT/'Assets/Game/Resources/SkirmishBattleCatalog.asset'
FIELDS=('MapId','ObjectiveId','ArmyProfile','StartProfile','Status','PlayableScenarioIndex','DefinitionId')
def digest(data):return hashlib.sha256(data).hexdigest()
def parse(raw):
 text=raw.decode('utf-8'); starts=list(re.finditer(r'^  - ScenarioId: (S\d{3})\s*$',text,re.M))
 rows=[]
 for i,m in enumerate(starts):
  segment=text[m.end():starts[i+1].start() if i+1<len(starts) else len(text)];row={'ScenarioId':m.group(1)}
  for field in FIELDS:
   matches=re.findall(r'^    '+field+r':[ \t]*([^\r\n]*)$',segment,re.M)
   if len(matches)!=1:raise ValueError('Exactly one '+field+' required for '+m.group(1))
   row[field]=matches[0].strip()
  rows.append(row)
 ids=[r['ScenarioId'] for r in rows];expected=['S%03d'%i for i in range(1,121)]
 if ids!=expected:raise ValueError('Exact ordered S001-S120 catalog required')
 for r in rows:
  r['Status']=int(r['Status']);r['PlayableScenarioIndex']=int(r['PlayableScenarioIndex'])
 return rows

def main():
 p=argparse.ArgumentParser();p.add_argument('output',type=Path);args=p.parse_args();before=SOURCE.read_bytes();rows=parse(before)
 # Negative controls do not alter the authoritative file.
 text=before.decode('utf-8');controls={}
 for name,mutated in [('missing_id',text.replace('  - ScenarioId: S120','  - ScenarioId: S121',1)),('duplicate_field',text.replace('    ObjectiveId: BA','    ObjectiveId: BA\n    ObjectiveId: BA',1))]:
  try:parse(mutated.encode('utf-8'));controls[name]=False
  except ValueError:controls[name]=True
 controls['empty_definition_preserved']=rows[0]['DefinitionId']==''
 if not all(controls.values()) or SOURCE.read_bytes()!=before:raise ValueError('Negative control failed or catalog changed')
 def counts(keys):
  c=collections.Counter(tuple(r[k] for k in keys) for r in rows)
  return [{**dict(zip(keys,k)),'count':v} for k,v in sorted(c.items())]
 result={'status':'CollectedAuthoredCatalogOnly','source_path':str(SOURCE),'source_sha256':digest(before),'rows':len(rows),'counts_by_map_objective_army_start':counts(('MapId','ObjectiveId','ArmyProfile','StartProfile')),'counts_by_status':counts(('Status',)),'coded_definitions':[{'id':r['ScenarioId'],'definition':r['DefinitionId'],'status':r['Status'],'playable_index':r['PlayableScenarioIndex']} for r in rows if r['DefinitionId']],'negative_controls_passed':controls,'scope':'Static serialized catalog inventory; no source-fit, compiled configuration, gameplay, victories or readiness asserted','mission_ready':False}
 args.output.parent.mkdir(parents=True,exist_ok=True)
 with args.output.open('x') as f:json.dump(result,f,indent=2);f.write('\n')
 print(json.dumps({'status':result['status'],'rows':len(rows),'family_combinations':len(result['counts_by_map_objective_army_start']),'status_counts':result['counts_by_status']}))
if __name__=='__main__':main()
