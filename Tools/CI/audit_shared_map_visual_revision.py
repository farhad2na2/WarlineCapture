"""Reconcile the reviewed PR59-61 shared visual revision against commit 81569142.
Reads historical scene records and preserves a reproducible source reconciliation receipt.
Does not edit any scene, asset, gameplay or Unity process.
"""
import hashlib, json, re, subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[2]
scene='Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity'
pointer=subprocess.check_output(['git','show','81569142:'+scene],cwd=root).decode()
oid=pointer.split('oid sha256:')[1].splitlines()[0]
before=root/'.git/lfs/objects'/oid[:2]/oid[2:4]/oid
# Pin the reviewed historical visual commit: later landscape edits must not
# silently change which scene this retained native822 receipt reconciles.
reviewed_commit='d5c9d600d'
reviewed_pointer=subprocess.check_output(['git','show',reviewed_commit+':'+scene],cwd=root).decode()
reviewed_oid=reviewed_pointer.split('oid sha256:')[1].splitlines()[0]
after=root/'.git/lfs/objects'/reviewed_oid[:2]/reviewed_oid[2:4]/reviewed_oid
receipt_path=root/'Design/AgentReports/MapVisualImprovement20261009/DenseCity-district-details.json'
receipt=json.loads(receipt_path.read_text())
scene_ids={m['transformId'] for m in receipt['moves'] if not m['instanceId']}
prefab_ids={}
for m in receipt['moves']:
    if m['instanceId']: prefab_ids.setdefault(m['instanceId'],set()).add(m['prefabTransformId'])
def digest(path):
    with path.open('rb') as f:
        h=hashlib.sha256()
        for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
        return h.hexdigest()
def records(path):
    with path.open() as f:
        block=[]
        for line in f:
            if line.startswith('--- !u!'):
                if block:yield ''.join(block)
                block=[]
            block.append(line)
        if block:yield ''.join(block)
def normalize(block):
    match=re.match(r'--- !u!(\d+) &(\d+)',block)
    kind,ident=(int(match[1]),int(match[2])) if match else (0,0)
    if kind==23:
        block=re.sub(r'(?m)^  m_Materials:\n(?:  - .*\n)*','  m_Materials: [visual]\n',block)
    if kind==33:
        block=re.sub(r'(?m)^  m_Mesh: .*\n','  m_Mesh: [visual]\n',block)
    if kind==1001:
        block=re.sub(r"(?m)^    - target: .*\n      propertyPath: '?m_Materials\.Array\.(?:data\[\d+\]|size)'?\n      value: .*\n      objectReference: .*\n",'',block)
        block=re.sub(r'(?m)^    - target: .*\n      propertyPath: m_Mesh\n      value: .*\n      objectReference: .*\n','',block)
        allowed=prefab_ids.get(ident,set())
        block=re.sub(r'(?m)^    - target: \{fileID: (-?\d+),[^\n]*\n      propertyPath: m_Local(?:Position|Rotation|EulerAnglesHint)\.[xyzw]\n      value: .*\n      objectReference: .*\n',lambda m:'' if int(m[1]) in allowed else m[0],block)
    if kind==4 and ident in scene_ids:
        block=re.sub(r'(?m)^  m_Local(?:Position|Rotation|EulerAnglesHint): .*\n','',block)
    return kind,ident,block
def inventory(path):
    result={}
    for block in records(path):
        kind,ident,text=normalize(block)
        key=(kind,ident)
        assert key not in result
        result[key]=(hashlib.sha256(text.encode()).hexdigest(),hashlib.sha256(block.encode()).hexdigest())
    return result
a=inventory(before);b=inventory(after)
protected=[k for k in a.keys()|b.keys() if a.get(k,(None,))[0]!=b.get(k,(None,))[0]]
changed=[k for k in a.keys()|b.keys() if a.get(k,(None,None))[1]!=b.get(k,(None,None))[1]]
assert digest(before)==oid
assert digest(after)==receipt['afterHash']
proof={'result':'Passed' if not protected else 'Failed','beforeSceneHash':oid,'afterSceneHash':digest(after),'baselineCommit':'81569142','approvedVisualCommits':['421384875','f1ec95e83','089ee8fb0'],'receipt':str(receipt_path.relative_to(root)),'objectRecordsBefore':len(a),'objectRecordsAfter':len(b),'changedRecords':len(changed),'unreconciledRecords':protected,'allowedDifferences':['MeshRenderer material slots','MeshFilter visual mesh references','prefab material/visual mesh overrides','59 render-only prop moves named by merged district receipt'],'geometryAndGameplayProtected':not protected,'physicalMapReauthored':False,'playerReady':False}
out=root/'Design/AgentReports/SkirmishExpansion/S005/native822-shared-map-visual-revision-reconciliation.json'
out.write_text(json.dumps(proof,indent=2)+'\n')
print(json.dumps(proof))
assert not protected
