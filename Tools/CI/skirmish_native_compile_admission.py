"""Schema3 evidence-driven compile facet. No queue, publication or qualification effects."""
from pathlib import Path
import hashlib,json,ntpath,re
from skirmish_production_operational_inspector import Reader,need,parse
from skirmish_production_queue import fingerprint
from skirmish_native_identity import verify_job,normalize_native_locale
from compile_evidence_common import snapshot,rsp,terminal,code_rows
from composite_compile_evidence import validate as composite
from native_compile_closure import validate_values as closure_values,validate_source_hashes,validate_output_artifact_refs,validate_capture_origin,declared,key,CS
REFS={"focus_owner","focus_grant","focus_shutdown","focus_settings","focus_watch","focus_result","focus_before","focus_after","focus_log","focus_rsp","focus_outputs","focus_review","focus_pins_before","focus_pins_after","focus_markers","census_result","census_before","census_after","census_log","census_rsp","census_output","offline_result","offline_scope","offline_pins_before","offline_pins_after","offline_sources","offline_membership","owner_graph","compiler_lineage","stable_projection","stable_commit","native_closure"}
def read_json(read,ref):
 _,text=read.read(ref);return parse(text)
def indexed(read,values):
 result={}
 for path,h in values.items():
  k=key(read.root,path);need(k not in result or result[k]==h,"Conflicting compiler input aliases");result[k]=h
 return result
def exact_refs(read,proof,prefix,names):
 result=read.path(proof[prefix+"_result"]["path"]);need(result.name=="verified-result.json"if prefix!="offline"else result.name=="result.json","Actual result filename")
 for suffix,name in names.items():need(read.path(proof[prefix+"_"+suffix]["path"])==result.parent/name,"Exact "+prefix+" sibling")
 return result.parent

