#!/usr/bin/env python3
"""Static Git/source evidence checks only. No runtime fixtures or process actions."""
import hashlib,json,re,subprocess,sys
BASE="dd4a5bca3ed3c8988efc51e4a37ecdfe49890b5d"
ROOT="Design/AgentReports/SkirmishExpansion/MacAriaEmptyViewReview01/"
REV=sys.argv[1] if len(sys.argv)>1 else "HEAD"
def raw(rev,p):return subprocess.check_output(["git","show",(":" if rev=="--index" else rev+":")+p])
def sha(b):return hashlib.sha256(b).hexdigest()
def own(n):return json.loads(raw(REV,ROOT+n))
for x in own("manifest.json")["payloads"]:
 b=raw(REV,ROOT+x["name"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
print("PASS canonical Git payload manifest")
s=own("source-pins.json");assert s["base"]==BASE
for p,x in s["pins"].items():
 b=raw(BASE,p);assert len(b)==x["bytes"] and sha(b)==x["sha256"],p
for x in s["priorImmutableReports"]:
 b=raw(x["commit"],x["path"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
print("PASS28 named base source/test/policy pins and3 prior immutable reports")
cs=s["priorCorePinComparison"];assert len(cs)==17
for x in cs:
 assert sha(raw(BASE,x["path"]))==x["baseSha256"]
 assert sha(raw(x["priorAnalysisRevision"],x["path"]))==x["priorSha256"]
 assert x["sameBytes"]==(x["baseSha256"]==x["priorSha256"])
 assert x["historical92a727PerFileAttribution"]=="UNKNOWN"
assert all(x["sameBytes"] for x in cs[:11])
assert [x["path"] for x in cs if not x["sameBytes"]]==["Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.cs"]
print("PASS11 core pins unchanged; only harness differs among17 comparisons; historical candidate attribution remainsUNKNOWN")
f=own("findings.json");assert len(f["findings"])==8
for x in f["findings"]:
 lines=raw(BASE,x["path"]).decode().splitlines();assert x["sourceExcerpt"]=="\n".join(lines[x["startLine"]-1:x["endLine"]])
print("PASS8 exact source excerpts; source-supported seams do not establish screenshot cause")
p=own("disabled-proposal.json");assert p["enabled"] is p["implementationSupplied"] is p["acceptedAsEmptyViewFix"] is False
assert p["separateConditionalCenterProposal"]["enabled"] is p["aircraftRestorePolicy"]["enabled"] is False
fixtures=own("fixtures.json");assert fixtures["status"]=="ProposedOnly_NotExecuted" and fixtures["laterBoundedReproducer"]["notAssignedOrRun"] is True
for path in fixtures["reuse"]:assert path in s["pins"]
e=own("evidence.json");assert e["causeProven"] is e["newNativeRun"] is False
assert e["boundedObservation"]["cameraPose"] is e["boundedObservation"]["activeCameraOwner"] is e["boundedObservation"]["panRequests"] is None
assert e["attribution"]["candidateSha256"]=="92a727e28012eca404d45ca673b847b37ce2e3ca84e5e3949f318706414178c2"
print("PASS disabled proposals/fixture-only status and causal-evidence limitations")
o=own("ownership.json");assert o["nativeJobAssignedToThisReview"] is o["processControlPerformed"] is False
editors=[r for r in o["editorsAndWorkers"] if r["processKind"]=="Editor"]
assert any(r["pid"]==63423 and r["cpuPercent"]==152.7 and "skirmish-saga-menu" in r["project"] for r in editors)
assert any(r["pid"]==51962 and r["project"]=="/Volumes/T7/Projects/Fig/Fig" for r in editors)
for r in o["editorsAndWorkers"]:
 for m in re.finditer(r"(?i)(-(?:accessToken|hubSessionId|cbmEditorLicense|cbmMachineId)(?:=|\s+))(?:\"[^\"]*\"|\S+)",r["redactedCommand"]):assert m.group(0).endswith("[REDACTED]")
print("PASS protected busy-capacity observation and sensitive argument redaction; no live launch admission")
print("RESULT Passed: static review integrity only; fixture/compiler/native/visual/mission/cleanup/paid/queue/APK/device acceptance NOT_GRANTED")
