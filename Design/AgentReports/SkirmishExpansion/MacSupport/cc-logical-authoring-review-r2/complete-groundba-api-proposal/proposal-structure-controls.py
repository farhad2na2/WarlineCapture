import re,json,hashlib,difflib
from pathlib import Path
HERE=Path(__file__).parent;BASE=HERE.parent
NAMES=["MpGroundBaPilot","SimultaneousRefinement","FacilityPreferences","PilotFacilityRelocation"]
PINS={"MpGroundBaPilot":"22816c78968066fcaecd692056c3fa1f2392ae78be484fdcaf02cbc79a84d5c5","SimultaneousRefinement":"7d7ba98442f3ae7c6d19521a0f9f25abc290bf1d1084c6377eee1c2f8282a1e1","FacilityPreferences":"b17bd14017b82f2af192feb6dc6bc5170c46cfdb3637b088796814f84604684c","PilotFacilityRelocation":"3bc066376fb23692f8947960472005100c483d17d25bd6fb4c2e2f8ce0b20cc4"}
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
old={n:(BASE/(n+".f90.reference.cs.txt")).read_text() for n in NAMES}
proposed={n:(HERE/("SkirmishGroundSourceFitSurvey."+n+".cs.proposal")).read_text() for n in NAMES}
protected=["ApplyMpPilotEndpointRevision","ApplyS049NativeCorridorMainSegments","ProbeS049OriginalCentralLegControls","MpPilotCanonicalRouteCoverage","MpPilotServiceAccess","MpPilotRepairRegionCells","MpPilotRepairWorldInRange","MpPilotQueryMatches","ApPairServiceAuthority","RunMpGroundBaCompleteStaticPilot","RunS049BoundedCompoundStaticPilot","RunS049BoundedLogicalFrameStaticPilot"]
legacy=method(old["MpGroundBaPilot"],"ValidateMpGroundBaStaticPilotResult");predicates=legacy[legacy.index("            Require((bool?)report"):legacy.index("            Debug.Log(")]
def validate(v):
 for s in v.values():balanced(s)
 mp=v["MpGroundBaPilot"];sim=v["SimultaneousRefinement"];pref=v["FacilityPreferences"];reloc=v["PilotFacilityRelocation"]
 for name in protected:assert method(mp,name)==method(old["MpGroundBaPilot"],name),"Dedicated existing behavior changed: "+name
 assert predicates in method(mp,"RequireCompleteGroundBaStaticPass"),"Old strict predicates changed"
 entry=method(mp,"RunGroundBaCompleteStaticPilot")
 assert 'if(id=="S049"||id=="S050"){RunMpGroundBaCompleteStaticPilot();return;}' in entry
 assert 'expectedSignatures=CompileCompleteGroundBaExpectedSignatures(authority,pin);' in entry and 'RunAll120BoundedFacilityPreferences();' in entry and 'finally{completeGroundBaAuthority=null;completeGroundBaSourcePins=null;}' in entry
 assert 'mpGroundBaPilotRequested=true' not in entry and 'ApplyMpPilotEndpointRevision' not in entry
 authority=method(mp,"CompleteGroundBaServiceAuthority")
 for token in ['sourceAssetSha256','gridAssetSha256','surfaceAssetSha256','definition.RequiredFeatureIds.SequenceEqual','definition.EconomyConfig==profiles.Economy','!layout.WorldBinding.HasMeasuredDeployments','!layout.WorldBinding.BindAllConfigurations']:
  assert token in authority,"Missing owner/source/profile guard: "+token
 assert 'Hash((string)pin["gridPath"])==(string)pin["gridAssetSha256"]' in authority
 for token in ['serviceAuthority:(mpGroundBaPilotRequested||apPairServiceRequested||completeGroundBaServiceRequested)?','if(refinementPreferenceReplay||mpGroundBaPilotRequested||apPairServiceRequested||completeGroundBaServiceRequested)','if(mpGroundBaPilotRequested||apPairServiceRequested||completeGroundBaServiceRequested)','(requireExactJointRoutes||apPairServiceRequested||completeGroundBaServiceRequested)','row["completeGroundBaAuthority"]=CompleteGroundBaBudgetReceipt']:
  assert token in sim,"Generic complete gate not wired: "+token
 assert '||CompleteGroundBaServiceAuthority(owner.Definition))?' in pref
 assert '||CompleteGroundBaServiceAuthority(serviceAuthority))&&' in reloc
 report=method(mp,"ValidateCompleteGroundBaBudgetReceipt")
 for token in ['JToken.DeepEquals(receipt["canonicalSizeRow"],own)','canonical_size_rows_sha256','kinds.Count==9','"Radar"','"Siege"','exactCanonicalQuantities','actualGridMatchesSource','starting_structures','logistics_support','objective_trucks','sourceFitCertified','normalMissionReady']:
  assert token in report,"Own budget/profile/false-credit check lost: "+token
 assert 'JToken.DeepEquals(signature,expectedSignature)' in report
 assert 'expectedSignatures?[size]' in report
 compiler=method(mp,"CompileCompleteGroundBaExpectedSignatures")
 for token in ['TryCreateGroundBa10Candidate(authority,source','CompleteGroundBaServiceAuthority(owner.Definition)','SkirmishSetupMatrixTable.TryLoadPackaged','SkirmishSetupCompiler.TryInspectStaticAuthoringBudget','budget.ExactCanonicalQuantities','ApPreferenceBudgetKey(budget).DeepClone()']:
  assert token in compiler, "Expected signature is not compiler-authoritative: "+token
 assert entry.index('CompileCompleteGroundBaExpectedSignatures')<entry.index('RunAll120BoundedFacilityPreferences();')
 controls=method(mp,"RunCompleteGroundBaTupleControls")
 assert len(re.findall(r'^            Authority\(',controls,re.M))==10
 assert len(re.findall(r'^            Report\(',controls,re.M))==35
 assert 'Require(controls==47' in controls and 'nativeQueries=0 sourceWorlds=0 sourceFitCertified=0' in controls
 assert 'new World(' not in entry and 'Process.Start' not in entry and 'File.Write' not in entry
 return True
