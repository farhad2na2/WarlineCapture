#!/usr/bin/env python3
"""Compare an experimental fresh pooled witness snapshot with the original owner."""
from pathlib import Path

recipe = Path(__file__).with_name('benchmark_s003_fresh_witness_digest.py')
source = recipe.read_text()


def replace_once(old, new):
    global source
    if source.count(old) != 1:
        raise ValueError('Exact benchmark seam required: ' + old[:100])
    source = source.replace(old, new, 1)


replace_once('s003-actual-importer-current-fixture-20261105-T6', 's003-actual-importer-current-fixture-20261105-T7')
replace_once("CONTROL = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/S003SnapshotCacheOriginalParityControl.cs'",
             "CONTROL = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/S003SnapshotCacheOriginalParityControl.cs'\nSHIM = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs'")
replace_once("BASE / 'compile.rsp', ORIGINAL, CONTROL, Path(__file__).resolve()", "BASE / 'compile.rsp', ORIGINAL, CONTROL, SHIM, Path(__file__).resolve(), Path(" + repr(str(Path(__file__).resolve())) + ")")
replace_once('var snapshot=cells.Copy();var snapshotBytes=System.Runtime.InteropServices.MemoryMarshal.AsBytes(snapshot.AsSpan());\n if(System.Runtime.InteropServices.Marshal.SizeOf<GridWalkable>()!=1||snapshotBytes.Length!=snapshot.Length)throw new InvalidOperationException("Exact one-byte grid layout required");\n using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(snapshotBytes);\n return new FixedString128Bytes(BitConverter.ToString(hash.GetHashAndReset()).Replace("-","").ToLowerInvariant());',
             'int length=cells.Length;var snapshot=System.Buffers.ArrayPool<GridWalkable>.Shared.Rent(Math.Max(1,length));\n try{cells.SourceCopyTo(snapshot,length);var snapshotBytes=System.Runtime.InteropServices.MemoryMarshal.AsBytes(snapshot.AsSpan(0,length));\n if(System.Runtime.InteropServices.Marshal.SizeOf<GridWalkable>()!=1||snapshotBytes.Length!=length)throw new InvalidOperationException("Exact one-byte grid layout required");\n using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(snapshotBytes);\n return new FixedString128Bytes(BitConverter.ToString(hash.GetHashAndReset()).Replace("-","").ToLowerInvariant());}\n finally{System.Buffers.ArrayPool<GridWalkable>.Shared.Return(snapshot);}')
replace_once("    args = [json.loads(s) for s in (BASE / 'compile.rsp').read_text().splitlines() if s.strip()]",
             "    shim = SHIM.read_text()\n    seam = 'internal T[] Copy()=>rows.ToArray();'\n    if shim.count(seam) != 1: raise ValueError('Exact dynamic buffer seam required')\n    shim_copy = out / 'PooledSnapshotEcs.cs'\n    shim_copy.write_text(shim.replace(seam, seam + 'internal void SourceCopyTo(T[] dest,int length){if(length!=rows.Count)throw new InvalidOperationException(\"Buffer changed during copy\");rows.CopyTo(0,dest,0,length);}', 1))\n    args = [json.loads(s) for s in (BASE / 'compile.rsp').read_text().splitlines() if s.strip()]\n    if args.count(str(SHIM)) != 1: raise ValueError('Exact source shim input required')\n    args = [str(shim_copy) if x == str(SHIM) else x for x in args]")
replace_once('Console.WriteLine("[FreshWitnessBenchmark]', 'Console.WriteLine("[PooledWitnessBenchmark]')
replace_once("'[FreshWitnessBenchmark]' not in run.stdout", "'[PooledWitnessBenchmark]' not in run.stdout")
replace_once('Full snapshot digest on every call; source fixture and microbenchmark only', 'Experimental pooled full snapshot digest on every call; source fixture and microbenchmark only')
exec(compile(source, str(recipe), 'exec'), {'__name__': '__main__', '__file__': str(recipe)})
