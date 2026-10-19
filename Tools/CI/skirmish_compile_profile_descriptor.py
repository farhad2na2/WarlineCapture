"""Exact1400 compile-profile METADATA only. Never calls a compiler or admission backend.

create: --project ROOT --context context.json --output ROOT/Logs/SkirmishProduction/new.json
verify: --project ROOT --verify descriptor.json --expected-sha256 <descriptor_sha256>

Context/evidence references are structural, UNVERIFIED pointers. Even complete pointers
stay Pending; this tool cannot establish RSP consumption, retained origin or freshness.
The fixed authority is the reviewed1400 named scope, not the project's claimed HEAD.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path, PurePosixPath
import re

AUTHORITY_COMMIT = "1400fd782be7ebbafef0cc90b59552089727a0a5"
BACKEND_SHA256 = "f27135fb3c7934c3fc151f19c82f5537188043271d48098c1872f54860c5070a"
NATIVE = tuple(sorted("""Game.Authoring Game.Components Game.Composition Game.Configs
Game.Editor Game.Rendering Game.Runtime Game.Runtime.Combat Game.Runtime.Pathfinding
Game.Tests.Editor Game.Tests.MapPreparation.Native Game.Tests.PlayMode Game.UI.Contracts
Game.UI.Runtime Game.UI.ScenarioLab.Runtime Game.UI.Shell.Contracts.Ecs Game.UI.Shell.Ecs""".split()))
EXTRA = tuple(sorted("""Game.Catalog.Contracts Game.Narrative.Contracts Game.Missions.Contracts
Game.Skirmish.Contracts Game.Operations.Contracts Game.Tactical.Contracts
Game.Operations.Strategic Game.Rendering.Contracts Game.Narrative.Runtime""".split()))
CLEAN = tuple(sorted(NATIVE + EXTRA))
# Raw immutable Git blob pins: asmdef and its GUID metadata, not CSharp source bodies.
METADATA_PINS = {
    'Assets/Game/Scripts/Authorings/Game.Authoring.asmdef': ('2a9406d8ed8007c05b2602ae3a5bc43f503fb0924f928c87a5b5ede1529482a8', '3eefb2e2597b225ee87afeb7bbbb2e5c34d8fb2a93aedf2b356c52c3c69fa9f0'),
    'Assets/Game/Scripts/Catalog/Contracts/Game.Catalog.Contracts.asmdef': ('8142bc76ae9afdbe2622b7ffd6b5384e9cd73039d95970490019adb56c08a029', '12564edf3274fb4adc9d7db5b46a233238c0b69d2862724d121ad72fefe0f4cb'),
    'Assets/Game/Scripts/Components/Game.Components.asmdef': ('37202a4740993abaa19ff29f09d482e1b6b2fc2414f0fa9fb4277826ea996979', '28950e125c8964a085cdf66f2f6b6c4a6b34ac536914cb7d42c4f6a7feabd0b8'),
    'Assets/Game/Scripts/Composition/Game.Composition.asmdef': ('ebb2ee20bb7f6cd4c4a8e7540d53bc9a8a40cab9977be0442ac8938a6b54b965', '830f929850d9c3bbe07b95662ca2bdc03a56f19caa4c542045b04d54e2ad37ae'),
    'Assets/Game/Scripts/Configs/Game.Configs.asmdef': ('78542ac6909ee604a6993028f87c7ac61b3da5aa18473ac40cc181afb2d2d5d2', 'a63c2d4f7c15528baac775c0c35e5be4612a4f4b333a887e33768e9cfeab929e'),
    'Assets/Game/Scripts/Contracts/Game.Tactical.Contracts.asmdef': ('30fac3098673e5bd8d9f7503157f43cf2870e9035a87f5d8afc59030c1151b5e', 'cdb6747c4330606d7ff9f7a92ebfc6608833ede4735765aa5c5c136af2ca1f90'),
    'Assets/Game/Scripts/Editor/Game.Editor.asmdef': ('ea33d806db2c6af67a5b1464a6566ee32d5209d51cb4c2e25806b66a1c66dc8b', '067108409c1a06a304e510162bb4b7e387283c0c5f774cfc4193bcc01e804540'),
    'Assets/Game/Scripts/Game.Runtime.asmdef': ('8975499d6c536f7c8ea7e1f245e5964f5e61e785d111866c94256985889a16e5', '6a7cc30139d637263c214e4f51636b6efc8088fafe8afec58267034a168e7573'),
    'Assets/Game/Scripts/Missions/Contracts/Game.Missions.Contracts.asmdef': ('35e65800a48d49bbd49b8ca248050006c532c7dd40c07151c3f722a79cab3db1', '66d72c2bdcfe2d66710d18121ee1856ed92002309835b821e075e85341eed3b0'),
    'Assets/Game/Scripts/Narrative/Contracts/Game.Narrative.Contracts.asmdef': ('e9691a342d92b123af23fb4ea7a2e6205997dd7320f088d0e3a948bd068428bf', '739a75a66c75040b0e9b15974345fec878aeae85235c8eab74df83dc7509a836'),
    'Assets/Game/Scripts/Narrative/Runtime/Game.Narrative.Runtime.asmdef': ('2240a77813c8f59ba9ec71545d96971c097ab59c8138d6095dea9ae3298e2278', '0677db46bc92c20051415030ac450e3b15275f80f4756dd4a9db68ce8fee60b6'),
    'Assets/Game/Scripts/Operations/Capture/Game.Operations.Capture.asmdef': ('a1f830e7ace7090a479229d99de9e76eb42586dcec653c9b114a4a03b82d6573', '9eb8359289fb1a09e40323bb44e1c0be70a4e6b0753168557007c54ca4aef75c'),
    'Assets/Game/Scripts/Operations/Content/Game.Operations.Content.asmdef': ('c1690d3ea4bd437b2c95489dfdbe1c49f8914c3f59617194a3e4eeacde83ee78', 'bc0e944a42e0aa12b8a713ff90385e5c530738b0e09efdebb782fc9aaa5fd0c7'),
    'Assets/Game/Scripts/Operations/Contracts/Game.Operations.Contracts.asmdef': ('0e8796eb34363fdbfd89ae836c813a20e4ac01741ff571eda6efb398dffcc4e4', '2439a0ba28b1b425d4ed66736244319aa19e9b90e6cfe76ef7ef40c916ac578b'),
    'Assets/Game/Scripts/Operations/Loop/Game.Operations.Loop.asmdef': ('295579a3eca4c3ef1ed337482bd6ad03e480ce41103635ff6aa190ffe6e2ee25', '091ebfd7db00409f41e65ad98afda740d1f7dbc461b57f02af697e8751271485'),
    'Assets/Game/Scripts/Operations/Strategic/Game.Operations.Strategic.asmdef': ('5121a793c22450af8317a5a63fba0be8e3c058dafd5791e1c1c52aba89d0c26b', '282521d420e560f53ae70c7f8666cd6c4c8f919db2f3c9fc6df4193337925718'),
    'Assets/Game/Scripts/Operations/Tactical/Game.Operations.Tactical.asmdef': ('0b64146df232552d427f43fa1edbb98af87fa828440516823939fcbef84d97a5', '7c7be77c61a860a63a414e87e1f0dd98002e921a616eea2d16f8c612fabc3ce4'),
    'Assets/Game/Scripts/Rendering/Contracts/Game.Rendering.Contracts.asmdef': ('9cd88cb2f0ae956c453f2bf188b5ae8e94bfb8681f21d0c00fd52efdf9f33286', '46b6f1114817d659a12edcf3223a0a7fab76e7eb420b38590bf58e963c23c330'),
    'Assets/Game/Scripts/Rendering/Game.Rendering.asmdef': ('d6148f39800348e5b254abbc9a4dd8691639f221d3701af7245fea2b6efb1b2d', '3594a20e6377d4d273c5d4ea1ce8a54a4bab99038d8b110f0c634c3c657f6aa4'),
    'Assets/Game/Scripts/Skirmish/Contracts/Game.Skirmish.Contracts.asmdef': ('5f7afa2d827cc6192b4a0ad500736deecd8253c206261d184048c4984c25a4d5', '135ef9a59c81f2cfca2fc3638890c5d275b98cf33fd52c3ee592cc6c547c78b5'),
    'Assets/Game/Scripts/Systems/Combat/Game.Runtime.Combat.asmdef': ('ef6041b67ae15400c3ff8b1f95e62caf1434318e4005b775ab7ef891c3688d25', '81dba2f5b7a66f94136a992787b356e8d382a6339f8da0326c42ab3469680427'),
    'Assets/Game/Scripts/Systems/Pathfinding/Surface/Game.Runtime.Pathfinding.asmdef': ('eca3a23a40b6c667c6ef6b790bdd727cd1b386315f9b4711e1461c205c651e67', '60d41c5d263aa857f4ca9fe4f91c94bfd72dc92d1c1d6400439a24fd02045e8d'),
    'Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef': ('684178e0705c008be375ebf4a92bd02fd835cc404de9028fa704245acfc07df0', '61369125c19fd8027947241cd29a5e298a025d92d74971189df42f57221f989a'),
    'Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef': ('b05d7a62f96bb19f02bbd89530e3956fec5af16210aaaa4b2a34a9f2c3174dc2', '74fe1c617cad9318493076d3a9cc7f423c39a3e4e2040e3876825011dd002f00'),
    'Assets/Game/Scripts/UI/ScenarioLab/Game.UI.ScenarioLab.Runtime.asmdef': ('5047fd7ccee8f5862486a0997d22ef6bbd02e7103af36aa383927a16fd692228', '4ad33c1182c3a9ce4e518aa1ead5fec9dc2e19bd2c8582b0bb9a286a2da62432'),
    'Assets/Game/Scripts/UI/Shell/Ecs/Contracts/Game.UI.Shell.Contracts.Ecs.asmdef': ('fd27284ed422462b1de1dd294e21ae09e6a487a06d1954d957bf046c3c0de254', 'a6c73a71fdb28332781d8524a7fb9f44f348cbdf5c9f962aeaff0e582dea9001'),
    'Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef': ('a0c8e9f71755e4af053a5b7467c06fe23e596cb2e62ee894c0bf1f024b075feb', '25531d6f4cd2a97800b8857ae599a7b91389c0453dcf780be6a48232b45a970f'),
    'Assets/Tests/Editor/Game.Tests.Editor.asmdef': ('6f60a3a6e2db030d2ff0a4c8c1959b47d34a6b2983c7ffdbc25e46b3e8ad5249', 'e8603d2b95c2c28344a467f5442a64e58d36a6046f3a39d99b99ad217648354b'),
    'Assets/Tests/Editor/Operations/Game.Operations.Tests.Editor.asmdef': ('0db8326395388e024dd13c3a01fee7efe3e8466ec637e793e2e743464cc2eb65', 'dd6bfaa50df9c8fc0376f77f532b30b9cb7f1700458f26a18cb0cc4ccb76759a'),
    'Assets/Tests/MapPreparationNative/Game.Tests.MapPreparation.Native.asmdef': ('f1b9cfe23714eaa9f2e76b0ebdef0596d1dd99e0e3ce687f4b02b9aefaf0d095', '797203117fcbf51ce89c8125f02e3aa1627689374e0af7be3884da2f80ac675a'),
    'Assets/Tests/PlayMode/Game.Tests.PlayMode.asmdef': ('c35bca9d659b63b7f8ba1647b7d3ae2d31c714bcefc414edf65e2ac56f71c5a8', '5d28aac0988f4fd1ad9ffab71f3187e0979eabcff98c03d36c71c3f328211978'),

}
FLAGS = dict(metadata_only=True, compile_admitted=False, native_compile_authorized=False,
             live_api_proved=False, qualified=False, mission_ready=False,
             queue_success_enabled=False, publication_enabled=False,
             android_player_built=False, device_accepted=False)
CONTEXT_REFS = ("selector", "source_snapshot", "toolchain", "native_epoch", "isolated_epoch")
EVIDENCE_REFS = ("rsp", "source_membership", "toolchain", "retained_origin", "per_consumer")
STATES = {
    "native-selected-rsp": {"ReportedFreshCsc", "ReportedCachedObservation"},
    "isolated-owning-graph": {"ReportedFreshCompile", "ReportedRetainedCompile"},
}


def need(condition, message):
    if not condition:
        raise ValueError(message)


def encoded(value):
    return (json.dumps(value, sort_keys=True, separators=(",", ":"),
                       ensure_ascii=False, allow_nan=False) + "\n").encode("utf-8")


def fingerprint(value):
    return hashlib.sha256(encoded(value)).hexdigest()


def ref(value):
    if value is None:
        return
    need(type(value) is dict and set(value) == {"path", "sha256"}, "Exact unverified reference")
    need(isinstance(value["path"], str) and value["path"] and "\0" not in value["path"], "Reference path")
    need(isinstance(value["sha256"], str) and re.fullmatch("[a-f0-9]{64}", value["sha256"]), "Reference digest")


def relative_path(value):
    need(isinstance(value, str) and value and "\\" not in value and "\0" not in value, "Canonical relative path")
    path = PurePosixPath(value)
    need(not path.is_absolute() and ".." not in path.parts and ":" not in value
         and path.as_posix() == value, "Canonical relative path")
    return path


def ordinary(root, relative):
    path = root / str(relative_path(relative))
    for part in (path, *path.parents):
        if part == root:
            break
        need(not part.is_symlink() and not getattr(part, "is_junction", lambda: False)(),
             "Metadata reparse path")
    need(path.is_file() and path.resolve().is_relative_to(root), "Ordinary metadata file")
    return path


def metadata(project):
    root = Path(project).resolve(strict=True)
    paths = set()
    for scope in ("Assets/Game/Scripts", "Assets/Tests"):
        folder = root / scope
        need(folder.is_dir(), "Assembly metadata scope missing")
        for path in folder.rglob("*"):
            need(not path.is_symlink() and not getattr(path, "is_junction", lambda: False)(),
                 "Metadata scope contains a reparse path")
            if path.suffix in (".asmdef", ".asmref"):
                paths.add(path.relative_to(root).as_posix())
    need(paths == set(METADATA_PINS), "Exact reviewed asmdef/asmref path membership changed")
    result, guids = {}, set()
    for path, (definition_hash, meta_hash) in sorted(METADATA_PINS.items()):
        raw = ordinary(root, path).read_bytes()
        raw_meta = ordinary(root, path + ".meta").read_bytes()
        need(hashlib.sha256(raw).hexdigest() == definition_hash and
             hashlib.sha256(raw_meta).hexdigest() == meta_hash, "Exact raw assembly metadata changed: " + path)
        definition = json.loads(raw.decode("utf-8-sig"))
        name = definition["name"]
        need(name not in result, "Duplicate assembly identity")
        match = re.findall(r"(?m)^guid: ([a-f0-9]{32})\r?$", raw_meta.decode("utf-8-sig"))
        need(len(match) == 1 and match[0] not in guids, "Exact unique assembly GUID")
        guids.add(match[0])
        result[name] = dict(path=path, sha256=definition_hash, meta_sha256=meta_hash,
                            guid=match[0], definition=definition)
    need(set(CLEAN) <= set(result), "Reviewed named profile member missing")
    return result


def dependency_closure(names, definitions):
    result = set(names)
    while True:
        following = result | {value for name in result
                              for value in definitions[name]["definition"]["references"]
                              if value in definitions}
        if following == result:
            return sorted(result)
        result = following


def source_owner(path, definitions):
    value = relative_path(path)
    need(value.suffix == ".cs", "Source ownership is for CSharp paths")
    matches = [(len(PurePosixPath(row["path"]).parent.parts), name)
               for name, row in definitions.items()
               if value.is_relative_to(PurePosixPath(row["path"]).parent)]
    need(matches, "Source outside reviewed assembly scope")
    return max(matches)[1]


def check_context(context):
    need(type(context) is dict and set(context) == {
        "platform", "editor_version", "target", "defines", "tests_included", *CONTEXT_REFS}, "Exact context fields")
    need((context["platform"], context["editor_version"], context["target"]) ==
         ("Windows", "6000.5.8f1", "Editor"), "Reviewed context family only")
    defines = context["defines"]
    need(defines is None or (type(defines) is list and all(isinstance(x, str) and x for x in defines)
                            and defines == sorted(set(defines))), "Canonical defines or unknown")
    need(context["tests_included"] is None or type(context["tests_included"]) is bool,
         "Typed test inclusion or unknown")
    need(context["tests_included"] is not False, "Known context excludes test owners")
    need(defines is None or "WARLINE_MAP_NATIVE_TEST_PLAYER" not in defines,
         "Known defines exclude the reviewed PlayMode owner")
    for key in CONTEXT_REFS:
        ref(context[key])


def observations(value, context, definitions):
    need(type(value) is list, "Observation reference list")
    output, seen = [], set()
    for row in value:
        need(type(row) is dict and set(row) == {
            "layer", "owner", "state", "epoch", "refs", "source_paths", "input_drift"}, "Exact unverified observation fields")
        layer, owner = row["layer"], row["owner"]
        need(layer in STATES, "Legacy named authority is unavailable; unknown layer")
        members = NATIVE if layer == "native-selected-rsp" else CLEAN
        need(owner in members and row["state"] in STATES[layer], "Owner/state not in named layer")
        need((layer, owner) not in seen, "Duplicate layer owner observation")
        seen.add((layer, owner))
        ref(row["epoch"])
        need(row["epoch"] == context["native_epoch" if layer == "native-selected-rsp" else "isolated_epoch"],
             "Observation epoch differs from bound context")
        need(type(row["refs"]) is dict and set(row["refs"]) == set(EVIDENCE_REFS), "Exact evidence reference keys")
        for item in row["refs"].values():
            ref(item)
        need(type(row["source_paths"]) is list and len(set(row["source_paths"])) == len(row["source_paths"]),
             "Unique claimed source paths")
        for path in row["source_paths"]:
            need(source_owner(path, definitions) == owner, "Nested asmdef owner claim differs")
        need(type(row["input_drift"]) is bool, "Typed reported input drift")
        pending = ["UnverifiedObservationReferences_NotActualCompilerEvidence"]
        pending.extend("Missing:" + key for key, item in row["refs"].items() if item is None)
        if row["state"] == "ReportedRetainedCompile":
            pending.append("RetainedOriginAndCompleteInputDependencyJoinsNotValidated")
        if row["input_drift"]:
            pending.append("ReportedInputDrift_NoValidatedPerConsumerWindow")
        output.append(dict(reference=row, status="Pending", pending=sorted(pending)))
    return sorted(output, key=lambda item: (item["reference"]["layer"], item["reference"]["owner"]))


def make_descriptor(project, context, reported=None):
    """Structural metadata only: no reference target is treated as a genuine receipt."""
    context, reported = copy.deepcopy(context), copy.deepcopy([] if reported is None else reported)
    check_context(context)
    definitions = metadata(project)
    need(dependency_closure(NATIVE, definitions) == list(CLEAN), "Exact reviewed Game dependency closure")
    records = observations(reported, context, definitions)

    def layer(names):
        owners = []
        for name in names:
            row = definitions[name]
            refs = row["definition"]["references"]
            owners.append(dict(owner=name, metadata=row,
                               evidence_state=("NativeOwnerEvidencePending" if names == NATIVE
                                               else "IsolatedOwnerEvidencePending"),
                               referenced_game_binary_state="PendingActualRSPBinaryBytesAndOrigin",
                               owning_dependencies=sorted(x for x in refs if x in names),
                               referenced_game_binary_dependencies=sorted(x for x in refs if x in definitions and x not in names),
                               external_binary_leaves=[dict(assembly=x, status="PendingActualBinaryPathHashAndOrigin")
                                                       for x in refs if x not in definitions]))
        remaining, ordered = set(names), []
        while remaining:
            ready = sorted(name for name in remaining if not
                           (set(definitions[name]["definition"]["references"]) & remaining))
            need(ready, "Cyclic owning dependency graph")
            ordered.extend(ready)
            remaining.difference_update(ready)
        return dict(owners=owners, owner_count=len(owners), topological_order=ordered,
                    status="MetadataOnly_PendingEvidence")

    result = dict(schema_version=1, kind="CompileProfileAuthorityDescriptor-v1",
                  status="MetadataOnly_PendingEvidence", flags=copy.deepcopy(FLAGS),
                  authority_commit=AUTHORITY_COMMIT,
                  producer_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                  protected_legacy_contract=dict(backend_sha256=BACKEND_SHA256,
                      adapter_lineage="b1dad3ec224d8da66a745ff17feaafffd83a1d1c",
                      acceptance_supported=False, backend_bytes_verified=False),
                  context=context, context_sha256=fingerprint(context),
                  source_metadata=definitions,
                  layers={"native-selected-rsp": layer(NATIVE), "isolated-owning-graph": layer(CLEAN),
                          "legacy-offline24": dict(owners=None, owner_count=None,
                              status="PendingMissingNamedAuthority_NoGuessedMembership")},
                  out_of_scope_game_owners=sorted(set(definitions) - set(CLEAN)),
                  other_scope="NonGame Assets, Packages, predefined assemblies and effective RSP membership are not inventoried as owning compilation here.",
                  observations=records,
                  pending=["ActualRSPAndSelectorEpochNotValidated", "ActualSourceAndToolchainNotValidated",
                           "ActualRetainedOriginsAndPerConsumerInputsNotValidated",
                           "NoNativeOrOfflineAdmissionProfileImplementation", "Legacy24NamedAuthorityMissing"])
    result["descriptor_sha256"] = fingerprint(result)
    return result


def validate_descriptor(value, project, expected_sha256):
    """Require an external immutable descriptor pin; resealing changed context is not enough."""
    need(type(value) is dict and isinstance(expected_sha256, str) and
         re.fullmatch("[a-f0-9]{64}", expected_sha256), "External descriptor pin required")
    need(value.get("descriptor_sha256") == expected_sha256, "Descriptor pin differs")
    refs = [item["reference"] for item in value["observations"]]
    expected = make_descriptor(project, value["context"], refs)
    need(value == expected, "Structural descriptor bytes/authority/context/profile changed")
    return dict(status="MetadataOnly_PendingEvidence", descriptor_sha256=expected_sha256,
                flags=copy.deepcopy(FLAGS))


def write_immutable(project, path, value):
    root = Path(project).resolve(strict=True)
    target = Path(path).absolute()
    allowed = root / "Logs/SkirmishProduction"
    need(target.is_relative_to(allowed) and ".." not in target.parts, "Local diagnostics destination required")
    for part in (target, *target.parents):
        if part == root:
            break
        need(not part.is_symlink() and not getattr(part, "is_junction", lambda: False)(), "Output reparse path")
    validate_descriptor(value, root, value["descriptor_sha256"])
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("xb") as stream:
        stream.write(encoded(value))
    return dict(path=str(target), sha256=hashlib.sha256(encoded(value)).hexdigest(),
                descriptor_sha256=value["descriptor_sha256"], status="MetadataOnly_PendingEvidence")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--context", type=Path)
    parser.add_argument("--observations", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--verify", type=Path)
    parser.add_argument("--expected-sha256")
    args = parser.parse_args()
    if args.verify:
        need(args.context is None and args.observations is None and args.output is None, "Verify mode only")
        result = validate_descriptor(json.loads(args.verify.read_bytes()), args.project, args.expected_sha256)
    else:
        need(args.context is not None and args.output is not None and args.expected_sha256 is None, "Create context/output required")
        value = make_descriptor(args.project, json.loads(args.context.read_bytes()),
                                json.loads(args.observations.read_bytes()) if args.observations else None)
        result = write_immutable(args.project, args.output, value)
    print(json.dumps(result, sort_keys=True))


if __name__ == "__main__":
    main()
