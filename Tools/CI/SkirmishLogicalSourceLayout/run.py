#!/usr/bin/env python3
"""Predicate-only source proposal checks. Never invokes Unity or copies installed DLLs."""
import argparse, hashlib, json, os, re, shutil, stat, subprocess, sys
from pathlib import Path
from xml.sax.saxutils import escape, quoteattr
HERE = Path(__file__).resolve().parent
CFG = "Assets/Game/Scripts/Configs/Skirmish/"
ED = "Assets/Game/Scripts/Editor/Skirmish/"
CON = "Assets/Game/Scripts/Skirmish/Contracts/"
NEW = ED + "SkirmishGroundSourceFitSurvey.LogicalSourceLayoutAuthoring.cs"
CONTROL = ED + "SkirmishGroundSourceFitSurvey.LogicalSourceLayoutAuthoringControls.cs"
BUILDER = ED + "SkirmishConfigOnlyBatchBuilder.cs"
AUTHORITY = "Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json"
CITY = "Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/"
SOURCES = [CFG+n for n in ["SkirmishMapLayoutConfig.cs", "SkirmishScenarioDefinitionConfig.cs", "SkirmishObjectiveConfig.cs", "SkirmishStaticAuthoringBudget.cs", "SkirmishMeasuredLayoutBinder.cs", "SkirmishMapLayoutBuilder.cs", "SkirmishResolvedSetup.cs", "SkirmishMissionLogicalBindingConfig.cs", "SkirmishMissionDispatch.cs"]] + [CON+n for n in ["SkirmishScenarioContracts.cs", "SkirmishObjectiveContracts.cs", "SkirmishReasonCodes.cs", "SkirmishRosterContracts.cs"]] + [ED+"SkirmishGroundSourceFitSurvey."+n+".cs" for n in ["GroundBa10Authoring", "GroundObjective30Authoring", "GroundObjective30", "AirCombined80Authoring"]]

def sha(raw): return hashlib.sha256(raw).hexdigest()
def need(ok, why):
    if not ok: raise RuntimeError(why)
FILE_LIMIT = 128 * 1024 * 1024

def read_regular(path, limit=FILE_LIMIT):
    path=Path(path);initial=path.lstat()
    need(stat.S_ISREG(initial.st_mode) and 0 <= initial.st_size <= limit,"Bounded regular file required: "+str(path))
    descriptor=os.open(path,os.O_RDONLY|getattr(os,"O_NONBLOCK",0)|getattr(os,"O_NOFOLLOW",0))
    try:
        opened=os.fstat(descriptor)
        need(stat.S_ISREG(opened.st_mode) and (opened.st_dev,opened.st_ino)==(initial.st_dev,initial.st_ino) and opened.st_size<=limit,"Input changed before read: "+str(path))
        raw=bytearray()
        while len(raw)<=limit:
            chunk=os.read(descriptor,min(1024*1024,limit+1-len(raw)))
            if not chunk:break
            raw.extend(chunk)
        need(len(raw)<=limit,"File exceeded read bound: "+str(path))
        final=os.fstat(descriptor)
        need((final.st_dev,final.st_ino,final.st_size,final.st_mtime_ns)==(opened.st_dev,opened.st_ino,opened.st_size,opened.st_mtime_ns),"Input changed during read: "+str(path))
    finally:os.close(descriptor)
    after=path.lstat()
    need((after.st_dev,after.st_ino,after.st_size,after.st_mtime_ns)==(initial.st_dev,initial.st_ino,initial.st_size,initial.st_mtime_ns),"Input replaced during read: "+str(path))
    return bytes(raw)

def read_git(repo, base, relative):
    object_name=base+":"+relative
    command=["git","-c","maintenance.auto=false","-c","gc.auto=0"]
    size=int(subprocess.check_output(command+["cat-file","-s",object_name],cwd=repo,timeout=30))
    need(0 <= size <= FILE_LIMIT,"Git authority blob exceeds source bound: "+relative)
    raw=subprocess.check_output(command+["show",object_name],cwd=repo,timeout=30)
    need(len(raw)==size,"Git authority blob size changed: "+relative)
    return raw

