"""Build an explicit source-authority successor; never change mission content."""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ORIGINAL = "Tools/CI/SkirmishProductionDrafts/AirCombined80/air-combined-eighty.authoring.json"
ORIGINAL_SHA = "a869c03469d655f87389961242ffc0dc05720248ab6f6480cec43138793d8926"
ALLOWED = frozenset({
    "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs",
    "Assets/Game/Scripts/Configs/Skirmish/SkirmishMeasuredLayoutBinder.cs",
})


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def verify_semantics(before: dict, after: dict) -> None:
    """Only the two declared requirement-source digest values may differ."""
    left, right = copy.deepcopy(before), copy.deepcopy(after)
    for document in (left, right):
        pins = document["requirement_source_pins"]
        paths = [pin["path"] for pin in pins]
        if len(paths) != len(set(paths)):
            raise ValueError("Duplicate requirement source path")
        for pin in pins:
            if pin["path"] in ALLOWED:
                pin["sha256"] = "<coherent-source-authority>"
    if left != right:
        raise ValueError("Mission content or undeclared authority changed")
    rows = after["missions"]
    if len(rows) != 80 or len({row["catalog_id"] for row in rows}) != 80:
        raise ValueError("Exactly eighty unique mission identities required")
    if sum(len(row["canonical_size_rows"]) for row in rows) != 240:
        raise ValueError("Exactly240 canonical size vectors required")


def successor(original: dict, rows: list[dict], read_bytes) -> tuple[dict, list[dict]]:
    targets = [row["target"] for row in rows]
    if len(targets) != len(set(targets)):
        raise ValueError("Duplicate coherent projection target")
    by_target = {row["target"]: row for row in rows}
    output = copy.deepcopy(original)
    changes = []
    for pin in output["requirement_source_pins"]:
        target = pin["path"]
        if target not in ALLOWED:
            continue
        if target not in by_target:
            raise ValueError("Complete coherent compiler and binder projections required")
        row = by_target[target]
        if row["before_sha256"] != pin["sha256"]:
            raise ValueError("Projection does not descend from original authority: " + target)
        actual = digest(read_bytes(row["source"]))
        if actual != row["sha256"] or len(actual) != 64:
            raise ValueError("Projected source bytes differ: " + target)
        changes.append({"target": target, "source": row["source"],
                        "before_sha256": pin["sha256"], "sha256": actual})
        pin["sha256"] = actual
    if {row["target"] for row in changes} != ALLOWED:
        raise ValueError("Missing original requirement authority")
    verify_semantics(original, output)
    return output, changes


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--coherent-manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    raw = (ROOT / ORIGINAL).read_bytes()
    if digest(raw) != ORIGINAL_SHA:
        raise ValueError("Original a869 authority must remain exact")
    manifest_raw = args.coherent_manifest.read_bytes()
    manifest = json.loads(manifest_raw)
    result, changes = successor(json.loads(raw), manifest["files"],
                                lambda relative: (ROOT / relative).read_bytes())
    output = args.output.resolve()
    allowed_root = (ROOT / "Tools/CI/SkirmishProductionDrafts/AirCombined80CoherentAuthority").resolve()
    if not output.is_relative_to(allowed_root):
        raise ValueError("Successor must be outside Assets in its owned draft directory")
    receipt_path = output.with_suffix(output.suffix + ".receipt.json")
    if output.exists() or receipt_path.exists():
        raise FileExistsError("Fresh successor output and receipt required")
    encoded = (json.dumps(result, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    receipt = {"schema": 1, "scope": "SourceAuthoritySuccessorOnly",
               "original_path": ORIGINAL, "original_sha256": ORIGINAL_SHA,
               "coherent_manifest": str(args.coherent_manifest),
               "coherent_manifest_sha256": digest(manifest_raw),
               "output_sha256": digest(encoded), "changes": changes,
               "mission_content_identical": True, "missions": 80,
               "canonical_size_vectors": 240, "source_fit_certified": False,
               "camera_accepted": False, "mission_ready": False}
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("xb") as stream:
        stream.write(encoded)
    with receipt_path.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(receipt, stream, ensure_ascii=False, indent=2)
        stream.write("\n")
    print(json.dumps({"result": "BuiltUnqualified", "output": str(output),
                      "sha256": digest(encoded), "changed_authority_pins": len(changes)}))


if __name__ == "__main__":
    main()
