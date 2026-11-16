#!/usr/bin/env python3
"""Project an already-authored logical binding through the current original compiler."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
CODEC = ROOT / "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs"
QUALIFICATION = ROOT / "T Logs/SkirmishProduction/s050-source-qualification-20261107-r1/qualification.json"
PASS = "[CurrentAuthoringLogicalBindingProbe] result=PassedSourceOnly"


def sha(path: Path) -> str:
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def extract_method(source: str, signature: str) -> str:
    start = source.find(signature)
    need(start >= 0 and source.find(signature, start + len(signature)) < 0,
         "Unique original portable field codec method required")
    brace = source.find("{", start + len(signature))
    depth = 0
    for i in range(brace, len(source)):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[start:i + 1]
    raise ValueError("Unclosed original portable codec method")


def verify_pin_map(pins: dict[str, str], label: str) -> None:
    need(isinstance(pins, dict) and pins, label + " pin map required")
    for path, expected in pins.items():
        p = Path(path)
        need(p.is_file() and sha(p) == expected, label + " pin drift: " + path)


def repository_source_evidence(path: Path, label: str) -> Path:
    """Resolve existing source evidence while keeping it inside an owned log root."""
    candidate = Path(path).absolute()
    roots = (ROOT / "Logs", ROOT / "T Logs")
    need(not candidate.is_symlink(), label + " must not be a symlink")
    resolved = candidate.resolve(strict=True)
    need(candidate == resolved and any(resolved.is_relative_to(root.resolve()) for root in roots),
         label + " must be canonical repository-owned Logs or T Logs evidence")
    need(resolved.is_file(), label + " must be a regular file")
    return resolved


def run(request_path: Path, result_path: Path, host_build_path: Path, output: Path,
        qualification_path: Path = QUALIFICATION, source_manifest_path: Path | None = None) -> dict:
    request_path = request_path.resolve(strict=True)
    result_path = result_path.resolve(strict=True)
    host_build_path = host_build_path.resolve(strict=True)
    output = output.absolute()
    need(output.is_relative_to(ROOT / "T Logs") and not output.exists(),
         "Fresh repository-owned T Logs output required")
    need(CODEC.is_file(), "Original portable codec source required")
    codec_sha = sha(CODEC)
    host = json.loads(host_build_path.read_bytes())
    host_build_sha = sha(host_build_path)
    need(host.get("kind") == "SkirmishSourceSimulationBuild" and
         host.get("status") == "Passed" and host.get("compile_exit") == 0 and
         host.get("repository_root") == str(ROOT), "Passed current source host build required")
    host_pins = host.get("source_and_reference_pins")
    verify_pin_map(host_pins, "Source host")
    manifest_descriptor = host.get("source_manifest")
    need(isinstance(manifest_descriptor, dict) and set(manifest_descriptor) == {"path", "sha256", "bytes"},
         "Exact source-host manifest descriptor required")
    manifest_path = Path(source_manifest_path).resolve(strict=True) if source_manifest_path else Path(manifest_descriptor["path"]).resolve(strict=True)
    need(manifest_path == Path(manifest_descriptor["path"]).resolve(strict=True) and
         sha(manifest_path) == manifest_descriptor["sha256"] and
         manifest_path.stat().st_size == manifest_descriptor["bytes"],
         "Selected source manifest must be the exact one pinned by this host build")
    baseline = json.loads(result_path.read_bytes())
    request = json.loads(request_path.read_bytes())
    need(baseline.get("kind") == "SkirmishMissionAuthoringPreflightResult" and
         baseline.get("status") == "ConfigurationCompiled" and baseline.get("compiler_accepted") is True and
         baseline.get("world_allocated") is False and baseline.get("mission_ready") is False,
         "Retained successful source-only authoring selection required")
    need(request.get("catalog_id") == baseline.get("catalog_id") and
         request.get("seed") == baseline.get("seed") and request.get("size") == baseline.get("size") and
         request.get("difficulty") == baseline.get("difficulty"), "Retained authoring request/result identity mismatch")
    binding_descriptor = request.get("binding")
    need(isinstance(binding_descriptor, dict) and set(binding_descriptor) == {"path", "sha256"},
         "Exact authoring logical binding descriptor required")
    binding_path = Path(binding_descriptor["path"]).resolve(strict=True)
    need(sha(binding_path) == binding_descriptor["sha256"],
         "Binding is not the exact retained request-pinned input")
    need(baseline.get("resolved_setup", {}).get("setup_hash") is not None,
         "Compiler-selected setup hash required")
    need(sha(CODEC) == codec_sha, "Portable codec changed before projection")
    qualification_path = repository_source_evidence(qualification_path, "Repository-owned source-only qualification")
    qualification = json.loads(qualification_path.read_bytes())
    need(qualification.get("kind") == "ExperimentalGroundConfigurationQualification" and
         qualification.get("runtime_backend") == "TypedGroundS026V1" and
         qualification.get("scope") == "OriginalCompilerAndQualifiedFactoryInputs_SourceOnly" and
         qualification.get("native_context_qualified") is False and
         qualification.get("mission_ready") is False,
         "Exact source-only S050 compiler qualification required")
    factory_descriptor = qualification.get("compiler_context")
    need(isinstance(factory_descriptor, dict) and set(factory_descriptor) == {"path", "sha256"},
         "Exact qualified S050 factory-context descriptor required")
    factory_context_path = repository_source_evidence(Path(factory_descriptor["path"]), "Qualified S050 factory context")
    need(sha(factory_context_path) == factory_descriptor["sha256"],
         "Qualified S050 factory context is stale or outside retained source evidence")
    factory_context = json.loads(factory_context_path.read_bytes())
    need(factory_context.get("kind") == "OriginalCompilerDerivedNativeFactoryContext" and
         factory_context.get("world_allocated") is False and factory_context.get("native_S050_capture") is False and
         factory_context.get("source_fit_certified") is False and factory_context.get("mission_ready") is False,
         "Honest source-only original S050 factory context required")
    factory_pins = factory_context.get("consumed_input_pins")
    verify_pin_map(factory_pins, "Qualified factory-context consumed input")
    need(factory_pins == factory_context.get("consumed_input_pins"), "Factory-context pin closure changed")

    output.mkdir(parents=True)
    replay = output / "authoring-replay"
    draft = dict(request)
    draft["kind"] = "SkirmishMissionAuthoringDraft"
    for key in ("source_repository_root", "data_repository_root", "output_directory", "runtime_config"):
        draft.pop(key, None)
    draft_path = output / "authoring-draft.json"
    draft_path.write_text(json.dumps(draft, indent=2) + "\n")
    sys.path.insert(0, str(ROOT / "Tools/CI"))
    import run_skirmish_source_simulation as source_cli
    skirmish_source_authoring = source_cli.owned.require("skirmish_source_authoring")
    invocation = skirmish_source_authoring.author(
        ROOT, ROOT, manifest_path,
        replay, baseline["catalog_id"], baseline["seed"], draft_path,
        build_receipt=host_build_path, data_repo=ROOT, timeout=600, owner_projection=False)
    need(invocation.get("status") == "ConfigurationCompiled" and invocation.get("process_exit") == 0,
         "Fresh checked-host authoring replay failed; inspect authoring-replay logs")
    replay_request_path = replay / "authoring-request.json"
    replay_result_path = replay / "authoring-result.json"
    replay_request = json.loads(replay_request_path.read_bytes())
    result = json.loads(replay_result_path.read_bytes())
    baseline_fields = ("catalog_id", "seed", "size", "difficulty", "definition_id", "layout_id",
                       "operation_map_id", "source_content_hash", "layout_fingerprint", "source_fingerprint")
    need(all(result.get(key) == baseline.get(key) for key in baseline_fields) and
         result.get("resolved_setup", {}).get("setup_hash") == baseline["resolved_setup"]["setup_hash"],
         "Fresh compiler-selected authoring result drifted from retained r3 selection")
    need(replay_request.get("binding") == binding_descriptor and sha(binding_path) == binding_descriptor["sha256"],
         "Fresh authoring replay did not retain exact pinned logical binding")
    author_pins = result.get("input_pins")
    need(isinstance(author_pins, dict), "Fresh authoring result input pins required")
    verify_pin_map({p: row["sha256"] for p, row in author_pins.items()}, "Fresh authoring input")
    need(sha(replay_request_path) == author_pins.get(str(replay_request_path), {}).get("sha256"),
         "Fresh authoring request is not in result pin closure")

    owner = output / "owner"
    owner.mkdir()
    host_pins_json = json.dumps(host_pins, sort_keys=True)
    helper = r'''using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEngine;

internal static class CurrentAuthoringLogicalBindingProbe
{
    private static string Hash(string path) { using (var sha = System.Security.Cryptography.SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    private static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static JObject Descriptor(JObject request, string name) { var d=(JObject)request[name]; Need(d!=null && d.Properties().Count()==2 && d["path"]!=null && d["sha256"]!=null,"Exact descriptor required: "+name); string path=Path.GetFullPath((string)d["path"]); Need(File.Exists(path) && Hash(path)==(string)d["sha256"],"Pinned descriptor mismatch: "+name); return d; }
    static int Main(string[] args) {
      try {
        Need(args.Length==3,"request, result, output required");
        var request=JObject.Parse(File.ReadAllText(args[0])); var expected=JObject.Parse(File.ReadAllText(args[1])); string outDir=Path.GetFullPath(args[2]);
        string root=Path.GetFullPath((string)request["source_repository_root"]); var graph=JObject.Parse(File.ReadAllText((string)Descriptor(request,"graph")["path"])); var materializer=new SourceConfigMaterializer(graph);
        var layout=materializer.LogicalLayout(JObject.Parse(File.ReadAllText((string)Descriptor(request,"layout")["path"])));
        var profiles=(JObject)request["profiles"]; var source=materializer.ByPath<OperationMapDefinition>((string)request["campaign_source_path"]);
        var definition=new SkirmishScenarioDefinitionConfig();
        definition.Configure((string)request["catalog_id"],materializer.ByPath<SkirmishObjectiveConfig>((string)profiles["objective"]),materializer.ByPath<SkirmishArmyProfileConfig>((string)profiles["army"]),materializer.ByPath<SkirmishStartPackageConfig>((string)profiles["start"]),materializer.ByPath<SkirmishEconomyConfig>((string)profiles["economy"]),materializer.ByPath<SkirmishUpgradeConfig>((string)profiles["upgrades"]),materializer.ByPath<SkirmishIntelConfig>((string)profiles["intel"]),materializer.ByPath<SkirmishRoleCatalogConfig>((string)profiles["roles"]),layout);
        definition.BindCampaignMap(source);
        var catalog=materializer.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset"); UnityEngine.Resources.BindSourceResources(new System.Collections.Generic.Dictionary<string,object>{{SkirmishExpansionCatalogConfig.ResourceName,catalog},{SkirmishSetupMatrixTable.PackagedResourceName,new TextAsset(File.ReadAllText((string)Descriptor(request,"setup_matrix")["path"]))}});
        Need(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out var matrixError),matrixError);
        var difficulty=(SkirmishDifficultyId)Enum.Parse(typeof(SkirmishDifficultyId),(string)request["difficulty"]); var size=(SkirmishSizeId)Enum.Parse(typeof(SkirmishSizeId),(string)request["size"]); int seed=(int)request["seed"]; var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
        Need(SkirmishSetupCompiler.TryCompile(definition,difficulty,size,seed,manifest,matrix,out var unbound,out var unboundReasons),"Original compiler rejected unbound setup");
        var raw=Descriptor(request,"binding"); var binding=materializer.LogicalBinding(JObject.Parse(File.ReadAllText((string)raw["path"]))); definition.BindMissionLogicalBinding(binding);
        Need(SkirmishSetupCompiler.TryCompile(definition,difficulty,size,seed,manifest,matrix,out var bound,out var boundReasons),"Original compiler rejected request-pinned binding");
        Need((long)bound.SetupHash==(long)expected["resolved_setup"]["setup_hash"],"Bound setup hash differs from current authoring result");
        Need(SkirmishMissionLogicalBindingCompiler.TryCompile(definition,bound,out var compiledBinding,out string bindError),"Logical binding compiler rejected bound setup: "+bindError);
        var portable=new JObject{["schema_version"]=1,["kind"]="DeclaredOriginalLogicalBindingFieldsWithInlineViews",["float_encoding"]="IEEE754_float32_hex_bits",["unity_object_ids_exported"]=false,["fields"]=PortableLogicalValue(binding,0)};
        var decoded=SourceNativeMissionLogicalBindingReference.DecodeDeclaredFields(portable["fields"]); var decodedDefinition=new SkirmishScenarioDefinitionConfig();
        decodedDefinition.Configure((string)request["catalog_id"],materializer.ByPath<SkirmishObjectiveConfig>((string)profiles["objective"]),materializer.ByPath<SkirmishArmyProfileConfig>((string)profiles["army"]),materializer.ByPath<SkirmishStartPackageConfig>((string)profiles["start"]),materializer.ByPath<SkirmishEconomyConfig>((string)profiles["economy"]),materializer.ByPath<SkirmishUpgradeConfig>((string)profiles["upgrades"]),materializer.ByPath<SkirmishIntelConfig>((string)profiles["intel"]),materializer.ByPath<SkirmishRoleCatalogConfig>((string)profiles["roles"]),layout);
        decodedDefinition.BindCampaignMap(source); decodedDefinition.BindMissionLogicalBinding(decoded);
        Need(SkirmishSetupCompiler.TryCompile(decodedDefinition,difficulty,size,seed,manifest,matrix,out var decodedSetup,out var decodedReasons) && (long)decodedSetup.SetupHash==(long)bound.SetupHash,"Portable decoded binding did not reproduce setup");
        Need(SkirmishMissionLogicalBindingCompiler.TryCompile(decodedDefinition,decodedSetup,out var decodedBinding,out bindError) && decodedBinding.Fingerprint==compiledBinding.Fingerprint,"Portable decoded binding fingerprint differs");
        var negatives=new JArray(); var badFields=(JObject)portable["fields"].DeepClone(); badFields["SourceContentHash"]=new string('0',64); var bad=SourceNativeMissionLogicalBindingReference.DecodeDeclaredFields(badFields); var badDefinition=new SkirmishScenarioDefinitionConfig();
        badDefinition.Configure((string)request["catalog_id"],materializer.ByPath<SkirmishObjectiveConfig>((string)profiles["objective"]),materializer.ByPath<SkirmishArmyProfileConfig>((string)profiles["army"]),materializer.ByPath<SkirmishStartPackageConfig>((string)profiles["start"]),materializer.ByPath<SkirmishEconomyConfig>((string)profiles["economy"]),materializer.ByPath<SkirmishUpgradeConfig>((string)profiles["upgrades"]),materializer.ByPath<SkirmishIntelConfig>((string)profiles["intel"]),materializer.ByPath<SkirmishRoleCatalogConfig>((string)profiles["roles"]),layout); badDefinition.BindCampaignMap(source); badDefinition.BindMissionLogicalBinding(bad); Need(!SkirmishSetupCompiler.TryCompile(badDefinition,difficulty,size,seed,manifest,matrix,out _,out var badReasons),"Wrong source hash control unexpectedly passed"); negatives.Add("wrong_source_content_hash_rejected");
        var malformed=(JObject)portable["fields"].DeepClone(); var variant=(JObject)((JArray)malformed["Variants"])[0]; ((JArray)variant["Framing"]).RemoveAt(0); var missing=SourceNativeMissionLogicalBindingReference.DecodeDeclaredFields(malformed); var missingDefinition=new SkirmishScenarioDefinitionConfig();
        missingDefinition.Configure((string)request["catalog_id"],materializer.ByPath<SkirmishObjectiveConfig>((string)profiles["objective"]),materializer.ByPath<SkirmishArmyProfileConfig>((string)profiles["army"]),materializer.ByPath<SkirmishStartPackageConfig>((string)profiles["start"]),materializer.ByPath<SkirmishEconomyConfig>((string)profiles["economy"]),materializer.ByPath<SkirmishUpgradeConfig>((string)profiles["upgrades"]),materializer.ByPath<SkirmishIntelConfig>((string)profiles["intel"]),materializer.ByPath<SkirmishRoleCatalogConfig>((string)profiles["roles"]),layout); missingDefinition.BindCampaignMap(source); missingDefinition.BindMissionLogicalBinding(missing); Need(!SkirmishSetupCompiler.TryCompile(missingDefinition,difficulty,size,seed,manifest,matrix,out _,out var missingReasons),"Missing framing control unexpectedly passed"); negatives.Add("missing_role_framing_rejected");
        Directory.CreateDirectory(outDir); File.WriteAllText(Path.Combine(outDir,"portable-binding.json"),portable.ToString(Newtonsoft.Json.Formatting.Indented)+"\n");
        var proof=new JObject{["kind"]="CurrentAuthoringLogicalBindingRoundtrip",["catalog_id"]=(string)request["catalog_id"],["definition_id"]=definition.DefinitionId,["content_version"]=definition.ContentVersion,["seed"]=seed,["size"]=(string)request["size"],["difficulty"]=(string)request["difficulty"],["operation_map_id"]=source.OperationMapId,["source_content_hash"]=source.ContentHash,["source_fingerprint"]=expected["source_fingerprint"],["layout_fingerprint"]=expected["layout_fingerprint"],["layout_binding_fingerprint"]=bound.LayoutBindingFingerprint,["setup_hash_without_binding"]=unbound.SetupHash,["setup_hash_with_binding"]=bound.SetupHash,["portable_roundtrip_setup_hash"]=decodedSetup.SetupHash,["logical_binding_fingerprint"]=compiledBinding.Fingerprint,["portable_roundtrip_fingerprint"]=decodedBinding.Fingerprint,["negative_controls"]=negatives,["world_allocated"]=false,["placement_performed"]=false,["native_capture"]=false,["runtime_admission"]=false,["source_fit_certified"]=false,["mission_ready"]=false}; File.WriteAllText(Path.Combine(outDir,"roundtrip.json"),proof.ToString(Newtonsoft.Json.Formatting.Indented)+"\n");
        Console.WriteLine("[CurrentAuthoringLogicalBindingProbe] result=PassedSourceOnly setup="+bound.SetupHash+" roundtrip="+decodedSetup.SetupHash+" world=0 readiness=0"); return 0;
      } catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
    __PORTABLE_CODEC__
}
'''
    method = extract_method(CODEC.read_text(), "static JToken PortableLogicalValue(")
    source = owner / "CurrentAuthoringLogicalBindingProbe.cs"
    source.write_text(helper.replace("__PORTABLE_CODEC__", method))
    rsp_path = host_build_path.parent / "compile.rsp"
    need(rsp_path.is_file(), "Source host compiler response required")
    response = [json.loads(line) for line in rsp_path.read_text().splitlines() if line.strip()]
    need(response.count("-main:SourceSimulationProgram") == 1 and
         response.count("-out:" + str(host_build_path.parent / "SourceSimulation.exe")) == 1,
         "Exact host compiler response shape required")
    dotnet = Path("/usr/local/share/dotnet/dotnet")
    csc_candidates = list(dotnet.parent.glob("sdk/*/Roslyn/bincore/csc.dll"))
    need(csc_candidates, "Installed Roslyn required")
    csc = sorted(csc_candidates, key=lambda p: tuple(int(x) for x in p.parts[-4].split(".")))[-1]
    exe = owner / "binding-probe.exe"
    response = ["-main:CurrentAuthoringLogicalBindingProbe" if row == "-main:SourceSimulationProgram" else
                "-out:" + str(exe) if row == "-out:" + str(host_build_path.parent / "SourceSimulation.exe") else row
                for row in response]
    response.append(str(source))
    compile_rsp = owner / "compile.rsp"
    compile_rsp.write_text("".join(json.dumps(row) + "\n" for row in response))
    compiled = subprocess.run([str(dotnet), str(csc), "@" + str(compile_rsp)], cwd=ROOT,
                              capture_output=True, text=True, timeout=300)
    (owner / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0 and exe.is_file(), "Binding probe compile failed; see compile.log")
    runtime = host["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((runtime["engine"], str(Path(runtime["newtonsoft"]).parent),
                                        str(ROOT / "Library/ScriptAssemblies"),
                                        str(host_build_path.parent / "placement"), str(owner)))
    run = subprocess.run([runtime["mono"], "--optimize=-float32", str(exe), str(replay_request_path),
                          str(replay_result_path), str(output / "owner")], cwd=ROOT, env=env,
                         capture_output=True, text=True, timeout=600)
    (owner / "run.stdout.log").write_text(run.stdout)
    (owner / "run.stderr.log").write_text(run.stderr)
    need(run.returncode == 0 and PASS in run.stdout, "Original source binding replay failed; see owner logs")
    portable_path = owner / "portable-binding.json"
    roundtrip_path = owner / "roundtrip.json"
    proof = json.loads(roundtrip_path.read_bytes())
    need(proof.get("setup_hash_with_binding") == result["resolved_setup"]["setup_hash"] and
         proof.get("setup_hash_with_binding") == proof.get("portable_roundtrip_setup_hash") and
         proof.get("logical_binding_fingerprint") == proof.get("portable_roundtrip_fingerprint"),
         "Source owner proof does not match current authoring result")
    portable = json.loads(portable_path.read_bytes())
    observed_target = {"catalog_id": proof["catalog_id"], "definition_id": proof["definition_id"],
                       "content_version": proof["content_version"], "seed": proof["seed"],
                       "size": proof["size"], "difficulty": proof["difficulty"],
                       "setup_hash": proof["setup_hash_with_binding"],
                       "operation_map_id": proof["operation_map_id"],
                       "source_content_hash": proof["source_content_hash"],
                       "source_fingerprint": proof["source_fingerprint"],
                       "layout_fingerprint": proof["layout_fingerprint"],
                       "layout_binding_fingerprint": proof["layout_binding_fingerprint"],
                       "mission_logical_binding_fingerprint": proof["logical_binding_fingerprint"]}
    target = factory_context.get("target_context")
    need(isinstance(target, dict) and all(target.get(key) == value for key, value in observed_target.items()),
         "Original qualified factory-context target differs from current compiler replay")
    wrapper = {"schema_version": 1, "kind": "OriginalCompilerDeclaredLogicalBinding",
               "metadata_compiler_context": factory_descriptor,
               "target_context": target, "portable_binding": portable,
               "source_owner_binding": {"path": str(binding_path), "sha256": binding_descriptor["sha256"]},
               "world_allocated": False, "native_capture": False, "runtime_admission": False,
               "source_fit_certified": False, "mission_ready": False}
    wrapper_path = output / "logical-binding.json"
    wrapper_path.write_text(json.dumps(wrapper, indent=2, sort_keys=True) + "\n")
    verify_pin_map(host_pins, "Source host after replay")
    verify_pin_map({p: row["sha256"] for p, row in author_pins.items()}, "Authoring inputs after replay")
    need(sha(host_build_path) == host_build_sha, "Source host build receipt changed during replay")
    receipt = {"kind": "CurrentOriginalCompilerLogicalBindingProjection", "status": "ProjectedSourceOnly",
               "baseline_request": {"path": str(request_path), "sha256": sha(request_path)},
               "baseline_result": {"path": str(result_path), "sha256": sha(result_path)},
               "authoring_request": {"path": str(replay_request_path), "sha256": sha(replay_request_path)},
               "authoring_result": {"path": str(replay_result_path), "sha256": sha(replay_result_path)},
               "host_build": {"path": str(host_build_path), "sha256": sha(host_build_path)},
               "binding_input": {"path": str(binding_path), "sha256": sha(binding_path)},
               "portable_binding": {"path": str(portable_path.resolve()), "sha256": sha(portable_path)},
               "logical_binding": {"path": str(wrapper_path.resolve()), "sha256": sha(wrapper_path)},
               "compiler_context": factory_descriptor,
               "configuration_qualification": {"path": str(qualification_path), "sha256": sha(qualification_path)},
               "source_manifest": {"path": str(manifest_path), "sha256": sha(manifest_path), "bytes": manifest_path.stat().st_size},
               "factory_context_consumed_pin_count": len(factory_pins),
               "roundtrip": {"path": str(roundtrip_path.resolve()), "sha256": sha(roundtrip_path)},
               "codec_source": {"path": str(CODEC.resolve()), "sha256": codec_sha},
               "projector_source": {"path": str(Path(__file__).resolve()), "sha256": sha(Path(__file__))},
               "host_source_pin_count": len(host_pins), "authoring_input_pin_count": len(author_pins),
               "setup_hash_unbound": proof["setup_hash_without_binding"],
               "setup_hash_bound": proof["setup_hash_with_binding"],
               "logical_binding_fingerprint": proof["logical_binding_fingerprint"],
               "negative_controls": proof["negative_controls"],
               "world_allocated": False, "native_capture": False, "runtime_admission": False,
               "source_fit_certified": False, "mission_ready": False}
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
    return receipt


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--request", type=Path, required=True)
    parser.add_argument("--result", type=Path, required=True)
    parser.add_argument("--host-build", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--qualification", type=Path, default=QUALIFICATION)
    parser.add_argument("--source-manifest", type=Path)
    args = parser.parse_args()
    print(json.dumps(run(args.request, args.result, args.host_build, args.output_dir,
                         args.qualification, args.source_manifest), sort_keys=True))


if __name__ == "__main__":
    main()
