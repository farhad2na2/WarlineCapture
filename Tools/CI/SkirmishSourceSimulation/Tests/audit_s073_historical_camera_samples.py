#!/usr/bin/env python3
"""Report camera samples from one qualified historical observation only."""
import hashlib
import json
import sys
from pathlib import Path

directory = Path(sys.argv[1]).resolve(strict=True)
output = Path(sys.argv[2])
receipt_path = directory / "qualification-receipt.json"
data_path = directory / "delivery.jsonl"
receipt_bytes = receipt_path.read_bytes()
receipt = json.loads(receipt_bytes)
data = data_path.read_bytes()
digest = lambda b: hashlib.sha256(b).hexdigest()
def need(condition, message):
    if not condition:
        raise ValueError(message)

need(receipt["kind"] == "S073NativeProductionDeliveryQualificationReceipt", "Unexpected receipt kind")
need(receipt["wrapper"]["exit_code"] == 0, "Native wrapper did not pass")
need(receipt["source_artifacts"]["delivery.jsonl"]["sha256"] == digest(data), "Native observation hash changed")
session = receipt["session"]["session_id"]
samples = []
for line in data.decode("utf-8-sig").splitlines():
    row = json.loads(line)
    if row.get("camera_right"):
        need(row["session_id"] == session, "Camera sample belongs to a different session")
        need(all(len(row[field]) == 3 for field in ("camera_position", "camera_forward", "camera_right")), "Incomplete camera sample")
        samples.append({key: row[key] for key in ("unity_time", "camera_position", "camera_forward", "camera_right")})
need(len(samples) >= 2, "At least two camera samples required")
result = {"kind": "S073HistoricalNativeCameraSamples", "session_id": session,
          "receipt_sha256": digest(receipt_bytes), "delivery_sha256": digest(data),
          "samples": samples, "forward_changed": samples[0]["camera_forward"] != samples[-1]["camera_forward"],
          "scope": "Historical independent passive session; Unity Time.time, not match elapsed",
          "current_source_pins_verified": False, "source_host_camera_admitted": False, "mission_ready": False}
with output.open("x", encoding="utf-8") as f:
    json.dump(result, f, indent=2)
    f.write("\n")
print(json.dumps({"status": "Passed", "samples": len(samples), "forward_changed": result["forward_changed"]}))
