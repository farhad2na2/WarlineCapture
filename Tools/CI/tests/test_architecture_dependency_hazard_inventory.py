#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "architecture_dependency_hazard_inventory.py"
sys.path.insert(0, str(SCRIPT.parent))
SPEC = importlib.util.spec_from_file_location("architecture_dependency_hazard_inventory", SCRIPT)
assert SPEC and SPEC.loader
inventory = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = inventory
SPEC.loader.exec_module(inventory)


class ArchitectureDependencyHazardInventoryTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.revision = "a" * 40
        self.tree = "b" * 40
        for relative in inventory.AUTHORITY_PATHS:
            self.write(relative, f"authority:{relative}\n")
        for relative in inventory.TOOL_PATHS:
            self.write(relative, f"tool:{relative}\n")
        self.write(
            "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json",
            json.dumps({
                "activeWorkOwnership": {
                    "owners": [
                        {
                            "authorityPath": "Design/Architecture/operation_map_scene_split_and_generator_tracker.md",
                            "id": "operation-map",
                            "protectedPaths": ["Assets/Game/Scripts/**/*OperationMap*.cs"],
                            "status": "active",
                        },
                    ],
                },
            }),
        )

    def tearDown(self) -> None:
        self.temp.cleanup()

    def write(self, relative: str, content: str) -> Path:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        return path

    def seed_hazards(self) -> None:
        self.write(
            "Assets/Game/Scripts/Runtime/OperationMapRuntimeHost.cs",
            "using System.Collections.Generic;\n"
            "using Unity.Entities;\n"
            "using UnityEngine;\n"
            "using UnityEngine.SceneManagement;\n"
            "public sealed class OperationMapRuntimeHost : MonoBehaviour\n"
            "{\n"
            "    private static readonly Dictionary<int, Entity> EntityCache = new();\n"
            "    private static OperationMapRuntimeHost _instance;\n"
            "    public static OperationMapRuntimeHost Instance { get; private set; }\n"
            "    private void Awake()\n"
            "    {\n"
            "        _ = World.DefaultGameObjectInjectionWorld;\n"
            "        SceneManager.sceneLoaded += HandleSceneLoaded;\n"
            "        _ = Object.FindFirstObjectByType<Camera>();\n"
            "        _ = transform.Find(\"Child\");\n"
            "        _ = Camera.main;\n"
            "        _ = gameObject.scene.GetRootGameObjects();\n"
            "    }\n"
            "    private void OnDestroy() { SceneManager.sceneLoaded -= HandleSceneLoaded; }\n"
            "    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) { }\n"
            "}\n",
        )
        self.write(
            "Assets/Game/Scripts/Runtime/ServiceConsumer.cs",
            "public sealed class ServiceConsumer\n"
            "{\n"
            "    private void Start() { _ = ServiceLocator.Resolve<IClock>(); }\n"
            "}\n",
        )

    def test_inventory_detects_all_five_categories_and_routes_follow_up(self) -> None:
        self.seed_hazards()
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        self.assertEqual(data["summary"]["globalWorldLookupsCount"], 1)
        self.assertEqual(data["summary"]["mutableStaticCachesCount"], 3)
        self.assertEqual(data["summary"]["staticEventSubscriptionsCount"], 1)
        self.assertEqual(data["summary"]["runtimeObjectDiscoveryCount"], 4)
        self.assertEqual(data["summary"]["hiddenSingletonsCount"], 3)
        self.assertEqual(data["summary"]["findingCount"], 12)
        self.assertEqual(data["summary"]["protectedFindingCount"], 11)
        self.assertEqual(
            data["categories"]["globalWorldLookups"][0]["followUpTasks"],
            ["AM-019", "AM-022"],
        )
        self.assertEqual(
            data["categories"]["staticEventSubscriptions"][0]["followUpTasks"],
            ["AM-021", "AM-022"],
        )
        self.assertEqual(
            {row["symbol"] for row in data["categories"]["hiddenSingletons"]},
            {"_instance", "Instance", "ServiceLocator.Resolve<"},
        )
        self.assertTrue(all(row["responsibleOwner"] for rows in data["categories"].values() for row in rows))
        self.assertTrue(all(row["disposition"] for rows in data["categories"].values() for row in rows))
        self.assertTrue(all(row["rationale"] for rows in data["categories"].values() for row in rows))
        self.assertTrue(data["categories"]["staticEventSubscriptions"][0]["pairedUnsubscribeObserved"])
        self.assertEqual(data["summary"]["mutableStaticCacheCandidateCount"], 1)
        self.assertEqual(data["summary"]["mutableStaticLifecycleStateCount"], 2)

    def test_exact_handoff_path_is_not_protected_by_broader_owner_glob(self) -> None:
        path = "Assets/Game/Scripts/Runtime/OperationMapRuntimeHost.cs"
        exclusions = [{
            "id": "operation-map",
            "protectedPaths": ["Assets/Game/Scripts/**/*OperationMap*.cs"],
            "handoffPaths": [path],
        }]
        self.assertEqual(inventory.protected_owner_ids(path, exclusions), [])
        self.assertEqual(
            inventory.protected_owner_ids(
                "Assets/Game/Scripts/Runtime/OtherOperationMapOwner.cs",
                exclusions,
            ),
            ["operation-map"],
        )

    def test_hsl_world_and_authoring_discovery_use_disposition_specific_routes(self) -> None:
        self.write(
            "Assets/Game/Scripts/Composition/MatchIntroEcsStateQuery.cs",
            "public sealed class MatchIntroEcsStateQuery\n"
            "{\n"
            "    public void Read() { _ = World.DefaultGameObjectInjectionWorld; }\n"
            "}\n",
        )
        self.write(
            "Assets/Game/Scripts/Authorings/ExampleAuthoring.cs",
            "public sealed class ExampleBaker\n"
            "{\n"
            "    public void Bake() { _ = transform.Find(\"Model\"); }\n"
            "}\n",
        )
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        world_row = data["categories"]["globalWorldLookups"][0]
        self.assertEqual(world_row["disposition"], "HSL")
        self.assertEqual(world_row["followUpTasks"], ["AM-019", "AM-020", "AM-022"])
        discovery_row = data["categories"]["runtimeObjectDiscovery"][0]
        self.assertEqual(discovery_row["disposition"], "AD")
        self.assertEqual(discovery_row["followUpTasks"], [])

    def test_scanner_ignores_comments_strings_editor_sources_and_instance_events(self) -> None:
        self.write(
            "Assets/Game/Scripts/Runtime/FalseSignals.cs",
            "public sealed class FalseSignals\n"
            "{\n"
            "    private string Text = \"World.DefaultGameObjectInjectionWorld GameObject.Find(\";\n"
            "    private void Start() { localButton.Clicked += HandleClicked; }\n"
            "    private static readonly ProfilerMarker MinimapUpdateMarker;\n"
            "    private static readonly ProfilerMarker ExtractQueryMarker;\n"
            "#if UNITY_EDITOR\n"
            "    private static GameObject EditorOnlyRoot;\n"
            "#endif\n"
            "    private void Resolve() { _ = Shader.Find(\"Hidden/Test\"); }\n"
            "    // private static Dictionary<int, int> Cache;\n"
            "    private void HandleClicked() { }\n"
            "}\n",
        )
        self.write(
            "Assets/Game/Scripts/Editor/EditorSignals.cs",
            "public sealed class EditorSignals { private void Run() { _ = GameObject.Find(\"x\"); } }\n",
        )
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        self.assertEqual(data["summary"]["findingCount"], 0)

    def test_outputs_regenerate_byte_identically(self) -> None:
        self.seed_hazards()
        inventory.write_inventory(self.root, self.revision, self.tree, "out/first.json", "out/first.md")
        inventory.write_inventory(self.root, self.revision, self.tree, "out/second.json", "out/second.md")
        self.assertEqual((self.root / "out/first.json").read_bytes(), (self.root / "out/second.json").read_bytes())
        self.assertEqual((self.root / "out/first.md").read_bytes(), (self.root / "out/second.md").read_bytes())

    def test_invalid_identity_and_missing_authority_fail_closed(self) -> None:
        with self.assertRaisesRegex(ValueError, "exact 40-character"):
            inventory.build_inventory(self.root, "short", self.tree)
        (self.root / inventory.AUTHORITY_PATHS[0]).unlink()
        with self.assertRaisesRegex(ValueError, "required authority is missing"):
            inventory.build_inventory(self.root, self.revision, self.tree)


if __name__ == "__main__":
    unittest.main()
