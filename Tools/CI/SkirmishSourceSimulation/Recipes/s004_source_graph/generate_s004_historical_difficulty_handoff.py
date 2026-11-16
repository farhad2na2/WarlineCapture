#!/usr/bin/env python3
"""Project an explicit selected-setup handoff into the source-only S004 host."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/S004BundleDrivenStartup.cs'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def generate(output, expected_sha):
    if sha(SOURCE) != expected_sha: raise ValueError('Exact S004 source host required')
    original = SOURCE.read_text()
    substitutions = [
        ('var setup = SourceS004CompiledSetupInput.Read(Path.GetFullPath(args[0]), args[1]);',
         '''var historicalSetup = SourceS004CompiledSetupInput.Read(Path.GetFullPath(args[0]), args[1]);
            var setup = SourceS004CompiledSetupInput.Read(Path.GetFullPath(args[17]), args[18]);
            var compatibilityGrid = new GridConfig { Width = 2048, Height = 1024, CellSize = 1f, Origin = float3.zero };
            SourceS004HistoricalReadmodelCompatibility.RequireCompatible(historicalSetup, setup, in compatibilityGrid);'''),
        ('(uint?)nativeReadmodel["setup_hash"] == setup.SetupHash',
         '(uint?)nativeReadmodel["setup_hash"] == historicalSetup.SetupHash'),
        ('GeneratedBuildingDefinitions.LoadS004Source(Path.GetFullPath(args[7]), args[8], templates, setup, Path.GetFullPath(args[0]), args[1])',
         'GeneratedBuildingDefinitions.LoadS004Source(Path.GetFullPath(args[7]), args[8], templates, setup, Path.GetFullPath(args[17]), args[18])'),
        ('SourceS004SetupGuardCacheControls.Run(setup,Path.GetFullPath(args[0]),args[1],Path.GetDirectoryName(args[4]));',
         'SourceS004SetupGuardCacheControls.Run(historicalSetup,Path.GetFullPath(args[0]),args[1],Path.GetDirectoryName(args[4]));'),
        ('if(args.Length!=4)throw new ArgumentException("usage: repo bundle sha256 fresh-output-directory");',
         'if(args.Length!=6)throw new ArgumentException("usage: repo bundle sha256 fresh-output-directory selected-setup selected-sha256");'),
        ('PathOf("authored_loading_audit"),bundlePath,args[2],preflight};',
         'PathOf("authored_loading_audit"),bundlePath,args[2],preflight,Path.GetFullPath(args[4]),args[5]};'),
    ]
    text = original
    for old, new in substitutions:
        if text.count(old) != 1: raise ValueError('Exact handoff source anchor required: ' + old[:80])
        text = text.replace(old, new, 1)
    old = 'retainedWorld.AttachS004DiagnosticStartup(gridEntity, surfaceEntity, session, boundary, setup, army, pipeline, definitions, mapRoot, args[0], args[1])'
    new = 'retainedWorld.AttachS004DiagnosticStartup(gridEntity, surfaceEntity, session, boundary, setup, army, pipeline, definitions, mapRoot, args[17], args[18])'
    if text.count(old) != 2: raise ValueError('Exact original and repeat attachment anchors required')
    text = text.replace(old, new)
    output.mkdir(parents=True, exist_ok=False)
    target = output / 'S004HistoricalDifficultyStartup.cs'
    with target.open('x') as stream: stream.write(text)
    if sha(SOURCE) != expected_sha: raise ValueError('Source host changed during generation')
    receipt = {'kind':'S004HistoricalDifficultyHandoffProjection','status':'GeneratedSourceOnly',
               'source':{'path':str(SOURCE),'sha256':expected_sha},'projection':{'path':str(target),'sha256':sha(target)},
               'source_preflight_unchanged':True,'capture_identity_unchanged':True,
               'scope':'Selected setup handoff only; caller must independently verify historical bundle and selected compiler evidence',
               'world_initialized':False,'runtime_admission':False,'mission_ready':False}
    with (output / 'receipt.json').open('x') as stream: json.dump(receipt, stream, indent=2); stream.write('\n')
    return receipt

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-sha256', required=True); parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(); print(json.dumps(generate(args.output.resolve(), args.source_sha256)))
