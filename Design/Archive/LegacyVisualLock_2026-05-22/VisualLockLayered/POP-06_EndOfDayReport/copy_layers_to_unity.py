#!/usr/bin/env python3
from pathlib import Path
import json
import shutil

ROOT = Path(__file__).resolve().parents[3]
PACK = Path(__file__).resolve().parent
manifest = json.loads((PACK / "layer_manifest.json").read_text())
for layer in manifest["layers"]:
    src = PACK / layer["file"]
    dst = ROOT / layer["unityDestination"]
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(src, dst)
    print(f"{src.relative_to(ROOT)} -> {dst.relative_to(ROOT)}")
