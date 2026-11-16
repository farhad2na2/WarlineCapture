#!/usr/bin/env python3
"""Produce a source-only, opt-in exact snapshot digest cache experiment."""
import hashlib
import re
from pathlib import Path

WITNESS = "Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs"
GRID = "Assets/Game/Scripts/Components/GridComponents.cs"
SHIM = "Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs"
OUTPUT = "SourceExperimentalWalkabilitySnapshotWitness.cs"
SIGNATURE = "public static FixedString128Bytes Digest(DynamicBuffer<GridWalkable> cells){"
INSERTION = '''
 if(SourceSnapshotCacheEnabled){
  var snapshot=cells.Copy();
  var snapshotBytes=System.Runtime.InteropServices.MemoryMarshal.AsBytes(snapshot.AsSpan());
  if(System.Runtime.InteropServices.Marshal.SizeOf<GridWalkable>()!=1 || snapshotBytes.Length!=snapshot.Length)throw new InvalidOperationException("Exact one-byte managed layout required");
  lock(SourceSnapshotGate){
   if(SourceSnapshotPrior!=null && snapshotBytes.SequenceEqual(System.Runtime.InteropServices.MemoryMarshal.AsBytes(SourceSnapshotPrior.AsSpan())))return SourceSnapshotDigest;
   using var fast=System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
   fast.AppendData(snapshotBytes);
   var digest=new FixedString128Bytes(BitConverter.ToString(fast.GetHashAndReset()).Replace("-","").ToLowerInvariant());
   SourceSnapshotPrior=snapshot;SourceSnapshotDigest=digest;return digest;
  }
 }
'''
FIELD = '''
 static readonly object SourceSnapshotGate=new object();
 static GridWalkable[] SourceSnapshotPrior;
 static FixedString128Bytes SourceSnapshotDigest;
 static readonly bool SourceSnapshotCacheEnabled=ReadSourceSnapshotCache();
 static bool ReadSourceSnapshotCache(){bool enabled=Environment.GetEnvironmentVariable("WARLINE_SOURCE_WALKABILITY_SNAPSHOT_CACHE")=="1";if(enabled)Console.WriteLine("[SourceWalkabilitySnapshotCache] enabled=1 scope=ExperimentalSourceOnly");return enabled;}
'''


def transform(witness, grid, shim):
    match = re.search(r"public struct GridWalkable\s*:\s*IBufferElementData\s*\{([^{}]*)\}", grid)
    if not match or re.sub(r"//[^\n]*|\s+", "", match.group(1)) != "publicbyteValue;":
        raise ValueError("Exact one-byte authored GridWalkable shape required")
    if shim.count("internal T[] Copy()=>rows.ToArray();") != 1:
        raise ValueError("Exact source-host buffer copy method required")
    if witness.count(SIGNATURE) != 1 or witness.count("public struct GridWalkableSourceWitness:IComponentData {") != 1:
        raise ValueError("Exact original witness shape required")
    return witness.replace(SIGNATURE, SIGNATURE + INSERTION, 1).replace(
        "public struct GridWalkableSourceWitness:IComponentData {",
        "public struct GridWalkableSourceWitness:IComponentData {" + FIELD, 1)


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    files = {name: (repo / name).read_bytes() for name in (WITNESS, GRID, SHIM)}
    result = transform(*(files[name].decode("utf-8") for name in (WITNESS, GRID, SHIM)))
    for name, content in files.items():
        if (repo / name).read_bytes() != content:
            raise ValueError("Snapshot cache input changed during generation: " + name)
    output = Path(output); output.mkdir(parents=True, exist_ok=False)
    raw = result.encode("utf-8")
    (output / OUTPUT).write_bytes(raw)
    return {"source_pins": {str(repo / name): hashlib.sha256(content).hexdigest() for name, content in files.items()},
            "experimental_only": True, "outputs": {OUTPUT: hashlib.sha256(raw).hexdigest()},
            "scope": "Source-only fresh full-buffer snapshot comparison; original default path retained"}
