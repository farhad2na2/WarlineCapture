#!/usr/bin/env python3
"""Extract the current original static render-bounds owner method for source-host use."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

SIGNATURE = "public void CollectStaticBounds(EntityManager manager, Rect horizontalArea, float minExtent, float maxExtent,"


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def extract(source: Path) -> tuple[str, bytes]:
    raw = source.read_bytes()
    text = raw.decode("utf-8")
    starts = [index for index in range(len(text)) if text.startswith(SIGNATURE, index)]
    if len(starts) != 1:
        raise ValueError("Expected exactly one current CollectStaticBounds owner signature")
    start = starts[0]
    open_brace = text.find("{", start)
    if open_brace < 0:
        raise ValueError("Owner method opening brace missing")
    depth = 0
    in_string = False
    escape = False
    end = -1
    for index in range(open_brace, len(text)):
        char = text[index]
        if in_string:
            if escape:
                escape = False
            elif char == "\\":
                escape = True
            elif char == '"':
                in_string = False
            continue
        if char == '"':
            in_string = True
        elif char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                end = index + 1
                break
    if end < 0:
        raise ValueError("Owner method closing brace missing")
    method = text[start:end]
    required = (
        "ComponentType.ReadOnly<WorldRenderBounds>()",
        "ComponentType.ReadOnly<Prefab>()",
        "ComponentType.ReadOnly<Disabled>()",
        "ComponentType.ReadOnly<UnitFootprint>()",
        "GetComponentTypeHandle<WorldRenderBounds>(true)",
        "bounds[i].Value",
        "float extent = math.max(value.Extents.x, value.Extents.z);",
        "if (!math.all(math.isfinite(value.Center)) || !math.all(math.isfinite(value.Extents)) ||",
        "extent < minExtent || extent > maxExtent) continue;",
        "var area = new Rect(value.Center.x - value.Extents.x, value.Center.z - value.Extents.z,",
        "area.xMax < horizontalArea.xMin || area.yMax < horizontalArea.yMin ||",
        "area.xMin >= horizontalArea.xMax || area.yMin >= horizontalArea.yMax) continue;",
        "new Bounds(value.Center, value.Extents * 2f)",
    )
    missing = [item for item in required if item not in method]
    if missing or "StaticRenderBoundsFilter" in method:
        raise ValueError("Current owner no longer matches required source-host boundary: " + repr(missing))
    wrapper = """extern alias ActualCore;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Game.Components;
using Rect = ActualCore::UnityEngine.Rect;
using Bounds = ActualCore::UnityEngine.Bounds;

namespace Game.Rendering.SourceHost
{
    internal sealed class ExtractedOriginalStaticRenderBoundsOwner
    {
        """ + method + "\n    }\n}\n"
    return wrapper, raw


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    source = args.source.resolve(strict=True)
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    generated, raw = extract(source)
    generated_path = output / "ExtractedOriginalStaticRenderBoundsOwner.cs"
    with generated_path.open("x", encoding="utf-8") as target:
        target.write(generated)
    receipt = {
        "schema_version": 1,
        "kind": "SourceHostExtractedOriginalStaticRenderBoundsOwner",
        "status": "Passed",
        "source_path": str(source),
        "source_sha256": sha(raw),
        "extracted_method_sha256": sha(generated.encode("utf-8")),
        "generated_path": str(generated_path),
        "boundary_substitutions": [
            "Unity.Rendering.WorldRenderBounds/AABB shim has float3 Center/Extents",
            "Rect and Bounds resolve through ActualCore aliases",
        ],
        "inline_original_filter_copied_without_rewrite": True,
        "query_owner_body_copied_without_rewrite": True,
        "source_scene_admission": False,
        "mission_readiness": False,
    }
    with (output / "extraction.json").open("x", encoding="utf-8") as target:
        target.write(json.dumps(receipt, indent=2) + "\n")
    print("[generate_source_owner] status=Passed method_sha256=" + receipt["extracted_method_sha256"])
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
