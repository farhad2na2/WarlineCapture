"""Prepare an append-only review plan. Never writes Assets or Git state."""
import hashlib,json,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[5]
HERE=Path(__file__).resolve().parent
def rel(p): return p.relative_to(ROOT).as_posix()
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
parent=ROOT/'Design/AgentReports/SkirmishExpansion/EngineeringCheckpoint/CurrentCandidateStableUnion/CURRENT_CHECKPOINT.plan.json'
assert digest(parent)=='ff57d8f8785432b224b8077f01152f1572e0b241be632095282fdba660daff4e'
old=json.loads(parent.read_text()); oldrows={r['path']:r for r in old['exactCurrentRows']}
maps=[ROOT/'Tools/CI/SkirmishProductionDrafts'/d/'manifest.json' for d in ['AriaUiOwnedStorage','S005MeasuredOpeningCanvasSuccessor']]
payload=set(); overrides=[]; targets=[]
for mp in maps:
    payload.add(mp)
    for row in json.loads(mp.read_text())['files']:
        src=ROOT/row['source']; target=ROOT/row['target']
        assert digest(src)==row['sha256']
        actual=digest(target) if target.exists() else None
        assert actual in (row['before_sha256'],row['sha256']), (row['target'],actual)
        payload.update([src,target]); targets.append({**row,'actual_sha256_at_plan':actual,'installed_at_plan':actual==row['sha256']})
        if row['target'] in oldrows:
            overrides.append({'path':row['target'],'parent_selected_sha256':oldrows[row['target']]['sha256'],'successor_sha256':row['sha256'],'authority':rel(mp)})
for target in ['Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishObservationStorage.cs.meta','Assets/Tests/Editor/SkirmishExpansion/AriaUiOwnedStorageTests.cs.meta']:
    p=ROOT/target; assert p.exists(); payload.add(p)
for directory,names in {
 'AriaUiOwnedStorage':['READY.json','NATIVE_MARKERS.json','INDEPENDENT_SOURCE_REVIEW.c98531ea.json','INDEPENDENT_SOURCE_REVIEW.f27d1a28.json','compile_focused.py','compile_terminal_expectation.py'],
 'S005MeasuredOpeningCanvasSuccessor':['READY.json','INDEPENDENT_SOURCE_REVIEW.json','compile_editor.py'],
 'S005MeasuredOpeningCandidates':['manifest.json','SkirmishS002AriaRunHarness.OpeningGroupCandidates.cs.txt'],
}.items():
    for name in names:
        p=ROOT/'Tools/CI/SkirmishProductionDrafts'/directory/name; assert p.exists(); payload.add(p)
proofbase=ROOT/'Logs/SkirmishProduction/Offline/AriaUiOwnedStorage'
archive=HERE/'concise-owning-proof.zip'
assert not archive.exists()
with zipfile.ZipFile(archive,'x',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for folder in ['owning-142sui8j','tests-terminal-uuhyr_ch','measured-opening-iouetnte']:
        for p in sorted((proofbase/folder).iterdir()):
            if p.is_file() and p.suffix in ('.json','.log','.rsp'): z.write(p,rel(p))
payload.add(archive)
receipts=[ROOT/'Design/AgentReports/SkirmishExpansion'/p/'RECEIPT.json' for p in [
 'EngineeringCheckpoint/shared150-authoring-warmup-20261015-02',
 'EngineeringCheckpoint/s005-opening-four-with-ui-runtime-errors-20261015-01',
 'EngineeringCheckpoint/shared-native-preparation-20261015-01',
 'ProductionAdapter/aircraft-root-support-20261015-03']]
evidence=[]
for receipt in receipts:
    row=json.loads(receipt.read_text()); raw=ROOT/row['archive']; assert digest(raw)==row['sha256']
    payload.update([receipt,raw]); evidence.append({'receipt':rel(receipt),'receipt_sha256':digest(receipt),'archive':rel(raw),'sha256':digest(raw)})
rows=[]
for p in sorted(payload):
    if p.exists(): rows.append({'path':rel(p),'sha256':digest(p),'bytes':p.stat().st_size,'already_in_parent_plan':rel(p) in oldrows})
    else: raise RuntimeError('Root import pending: '+rel(p))
plan={'schema':1,'scope':'Append-only UI owned storage and measured opening diagnostic engineering checkpoint; Batch excluded',
 'parent':{'path':rel(parent),'sha256':digest(parent),'declared_stage_count':old['stagePathCount'],'root_reported_current_index_paths':538,'index_not_inspected_or_modified':True},
 'authority_maps':[{'path':rel(p),'sha256':digest(p)}for p in maps],
 'target_successors':targets,'changed_parent_overrides':overrides,'payload':rows,
 'additional_paths_not_in_parent':sorted(r['path'] for r in rows if not r['already_in_parent_plan']),
 'proof_archive':{'path':rel(archive),'sha256':digest(archive),'dlls_included':False,'sources_supplied_by_exact_draft_payload':True},
 'native_archives':evidence,'root_required_gates':['Actual UI7 controls and shared preparation wrapper0','Exception-free measured opening through quitting; exact four PNG/facts plus wrapper0','Rehash every selected actual target and two new metas after import/native exit','Root update exact path-specific -text rules if filtered Git bytes differ, and independently verify indexed blobs against selected SHA'],
 'acceptance':{'engineering_step_only':True,'camera_accepted':False,'mission_ready':False,'actual_normal_wins':False},
 'exclusions':['ProductionBaselineBatch and its proof/draft','Unlisted WIP and large offline DLLs','No physical map changes or new profile promotion'],
 'writes':{'Assets':False,'Git':False,'old_plan':False}}
(HERE/'APPEND.plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'plan':rel(HERE/'APPEND.plan.json'),'sha256':digest(HERE/'APPEND.plan.json'),'payload_paths':len(rows),'additional':len(plan['additional_paths_not_in_parent']),'overrides':len(overrides)},indent=2))
