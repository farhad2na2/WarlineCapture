"""Generate a build-bound digest for the current S002 selected-map graph.

The selected-map graph is independently produced by the existing
selected_operation_map preparation owner. This recipe only embeds the
deterministic output digest into the diagnostic source build; it adds no map
selection policy.
"""
from __future__ import annotations

import hashlib
import importlib.util
import json
from pathlib import Path
import re
import tempfile


PREPARER = Path("Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/prepare_inputs.py")
BASE_GRAPH = Path("T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/source-config-graph.json")
BASE_PROOF = Path("T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/graph-proof.json")
BASE_GENERATOR = Path("Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_authored_config_graph.py")
CATALOG = "Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset"
MAP = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset"
OUTPUT = "S002CurrentSelectedMapGraphBuildAttestation.cs"
SOURCE_PACKAGE = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T2/package.json")
DIAGNOSTIC_DIR = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T12")
DERIVED_PACKAGE = DIAGNOSTIC_DIR / "package.json"
DERIVED_GRAPH = DIAGNOSTIC_DIR / "source-config-graph.json"
DERIVED_GRAPH_PROOF = DIAGNOSTIC_DIR / "graph-preparation-proof.json"
REFRESHED_SOURCE_PACKAGE = Path("T Logs/SkirmishProduction/s002-refreshed-source-diagnostic-input-20261105-T1/package.json")
REFRESHED_DIAGNOSTIC_DIR = Path("T Logs/SkirmishProduction/s002-refreshed-source-diagnostic-input-20261105-T3")
GRID_EXTENSION = Path("Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/run_s002_pinned_physical_input_projection.py")
GRID_PATH = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"
GRID_GUID = "b201000000000000000000000000000b"
GRID_TYPE = "GridAuthoringSceneConfigAsset"
SURFACE_PATH = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset"


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def need(ok: bool, why: str) -> None:
    if not ok:
        raise ValueError(why)


def merge_pins(*sources: dict[str, str]) -> dict[str, str]:
    result: dict[str, str] = {}
    for source in sources:
        for path, digest in source.items():
            prior = result.get(path)
            need(prior is None or prior == digest, "Conflicting graph/generator source pin: " + path)
            result[path] = digest
    return dict(sorted(result.items()))


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec is not None and spec.loader is not None
    spec.loader.exec_module(module)
    return module


