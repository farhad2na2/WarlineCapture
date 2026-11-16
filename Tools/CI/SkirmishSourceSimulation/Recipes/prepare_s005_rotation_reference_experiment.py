#!/usr/bin/env python3
"""Make a source-only S005 adapter from the existing strict S004 adapter.

The output is an experimental host source, not a change to game policy or a
qualification of map publication, scene loading, or mission readiness.
"""
import argparse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceOperationMapRotationReference.cs"


def once(text, old, new):
    if text.count(old) != 1:
        raise ValueError(f"expected one adapter anchor: {old[:100]}")
    return text.replace(old, new, 1)


def generate():
    text = SOURCE.read_text()
    start = text.index("  static SourceOperationMapRotationReference LoadCurrentS004FunctionCapture(")
    end = text.index("  static SourceOperationMapRotationReference LoadCurrentS073FunctionCapture(", start)
    method = text[start:end].replace("S004", "S005")
    substitutions = {
        "Assets/Game/Resources/SkirmishS005OperationMap.asset": "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishOperationMap_S005.asset",
        "5c47f6d88b5277d6d0b4a68474104d82b23e3dee2c46cf3a3bb7cb8a336613f4": "5b907fd62ea397e6a02eaea15aef0461a66400fe07b89ec77b8e729d0d895458",
        "f4ae9c56314db4509a41eb6981f68694": "f19b45a50b78d434987e806634756d87",
        "2f256289b279a99e599e0c5c20baa50d3ccc1bd426da8cb046d9c84f31a8c1ac": "77e11b6fcee716e02822165ae70f38f3e69fc287e93d4c27483563f0f636f319",
        "194144440d5aac4db713a2ededd4be53581646b18548e6e41d3b0104ea8a7570": "313deede00d7ac9cef181b4be9b8e65ec804bf293e5490c24b5300b1539715c2",
        "24b201a0e2ac75c7d83c6f924bb4d340caab8b8d2aed588a44ef884583f4e06a": "b3bd30faed22ed1595ff169b089f7c5e84c10faac4923f1eb652cd121479eb3c",
        "Logs/SkirmishProduction/s004-operation-map-rotation-capture-20261103-r1/contract.json": "T Logs/SkirmishProduction/s005-rotation-contract-20261105-T1/contract.json",
        "int[][] expectedInputs={new[]{1112434278,0,0},new[]{1119092736,0,0}}": "int[][] expectedInputs={new[]{1112434278,0,0},new[]{1112014848,1108082688,0},new[]{1119092736,0,0}}",
        "int[][] rowInputs={expectedInputs[0],expectedInputs[0],expectedInputs[1]}": "int[][] rowInputs={expectedInputs[0],expectedInputs[1],expectedInputs[2]}",
        'rows?.Count==2,"S005 finite two-input rotation set"': 'rows?.Count==3,"S005 finite three-input rotation set"',
        "int[][] expectedOutput={new[]{1054791324,0,0,1063680843},new[]{1060439283,0,0,1060439283}};for(int i=0;i<2;i++)": "int[][] expectedOutput={new[]{1054791324,0,0,1063680843},new[]{1053711781,1049332048,-1107156417,1063077572},new[]{1060439283,0,0,1060439283}};for(int i=0;i<3;i++)",
    }
    for old, new in substitutions.items():
        method = once(method, old, new)
    anchor = "   if(referenceSha256==S004ReferenceSha256) return LoadCurrentS004FunctionCapture(path,referenceSha256,sourceRoot);"
    text = once(text, anchor, anchor + '\n   if(referenceSha256=="e4effc83bed0987eef73fd30015b2555049a4eccbd4ea07273bec086924e8a2f") return LoadCurrentS005FunctionCapture(path,referenceSha256,sourceRoot);')
    anchor = "  static SourceOperationMapRotationReference LoadCurrentS073FunctionCapture("
    return once(text, anchor, method + anchor)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x") as out:
        out.write(generate())
