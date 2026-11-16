#!/usr/bin/env python3
"""Generate and pin the isolated source copies for the S073 shell-owner profile."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
GENERATOR = Path(__file__).with_name("generate.py")
PROFILE_ID = "S073UiShellStateOwnerOnCreateV1_NonNative"
DEFINE = "WARLINE_SOURCE_S073_UI_SHELL_OWNER_PROFILE"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_generator():
    spec = importlib.util.spec_from_file_location("s073_ui_shell_state_owner_generator", GENERATOR)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def prove(manifest_path: Path, output_dir: Path, repo: Path = ROOT) -> dict:
    manifest_path = manifest_path.resolve(strict=True)
    repo = repo.resolve(strict=True)
    manifest = json.loads(manifest_path.read_bytes())
    profile = manifest.get("experimental_source_profile", {})
    if profile.get("id") != PROFILE_ID or DEFINE not in manifest.get("compiler_defines", []):
        raise ValueError("Exact derived non-native shell-owner profile required")
    if "Diagnostic-only" not in profile.get("scope", "") or "matching explicit run/environment admission" not in profile.get("scope", ""):
        raise ValueError("Diagnostic-only explicit-admission profile scope required")
    source_rows = {row.get("id"): row for row in manifest.get("sources", [])}
    provenance_rows = {row.get("id"): row for row in manifest.get("derived_source_row_provenance", [])}
    for source_id in ("292F2_SourceS073LegacyNormalRun.cs", "112_MetadataSystemTypes.cs"):
        provenance = provenance_rows.get(source_id)
        if not isinstance(provenance, dict) or not isinstance(provenance.get("original_row"), dict):
            raise ValueError("Original source row provenance is missing: " + source_id)
        original = provenance["original_row"]
        original_digest = hashlib.sha256(json.dumps(original, sort_keys=True,
            separators=(",", ":"), ensure_ascii=False).encode("utf-8")).hexdigest()
        if provenance.get("original_row_sha256") != original_digest:
            raise ValueError("Original source row provenance digest mismatch: " + source_id)
        derived = source_rows.get(source_id)
        if not isinstance(derived, dict):
            raise ValueError("Transformed source row is missing: " + source_id)
        if source_id == "112_MetadataSystemTypes.cs":
            for key in ("role", "contains_original_gameplay", "manual_scope", "scope_declaration_is_not_semantic_proof"):
                if derived.get(key) != original.get(key):
                    raise ValueError("Original adapter scope metadata changed: " + key)
            if original.get("role") != "engine_boundary" or original.get("manual_scope") != "Finite source metadata marker types":
                raise ValueError("Exact original metadata adapter role/scope proof required")
        if source_id == "292F2_SourceS073LegacyNormalRun.cs" and (
                original.get("kind") != "native_direct" or
                original.get("path") != "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs"):
            raise ValueError("Exact original S073 runner source row provenance required")
    generated_rows = [row for row in manifest.get("sources", [])
                      if row.get("generator") == str(GENERATOR.relative_to(repo))]
    if len(generated_rows) != 5:
        raise ValueError("Expected exactly five generated source-copy manifest rows")
    output_dir = Path(output_dir)
    if output_dir.exists():
        raise FileExistsError("Source proof output directory must be fresh: " + str(output_dir))
    generator = load_generator()
    result = generator.generate(repo, output_dir)
    actual = result["outputs"]
    expected = {row["generator_output"]: row.get("output") for row in generated_rows}
    if expected != {name: name for name in actual}:
        raise ValueError("Derived manifest output names do not match the recipe outputs")
    for name, digest in actual.items():
        target = output_dir / name
        if not target.is_file() or sha(target) != digest:
            raise ValueError("Generated output pin mismatch: " + name)
    source_pins = result["source_pins"]
    changed = [path for path, digest in source_pins.items()
               if not Path(path).is_file() or sha(Path(path)) != digest]
    if changed:
        raise ValueError("Generated source input changed: " + ", ".join(changed[:8]))
    proof = {
        "kind": "S073UiShellStateOwnerDerivedSourceProof",
        "status": "Passed_DerivedSourcesOnly_NoBuildOrRun",
        "profile_id": PROFILE_ID,
        "scope": result["scope"],
        "manifest": {"path": str(manifest_path), "sha256": sha(manifest_path)},
        "generator": {"path": str(GENERATOR.resolve(strict=True)), "sha256": sha(GENERATOR)},
        "proof_recipe": {"path": str(Path(__file__).resolve(strict=True)), "sha256": sha(Path(__file__).resolve(strict=True))},
        "source_pins": source_pins,
        "outputs": actual,
        "authorized_transformations": result["authorized_transformations"],
        "host_binding": result["host_binding"],
        "manifest_rows": sorted(row["id"] for row in generated_rows),
        "derived_source_row_provenance": manifest["derived_source_row_provenance"],
        "compile_invoked": False,
        "host_build_invoked": False,
        "host_run_invoked": False,
        "native_runtime_claimed": False,
        "player_or_ui_readiness_claimed": False,
    }
    if not output_dir.is_dir():
        raise ValueError("Source generator did not create its output directory")
    (output_dir / "source-proof.json").write_text(
        json.dumps(proof, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return proof


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--repo", type=Path, default=ROOT)
    args = parser.parse_args()
    proof = prove(args.manifest, args.output_dir, args.repo)
    print(json.dumps({"kind": proof["kind"], "status": proof["status"],
                      "manifest_sha256": proof["manifest"]["sha256"],
                      "output_count": len(proof["outputs"]),
                      "source_pin_count": len(proof["source_pins"]),
                      "proof_path": str((args.output_dir / "source-proof.json").resolve())}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
