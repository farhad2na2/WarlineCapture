import os,sys,time,json,hashlib,struct,subprocess,xml.etree.ElementTree as ET
from pathlib import Path
root=Path('/Users/farhad/.codex/worktrees/skirmish-saga-menu/WarlineCapture')
out=root/'Design/AgentReports/MissionAtlasColorPolish/menu-card-followup';out.mkdir(parents=True,exist_ok=True)
state={'status':'Running','stages':[]}
def save(): (out/'VALIDATION.json').write_text(json.dumps(state,indent=2)+'\n')
def fingerprint():
 extensions={'.cs','.asmdef','.asmref','.rsp','.json','.asset','.prefab','.unity','.shader','.hlsl','.cginc','.compute','.meta'}
 files=sorted((p for d in ('Assets','Packages','ProjectSettings') for p in (root/d).rglob('*') if p.is_file() and p.suffix.lower() in extensions),key=lambda p:str(p).encode('utf-16-be'))
 manifest=hashlib.sha256(); rows=[]
 for p in files:
  h=hashlib.sha256()
  with p.open('rb') as f:
   for chunk in iter(lambda:f.read(1048576),b''): h.update(chunk)
  digest=h.digest(); rel=p.relative_to(root).as_posix(); rows.append({'path':rel,'sha256':h.hexdigest()})
  encoded=rel.encode('utf-8'); size=len(encoded)
  while size>=128: manifest.update(bytes([(size&127)|128]));size>>=7
  manifest.update(bytes([size]));manifest.update(encoded);manifest.update(struct.pack('<i',32));manifest.update(digest)
 return manifest.hexdigest(),rows
initial,rows=fingerprint();state['candidateSourceHash']=initial
(out/'SOURCE_MANIFEST.json').write_text(json.dumps({'sourceHash':initial,'files':rows},indent=2)+'\n');save();print('Frozen source:',initial,flush=True)
def run(name,args,markers,timeout=1200,env=None):
 log=out/'logs'/(name+'.log')
 cmd=['rtk','proxy','Tools/CI/invoke_unity_macos.sh','--unity','/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity','--timeout',str(timeout),'--log',str(log),'--']+args
 start=time.time();stage={'name':name,'status':'Running','log':str(log),'started':start};state['stages'].append(stage);save();print('START',name,flush=True)
 result=subprocess.run(cmd,cwd=root,env=env)
 text=log.read_text(errors='replace') if log.exists() else ''
 stage.update(exitCode=result.returncode,elapsedSeconds=round(time.time()-start,1),markers={x:x in text for x in markers})
 if result.returncode!=0 or not all(stage['markers'].values()):stage['status']='Failed';save();raise RuntimeError('Validation failed: '+name)
 stage['status']='Passed';save();print('PASS',name,stage['elapsedSeconds'],flush=True);return start,text
try:
 _,visual=run('native-review',['-executeMethod','Game.Editor.ApprovedMissionAtlasBuilder.ReviewExisting'],['[ApprovedMissionAtlasReview] result=Passed','[ArmySizeChoiceReview] result=Passed','[ArmySizeChoiceReview] rebindStyles=Passed'],720)
 import re
 state['nativeVisualOutput']=re.findall(r'output=(Design/AgentReports/ApprovedMissionAtlas/[^\s]+)',visual)[-1];save()
 xml=str(out/'logs/tests.xml')
 run('tests',['-runTests','-testPlatform','EditMode','-testFilter','Game.Tests.Editor.SkirmishLibraryViewModelTests;Game.Tests.Editor.SkirmishMedalLedgerTests;Game.Tests.Editor.SkirmishLibraryJourneyTests;MainMenuNavigationViewTests','-testResults',xml],[],600)
 tests=ET.parse(xml).getroot().attrib
 if tests.get('result')!='Passed' or int(tests.get('passed',0))!=35 or int(tests.get('failed',1))!=0:raise RuntimeError('Focused tests did not pass 35/35.')
 state['tests']=tests;save()
 run('campaign',['-executeMethod','Game.Editor.CampaignTutorialEntryEditorProbe.RunApprovedAtlasM01Journey'],['[ApprovedCampaignJourney] result=Passed'],1200)
 for locale,suffix in [('en','En'),('fa-IR','Fa')]:
  start,log=run('s002-'+locale,['-executeMethod','Game.Editor.SkirmishS002AriaRunHarness.RunSagaS002EditorAria'+suffix],['[SagaNativeMedalJourney] result=Passed','[SkirmishS002AriaRun] appended=1 countedWin=1 error='],1200)
  matches=[]
  for p in (root/'Design/AgentReports/SkirmishExpansion/S002/_Evidence').glob('*-saga-medal-proof.json'):
   if p.stat().st_mtime<start:continue
   proof=json.loads(p.read_text())
   if proof.get('locale')==locale:matches.append((p,proof))
  if len(matches)!=1:raise RuntimeError('Expected exactly one fresh native medal proof for '+locale)
  p,proof=matches[0]
  if proof.get('candidateSourceHash')!=initial or proof.get('outcome')!='Victory' or not proof.get('nativeMissionCardMedalVisible'):raise RuntimeError('Fresh medal proof does not match frozen native candidate.')
  state['stages'][-1]['proof']=str(p);save()
  env=os.environ.copy();env['WARLINE_SAGA_MEDAL_PROOF']=str(p)
  run('restart-'+locale,['-executeMethod','Game.Editor.SkirmishS002AriaRunHarness.RunSagaProcessRestartProof'],['[SagaProcessRestart] result=Passed'],600,env)
 final,_=fingerprint();state['finalSourceHash']=final
 if final!=initial:raise RuntimeError('Candidate source changed during validation.')
 state['status']='Passed';save();print('ALL FINAL VALIDATIONS PASSED',initial,flush=True)
except BaseException as error:
 state['status']='Failed';state['error']=str(error);save();print('FAILED',str(error),flush=True);raise
