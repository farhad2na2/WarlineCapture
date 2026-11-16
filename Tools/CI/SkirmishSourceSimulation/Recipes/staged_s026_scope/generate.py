#!/usr/bin/env python3
"""Generate an isolated S026 declared-ground manifest with the original Editor catalog scope.

The generated source files are byte-preserving copies of the two catalog owners and
the existing availability scope, except for file-local UNITY_EDITOR framing and the
scope's managed-only DestroyImmediate adaptation. This is a source-host portability
overlay; it does not alter canonical game source or admit/run a mission.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
DEFAULT_MANIFEST = ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.s026.plan.json"
CATALOG_ROWS = {
    "015_SkirmishBattleCatalogConfig.cs": "Assets/Game/Scripts/Configs/SkirmishBattleCatalogConfig.cs",
    "023_SkirmishExpansionCatalogConfig.cs": "Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogConfig.cs",
}
SCOPE_PATH = "Assets/Game/Scripts/Editor/Skirmish/SkirmishGenericMissionAvailabilityScope.cs"
FACTORY_PATH = "Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogFactory.cs"
DESTROY_CALL = "UnityEngine.Object.DestroyImmediate(candidate)"
ORIGINAL_CLOSURE = {
    "S026_OriginalContentAccessRuntime": "Assets/Game/Scripts/Runtime/Campaign/ContentAccessRuntime.cs",
    "S026_OriginalContentAccessAuthority": "Assets/Game/Scripts/Missions/Contracts/ContentAccessAuthority.cs",
    "S026_StagedLaunchTooling": "Tools/CI/SkirmishSourceSimulation/Recipes/staged_s026_handoff/SourceStagedLaunch.cs",
}
RESOLVER_GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/generate_expanded_resolver_source.py"
LAUNCH_GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/generate_expanded_launch_source.py"
LAUNCH_SOURCE = "Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchProjection.cs"
LAUNCH_TRYGET_SOURCE = "Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs"
MEMBER_EXTRACTOR = "Tools/CI/SkirmishSourceSimulation/Recipes/operation_map_publication/regenerate_config_metadata.py"


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def dump(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def extract_braced(text: str, start: int) -> str:
    """Return a declaration's balanced-brace extent, ignoring C# comments and literals."""
    opening = text.find("{", start)
    require(opening >= 0, "Braced declaration required")
    depth = 0
    i = opening
    state = "code"
    while i < len(text):
        ch = text[i]
        nxt = text[i + 1] if i + 1 < len(text) else ""
        if state == "code":
            if ch == "/" and nxt == "/": state = "line"; i += 2; continue
            if ch == "/" and nxt == "*": state = "block"; i += 2; continue
            if ch == '"': state = "string"; i += 1; continue
            if ch == "'": state = "char"; i += 1; continue
            if ch == "{": depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0: return text[start:i + 1]
        elif state == "line":
            if ch == "\n": state = "code"
        elif state == "block":
            if ch == "*" and nxt == "/": state = "code"; i += 2; continue
        elif state in ("string", "char"):
            if ch == "\\": i += 2; continue
            if (state == "string" and ch == '"') or (state == "char" and ch == "'"): state = "code"
        i += 1
    raise ValueError("Unclosed original C# declaration")


