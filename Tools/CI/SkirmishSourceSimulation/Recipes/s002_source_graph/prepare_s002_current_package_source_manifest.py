"""Create an isolated S002 source-build manifest for current package diagnostics."""
from __future__ import annotations

import copy
import json
from pathlib import Path

BASE_MANIFEST = Path("Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json")
BASE_GRAPH = Path("T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/source-config-graph.json")
BASE_PROOF = Path("T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/graph-proof.json")
BASE_GENERATOR = Path("Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_authored_config_graph.py")
PREPARER = Path("Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/prepare_inputs.py")
GENERATOR = Path("Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_selected_map_graph_attestation.py")
GRID_EXTENSION = Path("Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/run_s002_pinned_physical_input_projection.py")
PACKAGE_READER = Path("Tools/CI/s002_current_source_diagnostic_package.py")
SOURCE_PACKAGE = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T2/package.json")
MAP_GENERATOR = Path("Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/generate_current.py")
MAP_RECIPE = Path("Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/generate.py")
GRAPH_HELPERS = Path("Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_typed_config_graph.py")
OUTPUT_ID = "S002CurrentSelectedMapGraphBuildAttestation.cs"
DEFINE = "S002_CURRENT_PACKAGE_DIAGNOSTIC"


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def prepare(repo: Path, output: Path) -> dict:
    repo = Path(repo).resolve(strict=True)
    output = Path(output).absolute()
    need(not output.exists(), "Fresh isolated diagnostic manifest output required")
    base = json.loads((repo / BASE_MANIFEST).read_text(encoding="utf-8"))
    need(base.get("schema_version") == 1 and base.get("profile") == {
        "mission": "S002", "seed": 104731, "numeric_runtime": "InstalledMono"},
        "Canonical S002 source manifest identity required")
    manifest = copy.deepcopy(base)
    defines = manifest.get("compiler_defines")
    need(isinstance(defines, list) and DEFINE not in defines, "Fresh diagnostic compiler define required")
    manifest["compiler_defines"] = [*defines, DEFINE]
    with (repo / BASE_GRAPH).open(encoding="utf-8") as stream:
        base_graph = json.load(stream)
    pins = base_graph.get("source_pins")
    need(isinstance(pins, dict) and len(pins) == 336, "Exact current T3 graph source pin set required")
    graph_pin_inputs = []
    for path in pins:
        source = Path(path).resolve(strict=True)
        need(source.is_relative_to(repo) and source.is_file(), "T3 graph pin must remain in current repository")
        graph_pin_inputs.append(source.relative_to(repo).as_posix())
    graph_inputs = [BASE_GRAPH.as_posix(), BASE_PROOF.as_posix(), BASE_GENERATOR.as_posix(),
                    GRAPH_HELPERS.as_posix(), PREPARER.as_posix(), MAP_GENERATOR.as_posix(),
                    MAP_RECIPE.as_posix(), GRID_EXTENSION.as_posix(), *graph_pin_inputs]
    import importlib.util
    spec = importlib.util.spec_from_file_location("s002_prepare_inputs", repo / PREPARER)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    for relative in module.SOURCES:
        path = Path(relative)
        if path not in (repo / BASE_MANIFEST,):
            graph_inputs.append(path.as_posix())
    for relative in module.ASSETS:
        graph_inputs.extend([relative, relative + ".meta"])
    # The generated attestation is allowed to bind a derived package only
    # after the existing strict reader has revalidated the current evidence.
    package_reader_spec = importlib.util.spec_from_file_location("s002_package_reader", repo / PACKAGE_READER)
    package_reader = importlib.util.module_from_spec(package_reader_spec)
    assert package_reader_spec and package_reader_spec.loader
    package_reader_spec.loader.exec_module(package_reader)
    package_bytes = (repo / SOURCE_PACKAGE).read_bytes()
    package_info = package_reader.load_current_s002_package(
        repo / SOURCE_PACKAGE, package_reader._sha(package_bytes), repo)
    graph_inputs.extend([PACKAGE_READER.as_posix(), SOURCE_PACKAGE.as_posix()])
    map_helpers = sorted(path.relative_to(repo).as_posix()
                         for path in (repo / "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers").iterdir()
                         if path.is_file())
    source = {
        "id": OUTPUT_ID,
        "kind": "current_original",
        "generator": GENERATOR.as_posix(),
        "generator_inputs": sorted(set(graph_inputs)),
        "generator_helpers": map_helpers,
        "generator_output": OUTPUT_ID,
        "output": OUTPUT_ID,
    }
    need(not any(row.get("id") == OUTPUT_ID or row.get("output") == OUTPUT_ID for row in manifest["sources"]),
         "Diagnostic source output must be unique")
    manifest["sources"].append(source)
    output.parent.mkdir(parents=True, exist_ok=True)
    raw = (json.dumps(manifest, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    manifest_inputs = sorted(set([*source["generator_inputs"], *source["generator_helpers"],
                                  BASE_MANIFEST.as_posix(), GENERATOR.as_posix()]))
    input_pins = {}
    for relative in manifest_inputs:
        candidate = repo / relative
        current = repo
        for component in Path(relative).parts:
            current = current / component
            need(not current.is_symlink(), "Symlink in diagnostic manifest proof input: " + str(current))
        path = candidate.resolve(strict=True)
        need(path.is_relative_to(repo) and path.is_file(),
             "Diagnostic manifest proof input must be a regular in-repository file: " + relative)
        input_pins[str(path)] = __import__("hashlib").sha256(path.read_bytes()).hexdigest()
    output.write_bytes(raw)
    proof_path = output.with_name("source_manifest.proof.json")
    proof = {
        "schema_version": 1,
        "kind": "S002CurrentPackageDiagnosticSourceManifestProof",
        "status": "Passed",
        "manifest": {"path": str(output.resolve()), "sha256": __import__("hashlib").sha256(raw).hexdigest(),
                     "bytes": len(raw)},
        "generator": {"path": GENERATOR.as_posix(),
                      "sha256": __import__("hashlib").sha256((repo / GENERATOR).read_bytes()).hexdigest()},
        "base_manifest": {"path": BASE_MANIFEST.as_posix(),
                          "sha256": __import__("hashlib").sha256((repo / BASE_MANIFEST).read_bytes()).hexdigest()},
        "package_validation": {"path": SOURCE_PACKAGE.as_posix(),
                               "sha256": __import__("hashlib").sha256((repo / SOURCE_PACKAGE).read_bytes()).hexdigest(),
                               "validated_pin_count": len(package_info["source_pins"])},
        "generation_input_count": len(source["generator_inputs"]),
        "generator_helper_count": len(source["generator_helpers"]),
        "source_pin_count": len(input_pins),
        "source_pins": dict(sorted(input_pins.items())),
        "default_manifest_unchanged": True,
        "compiled_setup": False,
        "world_initialized": False,
        "mission_ready": False,
    }
    proof_raw = (json.dumps(proof, indent=2, sort_keys=True, ensure_ascii=False) + "\n").encode("utf-8")
    if proof_path.exists():
        need(proof_path.read_bytes() == proof_raw, "Existing diagnostic manifest proof differs")
    else:
        proof_path.write_bytes(proof_raw)
    for path_text, expected in input_pins.items():
        need(__import__("hashlib").sha256(Path(path_text).read_bytes()).hexdigest() == expected,
             "Diagnostic manifest input changed during generation: " + path_text)
    need(output.read_bytes() == raw, "Diagnostic source manifest changed during proof generation")
    return {"path": str(output), "sha256": __import__("hashlib").sha256(raw).hexdigest(),
            "source_count": len(manifest["sources"]), "compiler_define": DEFINE,
            "generation_inputs": len(source["generator_inputs"]),
            "proof_path": str(proof_path), "proof_sha256": __import__("hashlib").sha256(proof_raw).hexdigest(),
            "default_manifest_unchanged": True, "world_initialized": False,
            "mission_ready": False}