cases=[]
for name in NAMES:assert hashlib.sha256((BASE/(name+".f90.reference.cs.txt")).read_bytes()).hexdigest()==PINS[name]
validate(proposed);cases.append({"case":"all_four_preimages_and_proposal_guards","passed":True})
mutations=[("same_count_signature_swap", "MpGroundBaPilot",'JToken.DeepEquals(signature,expectedSignature)','true'),("report_self_authority", "MpGroundBaPilot",'ApPreferenceBudgetKey(budget).DeepClone()','new JObject()'),("foreign_MP_geometry", "MpGroundBaPilot",'"Exact reviewed ArmorBreak grid required"','"Weakened MP grid"'),("activate_MP_geometry_for_generic","MpGroundBaPilot",'RunAll120BoundedFacilityPreferences();', 'mpGroundBaPilotRequested=true;RunAll120BoundedFacilityPreferences();'),("skip_strict_service","MpGroundBaPilot",'(bool?)(r["mpPilotServiceAccess"]?["allMembersHaveExactServiceIngressAndReturn"])==true','true'),("raw_grid_hash_waiver","MpGroundBaPilot",'Hash((string)pin["gridPath"])==(string)pin["gridAssetSha256"]','true'),("reduce_later_profiles","MpGroundBaPilot",'kinds.Count==9','kinds.Count>=5'),("budget_splice","MpGroundBaPilot",'JToken.DeepEquals(receipt["canonicalSizeRow"],own)','true'),("missing_preference_service_owner","FacilityPreferences",'||CompleteGroundBaServiceAuthority(owner.Definition))?',')?'),("missing_relocation_service_owner","PilotFacilityRelocation",'||CompleteGroundBaServiceAuthority(serviceAuthority))&&',')&&'),("skip_generic_route_independence","SimultaneousRefinement",'if(refinementPreferenceReplay||mpGroundBaPilotRequested||apPairServiceRequested||completeGroundBaServiceRequested)','if(refinementPreferenceReplay||mpGroundBaPilotRequested||apPairServiceRequested)')]
for label,key,before,after in mutations:
 v=dict(proposed);assert before in v[key],label;v[key]=v[key].replace(before,after,1);rejected=False
 try:validate(v)
 except AssertionError:rejected=True
 assert rejected,label;cases.append({"case":label,"passed":True,"mutant_rejected":True})
pins=[];diff=[]
for name in NAMES:
 p=HERE/("SkirmishGroundSourceFitSurvey."+name+".cs.proposal");pins.append({"path":"Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey."+name+".cs","preimage_ref":"f90b6bfed2e3ecf2d630d5eede839777812a1bf5","preimage_sha256":PINS[name],"proposal_sha256":hashlib.sha256(p.read_bytes()).hexdigest(),"proposal_bytes":p.stat().st_size});diff.extend(difflib.unified_diff(old[name].splitlines(keepends=True),proposed[name].splitlines(keepends=True),fromfile="f90/"+name+".cs",tofile="proposal/"+name+".cs"))
(HERE/"source-pins.json").write_text(json.dumps({"scope":"SourceProposalOnly_OutsideAssets_Uncompiled_Unexecuted","files":pins},indent=2)+"\n");(HERE/"source-api.diff").write_text("".join(diff));result={"scope":"TextStructureAndDeliberateSourceMutationControls_NotCSharpCompilerOrRuntimeProof","passed":len(cases),"failures":0,"preserved_dedicated_method_blocks":len(protected),"CSharp_controls_provided":47,"CSharp_controls_compiled":False,"CSharp_controls_executed":False,"native_runs":0,"cases":cases};(HERE/"structure-controls.log").write_text(json.dumps(result,indent=2)+"\n");print(json.dumps(result,indent=2))
