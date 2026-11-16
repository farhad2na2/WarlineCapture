#!/usr/bin/env python3
"""Extract the original drop-buffer reservation methods for an isolated compile probe."""
from __future__ import annotations
import argparse, hashlib, json
from pathlib import Path

SIGNATURES = (
    "internal static void ReserveStaticRenderDropBuffers(\n            EntityManager entityManager,\n            ref NativeBitArray reserved,\n            GridConfig grid,\n            int2 preferredCell)",
    "internal static void ReserveStaticRenderDropBuffers(\n            EntityManager entityManager,\n            ref NativeBitArray reserved,\n            Entity gridEntity,\n            GridConfig grid,",
    "private static void ReserveCellRect(ref NativeBitArray reserved, GridConfig grid, int minX, int minY, int maxX, int maxY)",
)

def extract(text: str, signature: str) -> str:
    at = text.find(signature)
    if at < 0 or text.find(signature, at + 1) >= 0:
        raise ValueError("Expected unique source signature: " + signature.splitlines()[0])
    brace = text.find("{", at)
    depth = 0; quoted = False; escape = False
    for i in range(brace, len(text)):
        c = text[i]
        if quoted:
            if escape: escape = False
            elif c == "\\": escape = True
            elif c == '"': quoted = False
        elif c == '"': quoted = True
        elif c == "{": depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0: return text[at:i + 1]
    raise ValueError("Unclosed source method")

def main() -> None:
    p = argparse.ArgumentParser(); p.add_argument("--source", type=Path, required=True); p.add_argument("--contract", type=Path, required=True); p.add_argument("--output", type=Path, required=True)
    a = p.parse_args(); raw = a.source.read_bytes(); text = raw.decode("utf-8")
    methods = [extract(text, s) for s in SIGNATURES]
    constants = []
    for name in ("HelicopterDropSearchRadiusCells", "HelicopterDropStaticRenderBufferCells", "HelicopterDropMaxStaticRenderExtentCells"):
        import re
        found = re.findall(r"private const int " + name + r"\s*=\s*\d+;", text)
        if len(found) != 1: raise ValueError("Expected unique constant " + name)
        constants.append(found[0].replace("private", "internal", 1))
    output = a.output; output.mkdir(parents=True, exist_ok=False)
    generated = """extern alias ActualCore;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Game.Components;
using Bounds = ActualCore::UnityEngine.Bounds;
using Rect = ActualCore::UnityEngine.Rect;

namespace Game.Rendering.SourceHost {
internal static class ExtractedOriginalDropBufferReservation {
""" + "\n".join(constants + methods) + "\n}\n}\n"
    (output / "ExtractedOriginalDropBufferReservation.cs").write_text(generated, encoding="utf-8")
    contract_raw=a.contract.read_bytes(); contract=contract_raw.decode("utf-8")
    if contract.count("using UnityEngine;") != 1: raise ValueError("Provider contract UnityEngine using changed")
    contract=contract.replace("using UnityEngine;", "using Matrix4x4 = ActualCore::UnityEngine.Matrix4x4;\nusing Bounds = ActualCore::UnityEngine.Bounds;\nusing Rect = ActualCore::UnityEngine.Rect;")
    contract="extern alias ActualCore;\n"+contract
    (output / "ExtractedRenderBoundsContract.cs").write_text(contract, encoding="utf-8")
    (output / "extraction.json").write_text(json.dumps({"kind":"ExtractedOriginalDropBufferReservation","status":"Passed","source":str(a.source.resolve()),"source_sha256":hashlib.sha256(raw).hexdigest(),"provider_contract_source":str(a.contract.resolve()),"provider_contract_sha256":hashlib.sha256(contract_raw).hexdigest(),"generated_sha256":hashlib.sha256(generated.encode()).hexdigest(),"provider_contract_is_original_with_engine_type_aliases":True,"method_bodies_copied_verbatim":True,"methods":["ReserveStaticRenderDropBuffers overloads","ReserveCellRect"],"rewritten_logic":False}, indent=2)+"\n", encoding="utf-8")
    print("[generate_reserve_probe] status=Passed methods=3 method_bodies_copied_verbatim=true")
if __name__ == "__main__": main()
