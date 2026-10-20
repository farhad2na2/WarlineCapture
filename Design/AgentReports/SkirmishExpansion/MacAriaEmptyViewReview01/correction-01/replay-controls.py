#!/usr/bin/env python3
"""Bounded offline regression controls for the report reader; no camera fixtures."""
import contextlib,hashlib,importlib.util,io,json,pathlib,sys,types
path=pathlib.Path(__file__).resolve().parents[1]/"verify.py"
spec=importlib.util.spec_from_file_location("camera_report_verifier",path);v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
v.REV=sys.argv[1] if len(sys.argv)>1 else "HEAD"
rows=v.own("source-pins.json")["priorCorePinComparison"];first=rows[0];current=v.raw(v.BASE,first["path"])
assert hashlib.sha256(current).hexdigest()==first["priorSha256"]
result=lambda code,data=b"",error=b"":types.SimpleNamespace(returncode=code,stdout=data,stderr=error)
observed=v.measure_historical([first],lambda rev,path:result(0,current));assert v.historical_exit(observed)==0
print("PASS available exact committed bytes measured; exit0 only for actually checked row")
missing=v.measure_historical([first],lambda rev,path:result(128,error=b"Path exists on disk but is absent from this revision (simulated Windows diagnostic)"))
assert v.historical_exit(missing)==2 and missing[0]["status"]=="INCOMPLETE_UnavailableGitPathOrObject" and "sameAsBase" not in missing[0]
print("PASS simulated unavailable historical path gives INCOMPLETE/exit2 with no byte-match claim")
try:v.measure_historical([first],lambda rev,path:result(0,b"wrong committed bytes"))
except RuntimeError as e:assert "Historical bytes mismatch" in str(e)
else:raise AssertionError("Available hash mismatch accepted")
print("PASS available wrong historical bytes hard-fail; expected prior digest is not a substitute")
original=v.query
def missing_first(rev,path):
 if rev==first["priorAnalysisRevision"] and path==first["path"]:return result(128,error=b"Simulated unavailable historical Git row; no disk fallback")
 return original(rev,path)
v.query=missing_first
captured=io.StringIO()
with contextlib.redirect_stdout(captured):code=v.main()
assert code==2 and "PASS8 exact source excerpts" in captured.getvalue() and "RESULT INCOMPLETE" in captured.getvalue()
assert "RESULT Passed:" not in captured.getvalue()
print("BEGIN full verifier simulated first-historical-row absence (all other Git reads real)")
print(captured.getvalue(),end="")
print("END full verifier simulated absence")
print("PASS full verifier finishes remaining strict checks but returns2; original Windows failure remains unchanged")
print("RESULT Offline reader regression controls Passed; this is not Windows historical replay or camera/native acceptance")
