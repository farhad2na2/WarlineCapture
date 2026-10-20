#!/usr/bin/env python3
"""Read-only committed Git/archive provenance checks. No Unity or source restore."""
import hashlib,io,json,subprocess,sys,zipfile
BASE="dd4a5bca3ed3c8988efc51e4a37ecdfe49890b5d"
ROOT="Design/AgentReports/SkirmishExpansion/MacAirInputProvenanceReview01/"
REV=sys.argv[1] if len(sys.argv)>1 else "HEAD"
def raw(rev,p):return subprocess.check_output(["git","show",(":" if rev=="--index" else rev+":")+p])
def sha(b):return hashlib.sha256(b).hexdigest()
def own(n):return json.loads(raw(REV,ROOT+n))
manifest=own("manifest.json")
for x in manifest["payloads"]:
 b=raw(REV,ROOT+x["name"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"],x["name"]
print("PASS canonical Git payloads",len(manifest["payloads"]))
s=own("source-pins.json")
for x in s["pins"]:
 b=raw(x["commit"],x["path"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
 oid=subprocess.check_output(["git","rev-parse",x["commit"]+":"+x["path"]]).decode().strip();assert oid==x["gitBlobId"]
print("PASS immutable Git source/receipt/archive refs",len(s["pins"]))
c=own("coherent-comparison.json");versions=c["versions"];docs=[]
for v in versions:
 b=raw(v["commit"],v["path"]);docs.append(json.loads(b))
 assert len(b)==v["bytes"] and sha(b)==v["sha256"]
 lf=b.replace(b"\r\n",b"\n");crlf=lf.replace(b"\n",b"\r\n")
 assert sha(lf)==v["LFBytesSha256"] and sha(crlf)==v["CRLFBytesSha256"]
 assert s["expectedOriginalSha256"] not in [sha(lf),sha(crlf)] and v["eitherEolFormMatchesOriginal"] is False
 assert b.count(b"\n")==v["LF"] and b.count(b"\r\n")==v["CRLF"]==0
 original=subprocess.check_output(["git","ls-tree","-r","--name-only",v["commit"],"--",s["originalPath"]]).decode().strip()
 assert not original, "Original path now unexpectedly present in frozen tree"
print("PASS original absent from3 exact trees;3 coherent byte/EOL identities distinct from original")
def diffs(a,b,p=""):
 if type(a)!=type(b):return [{"path":p,"before":a,"after":b}]
 if isinstance(a,dict):return sum([diffs(a.get(k),b.get(k),p+"/"+k) for k in sorted(set(a)|set(b))],[])
 if isinstance(a,list):
  if len(a)!=len(b):return [{"path":p+"/length","before":len(a),"after":len(b)}]
  return sum([diffs(x,y,p+"/"+str(i)) for i,(x,y) in enumerate(zip(a,b))],[])
 return [] if a==b else [{"path":p,"before":a,"after":b}]
assert diffs(docs[0],docs[1])==c["historicalCoherentDiff"]
assert len(c["historicalCoherentDiff"])==1 and c["historicalCoherentDiff"][0]["path"]=="/requirement_source_pins/2/sha256"
assert docs[0]["missions"]==docs[1]["missions"]==docs[2]["missions"]
f=json.loads(raw(BASE,"Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json"));by={m["catalog_id"]:m for m in f["missions"]}
assert len(docs[2]["missions"])==80 and len({m["catalog_id"] for m in docs[2]["missions"]})==80
for m in docs[2]["missions"]:
 for k in ["definition_id","scenario_setup_id","tuple","packet","packet_section","canonical_size_rows","canonical_size_rows_sha256","required_capabilities"]:assert m[k]==by[m["catalog_id"]][k]
 assert len(m["canonical_size_rows"])==3 and all(len(r)==62 for r in m["canonical_size_rows"])
print("PASS one declared-pin-only coherent change;80 mission/240full62-field rows unchanged")
a=own("authored-receipts.json");archive=raw(BASE,a["archive"]);assert len(archive)==a["archiveBytes"] and sha(archive)==a["archiveSha256"]
z=zipfile.ZipFile(io.BytesIO(archive));assert len(z.namelist())==a["archiveEntries"]==31
assert not any(n.endswith("air-combined-eighty.authoring.json") for n in z.namelist())
for x in a["selectedMembers"]:
 b=z.read(x["name"]);assert len(b)==x["bytes"] and sha(b)==x["sha256"]
old=next(x for x in a["selectedMembers"] if "/before/" in x["name"]);b=z.read(old["name"]).decode()
assert 'const string AirCombined80Inputs="'+s["originalPath"]+'"' in b
assert 'const string AirCombined80InputsSha="'+s["expectedOriginalSha256"]+'"' in b
print("PASS archive receipt hash/31-member absence/4 selected members; before-reader original path/hash literals")
w=own("windows-attribution.json")
assert w["reportedBytes"]==815276 and w["reportedRawSha256"]==w["reportedSnapshotSha256"]==s["expectedOriginalSha256"]
assert w["payloadTransferredOrReadByMac"] is w["receiptTransferredOrReadByMac"] is False
assert w["authoritativeGitCommitIdentified"] is None and w["sourceEpochInclusionProven"] is w["compilerConsumptionProven"] is False
assert s["ToolsAssetsRestoredOrChanged"] is s["compilerNativeSDKLibraryProcessActions"] is s["acceptance"] is False
assert own("recovery-proposal.json")["originalExactBytesRecoveredByMac"] is False
print("PASS Windows availability retained as attributed metadata only; payload/receipt not independently verified")
print("RESULT Passed: local provenance review only; original reported available on Windows; authoritative commit/current source-epoch/consumption/mission acceptance pending")
