#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "architecture_lifecycle_inventory.py"
SPEC = importlib.util.spec_from_file_location("architecture_lifecycle_inventory", SCRIPT)
assert SPEC and SPEC.loader
inventory = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = inventory
SPEC.loader.exec_module(inventory)


class ArchitectureLifecycleInventoryTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.revision = "a" * 40
        self.tree = "b" * 40
        for relative in inventory.AUTHORITY_PATHS:
            self.write(relative, f"authority:{relative}\n")
        self.write(
            "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json",
            json.dumps({
                "activeWorkOwnership": {
                    "owners": [
                        {
                            "authorityPath": "Design/Audio_Config_Driven_Implementation_Spec.md",
                            "id": "audio",
                            "protectedPaths": ["Assets/Game/Scripts/Audio/**"],
                            "status": "active",
                        },
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

    def write(self, relative: str, value: str) -> Path:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(value, encoding="utf-8")
        return path

    def seed_complete_lifecycle(self) -> None:
        self.write(
            "Assets/Game/Scripts/Runtime/LifecycleSceneRootView.cs",
            "using System.Collections.Generic;\n"
            "using Unity.Collections;\n"
            "using Unity.Entities;\n"
            "using UnityEngine;\n"
            "using UnityEngine.SceneManagement;\n"
            "public sealed class LifecycleSceneRootView : MonoBehaviour\n"
            "{\n"
            "    private World _world;\n"
            "    private NativeList<int> _values;\n"
            "    private EntityQuery _query;\n"
            "    private Stack<GameObject> _presentationPool;\n"
            "    private static readonly Dictionary<int, int> Cache = new();\n"
            "    private void Awake()\n"
            "    {\n"
            "        _values = new NativeList<int>(Allocator.Persistent);\n"
            "        SceneManager.sceneLoaded += HandleSceneLoaded;\n"
            "        NativeList<int> localValues = new NativeList<int>(Allocator.Temp);\n"
            "        Stack<GameObject> localPool = new();\n"
            "    }\n"
            "    private void Update() { _presentationPool.Clear(); }\n"
            "    private void OnDisable() { SceneManager.sceneLoaded -= HandleSceneLoaded; }\n"
            "    private void OnDestroy()\n"
            "    {\n"
            "        _values.Dispose();\n"
            "        _presentationPool.Clear();\n"
            "        Cache.Clear();\n"
            "    }\n"
            "    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) { }\n"
            "}\n"
            "public partial struct RuntimeQuerySystem : ISystem\n"
            "{\n"
            "    private EntityQuery _systemQuery;\n"
            "    public void OnCreate(ref SystemState state) { }\n"
            "}\n"
            "public sealed class NestedHost\n"
            "{\n"
            "    private sealed class NestedOwner\n"
            "    {\n"
            "        private void Start()\n"
            "        {\n"
            "            _ = World.DefaultGameObjectInjectionWorld;\n"
            "        }\n"
            "    }\n"
            "}\n",
        )

    def test_inventory_covers_lifecycle_surfaces_and_attributes_nested_owners(self) -> None:
        self.seed_complete_lifecycle()
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        categories = data["categories"]

        self.assertEqual(data["summary"]["worldOwnerCount"], 2)
        self.assertEqual(data["summary"]["worldDefaultAccessCount"], 1)
        self.assertEqual([item["ownerType"] for item in categories["worlds"]], [
            "LifecycleSceneRootView",
            "NestedOwner",
        ])
        self.assertEqual(len(categories["nativeContainers"]), 1)
        self.assertEqual(data["summary"]["persistentNativeContainerCount"], 1)
        self.assertTrue(categories["nativeContainers"][0]["persistentAllocatorObserved"])
        self.assertEqual(categories["nativeContainers"][0]["cleanupMethodsObserved"], ["OnDestroy"])
        self.assertEqual(len(categories["queryCaches"]), 2)
        self.assertEqual(
            {item["ownerType"]: item["lifecycleDisposition"] for item in categories["queryCaches"]},
            {
                "LifecycleSceneRootView": "classification-required",
                "RuntimeQuerySystem": "ecs-system-owned",
            },
        )
        self.assertEqual(len(categories["presentationPools"]), 1)
        self.assertEqual(categories["presentationPools"][0]["cleanupMethodsObserved"], ["OnDestroy"])
        self.assertEqual(len(categories["sceneRoots"]), 1)
        self.assertEqual(len(categories["subscriptions"]), 1)
        self.assertEqual(categories["subscriptions"][0]["unsubscribeMethodsObserved"], ["OnDisable"])
        self.assertEqual(len(categories["staticCaches"]), 1)
        self.assertEqual(categories["staticCaches"][0]["resetMethodsObserved"], ["OnDestroy"])

    def test_non_lifecycle_cleanup_and_unsubscribe_do_not_satisfy_contract(self) -> None:
        self.write(
            "Assets/Game/Scripts/Runtime/UnownedLifetime.cs",
            "using System.Collections.Generic;\n"
            "using Unity.Collections;\n"
            "public sealed class UnownedPresentationLifetime\n"
            "{\n"
            "    private NativeList<int> _values;\n"
            "    private Stack<int> _pool;\n"
            "    private static Dictionary<int, int> Cache;\n"
            "    private void OnEnable() { Source.Changed += HandleChanged; }\n"
            "    private void Tick()\n"
            "    {\n"
            "        _values.Dispose();\n"
            "        _pool.Clear();\n"
            "        Cache.Clear();\n"
            "        Source.Changed -= HandleChanged;\n"
            "    }\n"
            "}\n",
        )
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        self.assertFalse(data["categories"]["nativeContainers"][0]["cleanupObserved"])
        self.assertFalse(data["categories"]["presentationPools"][0]["cleanupObserved"])
        self.assertFalse(data["categories"]["staticCaches"][0]["resetObserved"])
        self.assertTrue(data["categories"]["subscriptions"][0]["pairedUnsubscribeObserved"])
        self.assertFalse(data["categories"]["subscriptions"][0]["teardownUnsubscribeObserved"])

    def test_scanner_ignores_comments_strings_locals_and_editor_sources(self) -> None:
        self.write(
            "Assets/Game/Scripts/Runtime/Comments.cs",
            "public sealed class Comments\n"
            "{\n"
            "    private string _text = \"NativeList<int> _falseField; Source.Changed += Handler;\";\n"
            "    // private NativeList<int> _commentedField;\n"
            "    private void Start() { NativeList<int> localValues; }\n"
            "}\n",
        )
        self.write(
            "Assets/Game/Scripts/Editor/EditorOnly.cs",
            "public sealed class EditorOnly { private NativeList<int> _ignored; }\n",
        )
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        self.assertEqual(data["summary"]["nativeContainerCount"], 0)
        self.assertEqual(data["summary"]["subscriptionCount"], 0)

    def test_outputs_regenerate_byte_identically(self) -> None:
        self.seed_complete_lifecycle()
        inventory.write_inventory(
            self.root, self.revision, self.tree, "out/first.json", "out/first.md"
        )
        inventory.write_inventory(
            self.root, self.revision, self.tree, "out/second.json", "out/second.md"
        )
        self.assertEqual((self.root / "out/first.json").read_bytes(), (self.root / "out/second.json").read_bytes())
        self.assertEqual((self.root / "out/first.md").read_bytes(), (self.root / "out/second.md").read_bytes())

    def test_invalid_identity_missing_authority_and_unsorted_owners_fail_closed(self) -> None:
        with self.assertRaisesRegex(ValueError, "exact 40-character"):
            inventory.build_inventory(self.root, "short", self.tree)
        (self.root / inventory.AUTHORITY_PATHS[0]).unlink()
        with self.assertRaisesRegex(ValueError, "required authority is missing"):
            inventory.build_inventory(self.root, self.revision, self.tree)
        self.write(inventory.AUTHORITY_PATHS[0], "restored\n")
        ownership_path = self.root / "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json"
        ownership = json.loads(ownership_path.read_text(encoding="utf-8"))
        ownership["activeWorkOwnership"]["owners"].reverse()
        ownership_path.write_text(json.dumps(ownership), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "sorted by id"):
            inventory.build_inventory(self.root, self.revision, self.tree)


if __name__ == "__main__":
    unittest.main()
