import importlib.util
from pathlib import Path
import tempfile
import unittest


HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location("resource_probe", HERE / "generate.py")
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
MANIFEST_SPEC = importlib.util.spec_from_file_location("resource_manifest", HERE / "prepare_manifest.py")
MANIFEST_MODULE = importlib.util.module_from_spec(MANIFEST_SPEC)
MANIFEST_SPEC.loader.exec_module(MANIFEST_MODULE)
REPO = HERE.parents[4]


class ResourceProductionProbeTests(unittest.TestCase):
    def test_projects_original_owner_calls_and_runtime_kernels(self):
        with tempfile.TemporaryDirectory() as temp:
            result = MODULE.generate(REPO, Path(temp) / "probe")
            self.assertEqual(result["kind"], "S073OriginalFactionResourceProductionOwnerSourceProbe")
            self.assertEqual(len(result["native_members"]), 7)
            owner = (Path(temp) / "probe" / "FactionResourceProductionOwner.members.cs.txt").read_text()
            system = (Path(temp) / "probe" / "BuildingResourceProductionEcsSystem.cs.txt").read_text()
            self.assertIn("BuildingResourceProductionEcsSystem.ApplyStorageQuery(", owner)
            self.assertIn("BuildingResourceProductionEcsSystem.ApplyTick(", owner)
            self.assertIn("BuildingResourceProductionProfileComponent", system)
            self.assertIn("public static TickResult ApplyStorageQuery(", system)
            self.assertIn("public static TickResult ApplyProfileTick(", system)
            self.assertFalse(any("stub" in p.read_text().lower() for p in (Path(temp) / "probe").glob("*.txt")))

    def test_rejects_missing_live_owner_seam(self):
        source = (REPO / MODULE.OWNER).read_text()
        self.assertIn("_storageQueryCache.Get(entityManager)", source)
        self.assertIn("SyncRuntimeBuildingsFromEcsStorage(entityManager, buildings)", source)

    def test_native_owner_query_and_fallback_order_remain_real(self):
        with tempfile.TemporaryDirectory() as temp:
            MODULE.generate(REPO, Path(temp) / "probe")
            owner = (Path(temp) / "probe" / "FactionResourceProductionOwner.members.cs.txt").read_text()
            query = owner.index("_storageQueryCache.Get(entityManager)")
            batch = owner.index("BuildingResourceProductionEcsSystem.ApplyStorageQuery(", query)
            mirror = owner.index("SyncRuntimeBuildingsFromEcsStorage(entityManager, buildings)", batch)
            fallback_guard = owner.index("if (buildings == null || buildings.Count == 0)", mirror)
            fallback_update = owner.index("UpdateRuntimeBuildingResourceProduction(", fallback_guard)
            self.assertLess(query, batch)
            self.assertLess(batch, mirror)
            self.assertLess(mirror, fallback_guard)
            self.assertLess(fallback_guard, fallback_update)
            self.assertIn("foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in buildingMap)", owner)
            self.assertIn("BuildingResourceProductionEcsSystem.ApplyTick(", owner)
            self.assertIn("SyncResourceStorageMetadata(entityManager, building, ref storage)", owner)

    def test_alternate_manifest_retains_actual_native_owner_rows(self):
        source_manifest = REPO / "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json"
        with tempfile.TemporaryDirectory() as temp:
            out = Path(temp) / "resource-owner-manifest.json"
            proof = MANIFEST_MODULE.prepare(source_manifest, out)
            self.assertEqual(len(proof["owner_rows"]), 3)
            manifest = __import__("json").loads(out.read_text())
            rows = {row["id"]: row for row in manifest["sources"]}
            self.assertEqual(rows["407_FactionResourceCompositionSystemHelper.cs"]["path"], MODULE.OWNER)
            self.assertEqual(rows["284_BuildingResourceProductionEcsSystem.cs"]["path"], MODULE.SYSTEM)
            self.assertTrue(manifest["experimental_only"])


if __name__ == "__main__":
    unittest.main()
