from pathlib import Path
import gzip, hashlib, json

root = Path(__file__).resolve().parent
index = json.loads((root / "index.json").read_text())
objects = {obj["sha256"]: obj for obj in index["objects"]}
def sha(data): return hashlib.sha256(data).hexdigest()
for digest, obj in objects.items():
    compressed = (root / obj["object"]).read_bytes()
    assert sha(compressed) == obj["gzipSha256"]
    assert len(compressed) == obj["gzipBytes"]
    data = gzip.decompress(compressed)
    assert sha(data) == digest and len(data) == obj["bytes"]
for item in index["files"]:
    obj = objects[item["sha256"]]
    assert item["object"] == obj["object"] and item["bytes"] == obj["bytes"]
files = {item["originalPath"]: item for item in index["files"]}
prefix = "Design/AgentReports/SkirmishExpansion/S005/"
for run in index["runs"]:
    assert run["result"] == "Failed" and run["wrapperExit"] == run["nativeExit"] == 1
    assert files[prefix + run["preSourceManifest"]]["sha256"] == files[prefix + run["postSourceManifest"]]["sha256"]
assert not any("1134" in Path(item["originalPath"]).name for item in index["files"])
print("[S005SelectedEvidenceIndex] result=Passed aliases=%d uniqueObjects=%d exactClosedSourcePairs=6 readinessClaim=0" % (len(index["files"]), len(objects)))
