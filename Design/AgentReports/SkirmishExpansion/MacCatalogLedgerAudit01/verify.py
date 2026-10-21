#!/usr/bin/env python3
"""Static catalog/source ledger checks only; no generated setup or native run."""
import collections,csv,hashlib,io,json,re,subprocess,sys
BASE="dd4a5bca3ed3c8988efc51e4a37ecdfe49890b5d"
ROOT="Design/AgentReports/SkirmishExpansion/MacCatalogLedgerAudit01/"
REV=sys.argv[1] if len(sys.argv)>1 else "HEAD"
def raw(rev,p):return subprocess.check_output(["git","--no-replace-objects","show",(":" if rev=="--index" else rev+":")+p])
def sha(b):return hashlib.sha256(b).hexdigest()
def own(n):return json.loads(raw(REV,ROOT+n))
def rows(n):return list(csv.DictReader(io.StringIO(raw(BASE,"Design/Roadmap/Skirmish_Expansion/"+n+".csv").decode())))
for x in own("manifest.json")["payloads"]:
 b=raw(REV,ROOT+x["name"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
print("PASS canonical Git report payload manifest")
s=own("source-pins.json")
for path,x in s["pins"].items():
 b=raw(BASE,path);assert len(b)==x["bytes"] and sha(b)==x["sha256"],path
for x in s["priorReviewReferences"]:
 b=raw(x["commit"],x["path"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
print("PASS52 exact bounded base refs and3 prior immutable review manifest refs")
cat=rows("SCENARIO_CATALOG");matrix=rows("INITIAL_SETUP_MATRIX");impl=rows("IMPLEMENTATION_MANIFEST");queue=rows("WORK_QUEUE_004_120")
a=own("catalog-audit.json");entries=a["entries"];im={r["catalog_id"]:r for r in impl};q={r["catalog_id"]:r for r in queue}
assert [r["scenario_id"] for r in cat]==[e["catalogId"] for e in entries]==["S%03d"%i for i in range(1,121)]
assert len(matrix)==360 and all(len(r)==62 for r in matrix) and len(queue)==117
assert set(q)==set(im)-{"S001","S025","S073"}
assert all(q[id]==im[id] for id in q)
f=json.loads(raw(BASE,"Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json"));by={r["catalog_id"]:r for r in f["missions"]}
for c,e in zip(cat,entries):
 id=e["catalogId"];v=[r for r in matrix if r["catalog_id"]==id]
 assert len(v)==3 and v==by[id]["canonical_size_rows"]
 assert e["canonicalRows"]==[{"size":r["size_id"],"fields":62,"dictionarySha256":sha(json.dumps(r,sort_keys=True,separators=(",",":"),ensure_ascii=False).encode())} for r in v]
 assert e["tuple"]=={k:c[k] for k in ["map_id","objective_id","army_profile","start_profile"]}
 assert e["family"]==c["map_id"]+"_"+c["objective_id"] and e["designStatus"]==c["status"]
 assert e["implementationDisposition"]=={k:im[id][k] for k in ["handoff_ordinal","legacy_runtime_index","implementation_status","manual_win","aria_win","checkpoint","device","evidence_path"]}
 assert e["in117Queue"]==(id in q) and e["currentRuntimeOrCertification"]=="UNKNOWN"
assert raw(BASE,"Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv")==raw(BASE,"Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv")
print("PASS120IDs/360unchanged62-field rows and117 exact queue/disposition joins; runtime matrix raw equality")
assert len(a["families"])==20 and sum(x["plannedIds"] for x in a["families"])==120
for family in a["families"]:
 es=[e for e in entries if e["family"]==family["family"]]
 assert family["catalogIds"]==[e["catalogId"] for e in es] and len(es)==family["plannedIds"]==6 and family["plannedSetups"]==18
 assert family["currentQualifiedIds"]==family["currentQualifiedSetups"]=="UNKNOWN"
summary=a["summary"]
assert all(v=="UNKNOWN" for v in summary["currentCertificationTotals"].values())
assert all(v=="UNKNOWN" for v in summary["currentImplementationTotals"].values())
assert summary["persistentDefinitionIds"]==["S002","S003","S004","S005","S049","S074"] and summary["missingPersistentDefinitionCount"]==114 and summary["missingNonProtectedDefinitionCount"]==111
paths=subprocess.check_output(["git","ls-tree","-r","--name-only",BASE,"Assets/Game/Configs/SkirmishExpansion/Scenarios"]).decode().splitlines()
actual_defs=sorted(p for p in paths if re.search(r"/SkirmishScenario_S\d{3}\.asset$",p))
assert actual_defs==sorted(e["definitionAssetAtBase"] for e in entries if e["definitionAssetAtBase"])
for fam in a["families"]:
 es=[e for e in entries if e["family"]==fam["family"]]
 assert fam["persistedDefinitionIds"]==[e["catalogId"] for e in es if e["definitionAssetAtBase"]]
 assert fam["missingDefinitionIds"]==[e["catalogId"] for e in es if not e["definitionAssetAtBase"]]
assert [(x["plannedIds"],x["plannedSetups"],x["persistedDefinitions"]) for x in summary["pipelineFamilies"]]==[(10,30,3),(30,90,0),(80,240,3)]
print("PASS20families with explicit denominators; current implementation/certification totals UNKNOWN")
def yamlrows(path,field):
 out={}
 for block in raw(BASE,path).decode().split("\n  - "+field+": ")[1:]:
  id=block.splitlines()[0];out[id]={k:v for k,v in re.findall(r"^    ([A-Za-z][A-Za-z0-9]*): ?(.*)$",block,re.M)}
 return out
lib=yamlrows("Assets/Game/Resources/SkirmishBattleCatalog.asset","ScenarioId");pub=yamlrows("Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset","CatalogId")
assert len(lib)==120 and dict(collections.Counter(r["Status"] for r in lib.values()))==summary["resourceLibraryDeclarationCounts"]=={"1":6,"0":114}
assert [id for id,r in lib.items() if r["Status"]=="1"]==summary["resourceLibraryPlayableIds"]==["S001","S002","S003","S004","S025","S073"]
assert len(pub)==4 and dict(collections.Counter(r["Status"] for r in pub.values()))==summary["publicationDeclarationCounts"]=={"2":3,"1":1}
for e in entries:
 id=e["catalogId"];assert e["libraryDeclaration"]=={k:lib[id].get(k) for k in ["Status","PlayableScenarioIndex","DefinitionId","ContentVersion","ReadinessManifestId"]}
 for legacy,index in [("S001","0"),("S025","1"),("S073","3")]:assert lib[legacy]["PlayableScenarioIndex"]==index
assert not any(r["Status"]=="1" and r["PlayableScenarioIndex"]=="2" for r in lib.values())
print("PASS serialized library/publication declarations and protected0/1/3; no runtime or certification credit")
b=own("source-bindings.json");assert b["candidateGroupCount"]==5 and b["uniqueCandidatePathCount"]==4
assert len(b["persistentDefinitionBindings"])==6 and sum(bool(x["serializedCampaignGuid"]) for x in b["persistentDefinitionBindings"])==3
for x in b["persistentDefinitionBindings"]:
 t=raw(BASE,x["path"]).decode();assert "  catalogId: "+x["catalogId"] in t
 if x["serializedCampaignGuid"]:
  ref=x["resolvedCampaignReference"];assert ref is not None
  meta=raw(BASE,ref["path"]+".meta").decode();assert re.search(r"^guid: (\w+)",meta,re.M)[1]==x["serializedCampaignGuid"]
 assert x["runtimePhysicalSource"]==x["nativeSourceFit"]==x["normalMissionCertification"]=="UNKNOWN"
assert [x["catalogId"] for x in b["ledgerDrift"]]==["S049","S074"] and all(x["Full120Declared"] is None for x in b["ledgerDrift"])
print("PASS five proposed groups/four candidate paths and six definition binding metadata joins; S049/S074 ledger drift explicit")
r=own("runtime-paths.json");assert len(r["traces"])==9
for x in r["traces"]:
 lines=raw(BASE,x["path"]).decode().splitlines();assert x["excerpt"]=="\n".join(lines[x["startLine"]-1:x["endLine"]])
assert r["countInterpretation"]["currentImplementedMissionTotal"]=="UNKNOWN" and r["countInterpretation"]["WindowsDirtyA7ImplementationTotal"]=="UNKNOWN"
focused=next(x for x in a["families"] if x["family"]=="DB_FC");packet=focused["existingFamilyInputReference"];assert sha(raw(BASE,packet["path"]))==packet["sha256"] and focused["focusedPacketGitHashMatchesDeclared"] is True
d=json.loads(raw(BASE,packet["path"]));assert d["actual_authored_objective_geometry"] is d["per_source_readable_view_profiles"] is None
for x in d["missions"]:assert x["canonical_size_rows"]==by[x["catalog_id"]]["canonical_size_rows"]
work=own("independent-work.json");assert work["status"]=="RecommendationsOnly_NotAssignedOrImplemented" and work["priorityOne"]["catalogIds"]==["S007","S008"]
print("PASS9runtime generation/launch excerpts and one focused DB_FC packet; S007/S008 next-step recommendation only")
print("RESULT Passed: static source ledger integrity; no global implemented/certified count; compiler/native/Game/process/Library/qualification actions NOT_PERFORMED")
