"""Run the actual Windows wrapper against a tiny owned test EXE, never Unity."""
import json
from pathlib import Path
import os
import subprocess
import tempfile

CHILD = r'''
using System;
using System.IO;
using System.Threading;
class ExitProbe {
 static string Arg(string[] args,string key,string fallback) {
   for(int i=0;i<args.Length-1;i++)if(args[i]==key)return args[i+1];
   return fallback;
 }
 static int Main(string[] args) {
   string log=Arg(args,"-logFile","");
   if(Arg(args,"--fatal","0")=="1")File.AppendAllText(log,"\nCrash!!! owned synthetic child only\n");
   Thread.Sleep(int.Parse(Arg(args,"--sleep","0")));
   return int.Parse(Arg(args,"--exit","0"));
 }
}
'''


def quote(value): return "'"+str(value).replace("'","''")+"'"


def main():
    project=Path(__file__).resolve().parents[2]
    parent=project/"Temp/InvokeUnityProcessExitTests"
    parent.mkdir(parents=True,exist_ok=True)
    output=Path(tempfile.mkdtemp(prefix="canary-",dir=parent))
    source=output/"OwnedExitProbe.cs"
    source.write_text(CHILD,encoding="utf-8")
    executable=output/"OwnedExitProbe.exe"
    compiler=Path(os.environ["WINDIR"])/"Microsoft.NET/Framework64/v4.0.30319/csc.exe"
    build=subprocess.run(["rtk","proxy",str(compiler),"/nologo","/target:exe","/out:"+str(executable),str(source)],
                         capture_output=True,text=True)
    (output/"compiler.log").write_text(build.stdout+build.stderr,encoding="utf-8")
    if build.returncode: raise RuntimeError(build.stdout+build.stderr)
    cases=[("fast-zero",0,0,0,10,0,False),("fast-nonzero",37,0,0,10,37,False),
           ("refresh-zero",0,1250,0,10,0,False),("refresh-nonzero",37,1250,0,10,37,False),
           ("no-process-exit",37,1250,0,10,37,True),("timeout",0,15000,0,1,124,False),
           ("fatal-before-timeout",0,15000,1,1,1,False),("fatal-over-zero",0,0,1,10,1,False)]
    results=[]
    for name,code,sleep,fatal,timeout,expected,no_exit in cases:
        log=output/(name+".log")
        command="& "+quote(project/"Tools/CI/InvokeUnity.ps1")+" -UnityExe "+quote(executable)+" -ProjectPath "+quote(output)+\
                " -LogFile "+quote(log)+" -GuiLicensing -TimeoutSeconds "+str(timeout)+\
                " -UnityArguments @('--exit',"+quote(code)+",'--sleep',"+quote(sleep)+",'--fatal',"+quote(fatal)+")"
        if no_exit:command+=" -NoProcessExit"
        command+="\nexit $global:LASTEXITCODE"
        script=output/(name+".ps1")
        script.write_text(command+"\n",encoding="utf-8-sig")
        process=subprocess.run(["rtk","proxy","powershell.exe","-NoProfile","-ExecutionPolicy","Bypass","-File",str(script)],
                               cwd=project,capture_output=True,text=True,timeout=30)
        (output/(name+".console.log")).write_text(process.stdout+process.stderr,encoding="utf-8")
        text=log.read_text(encoding="utf-8-sig",errors="replace") if log.exists() else ""
        passed=process.returncode==expected and "[UnityInvoke] ExitCode: "+str(expected) in text and \
               "accepting explicit success marker" not in text and "without a readable process exit code" not in text
        results.append(dict(case=name,expected=expected,actual=process.returncode,passed=passed,log=log.name))
        print(json.dumps(results[-1]),flush=True)
    record=dict(result="PassedOwnedProcessExitRegression" if all(r["passed"] for r in results) else "FailedOwnedProcessExitRegression",
                cases=results,output_directory=str(output),unity_launched=False,assets_changed=False,
                scope="Actual InvokeUnity.ps1 and Windows PowerShell5.1 with freshly compiled owned synthetic child EXE; no log-only success acceptance")
    (output/"result.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(record,indent=2))
    return 0 if all(r["passed"] for r in results) else 1


if __name__=="__main__":raise SystemExit(main())
