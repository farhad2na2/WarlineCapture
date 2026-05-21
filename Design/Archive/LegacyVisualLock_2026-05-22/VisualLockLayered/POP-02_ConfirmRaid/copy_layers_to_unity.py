#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

PACKAGE_ROOT = Path(__file__).resolve().parent
REPO_ROOT = PACKAGE_ROOT.parents[2]
MANIFEST_PATH = PACKAGE_ROOT / "layer_manifest.json"


def main() -> int:
    parser = argparse.ArgumentParser(description="Copy POP-02 Confirm Raid layers into Unity asset paths.")
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    copied = 0
    for layer in manifest["layers"]:
        src = PACKAGE_ROOT / layer["file"]
        dst = REPO_ROOT / layer["unityDestination"]
        if not src.exists():
            raise FileNotFoundError(src)
        if dst.exists() and not args.force:
            action = "skip-existing"
        elif not args.apply:
            action = "dry-run"
        else:
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
            copied += 1
            action = "copy"
        print(f"{action}: {src.relative_to(REPO_ROOT)} -> {dst.relative_to(REPO_ROOT)}")
    print(f"Copied {copied} layer file(s)." if args.apply else "Dry run only. Re-run with --apply to copy files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
