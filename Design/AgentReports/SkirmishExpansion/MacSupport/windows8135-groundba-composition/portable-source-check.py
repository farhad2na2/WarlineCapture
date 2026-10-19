import argparse,subprocess,json,hashlib,re,tempfile
from pathlib import Path
BASE="f90b6bfed2e3ecf2d630d5eede839777812a1bf5"
MAC="923ed3cd5baf44251b65684c8e7a1c302db1305b"
WIN="8135ce92385620025b39a0cd7f284bb0f1a88db0"
PREFIX="Assets/Game/Scripts/Editor/Skirmish/"
def clean(s):
 pat=r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''
 return re.sub(pat,lambda m:"".join("\n" if c=="\n" else " " for c in m.group()),s)

def method(s,name):
 c=clean(s);m=re.search(r'(?m)^[ \t]*(?:(?:public|private|internal|protected)\s+)?static\s+[\w<>,.\[\]?\s]+?\s+'+name+r'\s*\(',c);assert m,name
 start=c.find("{",m.end());depth=0
 for i in range(start,len(c)):
  if c[i]=="{":depth+=1
  elif c[i]=="}":
   depth-=1
   if depth==0:return s[m.start():i+1]
 raise AssertionError("Unbalanced method")

def balanced(s):
 stack=[];pairs={")":"(","]":"[","}":"{"}
 for x in clean(s):
  if x in "([{":stack.append(x)
  elif x in ")]}":assert stack and stack.pop()==pairs[x],"Unbalanced delimiters"
 assert not stack
def run(worktree):
    worktree=Path(worktree);checks=[]
    def git(*a):return subprocess.check_output(["git","-C",str(worktree),*a])
    def blob(ref,path):return git("show",ref+":"+path)
    def check(name,value):
        if not value:raise AssertionError(name)
        checks.append(name)
    check("Windows_direct_parent_f90",git("rev-parse",WIN+"^").decode().strip()==BASE)
    paths=git("diff-tree","--no-commit-id","--name-only","-r",WIN).decode().splitlines()
    check("Windows_delta_exact_four_sources_two_fixtures",len(paths)==6 and sum(p.endswith(".cs")for p in paths)==4)
    mp=PREFIX+"SkirmishGroundSourceFitSurvey.MpGroundBaPilot.cs";sim=PREFIX+"SkirmishGroundSourceFitSurvey.SimultaneousRefinement.cs"
    for path in [mp,sim]:
        with tempfile.TemporaryDirectory(prefix="warline-sourcecheck-")as temp:
            temp=Path(temp)
            for label,ref in [("mac",MAC),("base",BASE),("win",WIN)]: (temp/label).write_bytes(blob(ref,path))
            merged=subprocess.run(["git","merge-file","-p",str(temp/"mac"),str(temp/"base"),str(temp/"win")],capture_output=True)
            check("conflict_free_three_way_"+Path(path).name,merged.returncode==0)
            check("exact_three_way_output_"+Path(path).name,(worktree/path).read_bytes()==merged.stdout)
    composed=(worktree/mp).read_text();mac=blob(MAC,mp).decode();win=blob(WIN,mp).decode()
    for name in ["ApplyMpPilotEndpointRevision","RunMpGroundBaPreparedReplayControls","RunS050ReviewedPadEndpointControls"]:
        check("Windows_method_byte_preserved_"+name,method(composed,name)==method(win,name))
    # Every Mac static method body remains exact except the explicitly owned Windows Apply delta.
    names=re.findall(r"(?m)^        (?:(?:public|private|internal|protected) )?static [\w<>,.\[\]? ]+? (\w+)\(",clean(mac))
    for name in names:
        if name!="ApplyMpPilotEndpointRevision":check("Mac_method_byte_preserved_"+name,method(composed,name)==method(mac,name))
    macsim=blob(MAC,sim).decode();composedsim=(worktree/sim).read_text()
    before="ApplyMpPilotEndpointRevision(owner.Definition,grid);";after="ApplyMpPilotEndpointRevision(owner.Definition,grid,preparedAuthoring);"
    check("Sim_only_exact_Windows_forwarding_on_Mac",composedsim==macsim.replace(before,after,1)and macsim.count(before)==1)
    for name in ["FacilityPreferences","PilotFacilityRelocation"]:
        p=PREFIX+"SkirmishGroundSourceFitSurvey."+name+".cs"
        check("whole_Mac_file_exact_"+name,(worktree/p).read_bytes()==blob(MAC,p))
    for p in paths:
        if p not in [mp,sim]:check("whole_Windows_path_exact_"+p,(worktree/p).read_bytes()==blob(WIN,p))
    for p in list(worktree.glob(PREFIX+"*.cs")):
        balanced(p.read_text())
    checks.append("six_CSharp_lexical_delimiters_balanced_not_compiler")
    check("generic_signature_authority_intact","JToken.DeepEquals(signature,expectedSignature)"in method(composed,"ValidateCompleteGroundBaBudgetReceipt")and"SkirmishSetupCompiler.TryInspectStaticAuthoringBudget"in method(composed,"CompileCompleteGroundBaExpectedSignatures"))
    check("generic47_exact_proposed_marker","result=Passed controls=47 nativeQueries=0 sourceWorlds=0 sourceFitCertified=0"in method(composed,"RunCompleteGroundBaTupleControls"))
    check("Windows20_exact_proposed_marker","[MpGroundBaPreparedReplayControls] result=Passed controls=20"in composed)
    check("Windows30_exact_proposed_marker","[S050ReviewedPadEndpointControls] result=Passed controls=30"in composed)
    oldpath=next(p for p in paths if p.endswith("logical-pad-input.json")and"OwnEstablished"in p);newpath=next(p for p in paths if p.endswith("logical-pad-input.json")and"Alignment"in p)
    old=json.loads((worktree/oldpath).read_bytes());new=json.loads((worktree/newpath).read_bytes())
    for d in [old,new]:d.pop("observedInputPath",None);d.pop("observedInputSha256",None)
    pad=next(p for p in new["padOverrides"]if p["padId"]=="pad.skirmish.db.staging_player")
    oldcenter=next(p for p in old["padOverrides"]if p["padId"]=="pad.skirmish.db.staging_player")["after"]["CenterX"]
    newcenter=pad["after"]["CenterX"];pad["after"]["CenterX"]=oldcenter
    check("fixture_only_typed_player_staging_center_scalar_changes",old==new and oldcenter!=newcenter)
    for path in sorted(set(paths+[PREFIX+"SkirmishGroundSourceFitSurvey.FacilityPreferences.cs",PREFIX+"SkirmishGroundSourceFitSurvey.PilotFacilityRelocation.cs"])):
        check("committed_raw_blob_equals_worktree_"+path,blob("HEAD",path)==(worktree/path).read_bytes())
    check("no_Library_materialized",not(worktree/"Library").exists())
    return {"scope":"PortableExactGitCompositionAndSourceStructure_NotCSharpCompilationOrNativeExecution","passed":len(checks),"checks":checks,"fixture_center_before":oldcenter,"fixture_center_after":newcenter,"CSharp47_executed":False,"Windows20_executed_on_composed_epoch":False,"Windows30_executed_on_composed_epoch":False,"compiler":False,"native":False}
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--worktree",required=True);p.add_argument("--output");a=p.parse_args();result=run(a.worktree);text=json.dumps(result,indent=2)+"\n"
    if a.output:Path(a.output).write_text(text)
    print(text,end="")
