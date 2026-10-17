"""Emit proposed paths/attributes and verify isolated Git filters; never stage."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Design/AgentReports/SkirmishExpansion/EngineeringCheckpoint/Ground30Only58'
PACKAGE=ROOT/'Tools/CI/SkirmishProductionDrafts/Ground30Subset58'
def sha(b):return hashlib.sha256(b).hexdigest()
def git(args,**kwargs):return subprocess.run(['rtk','proxy','git',*args],capture_output=True,check=True,**kwargs).stdout

def main():
    paths=set((OUT/'candidate-existing.paths').read_text().splitlines())
    paths.update((OUT/'pending-import.paths').read_text().splitlines())
    source_map=json.loads((PACKAGE/'manifest.json').read_text())
    workflow=json.loads((PACKAGE/'workflow.json').read_text())
    sources={r['target']:ROOT/r['source'] for r in source_map['files']+workflow['files']}
    sources[source_map['pending_meta']['target']]=ROOT/source_map['pending_meta']['source']
    paths.update(p.relative_to(ROOT).as_posix() for p in PACKAGE.rglob('*')
                 if p.is_file() and '__pycache__' not in str(p) and p.name!='READY.json')
    paths.update(['Tools/CI/skirmish_head278_ground30_58_offline_compile.py','Tools/CI/ground30_subset58_workflow_test.py',
        'Tools/CI/prepare_ground30_subset58_stage_list.py',
        'Design/AgentReports/SkirmishExpansion/EngineeringCheckpoint/ground30-subset58-independent-review-20261015.json',
        'Design/AgentReports/SkirmishExpansion/EngineeringCheckpoint/head278-g30-58-compile-zyxol3v4/checked-owning-proof.zip',
        'Design/AgentReports/SkirmishExpansion/EngineeringCheckpoint/head278-g30-58-compile-zyxol3v4/FINAL_SOURCE_RECHECK.json',
        'Design/AgentReports/SkirmishExpansion/ProductionAdapter/production-authoring-native-20261015/Unity5-8-run02/RECEIPT.json',
        'Design/AgentReports/SkirmishExpansion/ProductionAdapter/production-authoring-native-20261015/Unity5-8-run02/passed-corrected-production-authoring-preparation.zip'])
    paths.update((OUT/p).relative_to(ROOT).as_posix() for p in ('candidate-existing.paths','pending-import.paths','receipt.json','CHECKPOINT.md'))
    attrs=PACKAGE/'Attributes';attrs.mkdir(exist_ok=True)
    for name in ('fragment.gitattributes','projected.gitattributes'):
        paths.add((attrs/name).relative_to(ROOT).as_posix())
    paths.add('.gitattributes')
    stage_path=OUT/'complete-stage.paths';paths.add(stage_path.relative_to(ROOT).as_posix())
    # Records are added after verification; their own contents are not raw-byte
    # authorities. All hashes below describe proposed source/metadata bytes.
    records_path=OUT/'STAGE_RECEIPT.json';paths.add(records_path.relative_to(ROOT).as_posix())
    verify_path=OUT/'git-byte-preservation.json';paths.add(verify_path.relative_to(ROOT).as_posix())
    missing=[p for p in paths if p not in sources and not (ROOT/p).is_file() and p not in
        {str(q.relative_to(ROOT)).replace('\\','/') for q in (stage_path,records_path,verify_path,attrs/'fragment.gitattributes',attrs/'projected.gitattributes')}]
    if missing:raise ValueError('Missing proposed dependency: '+repr(missing))
    byte_paths=[]
    for p in sorted(paths):
        actual=sources.get(p,ROOT/p)
        if actual.is_file() and b'\r\n' in actual.read_bytes():byte_paths.append(p)
    fragment='# Exact engineering authority bytes; scoped preservation, no global policy change.\n'
    fragment+=''.join('/'+p+' -text\n' for p in byte_paths if p!='.gitattributes')
    fragment=fragment.encode('utf-8')
    (attrs/'fragment.gitattributes').write_bytes(fragment)
    original=(ROOT/'.gitattributes').read_bytes()
    projected=original.rstrip(b'\n')+b'\n\n'+fragment
    (attrs/'projected.gitattributes').write_bytes(projected);sources['.gitattributes']=attrs/'projected.gitattributes'
    stage_path.write_bytes(('\n'.join(sorted(paths))+'\n').encode('utf-8'))
    scratch=ROOT/'Logs/SkirmishProduction/Offline/Ground30GitFilters';scratch.mkdir(parents=True,exist_ok=True)
    work=Path(tempfile.mkdtemp(prefix='isolated-',dir=scratch))
    git(['-C',str(work),'init','-q'])
    (work/'.gitattributes').write_bytes(projected)
    checked=[]
    for p in sorted(paths):
        if p in {records_path.relative_to(ROOT).as_posix(),verify_path.relative_to(ROOT).as_posix()}:continue
        data=sources.get(p,ROOT/p).read_bytes()
        oid=git(['-C',str(work),'hash-object','-w','--path',p,'--stdin'],input=data).decode().strip()
        blob=git(['-C',str(work),'cat-file','blob',oid])
        checkout=git(['-C',str(work),'-c','core.autocrlf=false','cat-file','--filters','--path='+p,oid])
        if data!=blob or data!=checkout:raise ValueError('Projected Git filter changes pinned bytes: '+p)
        checked.append(dict(path=p,sha256=sha(data),bytes=len(data),git_blob=oid))
    verification=dict(result='ProjectedGitBlobAndCheckoutBytesEqualPinnedInput',isolated_repository=str(work),
        actual_repository_staged=False,actual_repository_attributes_changed=False,checked_paths=len(checked),
        projected_attributes_sha256=sha(projected),preserved_crlf_paths=byte_paths,records=checked)
    verify_path.write_text(json.dumps(verification,indent=2)+'\n',encoding='utf-8',newline='\n')
    receipt=dict(result='CompleteProposedEngineeringStageList_NoStagingPerformed',paths=len(paths),
        source_targets=63,pending_root_copies=[r['target'] for r in workflow['files']]+[source_map['files'][-1]['target'],source_map['pending_meta']['target'],'.gitattributes'],
        source_overrides={p:dict(source=q.relative_to(ROOT).as_posix(),sha256=sha(q.read_bytes())) for p,q in sources.items()},
        stage_list_sha256=sha(stage_path.read_bytes()),git_filter_receipt_sha256=sha(verify_path.read_bytes()),
        attribution='Native30/60+5+3 same functions passed on mixed full candidate; isolated eight-owner subset type proof passed. No native isolated-subset or mission readiness claim.',
        warm_outcome='Root reported frozen WarmNone outer1/Unity2 menuTimeout, endpoints equal, no ASIO crash marker; preserve separate failed evidence. Not a success claim.',
        pending=['Root applies projected narrow attributes and exact source/tool copies after freeze','Root verifies actual staged blobs against these bytes','Root adds concise actual WarmNone outcome receipt if desired; never relabel failure'],
        excluded=['Camera/profile/Guard/Batch/rigid Transport/runtime UI additions','Unconsumed launcher/joint/pilot/source-chunk future packages','Full offline DLL/RSP/source proof trees','Historical failed log modifications and ignored Logs'],
        readiness=0)
    records_path.write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n')
    print('COMPLETE_STAGE_PLAN_PASS paths='+str(len(paths))+' filtered='+str(len(checked))+' crlf='+str(len(byte_paths))+' receipt='+sha(records_path.read_bytes()))

if __name__=='__main__':main()
