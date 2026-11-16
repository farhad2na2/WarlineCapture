#!/usr/bin/env python3
"""Generate an isolated, source-only S003 + captured-map host composition."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
HOST = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/000-000_AutonomousWorld.s003.cs"
WORLD = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/307_GeneratedS002StartingWorld.lifetime.s003.cs"
PROGRAM = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceSimulationProgram.s003.cs"
DECLARED_INPUTS = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceDeclaredRuntimeInputs.s003.cs"
CLEARANCE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/527_GeneratedS002DropClearance.s003.cs"
ALIAS_GENERATOR = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate.py"


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise ValueError(f"Expected exactly one {label} anchor; found {count}")
    return text.replace(old, new, 1)


def overlay_host(source: str) -> str:
    field = "        private string inputHash;\n"
    insert = field + (
        "        private SourceDerivedMapRuntime sourceDerivedMap;\n"
        "        internal JObject SourceDerivedMapReceipt { get; private set; }\n"
        "        internal void BindSourceDerivedMap(SourceDerivedMapRuntime value) "
        "{ if (value == null || sourceDerivedMap != null) throw new InvalidOperationException(\"Source-derived map binding must be nonnull and single-use\"); sourceDerivedMap = value; }\n"
    )
    return once(source, field, insert, "source-map host field/bind")


def overlay_world(source: str) -> str:
    text = once(
        source,
        " var nav=(JObject)request[\"navigation_reference\"];AuthoredNavigationCount=SourceNavigationFacts.Import(Entities,nav);",
        " var nav=(JObject)request[\"navigation_reference\"];if(sourceDerivedMap!=null)AuthoredNavigationCount=GeneratedAuthoredNavigationImporter.Materialize(Entities,sourceDerivedMap.Navigation);else AuthoredNavigationCount=SourceNavigationFacts.Import(Entities,nav);",
        "navigation owner handoff",
    )
    text = once(
        text,
        ' if(Setup.CatalogId=="S003"){',
        ' if(sourceDerivedMap!=null)domain=sourceDerivedMap.Domain;else if(Setup.CatalogId=="S003"){',
        "placement-domain owner handoff",
    )
    original_water = "var waterMask=domain.BuildWaterMask(actualWaterMatch,grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);"
    replacement_water = "var waterMask=sourceDerivedMap!=null?sourceDerivedMap.Water:domain.BuildWaterMask(actualWaterMatch,grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);"
    text = once(text, original_water, replacement_water, "water owner handoff")
    receipt_anchor = " Boundary=Entities.CreateEntity();Entities.AddComponentData(Boundary,new BuildingRuntimeStateTag());"
    receipt = " if(sourceDerivedMap!=null)SourceDerivedMapReceipt=sourceDerivedMap.InstalledReceipt(masks.AuthoredBuildings,roads,masks.Sidewalks,waterMask,AuthoredNavigationCount);\n"
    return once(text, receipt_anchor, receipt + receipt_anchor, "installed-map receipt")


def reverse_host(text: str) -> str:
    overlay = (
        "        private string inputHash;\n"
        "        private SourceDerivedMapRuntime sourceDerivedMap;\n"
        "        internal JObject SourceDerivedMapReceipt { get; private set; }\n"
        "        internal void BindSourceDerivedMap(SourceDerivedMapRuntime value) "
        "{ if (value == null || sourceDerivedMap != null) throw new InvalidOperationException(\"Source-derived map binding must be nonnull and single-use\"); sourceDerivedMap = value; }\n"
    )
    return once(text, overlay, "        private string inputHash;\n", "host field inverse")


def reverse_world(text: str) -> str:
    text = once(
        text,
        " var nav=(JObject)request[\"navigation_reference\"];if(sourceDerivedMap!=null)AuthoredNavigationCount=GeneratedAuthoredNavigationImporter.Materialize(Entities,sourceDerivedMap.Navigation);else AuthoredNavigationCount=SourceNavigationFacts.Import(Entities,nav);",
        " var nav=(JObject)request[\"navigation_reference\"];AuthoredNavigationCount=SourceNavigationFacts.Import(Entities,nav);",
        "navigation inverse",
    )
    text = once(text, ' if(sourceDerivedMap!=null)domain=sourceDerivedMap.Domain;else if(Setup.CatalogId=="S003"){', ' if(Setup.CatalogId=="S003"){', "domain inverse")
    text = once(
        text,
        "var waterMask=sourceDerivedMap!=null?sourceDerivedMap.Water:domain.BuildWaterMask(actualWaterMatch,grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);",
        "var waterMask=domain.BuildWaterMask(actualWaterMatch,grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);",
        "water inverse",
    )
    receipt = " if(sourceDerivedMap!=null)SourceDerivedMapReceipt=sourceDerivedMap.InstalledReceipt(masks.AuthoredBuildings,roads,masks.Sidewalks,waterMask,AuthoredNavigationCount);\n"
    return once(text, receipt + " Boundary=Entities.CreateEntity();Entities.AddComponentData(Boundary,new BuildingRuntimeStateTag());", " Boundary=Entities.CreateEntity();Entities.AddComponentData(Boundary,new BuildingRuntimeStateTag());", "receipt inverse")


def load_alias_runtime(output: Path):
    spec = importlib.util.spec_from_file_location("source_map_host_generator", ALIAS_GENERATOR)
    if spec is None or spec.loader is None:
        raise ValueError("Could not load pinned source-map alias proof loader")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    wrapper = Path(__file__).resolve(strict=True)
    return module.generate_logical_alias_runtime(ROOT, output, wrapper)


def generate(output: Path) -> dict:
    output = output.resolve()
    if output.exists():
        raise ValueError("Fresh isolated output directory required")
    for source in (HOST, WORLD, PROGRAM, DECLARED_INPUTS, CLEARANCE, ALIAS_GENERATOR):
        if source.is_symlink() or not source.is_file():
            raise ValueError(f"Expected regular source input: {source}")
    host_source, world_source, program_source = HOST.read_text(), WORLD.read_text(), PROGRAM.read_text()
    host_output, world_output = overlay_host(host_source), overlay_world(world_source)
    if reverse_host(host_output) != host_source or reverse_world(world_output) != world_source:
        raise ValueError("Reverse-transform equality failed; source outside declared owner handoffs changed")
    # Explicitly protect the proof-aware template reader and current projected Rifle path.
    for needle in (
        "GeneratedReferenceTemplateView.Read(reference,out referenceBinaryDirectory,expectedCaptureGraph,request,Setup,derivedContext,SourceRepositoryRoot)",
        "SourceUnitMoveAuthoringSource.Load(request[\"unit_move_authoring_source\"]as JObject,referenceDomain,projectedRifleKey,SourceRepositoryRoot",
        "GeneratedReferenceTemplateImporter.Import(Entities,referenceDomain,referenceBinaryDirectory,requiredTemplateKeys,moveSource.RootKey,moveSource.Projection,moveSource.Combat,moveSource.Health,moveSource.Attack)",
    ):
        if world_source.count(needle) != 1 or world_output.count(needle) != 1:
            raise ValueError("Current S003 derived-template or projected Rifle source branch changed")

    output.mkdir(parents=True)
    generated = output / "generated"
    generated.mkdir()
    (generated / "AutonomousWorld.cs").write_text(host_output)
    (generated / "StartingWorld.cs").write_text(world_output)
    program_anchor = 'static int Main(string[] args){try{'
    program_insert = program_anchor + 'if(args.Length==2&&args[0]=="--source-map-initialize-only")return S075SourceMapInitializationControl.Run(JObject.Parse(File.ReadAllText(args[1])));'
    program_output = once(program_source, program_anchor, program_insert, "explicit source-map initialization diagnostic")
    if once(program_output, program_insert, program_anchor, "diagnostic entry inverse") != program_source:
        raise ValueError("Source program changed outside explicit diagnostic entry")
    (generated / "SourceSimulationProgram.cs").write_text(program_output)
    declared_source = DECLARED_INPUTS.read_text()
    declared_anchor = "internal static class SourceDeclaredRuntimeInputs{"
    declared_insert = declared_anchor + '\n static global::SourceDerivedMapRuntime sourceDerivedMap;\n internal static void BindSourceDerivedMap(global::SourceDerivedMapRuntime value){if(value==null||sourceDerivedMap!=null)throw new InvalidOperationException("Exactly one source-derived map runtime instance may be bound");sourceDerivedMap=value;}'
    declared_output = once(declared_source, declared_anchor, declared_insert, "source-map declared-input bind")
    if once(declared_output, declared_insert, declared_anchor, "declared-input bind inverse") != declared_source:
        raise ValueError("Declared inputs changed outside explicit source-map bind")
    (generated / "SourceDeclaredRuntimeInputs.cs").write_text(declared_output)
    alias_evidence = load_alias_runtime(generated)
    spec = importlib.util.spec_from_file_location("source_map_clearance_generator", ALIAS_GENERATOR)
    if spec is None or spec.loader is None:
        raise ValueError("Could not load source-map clearance proof loader")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    clearance_evidence = module.generate_seed_output(ROOT, generated, "SourceDerivedClearance.cs", Path(__file__).resolve())
    clearance_seed = (generated / "SourceDerivedClearance.cs").read_text()
    start = '   if((string)root["kind"]=="SourceDerivedCampaignMapDropClearanceProjection"){'
    end = '   if((string)root["kind"]=="S026InactiveOriginalDropBoundsProjection"){'
    a, b = clearance_seed.find(start), clearance_seed.find(end)
    if a < 0 or b <= a or clearance_seed.count(start) != 1 or clearance_seed.count(end) != 1:
        raise ValueError("Unique verified source-map clearance branch required")
    branch = clearance_seed[a:b]
    if not branch.rstrip().endswith("}") or "return;" not in branch:
        raise ValueError("Complete terminating source-map clearance branch required")
    clearance_source = CLEARANCE.read_text()
    clearance_anchor = '   if((string)root["kind"]=="OriginalCapturedFixedMapCompilerContext"){'
    clearance_output = once(clearance_source, clearance_anchor, branch + clearance_anchor, "source-map clearance branch")
    if once(clearance_output, branch + clearance_anchor, clearance_anchor, "clearance branch inverse") != clearance_source:
        raise ValueError("S003 clearance fallback changed outside source-map branch")
    (generated / "S003DerivedClearance.cs").write_text(clearance_output)

    inputs = {
        str(path): sha(path.read_bytes())
        for path in (HOST, WORLD, PROGRAM, DECLARED_INPUTS, CLEARANCE, ALIAS_GENERATOR, Path(__file__).resolve())
    }
    inputs.update(alias_evidence["source_pins"])
    inputs.update(clearance_evidence["source_pins"])
    outputs = {
        str(path): sha(path.read_bytes())
        for path in (generated / "AutonomousWorld.cs", generated / "StartingWorld.cs", generated / "SourceSimulationProgram.cs", generated / "SourceDeclaredRuntimeInputs.cs", generated / "SourceDerivedMapRuntime.cs", generated / "SourceDerivedClearance.cs", generated / "S003DerivedClearance.cs")
    }
    receipt = {
        "kind": "S003DerivedSourceMapOverlay",
        "status": "GeneratedSourceOnly",
        "experimental_only": True,
        "mission_ready": False,
        "native_or_player_readiness": False,
        "reverse_transform_equality": True,
        "scope": "Isolated S003 host binding and four source-map owner handoffs; current template proof and projected Rifle branches preserved byte-for-byte.",
        "inputs": inputs,
        "outputs": outputs,
    }
    (output / "generation-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(generate(args.output_dir), indent=2))


if __name__ == "__main__":
    main()