def generate(manifest_path: Path, output_dir: Path) -> dict:
    manifest_path = manifest_path.resolve(strict=True)
    output_dir = output_dir.absolute()
    require(manifest_path.is_relative_to(ROOT), "Repository-owned S026 manifest required")
    require(not output_dir.exists(), "Fresh output directory required")
    require(output_dir.is_relative_to(ROOT), "Repository-owned output required for source compilation")

    manifest_raw = manifest_path.read_bytes()
    manifest = json.loads(manifest_raw)
    require(manifest.get("schema_version") == 1 and
            manifest.get("runtime_backend") == "TypedGroundS026V1",
            "Exact S026 declared-ground source manifest required")
    rows = manifest.get("sources")
    require(isinstance(rows, list), "Manifest source list required")
    by_id = {row.get("id"): row for row in rows if isinstance(row, dict)}
    require(len(by_id) == len(rows), "Unique source row IDs required")
    for row_id, source_path in CATALOG_ROWS.items():
        row = by_id.get(row_id)
        require(row is not None and row.get("kind") == "native_direct" and row.get("path") == source_path,
                "Exact original catalog manifest row required: " + row_id)
    require(not any(row.get("path") == SCOPE_PATH for row in rows),
            "Base manifest unexpectedly already includes the generic availability scope")

    source_inputs = {str(manifest_path): sha(manifest_raw)}
    generated = output_dir / "generated"
    output_dir.mkdir(parents=True)
    generated.mkdir()

    def read_source(relative: str) -> bytes:
        path = (ROOT / relative).resolve(strict=True)
        require(path.is_relative_to(ROOT) and path.is_file(), "Repository source required: " + relative)
        raw = path.read_bytes()
        source_inputs[str(path)] = sha(raw)
        return raw

    def framed(raw: bytes, label: str) -> bytes:
        require(raw.endswith(b"\n"), "Expected newline-terminated original source: " + label)
        # Preprocessor symbols are already file-local in C#; placing #undef at EOF
        # is illegal (CS1032) and would not improve isolation between source files.
        return b"#define UNITY_EDITOR\n" + raw

    overlay_files = {}
    for row_id, source_path in CATALOG_ROWS.items():
        raw = read_source(source_path)
        text = raw.decode("utf-8")
        require(text.count("public sealed class SkirmishBattleCatalogConfig") == (1 if row_id.startswith("015_") else 0),
                "Original battle catalog declaration mismatch")
        require(text.count("public sealed partial class SkirmishExpansionCatalogConfig") == (1 if row_id.startswith("023_") else 0),
                "Original expansion catalog declaration mismatch")
        name = Path(source_path).name
        target = generated / name
        target.write_bytes(framed(raw, source_path))
        overlay_files[source_path] = target

    scope_raw = read_source(SCOPE_PATH)
    scope_text = scope_raw.decode("utf-8")
    require(scope_text.count("public sealed class SkirmishGenericMissionAvailabilityScope") == 1,
            "Exact original generic availability scope required")
    require(scope_text.count(DESTROY_CALL) == 1,
            "Expected single managed cleanup adaptation site")
    require("#if UNITY_EDITOR" in scope_text and "#endif" in scope_text,
            "Original Editor-only scope guard required")
    scope_text = scope_text.replace(DESTROY_CALL, "candidate=null", 1)
    scope_output = generated / Path(SCOPE_PATH).name
    scope_output.write_bytes(framed(scope_text.encode("utf-8"), SCOPE_PATH))

    factory_raw = read_source(FACTORY_PATH)
    factory_text = factory_raw.decode("utf-8")
    factory_marker = "public static class SkirmishExpansionCatalogFactory"
    require(factory_text.count(factory_marker) == 1, "Exact original factory lookup class required")
    factory_start = factory_text.index(factory_marker)
    factory_body = extract_braced(factory_text, factory_start)
    methods = []
    method_markers = (
        "public static bool TryGetDefinition(SkirmishExpansionAuthoredSet set, string catalogId, int version,",
        "public static bool TryGetDefinition(\n            SkirmishExpansionAuthoredSet set,\n            string catalogId,",
    )
    for marker in method_markers:
        require(factory_body.count(marker) == 1, "Exact original factory TryGetDefinition overload required")
        methods.append(extract_braced(factory_body, factory_body.index(marker)))
    factory_projection = (
        "using Game.Skirmish.Contracts;\nnamespace Game.Configs\n{\n" +
        "    public static class SkirmishExpansionCatalogFactory\n    {\n" +
        "\n".join("        " + line if line else "" for method in methods for line in method.splitlines()) +
        "\n    }\n}\n"
    ).encode("utf-8")
    factory_output = generated / "SkirmishExpansionCatalogFactory.Lookup.cs"
    factory_output.write_bytes(factory_projection)

    def run_generator(relative: str, folder: Path) -> dict:
        generator_path = ROOT / relative
        source_bytes = generator_path.read_bytes()
        source_inputs[str(generator_path)] = sha(source_bytes)
        spec = importlib.util.spec_from_file_location("staged_s026_" + Path(relative).stem, generator_path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        proof = module.generate(ROOT, folder)
        source_inputs[str(generator_path)] = sha(generator_path.read_bytes())
        for relative_source, expected in proof.get("source_pins", {}).items():
            pinned_source = (ROOT / relative_source).resolve(strict=True)
            require(pinned_source.is_relative_to(ROOT) and sha(pinned_source.read_bytes()) == expected,
                    "Existing source generator pin mismatch: " + relative_source)
            source_inputs[str(pinned_source)] = expected
        return {"generator": str(generator_path), "generator_sha256": source_inputs[str(generator_path)],
                "generation_proof": proof}

    resolver_folder = generated / "expanded-resolver"
    resolver_proof = run_generator(RESOLVER_GENERATOR, resolver_folder)
    resolver_output = resolver_folder / "SourceExpandedLaunchResolver.cs"
    resolver_id = "S026_OriginalExpandedLaunchResolver"
    require(resolver_id not in by_id, "Expanded resolver manifest row ID collision")
    rows.append({"id": resolver_id, "kind": "native_direct",
                 "path": resolver_output.relative_to(ROOT).as_posix(),
                 "require_declarations": ["public static class SkirmishExpandedLaunchResolver"],
                 "scope": "Existing original-resolver generator output; null-only visual registry guard; no Unity visual registry input."})
    launch_folder = generated / "expanded-launch-proof"
    launch_proof = run_generator(LAUNCH_GENERATOR, launch_folder)
    launch_output = launch_folder / "SourceExpandedLaunchProjection.cs"
    require(launch_output.is_file(), "Original launch projection generator output required")
    launch_generated_hash = sha(launch_output.read_bytes())
    require(launch_proof["generation_proof"]["outputs"].get("SourceExpandedLaunchProjection.cs") == launch_generated_hash,
            "Original launch projection generation proof mismatch")
    launch_projection_id = "S026_OriginalExpandedLaunchProjection"
    require(launch_projection_id not in by_id, "Expanded launch projection manifest row ID collision")
    rows.append({"id": launch_projection_id, "kind": "native_direct",
                 "path": launch_output.relative_to(ROOT).as_posix(),
                 "require_declarations": ["public static class SkirmishExpandedLaunchProjection"],
                 "scope": "Existing original launch generator output; null-only managed registry; no native visual registry input."})

    extractor_path = ROOT / MEMBER_EXTRACTOR
    extractor_bytes = extractor_path.read_bytes()
    source_inputs[str(extractor_path)] = sha(extractor_bytes)
    extractor_spec = importlib.util.spec_from_file_location("staged_s026_member_extractor", extractor_path)
    extractor = importlib.util.module_from_spec(extractor_spec)
    extractor_spec.loader.exec_module(extractor)
    launch_tryget_path = (ROOT / LAUNCH_TRYGET_SOURCE).resolve(strict=True)
    launch_tryget_raw = launch_tryget_path.read_bytes()
    launch_tryget_text = launch_tryget_raw.decode("utf-8").replace("\r\n", "\n")
    source_inputs[str(launch_tryget_path)] = sha(launch_tryget_raw)
    tryget_member = extractor.member(launch_tryget_text, "TryGet")
    tryget_source = ("using Game.Components;using Unity.Entities;namespace Game.Composition{\n" +
                     "internal static class SkirmishLaunchProjection{\n" + tryget_member + "\n}}\n")
    tryget_folder = generated / "original-launch-tryget"
    tryget_folder.mkdir()
    tryget_output = tryget_folder / "SourceStagedLaunchTryGet.cs"
    with tryget_output.open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(tryget_source)
    tryget_id = "S026_OriginalLaunchTryGet"
    require(tryget_id not in by_id, "Original launch TryGet manifest row ID collision")
    rows.append({"id": tryget_id, "kind": "native_direct",
                 "path": tryget_output.relative_to(ROOT).as_posix(),
                 "require_declarations": ["internal static class SkirmishLaunchProjection"],
                 "scope_overlay_origin": LAUNCH_TRYGET_SOURCE,
                 "scope": "Exact original launch TryGet member extracted with the repository member lexer; no PrepareMap or scene launch."})
    launch_tryget_proof = {
        "kind": "OriginalLaunchTryGetExtraction",
        "source_path": str(launch_tryget_path),
        "source_sha256": source_inputs[str(launch_tryget_path)],
        "member_extractor": str(extractor_path),
        "member_extractor_sha256": source_inputs[str(extractor_path)],
        "member": "TryGet",
        "generated_path": str(tryget_output),
        "generated_sha256": sha(tryget_output.read_bytes()),
        "scope": "Original read-only launch state lookup; no launch queue or scene action.",
    }

    current_paths = {row.get("path") for row in rows if isinstance(row, dict)}
    for source_id, source_path in ORIGINAL_CLOSURE.items():
        require(source_path not in current_paths, "Original closure source already present in base manifest: " + source_path)
        source = (ROOT / source_path).resolve(strict=True)
        require(source.is_relative_to(ROOT) and source.is_file(), "Original closure source missing: " + source_path)
        raw = source.read_bytes()
        source_inputs[str(source)] = sha(raw)
        class_requirements = {
            "S026_OriginalUnitPrefabRegistryConfig": ["public class UnitPrefabRegistryAuthoringConfig"],
            "S026_OriginalExpandedCopyProjection": ["public static class SkirmishExpandedCopyProjection"],
            "S026_OriginalExpandedLaunchResolver": ["public static class SkirmishExpandedLaunchResolver"],
            "S026_OriginalContentAccessRuntime": ["public static class ContentAccessRuntime"],
            "S026_OriginalContentAccessAuthority": ["public sealed class ContentAccessAuthority"],
            "S026_StagedLaunchTooling": ["internal sealed class SourceStagedLaunch"],
        }[source_id]
        source_text = raw.decode("utf-8")
        require(all(signature in source_text for signature in class_requirements),
                "Exact original closure declaration required: " + source_id)
        rows.append({"id": source_id, "kind": "native_direct", "path": source_path,
                     "require_declarations": class_requirements,
                     "scope": "Original declared-ground host closure source; source-only staged S026 tool, no native or player claim."})
        current_paths.add(source_path)

    factory_id = "S026_OriginalExpansionFactoryLookup"
    require(factory_id not in by_id, "Factory lookup manifest row ID collision")
    rows.append({"id": factory_id, "kind": "native_direct",
                 "path": factory_output.relative_to(ROOT).as_posix(),
                 "require_declarations": ["public static class SkirmishExpansionCatalogFactory"],
                 "scope_overlay_origin": FACTORY_PATH,
                 "scope": "Exact original factory TryGetDefinition overload bodies only; no synthetic factory behavior."})

    for row_id, source_path in CATALOG_ROWS.items():
        by_id[row_id]["path"] = overlay_files[source_path].relative_to(ROOT).as_posix()
        by_id[row_id]["scope_overlay_origin"] = source_path
    scope_id = "StagedS026GenericMissionAvailabilityScope"
    require(scope_id not in by_id, "Staged availability scope row ID collision")
    rows.append({
        "id": scope_id,
        "kind": "native_direct",
        "path": scope_output.relative_to(ROOT).as_posix(),
        "require_declarations": ["public sealed class SkirmishGenericMissionAvailabilityScope"],
        "scope_overlay_origin": SCOPE_PATH,
        "scope": "Original Editor-memory-only authored catalog admission scope; isolated managed-host adaptation only at candidate destruction.",
    })
    manifest["experimental_only"] = True
    manifest["staged_scope_overlay"] = {
        "kind": "S026ManagedEditorScopePortabilityOverlay",
        "canonical_manifest_modified": False,
        "source_host_only": True,
        "mission_run": False,
        "original_catalog_class_bodies": True,
        "file_local_unity_editor": True,
        "managed_only_destroy_immediate_adaptation": True,
        "original_factory_lookup_projection": factory_output.relative_to(ROOT).as_posix(),
        "original_declared_ground_closure": ORIGINAL_CLOSURE,
        "expanded_resolver_generation": resolver_proof,
        "expanded_launch_projection_generation": launch_proof,
        "launch_tryget_generation": launch_tryget_proof,
        "scope_row": scope_id,
    }

    manifest_out = output_dir / "source-manifest.json"
    dump(manifest_out, manifest)
    generated_hashes = {str(path): sha(path.read_bytes()) for path in sorted(generated.rglob("*")) if path.is_file()}
    receipt = {
        "schema_version": 1,
        "kind": "S026StagedScopePortabilityOverlayGeneration",
        "status": "GeneratedSourceOnly",
        "base_manifest": {"path": str(manifest_path), "sha256": sha(manifest_raw)},
        "source_inputs": source_inputs,
        "generated_sources": generated_hashes,
        "derived_manifest": {"path": str(manifest_out), "sha256": sha(manifest_out.read_bytes())},
        "catalog_rows_repointed": sorted(CATALOG_ROWS),
        "original_closure_sources": ORIGINAL_CLOSURE,
        "expanded_resolver_generation": resolver_proof,
        "expanded_launch_projection_generation": launch_proof,
        "launch_tryget_generation": launch_tryget_proof,
        "factory_owner_source": {str((ROOT / FACTORY_PATH).resolve()): source_inputs[str((ROOT / FACTORY_PATH).resolve())]},
        "scope_row_added": scope_id,
        "only_source_adaptation": "UnityEngine.Object.DestroyImmediate(candidate) -> candidate=null",
        "compile_attempted": False,
        "compile_failures_suppressed": False,
        "world_allocated": False,
        "mission_run": False,
        "native_parity": False,
        "mission_ready": False,
    }
    dump(output_dir / "generation-receipt.json", receipt)
    require(manifest_path.read_bytes() == manifest_raw, "Base manifest changed during generation")
    for name, expected in source_inputs.items():
        require(Path(name).is_file() and sha(Path(name).read_bytes()) == expected,
                "Source input changed during generation: " + name)
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    result = generate(args.manifest, args.output_dir)
    print(json.dumps({"status": result["status"], "derived_manifest": result["derived_manifest"],
                      "scope_row_added": result["scope_row_added"], "mission_run": False}, indent=2))
