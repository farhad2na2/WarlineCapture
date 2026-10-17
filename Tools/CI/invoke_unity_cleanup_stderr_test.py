"""Actual checked wrapper, owned child and taskkill stderr shim; never Unity."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile

SHIM=r'''
using System;
using System.Diagnostics;
class TaskkillStderrProbe {
 static int Main(string[] args) {
  int id=int.Parse(args[1]);
  Console.WriteLine("OWNED_SYNTHETIC_TASKKILL_TARGET="+id);
  if(Environment.GetEnvironmentVariable("WARLINE_TEST_TASKKILL_MODE")=="killthenstderr") {
   try { Process.GetProcessById(id).Kill(); } catch(ArgumentException) {}
  }
  Console.Error.WriteLine("ERROR: Owned synthetic child could not be terminated: already ended or intentional failure.");
  return 128;
 }
}
'''

def main():
    root=Path(__file__).resolve().parents[2]
    spec=importlib.util.spec_from_file_location('exit_controls',root/'Tools/CI/invoke_unity_process_exit_test.py')
    baseline=importlib.util.module_from_spec(spec);spec.loader.exec_module(baseline)
    parent=root/'Logs/SkirmishProduction/Offline/InvokeUnityCleanupStderr';parent.mkdir(parents=True,exist_ok=True)
    out=Path(tempfile.mkdtemp(prefix='owned-',dir=parent))
    child=baseline.CHILD.replace('string log=Arg(args,"-logFile","");','''if(args.Length==2&&args[0]=="--check-pid") {
    try { return System.Diagnostics.Process.GetProcessById(int.Parse(args[1])).HasExited?0:1; } catch(ArgumentException) { return 0; }
   }
   string log=Arg(args,"-logFile","");
   File.WriteAllText(log+".owned-pid",System.Diagnostics.Process.GetCurrentProcess().Id.ToString());''')
    compiler=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    fake_root=out/'fake-system-root';taskkill=fake_root/'System32/taskkill.exe';taskkill.parent.mkdir(parents=True)
    executable=out/'OwnedExitProbe.exe'
    for name,code,exe in [('OwnedExitProbe',child,executable),('TaskkillStderrProbe',SHIM,taskkill)]:
        source=out/(name+'.cs');source.write_text(code,encoding='utf-8')
        build=subprocess.run(['rtk','proxy',str(compiler),'/nologo','/target:exe','/out:'+str(exe),str(source)],capture_output=True,text=True)
        (out/(name+'.compiler.log')).write_text(build.stdout+build.stderr,encoding='utf-8')
        if build.returncode:raise RuntimeError(build.stdout+build.stderr)
    results=[]
    for name,mode,fatal,expected in [('fatal-child-ended-stderr','killthenstderr',1,1),('fatal-stderr-root-fallback','no-kill',1,1),('timeout-stderr-root-fallback','no-kill',0,124)]:
        log=out/(name+'.log');q=baseline.quote
        command='$env:SystemRoot='+q(fake_root)+'\n$env:WARLINE_TEST_TASKKILL_MODE='+q(mode)+'\n'
        command+='& '+q(root/'Tools/CI/InvokeUnity.ps1')+' -UnityExe '+q(executable)+' -ProjectPath '+q(out)+' -LogFile '+q(log)+\
            " -GuiLicensing -TimeoutSeconds 1 -UnityArguments @('--sleep','20000','--fatal',"+q(fatal)+')\nexit $global:LASTEXITCODE\n'
        script=out/(name+'.ps1');script.write_text(command,encoding='utf-8-sig')
        process=subprocess.run(['rtk','proxy','powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-File',str(script)],cwd=root,capture_output=True,text=True,timeout=35)
        console=process.stdout+process.stderr;(out/(name+'.console.log')).write_text(console,encoding='utf-8')
        text=log.read_text(encoding='utf-8-sig',errors='replace')
        pid=int(Path(str(log)+'.owned-pid').read_text())
        check=subprocess.run(['rtk','proxy',str(executable),'--check-pid',str(pid)],capture_output=True,text=True)
        passed=process.returncode==expected and '[UnityInvoke] ExitCode: '+str(expected) in text and \
            '[UnityInvoke] taskkill ExitCode: 128' in text and 'OWNED_SYNTHETIC_TASKKILL_TARGET='+str(pid) in text and \
            'could not be terminated' in text and check.returncode==0 and 'NativeCommandError' not in console
        if mode=='no-kill':passed=passed and 'attempting its cached Process handle' in text
        if fatal:passed=passed and 'fatal log marker' in text
        results.append({'case':name,'passed':passed,'actual_exit':process.returncode,'expected_exit':expected,'owned_pid':pid,'owned_child_exited':check.returncode==0})
        print(json.dumps(results[-1]),flush=True)
    # The focused wrapper must still fail when its child exits zero without the
    # exact required pass marker. Cleanup robustness is not marker acceptance.
    name='focused-missing-pass-marker';log=out/(name+'.log');q=baseline.quote
    script=out/(name+'.ps1')
    script.write_text('& '+q(root/'Tools/CI/InvokeUnityExecuteMethodValidation.ps1')+' -UnityExe '+q(executable)+
        ' -ProjectPath '+q(out)+' -ExecuteMethod OwnedSyntheticValidation -LogFile '+q(log)+
        ' -RequiredPassMarker '+q('[OwnedSyntheticRequired] result=Passed')+' -GuiLicensing -TimeoutSeconds 10\n',encoding='utf-8-sig')
    process=subprocess.run(['rtk','proxy','powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-File',str(script)],cwd=root,capture_output=True,text=True,timeout=25)
    console=process.stdout+process.stderr;(out/(name+'.console.log')).write_text(console,encoding='utf-8')
    text=log.read_text(encoding='utf-8-sig',errors='replace')
    passed=process.returncode!=0 and 'missing required pass marker' in console and '[UnityInvoke] ExitCode: 0' in text and '[ArchitectureValidation] method=OwnedSyntheticValidation result=Passed' not in console
    results.append({'case':name,'passed':passed,'actual_exit':process.returncode,'expected_exit':1});print(json.dumps(results[-1]),flush=True)
    record={'result':'PassedOwnedCleanupStderrRegression' if all(x['passed'] for x in results) else 'FailedOwnedCleanupStderrRegression',
        'cases':results,'output_directory':str(out),'unity_launched':False,'assets_changed':False,
        'scope':'Actual InvokeUnity.ps1; synthetic stderr/status shim acts only on exact wrapper-owned child PID; fallback uses cached Process handle.'}
    (out/'result.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8');print(json.dumps(record,indent=2))
    return 0 if all(x['passed'] for x in results) else 1

if __name__=='__main__':raise SystemExit(main())
