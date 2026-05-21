#!/usr/bin/env python3
"""Copy the SCN-02 Main Menu layer PNGs into their proposed Unity asset paths.

Default mode is dry-run. Use --apply to copy files. Use --force to overwrite
existing Unity assets.
"""

from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path


PACKAGE_ROOT = Path(__file__).resolve().parent
REPO_ROOT = PACKAGE_ROOT.parents[2]
MANIFEST_PATH = PACKAGE_ROOT / "layer_manifest.json"


def load_manifest() -> dict:
    with MANIFEST_PATH.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def copy_layers(apply: bool, force: bool) -> int:
    manifest = load_manifest()
    copied = 0

    for layer in manifest["layers"]:
        source = PACKAGE_ROOT / layer["file"]
        destination = REPO_ROOT / layer["unityDestination"]

        if not source.exists():
            raise FileNotFoundError(f"Missing source layer: {source}")

        action = "copy"
        if destination.exists() and not force:
            action = "skip-existing"
        elif not apply:
            action = "dry-run"
        else:
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)
            copied += 1

        print(f"{action}: {source.relative_to(REPO_ROOT)} -> {destination.relative_to(REPO_ROOT)}")

    return copied


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true", help="Actually copy files into Assets.")
    parser.add_argument("--force", action="store_true", help="Overwrite existing destination files.")
    args = parser.parse_args()

    copied = copy_layers(apply=args.apply, force=args.force)
    if args.apply:
        print(f"Copied {copied} layer file(s).")
    else:
        print("Dry run only. Re-run with --apply to copy files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
