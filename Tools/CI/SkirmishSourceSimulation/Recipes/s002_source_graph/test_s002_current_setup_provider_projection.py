import importlib.util
import copy
import json
import shutil
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).with_name("run_s002_current_setup_provider_projection.py")
SPEC = importlib.util.spec_from_file_location("s002_current_setup_provider_projection", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
SPEC.loader.exec_module(MODULE)


def synthetic_readback_tree(temp_root: Path):
    """Copy a complete pinned input set and build a synthetic positive receipt tree."""
    real_root = MODULE.ROOT.resolve()
    root = temp_root.resolve() / "project"
    readback_rel = Path("T Logs/SkirmishProduction/s002-prefab-importer-readback-20261103-T2")
    partial_rel = Path("T Logs/SkirmishProduction/s002-current-provider-partial-targets-20261103-T4")
    readback_source = real_root / readback_rel
    original_receipt = json.loads((readback_source / "receipt.json").read_text())
    partial_receipt_rel = Path(original_receipt["partial_source_receipt"]["path"])
    partial_evidence_rel = Path(original_receipt["partial_source_evidence"]["path"])
    output_artifacts = {partial_receipt_rel.as_posix(), partial_evidence_rel.as_posix()}

    for relative in original_receipt["source_pins_before"]:
        if relative in output_artifacts:
            continue
        source = real_root / relative
        destination = root / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, destination)

    def remap_pin_map(pins):
        result = {}
        for raw, _digest in pins.items():
            source = Path(raw).resolve(strict=True)
            relative = source.relative_to(real_root).as_posix()
            target = root / relative
            result[str(target)] = MODULE.sha(target.read_bytes())
        return result

    partial_receipt = json.loads((real_root / partial_receipt_rel).read_text())
    partial_evidence = json.loads((real_root / partial_evidence_rel).read_text())
    mapped_partial_pins = remap_pin_map(partial_receipt["source_pins"])
    partial_evidence["source_pins"] = mapped_partial_pins
    partial_evidence_path = root / partial_evidence_rel
    partial_evidence_path.parent.mkdir(parents=True, exist_ok=True)
    partial_evidence_path.write_text(json.dumps(partial_evidence, indent=2) + "\n")
    inspector = root / "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/inspect_s002_prefab_config_targets.py"
    projector = root / "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/run_s002_current_setup_provider_projection.py"
    partial_receipt["source_pins"] = mapped_partial_pins
    partial_receipt["evidence"] = {"path": partial_evidence_rel.as_posix(),
                                    "sha256": MODULE.sha(partial_evidence_path.read_bytes())}
    partial_receipt["runner"] = {"path": str(inspector), "sha256": MODULE.sha(inspector.read_bytes())}
    partial_receipt["projection_runner"] = {"path": str(projector), "sha256": MODULE.sha(projector.read_bytes())}
    partial_receipt_path = root / partial_receipt_rel
    partial_receipt_path.parent.mkdir(parents=True, exist_ok=True)
    partial_receipt_path.write_text(json.dumps(partial_receipt, indent=2) + "\n")

    before = {}
    for relative in original_receipt["source_pins_before"]:
        path = root / relative
        before[relative] = MODULE.sha(path.read_bytes())
    receipt = copy.deepcopy(original_receipt)
    observation_rel = Path(readback_rel / "observation.json")
    manifest_rel = Path(readback_rel / "source-pin-manifest.json")
    unity_log_rel = Path(readback_rel / "unity.log")
    wrapper_log_rel = Path(readback_rel / "wrapper.log")
    observation_path, manifest_path = root / observation_rel, root / manifest_rel
    observation = json.loads((readback_source / "observation.json").read_text())
    observation["source_pins_before"] = before
    observation["source_pins_after"] = before
    observation_path.parent.mkdir(parents=True, exist_ok=True)
    observation_path.write_text(json.dumps(observation, indent=2) + "\n")
    manifest = json.loads((readback_source / "source-pin-manifest.json").read_text())
    manifest["source_pins"] = before
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")
    shutil.copyfile(readback_source / "unity.log", root / unity_log_rel)
    shutil.copyfile(readback_source / "wrapper.log", root / wrapper_log_rel)

    receipt["source_pins_before"] = before
    receipt["source_pins_after"] = before
    receipt["partial_source_receipt"] = {"path": partial_receipt_rel.as_posix(),
                                          "sha256": MODULE.sha(partial_receipt_path.read_bytes())}
    receipt["partial_source_evidence"] = {"path": partial_evidence_rel.as_posix(),
                                           "sha256": MODULE.sha(partial_evidence_path.read_bytes())}
    receipt["pin_manifest"] = {"path": manifest_rel.as_posix(),
                               "sha256_before": MODULE.sha(manifest_path.read_bytes()),
                               "sha256_after": MODULE.sha(manifest_path.read_bytes())}
    receipt["observation"] = {"path": observation_rel.as_posix(), "sha256": MODULE.sha(observation_path.read_bytes())}
    receipt["unity_log"] = {"path": unity_log_rel.as_posix(),
                            "sha256": MODULE.sha((root / unity_log_rel).read_bytes())}
    receipt["wrapper"]["log"] = wrapper_log_rel.as_posix()
    receipt["wrapper"]["log_sha256"] = MODULE.sha((root / wrapper_log_rel).read_bytes())
    command = receipt["wrapper"]["command"]
    command[2] = str(root / "Tools/CI/invoke_unity_macos.sh")
    command[command.index("--log") + 1] = str(root / unity_log_rel)
    command[command.index("-s002-prefab-importer-output") + 1] = str(observation_path)
    command[command.index("-s002-prefab-importer-pins") + 1] = str(manifest_path)
    receipt["wrapper"]["command"] = command
    receipt_path = root / readback_rel / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n")
    return root, receipt_path