def generate(repo: Path, output: Path, refreshed: bool = False) -> dict:
    repo = Path(repo).resolve(strict=True)
    generator_path = Path(__file__).resolve(strict=True)
    generator_sha = sha(generator_path.read_bytes())
    output = Path(output).absolute()
    need(not output.exists(), "Fresh graph-attestation output required")
    base_path, proof_path = repo / BASE_GRAPH, repo / BASE_PROOF
    for path in (base_path, proof_path, repo / PREPARER, repo / BASE_GENERATOR):
        need(path.is_file() and not path.is_symlink(), "Pinned current graph/preparer input required: " + str(path))
    base_raw, proof_raw = base_path.read_bytes(), proof_path.read_bytes()
    base_sha, base_proof_sha = sha(base_raw), sha(proof_raw)
    base = json.loads(base_raw)
    base_proof = json.loads(proof_raw)
    need(base_proof.get("kind") == "S002CurrentAuthoredConfigGraph" and
         base_proof.get("status") == "Passed" and
         base_proof.get("graph") == {"path": str(base_path), "sha256": base_sha} and
         base_proof.get("compiled_setup") is False and base_proof.get("world_initialized") is False and
         base_proof.get("mission_ready") is False,
         "Exact passing T3 authored graph proof required")
    generator_info = base_proof.get("generator")
    base_generator = load_module(repo / BASE_GENERATOR, "s002_base_graph_owner")
    need(isinstance(generator_info, dict) and
         generator_info.get("path") == BASE_GENERATOR.as_posix() and
         generator_info.get("sha256") == sha((repo / BASE_GENERATOR).read_bytes()) and
         generator_info.get("unchanged_before_and_after_generation") is True,
         "T3 graph proof must bind current S002 graph generator")
    with tempfile.TemporaryDirectory(prefix="s002-base-graph-replay-") as temp:
        replay_dir = Path(temp) / "graph"
        base_generator.generate(repo, replay_dir)
        replay_graph = (replay_dir / "source-config-graph.json").read_bytes()
        replay_proof = json.loads((replay_dir / "graph-proof.json").read_bytes())
    need(replay_graph == base_raw,
         "T3 graph is not the exact output of its current pinned source generator")
    replay_proof["graph"]["path"] = str(base_path)
    need(replay_proof == base_proof,
         "T3 graph proof differs from independently regenerated current proof")
    base_pins = base.get("source_pins")
    need(isinstance(base_pins, dict) and len(base_pins) == base_proof.get("source_pin_count"),
         "Complete exact T3 graph source-pin set required")
    for path_text, expected in base_pins.items():
        path = Path(path_text)
        need(path.is_absolute() and path.is_file() and not path.is_symlink() and
             sha(path.read_bytes()) == expected,
             "Current T3 graph source pin differs: " + path_text)
    preparer = load_module(repo / PREPARER, "s002_map_preparation_owner")
    with tempfile.TemporaryDirectory(prefix="s002-current-map-graph-") as temp:
        generated = Path(temp) / "prepared"
        proof = preparer.generate(repo, generated, repo, base_path, None)
        enriched_path = generated / "source-config-graph.json"
        extraction_path = generated / "source-extraction.json"
        selected_map_graph_raw = enriched_path.read_bytes()
        extraction_raw = extraction_path.read_bytes()
    enriched = json.loads(selected_map_graph_raw)
    base_nodes = {row["source_path"]: row for row in base["nodes"]}
    enriched_nodes = {row["source_path"]: row for row in enriched["nodes"]}
    need(all(enriched_nodes.get(path) == row for path, row in base_nodes.items()),
         "Current selected-map preparation altered T3 base nodes")
    need(len(enriched_nodes) == len(base_nodes) + 2,
         "Selected-map owner must add exactly the baseline preset and Match catalog nodes")
    for relative, expected_type in (("Assets/Game/Resources/SkirmishBaseAssault.asset", "SkirmishPresetConfig"),
                                    (CATALOG, "OperationMapCatalogConfig"),
                                    (MAP, "OperationMapDefinition")):
        rows = [row for row in enriched["nodes"] if row.get("source_path") == relative and
                row.get("source_type") == expected_type]
        need(len(rows) == 1, "Exact current selected-map graph node required: " + relative)
    selected_map_graph_sha = sha(selected_map_graph_raw)
    extraction = json.loads(extraction_raw)
    outputs = extraction.get("outputs")
    need(isinstance(outputs, dict) and outputs and
         all(Path(path).name in {"SourceSelectedOperationMap.cs", "SourceSelectedMapCatalogEntries.cs"}
             for path in outputs),
         "Current selected-map producer outputs required")
    output_hashes = {Path(path).name: digest for path, digest in outputs.items()}
    need(len(output_hashes) == len(outputs) and all(re.fullmatch(r"[0-9a-f]{64}", digest)
         for digest in output_hashes.values()), "Unique selected-map producer output names/hashes required")
    extraction["outputs"] = dict(sorted(output_hashes.items()))
    selected_map_proof_sha = sha(json.dumps(extraction, sort_keys=True, separators=(",", ":")).encode("utf-8"))
    grid_extension_path = repo / GRID_EXTENSION
    need(grid_extension_path.is_file() and not grid_extension_path.is_symlink(),
         "Pinned original grid-node source parser/producer helper required")
    grid_extension = load_module(grid_extension_path, "s002_current_grid_node_owner")
    enriched = grid_extension.add_grid_node(
        repo, enriched, base_proof,
        base_graph_path=base_path, base_graph_sha256=base_sha,
        parent_graph_sha256=selected_map_graph_sha,
        base_graph_proof_sha256=base_proof_sha)
    extension = enriched.get("source_graph_extension")
    need(isinstance(extension, dict) and extension.get("kind") == "S002ExactGridAuthoringConfigNode" and
         extension.get("base_graph_path") == str(base_path) and extension.get("base_graph_sha256") == base_sha and
         extension.get("base_graph_proof_sha256") == base_proof_sha and
         extension.get("parent_graph_sha256") == selected_map_graph_sha and
         re.fullmatch(r"[0-9a-f]{32}", extension.get("added_script_guid", "")) is not None,
         "Grid node must be derived from the exact T3 graph/proof and selected-map producer output")
    grid_rows = [row for row in enriched["nodes"] if row.get("source_path") == GRID_PATH]
    need(len(grid_rows) == 1 and grid_rows[0].get("asset_guid") == GRID_GUID and
         grid_rows[0].get("source_type") == GRID_TYPE,
         "Exact parser-derived grid authoring node required")
    need(enriched.get("source_pins", {}).get(str(repo / GRID_PATH)) == grid_rows[0].get("source_sha256") and
         grid_rows[0].get("source_sha256") == sha((repo / GRID_PATH).read_bytes()),
         "Grid node asset bytes must be pinned by its exact source hash")
    need(all(enriched_nodes.get(path) == row for path, row in base_nodes.items()) and
         len(enriched["nodes"]) == len(base_nodes) + 3,
         "Grid extension must preserve all selected-map nodes and add only the exact grid node")
    runtime_assets = (
        (enriched["definition_path"], "SkirmishScenarioDefinitionConfig"),
        (enriched["catalog_path"], "SkirmishExpansionCatalogConfig"),
        (enriched["battle_catalog_path"], "SkirmishBattleCatalogConfig"),
        (SURFACE_PATH, "MapSurfaceDataAsset"),
        (GRID_PATH, GRID_TYPE),
    )
    for relative, source_type in runtime_assets:
        rows = [row for row in enriched["nodes"] if row.get("source_path") == relative and
                row.get("source_type") == source_type]
        need(len(rows) == 1, "Exact Initialize source asset node required: " + relative)
    enriched_raw = (json.dumps(enriched, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    graph_sha = sha(enriched_raw)
    extension_proof = {
        "selected_map_graph_sha256": selected_map_graph_sha,
        "selected_map_producer_proof_sha256": selected_map_proof_sha,
        "grid_extension": extension,
        "grid_extension_source_sha256": sha(grid_extension_path.read_bytes()),
    }
    producer_proof_sha = sha(json.dumps({"selected_map": extraction, "grid_extension": extension_proof},
                                        sort_keys=True, separators=(",", ":")).encode("utf-8"))
    grid_extension_inputs = {
        str(grid_extension_path): sha(grid_extension_path.read_bytes()),
        str(Path(grid_extension.HELPER_PATH).resolve(strict=True)):
            sha(Path(grid_extension.HELPER_PATH).resolve(strict=True).read_bytes()),
    }
    graph_generation_pins = merge_pins(base_pins, proof["generator_inputs"], enriched["source_pins"],
                                       grid_extension_inputs)
    for path_text, expected in graph_generation_pins.items():
        path = Path(path_text)
        need(path.is_absolute() and path.is_file() and not path.is_symlink() and sha(path.read_bytes()) == expected,
             "Selected-map/grid graph source pin changed: " + path_text)
    # Derive the diagnostic package only after the existing strict package
    # reader has revalidated every passed owner artifact, capture proof, source
    # pin and supplement. The sole package-field change is the regenerated
    # source graph descriptor (the setup matrix identity must remain equal).
    package_reader_path = repo / "Tools/CI/s002_current_source_diagnostic_package.py"
    package_reader_sha = sha(package_reader_path.read_bytes())
    package_reader = load_module(package_reader_path, "s002_current_package_reader")
    source_package_relative = REFRESHED_SOURCE_PACKAGE if refreshed else SOURCE_PACKAGE
    derived_package_relative = (REFRESHED_DIAGNOSTIC_DIR / "package.json") if refreshed else DERIVED_PACKAGE
    derived_graph_relative = (REFRESHED_DIAGNOSTIC_DIR / "source-config-graph.json") if refreshed else DERIVED_GRAPH
    derived_proof_relative = (REFRESHED_DIAGNOSTIC_DIR / "graph-preparation-proof.json") if refreshed else DERIVED_GRAPH_PROOF
    source_package_path = repo / source_package_relative
    source_package_sha = sha(source_package_path.read_bytes())
    validated = package_reader.load_current_s002_package(source_package_path, source_package_sha, repo)
    package_value = dict(validated["value"])
    need(package_value["source_config_graph"] == {
        "path": str((repo / BASE_GRAPH).resolve()), "sha256": base_sha
    }, "Passed current package must bind the pinned T3 graph before derivation")
    need(package_value["setup_matrix"] == enriched["setup_matrix"],
         "Selected-map graph must preserve the package setup-matrix descriptor")
    graph_destination = repo / derived_graph_relative
    package_destination = repo / derived_package_relative
    proof_destination = repo / derived_proof_relative
    for destination in (graph_destination, package_destination, proof_destination):
        need(not destination.exists() or not destination.is_symlink(),
             "Derived S002 package output cannot be a symlink")
    package_value["source_config_graph"] = {"path": str(graph_destination.resolve()), "sha256": graph_sha}
    package_raw = (json.dumps(package_value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    package_sha = sha(package_raw)
    output.mkdir(parents=True)
    source = """// Generated by the current selected-map preparation owner and validated package derivation.\nnamespace Game.Runtime {\n internal static class S002CurrentSelectedMapGraphBuildAttestation {\n  internal const string BaseGraphSha256 = \"%s\";\n  internal const string BaseGraphProofSha256 = \"%s\";\n  internal const string ProducerProofSha256 = \"%s\";\n  internal const string EnrichedGraphSha256 = \"%s\";\n  internal const string DerivedPackageSha256 = \"%s\";\n  internal const string DerivedPackageRelativePath = \"%s\";\n  internal static void Require(string descriptorSha256, System.Func<byte[]> read) {\n   if (descriptorSha256 != EnrichedGraphSha256 || read == null) throw new System.IO.InvalidDataException(\"Build-attested current S002 selected-map graph required\");\n   var bytes = read();\n   if (bytes == null || AutonomousWorld.Hash(bytes) != EnrichedGraphSha256) throw new System.IO.InvalidDataException(\"Build-attested selected-map graph bytes differ\");\n  }\n  internal static void RequirePackage(string path, string root, string descriptorSha256, System.Func<byte[]> read) {\n   string expected = System.IO.Path.Combine(root, DerivedPackageRelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));\n   if (path == null || path != expected || descriptorSha256 != DerivedPackageSha256 || read == null) throw new System.IO.InvalidDataException(\"Build-attested current S002 package required\");\n   var bytes = read();\n   if (bytes == null || AutonomousWorld.Hash(bytes) != DerivedPackageSha256) throw new System.IO.InvalidDataException(\"Build-attested current S002 package bytes differ\");\n  }\n }\n}\n""" % (base_sha, base_proof_sha, producer_proof_sha, graph_sha, package_sha, derived_package_relative.as_posix())
    with (output / OUTPUT).open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(source)
    attestation_sha = sha((output / OUTPUT).read_bytes())
    enriched_output = output / "source-config-graph.json"
    enriched_output.write_bytes(enriched_raw)
    preparation_proof = {
        "schema_version": 1,
        "kind": "S002CurrentSelectedMapGraphPreparationProof",
        "status": "Passed",
        "base_graph": {"path": str(base_path), "sha256": base_sha},
        "base_graph_proof": {"path": str(proof_path), "sha256": base_proof_sha},
        "graph": {"path": str(graph_destination), "sha256": graph_sha},
        "selected_map_graph": {"sha256": selected_map_graph_sha},
        "producer_proof_sha256": producer_proof_sha,
        "producer_outputs": dict(sorted(output_hashes.items())),
        "grid_extension": extension_proof,
        "producer_output_location_policy": "Temporary producer destination omitted; exact output basenames and content hashes are retained; graph destination is separately fixed and byte-hashed.",
        "producer_path": PREPARER.as_posix(),
        "producer_sha256": sha((repo / PREPARER).read_bytes()),
        "source_pins": graph_generation_pins,
        "world_initialized": False,
        "mission_ready": False,
    }
    (output / "graph-preparation-proof.json").write_text(
        json.dumps(preparation_proof, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    # These are explicit runtime descriptors, not generated-source outputs.
    # Repeated generation is allowed only when each byte sequence is identical.
    for destination, raw in ((graph_destination, enriched_raw),
                             (proof_destination, (json.dumps(preparation_proof, indent=2, sort_keys=True) + "\n").encode("utf-8")),
                             (package_destination, package_raw)):
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination.exists():
            need(destination.read_bytes() == raw, "Existing diagnostic package output differs: " + str(destination))
        else:
            destination.write_bytes(raw)
    need(sha(package_reader_path.read_bytes()) == package_reader_sha and
         sha(source_package_path.read_bytes()) == source_package_sha and
         sha(generator_path.read_bytes()) == generator_sha and
         sha(grid_extension_path.read_bytes()) == grid_extension_inputs[str(grid_extension_path)] and
         sha(Path(grid_extension.HELPER_PATH).resolve(strict=True).read_bytes()) == grid_extension_inputs[str(Path(grid_extension.HELPER_PATH).resolve(strict=True))],
         "Current package reader/source package changed during derivation")
    for path_text, expected in graph_generation_pins.items():
        path = Path(path_text)
        need(path.is_file() and not path.is_symlink() and sha(path.read_bytes()) == expected,
             "Selected-map/grid graph input changed during derivation: " + path_text)
    need((output / OUTPUT).is_file() and sha((output / OUTPUT).read_bytes()) == attestation_sha,
         "Generated graph/package attestation source changed before final proof")
    generated_source_pins = {
        path: digest for path, digest in validated["source_pins"].items()
        if Path(path).is_absolute() and Path(path).is_relative_to(repo)
    }
    generated_source_pins = merge_pins(
        graph_generation_pins,
        generated_source_pins,
        {
            str(base_path): base_sha,
            str(proof_path): base_proof_sha,
            str(repo / BASE_GENERATOR): sha((repo / BASE_GENERATOR).read_bytes()),
            str(Path(base_generator.GENERIC).resolve(strict=True)):
                sha(Path(base_generator.GENERIC).resolve(strict=True).read_bytes()),
            str(repo / PREPARER): sha((repo / PREPARER).read_bytes()),
            str(generator_path): generator_sha,
            str(package_reader_path): package_reader_sha,
            str(source_package_path): source_package_sha,
            str(graph_destination): graph_sha,
            str(package_destination): package_sha,
            str(proof_destination): sha(proof_destination.read_bytes()),
        },
    )
    return {
        "source_pins": generated_source_pins,
        "base_graph": {"path": str(base_path), "sha256": base_sha},
        "base_graph_proof": {"path": str(proof_path), "sha256": base_proof_sha},
        "producer_proof_sha256": producer_proof_sha,
        "producer_outputs": dict(sorted(output_hashes.items())),
        "producer_output_location_policy": "Temporary producer destination omitted; exact output basenames and content hashes are retained; graph destination is separately fixed and byte-hashed.",
        "enriched_graph_sha256": graph_sha,
        "derived_package_path": str(package_destination),
        "derived_package_sha256": package_sha,
        "attestation_source_sha256": attestation_sha,
        "generated_graph_path": str(graph_destination),
        "generated_graph_proof_path": str(proof_destination),
        "added_nodes": ["Assets/Game/Resources/SkirmishBaseAssault.asset", CATALOG, MAP, GRID_PATH],
        "selected_map_graph_sha256": selected_map_graph_sha,
        "world_initialized": False,
        "mission_ready": False,
    }