def block(text, marker):
    """Bounded extraction of a pinned C# member, retaining its original body."""
    need(marker in text,"Missing pinned source member: "+marker)
    start = text.index(marker)
    brace, arrow = text.find("{",start), text.find("=>",start)
    if arrow >= 0 and (brace < 0 or arrow < brace): return text[start:text.index(";",arrow)+1]
    semi = text.find(";",start)
    if semi >= 0 and semi < min(x for x in (brace,arrow) if x >= 0): return text[start:semi+1]
    depth = 0
    # These exact pinned members have no unmatched braces inside string literals.
    for index in range(brace, len(text)):
        if text[index] == "{": depth += 1
        if text[index] == "}":
            depth -= 1
            if depth == 0:
                end = index+1
                if text[end:end+1] == ";": end += 1
                return text[start:end]
    raise RuntimeError("Unterminated pinned member: " + marker)

def generate(text, work):
    declarations = []
    def parts(path, markers):
        result=[]
        for marker in markers:
            value=block(text[path],marker);result.append(value)
            declarations.append({"path":path,"marker":marker,"sha256":sha(value.encode())})
        return "\n".join(result)
    def put(name,value): (work/name).write_text(value,encoding="utf-8")
    for p in SOURCES[:4]+[CON+n for n in ["SkirmishScenarioContracts.cs","SkirmishObjectiveContracts.cs","SkirmishReasonCodes.cs"]]+[ED+"SkirmishGroundSourceFitSurvey.GroundBa10Authoring.cs"]:
        put(Path(p).name,text[p])
    imports="using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using System.Text;using Game.Configs;using Game.Skirmish.Contracts;using UnityEngine;using UnityEditor;using Newtonsoft.Json.Linq;"
    owner=parts(ED+"SkirmishGroundSourceFitSurvey.GroundObjective30Authoring.cs",["public sealed class GroundObjective30CandidateOwner", "public const string GroundObjective30Inputs=", "public const string GroundObjective30InputsSha="])
    clone=parts(ED+"SkirmishGroundSourceFitSurvey.AirCombined80Authoring.cs",["static SkirmishMapLayoutConfig CloneAirCombined80AdaptedLayout"])
    # Legacy FC methods compile but are never called by the explicit-source path;
    # their required legacy input/survey files are deliberately absent in this test.
    ground=parts(ED+"SkirmishGroundSourceFitSurvey.GroundObjective30.cs",["static readonly string[] Ground30Ids", "static JObject ReadGround30Envelope", "static void ValidateGround30Envelope"])
    put("ExactEditorHelpers.cs",imports+"namespace Game.Editor{public static partial class SkirmishGroundSourceFitSurvey{"+owner+clone+ground+"}}")
    admission=parts(BUILDER,["private static readonly HashSet<string> CampaignSources", "public sealed class Request", "internal static void ValidateCampaignSource", "private static void ValidateGeometryDeclaration", "public static string HashFile", "private static bool Inside", "private static bool Finite", "private static bool Persistent", "internal static void ValidateLogicalGeometryDeclaration"])
    put("ExactBatchSourceHelpers.cs",imports+"namespace Game.Editor{public static class SkirmishConfigOnlyBatchBuilder{"+admission+"}}")
    measured=parts(CFG+"SkirmishMeasuredLayoutBinder.cs",["public static string ComputeLayoutSemanticFingerprint", "private sealed class LayoutValues", "[Serializable] private sealed class CompoundValues", "[Serializable] private sealed class FacilityPlacementValues"])
    binding=parts(CFG+"SkirmishMissionLogicalBindingConfig.cs",["public static string SourceFingerprint", "public static bool Hex", "private static string Hash", "[Serializable] private sealed class Source"])
    put("ExactFingerprints.cs",imports+"namespace Game.Configs{public static class SkirmishMeasuredLayoutBinder{"+measured+"}public static class SkirmishMissionLogicalBindingCompiler{"+binding+"}}")
    pad=parts(CFG+"SkirmishMapLayoutBuilder.cs",["public static bool TryPadForStructure", "public static SkirmishLegalPadKind PadKindForStructure", "public static void ApplyFrameOffset"])
    ids=parts(CON+"SkirmishRosterContracts.cs",["public static class SkirmishStructureIds", "public enum SkirmishProducerKind"])
    structure=parts(CFG+"SkirmishResolvedSetup.cs",["public struct SkirmishResolvedStructureEntry"])
    dispatch=parts(CFG+"SkirmishMissionDispatch.cs",["public static bool IsExpandedCounterpart"])
    put("ExactPadResolver.cs",imports+"namespace Game.Configs{public static class SkirmishMapLayoutBuilder{"+pad+"public static void ApplyDesertBaseAssault(SkirmishMapLayoutConfig l)=>throw new InvalidOperationException(\"Unexecuted legacy configure API\");}"+structure+"public static class SkirmishMissionDispatch{"+dispatch+"}}namespace Game.Skirmish.Contracts{"+ids+"}")
    return declarations