class S002CurrentSetupProviderProjectionTests(unittest.TestCase):
    def test_role_path_or_runtime_key_mutation_is_rejected(self):
        role = {"SourceConfigPath": "Assets/Game/Configs/Prefabs/Rifle.asset",
                "RuntimePrefabKey": "Unit_Rifle"}
        with self.assertRaisesRegex(ValueError, "authored role catalog binding"):
            MODULE.require_force_role_binding({"role_id": "role.rifle", "source_config_path": "Assets/Other.asset",
                                                "runtime_prefab_key": "Unit_Rifle"}, {"role.rifle": role})
        with self.assertRaisesRegex(ValueError, "authored role catalog binding"):
            MODULE.require_force_role_binding({"role_id": "role.rifle", "source_config_path": "Assets/Game/Configs/Prefabs/Rifle.asset",
                                                "runtime_prefab_key": "Unit_Other"}, {"role.rifle": role})

    def test_missing_or_ambiguous_provider_membership_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "exactly one provider object"):
            MODULE.require_unique_key([], "Unit_Rifle", "UnitPrefabRegistry provider")
        rows = [{"key": "Unit_Rifle", "guid": "a" * 32, "file_id": 1},
                {"key": "Unit_Rifle", "guid": "b" * 32, "file_id": 2}]
        with self.assertRaisesRegex(ValueError, "exactly one provider object"):
            MODULE.require_unique_key(rows, "Unit_Rifle", "UnitPrefabRegistry provider")

    def test_provider_anchor_presence_is_not_misreported_as_root_object_proof(self):
        helper = MODULE.load_module(MODULE.GENERIC, "s002_local_id_test_helpers")
        with tempfile.TemporaryDirectory(prefix="s002-provider-local-id-") as temp:
            prefab = Path(temp) / "Unit_Rifle.prefab"
            # A PrefabInstance document anchor is not evidence that a referenced
            # imported local ID names the asset's root/main GameObject.
            prefab.write_text("%YAML 1.1\n--- !u!1001 &123\nPrefabInstance: {}\n")
            self.assertEqual("unresolved", MODULE.local_id_validation(helper, prefab, 456))
            self.assertEqual("anchor_present_unqualified", MODULE.local_id_validation(helper, prefab, 123))

    def test_crlf_meta_requires_one_guid_and_rejects_duplicate(self):
        with tempfile.TemporaryDirectory(prefix="s002-provider-guid-") as temp:
            meta = Path(temp) / "config.asset.meta"
            meta.write_bytes(b"fileFormatVersion: 2\r\nguid: " + b"a" * 32 + b"\r\n")
            self.assertEqual("a" * 32, MODULE.config_guid(meta))
            meta.write_bytes(b"guid: " + b"a" * 32 + b"\r\nguid: " + b"b" * 32 + b"\r\n")
            with self.assertRaisesRegex(ValueError, "exactly one lowercase config GUID"):
                MODULE.config_guid(meta)

    def test_config_override_rows_cannot_pair_across_yaml_list_entries(self):
        helper = MODULE.load_module(MODULE.GENERIC, "s002_override_row_test_helpers")
        wrong_target = b"a" * 32
        right_target = b"b" * 32
        config_guid = b"c" * 32
        raw = (b"%YAML 1.1\n--- !u!1001 &123\nPrefabInstance:\n"
               b"  m_Modification:\n    m_Modifications:\n"
               b"    - target: {fileID: 391336776749024018, guid: " + wrong_target + b", type: 3}\n"
               b"      propertyPath: price\n      value: 7\n      objectReference: {fileID: 0}\n"
               b"    - target: {fileID: 106347591433935587, guid: " + right_target + b", type: 3}\n"
               b"      propertyPath: config\n      value:\n"
               b"      objectReference: {fileID: 11400000, guid: " + config_guid + b", type: 2}\n")
        with tempfile.TemporaryDirectory(prefix="s002-config-row-") as temp:
            rows, _ = MODULE.exact_config_rows(MODULE.ROOT, helper, raw, "mutated prefab")
        self.assertEqual(1, len(rows))
        self.assertEqual(106347591433935587, rows[0]["target"]["fileID"])
        self.assertEqual(right_target.decode(), rows[0]["target"]["guid"])
        self.assertEqual(config_guid.decode(), rows[0]["objectReference"]["guid"])

    def test_strict_prefab_parser_rejects_duplicate_keys_in_override_document(self):
        helper = MODULE.load_module(MODULE.GENERIC, "s002_duplicate_override_test_helpers")
        raw = (b"%YAML 1.1\n--- !u!1001 &123\nPrefabInstance:\n"
               b"  m_Modification:\n    m_Modifications:\n"
               b"    - target: {fileID: 1, guid: " + b"a" * 32 + b", type: 3}\n"
               b"      propertyPath: config\n      propertyPath: price\n")
        with tempfile.TemporaryDirectory(prefix="s002-config-duplicate-") as temp:
            with self.assertRaisesRegex(ValueError, "strict Unity document rejected"):
                MODULE.exact_config_rows(MODULE.ROOT, helper, raw, "duplicate mutation")

    def test_actual_vehicle_variant_partial_evidence_preserves_unresolved_self_target(self):
        helper = MODULE.load_module(MODULE.GENERIC, "s002_actual_partial_test_helpers")
        prefab = MODULE.ROOT / "Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab"
        evidence = MODULE.inspect_prefab_config_targets(MODULE.ROOT, prefab, MODULE.ROOT / MODULE.UNIT_OWNER, helper)
        self.assertEqual("PartialUnresolved", evidence["status"])
        self.assertFalse(evidence["complete_provider_join"])
        self.assertEqual(2, len(evidence["rows"]))
        self.assertEqual("serialized_expected_authoring_component", evidence["rows"][0]["target_resolution"])
        self.assertEqual(106347591433935587, evidence["rows"][0]["target_file_id"])
        self.assertEqual("unresolved_local_id_not_in_serialized_document", evidence["rows"][1]["target_resolution"])
        self.assertEqual(391336776749024018, evidence["rows"][1]["target_file_id"])
        self.assertEqual("9ce6998c034284d98a89b5badc6e2f24", evidence["rows"][1]["target_guid"])

    def test_partial_inspection_refuses_to_overwrite_existing_output(self):
        script = Path(__file__).with_name("inspect_s002_prefab_config_targets.py")
        spec = importlib.util.spec_from_file_location("s002_partial_inspection", script)
        module = importlib.util.module_from_spec(spec)
        assert spec and spec.loader
        spec.loader.exec_module(module)
        with tempfile.TemporaryDirectory(prefix="s002-partial-output-") as temp:
            out = Path(temp) / "existing"
            out.mkdir()
            with self.assertRaisesRegex(ValueError, "fresh inspection output directory"):
                module.run(MODULE.ROOT / "Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab", out)

    def test_partial_inspection_pins_helper_and_reader_with_pre_post_verifier(self):
        script = Path(__file__).with_name("inspect_s002_prefab_config_targets.py")
        spec = importlib.util.spec_from_file_location("s002_partial_pin_inspection", script)
        module = importlib.util.module_from_spec(spec)
        assert spec and spec.loader
        spec.loader.exec_module(module)
        with tempfile.TemporaryDirectory(prefix="s002-partial-pins-") as temp:
            sample = Path(temp) / "helper.py"
            sample.write_text("before")
            pins = module.capture_pins([sample])
            module.verify_pins(pins, "test")
            sample.write_text("changed")
            with self.assertRaisesRegex(ValueError, "test source pin changed"):
                module.verify_pins(pins, "test")
        with tempfile.TemporaryDirectory(prefix="s002-partial-receipt-") as temp:
            out = Path(temp) / "evidence"
            receipt = module.run(MODULE.ROOT / "Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab", out)
            expected = {str(module.GENERIC.resolve()), str(module.READER.resolve())}
            self.assertTrue(expected.issubset(receipt["source_pins"]))
            self.assertEqual(module.sha(module.GENERIC.read_bytes()), receipt["source_pins"][str(module.GENERIC.resolve())])
            self.assertEqual(module.sha(module.READER.read_bytes()), receipt["source_pins"][str(module.READER.resolve())])

    def test_importer_identity_witness_is_exactly_scoped_to_one_child_target(self):
        unit_owner = MODULE.ROOT / MODULE.UNIT_OWNER
        witness = {"owner_prefab_path": MODULE.IMPORTER_TARGET_PATH,
                   "owner_prefab_sha256": "d" * 64,
                   "target_guid": MODULE.IMPORTER_TARGET_GUID,
                   "target_file_id": MODULE.IMPORTER_TARGET_FILE_ID,
                   "target_path": MODULE.IMPORTER_TARGET_PATH,
                   "target_type": MODULE.IMPORTER_TARGET_TYPE,
                   "unit_owner_script_path": str(unit_owner.resolve()),
                   "unit_owner_script_guid": MODULE.script_guid(unit_owner)}
        args = (MODULE.IMPORTER_TARGET_PATH, "d" * 64, MODULE.IMPORTER_TARGET_GUID,
                MODULE.IMPORTER_TARGET_FILE_ID, MODULE.script_guid(unit_owner))
        self.assertTrue(MODULE.importer_witness_matches_target(witness, *args))
        for field, changed in (("owner_prefab_path", "Assets/Other.prefab"), ("owner_prefab_sha256", "e" * 64),
                               ("target_guid", "f" * 32), ("target_file_id", 123),
                               ("target_path", "Assets/Game/Prefabs/Vehicles/Other.prefab"),
                               ("target_type", "Game.Authoring.Other"),
                               ("unit_owner_script_path", str(MODULE.ROOT / "Assets/Other.cs")),
                               ("unit_owner_script_guid", "1" * 32)):
            mutation = dict(witness)
            mutation[field] = changed
            self.assertFalse(MODULE.importer_witness_matches_target(mutation, *args), field)
        self.assertFalse(MODULE.importer_witness_matches_target(
            witness, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab", *args[1:]))

    def test_observation_identity_control_accepts_exact_readback_and_rejects_mutations(self):
        observation_path = MODULE.ROOT / "T Logs/SkirmishProduction/s002-prefab-importer-readback-20261103-T1/observation.json"
        observation = json.loads(observation_path.read_text())
        observed = MODULE.validate_importer_observation_identity(observation)
        self.assertEqual(MODULE.IMPORTER_TARGET_GUID, observed["target_guid"])
        self.assertEqual(str(MODULE.IMPORTER_TARGET_FILE_ID), observed["target_local_id"])
        for field, changed in (("target_guid", "0" * 32), ("target_local_id", "1"),
                               ("target_type", "Game.Authoring.Other"),
                               ("config_guid", "f" * 32), ("config_local_id", "7")):
            mutated = copy.deepcopy(observation)
            row = next(row for row in mutated["config_overrides"]
                       if row.get("target_guid") == MODULE.IMPORTER_TARGET_GUID)
            row[field] = changed
            with self.assertRaisesRegex(ValueError, "exact serialized identities"):
                MODULE.validate_importer_observation_identity(mutated)

    def test_partial_source_rows_bind_both_base_and_child_config_targets(self):
        evidence_path = MODULE.ROOT / "T Logs/SkirmishProduction/s002-current-provider-partial-targets-20261103-T2/partial-source-evidence.json"
        evidence = json.loads(evidence_path.read_text())
        target = MODULE.validate_partial_target_evidence(evidence, MODULE.ROOT)
        self.assertEqual(MODULE.IMPORTER_TARGET_GUID, target["target_guid"])
        self.assertEqual(MODULE.IMPORTER_TARGET_FILE_ID, target["target_file_id"])
        self.assertEqual(MODULE.IMPORTER_CONFIG_GUID, target["config_guid"])
        for idx, field, changed in ((0, "target_guid", "0" * 32),
                                    (1, "target_file_id", 2),
                                    (1, "config_guid", "f" * 32),
                                    (1, "target_resolution", "serialized_expected_authoring_component")):
            mutated = copy.deepcopy(evidence)
            mutated["rows"][idx][field] = changed
            with self.assertRaisesRegex(ValueError, "Child prefab serialized config row mismatch"):
                MODULE.validate_partial_target_evidence(mutated, MODULE.ROOT)

    def test_config_assignment_seam_uses_witness_only_for_exact_unserialized_target(self):
        helper = MODULE.load_module(MODULE.GENERIC, "s002_exact_witness_seam_helpers")
        prefab = MODULE.ROOT / MODULE.IMPORTER_PREFAB
        owner_script = MODULE.ROOT / MODULE.UNIT_OWNER
        owner_prefab = MODULE.ROOT / MODULE.IMPORTER_TARGET_PATH
        witness = {"owner_prefab_path": MODULE.IMPORTER_TARGET_PATH,
                   "owner_prefab_sha256": MODULE.sha(owner_prefab.read_bytes()),
                   "target_guid": MODULE.IMPORTER_TARGET_GUID,
                   "target_file_id": MODULE.IMPORTER_TARGET_FILE_ID,
                   "target_path": MODULE.IMPORTER_TARGET_PATH,
                   "target_type": MODULE.IMPORTER_TARGET_TYPE,
                   "unit_owner_script_path": str(owner_script.resolve()),
                   "unit_owner_script_guid": MODULE.script_guid(owner_script),
                   "source_pins": {}, "evidence_artifacts": {}}
        rows = MODULE.config_assignments(MODULE.ROOT, prefab, MODULE.script_guid(owner_script),
                                        MODULE.ROOT / MODULE.UNIT_CONFIG_SCRIPT, helper, {}, {}, witness)
        self.assertEqual("exact_unity_importer_identity_witness", rows["owner_resolution_basis"])
        matched = [row for row in rows["owner_bindings"]
                   if row["owner_resolution_basis"] == "exact_unity_importer_identity_witness"]
        self.assertEqual(1, len(matched))
        self.assertEqual(MODULE.IMPORTER_TARGET_GUID, matched[0]["owner_component_guid"])
        self.assertEqual(MODULE.IMPORTER_TARGET_FILE_ID, matched[0]["owner_component_file_id"])
        self.assertFalse(rows["effective_override_selection_verified"])
        self.assertFalse(rows["unity_effective_config_readback"])
        self.assertEqual(MODULE.IMPORTER_CONFIG_PATH, rows["config_path"])
        with self.assertRaisesRegex(ValueError, "lacks the exact current importer identity witness"):
            MODULE.config_assignments(MODULE.ROOT, prefab, MODULE.script_guid(owner_script),
                                      MODULE.ROOT / MODULE.UNIT_CONFIG_SCRIPT, helper, {}, {}, None)
        mutations = (
            {"target_file_id": MODULE.IMPORTER_TARGET_FILE_ID + 1},
            {"target_path": "Assets/Game/Prefabs/Vehicles/Other.prefab"},
            {"target_type": "Game.Authoring.Other"},
            {"owner_prefab_sha256": "0" * 64},
            {"unit_owner_script_path": str(MODULE.ROOT / "Assets/Other.cs")},
            {"unit_owner_script_guid": "1" * 32},
        )
        for mutation in mutations:
            wrong = dict(witness, **mutation)
            with self.subTest(mutation=mutation), self.assertRaisesRegex(
                    ValueError, "lacks the exact current importer identity witness"):
                MODULE.config_assignments(MODULE.ROOT, prefab, MODULE.script_guid(owner_script),
                                          MODULE.ROOT / MODULE.UNIT_CONFIG_SCRIPT, helper, {}, {}, wrong)

    def test_compiled_force_joins_exactly_one_independently_pinned_config_row(self):
        binding_a = {"owner_prefab_path": "Assets/Game/Prefabs/Characters/Unit.prefab",
                     "owner_component_file_id": 106347591433935587,
                     "owner_component_guid": MODULE.IMPORTER_BASE_TARGET_GUID,
                     "config_path": "Assets/Game/Configs/Prefabs/Base.asset",
                     "owner_resolution_basis": "serialized_yaml_component"}
        binding_b = {"owner_prefab_path": MODULE.IMPORTER_TARGET_PATH,
                     "owner_component_file_id": MODULE.IMPORTER_TARGET_FILE_ID,
                     "owner_component_guid": MODULE.IMPORTER_TARGET_GUID,
                     "config_path": "Assets/Game/Configs/Prefabs/Variant.asset",
                     "owner_resolution_basis": "exact_unity_importer_identity_witness"}
        prefab = {"config": {"owner_bindings": [binding_a, binding_b],
                              "effective_override_selection_verified": False}}
        selected = MODULE.require_force_config_match({"runtime_prefab_key": "Unit_Veh_APC_Slow",
                                                       "source_config_path": binding_b["config_path"]}, prefab)
        self.assertEqual(binding_b, selected["config"]["selected_source_config_binding"])
        self.assertFalse(selected["config"]["effective_override_selection_verified"])
        self.assertFalse(selected["config"]["unity_effective_config_readback"])
        with self.assertRaisesRegex(ValueError, "match exactly one"):
            MODULE.require_force_config_match({"runtime_prefab_key": "ambiguous",
                                               "source_config_path": "Assets/Game/Configs/Prefabs/Missing.asset"}, prefab)
        duplicate = {"config": {"owner_bindings": [binding_a, binding_b, dict(binding_b)],
                                 "effective_override_selection_verified": False}}
        with self.assertRaisesRegex(ValueError, "match exactly one"):
            MODULE.require_force_config_match({"runtime_prefab_key": "ambiguous",
                                               "source_config_path": binding_b["config_path"]}, duplicate)

    def test_distinct_variant_configs_remain_child_specific_under_shared_base_witness(self):
        helper = MODULE.load_module(MODULE.GENERIC, "s002_shared_base_config_rows")
        owner_script = MODULE.ROOT / MODULE.UNIT_OWNER
        base_prefab = MODULE.ROOT / MODULE.IMPORTER_TARGET_PATH
        witness = {"owner_prefab_path": MODULE.IMPORTER_TARGET_PATH,
                   "owner_prefab_sha256": MODULE.sha(base_prefab.read_bytes()),
                   "target_guid": MODULE.IMPORTER_TARGET_GUID,
                   "target_file_id": MODULE.IMPORTER_TARGET_FILE_ID,
                   "target_path": MODULE.IMPORTER_TARGET_PATH,
                   "target_type": MODULE.IMPORTER_TARGET_TYPE,
                   "unit_owner_script_path": str(owner_script.resolve()),
                   "unit_owner_script_guid": MODULE.script_guid(owner_script),
                   "source_pins": {}, "evidence_artifacts": {}}
        names = ("Unit_Veh_Light_Armored_Car", "Unit_Veh_APC_Slow", "Unit_Veh_Tank_USA",
                 "Unit_Veh_Truck_Tanker", "Unit_Veh_Truck_Tray")
        child_config_paths = set()
        for name in names:
            prefab = MODULE.ROOT / "Assets/Game/Prefabs/Vehicles" / (name + ".prefab")
            assignments = MODULE.config_assignments(
                MODULE.ROOT, prefab, MODULE.script_guid(owner_script),
                MODULE.ROOT / MODULE.UNIT_CONFIG_SCRIPT, helper, {}, {}, witness)
            rows = assignments["owner_bindings"]
            self.assertEqual(2, len(rows), name)
            base_row, child_row = rows
            self.assertEqual(MODULE.IMPORTER_BASE_TARGET_GUID, base_row["owner_component_guid"], name)
            self.assertEqual(MODULE.IMPORTER_TARGET_GUID, child_row["owner_component_guid"], name)
            child_config_paths.add(child_row["config_path"])
            projected = MODULE.require_force_config_match(
                {"runtime_prefab_key": name, "source_config_path": child_row["config_path"]},
                {"key": name, "config": assignments})
            self.assertEqual(child_row, projected["config"]["selected_source_config_binding"], name)
            self.assertFalse(projected["config"]["effective_override_selection_verified"], name)
        self.assertEqual(len(names), len(child_config_paths))

    def test_ground_staging_uses_separate_pinned_special_source_owner(self):
        helper = MODULE.load_module(MODULE.GENERIC, "s002_ground_staging_owner_helpers")
        pins = {}
        binding = MODULE.resolve_ground_staging_special_owner(MODULE.ROOT, helper, pins)
        self.assertEqual(MODULE.GROUND_STAGING_KEY, binding["key"])
        self.assertEqual(MODULE.GROUND_STAGING_PREFAB, binding["prefab_path"])
        self.assertTrue(binding["fallbacks_source_declared_but_not_executed"])
        self.assertFalse(binding["unity_asset_load_or_import_execution"])
        self.assertFalse(binding["generic_building_provider_membership"])
        self.assertFalse(binding["factory_execution"])
        self.assertFalse(binding["source_fit_verified"])
        self.assertFalse(binding["mission_ready"])
        self.assertIn(str(MODULE.ROOT / MODULE.GROUND_STAGING_PREFAB), pins)
        self.assertIn(str(MODULE.ROOT / MODULE.GROUND_STAGING_ACCESS), pins)
        self.assertIn(str(MODULE.ROOT / MODULE.GROUND_STAGING_VISUAL_OWNER), pins)
        provider = MODULE.load_provider_asset(MODULE.ROOT, helper, MODULE.BUILDING_CONFIG, "spawnables",
                                              MODULE.ROOT / MODULE.BUILDING_PROVIDER_SCRIPT)
        provider["pins"] = {}
        rows = MODULE.resolve_provider_rows(
            MODULE.ROOT, helper, provider, MODULE.ROOT / MODULE.BUILDING_OWNER,
            MODULE.ROOT / MODULE.BUILDING_CONFIG_SCRIPT, set(), {})
        MODULE.validate_ground_staging_absent_from_generic_provider(rows)
        self.assertNotIn(MODULE.GROUND_STAGING_KEY, {row["key"] for row in rows})

    def test_ground_staging_special_owner_rejects_missing_or_changed_routes(self):
        visual = (MODULE.ROOT / MODULE.GROUND_STAGING_VISUAL_OWNER).read_text()
        access = (MODULE.ROOT / MODULE.GROUND_STAGING_ACCESS).read_text()
        ids = (MODULE.ROOT / MODULE.GROUND_STAGING_ID_OWNER).read_text()
        MODULE.validate_ground_staging_source_contract(visual, access, ids)
        with self.assertRaisesRegex(ValueError, "source-owned method missing"):
            MODULE.validate_ground_staging_source_contract("", access, ids)
        with self.assertRaisesRegex(ValueError, "exact authored access path and order"):
            MODULE.validate_ground_staging_source_contract(visual.replace(
                "SkirmishGroundStagingPrefabAccess.TryLoadAuthored(",
                "SkirmishGroundStagingPrefabAccess.TryLoadOther("), access, ids)
        editor_start = access.index("#if UNITY_EDITOR")
        editor_end = access.index("#endif", editor_start) + len("#endif")
        resources_start = access.index("            prefab = Resources.Load<GameObject>(ResourcesKey);")
        resources_end = access.index("            SkirmishGroundStagingConfig config", resources_start)
        editor_block = access[editor_start:editor_end]
        resources_block = access[resources_start:resources_end]
        reordered = access[:editor_start] + resources_block + editor_block + access[resources_end:]
        with self.assertRaisesRegex(ValueError, "guarded authored, Resources, and builder fallback order"):
            MODULE.validate_ground_staging_source_contract(visual, reordered, ids)
        with self.assertRaisesRegex(ValueError, "exact current prefab path"):
            MODULE.validate_ground_staging_source_contract(visual, access.replace(
                MODULE.GROUND_STAGING_PREFAB, "Assets/Game/Prefabs/Other.prefab"), ids)
        with self.assertRaisesRegex(ValueError, "exact visual key"):
            MODULE.validate_ground_staging_source_contract(visual, access, ids.replace(
                "Building_GroundStaging", "Building_Staging_Other"))

    def test_ground_staging_special_owner_rejects_same_key_generic_provider_row(self):
        with self.assertRaisesRegex(ValueError, "must remain absent from generic"):
            MODULE.validate_ground_staging_absent_from_generic_provider([
                {"key": "Building_Barrack"}, {"key": MODULE.GROUND_STAGING_KEY}])

    def test_s002_building_scalar_source_inputs_join_eight_current_candidate_rows(self):
        helper = MODULE.load_module(MODULE.GENERIC, "s002_building_scalar_source_inputs")
        historical_candidate = MODULE.ROOT / "T Logs/SkirmishProduction/s002-current-provider-projection-20261103-T5/provider-projection.json"
        candidate = json.loads(historical_candidate.read_text())
        pins = {}
        inputs = MODULE.build_building_scalar_inputs(MODULE.ROOT, helper, candidate["structure_rows"], pins)
        expected = {
            "Building_Ammunition_Depot": (900, 19, 16, 45000, 100, 30.0),
            "Building_Barrack": (1200, 28, 15, 40000, 90, 30.0),
            "Building_Fuel_Bladder": (500, 14, 10, 18000, 40, 30.0),
            "Building_GroundStaging": (600, 20, 14, 20000, 0, 30.0),
            "Building_GuardTower": (700, 3, 4, 22000, 50, 30.0),
            "Building_OilPump": (650, 15, 4, 50000, 100, 30.0),
            "Building_Refinery": (1400, 25, 15, 80000, 160, 30.0),
            "Building_Satelite_Dish": (450, 10, 10, 20000, 45, 30.0),
        }
        actual = {}
        for row in inputs["rows"]:
            values = row["source_config_values"] if "source_config_values" in row else None
            if values is None:
                values = {"max_health": row["config_max_health"], "raw_price": row["config_raw_price"],
                          "raw_materials_cost": row["config_raw_materials_cost"],
                          "raw_production_duration_seconds": row["config_raw_production_duration_seconds"]}
            actual[row["key"]] = (values["max_health"], row["prefab_footprint_width"],
                                   row["prefab_footprint_depth"], values["raw_price"],
                                   values["raw_materials_cost"], values["raw_production_duration_seconds"])
            self.assertTrue(row["has_config"])
            self.assertFalse(row["unity_effective_prefab_state_verified"])
        self.assertEqual(expected, actual)
        self.assertEqual("S002BuildingDefinitionScalarMetadataInputs", inputs["kind"])
        self.assertFalse(inputs["world_allocated"])
        self.assertFalse(inputs["factory_execution"])
        self.assertFalse(inputs["mission_ready"])
        self.assertIn(str(MODULE.ROOT / MODULE.SCALAR_POLICY), pins)
        self.assertIn(str(MODULE.ROOT / MODULE.UNIT_SCALAR_POLICY), pins)
        self.assertIn(str(MODULE.ROOT / (MODULE.UNIT_SCALAR_POLICY + ".meta")), pins)
        self.assertIn(str(MODULE.ROOT / MODULE.BUILDING_OWNER), pins)

    def test_s002_source_host_calls_shared_scalar_policy_and_runs_edge_controls(self):
        binding = MODULE.BINDING.read_text()
        self.assertIn("BuildingDefinitionAuthoringScalarProjection.Project(source)", binding)
        self.assertIn("RunScalarPolicyControls();", binding)
        self.assertIn("float.NaN", binding)
        self.assertIn("float.PositiveInfinity", binding)
        self.assertIn("float.NegativeInfinity", binding)
        self.assertIn("--project-building-metadata-scalars", binding)
        self.assertIn("--unit-production-metadata-policy-controls", binding)
        self.assertIn("RunUnitProductionMetadataPolicyControls();", binding)
        compiled_sources = MODULE.append_source_host_sources([], MODULE.ROOT)
        self.assertEqual(compiled_sources, [str(MODULE.BINDING.resolve()),
                                            str((MODULE.ROOT / MODULE.SCALAR_POLICY).resolve()),
                                            str((MODULE.ROOT / MODULE.UNIT_SCALAR_POLICY).resolve())])
        with self.assertRaisesRegex(ValueError, "must not source-define scalar policy types"):
            MODULE.append_source_host_sources(["-r:/tmp/Game.Components.dll"], MODULE.ROOT)

    def test_unit_metadata_owner_uses_shared_policy_without_reapplying_config_or_replacing_transport(self):
        owner = (MODULE.ROOT / "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs").read_text()
        authoring = (MODULE.ROOT / "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs").read_text()
        editor_control = (MODULE.ROOT / "Assets/Game/Scripts/Editor/Skirmish/S002UnitProductionMetadataScalarParityValidation.cs").read_text()
        self.assertIn("UnitProductionMetadataScalarPolicy.Project(authoring.CaptureProductionMetadataScalarSource())", owner)
        self.assertIn("authoring.ProductionTransportPrefab,", owner)
        self.assertNotIn("ApplyConfigIfAvailable", owner)
        self.assertIn("config != null && config.IsAirUnit", authoring)
        self.assertIn("productionDurationSeconds,", authoring)
        self.assertIn("ReferenceEquals(Field(metadata, \"ProductionTransportPrefab\"), transport)", editor_control)
        self.assertIn("float.NaN", editor_control)
        self.assertIn("float.PositiveInfinity", editor_control)
        self.assertIn("float.NegativeInfinity", editor_control)

    def test_unit_metadata_source_adapter_uses_pinned_resolved_getter_fields_and_policy_extraction(self):
        adapter = (MODULE.ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/426_CapturedCatalogAuthoringValues.cs").read_text()
        self.assertIn("CreateResolvedProductionMetadataScalarSource()", adapter)
        self.assertIn("already source-resolved original getter outputs", adapter)
        self.assertIn("ProductionTransportPrefab", adapter)
        for manifest_name in ("source_manifest.plan.json", "source_manifest.s003.plan.json",
                              "source_manifest.s026.plan.json", "source_manifest.s073-ai-experimental.plan.json"):
            manifest = json.loads((MODULE.ROOT / "Tools/CI/SkirmishSourceSimulation" / manifest_name).read_text())
            policy = [item for item in manifest["sources"] if item.get("id") == "418_UnitProductionMetadataScalarPolicy.cs"]
            owner = [item for item in manifest["sources"] if item.get("id") == "429_ProductionMetadataSource.cs"]
            self.assertEqual(len(policy), 1)
            self.assertEqual(policy[0]["path"], MODULE.UNIT_SCALAR_POLICY)
            self.assertEqual(len(owner), 1)
            bindings = {item["from"]: item["to"] for item in owner[0]["bindings"]}
            self.assertEqual(bindings["authoring.CaptureProductionMetadataScalarSource()"],
                             "authoring.CreateResolvedProductionMetadataScalarSource()")

    def test_historical_readback_receipt_is_rejected_after_pinned_projector_changes(self):
        receipt = MODULE.ROOT / "T Logs/SkirmishProduction/s002-prefab-importer-readback-20261103-T1/receipt.json"
        with self.assertRaisesRegex(ValueError, "Importer readback source pin is stale"):
            MODULE.validate_importer_identity_receipt(MODULE.ROOT, receipt)

    def test_historical_partial_receipt_is_not_relabelled_current_after_projector_change(self):
        script = Path(__file__).with_name("run_s002_prefab_importer_identity_readback.py")
        spec = importlib.util.spec_from_file_location("s002_readback_freshness", script)
        module = importlib.util.module_from_spec(spec)
        assert spec and spec.loader
        spec.loader.exec_module(module)
        receipt = MODULE.ROOT / "T Logs/SkirmishProduction/s002-current-provider-partial-targets-20261103-T2/receipt.json"
        with self.assertRaisesRegex(ValueError, "Partial source pin changed"):
            module.load_partial_receipt(receipt)

    def test_full_importer_receipt_validator_accepts_synthetic_pinned_tree_and_rejects_mutations(self):
        with tempfile.TemporaryDirectory(prefix="s002-importer-receipt-tree-") as temp:
            original_root = MODULE.ROOT
            synthetic_root, receipt_path = synthetic_readback_tree(Path(temp))
            try:
                MODULE.ROOT = synthetic_root
                witness = MODULE.validate_importer_identity_receipt(synthetic_root, receipt_path)
                self.assertEqual(MODULE.IMPORTER_TARGET_GUID, witness["target_guid"])
                self.assertEqual(MODULE.IMPORTER_TARGET_FILE_ID, witness["target_file_id"])
                self.assertEqual(MODULE.IMPORTER_TARGET_TYPE, witness["target_type"])
                self.assertIn(str(synthetic_root / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_typed_config_graph.py"),
                              witness["source_pins"])

                receipt = json.loads(receipt_path.read_text())
                receipt["wrapper"]["exit_code"] = 19
                bad_wrapper = synthetic_root / "T Logs/SkirmishProduction/s002-prefab-importer-readback-20261103-T2/receipt-bad-wrapper.json"
                bad_wrapper.write_text(json.dumps(receipt, indent=2) + "\n")
                with self.assertRaisesRegex(ValueError, "wrapper must exit zero"):
                    MODULE.validate_importer_identity_receipt(synthetic_root, bad_wrapper)

                observation_path = synthetic_root / "T Logs/SkirmishProduction/s002-prefab-importer-readback-20261103-T2/observation.json"
                observation = json.loads(observation_path.read_text())
                target = next(row for row in observation["config_overrides"]
                              if row.get("target_guid") == MODULE.IMPORTER_TARGET_GUID)
                target["target_type"] = "Game.Authoring.Other"
                observation_path.write_text(json.dumps(observation, indent=2) + "\n")
                bad_observation = json.loads(receipt_path.read_text())
                bad_observation["observation"]["sha256"] = MODULE.sha(observation_path.read_bytes())
                bad_observation_path = synthetic_root / "T Logs/SkirmishProduction/s002-prefab-importer-readback-20261103-T2/receipt-bad-observation.json"
                bad_observation_path.write_text(json.dumps(bad_observation, indent=2) + "\n")
                with self.assertRaisesRegex(ValueError, "exact serialized identities"):
                    MODULE.validate_importer_identity_receipt(synthetic_root, bad_observation_path)
            finally:
                MODULE.ROOT = original_root


if __name__ == "__main__":
    unittest.main()
