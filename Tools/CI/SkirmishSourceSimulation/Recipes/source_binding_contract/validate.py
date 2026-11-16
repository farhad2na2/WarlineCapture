#!/usr/bin/env python3
"""Compile and exercise the original OperationMapContractValidation binding methods.

The fixture reads the checked-in S074 logical definition and its physical source,
extracts the two original C# methods byte-for-byte, then compiles them with tiny
contract-compatible type stubs. It does not load Unity or modify game sources.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
CONTRACT = ROOT / "Assets/Game/Scripts/Configs/OperationMapContractValidation.cs"
LOGICAL = ROOT / "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
PHYSICAL = ROOT / "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset"
PASS = "[SourceBindingContract] result=Passed original_methods=2 actual_assets=2 negative_cases=5"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def scalar(text: str, key: str, section: str | None = None) -> str:
    if section:
        match = re.search(rf"(?ms)^  {re.escape(section)}:\n(.*?)(?=^  [A-Za-z][A-Za-z0-9]*:|\Z)", text)
        if not match:
            raise ValueError(f"missing section {section}")
        text = match.group(1)
    match = re.search(rf"(?m)^\s*{re.escape(key)}:\s*([^\s#]+)\s*$", text)
    if not match:
        raise ValueError(f"missing scalar {key}")
    return match.group(1)


def method(source: str, signature: str) -> str:
    start = source.index(signature)
    brace = source.index("{", start)
    depth = 0
    for i in range(brace, len(source)):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[start:i + 1]
    raise ValueError(f"unterminated method {signature}")


def cs(value: str) -> str:
    return json.dumps(value)


def build_source(values: dict[str, str]) -> str:
    original = CONTRACT.read_text(encoding="utf-8")
    find_index = method(original, "private static int FindOperationMapIndex(")
    validate_binding = method(original, "private static bool TryValidateSourceBinding(")
    # These method declarations are copied unchanged from the checked-in file.
    maps = f'''new OperationMapDefinition[] {{
      Make({cs(values['logical_id'])},{cs(values['logical_identity'])},{cs(values['logical_content'])},{cs(values['logical_source_id'])},{cs(values['binding_identity'])},{cs(values['binding_content'])},{cs(values['logical_scene'])}),
      Make({cs(values['physical_id'])},{cs(values['physical_identity'])},{cs(values['physical_content'])},"","","",{cs(values['physical_scene'])})
    }}'''
    return r'''using System;
namespace Game.Configs {
 public sealed class OperationMapSourceBindingConfig {
   public string SourceOperationMapId { get; set; }
   public string SourceIdentityHash { get; set; }
   public string SourceContentHash { get; set; }
   public bool IsConfigured => !string.IsNullOrEmpty(SourceOperationMapId);
 }
 public sealed class SceneReference { public string AssetGUID { get; set; } }
 public sealed class OperationMapDefinition {
   public string OperationMapId { get; set; }
   public string SourceIdentityHash { get; set; }
   public string ContentHash { get; set; }
   public OperationMapSourceBindingConfig SourceBinding { get; set; }
   public SceneReference SourceSceneReference { get; set; }
 }
 public static class ExtractedOriginal {
''' + find_index + "\n" + validate_binding + r'''
   static OperationMapDefinition Make(string id,string identity,string content,string sourceId,string sourceIdentity,string sourceContent,string scene) => new OperationMapDefinition {
     OperationMapId=id,SourceIdentityHash=identity,ContentHash=content,
     SourceBinding=new OperationMapSourceBindingConfig { SourceOperationMapId=sourceId,SourceIdentityHash=sourceIdentity,SourceContentHash=sourceContent },
     SourceSceneReference=new SceneReference { AssetGUID=scene }
   };
   static void Need(bool value,string label){if(!value)throw new Exception("control failed: "+label);}
   static bool Check(OperationMapDefinition[] definitions,out string error) => TryValidateSourceBinding(definitions,definitions[0],out error);
   static OperationMapDefinition[] Fresh() => ''' + maps + r''';
   public static int Main(){
     var good=Fresh(); string error;
     Need(Check(good,out error),"actual logical bound to retained physical source: "+error);
     Need(Check(new[]{good[1]},out error),"retained physical map independently valid: "+error);
     var bad=Fresh();bad[0].SourceBinding.SourceOperationMapId="opmap.invalid";Need(!Check(bad,out error),"wrong source map rejected");
     bad=Fresh();bad[0].SourceBinding.SourceIdentityHash="bad";Need(!Check(bad,out error),"source identity mismatch rejected");
     bad=Fresh();bad[0].SourceBinding.SourceContentHash="bad";Need(!Check(bad,out error),"source content mismatch rejected");
     bad=Fresh();bad[0].SourceSceneReference.AssetGUID="different-scene-guid";Need(!Check(bad,out error),"logical scene mismatch rejected");
     bad=Fresh();bad[1].SourceBinding.SourceOperationMapId="opmap.recursive";Need(!Check(bad,out error),"recursive physical binding rejected");
     Console.WriteLine("''' + PASS + r'''");return 0;
   }
 }
}
'''


def run(output: Path) -> dict:
    logical_text, physical_text = LOGICAL.read_text(), PHYSICAL.read_text()
    values = {
        "logical_id": scalar(logical_text, "operationMapId"),
        "logical_identity": scalar(logical_text, "sourceIdentityHash"),
        "logical_content": scalar(logical_text, "contentHash"),
        "logical_source_id": scalar(logical_text, "sourceOperationMapId", "sourceBinding"),
        "binding_identity": scalar(logical_text, "sourceIdentityHash", "sourceBinding"),
        "binding_content": scalar(logical_text, "sourceContentHash", "sourceBinding"),
        "logical_scene": scalar(logical_text, "m_AssetGUID", "sourceSceneReference"),
        "physical_id": scalar(physical_text, "operationMapId"),
        "physical_identity": scalar(physical_text, "sourceIdentityHash"),
        "physical_content": scalar(physical_text, "contentHash"),
        "physical_scene": scalar(physical_text, "m_AssetGUID", "sourceSceneReference"),
    }
    if values["logical_id"] != "opmap.skirmish.industrial_basin" or values["logical_source_id"] != values["physical_id"]:
        raise ValueError("fixture inputs are not the expected S074/physical-map pair")
    if values["logical_identity"] != values["binding_identity"] or values["physical_identity"] != values["binding_identity"]:
        raise ValueError("logical/binding/physical source identity hashes disagree")
    source = CONTRACT.read_text(encoding="utf-8")
    need_sha = {str(p): sha(p) for p in (CONTRACT, LOGICAL, PHYSICAL)}
    output.mkdir(parents=True, exist_ok=False)
    (output / "ExtractedOriginalFixture.cs").write_text(build_source(values), encoding="utf-8")
    (output / "SourceBindingContract.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>\n',
        encoding="utf-8")
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise ValueError("dotnet SDK is unavailable")
    build = subprocess.run([dotnet, "build", str(output / "SourceBindingContract.csproj")], capture_output=True, text=True)
    (output / "compile.log").write_text(build.stdout + build.stderr, encoding="utf-8")
    if build.returncode:
        raise ValueError("isolated validator fixture compile failed; see compile.log")
    execute = subprocess.run([dotnet, str(output / "bin/Debug/net8.0/SourceBindingContract.dll")], capture_output=True, text=True)
    (output / "run.log").write_text(execute.stdout + execute.stderr, encoding="utf-8")
    if execute.returncode or execute.stdout.strip() != PASS:
        raise ValueError("source binding controls failed; see run.log")
    receipt = {
        "kind": "SourceOnlyOriginalOperationMapBindingFixture", "status": "Passed",
        "logical_map_id": values["logical_id"], "physical_source_map_id": values["physical_id"],
        "logical_definition": {"path": str(LOGICAL), "sha256": need_sha[str(LOGICAL)]},
        "physical_definition": {"path": str(PHYSICAL), "sha256": need_sha[str(PHYSICAL)]},
        "original_validator_source": {"path": str(CONTRACT), "sha256": need_sha[str(CONTRACT)]},
        "extracted_original_methods": ["FindOperationMapIndex", "TryValidateSourceBinding"],
        "original_methods_unchanged": True,
        "checks": ["actual logical-to-physical binding accepted", "physical source retained and standalone",
                   "wrong source id rejected", "source identity mismatch rejected", "source content mismatch rejected",
                   "logical scene GUID mismatch rejected", "recursive physical binding rejected"],
        "scope": "Original binding contract only; no Unity assets written, no world/runtime/map geometry/readiness claim",
        "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log"),
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(PASS)
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True, help="fresh output directory")
    args = parser.parse_args()
    try:
        receipt = run(args.output.resolve())
        print(json.dumps(receipt, sort_keys=True))
        return 0
    except Exception as exc:
        print(str(exc), file=sys.stderr)
        return 1


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


if __name__ == "__main__":
    raise SystemExit(main())
