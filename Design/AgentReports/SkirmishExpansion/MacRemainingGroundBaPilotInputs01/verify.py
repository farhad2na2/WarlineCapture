#!/usr/bin/env python3
"""Static Git evidence checks only; never invokes Unity or BuildBatch."""
import csv, hashlib, io, json, re, subprocess, sys
ROOT = "Design/AgentReports/SkirmishExpansion/MacRemainingGroundBaPilotInputs01/"
BASE = "dd4a5bca3ed3c8988efc51e4a37ecdfe49890b5d"
REV = sys.argv[1] if len(sys.argv) > 1 else "HEAD"
def get(rev, path):
    return subprocess.check_output(["git", "show", (":" if rev == "--index" else rev + ":") + path])
def digest(b): return hashlib.sha256(b).hexdigest()
def own(name): return json.loads(get(REV, ROOT + name))
def rows(path): return list(csv.DictReader(io.StringIO(get(BASE, path).decode())))
manifest = own("manifest.json")
assert manifest["base"] == BASE
for item in manifest["payloads"]:
    b = get(REV, ROOT + item["name"])
    assert len(b) == item["bytes"] and digest(b) == item["sha256"], item["name"]
print("PASS manifest canonical Git payloads", len(manifest["payloads"]))
pins = own("source-pins.json")["pins"]
for path, expected in pins.items():
    b = get(BASE, path)
    assert len(b) == expected["gitBlobBytes"] and digest(b) == expected["gitBlobSha256"], path
print("PASS exact-base Git source pins", len(pins))
road = "Design/Roadmap/Skirmish_Expansion/"
matrix = rows(road + "INITIAL_SETUP_MATRIX.csv")
full = json.loads(get(BASE, "Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json"))
full = {m["catalog_id"]: m for m in full["missions"]}
api = own("api-contract.json")
assert api["ownedIds"] == ["S050", "S097", "S098"] and api["excludedIds"] == ["S026"]
source = get(BASE, api["api"]).decode()
block = source.split("public sealed class Request", 1)[1].split("public sealed class RowResult", 1)[0]
assert re.findall(r"public \w+ (\w+);", block) == api["requestFieldNames"]
assert api["enabled"] is False
payloads = own("source-evidence.json")["lfsPayloads"]
for path, ev in payloads.items():
    pointer = get(BASE, path).decode()
    assert ev["lfsPointerGitBytes"] == pointer
    b = ev["payloadUtf8"].encode()
    assert digest(b) == re.search(r"oid sha256:(\w+)", pointer).group(1) == ev["payloadSha256"]
    assert len(b) == int(re.search(r"size (\d+)", pointer).group(1)) == ev["payloadBytes"]
print("PASS LFS small definition payload identity", len(payloads))
for id in api["ownedIds"]:
    spec = own(id + ".json")
    expected = [r for r in matrix if r["catalog_id"] == id]
    assert spec["enabled"] is False and spec["base"] == BASE and spec["catalogId"] == id
    assert spec["canonicalSizeRows"] == expected == full[id]["canonical_size_rows"]
    assert len(expected) == 3 and all(len(r) == 62 for r in expected)
    assert spec["implementationManifestRow"] == next(r for r in rows(road + "IMPLEMENTATION_MANIFEST.csv") if r["catalog_id"] == id)
    assert spec["catalogRow"] == next(r for r in rows(road + "SCENARIO_CATALOG.csv") if r["scenario_id"] == id)
    request = spec["requestObjectBindings"]
    assert set(request) == set(api["requestFieldNames"]) and request["CatalogId"] == id
    for k in ["CampaignSource", "MeasuredLayout", "ExpectedSourceIdentityHash", "ExpectedSourceContentHash", "ExpectedSourceAssetSha256", "ExpectedSceneGuid", "ExpectedGridGuid", "ExpectedSurfaceGuid"]:
        assert request[k] is None, (id, k)
    assert all(v is None for v in spec["missingMeasuredInputs"].values())
    assert all(v == "Pending" for v in spec["acceptance"].values())
    assert spec["expectedLogicalIdentity"] == {"operationMapId":spec["implementationManifestRow"]["operation_map_id"], "layoutId":spec["implementationManifestRow"]["layout_id"]}
    for ref in list(request["Profiles"].values()) + [request["Start"]] + list(spec["additionalProfileReferences"].values()):
        assert digest(get(BASE, ref["path"])) == ref["gitBlobSha256"]
        assert re.search(r"^guid: (\w+)",get(BASE, ref["path"]+".meta").decode(),re.M).group(1) == ref["guid"]
    candidate = spec["unqualifiedExistingCandidate"]
    path = candidate["path"]
    b = payloads[path]["payloadUtf8"].encode() if path in payloads else get(BASE,path)
    assert digest(b) == candidate["payloadSha256"] == full[id]["pilot_candidate"]["source_asset_sha256"]
    assert candidate["measuredMissionFrame"] is None and candidate["historicalReport"]["rawGridSurfaceReverified"] is False
    assert request["Start"]["path"].endswith("SkirmishStart_" + ("Field" if id == "S097" else "Established") + ".asset")
    print("PASS", id, "3 unchanged 62-field rows, seeds, profiles, candidate identity, disabled/null gates")
assert get(BASE, road+"INITIAL_SETUP_MATRIX.csv") == get(BASE,"Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv")
print("PASS canonical Design/runtime matrix raw equality")
print("RESULT Passed: static integrity only; compiler/native/fit/mission/APK/device NOT_RUN; all acceptance pending")
