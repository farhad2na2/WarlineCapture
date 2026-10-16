"""Exercise the real focused wrapper against an isolated non-launching child stub."""
import argparse,json,os,shutil,subprocess
from pathlib import Path
PS=r"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
STUB=r'''param([string]$UnityExe,[string]$ProjectPath,[string]$LogFile,[switch]$NoProcessExit,[switch]$GuiLicensing,[int]$TimeoutSeconds,[string[]]$UnityArguments)
$case=Get-Content -LiteralPath $env:WARLINE_FOCUSED_TEST_CASE -Raw | ConvertFrom-Json
@{UnityExe=$UnityExe;ProjectPath=$ProjectPath;LogFile=$LogFile;NoProcessExit=[bool]$NoProcessExit;GuiLicensing=[bool]$GuiLicensing;TimeoutSeconds=$TimeoutSeconds;UnityArguments=@($UnityArguments)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $case.capture -Encoding UTF8
if ($case.writeLog) { [System.IO.File]::WriteAllText($LogFile,[string]$case.log) }
$global:LASTEXITCODE=[int]$case.exit
'''
def run(root,output):
 output.mkdir(parents=True,exist_ok=False)
 source=root/'Tools/CI/InvokeUnityExecuteMethodValidation.ps1'
 assert source.is_file()
 # Only the copied focused wrapper runs; adjacent stub cannot launch Unity.
 isolated=output/'isolated';isolated.mkdir()
 copied=isolated/source.name;shutil.copyfile(source,copied)
 (isolated/'InvokeUnity.ps1').write_text(STUB,encoding='utf-8')
 marker='[FocusedWrapperControls] fixture=Expected'
 method='Game.Tests.DataOnlyFixture.Run'
 cases=[
  dict(name='default',flags=[],ok=True,gui=False,graphics=False),
  dict(name='data-only',flags=['-NoGraphics'],ok=True,gui=False,graphics=True),
  dict(name='gui-default',flags=['-GuiLicensing'],ok=True,gui=True,graphics=False),
  dict(name='gui-no-graphics-refused',flags=['-NoGraphics','-GuiLicensing'],ok=False,launch=False,preexisting=True),
  dict(name='missing-marker',flags=['-NoGraphics'],ok=False,log='no marker'),
  dict(name='failure-beats-success',flags=['-NoGraphics'],ok=False,log=marker+'\nresult=Failed'),
  dict(name='child-failure-beats-marker',flags=['-NoGraphics'],ok=False,exit=7),
  dict(name='exception-beats-marker',flags=['-NoGraphics'],ok=False,log=marker+'\nexecuteMethod method '+method+' threw exception.'),
  dict(name='missing-log',flags=['-NoGraphics'],ok=False,writeLog=False)
 ]
 rows=[]
 for c in cases:
  folder=output/c['name'];folder.mkdir()
  log=folder/'unity.log';capture=folder/'invocation.json';config=folder/'case.json'
  if c.get('preexisting'):log.write_text('preserve-existing-evidence',encoding='utf-8')
  config.write_text(json.dumps(dict(capture=str(capture),writeLog=c.get('writeLog',True),log=c.get('log',marker),exit=c.get('exit',0))),encoding='utf-8')
  env=dict(os.environ,WARLINE_FOCUSED_TEST_CASE=str(config))
  args=[PS,'-NoProfile','-ExecutionPolicy','Bypass','-File',str(copied),'-UnityExe','not-launched.exe','-ProjectPath',str(root),'-ExecuteMethod',method,'-LogFile',str(log),'-RequiredPassMarker',marker,'-BuildTarget','Android','-RefreshImportMode','InProcess','-TimeoutSeconds','123']+c['flags']
  p=subprocess.run(['rtk','proxy']+args,env=env,cwd=root,capture_output=True,text=True,timeout=30)
  (folder/'wrapper.log').write_text(p.stdout+p.stderr,encoding='utf-8')
  passed=(p.returncode==0)==c['ok']
  if c.get('launch',True):
   passed &= capture.exists()
   if capture.exists():
    invocation=json.loads(capture.read_text(encoding='utf-8-sig'))
    expected=['-quit']+(['-nographics'] if c.get('graphics',False) else [])+['-buildTarget','Android','-refreshImportMode','InProcess','-executeMethod',method]
    # Failure cases still have the same NoGraphics forwarding.
    if '-NoGraphics' in c['flags']:expected=['-quit','-nographics','-buildTarget','Android','-refreshImportMode','InProcess','-executeMethod',method]
    passed &= invocation['UnityArguments']==expected and invocation['TimeoutSeconds']==123 and invocation['NoProcessExit'] is True and invocation['GuiLicensing'] is c.get('gui',False)
  else:
   passed &= not capture.exists() and log.read_text(encoding='utf-8')=='preserve-existing-evidence'
  rows.append(dict(name=c['name'],passed=bool(passed),exit=p.returncode,expectedSuccess=c['ok']))
 result=dict(result='Passed' if all(r['passed'] for r in rows) else 'Failed',controls=len(rows),nativeLaunched=False,cases=rows)
 (output/'result.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
 print('[FocusedUnityWrapperControls] result='+result['result']+' controls='+str(len(rows))+' nativeLaunched=0')
 if result['result']!='Passed':raise SystemExit(1)
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',required=True,type=Path);a=p.parse_args()
 run(Path(__file__).resolve().parents[2],a.output.resolve())