def admit(project,job,proof):
 answer=dict(schema_version=1,admission="compile",result="MissingOrFailedAdmission",qualified=False,queue_success_enabled=False,mission_ready=False,android_player_build_proved=False,device_accepted=False,errors=[])
 try:
  need(isinstance(proof,dict)and set(proof)=={"schema_version",*REFS}and type(proof["schema_version"])is int and proof["schema_version"]==3,"Exact schema3 evidence refs, no caller truth flags")
  need(all(isinstance(proof[x],dict)for x in REFS),"Unbound evidence remains Pending; no backfilled native closure")
  read=Reader(project);data={}
  for name in REFS:
   _,text=read.read(proof[name]);data[name]=text if name.endswith(("_log","_watch"))else parse(text)
  focus=exact_refs(read,proof,"focus",dict(before="source-before.json",after="source-after.json",log="unity.log",rsp="generated-rsp-after.json",outputs="build-diff.json",owner="memory-owner.json",grant="completed-memory-grant.json",shutdown="post-wrapper-owned-cim-shutdown.json",settings="import-worker-scope.json",watch="memory-watch.jsonl",pins_before="pins-before.json",pins_after="pins-after.json"))
  exact_refs(read,proof,"census",dict(before="source-before.json",after="source-after.json",log="unity.log",rsp="generated-rsp-after.json",output="native-compiled-setups.json"))
  offpath=read.path(proof["offline_result"]["path"]);need(offpath.name=="result.json","Actual owning result")
  for suffix,name in dict(scope="scope.json",pins_before="pins-before.json",pins_after="pins-after.json",sources="source-manifest.json",membership="membership-proof.json").items():need(read.path(proof["offline_"+suffix]["path"])==offpath.parent/name,"Exact offline sibling")
  fb=snapshot(data["focus_before"]);fa=snapshot(data["focus_after"]);cb=snapshot(data["census_before"]);ca=snapshot(data["census_after"])
  need(fb["source_manifest"]==fa["source_manifest"]and cb==ca==fa,"Frozen native source and unchanged census")
  need(job["source_manifest"]==fa["source_manifest"]and job["candidate_sha256"]==fa["candidate_sha256"]and job["build_sha256"]==fa["build_sha256"],"Whole exact candidate/build subject")
  terminal(data["focus_result"],"focus",data["focus_log"]);terminal(data["census_result"],"census",data["census_log"])
  need(data["focus_result"]["candidateIdentity"]==fa["candidate_sha256"]and data["focus_result"].get("nativeOwningCscCompleted")is True,"Actual native Csc/candidate")
  fpin=indexed(read,data["focus_pins_before"]);need(data["focus_pins_before"]==data["focus_pins_after"],"Native literal maps unchanged")
  for name in ["focus_review","focus_markers"]:need(fpin.get(key(read.root,proof[name]["path"]))==proof[name]["sha256"],"Native review/marker literal pin")
  grant=data['focus_grant'];owner=data['focus_owner'];shutdown=data['focus_shutdown'];settings=data['focus_settings']
  need(grant['status']=='Completed'and grant['id']==data['focus_review'].get('grantId',grant['id']),'Actual completed owning grant')
  watch=read.derived('Tools/CI/watch_owned_unity_memory.py');need(fpin.get(key(read.root,watch))==read.pins[watch],'Actual pinned ownership verifier')
  import runpy
  need(grant['ownerIdentity']==list(runpy.run_path(str(watch))['identity'](owner['process'])),'Actual owned Editor identity')
  armed=[parse(line)for line in data['focus_watch'].splitlines()if line.strip()];armed=[r for r in armed if r.get('kind')=='Armed']
  need(len(armed)==1 and armed[0]['reserveOverrideId']==grant['id']and(armed[0]['ramFloorGiB'],armed[0]['commitFloorGiB'])==(4,4),'Actual owning reserve floors')
  need(shutdown['status']=='VerifiedOwnedEditorAndProvenWorkersAbsent_OtherEditorsIdle'and shutdown['editorOrHubStopped']is False and settings['restored']is True and settings['byteIdentical']is True and settings['beforeSha256']==settings['afterSha256'],'Actual shutdown/settings')
  review=data["focus_review"];overlay=read_json(read,review["sourceProof"])
  need(isinstance(overlay,list)and overlay and len({r["target"]for r in overlay})==len(overlay),"Actual owning source delta")
  basepath=read.root/review["baselineDirectory"];base=snapshot(read_json(read,dict(path=str(basepath/"source-after.json"),sha256=review["baselineHashes"]["source-after.json"])))
  old={x["path"]:x["sha256"]for x in base["source_manifest"]};now={x["path"]:x["sha256"]for x in fb["source_manifest"]}
  oldcode={r['path']:r['sha256']for r in code_rows(base)};newcode={r['path']:r['sha256']for r in code_rows(fb)}
  need(oldcode.keys()<=newcode.keys()and all(oldcode.get(p)==h for p,h in newcode.items()if not p.endswith('.cs')),'Non-CSharp compiler metadata drift needs an explicitly reviewed expanded owning epoch')
  changes={p:h for p,h in now.items()if p.endswith(".cs")and old.get(p)!=h}
  need(changes=={r["target"]:r["afterSha256"]for r in overlay}and all(old.get(r["target"])==r["beforeSha256"]for r in overlay),"Native actual full CSharp delta; no invented freshness")
  need(fb["build_manifest"]==base["build_manifest"],"Actual native cached baseline outputs")
  required=sorted({r["owner"]for r in overlay});need(required==sorted(review["requiredOwners"]),"Required Csc owners derive source delta")
  need(set(required)<=set(CS.findall(data["focus_log"])),"Every direct owner actually Csc")
  facts=data["focus_result"]["nativeFocusedEvidence"];table=data["focus_markers"]["markers"];markers=facts["childMarkers"]
  need(facts.get("passed")is True and len(table)==len(markers)>0 and all(re.fullmatch(re.escape(r["text"])if r["mode"]=="literal"else r["pattern"],m)for r,m in zip(table,markers)),"Actual full reviewed marker contract")
  need(all(data["focus_log"].count(m)==1 for m in markers)and [data["focus_log"].index(m)for m in markers]==sorted(data["focus_log"].index(m)for m in markers),"Actual executed marker order")
  native_rsp=rsp(data["focus_rsp"]);need(native_rsp==rsp(data["census_rsp"]),"Native current RSP source/flags/ref lineage")
  validate_capture_origin(data["focus_result"],proof["native_closure"])
  need(read.path(proof["native_closure"]["path"])==focus/"native-compile-closure.json","Exact wrapper-produced closure descriptor sibling")
  declaration=data["native_closure"];need(set(declaration)=={"schema_version","before","after","rsps","sdk","output_artifacts"}and declaration["schema_version"]==1,"Exact native closure refs")
  pre=read_json(read,declaration["before"]);post=read_json(read,declaration["after"])
  need(read.path(declaration["before"]["path"])==focus/"native-compile-inputs-before.json"and read.path(declaration["after"]["path"])==focus/"native-compile-inputs-after.json","Genuine same native compile-window siblings required; no later backfill")
  sdk=declaration['sdk'];need(set(sdk)=={'dotnet','csc'},'Complete checked SDK set')
  editor=Path(owner['process']['ExecutablePath']).parent
  need(Path(sdk['dotnet']['path'])==editor/'Data/DotNetSdk/dotnet.exe'and Path(sdk['csc']['path']).is_relative_to(editor/'Data/DotNetSdk/sdk'),'Actual owned Editor SDK layout')
  for ref in sdk.values():need(re.fullmatch('[a-f0-9]{64}',ref['sha256'])and pre['inputs'].get(key(read.root,ref['path']))==post['inputs'].get(key(read.root,ref['path']))==ref['sha256'],'Recorded actual SDK bytes before/after')
  need(set(declaration["rsps"])==set(native_rsp)==set(pre["owners"])==set(post["owners"]),"Complete measured native owner set")
  native_deps={}
  for owner,ref in declaration["rsps"].items():
   _,text=read.read(ref);need(ref["sha256"]==native_rsp[owner]==post["owners"][owner]["rspSha256"],"Actual RSP bytes match native receipt")
   inputs,outputs,sources=declared(read.root,text);row=post["owners"][owner]
   need({key(read.root,p)for p in inputs}==set(row["inputs"])and {key(read.root,p)for p in outputs}==set(row["outputs"])and [key(read.root,p)for p in sources]==row["sources"],"No omitted source/reference/analyzer/additional declared input")
   native_deps[owner]={Path(p.replace(chr(92),"/")).name.removesuffix(".ref.dll").removesuffix(".dll")for p in inputs if p.endswith(".dll")}
  allowed=set(required)
  while True:
   expanded=allowed|{n for n,v in native_deps.items()if allowed.intersection(v)}
   if expanded==allowed:break
   allowed=expanded
  need(allowed==set(review["allowedOwners"]),"Exact actual native reverse closure")
  validate_source_hashes(read.root,pre,post,fb,fa)
  artifacts=declaration["output_artifacts"];validate_output_artifact_refs(read.root,post["outputs"],artifacts)
  seen=set()
  for output,ref in artifacts.items():
   need(set(ref)=={"path","sha256"}and ref["sha256"]==post["outputs"][output],"Actual output artifact hash equals recorded after output")
   p=read.path(ref["path"]);need(p not in seen and (p not in read.pins or read.pins[p]==ref["sha256"]),"Unique nonconflicting native output artifact")
   seen.add(p);read.derived(ref["path"]);need(read.pins[p]==ref["sha256"],"Actual output artifact bytes differ")
  nativeClosure=closure_values(pre,post,data["focus_log"],required,sorted(allowed))
  build={r["path"]:r["sha256"]for r in fa["build_manifest"]};delta=data["focus_outputs"]
  need(delta["after"]==build and set(delta["changed"])<=set(review["allowedNativeDllChanges"])and set(review["requiredNativeDllChanges"])<=set(delta["changed"]),"Actual allowed owning DLL outputs")
  off=data["offline_result"];scope=data["offline_scope"];projection=data["stable_projection"];commit=data["stable_commit"]
  need(off["result"]=="Passed"and off["mismatches"]==[]and off["head"]==off["headAfter"]==scope["head"]==projection["head"]==commit["parent"],"Actual clean compiler/projected HEAD lineage")
  need(len(off["owners"])==len({r["owner"]for r in off["owners"]})==24 and len(off["outputs"])==72 and all(type(r["exit"])is int and r["exit"]==0 and r.get("stopReason")is None for r in off["owners"]),"Actual complete owning terminals")
  need(data["offline_pins_before"]==data["offline_pins_after"],"Complete offline input map equality")
  inputs=indexed(read,data["offline_pins_before"])
  for name in ["offline_scope","offline_sources","offline_membership","owner_graph"]:need(inputs.get(key(read.root,proof[name]["path"]))==proof[name]["sha256"],"Owning metadata literal pin")
  owning=composite(read,proof,data)
  for path,h in off["outputs"].items():
   p=read.derived(path);need(read.pins[p]==h,"Actual output bytes")
  rows=projection["rows"];need(rows and len({r["path"]for r in rows})==len(rows),"Unique stable projection")
  need(commit["pushed"]is True and commit["commit"]==commit["remoteMain"]and commit["head"]==commit["parent"]and commit["prechecksPassed"]is True and commit["nativeCandidate"]==fa["candidate_sha256"]and commit["nativeBuild"]==fa["build_sha256"],"Actual pushed stable commit tied native envelope")
  committed={r["path"]:r for r in commit["rows"]};need(set(committed)=={r["path"]for r in rows},"Exact committed stable target set")
  overrides={r["path"]:r["sha256"]for r in rows if r["path"].endswith(".cs")};clean={r["path"]:r for r in data["offline_sources"]}
  need({r["path"]:r["stableProposedSha256"]for r in scope["sourceRows"]}==overrides and all(clean[p]["sha256"]==h for p,h in overrides.items()),"Exact complete source projection join")
  for row in rows:
   record=committed[row["path"]];p=read.derived(row["copy"]);need(read.pins[p]==row["sha256"]==record["cleanSha256"]and record["mode"]=="100644","Exact stable raw source/blob")
   raw=p.read_bytes();need(hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()==record["blob"],"Actual canonical Git blob bytes")
  census=data["census_output"];evidence=data["census_result"]["nativeCensusEvidence"]
  need(evidence["passed"]is True and read.path(evidence["output"])==read.path(proof["census_output"]["path"])and evidence["outputSha256"]==proof["census_output"]["sha256"],"Exact actual census output")
  need(census["candidate_sha256"]==fa["candidate_sha256"]and census["build_sha256"]==fa["build_sha256"]and census["locale_before"]==census["locale_after"]and census["gameplay_certified"]is False and census["queue_success_enabled"]is False,"Census envelope/locale")
  selected=[r for r in census["native_compiled_setups"]if (r["slot"]["catalog_id"],r["slot"]["seed"],r["slot"]["locale"])==(job["catalog_id"],job["seed"],job["locale"])]
  need(len(selected)==1,"Exactly one actual censused job")
  selected=selected[0];verify_job(job,selected["native_setup_identity"]);need(normalize_native_locale(selected["actual_native_locale"])==job["locale"],"Native actual locale")
  read.stable()
  native={r["path"]:r["sha256"]for r in fa["source_manifest"]}
  differences=[dict(path=p,owner=r["owner"],isolatedSha256=r["sha256"],nativeSha256=native.get(p))for p,r in clean.items()if native.get(p)!=r["sha256"]]
  answer.update(result="MeasuredNativeEditorCompileForExactCandidate",job_sha256=fingerprint(job),candidate_sha256=fa["candidate_sha256"],build_sha256=fa["build_sha256"],native_config_sha256=selected["native_config_sha256"],validated_code_commit=commit["commit"],coverage=dict(owningInputClosure=owning,nativeInputClosure=nativeClosure,nativeVsIsolatedDifferences=differences,isolatedWholeCurrentNativeSourceCertified=False,androidPlayerBuilt=False),proof_pins=[dict(path=str(p),sha256=h)for p,h in read.pins.items()])
 except (ValueError,OSError,KeyError,TypeError,IndexError,AttributeError,UnicodeError)as error:answer["errors"].append(str(error))
 return answer
