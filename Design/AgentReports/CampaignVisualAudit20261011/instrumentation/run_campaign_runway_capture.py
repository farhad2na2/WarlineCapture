import json,time,subprocess,shutil
from pathlib import Path
p=Path('/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit');v=p/'Design/AgentReports/CampaignVisualAudit20261011/validation'
deadline=time.time()+1000
while time.time()<deadline:
 f=v/'capture-supplemental-results.json'
 if f.exists() and len(json.loads(f.read_text()))==6:break
 time.sleep(5)
else:raise RuntimeError('Supplemental lane did not finish; no source helper update applied')
shutil.copy2(p/'Assets/Game/Scripts/Editor/CampaignSourceVisualAudit20261011.cs',v.parent/'instrumentation/CampaignSourceVisualAudit20261011-v1.cs')
shutil.copy2('/private/tmp/CampaignSourceVisualAudit20261011.cs',p/'Assets/Game/Scripts/Editor/CampaignSourceVisualAudit20261011.cs')
r=subprocess.run(['rtk','proxy','unity','command','recompile','--project-path',str(p),'--format','json'],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,cwd=p);(v/'runway-helper-recompile.json').write_bytes(r.stdout)
print('Runway helper compilation started',flush=True)
deadline=time.time()+360
while time.time()<deadline:
 r=subprocess.run(['rtk','proxy','unity','command','recompile_status','--project-path',str(p),'--format','json'],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,cwd=p);(v/'runway-helper-recompile-status.json').write_bytes(r.stdout)
 try:
  env=json.loads(r.stdout);state=env['data']['result'];state=json.loads(state) if isinstance(state,str) else state
  if state.get('status')=='completed':
   if state.get('failed'):raise RuntimeError(str(state))
   break
 except (json.JSONDecodeError,KeyError,TypeError):pass
 time.sleep(4)
else:raise RuntimeError('Compile completion not confirmed')
log=Path('/private/tmp/warline-campaign-runway-preview-20261011.log')
cmd=['rtk','proxy','Tools/CI/invoke_unity_macos.sh','--reuse','--project',str(p),'--timeout','420','--log',str(log),'--','-executeMethod','Game.Editor.CampaignSourceVisualAudit20261011.CaptureRunwayEnds']
print('START native authored runway endpoint capture',flush=True)
with (v/'runway-preview-wrapper.log').open('wb') as f:r=subprocess.run(cmd,cwd=p,stdout=f,stderr=subprocess.STDOUT)
for suffix in ['', '.exit','.dispatch.json']:
 s=Path(str(log)+suffix)
 if s.exists():shutil.copy2(s,v/('runway-preview'+(suffix or '.log')))
if r.returncode or '[CampaignRunwayAudit] result=Passed ' not in log.read_text():raise RuntimeError('Runway capture failed; inspect retained log')
print('PASS runway endpoint captures',flush=True)
