from skirmish_production_operational_inspector import need
from skirmish_production_queue import fingerprint
from s005_windows_source_snapshot import candidate_hash
import re
FOCUS_MARKER="[M5CloseInspectionFocusedControls] result=Passed closeControls=8 inspectionControls=26 legacyCases=141 cameraPolicyControls=9 precisionProducerCases=11 precisionPlannerChecks=22 focusChecks=27 missionReady=0"
CENSUS_MARKER="[SkirmishMatrixCompiledSetupCensus] result=Passed slots="
def snapshot(value):
 need(set(value)=={"source_manifest","candidate_sha256","build_manifest","build_sha256"},"Complete source/build snapshot")
 for name in ("source_manifest","build_manifest"):
  rows=value[name]
  need(isinstance(rows,list) and rows and len({r["path"] for r in rows})==len(rows),"Unique complete measured rows")
  need(all(isinstance(r["path"],str) and re.fullmatch("[a-f0-9]{64}",r["sha256"]) for r in rows),"Measured row hashes")
 need(candidate_hash(value["source_manifest"])==value["candidate_sha256"] and fingerprint(value["build_manifest"])==value["build_sha256"],"Snapshot identities derive from full rows")
 return value

def code_rows(s):
 return [r for r in s["source_manifest"] if r["path"].endswith((".cs",".asmdef",".asmref",".rsp")) or r["path"].startswith("ProjectSettings/") or r["path"] in ("Packages/manifest.json","Packages/packages-lock.json")]

def terminal(value,kind,text):
 need(type(value.get("wrapperExit"))is int and type(value.get("watcherExit"))is int and value["wrapperExit"]==value["watcherExit"]==0,"Actual terminal exits")
 for key in ("sourceUnchanged","pinsUnchanged","buildDeltaValid","generatedRspSemanticsPassed","initialRefreshCompleted","compilerErrorsAbsent","nativeCompileRestored","normalPackageManagerUsed","pipelineInstalled","approvedReservesUsed","launchHeadUnchanged","settingsRestored"):
  need(value.get(key)is True,"Measured native "+key)
 need(re.findall(r"\[UnityInvoke\] ExitCode: (-?\d+)",text)==["0"] and "process exit code was unavailable"not in text.lower(),"Unique actual checked exit")
 need("Application.AssetDatabase Initial Refresh End"in text and not re.search(r"error CS\d+:|Scripts have compiler errors",text),"Actual native import/compile")
 if kind=="focus":
  need(value.get("status")=="PassedFocusedControls_NoMissionAcceptance" and value.get("focusedControlsPassed")is True and isinstance(value.get("nativeFocusedEvidence"),dict),"Actual executed final test entry")
 else:
  need(value.get("status")=="PassedNativeCensus_NoGameplayOrQueueAcceptance" and value.get("censusCollected")is True and value.get("unchangedNativeBuildEstablished")is True,"Actual cached native census")
  need(text.count(CENSUS_MARKER)==1,"Actual native census execution")

def rsp(value):
 need(value.get("passed")is True and value.get("phase")=="postImport" and value.get("isolatedFullNativeCoverageClaim")is False and value.get("alreadyImportedMembership")is True and value.get("newMembershipOnRetry")is False,"Semantic RSP receipt must not become isolated compile coverage")
 owners=value.get("owners",[])
 need(len(owners)==17 and len({r["owner"]for r in owners})==17,"Complete accepted seventeen owners")
 need(all(r.get("multiplicitiesFlagsReferenceOrderMatch")is True and re.fullmatch("[a-f0-9]{64}",r["liveRawSha256"])for r in owners),"Actual strict RSP/reference receipts")
 return {r["owner"]:r["liveRawSha256"] for r in owners}
