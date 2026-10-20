import re
"""Read-only optional compile connector; operational outcomes and five gates stay intact."""
from skirmish_production_operational_inspector import inspect as inspect_operational,Reader,need,digest
from skirmish_production_queue import fingerprint
from skirmish_native_compile_admission import admit

def merge_proof_pins(read,refs):
 need(isinstance(refs,list)and refs,"Complete earlier proof pins required")
 for ref in refs:
  need(isinstance(ref,dict)and set(ref)=={"path","sha256"},"Exact earlier proof pin")
  need(isinstance(ref["sha256"],str)and re.fullmatch("[a-f0-9]{64}",ref["sha256"]),"Exact earlier proof digest")
  p=read.path(ref["path"])
  need(p not in read.pins or read.pins[p]==ref["sha256"],"Conflicting earlier proof pin")
  # read verifies literal hash and namespace now; final stable rechecks the full union.
  need(digest(p)==ref["sha256"],"Earlier proof bytes changed")
  read.pins[p]=ref["sha256"]

def inspect(project,spec):
 need(isinstance(spec,dict)and set(spec)=={'schema_version','operational','compile_proof'}and type(spec['schema_version'])is int and spec['schema_version']==2,'Exact connector spec')
 result=inspect_operational(project,spec['operational'])
 if result['result']!='NormalCompletedUnqualified':return result
 read=Reader(project);receipts=[];errors=[]
 try:
  merge_proof_pins(read,result.get('proof_pins'))
  items=spec['operational']['attempts'];need(len(items)==len(result['attempts'])and items,'Exact actual operational attempt set')
  for item,actual in zip(items,result['attempts']):
   _,job=read.obj(item['job'])
   need(fingerprint(job)==actual['job_sha256'],'Compile subject is actual inspected job')
   receipt=admit(project,job,spec['compile_proof']);receipts.append(receipt)
   need(receipt.get('admission')=='compile'and receipt.get('result')=='MeasuredNativeEditorCompileForExactCandidate'and receipt.get('job_sha256')==actual['job_sha256']and receipt.get('candidate_sha256')==job['candidate_sha256']and receipt.get('build_sha256')==job['build_sha256']and isinstance(actual.get('native_config_sha256'),str)and re.fullmatch('[a-f0-9]{64}',actual['native_config_sha256'])and receipt.get('native_config_sha256')==actual['native_config_sha256']and receipt.get('qualified')is False and receipt.get('queue_success_enabled')is False,'Exact per-job measured compile receipt')
   merge_proof_pins(read,receipt.get('proof_pins'))
  read.stable()
  result["proof_pins"]=[dict(path=str(p),sha256=h)for p,h in read.pins.items()]
  result['admission'].update(missing=[x for x in result['admission']['missing']if x!='compile'],compile_receipts=receipts)
 except (ValueError,OSError,KeyError,TypeError,IndexError,AttributeError)as e:
  errors.append(str(e));result['admission']['compile_errors']=errors;result['admission']['compile_receipts']=receipts
 # No completed operational loss/draw becomes a win and no missing gate is inferred.
 result.update(qualified=False,queue_success_enabled=False,mission_ready=False,publication_changed=False)
 return result
