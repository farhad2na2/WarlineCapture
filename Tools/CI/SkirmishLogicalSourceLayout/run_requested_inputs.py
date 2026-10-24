#!/usr/bin/env python3
"""Focused actual requested-input helper checks; reuses the pinned existing portable harness."""
import argparse, hashlib, importlib.util, json, os, shutil, subprocess, sys
from pathlib import Path
from xml.sax.saxutils import escape,quoteattr
BASE="8504f48215bdeca1a8a3010576bb21d8f446f38a"
HERE=Path(__file__).resolve().parent
TOOL="Tools/CI/SkirmishLogicalSourceLayout/"
HELPER="Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.RequestedLogicalSourceLayouts.cs"
def sha(value):return hashlib.sha256(value).hexdigest()
def need(ok,text):
    if not ok:raise RuntimeError(text)
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--authority-root",type=Path,default=HERE.parents[2]);p.add_argument("--source-root",type=Path);p.add_argument("--output",type=Path,required=True);p.add_argument("--dotnet",default="dotnet");p.add_argument("--newtonsoft",type=Path);a=p.parse_args()
    repo=a.authority_root.resolve();candidate=(a.source_root or repo).resolve();out=a.output.resolve()
    need(not a.output.exists() and not a.output.is_symlink(),"New output directory required")
    need(all(out!=r and r not in out.parents for r in [repo,candidate]),"Output must be outside source trees")
    out.mkdir(parents=True);receipt={"result":"Failed","scope":"Actual pure helper predicates only; inert Editor API and diagnostic budget doubles", "native":"UNRUN","owningEditorCompile":"UNRUN","actualBudgetCompiler":"UNRUN"}
    def git(relative,base=BASE):
        command=["git","-c","maintenance.auto=false","-c","gc.auto=0"]
        size=int(subprocess.check_output(command+["cat-file","-s",base+":"+relative],cwd=repo,timeout=30));need(size<=128*1024*1024,"Bounded public source required")
        raw=subprocess.check_output(command+["show",base+":"+relative],cwd=repo,timeout=30);need(len(raw)==size,"Git input size changed");return raw
    inputs={};source=candidate/HELPER;control=HERE/"RequestedInputChecks.cs"
    try:
        bootstrap=out/"bootstrap";bootstrap.mkdir();(bootstrap/"run.py").write_bytes(git(TOOL+"run.py"));spec=importlib.util.spec_from_file_location("existing",bootstrap/"run.py");b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
        pins=json.loads(git(TOOL+"source-pins.json"));raw={}
        for path,expected in {**pins["authority"],**pins["candidate"]}.items():
            value=git(path);need(sha(value)==expected,"Pinned8504 authority/helper changed: "+path);raw[path]=value;inputs[path]=sha(value)
        work=out/"compile";project=out/"test-project";work.mkdir();project.mkdir()
        receipt["extractedMembers"]=b.generate({p:r.decode("utf-8-sig") for p,r in raw.items() if p.endswith(".cs")},work)
        for name in ["ApiDoubles.cs","LogicalLayoutChecks.cs","PortableLauncher.cs"]:
            value=git(TOOL+name);need(sha(value)==pins["test_inputs"][name],"Harness pin changed");
            if name=="PortableLauncher.cs":value=value.replace(b"typeof(LogicalLayoutChecks)",b"typeof(RequestedInputChecks)")
            (work/name).write_bytes(value)
        for path in [b.AUTHORITY]+[b.CITY+n for n in ["Definition.asset","Grid.asset","Surface.asset"]]:
            f=project/path;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(raw[path])
        fixture=git(TOOL+"full-layout.fixture.json");need(sha(fixture)==pins["test_inputs"]["full-layout.fixture.json"],"Fixture pin changed");(project/"full-layout.json").write_bytes(fixture)
        with (project/"oversize.input").open("wb") as f:f.truncate(4*1024*1024+1)
        if os.name=="posix":os.mkfifo(project/"fifo.input")
        before={str(p):sha(b.read_regular(p)) for p in [source,control,Path(__file__)]};receipt["candidateBefore"]=before;receipt["baseSourcePins"]=inputs
        (work/"RequestedInputChecks.cs").write_bytes(b.read_regular(control))
        for path in [b.NEW,b.CONTROL]:(work/Path(path).name).write_bytes(raw[path])
        newt=a.newtonsoft
        if newt is None:
            matches=[p for p in sorted((repo/"Library/PackageCache").glob("com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll")) if sha(b.read_regular(p))==pins["newtonsoft_sha256"]];need(bool(matches),"Pinned installed Newtonsoft reference required");newt=matches[0]
        newt=newt.resolve();need(sha(b.read_regular(newt))==pins["newtonsoft_sha256"],"Installed reference pin changed")
        dotnet=shutil.which(a.dotnet);need(dotnet is not None,"Installed .NET8+ SDK required")
        env={**os.environ,"DOTNET_SKIP_FIRST_TIME_EXPERIENCE":"1","DOTNET_CLI_TELEMETRY_OPTOUT":"1","DOTNET_GENERATE_ASPNET_CERTIFICATE":"false","DOTNET_CLI_HOME":str(out/"sdk-home")}
        version,framework=b.query_sdk_version(dotnet,env,out);receipt["sdkVersion"]=version;receipt["targetFramework"]=framework;receipt["referenceSha256"]=pins["newtonsoft_sha256"]
        files=sorted(work.glob("*.cs"))+[source]
        xml='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>'+framework+'</TargetFramework><OutputType>Exe</OutputType><StartupObject>PortableLauncher</StartupObject><DefineConstants>UNITY_EDITOR</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup>'+''.join('<Compile Include='+quoteattr(str(f))+' />' for f in files)+'<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newt))+'</HintPath><Private>false</Private></Reference></ItemGroup></Project>'
        (work/"Checks.csproj").write_text(xml);(work/"NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
        def run(label,args,cwd):
            receipt[label+"Command"]=args
            try:r=subprocess.run(args,cwd=cwd,env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=120 if label=="compile" else 30)
            except subprocess.TimeoutExpired as e:
                (out/(label+".stdout.log")).write_bytes(e.stdout or b"");(out/(label+".stderr.log")).write_bytes(e.stderr or b"");raise RuntimeError(label+" timeout; partial output preserved") from e
            (out/(label+".stdout.log")).write_bytes(r.stdout);(out/(label+".stderr.log")).write_bytes(r.stderr);receipt[label+"Exit"]=r.returncode;need(r.returncode==0,label+" failed; inspect full logs");return r.stdout.decode()
        run("compile",[dotnet,"build",str(work/"Checks.csproj"),"--nologo","--configfile",str(work/"NuGet.Config")],work)
        text=run("controls",[dotnet,str(work/("bin/Debug/"+framework+"/Checks.dll")),str(newt)],project);need("[RequestedLogicalInputPortable] result=Passed" in text,"Missing focused pass marker")
        need(not list(out.rglob("Newtonsoft.Json.dll")),"Installed reference copied unexpectedly")
        receipt["candidateAfter"]={p:sha(b.read_regular(Path(p))) for p in before};need(receipt["candidateAfter"]==before,"Candidate changed during control run");need(sha(b.read_regular(newt))==pins["newtonsoft_sha256"],"Reference changed");receipt["result"]="Passed";print(text,end="")
    except Exception as error:receipt["error"]=str(error);raise
    finally:(out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
if __name__=="__main__":
    try:main()
    except Exception as e:print("[RequestedInputRunner] result=Failed "+str(e),file=sys.stderr);sys.exit(2)
