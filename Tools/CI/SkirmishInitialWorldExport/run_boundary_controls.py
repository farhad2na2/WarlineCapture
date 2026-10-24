#!/usr/bin/env python3
"""Run only actual source-extracted initial-world observer counter controls. No Unity execution."""
import argparse,hashlib,json,os,re,shutil,subprocess,sys
from pathlib import Path
HERE=Path(__file__).resolve().parent
SOURCE="Assets/Game/Scripts/Runtime/Skirmish/S002InitialWorldBoundaryWitness.cs"
def digest(path):
    h=hashlib.sha256()
    with Path(path).open("rb") as f:
        while True:
            block=f.read(65536)
            if not block:break
            h.update(block)
    return h.hexdigest()
def need(ok,message):
    if not ok:raise RuntimeError(message)
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--source-root",type=Path,default=HERE.parents[2]);p.add_argument("--output",type=Path,required=True);p.add_argument("--dotnet",default="dotnet");p.add_argument("--core-module",type=Path,help="Optional installed UnityEngine.CoreModule DLL metadata check; file is never loaded/executed")
    a=p.parse_args();root=a.source_root.resolve();out=a.output.resolve();need(not a.output.exists() and not a.output.is_symlink(),"New output required");need(out!=root and root not in out.parents,"Output must be outside source tree")
    source=root/SOURCE;control=HERE/"BoundaryCounterControls.cs";budget=root/"Assets/Game/Scripts/Runtime/Skirmish/S002InitialWorldPrivateCopyBudget.cs";budgetControl=HERE/"PrivateCopyBudgetControls.cs";before={str(f):digest(f) for f in [source,control,budget,budgetControl,Path(__file__)]};text=source.read_text();start=text.index("    internal struct S002InitialWorldBoundaryCounter");end=text.index("\n    public static class S002InitialWorldBoundaryWitness",start);counter=text[start:end]
    out.mkdir(parents=True);receipt={"result":"Failed","scope":"Actual source-extracted primitive observer counter only; no game simulator or native hooks executed","consumedSourceSha256":before[str(source)],"consumedCounterSha256":hashlib.sha256(counter.encode()).hexdigest(),"inputsBefore":before,"native":"UNRUN","BurstILPP":"UNRUN","owningEditorCompile":"UNRUN","initialWorldBundle":"UNRUN"}
    try:
        dotnet=shutil.which(a.dotnet);need(dotnet is not None,"Installed SDK8+ required; no install/download")
        env={**os.environ,"DOTNET_SKIP_FIRST_TIME_EXPERIENCE":"1","DOTNET_CLI_TELEMETRY_OPTOUT":"1","DOTNET_GENERATE_ASPNET_CERTIFICATE":"false","DOTNET_CLI_HOME":str(out/"sdk-home")}
        def run(label,args,timeout):
            receipt[label+"Command"]=args
            try:r=subprocess.run(args,cwd=out,env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=timeout)
            except subprocess.TimeoutExpired as e:
                (out/(label+".stdout.log")).write_bytes(e.stdout or b"");(out/(label+".stderr.log")).write_bytes(e.stderr or b"");raise RuntimeError(label+" timed out; partial logs preserved") from e
            (out/(label+".stdout.log")).write_bytes(r.stdout);(out/(label+".stderr.log")).write_bytes(r.stderr);receipt[label+"Exit"]=r.returncode;need(r.returncode==0,label+" failed; inspect full logs");return r.stdout.decode()
        version=run("sdk",[dotnet,"--version"],30).strip();match=re.fullmatch(r"(\d+)\.\d+\.[0-9A-Za-z.+-]+",version);need(match is not None and int(match.group(1))>=8,"Installed SDK8+ required");framework="net"+match.group(1)+".0";receipt["sdkVersion"]=version;receipt["targetFramework"]=framework;receipt["dotnetSha256"]=digest(Path(dotnet).resolve())
        (out/"Counter.cs").write_text("namespace Game.Runtime{"+counter+"}\n");(out/"Controls.cs").write_bytes(control.read_bytes());(out/"Budget.cs").write_bytes(budget.read_bytes());(out/"BudgetControls.cs").write_bytes(budgetControl.read_bytes());(out/"Checks.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>'+framework+'</TargetFramework><OutputType>Exe</OutputType><DefineConstants>UNITY_EDITOR</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Counter.cs"/><Compile Include="Controls.cs"/><Compile Include="Budget.cs"/><Compile Include="BudgetControls.cs"/></ItemGroup></Project>');(out/"NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>')
        run("compile",[dotnet,"build",str(out/"Checks.csproj"),"--nologo","--configfile",str(out/"NuGet.Config")],120)
        command=[dotnet,str(out/("bin/Debug/"+framework+"/Checks.dll"))]
        if a.core_module is not None:
            module=a.core_module.resolve();receipt["metadataAssemblySha256"]=digest(module);command.append(str(module))
        stdout=run("controls",command,30);need("[InitialWorldPrivateCounter] result=Passed" in stdout,"Missing control pass marker");receipt["checks"]=int(re.search(r"\[InitialWorldPrivateCounter\] result=Passed checks=(\d+)",stdout).group(1));receipt["budgetChecks"]=int(re.search(r"\[InitialWorldPrivateCopyBudget\] result=Passed checks=(\d+)",stdout).group(1));receipt["result"]="Passed";print(stdout,end="")
        if a.core_module is not None:need(digest(module)==receipt["metadataAssemblySha256"],"Metadata input changed")
    except Exception as error:receipt["result"]="Failed";receipt["error"]=str(error);raise
    finally:
        receipt["inputsAfter"]={f:digest(Path(f)) for f in before}
        if receipt["inputsAfter"]!=before:receipt["result"]="Failed";receipt["inputDrift"]=True
        (out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
    need(receipt["result"]=="Passed","Input drift during controls")
if __name__=="__main__":
    try:main()
    except Exception as e:print("[InitialWorldBoundaryControls] result=Failed "+str(e),file=sys.stderr);sys.exit(2)
