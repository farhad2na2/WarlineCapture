#!/usr/bin/env python3
from pathlib import Path
import json, shutil
ROOT = Path(__file__).resolve().parents[3]
PACK = Path(__file__).resolve().parent
manifest = json.loads((PACK / "layer_manifest.json").read_text())
for layer in manifest["layers"]:
    src = PACK / "layers" / layer["file"]
    dst = ROOT / layer["unityDestination"]
    print(f"{src} -> {dst}")
    if "--apply" in __import__("sys").argv:
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src, dst)
