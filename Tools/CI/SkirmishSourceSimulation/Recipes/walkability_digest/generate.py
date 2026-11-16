#!/usr/bin/env python3
"""Generate an opt-in source-only exact walkability digest experiment."""
import hashlib
import re
from pathlib import Path

WITNESS = "Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs"
GRID = "Assets/Game/Scripts/Components/GridComponents.cs"
SHIM = "Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs"
OUTPUT = "SourceExperimentalWalkabilityWitness.cs"
SIGNATURE = "public static FixedString128Bytes Digest(DynamicBuffer<GridWalkable> cells){"
INSERTION = '''
 if(SourceFastWalkabilityDigestEnabled){
  if(System.Runtime.InteropServices.Marshal.SizeOf<GridWalkable>()!=1)throw new InvalidOperationException("Exact one-byte GridWalkable source layout required");
  GridWalkable[] copy=cells.Copy();
  using var fast=System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
  fast.AppendData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(copy.AsSpan()));
  return new FixedString128Bytes(BitConverter.ToString(fast.GetHashAndReset()).Replace("-","").ToLowerInvariant());
 }
'''
FIELD = '''
 static readonly bool SourceFastWalkabilityDigestEnabled=ReadSourceFastWalkabilityDigest();
 static bool ReadSourceFastWalkabilityDigest(){bool enabled=Environment.GetEnvironmentVariable("WARLINE_SOURCE_FAST_WALKABLE_DIGEST")=="1";if(enabled)Console.WriteLine("[SourceFastWalkabilityDigest] enabled=1 scope=ExperimentalSourceOnly");return enabled;}
'''


def transform(witness, grid, shim):
    match = re.search(r"public struct GridWalkable\s*:\s*IBufferElementData\s*\{([^{}]*)\}", grid)
    if not match or re.sub(r"//[^\n]*|\s+", "", match.group(1)) != "publicbyteValue;":
        raise ValueError("Exact one-byte authored GridWalkable shape required")
    if shim.count("internal T[] Copy()=>rows.ToArray();") != 1:
        raise ValueError("Exact source-host buffer copy method required")
    if witness.count(SIGNATURE) != 1 or witness.count("public struct GridWalkableSourceWitness:IComponentData {") != 1:
        raise ValueError("Exact original witness shape required")
    output = witness.replace(SIGNATURE, SIGNATURE + INSERTION, 1)
    output = output.replace("public struct GridWalkableSourceWitness:IComponentData {",
                            "public struct GridWalkableSourceWitness:IComponentData {" + FIELD, 1)
    return output


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    files = {name: (repo / name).read_bytes() for name in (WITNESS, GRID, SHIM)}
    result = transform(*(files[name].decode("utf-8") for name in (WITNESS, GRID, SHIM)))
    for name, content in files.items():
        if (repo / name).read_bytes() != content:
            raise ValueError("Experimental digest input changed during generation: " + name)
    output = Path(output); output.mkdir(parents=True, exist_ok=False)
    raw = result.encode("utf-8")
    (output / OUTPUT).write_bytes(raw)
    return {"source_pins": {str(repo / name): hashlib.sha256(content).hexdigest() for name, content in files.items()},
            "experimental_only": True, "outputs": {OUTPUT: hashlib.sha256(raw).hexdigest()},
            "scope": "Source-only full-buffer snapshot hash; original native witness and default source branch retained; no caching"}
