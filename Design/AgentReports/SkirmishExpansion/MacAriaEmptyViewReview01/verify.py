#!/usr/bin/env python3
"""Static source evidence checks; exit2 means historical Git replay incomplete."""
import hashlib,json,re,subprocess,sys
BASE="dd4a5bca3ed3c8988efc51e4a37ecdfe49890b5d"
ROOT="Design/AgentReports/SkirmishExpansion/MacAriaEmptyViewReview01/"
REV="HEAD"
def sha(b):return hashlib.sha256(b).hexdigest()
def query(rev,path):
 return subprocess.run(["git","--no-replace-objects","show",(":" if rev=="--index" else rev+":")+path],capture_output=True)
def raw(rev,path):
 r=query(rev,path)
 if r.returncode:raise RuntimeError("Required Git object unavailable: "+rev+":"+path+"\n"+r.stderr.decode(errors="replace"))
 return r.stdout
def own(n):return json.loads(raw(REV,ROOT+n))
def measure_historical(rows,reader=None):
 if reader is None:reader=query
 measured=[]
 for x in rows:
  r=reader(x["priorAnalysisRevision"],x["path"])
  if r.returncode:
   measured.append({"path":x["path"],"revision":x["priorAnalysisRevision"],"status":"INCOMPLETE_UnavailableGitPathOrObject","exit":r.returncode,"diagnostic":r.stderr.decode(errors="replace")})
   continue
  actual=sha(r.stdout)
  if actual!=x["priorSha256"]:raise RuntimeError("Historical bytes mismatch: "+x["path"]+" expected="+x["priorSha256"]+" actual="+actual)
  measured.append({"path":x["path"],"revision":x["priorAnalysisRevision"],"status":"MeasuredExpectedDigest","actualSha256":actual,"sameAsBase":actual==x["baseSha256"]})
 return measured
def historical_exit(rows):return 2 if any(r["status"]!="MeasuredExpectedDigest" for r in rows) else 0
def main():
 global REV
 REV=sys.argv[1] if len(sys.argv)>1 else "HEAD"
 for x in own("manifest.json")["payloads"]:
  b=raw(REV,ROOT+x["name"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
 print("PASS canonical Git payload manifest")
 s=own("source-pins.json");assert s["base"]==BASE
 for path,x in s["pins"].items():
  b=raw(BASE,path);assert len(b)==x["bytes"] and sha(b)==x["sha256"],path
 for x in s["priorImmutableReports"]:
  b=raw(x["commit"],x["path"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
 print("PASS28 named base source/test/policy pins and3 prior immutable reports")
 correction=own("correction-01/correction.json")
 for x in correction["originalEvidence"]:
  b=raw(x["commit"],x["path"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
 assert correction["WindowsOriginalFailure"]["exit"]==1 and correction["WindowsOriginalFailure"]["originalFailureRemainsFailed"] is True
 print("PASS original4 artifact pins; Windows failure preserved as attributed report, not replayed/pass")
 cs=s["priorCorePinComparison"];assert len(cs)==17
 for x in cs:
  assert sha(raw(BASE,x["path"]))==x["baseSha256"]
  assert x["historical92a727PerFileAttribution"]=="UNKNOWN"
  assert x["crossHostHistoricalReplayComplete"] is False
 measured=measure_historical(cs)
 for row in measured:
  if row["status"]!="MeasuredExpectedDigest":print("INCOMPLETE historical Git row: "+json.dumps(row,ensure_ascii=True))
 complete=sum(r["status"]=="MeasuredExpectedDigest" for r in measured)
 equal=sum(r.get("sameAsBase",False) for r in measured)
 print("MEASURED historical expected-digest rows="+str(complete)+"/17; measured equal-to-base="+str(equal)+"; unmeasured="+str(17-complete)+"; no disk/hash fallback")
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

 code=historical_exit(measured)
 if code:
  print("RESULT INCOMPLETE: required current-base/report checks passed; historical Git replay incomplete; no complete historical comparison or acceptance")
 else:
  print("RESULT Passed: actual historical rows measured17/17; static review integrity only; runtime attribution/fixture/compiler/native/mission acceptance NOT_GRANTED")
 return code
if __name__=="__main__":
 try:sys.exit(main())
 except (AssertionError,RuntimeError,subprocess.CalledProcessError) as error:
  print("RESULT FAILED: "+str(error),file=sys.stderr);sys.exit(1)
