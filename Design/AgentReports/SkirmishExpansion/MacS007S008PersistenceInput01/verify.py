#!/usr/bin/env python3
"""Exact Git report/source consistency only. Does not execute native code."""
import csv,hashlib,io,json,subprocess,sys
BASE="1a6821d8d63de9c60413b01a12a8cee0c22cd31e"
ROOT="Design/AgentReports/SkirmishExpansion/MacS007S008PersistenceInput01/"
REV=sys.argv[1] if len(sys.argv)>1 else "HEAD"
def raw(rev,path):
 return subprocess.check_output(["git","--no-replace-objects","show",(":" if rev=="--index" else rev+":")+path])
def sha(b):return hashlib.sha256(b).hexdigest()
def own(name):return json.loads(raw(REV,ROOT+name))
def load(path):return json.loads(raw(BASE,path))
for x in own("manifest.json")["payloads"]:
 b=raw(REV,ROOT+x["name"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"],x["name"]
print("PASS exact Git report payload manifest")
pins=own("source-pins.json");assert pins["base"]==BASE
for path,x in pins["pins"].items():
 b=raw(BASE,path);assert len(b)==x["bytes"] and sha(b)==x["sha256"],path
assert len(pins["pins"])==34
print("PASS34 exact bounded base pins; physical grid/surface remain declared only")
road="Design/Roadmap/Skirmish_Expansion/"
def rows(name):return list(csv.DictReader(io.StringIO(raw(BASE,road+name+".csv").decode())))
matrix=rows("INITIAL_SETUP_MATRIX");manifest=rows("IMPLEMENTATION_MANIFEST")
ground=load("Tools/CI/SkirmishProductionDrafts/GroundObjective30/ground-objective-thirty.authoring.json")
full=load("Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json")
assert raw(BASE,road+"INITIAL_SETUP_MATRIX.csv")==raw(BASE,"Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv")
assert pins["rootGround30Authority"]==ground["authority"]
tree=subprocess.check_output(["git","--no-replace-objects","ls-tree","-r","--name-only",BASE,"Assets/Game/Configs/SkirmishExpansion/Scenarios"]).decode().splitlines()
for id in ["S007","S008"]:
 x=own(id+".json");m=next(m for m in ground["missions"] if m["catalog_id"]==id)
 v=[r for r in matrix if r["catalog_id"]==id]
 assert len(v)==3 and all(len(r)==62 for r in v)
 assert x["canonicalSizeRows"]==v==m["canonical_size_rows"]
 assert x["manifestRow"]==next(r for r in manifest if r["catalog_id"]==id)
 assert x["full120AuthorityRow"]==next(r for r in full["missions"] if r["catalog_id"]==id)
 assert x["authority"]=={k:m[k] for k in ["tuple","packet","canonical_size_rows_sha256","source_candidate","collector_source_group"]}
 assert x["proposalScope"]["typedAreaStatus"]==m["typed_objective_areas"]["status"]=="AuthoredCandidate_UnmeasuredSourceFit"
 assert m["typed_objective_areas"]["measurement"] is None
 assert x["proposalScope"]["viewIntent"]==m["view_intent"] and m["view_intent"]["scalars"] is None
 assert x["proposalScope"]["templateCatalog"]==m["layout_input"]["template_pilot_catalog_id"]=="S026"
 assert x["proposalScope"]["templateGroup"]==m["layout_input"]["template_group"]=="CC"
 assert not x["enabled"] and not x["publicationEnabled"] and not x["nativeAssigned"] and not x["measurementReuseBetweenS007S008"]
 assert not x["proposalScope"]["aliasIsMeasuredIdentity"] and not x["proposalScope"]["proposalCoordinatesPromoted"]
 assert len(x["activeMeasuredInputs"])==13 and all(v is None for v in x["activeMeasuredInputs"].values())
 assert x["pendingGates"]==m["pending_gates"]
 assert not any(path.endswith("/SkirmishScenario_"+id+".asset") for path in tree)
 for r in x["existingProfiles"]:
  meta=raw(BASE,r["path"]+".meta").decode();assert "guid: "+r["guid"] in meta.splitlines()
print("PASS6 unchanged full62-field canonical vectors, manifest seeds, exact authority/profile refs; no persistent definitions")
n=own("next-measurement.json");a=own("artifact-gap.json");o=own("ownership.json")
assert n["catalogId"]=="S007" and not n["enabled"] and not n["nativeAssigned"]
assert n["environmentSchema"]=={"WARLINE_ALL120_REFINEMENT_CATALOG":"S007","WARLINE_ALL120_REFINEMENT_OUTPUT":None}
assert not any(n[k] for k in ["sourceFitCertified","cameraAccepted","normalMissionReady"])
assert not n["existingPersistenceEntry"]["enabled"] and not n["existingPersistenceEntry"]["assigned"]
assert a["requiredHistoricalSurvey"]["Mac"]=="Absent_Untransferred"
assert a["requiredHistoricalSurvey"]["Windows"]=="Available_ExactDigestReportedByCoordinator"
assert a["requiredHistoricalSurvey"]["sha256"]=="18b520ff6cb45a25f645502cba1787b27375cb52a20843195dd501e3b7029f73"
assert not a["requiredHistoricalSurvey"]["payloadTransferRequested"] and not a["committedAnalysesAreRawSurveySubstitutes"]
assert a["areaMeasurement"] is None and a["viewScalars"] is None and not a["searchScope"]["globalAbsenceClaim"]
assert not o["nativeJobAssigned"] and not o["processControlPerformed"]
editor="Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey."
c=raw(BASE,editor+"All120Refinement.cs").decode();m=raw(BASE,editor+"All120Materialization.cs").decode()
for token in ["namespace Game.Editor","RunAll120SimultaneousLogicalRefinement", "WARLINE_ALL120_REFINEMENT_CATALOG", "WARLINE_ALL120_REFINEMENT_OUTPUT", 'report["missionArtifacts"]', 'report["sourcePinsRecheckedAfterMeasurement"]=true', 'report["cleanupVerified"]=restored']:
 assert token in c,token
for token in ["MaterializeAll120ReviewedLogicalCandidate", "WARLINE_ALL120_MATERIALIZATION_REPORT_SHA256", 'report["missionArtifacts"]', 'measurement["normalThreeSizeCompilerPassed"]', "NativeQueryFoundExactGoal", 'failureCode', 'MaterializedDevelopmentOnly']:
 assert token in m,token
assert n["existingCollector"]=="Game.Editor.SkirmishGroundSourceFitSurvey.RunAll120SimultaneousLogicalRefinement"
assert n["existingPersistenceEntry"]["method"]=="Game.Editor.SkirmishGroundSourceFitSurvey.MaterializeAll120ReviewedLogicalCandidate"
print("PASS disabled API schemas, original survey attribution and dated no-run ownership limits")
print("PASS SOURCE/DATA CONSISTENCY ONLY; no native, compiler, materialization or acceptance credit")
