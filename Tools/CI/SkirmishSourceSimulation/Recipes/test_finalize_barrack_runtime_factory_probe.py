import hashlib
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).with_name("finalize_barrack_runtime_factory_probe.py")
SPEC = importlib.util.spec_from_file_location("barrack_runtime_factory_finalizer", SCRIPT)
FINALIZER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FINALIZER)
REPO = FINALIZER.ROOT


class BarrackRuntimeFactoryProbeFinalizerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="barrack-runtime-factory-finalizer-")
        self.root = Path(self.temp.name).resolve()
        self.capture = self.root / "capture.json"
        self.log = self.root / "editor.log"
        paths = set(FINALIZER.REQUIRED_SOURCES)
        paths.add(FINALIZER.PREFAB)
        paths.add(FINALIZER.PREFAB + ".meta")
        paths.update({"ProjectSettings/ProjectVersion.txt", "Packages/manifest.json", "Packages/packages-lock.json"})
        for relative in sorted(paths):
            source = REPO / relative
            target = self.root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
            meta = source.with_name(source.name + ".meta")
            if relative in FINALIZER.REQUIRED_SOURCES and meta.is_file():
                meta_target = self.root / (relative + ".meta")
                meta_target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(meta, meta_target)
        for relative in ("Library/ScriptAssemblies/Game.Runtime.dll",
                         "Library/ScriptAssemblies/Game.Composition.dll",
                         "Library/ScriptAssemblies/Game.Editor.dll"):
            target = self.root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes((relative + " fixture bytes").encode("utf-8"))
        self.doc = self.valid_document()
        self.write_doc()
        self.log.write_text(FINALIZER.MARKER + " prefab=1 identity=1 footprint=1 dependencies=1 mission=0 world=0\n",
                            encoding="utf-8")

    def tearDown(self):
        self.temp.cleanup()

    def valid_document(self):
        pins = {}
        for relative in sorted(FINALIZER.REQUIRED_SOURCES | {FINALIZER.PREFAB, FINALIZER.PREFAB + ".meta"}):
            pins[relative] = hashlib.sha256((self.root / relative).read_bytes()).hexdigest()
        assemblies = []
        for name in ("Game.Runtime", "Game.Composition", "Game.Editor"):
            path = "Library/ScriptAssemblies/" + name + ".dll"
            digest = hashlib.sha256((self.root / path).read_bytes()).hexdigest()
            pins[path] = digest
            assemblies.append({"name": name, "path": path, "sha256": digest})
        return {
            "schema_version": 1,
            "kind": "BarrackRuntimeFactoryDirectAssetSourceProbe",
            "status": "PassedDirectAssetFactorySourceProbe",
            "authority": "Current Unity AssetDatabase source identity, evaluated prefab contents, and original CreateRuntimeBuildingDefinition output",
            "unity_version": "6000.5.2f1",
            "prefab": {"path": FINALIZER.PREFAB, "guid": FINALIZER.PREFAB_GUID,
                       "local_id": FINALIZER.PREFAB_LOCAL_ID,
                       "identity_basis": "AssetDatabase.TryGetGUIDAndLocalFileIdentifier on exact loaded prefab root",
                       "asset_sha256": pins[FINALIZER.PREFAB], "meta_sha256": pins[FINALIZER.PREFAB + ".meta"]},
            "footprint": {"asset_serialized_authoring_cells": [28, 15],
                          "contents_effective_authoring_cells": [28, 15],
                          "asset_visual_bounds": {"center": [0, 0, 0], "size": [27.8, 4, 14.8],
                                                  "min": [-13.9, -2, -7.4], "max": [13.9, 2, 7.4]},
                          "asset_visual_cells": [28, 15],
                          "contents_visual_bounds": {"center": [0, 0, 0], "size": [27.8, 4, 14.8],
                                                     "min": [-13.9, -2, -7.4], "max": [13.9, 2, 7.4]},
                          "contents_visual_cells": [28, 15], "factory_cells": [28, 15],
                          "factory_method": "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition",
                          "bound_owner": "BuildingModelBounds.TryMeasure/EnclosingCells",
                          "factory_target": "disposable PrefabUtility.LoadPrefabContents root; not AssetDatabase persistent object",
                          "asset_contents_geometry_equivalence": True,
                          "resolver_may_mutate_temporary_authoring_instance": True,
                          "temporary_contents_unloaded_without_save": True,
                          "authored_configured_cells_used_as_factory_authority": False},
            "dependency_paths": [{"path": FINALIZER.PREFAB, "sha256": pins[FINALIZER.PREFAB],
                                  "meta_sha256": pins[FINALIZER.PREFAB + ".meta"]}],
            "source_pins": pins,
            "loaded_assemblies": assemblies,
            "scope": {"direct_prefab_asset_only": True, "mission_scene_opened": False,
                      "temporary_prefab_contents_scene_used": True, "open_scene_setup_changed": False,
                      "ecs_world_created_by_probe": False, "mission_setup_invoked": False,
                      "asset_repair_invoked": False, "source_files_changed": False,
                      "loaded_source_prefab_dirty_after_probe": False, "runtime_gameplay_admission": False,
                      "mission_ready": False},
        }

    def write_doc(self):
        self.capture.write_text(json.dumps(self.doc, indent=2), encoding="utf-8")

    def validate(self, exit_code=0):
        return FINALIZER.validate(self.root, self.capture, self.log, exit_code)

    def test_valid_direct_asset_probe_receipt(self):
        receipt = self.validate()
        self.assertEqual(receipt["status"], "PassedDirectAssetFactoryProbeOnly")
        self.assertFalse(receipt["scope"]["mission_ready"])

    def test_nonzero_wrapper_exit_rejected(self):
        with self.assertRaisesRegex(ValueError, "wrapper exit must be zero"):
            self.validate(1)

    def test_missing_or_duplicate_marker_rejected(self):
        self.log.write_text("\n".join([FINALIZER.MARKER, FINALIZER.MARKER]) + "\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "marker must occur once"):
            self.validate()

    def test_identity_mismatch_rejected(self):
        self.doc["prefab"]["local_id"] += 1
        self.write_doc()
        with self.assertRaisesRegex(ValueError, "exact AssetDatabase Barrack root identity"):
            self.validate()

    def test_factory_value_cannot_be_replaced_with_authored_value(self):
        self.doc["footprint"]["factory_cells"] = [29, 15]
        self.doc["footprint"]["authored_configured_cells_used_as_factory_authority"] = True
        self.write_doc()
        with self.assertRaisesRegex(ValueError, "factory/bounds owner facts"):
            self.validate()

    def test_asset_and_disposable_contents_geometry_must_match(self):
        self.doc["footprint"]["asset_visual_cells"] = [27, 15]
        self.write_doc()
        with self.assertRaisesRegex(ValueError, "asset, disposable contents, and factory footprints"):
            self.validate()

    def test_factory_must_be_marked_as_disposable_contents_target(self):
        self.doc["footprint"]["factory_target"] = "AssetDatabase persistent prefab root"
        self.write_doc()
        with self.assertRaisesRegex(ValueError, "original factory/bounds owner facts"):
            self.validate()

    def test_mission_claim_rejected(self):
        self.doc["scope"]["mission_setup_invoked"] = True
        self.write_doc()
        with self.assertRaisesRegex(ValueError, "direct asset only scope"):
            self.validate()

    def test_source_pin_drift_rejected(self):
        source = self.root / "Assets/Game/Scripts/Systems/BuildingModelBounds.cs"
        source.write_text(source.read_text(encoding="utf-8") + "\n// drift\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "pinned source changed"):
            self.validate()


if __name__ == "__main__":
    unittest.main()
