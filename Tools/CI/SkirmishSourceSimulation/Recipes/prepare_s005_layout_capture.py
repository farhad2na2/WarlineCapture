#!/usr/bin/env python3
"""Prepare, but do not install or execute, the S005 asset-only Unity capture."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / 'Assets/Game/Scripts/Editor/Skirmish/S004OriginalLayoutNativeJsonCapture.cs'
SOURCE_SHA256 = '77ba04b9033ff0a7dacb3b72b8595c1a1a149b3d7cb0e5f6492b82decb64cf89'
SCENARIO = ROOT / 'Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset'
LAYOUT = ROOT / 'Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishLayout_S005.asset'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def prepare(output):
    if sha(SOURCE) != SOURCE_SHA256:
        raise ValueError('Original S004 capture source identity changed')
    text = SOURCE.read_text()
    if text.count('private const int Seed = 104733;') != 1 or text.count('"S004"') != 1:
        raise ValueError('Unexpected original capture contract')
    pins = {str(p): sha(p) for p in (SOURCE, SCENARIO, LAYOUT, Path(__file__).resolve())}
    # Retain the original capture's compiler, Unity JsonUtility, assembly checks,
    # asset checks and synthetic cases; change only its mission identity and seed.
    derived = text.replace('S004', 'S005').replace('s004', 's005')
    derived = derived.replace('private const int Seed = 104733;', 'private const int Seed = 1;')
    if 'item.CatalogId == "S005"' not in derived or 'JsonUtility.ToJson(values)' not in derived:
        raise ValueError('Derived capture identity or serializer missing')
    for p, digest in pins.items():
        if sha(Path(p)) != digest:
            raise ValueError('Source changed during preparation: ' + p)
    output.mkdir(parents=True, exist_ok=False)
    capture = output / 'S005OriginalLayoutNativeJsonCapture.cs'
    capture.write_text(derived)
    receipt = {'status': 'PreparedOnly', 'source_pins': pins,
               'capture_sha256': sha(capture), 'seed': 1,
               'installed': False, 'unity_executed': False,
               'native_capture_validated': False, 'world_created': False, 'mission_ready': False}
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    return receipt


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    print(json.dumps(prepare(parser.parse_args().output.resolve())))
