import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
REQUEST_PATH = ROOT / "T Logs/SkirmishProduction/s050-author-current-shared-20261115-r3/authoring-request.json"
MATERIALIZER = ROOT / "Assets/Game/Scripts/Editor/Skirmish/AuthoredSourceConfigGraphMaterializer.cs"
EVIDENCE = ROOT / "Assets/Game/Scripts/Editor/Skirmish/CompilerSelectedAuthoringEvidence.cs"
READBACK = ROOT / "Assets/Game/Scripts/Editor/Skirmish/CompilerSelectedUnityAssetReadback.cs"
UNIT = ROOT / "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.UnitData.cs"
PHYSICAL_EXPORT = ROOT / "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.PinnedPhysicalInputExport.cs"


def references(value):
    if isinstance(value, dict):
        if isinstance(value.get("fileID"), int) and value["fileID"] != 0 and isinstance(value.get("guid"), str):
            yield value["guid"], value["fileID"]
        else:
            for child in value.values():
                yield from references(child)
    elif isinstance(value, list):
        for child in value:
            yield from references(child)


def hydrated_closure(graph, request):
    by_path = {node["source_path"]: node for node in graph["nodes"]}
    by_guid = {node["asset_guid"]: node for node in graph["nodes"]}
    roots = list(request["profiles"].values()) + [request["campaign_source_path"]]
    pending = [by_path[path]["asset_guid"] for path in roots]
    seen = set()
    while pending:
        guid = pending.pop()
        if guid in seen:
            continue
        seen.add(guid)
        node = by_guid.get(guid)
        if node is not None:
            pending.extend(target for target, _ in references(node["fields"]) if target in by_guid)
    return seen


class CompilerSelectedUnityAssetReadbackTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.request = json.loads(REQUEST_PATH.read_text())
        cls.graph = json.loads(Path(cls.request["graph"]["path"]).read_text())
        cls.by_guid = {node["asset_guid"]: node for node in cls.graph["nodes"]}
        cls.closure = hydrated_closure(cls.graph, cls.request)
        cls.pins = cls.graph["source_pins"]

    def test_unused_legacy_preset_references_are_outside_actual_hydrated_closure(self):
        preset = next(n for n in self.graph["nodes"] if n["source_type"] == "SkirmishPresetConfig")
        preset_refs = list(references(preset["fields"]))
        self.assertEqual(len(self.closure), 8)
        self.assertNotIn(preset["asset_guid"], self.closure)
        self.assertEqual(len(preset_refs), 5)
        self.assertTrue(all(target not in self.by_guid for target, _ in preset_refs))
        self.assertEqual(sum(1 for node in self.graph["nodes"] if node["asset_guid"] in self.closure), 8)

    def test_reachable_unpinned_reference_remains_a_rejection_case(self):
        by_path = {node["source_path"]: node for node in self.graph["nodes"]}
        map_node = by_path[self.request["campaign_source_path"]]
        map_node["fields"]["syntheticControlReference"] = {
            "fileID": 11400000,
            "guid": "ffffffffffffffffffffffffffffffff",
            "type": 2,
        }
        self.assertIn(map_node["asset_guid"], self.closure)
        active_external = [
            (node["source_path"], target, file_id)
            for node in self.graph["nodes"] if node["asset_guid"] in self.closure
            for target, file_id in references(node["fields"])
            if target not in self.by_guid
        ]
        self.assertEqual(active_external, [(map_node["source_path"], "f" * 32, 11400000)])
        code = READBACK.read_text()
        self.assertIn("Serialized Unity reference targets an unpinned asset", code)
        self.assertIn('sourcePins[absolute + ".meta"]', code)

    def test_owner_passes_materializer_hydration_and_assetdb_identity_evidence(self):
        materializer = MATERIALIZER.read_text()
        evidence = EVIDENCE.read_text()
        readback = READBACK.read_text()
        unit = UNIT.read_text()
        physical_export = PHYSICAL_EXPORT.read_text()
        self.assertIn("HydratedAssetGuids", materializer)
        self.assertIn("out JArray hydratedAssetGuids", evidence)
        self.assertIn("values.HydratedAssetGuids", evidence)
        self.assertIn("out JArray hydratedAssetGuids", unit)
        self.assertIn("compilerPins, hydratedAssetGuids", unit)
        self.assertIn("out _,out JArray hydratedAssetGuids", physical_export)
        self.assertIn("pins,hydratedAssetGuids", physical_export)
        self.assertIn("AssetDatabase.LoadAllAssetsAtPath(relative)", readback)
        self.assertIn("AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate", readback)
        self.assertIn("owner=", readback)
        self.assertIn("field=", readback)
        self.assertIn('"asset_field_value_parity_proven"] = false', readback)


if __name__ == "__main__":
    unittest.main()