def query_sdk_version(dotnet, env, out):
    try:version=subprocess.run([dotnet,"--version"],stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env,cwd=out,timeout=30)
    except subprocess.TimeoutExpired as ex:
        (out/"sdk-version.stdout.log").write_bytes(ex.stdout or b"");(out/"sdk-version.stderr.log").write_bytes(ex.stderr or b"")
        raise RuntimeError("Installed SDK version query timed out; partial logs preserved") from ex
    (out/"sdk-version.stdout.log").write_bytes(version.stdout);(out/"sdk-version.stderr.log").write_bytes(version.stderr)
    need(version.returncode==0,"Installed SDK version query failed")
    sdk_version=version.stdout.decode().strip();match=re.fullmatch(r"(\d+)\.\d+\.[0-9A-Za-z.+-]+",sdk_version)
    need(match is not None and int(match.group(1))>=8,"Installed SDK8+ required; no download or framework fallback")
    framework="net"+match.group(1)+".0"
    return sdk_version,framework

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--authority-root",type=Path,default=HERE.parents[2])
    parser.add_argument("--source-root",type=Path,help="Candidate tree; defaults to authority root. Never applies candidate files.")
    parser.add_argument("--output",type=Path,required=True,help="New directory outside authority/candidate trees")
    parser.add_argument("--dotnet",default="dotnet",help="Installed SDK command (8 or newer); matching installed packs required")
    parser.add_argument("--newtonsoft",type=Path,help="Installed Newtonsoft.Json DLL; defaults to matching project package cache")
    args=parser.parse_args();repo=args.authority_root.resolve();candidate=(args.source_root or repo).resolve();out=args.output.resolve()
    need(not args.output.exists() and not args.output.is_symlink(),"Output must be new")
    for root in (repo,candidate): need(out!=root and root not in out.parents,"Output must be outside source trees")
    pins=json.loads(read_regular(HERE/"source-pins.json"));raw={};before={}
    for relative, expected in pins["authority"].items():
        path=repo/relative
        value=read_git(repo,pins["base"],relative)
        need(sha(value)==expected,"Exact-base authority pin changed: "+relative);raw[relative]=value
        before[str(path)]=sha(read_regular(path))
    for relative, expected in pins["candidate"].items():
        path=candidate/relative;value=read_regular(path);need(sha(value)==expected,"Candidate pin changed: "+relative);raw[relative]=value;before[str(path)]=expected
    for name, expected in pins["test_inputs"].items():
        value=read_regular(HERE/name);need(sha(value)==expected,"Test input changed: "+name);before[str(HERE/name)]=expected
    newtonsoft=args.newtonsoft
    if newtonsoft is None:
        choices=sorted((repo/"Library/PackageCache").glob("com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll"))
        matches=[p for p in choices if sha(read_regular(p))==pins["newtonsoft_sha256"]]
        need(bool(matches),"No pinned installed Newtonsoft.Json DLL; pass --newtonsoft (no download/copy)");newtonsoft=matches[0]
    newtonsoft=newtonsoft.resolve();need(sha(read_regular(newtonsoft))==pins["newtonsoft_sha256"],"Newtonsoft reference pin changed")
    dotnet=shutil.which(args.dotnet);need(dotnet is not None,"Installed .NET SDK required")
    out.mkdir(parents=True);receipt={"result":"Failed","scope":"predicate-only; inert Editor APIs and supplied diagnostic budget output", "owningEditorCompile":"UNRUN","actualThreeSizeBudgetCompiler":"UNRUN","native":"UNRUN","inputsBefore":before,"exactBaseAuthority":pins["authority"],"authorityBase":pins["base"],"newtonsoft":{"path":str(newtonsoft),"sha256":pins["newtonsoft_sha256"]},"dotnet":{"path":dotnet,"sha256":sha(read_regular(Path(dotnet).resolve()))}}
    try:
        env={**os.environ,"DOTNET_SKIP_FIRST_TIME_EXPERIENCE":"1","DOTNET_CLI_TELEMETRY_OPTOUT":"1","DOTNET_GENERATE_ASPNET_CERTIFICATE":"false","DOTNET_CLI_HOME":str(out/"sdk-home")}
        sdk_version,framework=query_sdk_version(dotnet,env,out)
        receipt["sdkVersion"]=sdk_version;receipt["targetFramework"]=framework
        work=out/"compile";project=out/"test-project";work.mkdir();project.mkdir()
        text={p:v.decode("utf-8-sig") for p,v in raw.items() if p.endswith(".cs")}
        receipt["extractedMembers"]=generate(text,work)
        for name in ["ApiDoubles.cs","LogicalLayoutChecks.cs","PortableLauncher.cs"]:(work/name).write_bytes(read_regular(HERE/name))
        for relative in [AUTHORITY]+[CITY+n for n in ["Definition.asset","Grid.asset","Surface.asset"]]:
            p=project/relative;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw[relative])
        (project/"full-layout.json").write_bytes(read_regular(HERE/"full-layout.fixture.json"))
        files=sorted(work.glob("*.cs"))+[candidate/NEW,candidate/CONTROL]
        xml='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>'+framework+'</TargetFramework><OutputType>Exe</OutputType><StartupObject>PortableLauncher</StartupObject><DefineConstants>UNITY_EDITOR</DefineConstants><EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup>'+''.join('<Compile Include='+quoteattr(str(p))+' />' for p in files)+'<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newtonsoft))+'</HintPath><Private>false</Private></Reference></ItemGroup></Project>'
        (work/"Portable.csproj").write_text(xml);(work/"NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
        env={**os.environ,"DOTNET_SKIP_FIRST_TIME_EXPERIENCE":"1","DOTNET_CLI_TELEMETRY_OPTOUT":"1","DOTNET_GENERATE_ASPNET_CERTIFICATE":"false","DOTNET_CLI_HOME":str(out/"sdk-home")}
        def invoke(label,command,cwd):
            receipt[label+"Command"]=command
            try: p=subprocess.run(command,cwd=cwd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env,timeout=120)
            except subprocess.TimeoutExpired as ex:
                (out/(label+".stdout.log")).write_bytes(ex.stdout or b"");(out/(label+".stderr.log")).write_bytes(ex.stderr or b"");raise RuntimeError(label+" timeout") from ex
            (out/(label+".stdout.log")).write_bytes(p.stdout);(out/(label+".stderr.log")).write_bytes(p.stderr);receipt[label+"Exit"]=p.returncode
            need(p.returncode==0,label+" failed; inspect full logs");return p.stdout.decode(errors="replace")
        invoke("compile",[dotnet,"build",str(work/"Portable.csproj"),"--nologo","--configfile",str(work/"NuGet.Config")],work)
        stdout=invoke("controls",[dotnet,str(work/("bin/Debug/"+framework+"/Portable.dll")),str(newtonsoft)],project)
        need("[LogicalSourceLayoutPortable] result=Passed" in stdout,"Missing control pass marker")
        need(not list(out.rglob("Newtonsoft.Json.dll")),"Installed reference was unexpectedly copied")
        receipt["result"]="Passed";print(stdout,end="")
    except Exception as ex:
        receipt["error"]=str(ex);raise
    finally:
        try:
            receipt["exactBaseAuthorityAfter"]={p:sha(read_git(repo,pins["base"],p)) for p in pins["authority"]}
            receipt["inputsAfter"]={p:sha(read_regular(Path(p))) for p in before}
            if receipt["inputsAfter"]!=before or receipt["exactBaseAuthorityAfter"]!=pins["authority"]:receipt["result"]="Failed";receipt["inputDrift"]=True
            receipt["installedReferenceAfter"]=sha(read_regular(newtonsoft))
        except Exception as error:
            receipt["result"]="Failed";receipt["postCheckError"]=str(error)
        (out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
    need(receipt["result"]=="Passed" and receipt.get("installedReferenceAfter")==pins["newtonsoft_sha256"],"Post-run pin drift")

if __name__ == "__main__":
    try:main()
    except Exception as error:print("[LogicalSourceLayoutPortableRunner] result=Failed "+str(error),file=sys.stderr);sys.exit(2)
