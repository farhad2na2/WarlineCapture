import json, subprocess, time, shutil
from pathlib import Path

project=Path('/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit')
root=project/'Design/AgentReports/CampaignVisualAudit20261011'
validation=root/'validation'
validation.mkdir(exist_ok=True)
missions=json.loads((root/'inventory.json').read_text())['missions']
hud='Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab'

def restore_audit_side_effect(label):
    diff=subprocess.check_output(['git','diff','--',hud],cwd=project)
    if diff:
        (validation/f'{label}-hud-side-effect.diff').write_bytes(diff)
        subprocess.run(['git','restore','--',hud],cwd=project,check=True)

restore_audit_side_effect('m01-01')
results=[]
for row in missions:
    idx=row['index']; mid=row['missionId']; run='01'
    target=root/'captures'/mid/f'run-{run}'
    log=Path(f'/private/tmp/warline-campaign-audit-m{idx+1:02d}-{run}.log')
    if idx==0:
        code=0
    else:
        if target.exists(): raise RuntimeError(f'Preserve existing evidence: {target}')
        (root/'request.json').write_text(json.dumps({'index':idx,'run':run}))
        command=['rtk','proxy','Tools/CI/invoke_unity_macos.sh','--reuse','--project',str(project),'--timeout','420','--log',str(log),'--','-executeMethod','Game.Editor.CampaignVisualAudit20261011.CaptureMission']
        print(f'START {idx+1:02d}/25 {mid}',flush=True)
        with (validation/f'm{idx+1:02d}-{run}-wrapper.log').open('wb') as output:
            result=subprocess.run(command,cwd=project,stdout=output,stderr=subprocess.STDOUT)
        code=result.returncode
    receipt=json.loads((target/'receipt.json').read_text()) if (target/'receipt.json').exists() else {}
    log_text=log.read_text() if log.exists() else ''
    marker=f'[CampaignVisualAudit] result=Passed mission={mid} captures=4'
    passed=code==0 and marker in log_text and receipt.get('result')=='Passed' and receipt.get('activeMissionId')==mid and receipt.get('activeMapId')==row['mapId'] and all((target/f'{x}.png').exists() for x in ['opening','opening-hud','objective-context','objective-detail','mission-overview'])
    for suffix in ['', '.exit']:
        source=Path(str(log)+suffix)
        if source.exists(): shutil.copy2(source, validation/f'm{idx+1:02d}-{run}{suffix or ".log"}')
    results.append({'index':idx,'missionId':mid,'run':run,'wrapperExit':code,'passed':passed,'receipt':str(target.relative_to(root)/'receipt.json')})
    (validation/'capture-results.json').write_text(json.dumps(results,indent=2))
    restore_audit_side_effect(f'm{idx+1:02d}-{run}')
    print(f'RESULT {idx+1:02d}/25 passed={passed} wrapperExit={code}',flush=True)
    if not passed: raise RuntimeError(f'Capture failed for {mid}; inspect preserved logs before proceeding')
print('COMPLETE native mission capture 25/25',flush=True)
