#!/usr/bin/env python3
"""Build an isolated staged-session handoff overlay for the declared source host."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
HOST = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/000-000_AutonomousWorld.cs"
WORLD = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/307_GeneratedS002StartingWorld.lifetime.cs"
INITIALIZER = ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishSessionInitializationSystem.cs"
SUPPRESSION = ROOT / "Assets/Game/Scripts/Systems/SkirmishStartupConfigSuppression.cs"
HELPER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/staged_s026_handoff/SourceStagedLaunch.cs"
FAMILY = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceGroundMissionFamily.cs"
ARIA = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/493_GeneratedS002UnifiedAria.cs"

def digest(data):
    return hashlib.sha256(data).hexdigest()

def replace_once(text, old, new):
    if text.count(old) != 1:
        raise ValueError("Expected exactly one source anchor: " + old[:100])
    return text.replace(old, new, 1)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    manifest_path = args.manifest.resolve(strict=True)
    output = args.output_dir.absolute()
    if not manifest_path.is_relative_to(ROOT) or not output.is_relative_to(ROOT) or output.exists():
        raise ValueError("Existing source manifest and fresh repository-owned output required")
    raw = manifest_path.read_bytes(); manifest = json.loads(raw)
    if manifest.get("runtime_backend") != "TypedGroundS026V1" or manifest.get("experimental_only") is not True:
        raise ValueError("Explicit experimental staged scope manifest required")
    pins = {str(p): digest(p.read_bytes()) for p in (manifest_path, HOST, WORLD, INITIALIZER, SUPPRESSION, HELPER, FAMILY, ARIA)}
    rows = manifest["sources"]
    host_row = next(x for x in rows if x.get("id") == "S026_000_AutonomousWorld.cs")
    world_row = next(x for x in rows if x.get("id") == "307_GeneratedS002StartingWorld.lifetime.cs")
    family_row = next(x for x in rows if x.get("id") == "SourceGroundMissionFamily.cs")
    aria_row = next(x for x in rows if x.get("id") == "493_GeneratedS002UnifiedAria.cs")
    if host_row.get("path") != str(HOST.relative_to(ROOT)) or world_row.get("path") != str(WORLD.relative_to(ROOT)) or family_row.get("path") != str(FAMILY.relative_to(ROOT)) or aria_row.get("path") != str(ARIA.relative_to(ROOT)):
        raise ValueError("Unexpected original declared-host source")
    output.mkdir(parents=True); generated = output / "generated"; generated.mkdir()
    host = HOST.read_text()
    host = replace_once(host, "        private bool initializationAttempted;", "        private bool initializationAttempted;\n        private SkirmishExpansionCatalogConfig stagedAuthoredCatalog;\n        private SourceStagedLaunch stagedLaunch;")
    host = replace_once(host, "            ActualCompileMatrix = matrix;", "            stagedAuthoredCatalog = catalog;\n            ActualCompileMatrix = matrix;")
    host = replace_once(host, "public void Dispose() { DisposeActualOperationMapPublication(); initialRoads?.Dispose(); disposed = true; }", "public void Dispose() { bool staged=stagedLaunch!=null; stagedLaunch?.Dispose();stagedLaunch=null; DisposeActualOperationMapPublication(); initialRoads?.Dispose(); disposed = true; if(staged&&SkirmishMissionDispatch.TryGetGenericById(SkirmishBattleCatalogConfig.Load(),ActualDefinition.CatalogId,out _))throw new InvalidOperationException(\"Staged catalog did not restore Planned rejection\"); }")
    world = WORLD.read_text()
    old = ''' Session=GeneratedS002SessionConstruction.Create(Entities,Setup,"generated-"+Setup.CatalogId+"-"+Setup.Seed,ActualDefinition);
 if(SkirmishMissionLogicalBindingCompiler.RequiresBinding(Setup)){if(!SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(ActualDefinition,Setup,out var logicalCompiled,out string logicalError)||!SkirmishMissionLogicalBindingProvider.TryAttachCompiled(Entities,Session,ActualDefinition,Setup,logicalCompiled,out logicalError))throw new InvalidOperationException("Original logical provider initialization rejected: "+logicalError);}'''
    new = ''' SourceGroundEnemyPolicyAdmission.Require(Setup);
 var toolCatalog=SourceStagedLaunch.CreateToolCatalog(stagedAuthoredCatalog,ActualDefinition);
 int toolIndex=8;while(ActualCanonicalCatalog.Entries.Any(x=>x.IsPlayable&&x.PlayableScenarioIndex==toolIndex))toolIndex++;
 stagedLaunch=SourceStagedLaunch.Open(Entities,toolCatalog,ActualDefinition,ActualCompileMatrix,ActualCompileManifest,Setup,toolIndex);
 Session=stagedLaunch.Session;Setup=stagedLaunch.Setup;ActualDefinitionScenarioIndex=toolIndex;
 freshStorageAuthority=FreshSourceStorageLifetimeAuthority.FromValidatedSource(request,inputHash,sourceInitializationBytes,ActualDefinition,Setup,ActualCompileManifest,ActualCompileMatrix);
 SourceQueuedInitializer.TryInitialize(Entities,Session);
 var stagedState=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
 if(stagedState.InitializationComplete!=1||stagedState.Phase!=SkirmishSessionPhase.Spawning||stagedState.SetupHash!=Setup.SetupHash)
  throw new InvalidOperationException("Original queued session initialization failed");'''
    world = replace_once(world, old, new)
    original = INITIALIZER.read_text(); begin = original.index("        private static void TryInitialize("); end = original.rindex("    }\n}")
    initializer = original[begin:end]
    initializer = replace_once(initializer, "private static void TryInitialize", "internal static void TryInitialize")
    initializer = replace_once(initializer, "SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);", "SkirmishStartupConfigSuppression.SuppressUnselectedStartupConfigs(em);")
    family = replace_once(FAMILY.read_text(),
        'if(playableIndex!=-1)throw new NotSupportedException("Source-only compiled mission must retain Planned/-1");',
        'if(playableIndex!=-1){var catalog=SkirmishBattleCatalogConfig.Load();var authored=SkirmishExpansionCatalogConfig.Load();if(!SkirmishMissionDispatch.TryGetGenericById(catalog,setup.CatalogId,out var entry)||entry.PlayableScenarioIndex!=playableIndex||entry.DefinitionId!=setup.DefinitionId||entry.ContentVersion!=setup.ContentVersion||authored==null||!authored.TryGetDefinition(setup.CatalogId,out var definition)||!SkirmishMissionDispatch.MatchesDefinition(entry,definition))throw new NotSupportedException("Exact active staged catalog and authored definition required");}')
    aria = replace_once(ARIA.read_text(),
        '  var canonical=ActualCanonicalCatalog??throw new InvalidOperationException("Actual pinned canonical catalog was not initialized");',
        '  var canonical=ActualCanonicalCatalog??throw new InvalidOperationException("Actual pinned canonical catalog was not initialized");\n  if(stagedLaunch!=null){canonical=Game.Configs.SkirmishBattleCatalogConfig.Load();if(!Game.Configs.SkirmishMissionDispatch.TryGetGenericById(canonical,Setup.CatalogId,out var stagedEntry)||stagedEntry.PlayableScenarioIndex!=ActualDefinitionScenarioIndex||!Game.Configs.SkirmishMissionDispatch.MatchesDefinition(stagedEntry,ActualDefinition))throw new InvalidOperationException("Active staged catalog identity changed");}')
    files = {"AutonomousWorld.cs": host, "StartingWorld.cs": world, "SourceGroundMissionFamily.cs": family, "GeneratedS002UnifiedAria.cs": aria,
             "SourceQueuedInitializer.cs": "using Game.Components; using Game.Configs; using Game.Skirmish.Contracts; using Unity.Entities; namespace Game.Runtime { internal static class SourceQueuedInitializer {\n" + initializer + "} }\n"}
    for name, text in files.items(): (generated / name).write_text(text)
    host_row["path"] = str((generated / "AutonomousWorld.cs").relative_to(ROOT)); world_row["path"] = str((generated / "StartingWorld.cs").relative_to(ROOT))
    family_row["path"] = str((generated / "SourceGroundMissionFamily.cs").relative_to(ROOT))
    aria_row["path"] = str((generated / "GeneratedS002UnifiedAria.cs").relative_to(ROOT))
    existing = {x.get("path") for x in rows}
    for ident, path, kind in [("SourceQueuedInitializer", generated / "SourceQueuedInitializer.cs", "native_direct"), ("SourceStartupConfigSuppression", SUPPRESSION, "native_direct"), ("SourceStagedLaunch", HELPER, "finite_adapter")]:
        relative = str(path.relative_to(ROOT))
        if relative not in existing:
            row = {"id": ident, "kind": kind, "path": relative}
            if kind == "finite_adapter": row.update({"role": "host_glue", "contains_original_gameplay": False, "scope": "Source-only staging and original resolver handoff; no native or readiness claim."})
            rows.append(row)
    manifest["staged_handoff_overlay"] = {"source_only": True, "scope": "Original resolver/session initializer; source-host diagnostic only", "mission_ready": False}
    target = output / "source-manifest.json"; target.write_text(json.dumps(manifest, indent=2) + "\n")
    receipt = {"status": "GeneratedSourceOnly", "inputs": pins, "outputs": {str(p): digest(p.read_bytes()) for p in [target, *(generated / x for x in files)]}, "mission_run": False, "mission_ready": False}
    (output / "generation-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    for name, sha in pins.items():
        if digest(Path(name).read_bytes()) != sha: raise ValueError("Input changed during generation: " + name)
    print(json.dumps({"status": receipt["status"], "manifest": str(target), "mission_ready": False}))

if __name__ == "__main__": main()
