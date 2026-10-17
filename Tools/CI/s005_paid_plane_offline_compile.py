"""Type-compile the new paid-Plane partial against real Game.Editor sources to Temp only."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile


def main():
    project=Path(__file__).resolve().parents[2]
    draft=project/"Design/AgentReports/SkirmishExpansion/S005/windows-paid-plane-canary/NormalPaidPlaneLineageCanary.cs.txt"
    responses=list((project/"Library/Bee").rglob("Game.Editor.rsp"))
    if not responses: raise RuntimeError("Current real Game.Editor response file absent")
    response=max(responses,key=lambda p:p.stat().st_mtime_ns)
    parent=project/"Temp/S005PaidPlaneOfflineCompile"
    parent.mkdir(parents=True,exist_ok=True)
    output=Path(tempfile.mkdtemp(prefix="compile-",dir=parent))
    candidate=output/"NormalPaidPlaneLineageCanary.cs"
    candidate.write_bytes(draft.read_bytes())
    lines=[]
    omitted=0
    for line in response.read_text(encoding="utf-8-sig").splitlines():
        if line.lower().startswith(("-out:","/out:","-refout:","/refout:")): continue
        if line.lower().startswith(("-analyzer:","/analyzer:")):
            omitted+=1
            continue
        lines.append(line)
    lines+=['"'+candidate.as_posix()+'"','-out:"'+(output/"Game.Editor.dll").as_posix()+'"',
            '-refout:"'+(output/"Game.Editor.ref.dll").as_posix()+'"']
    rsp=output/"paid-plane-Game.Editor.rsp"
    rsp.write_text("\n".join(lines)+"\n",encoding="utf-8")
    dotnet=shutil.which("dotnet")
    sdk=subprocess.run(["rtk","proxy",dotnet,"--list-sdks"],capture_output=True,text=True,check=True).stdout.splitlines()[0]
    version,location=sdk.split(" ",1)
    compiler=Path(location.strip("[]"))/version/"Roslyn/bincore/csc.dll"
    result=subprocess.run(["rtk","proxy",dotnet,str(compiler),"@"+str(rsp)],cwd=project,capture_output=True,text=True)
    (output/"compiler.log").write_text(result.stdout+result.stderr,encoding="utf-8")
    record=dict(result="PassedOfflineTypeCompile" if result.returncode==0 else "FailedOfflineTypeCompile",
                exit_code=result.returncode,response_source=str(response),output_directory=str(output),
                candidate_sha256=hashlib.sha256(candidate.read_bytes()).hexdigest(),
                current_harness_sources_preserved=True,diagnostic_analyzers_omitted=omitted,
                native_execution=False,native_qualification="pending_generators_ILPP_loaded_domain_and_real_canary")
    (output/"result.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(record,indent=2))
    if result.returncode: print("\n".join(line for line in (result.stdout+result.stderr).splitlines() if "error " in line)[:6000])
    return result.returncode


if __name__=="__main__":raise SystemExit(main())
