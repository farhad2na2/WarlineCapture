"""Verify final native visual evidence without claiming gameplay/device acceptance."""
from pathlib import Path
import hashlib, json
root=Path(__file__).resolve().parent.parent
notes=json.loads((root/'fixes/visual-review-notes.json').read_text())
inventory=json.loads((root/'inventory.json').read_text())['missions']
remaining={2,4,5,15,17,18,19}
rows=[]
for mission in inventory:
    index=mission['index']; run='visual-fix-02' if index in remaining else 'visual-fix-04'
    directory=root/'captures'/mission['missionId']/('run-'+run)
    receipt=directory/'receipt.json'; d=json.loads(receipt.read_text())
    assert d['result']=='Passed' and d['missionOutcome']=='None', mission['missionId']
    assert d['missionPhase']==('FindSquad' if index==0 else 'Engage'), mission['missionId']
    rejected=[r for r in d['spawnRequests'] if 'status=2' in r and ('requirePreferred=1' in r or 'authoredStartingGrant=1' in r)]
    assert not rejected, (mission['missionId'],rejected)
    key=str(receipt.relative_to(root)); assert notes[key]['status']=='reviewed', key
    lights=[s for s in d['lightStates'] if 'enabled=True active=True' in s and 'shadows=Soft' in s]
    assert len(lights)==1,(mission['missionId'],lights)
    log=root/'fixes/validation'/('remaining-native-02.log' if index in remaining else f'targeted-{run}-{index}.log')
    assert Path(str(log)+'.exit').read_text().strip()=='0',str(log)
    assert '[CampaignVisualAudit] result=Passed mission='+mission['missionId'] in log.read_text(errors='replace'),str(log)
    files=[receipt,*sorted(directory.glob('*.png'))]
    rows.append({'index':index,'missionId':mission['missionId'],'receipt':key,'phase':d['missionPhase'],'requiredPlacementFailures':0,'visualReview':notes[key],'shadowKeyCount':len(lights),'evidence':[{ 'path':str(f.relative_to(root)), 'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in files]})
packed=(root/'fixes/validation/packed-road-entries-01.log').read_text(errors='replace')
assert '[OperationMapRenderDatabaseBuilder] sourceParity=Passed' in packed
assert '[OperationMapRenderDatabaseBuilder] determinism=Passed' in packed
assert len(rows)==25
result={'result':'Passed','nativeCaptures':25,'manuallyReviewedOpenings':25,'requiredPlacementFailures':0,'packedParity':'Passed','determinism':'Passed','normalInputMissionCompletion':'pending','androidAcceptance':'pending','missions':rows}
(root/'fixes/final-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print('[CampaignFinalVisualEvidence] result=Passed missions=25 requiredPlacementFailures=0 visualOpeningsReviewed=25 normalInput=pending android=pending')
