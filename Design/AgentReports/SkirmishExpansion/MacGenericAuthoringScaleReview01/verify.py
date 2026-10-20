#!/usr/bin/env python3
"""Static immutable evidence verification. No Unity/compiler/importer execution."""
import collections,csv,hashlib,io,json,re,subprocess,sys
BASE="dd4a5bca3ed3c8988efc51e4a37ecdfe49890b5d"
ROOT="Design/AgentReports/SkirmishExpansion/MacGenericAuthoringScaleReview01/"
REV=sys.argv[1] if len(sys.argv)>1 else "HEAD"
def raw(rev,path):return subprocess.check_output(["git","show",(":" if rev=="--index" else rev+":")+path])
def sha(b):return hashlib.sha256(b).hexdigest()
def own(name):return json.loads(raw(REV,ROOT+name))
def rows(name):return list(csv.DictReader(io.StringIO(raw(BASE,"Design/Roadmap/Skirmish_Expansion/"+name+".csv").decode())))
m=own("manifest.json")
for e in m["payloads"]:
 b=raw(REV,ROOT+e["name"]);assert len(b)==e["bytes"] and sha(b)==e["sha256"],e["name"]
print("PASS committed/staged canonical Git payloads",len(m["payloads"]))
s=own("source-pins.json");assert s["base"]==BASE
for path,e in s["pins"].items():
 b=raw(BASE,path);assert len(b)==e["bytes"] and sha(b)==e["sha256"],path
print("PASS exact-base raw Git source pins",len(s["pins"]))
for e in s["attributedPriorReports"]:
 b=raw(e["commit"],e["path"]);assert len(b)==e["bytes"] and sha(b)==e["sha256"]
print("PASS attributed immutable prior manifest pins",len(s["attributedPriorReports"]),"no runtime/acceptance transfer")
for e in s["missingRequiredSource"]:
 r=subprocess.run(["git","cat-file","-e",BASE+":"+e["path"]],capture_output=True,text=True)
 assert r.returncode!=0 and "does not exist" in r.stderr
 assert e["substitutionOrRecoveryPerformed"] is False
 print("PASS expected missing required authority at base:",e["path"],"exit",r.returncode)
for e in s["coherent80DeclaredPinAudit"]:
 b=raw(BASE,e["path"]);assert sha(b)==e["gitBlobSha256"]
 if e["state"]=="GitRawMatchesExpected":assert sha(b)==e["expectedAuthoritySha256"]
 elif "expectedMatchesEolOnlyMapping" in e:
  lf=b.replace(b"\r\n",b"\n");crlf=lf.replace(b"\n",b"\r\n")
  assert sha(lf)==e["gitLFMappingSha256"] and sha(crlf)==e["gitCRLFMappingSha256"]
  assert e["expectedMatchesEolOnlyMapping"] is True and sha(crlf)==e["expectedAuthoritySha256"]
 assert e["nativeFileBytesOrConsumptionVerified"] is False
print("PASS11 declared coherent authority/source/profile pins; one raw mismatch mapped exactly to CRLF; no raw waiver")
cat=rows("SCENARIO_CATALOG");matrix=rows("INITIAL_SETUP_MATRIX");manifest=rows("IMPLEMENTATION_MANIFEST")
index=own("plan-index.json");entries=index["entries"]
assert len(cat)==len(entries)==120 and len(matrix)==360 and all(len(r)==62 for r in matrix)
assert [r["catalogId"] for r in entries]==[r["scenario_id"] for r in cat]==["S%03d"%n for n in range(1,121)]
for c,e in zip(cat,entries):
 id=e["catalogId"]
 assert e["tuple"]=={k:c[k] for k in ["map_id","objective_id","army_profile","start_profile"]}
 assert e["manifestRow"]==next(r for r in manifest if r["catalog_id"]==id)
 vectors=[r for r in matrix if r["catalog_id"]==id]
 assert [r["size_id"] for r in vectors]==["Standard","War","LargeWar"]
 assert e["canonicalRows"]==[{"sizeId":r["size_id"],"fieldCount":62,"canonicalDictionarySha256":sha(json.dumps(r,sort_keys=True,separators=(",",":"),ensure_ascii=False).encode())} for r in vectors]
 assert e["protectedPracticeCounterpart"]==(id in ["S001","S025","S073"])
 assert e["codedExpanded"]==(id in ["S002","S003","S004","S005"])
 assert e["acceptedByThisReview"] is False
paths=subprocess.check_output(["git","ls-tree","-r","--name-only",BASE,"Assets/Game/Configs/SkirmishExpansion/Scenarios"]).decode().splitlines()
ids=sorted(re.search(r"/SkirmishScenario_(S\d{3})\.asset$",p).group(1) for p in paths if re.search(r"/SkirmishScenario_S\d{3}\.asset$",p))
assert ids==index["persistentDefinitions"]==["S002","S003","S004","S005","S049","S074"]
assert all(e["persistentDefinitionAtBase"]==(e["catalogId"] in ids) for e in entries)
assert sum(not e["persistentDefinitionAtBase"] for e in entries)==index["missingPersistent"]==114
assert sum(not e["persistentDefinitionAtBase"] and not e["protectedPracticeCounterpart"] for e in entries)==index["missingNonProtected"]==111
assert dict(collections.Counter(e["family"] for e in entries))==index["families"]=={"GroundBA10":10,"GroundObjective30":30,"AirCombined80":80}
assert dict(collections.Counter(e["family"] for e in entries if not e["persistentDefinitionAtBase"]))==index["missingByFamily"]=={"GroundBA10":7,"GroundObjective30":30,"AirCombined80":77}
print("PASS120 IDs/full manifest rows/seed families;360 unchanged62-field dictionary digests;6 persistent/114 missing/111 nonprotected")
assert raw(BASE,"Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv")==raw(BASE,"Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv")
print("PASS Design/runtime matrix exact raw equality")
for a in own("api-inventory.json")["apis"]:
 lines=raw(BASE,a["path"]).decode().splitlines();i=a["line"]-1
 assert a["symbol"] in lines[i] and a["excerpt"]=="\n".join(lines[i:i+4])
 assert a["sourceSha256"]==s["pins"][a["path"]]["sha256"]
print("PASS16 exact API symbols/line excerpts/pins")
schema=own("descriptor.schema.json");sr=schema["$defs"]["sizeRow"]
assert list(sr["properties"])==sr["required"]==list(matrix[0])
assert schema["properties"]["enabled"]["const"] is False
assert schema["properties"]["catalogId"]["enum"]==[r["scenario_id"] for r in cat]
assert schema["properties"]["canonicalSizeRows"]["minItems"]==schema["properties"]["canonicalSizeRows"]["maxItems"]==3
assert schema["properties"]["externalQualificationRefs"]["maxItems"]==0
assert own("proposed-changes.json")["executionAssigned"] is False
assert s["physicalDependenciesRehashed"] is s["compilerOrNativeExecuted"] is False
print("PASS schema declaration/header integrity; no JSON-Schema runtime/importer/Unity tests claimed")
print("RESULT Passed: source/data integrity only; proposed changes unimplemented; compilation/native/mission/APK/device NOT_RUN")
