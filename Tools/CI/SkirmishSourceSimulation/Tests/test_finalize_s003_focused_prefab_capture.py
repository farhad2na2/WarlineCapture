import importlib.util
import unittest
from pathlib import Path


MODULE = Path(__file__).with_name("finalize_s003_focused_prefab_capture.py")
SPEC = importlib.util.spec_from_file_location("s003_focused_prefab_finalizer", MODULE)
FINALIZER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FINALIZER)


def fixture_capture():
    keys = ["S003_Unit_%02d" % i for i in range(19)]
    entities = []
    roots = {}
    for index, key in enumerate(keys, 1):
        entity_id = "%d:1" % index
        roots[key] = entity_id
        entities.append({"id": entity_id, "entity_enabled": True,
                         "components": {"Game.Components.SourcePrefab": {
                             "present": True, "buffer": False, "enabled": None,
                             "value": {"codec": "tag-v1"}}}})
    return keys, {"schema_version": 1, "kind": "AuthoredSkirmishPrefabReferenceData",
                  "authority": "ActualSourceAssetsAndNativeBakers_NotMissionState", "mission_id": "S003",
                  "source_scenes": ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"],
                  "requested_prefab_keys": keys, "requested_prefab_keys_sha256": FINALIZER.KEYS_SHA,
                  "diagnostic_status": "CollectedSourceOnly", "unsupported_values_preserved_verbatim": True,
                  "template_namespace_id": "a" * 32, "opening_prefabs": roots, "entities": entities,
                  "unsupported_values": [], "replay_ready": False, "fresh_world_allocator_exported": False,
                  "full_mission_initialized": False, "utility_world_created": True,
                  "utility_world_disposed": True, "mission_world_created": False,
                  "native_visual_readiness": False, "source_fit_certified": False,
                  "native_context_qualified": False, "mission_ready": False, "geometry_exported": False}


class S003FocusedPrefabFinalizerTests(unittest.TestCase):
    def setUp(self):
        self.keys, self.capture = fixture_capture()

    def test_accepts_exact_ordered_19_root_source_closure(self):
        audit = FINALIZER.audit_document(self.capture, self.keys)
        self.assertEqual(19, audit["root_count"])
        self.assertEqual(19, audit["entity_count"])
        self.assertEqual(0, audit["unsupported_count"])

    def test_rejects_root_key_order_or_membership_drift(self):
        changed = dict(self.capture)
        changed["opening_prefabs"] = dict(reversed(list(self.capture["opening_prefabs"].items())))
        with self.assertRaisesRegex(ValueError, "ordered 19-root"):
            FINALIZER.audit_document(changed, self.keys)

    def test_rejects_duplicate_or_malformed_entity_namespace(self):
        changed = dict(self.capture)
        changed["entities"] = list(self.capture["entities"])
        changed["entities"][1] = dict(changed["entities"][0])
        with self.assertRaisesRegex(ValueError, "Unique canonical entity IDs"):
            FINALIZER.audit_document(changed, self.keys)

    def test_rejects_dangling_entity_reference(self):
        changed = dict(self.capture)
        changed["entities"] = [dict(row) for row in self.capture["entities"]]
        changed["entities"][0] = {**changed["entities"][0], "components": {
            "Game.Components.SourcePrefab": {"present": True, "buffer": False, "enabled": None,
                                             "value": {"codec": "entity-v1", "id": "999:1"}}}}
        with self.assertRaisesRegex(ValueError, "dangling entity"):
            FINALIZER.audit_document(changed, self.keys)

    def test_classifies_presentation_object_ref_without_qualifying_it(self):
        changed = dict(self.capture)
        rows = [dict(row) for row in self.capture["entities"]]
        rows[0] = {**rows[0], "components": {**rows[0]["components"],
            "Game.Components.UnitAttackImpactVfxReference": {
                "present": True, "buffer": False, "enabled": None, "value": {"codec": "null-v1"}}}}
        changed["entities"] = rows
        changed["unsupported_values"] = [{"path": "1:1/Game.Components.UnitAttackImpactVfxReference/Prefab",
            "reason": "unity_object_ref_requires_bound_source_identity",
            "type": "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]"}]
        audit = FINALIZER.audit_document(changed, self.keys)
        self.assertEqual(1, audit["presentation_reference_count"])
        self.assertEqual("unresolved_presentation_object_reference",
                         audit["unsupported_classifications"][0]["classification"])
        self.assertFalse(changed["replay_ready"])
        self.assertFalse(changed["mission_ready"])

    def test_unknown_unsupported_semantics_remain_consumer_blocker(self):
        changed = dict(self.capture)
        rows = [dict(row) for row in self.capture["entities"]]
        rows[0] = {**rows[0], "components": {**rows[0]["components"],
            "Game.Components.UnknownThing": {"present": True, "buffer": False,
                                                "enabled": None, "value": {"codec": "null-v1"}}}}
        changed["entities"] = rows
        changed["unsupported_values"] = [{"path": "1:1/Game.Components.UnknownThing/Value",
            "reason": "unsupported_serialized_type", "type": "Example.Unknown"}]
        audit = FINALIZER.audit_document(changed, self.keys)
        self.assertEqual(1, audit["unclassified_unsupported_count"])

    def test_rejects_unsupported_path_without_present_component(self):
        changed = dict(self.capture)
        changed["unsupported_values"] = [{"path": "1:1/Game.Components.NotPresent/Value",
            "reason": "unsupported_serialized_type", "type": "Example.Unknown"}]
        with self.assertRaisesRegex(ValueError, "present component"):
            FINALIZER.audit_document(changed, self.keys)

    def test_rejects_any_readiness_promotion(self):
        changed = dict(self.capture)
        changed["mission_ready"] = True
        with self.assertRaisesRegex(ValueError, "false readiness"):
            FINALIZER.audit_document(changed, self.keys)

    def test_requires_unique_collected_marker_and_matching_counts(self):
        audit = FINALIZER.audit_document(self.capture, self.keys)
        output = Path("/capture/out")
        inputs = [Path("/key.json")]
        good = (FINALIZER.CAPTURE_METHOD + "\n" + str(output) + "\n/key.json\n" +
                FINALIZER.MARKER_PREFIX + "19 unsupported=0" + FINALIZER.MARKER_SUFFIX)
        FINALIZER.validate_wrapper_binding(good, output / "prefab-reference-data.json", inputs, audit)
        with self.assertRaisesRegex(ValueError, "unique"):
            FINALIZER.validate_wrapper_binding(good + "\n" + good.splitlines()[-1],
                                               output / "prefab-reference-data.json", inputs, audit)
        with self.assertRaisesRegex(ValueError, "counts"):
            FINALIZER.validate_wrapper_binding(good.replace("entities=19", "entities=18"),
                                               output / "prefab-reference-data.json", inputs, audit)

    def test_rejects_nonzero_checked_wrapper_exit(self):
        with self.assertRaisesRegex(ValueError, "exit code"):
            FINALIZER.validate_wrapper_exit(1)


if __name__ == "__main__":
    unittest.main()
