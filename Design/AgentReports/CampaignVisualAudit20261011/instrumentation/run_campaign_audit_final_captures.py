import subprocess,json,time,shutil
from pathlib import Path
p=Path('/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit')
v=p/'Design/AgentReports/CampaignVisualAudit20261011/validation'
(v/'source-helper-compile-repair.txt').write_text("Initial compile failed: CampaignSourceVisualAudit20261011.cs(40,43) CS1061, ReadOnlySpan<OperationMapAnchorConfig> has no Where. Corrected audit helper to enumerate Anchors.ToArray(); no production code changed.\n")
deadline=time.time()+360
while time.time()<deadline:
 r=subprocess.run(['rtk','proxy','unity','command','recompile_status','--project-path',str(p),'--format','json'],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,cwd=p)
 (v/'source-helper-recompile-status.json').write_bytes(r.stdout)
 try:
  envelope=json.loads(r.stdout); state=envelope['data']['result'];state=json.loads(state) if isinstance(state,str) else state
  if state.get('status')=='completed':
   if state.get('failed'):raise RuntimeError(str(state))
   print('Audit source helper compilation passed',flush=True);break
 except (json.JSONDecodeError,KeyError,TypeError):pass
 time.sleep(4)
else:raise RuntimeError('Compile completion not confirmed')
log=Path('/private/tmp/warline-campaign-source-preview-20261011.log')
cmd=['rtk','proxy','Tools/CI/invoke_unity_macos.sh','--reuse','--project',str(p),'--timeout','420','--log',str(log),'--','-executeMethod','Game.Editor.CampaignSourceVisualAudit20261011.CaptureBlockedSources']
print('START four blocked-map authored-source previews',flush=True)
with (v/'source-preview-wrapper.log').open('wb') as f:r=subprocess.run(cmd,cwd=p,stdout=f,stderr=subprocess.STDOUT)
for suffix in ['', '.exit','.dispatch.json']:
 s=Path(str(log)+suffix)
 if s.exists():shutil.copy2(s,v/('source-preview'+(suffix or '.log')))
if r.returncode or '[CampaignSourceVisualAudit] result=Passed missions=4' not in log.read_text():raise RuntimeError('Source-preview capture failed; inspect preserved log')
print('PASS source previews=4',flush=True)
subprocess.run(['rtk','proxy','python3','/private/tmp/run_campaign_visual_audit_supplemental.py'],cwd=p,check=True)
