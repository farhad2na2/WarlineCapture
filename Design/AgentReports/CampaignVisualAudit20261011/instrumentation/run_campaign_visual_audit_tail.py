import json, subprocess, shutil, re
from pathlib import Path
p=Path('/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit')
root=p/'Design/AgentReports/CampaignVisualAudit20261011'
v=root/'validation'; rows=json.loads((root/'inventory.json').read_text())['missions']
results=[]
for idx in range(20,25):
 for attempt in ([2] if idx==20 else [1,2]):
  run=f'{attempt:02d}'; mid=rows[idx]['missionId']; target=root/'captures'/mid/f'run-{run}'
  if target.exists(): raise RuntimeError('Existing evidence must be preserved: '+str(target))
  req={'index':idx,'run':run,'warmupSeconds':6,'reimportSource':attempt==2}
  (root/'request.json').write_text(json.dumps(req))
  log=Path(f'/private/tmp/warline-campaign-audit-m{idx+1:02d}-{run}.log')
  print(f'START {idx+1:02d}/25 run={run} reimport={attempt==2}',flush=True)
  cmd=['rtk','proxy','Tools/CI/invoke_unity_macos.sh','--reuse','--project',str(p),'--timeout','420','--log',str(log),'--','-executeMethod','Game.Editor.CampaignVisualAudit20261011.CaptureMission']
  with (v/f'm{idx+1:02d}-{run}-wrapper.log').open('wb') as output:r=subprocess.run(cmd,cwd=p,stdout=output,stderr=subprocess.STDOUT)
  receipt=json.loads((target/'receipt.json').read_text()) if (target/'receipt.json').exists() else {}
  text=log.read_text() if log.exists() else ''
  passed=r.returncode==0 and f'[CampaignVisualAudit] result=Passed mission={mid} captures=4' in text and receipt.get('activeMissionId')==mid and receipt.get('result')=='Passed'
  for suffix in ['', '.exit', '.dispatch.json']:
   source=Path(str(log)+suffix)
   if source.exists():shutil.copy2(source,v/f'm{idx+1:02d}-{run}{suffix or ".log"}')
  results.append({'index':idx,'missionId':mid,'run':run,'wrapperExit':r.returncode,'passed':passed,'reimport':attempt==2,'error':receipt.get('error',''),'receipt':str(target.relative_to(root)/'receipt.json')})
  (v/'capture-tail-results.json').write_text(json.dumps(results,indent=2))
  hud='Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab'; diff=subprocess.check_output(['git','diff','--',hud],cwd=p)
  if diff:
   (v/f'm{idx+1:02d}-{run}-hud-side-effect.diff').write_bytes(diff);subprocess.run(['git','restore','--',hud],cwd=p,check=True)
  print(f'RESULT {idx+1:02d}/25 run={run} passed={passed} outcome={receipt.get("missionOutcome")} error={receipt.get("error","")[:140]}',flush=True)
  if passed:break
print('TAIL CAPTURE ATTEMPTS COMPLETE',flush=True)
